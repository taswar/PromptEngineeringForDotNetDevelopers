using Microsoft.Extensions.AI;
using Polly;
using Polly.Retry;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DocumentSummary;

/// <summary>
/// A structured summary of a document, returned by <see cref="DocumentSummaryService"/>.
/// Property names match the snake_case keys the model is instructed to produce.
/// </summary>
public record DocumentSummary(
    [property: JsonPropertyName("title")]                   string   Title,
    [property: JsonPropertyName("key_points")]              string[] KeyPoints,
    [property: JsonPropertyName("sentiment")]               string   Sentiment,
    [property: JsonPropertyName("estimated_read_minutes")]  int      EstimatedReadMinutes
);

/// <summary>
/// Takes document text and returns a typed <see cref="DocumentSummary"/>, reliably.
/// Uses prompt-based JSON constraints (works with LM Studio and any OpenAI-compatible backend),
/// streaming accumulation for progress feedback, and exponential-backoff retry for transient failures.
/// </summary>
public sealed class DocumentSummaryService
{
    // ── JSON options ─────────────────────────────────────────────────────────
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true   // model may capitalise inconsistently
    };

    // ── Constraints ───────────────────────────────────────────────────────────
    private static readonly string[] ValidSentiments = ["positive", "neutral", "negative"];

    /// <summary>
    /// Rough character limit before a request is rejected without calling the model.
    /// Based on ~4 chars/token; keeps total context under 8k tokens for smaller local models.
    /// </summary>
    private const int MaxDocumentLength = 32_000;

    // ── System prompt ─────────────────────────────────────────────────────────
    // Prompt-based constraints work with every backend including LM Studio.
    // JSON Schema mode (ChatOptions.ResponseFormat) is omitted here because
    // LM Studio support is inconsistent — see §6.2 of the chapter.
    private const string SystemPrompt = """
        You are a document analysis assistant. Your sole task is to return a structured JSON summary.

        CRITICAL RULES — follow exactly:
        - Return ONLY valid JSON. Nothing else.
        - Do NOT wrap the JSON in ```json or ``` blocks.
        - Do NOT include any prose, explanation, or commentary before or after the JSON.
        - Your response must start with { and end with }.

        Return a JSON object matching this schema exactly:
        {
          "title": "<concise document title>",
          "key_points": ["<point 1>", "<point 2>", "<point 3>"],
          "sentiment": "<positive|neutral|negative>",
          "estimated_read_minutes": <integer>
        }

        Constraints:
        - key_points: exactly 3 to 5 string items
        - sentiment: MUST be exactly one of: positive, neutral, negative
        - estimated_read_minutes: positive integer, based on ~200 words/minute reading speed
        """;

    // ── Fields ────────────────────────────────────────────────────────────────
    private readonly IChatClient _client;
    private readonly ResiliencePipeline _pipeline;

    public DocumentSummaryService(IChatClient client)
    {
        _client = client;

        // Polly v8 via Microsoft.Extensions.Resilience.
        // Same pattern as the HttpClient retry policy you already have — different target.
        // Strategy order: timeout is OUTER, retry is INNER.
        // Each retry attempt is bounded by the inner per-call context, but the outer
        // timeout provides a single ceiling across all attempts.
        _pipeline = new ResiliencePipelineBuilder()
            .AddTimeout(TimeSpan.FromSeconds(60))   // outer: one budget for all attempts
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    Console.Error.WriteLine(
                        $"  [retry #{args.AttemptNumber + 1}] {args.Outcome.Exception?.Message}");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Summarises <paramref name="document"/> and returns a typed <see cref="DocumentSummary"/>.
    /// Writes progress dots to stderr while the model streams.
    /// Retries up to 3 times with exponential backoff on any transient failure.
    /// </summary>
    public async Task<DocumentSummary> SummarizeAsync(string document, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(document);

        // Cheap pre-flight check — context overflow is not retryable.
        if (document.Length > MaxDocumentLength)
            throw new ArgumentException(
                $"Document length ({document.Length:N0} chars) exceeds safe limit " +
                $"({MaxDocumentLength:N0} chars ≈ 8k tokens). Chunk it first.",
                nameof(document));

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, $"Summarize this document:\n\n{document}")
        };

        var options = new ChatOptions
        {
            Temperature  = 0f,   // float?, not double — deterministic output
            MaxOutputTokens = 512
        };

        DocumentSummary? result = null;

        // The pipeline wraps both the HTTP call and the parse.
        // If the model returns garbage JSON, ParseWithDefensiveHandling throws,
        // and the retry sends a fresh request. Not every parse failure is retryable
        // in theory, but in practice re-trying usually gets a cleaner response.
        await _pipeline.ExecuteAsync(async (pct) =>
        {
            var rawJson = await StreamAndAccumulateAsync(messages, options, pct);
            result = ParseWithDefensiveHandling(rawJson);
        }, ct);

        return result!;
    }

    // ── Private: streaming ────────────────────────────────────────────────────

    /// <summary>
    /// Streams the model response, writing a progress dot per token chunk to stderr.
    /// Returns the fully accumulated text once the stream closes.
    ///
    /// Streaming and structured output are mostly incompatible — you cannot parse
    /// JSON as it arrives because you don't have the complete object yet.
    /// The dots give the user proof that something is happening. That's it.
    /// </summary>
    private async Task<string> StreamAndAccumulateAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions options,
        CancellationToken ct)
    {
        var accumulated = new StringBuilder();

        Console.Error.Write("Analyzing");

        await foreach (var update in _client.GetStreamingResponseAsync(messages, options, ct))
        {
            // Not every StreamingChatMessageUpdate contains text —
            // some carry role or finish-reason metadata. Always null-check.
            if (update.Text is not null)
            {
                accumulated.Append(update.Text);
                Console.Error.Write(".");
            }
        }

        Console.Error.WriteLine(" done.");
        return accumulated.ToString();
    }

    // ── Private: defensive parsing ────────────────────────────────────────────

    private static DocumentSummary ParseWithDefensiveHandling(string rawOutput)
    {
        if (string.IsNullOrWhiteSpace(rawOutput))
            throw new InvalidOperationException("Model returned empty output.");

        var cleaned = CleanRawOutput(rawOutput);

        try
        {
            var summary = JsonSerializer.Deserialize<DocumentSummary>(cleaned, JsonOptions)
                ?? throw new InvalidOperationException("Deserialized to null.");

            ValidateSummary(summary);
            return summary;
        }
        catch (JsonException ex)
        {
            // Surface a preview so the caller (and the retry log) can see what went wrong.
            var preview = rawOutput.Length > 200 ? rawOutput[..200] + "…" : rawOutput;
            throw new InvalidOperationException(
                $"Model returned invalid JSON after cleaning. Preview: {preview}", ex);
        }
    }

    /// <summary>
    /// Validates semantic constraints that JSON deserialization cannot enforce.
    /// Throws <see cref="InvalidOperationException"/> on violations — retryable by the pipeline.
    /// </summary>
    private static void ValidateSummary(DocumentSummary summary)
    {
        if (string.IsNullOrWhiteSpace(summary.Title))
            throw new InvalidOperationException("title is null or empty.");

        if (summary.EstimatedReadMinutes <= 0)
            throw new InvalidOperationException(
                $"estimated_read_minutes is {summary.EstimatedReadMinutes}; must be a positive integer.");

        if (summary.KeyPoints.Length is < 3 or > 5)
            throw new InvalidOperationException(
                $"key_points has {summary.KeyPoints.Length} item(s); expected 3–5.");

        if (!ValidSentiments.Contains(summary.Sentiment, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"sentiment is '{summary.Sentiment}'; expected one of: positive, neutral, negative.");
    }

    /// <summary>
    /// Handles the four classic LLM output failure modes before deserialization:
    ///   1. Markdown code fences  (```json … ```)
    ///   2. Leading prose before the JSON object
    ///   3. Trailing text after the JSON closes
    ///   4. Whitespace padding
    ///
    /// Like trimming whitespace before int.Parse — the data is there, it just needs cleaning.
    /// </summary>
    private static string CleanRawOutput(string raw)
    {
        var text = raw.Trim();

        // Failure mode 1: markdown fences — yes, the model wraps JSON in fences
        // even when you explicitly told it not to. Every time. Strip and move on.
        if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            text = text["```json".Length..];
        else if (text.StartsWith("```"))
            text = text["```".Length..];

        if (text.EndsWith("```"))
            text = text[..^3];

        text = text.Trim();

        // Failure mode 2: leading prose — find where the JSON object actually starts.
        var jsonStart = text.IndexOf('{');
        if (jsonStart > 0)
            text = text[jsonStart..];

        // Failure mode 3 / 4: trailing text or whitespace — trim after the closing brace.
        var jsonEnd = text.LastIndexOf('}');
        if (jsonEnd >= 0 && jsonEnd < text.Length - 1)
            text = text[..(jsonEnd + 1)];

        return text;
    }
}

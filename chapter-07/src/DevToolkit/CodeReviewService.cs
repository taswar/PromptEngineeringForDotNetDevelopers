using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevToolkit;

// ── Output records ─────────────────────────────────────────────────────────────

/// <summary>A single code review finding with location, issue, severity, and fix.</summary>
public record CodeReviewFinding(
    [property: JsonPropertyName("location")] string Location,
    [property: JsonPropertyName("issue")]    string Issue,
    [property: JsonPropertyName("severity")] string Severity,
    [property: JsonPropertyName("fix")]      string Fix
);

/// <summary>The full code review result — a list of findings.</summary>
public record CodeReviewResult(
    [property: JsonPropertyName("findings")] CodeReviewFinding[] Findings
);

// ── Service ────────────────────────────────────────────────────────────────────

/// <summary>
/// Reviews C# code and returns structured findings tagged with severity.
/// Uses the PromptBuilder from Chapter 4 to assemble the system prompt,
/// with an optional <see cref="DevToolkitOptions"/> for codebase context.
/// </summary>
public sealed class CodeReviewService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IChatClient _client;
    private readonly string _systemPrompt;

    public CodeReviewService(IChatClient client, DevToolkitOptions? options = null)
    {
        _client = client;
        _systemPrompt = BuildSystemPrompt(options ?? DevToolkitOptions.Default);
    }

    /// <summary>
    /// Reviews the provided C# code and returns a <see cref="CodeReviewResult"/>.
    /// Streams the model response for progress feedback, then parses the JSON.
    /// </summary>
    public async Task<CodeReviewResult> ReviewAsync(string code, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, _systemPrompt),
            new(ChatRole.User, $"Review this C# code:\n\n{code}")
        };

        // Temperature 0f — deterministic output. Run the same code twice, get
        // the same findings. Useful for regression tracking across PR iterations.
        var options = new ChatOptions
        {
            Temperature     = 0f,
            MaxOutputTokens = 1024
        };

        var raw = await StreamAndAccumulateAsync(messages, options, "Reviewing", ct);
        return ParseReviewResult(raw);
    }

    // ── Prompt construction ───────────────────────────────────────────────────

    private static string BuildSystemPrompt(DevToolkitOptions opts) =>
        new PromptBuilder()
            .WithRole("""
                You are a senior C# code reviewer. You identify real issues:
                security flaws, bugs, performance problems, API misuse,
                and maintainability concerns.
                You do not comment on formatting or naming style.
                """)
            .WithContext($"Codebase context: {opts.CodebaseContext}")
            .WithTask("""
                Review the provided C# code and return a JSON object with findings.
                Each finding must have:
                - location: method or line reference (e.g. "ProcessPayment(), line 12")
                - issue: concise description of the problem
                - severity: exactly one of "critical", "warning", or "info"
                - fix: a concrete recommendation (1-2 sentences)
                """)
            .WithConstraints("""
                CRITICAL RULES:
                - Return ONLY valid JSON. Nothing else.
                - Do NOT wrap the output in markdown code fences.
                - Your response must start with { and end with }.
                - Return a JSON object with a single "findings" array.
                - If there are no issues, return: {"findings":[]}
                """)
            .WithExample(
                "public string GetUser(int id) => db.Query(\"SELECT * FROM users WHERE id=\" + id);",
                """{"findings":[{"location":"GetUser()","issue":"SQL injection via string concatenation","severity":"critical","fix":"Use parameterised queries: db.Query(\"SELECT * FROM users WHERE id=@id\", new {id})"}]}""")
            .Build();

    // ── Streaming + parsing ───────────────────────────────────────────────────

    private async Task<string> StreamAndAccumulateAsync(
        List<ChatMessage> messages,
        ChatOptions options,
        string progressLabel,
        CancellationToken ct)
    {
        var accumulated = new StringBuilder();
        Console.Error.Write(progressLabel);
        await foreach (var update in _client.GetStreamingResponseAsync(messages, options, ct))
        {
            if (update.Text is not null)
            {
                accumulated.Append(update.Text);
                Console.Error.Write(".");
            }
        }
        Console.Error.WriteLine(" done.");
        return accumulated.ToString();
    }

    private static CodeReviewResult ParseReviewResult(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("Model returned empty output.");

        var cleaned = OutputCleaner.Clean(raw);

        try
        {
            return JsonSerializer.Deserialize<CodeReviewResult>(cleaned, JsonOptions)
                ?? throw new InvalidOperationException("Deserialized to null.");
        }
        catch (JsonException ex)
        {
            var preview = raw.Length > 200 ? raw[..200] + "…" : raw;
            throw new InvalidOperationException(
                $"Model returned invalid JSON. Preview: {preview}", ex);
        }
    }
}

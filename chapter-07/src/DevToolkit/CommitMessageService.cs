using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevToolkit;

// ── Output records ─────────────────────────────────────────────────────────────

/// <summary>
/// A structured commit message following the Conventional Commits specification.
/// </summary>
public record CommitMessage(
    [property: JsonPropertyName("subject")]          string   Subject,
    [property: JsonPropertyName("body_bullets")]     string[] BodyBullets,
    [property: JsonPropertyName("breaking_changes")] string[] BreakingChanges
);

/// <summary>A structured pull request description.</summary>
public record PrDescription(
    [property: JsonPropertyName("title")]         string   Title,
    [property: JsonPropertyName("description")]   string[] DescriptionBullets,
    [property: JsonPropertyName("testing_notes")] string[] TestingNotes
);

// ── Service ────────────────────────────────────────────────────────────────────

/// <summary>
/// Generates conventional commit messages and PR descriptions from git diffs.
/// </summary>
/// <remarks>
/// The model sees what changed, not why. Review the generated subject line —
/// intent is yours to add. The body bullets describe the mechanics of the change.
/// </remarks>
public sealed class CommitMessageService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IChatClient _client;
    private readonly DevToolkitOptions _options;

    public CommitMessageService(IChatClient client, DevToolkitOptions? options = null)
    {
        _client = client;
        _options = options ?? DevToolkitOptions.Default;
    }

    /// <summary>
    /// Generates a conventional commit message from <paramref name="diff"/>.
    /// </summary>
    /// <param name="diff">A git diff (staged) or plain description of the change.</param>
    public async Task<CommitMessage> GenerateCommitMessageAsync(
        string diff,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(diff);

        var systemPrompt = BuildCommitSystemPrompt(_options);
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, $"Generate a commit message for this diff:\n\n{diff}")
        };

        var raw = await CallAndAccumulateAsync(messages, ct);
        return ParseResult<CommitMessage>(raw);
    }

    /// <summary>
    /// Generates a pull request description from <paramref name="diff"/>.
    /// </summary>
    public async Task<PrDescription> GeneratePrDescriptionAsync(
        string diff,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(diff);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, PrSystemPrompt),
            new(ChatRole.User, $"Generate a PR description for this diff:\n\n{diff}")
        };

        var raw = await CallAndAccumulateAsync(messages, ct);
        return ParseResult<PrDescription>(raw);
    }

    // ── Prompt construction ───────────────────────────────────────────────────

    private static string BuildCommitSystemPrompt(DevToolkitOptions opts) => $$"""
        You are a commit message author following the {{opts.CommitFormat}} specification.
        Given a git diff or a description of changes, produce a structured commit message.

        Conventional Commits subject format:
        <type>(<optional-scope>): <short description>

        Types: feat, fix, docs, style, refactor, test, chore, perf, ci
        The subject line must be 72 characters or fewer.

        Return a JSON object matching this schema exactly:
        {
          "subject": "<type(scope): short description>",
          "body_bullets": ["<what changed and why>", "..."],
          "breaking_changes": []
        }

        CRITICAL RULES:
        - Return ONLY valid JSON. Nothing else.
        - Do NOT wrap the output in markdown code fences.
        - Your response must start with { and end with }.
        - body_bullets: 2–5 items explaining what changed and why, not just what the diff shows
        - breaking_changes: empty array if none; otherwise list each breaking change
        - If the diff is ambiguous, note that in a body bullet — do not guess intent
        """;

    private const string PrSystemPrompt = """
        You are a pull request description author.
        Given a git diff, produce a structured PR description.

        Return a JSON object matching this schema exactly:
        {
          "title": "<short PR title, 72 chars or fewer>",
          "description": ["<bullet 1>", "<bullet 2>", "..."],
          "testing_notes": ["<how to test this change>", "..."]
        }

        CRITICAL RULES:
        - Return ONLY valid JSON. Nothing else.
        - Do NOT wrap the output in markdown code fences.
        - Your response must start with { and end with }.
        - description: 3–6 bullets covering what changed and why
        - testing_notes: 2–4 bullets on how a reviewer can verify the change
        """;

    // ── Shared infrastructure ─────────────────────────────────────────────────

    private async Task<string> CallAndAccumulateAsync(
        List<ChatMessage> messages,
        CancellationToken ct)
    {
        var options = new ChatOptions
        {
            Temperature     = 0f,   // deterministic — same diff → same message
            MaxOutputTokens = 512
        };

        var accumulated = new StringBuilder();
        Console.Error.Write("Generating");
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

    private static T ParseResult<T>(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("Model returned empty output.");

        var cleaned = OutputCleaner.Clean(raw);

        try
        {
            return JsonSerializer.Deserialize<T>(cleaned, JsonOptions)
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

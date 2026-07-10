using Microsoft.Extensions.AI;
using System.Text;

namespace DevToolkit;

/// <summary>
/// Generates XML doc comments and README sections for C# code.
/// Returns plain text — no JSON needed for documentation workflows.
/// </summary>
public sealed class DocGenerationService
{
    private readonly IChatClient _client;

    public DocGenerationService(IChatClient client) => _client = client;

    /// <summary>
    /// Generates <c>///</c> XML doc comments for the provided method or class.
    /// Output is ready to paste directly above the signature.
    /// </summary>
    public async Task<string> GenerateXmlDocAsync(string methodCode, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(methodCode);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, XmlDocSystemPrompt),
            new(ChatRole.User, $"Generate XML doc comments for:\n\n{methodCode}")
        };

        return await CallAndAccumulateAsync(messages, ct, "Documenting");
    }

    /// <summary>
    /// Generates a "Getting Started" README section in Markdown for the provided class.
    /// </summary>
    public async Task<string> GenerateReadmeSectionAsync(string classCode, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(classCode);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, ReadmeSystemPrompt),
            new(ChatRole.User, $"Generate a Getting Started README section for:\n\n{classCode}")
        };

        return await CallAndAccumulateAsync(messages, ct, "Writing README");
    }

    // ── Prompts ───────────────────────────────────────────────────────────────

    private const string XmlDocSystemPrompt = """
        You are a C# XML documentation author. Given a method or class, generate complete
        XML doc comments.

        Include:
        - <summary> — what the member does (not how it does it)
        - <param name="..."> — for every parameter, including nullable ones
        - <returns> — what is returned (omit for void methods)
        - <exception cref="..."> — for each exception the code can throw, including from dependencies
        - <remarks> — only when there is genuinely useful usage information

        Rules:
        - Output ONLY the XML doc comments, ready to paste above the method signature.
        - Do not repeat the method signature or body.
        - Do not include prose outside the XML tags.
        - Use /// prefix on every single line.
        - Keep <summary> to 1–2 sentences. What, not how.
        - The why — the design decision — is for the human to write.
        """;

    private const string ReadmeSystemPrompt = """
        You are a technical writer producing README sections for a .NET library or application.
        Given a C# class file, write a concise "Getting Started" README section in Markdown.

        Include:
        - One-paragraph description of what the class does
        - A minimal code example showing the most common use case
        - A bullet list of constructor parameters or required configuration

        Rules:
        - Output ONLY the Markdown section.
        - Use ## Getting Started as the heading.
        - Keep it concise — a developer should understand the usage in under 60 seconds.
        - Do not include installation instructions or package references.
        """;

    // ── Shared infrastructure ─────────────────────────────────────────────────

    private async Task<string> CallAndAccumulateAsync(
        List<ChatMessage> messages,
        CancellationToken ct,
        string progressLabel)
    {
        // Slight temperature for documentation — determinism produces "Returns the value."
        // A small value gives more natural prose without losing accuracy.
        var options = new ChatOptions
        {
            Temperature     = 0.1f,
            MaxOutputTokens = 1024
        };

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
}

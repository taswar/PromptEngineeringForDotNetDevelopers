using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevToolkit;

// ── Output records ─────────────────────────────────────────────────────────────

/// <summary>A single generated test method — name, intent, and compilable C# body.</summary>
public record TestMethod(
    [property: JsonPropertyName("name")]        string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("code")]        string Code
);

/// <summary>The full generated test suite — class name and an array of test methods.</summary>
public record TestSuiteResult(
    [property: JsonPropertyName("class_name")] string ClassName,
    [property: JsonPropertyName("tests")]      TestMethod[] Tests
);

// ── Service ────────────────────────────────────────────────────────────────────

/// <summary>
/// Generates an xUnit test suite for the provided C# method or class.
/// Returns structured output so the caller can inspect each test before writing files.
/// </summary>
public sealed class TestGenerationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IChatClient _client;
    private readonly DevToolkitOptions _options;

    public TestGenerationService(IChatClient client, DevToolkitOptions? options = null)
    {
        _client = client;
        _options = options ?? DevToolkitOptions.Default;
    }

    /// <summary>
    /// Generates a test suite for <paramref name="methodCode"/>.
    /// </summary>
    /// <param name="methodCode">The C# method or class to generate tests for.</param>
    /// <param name="existingTestStyle">
    /// Optional: an existing test file whose style the generated tests should match.
    /// This is the few-shot approach — one real test is worth three style constraints.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<TestSuiteResult> GenerateTestsAsync(
        string methodCode,
        string? existingTestStyle = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(methodCode);

        var systemPrompt = BuildSystemPrompt(_options);
        var userMessage = string.IsNullOrWhiteSpace(existingTestStyle)
            ? $"Generate tests for this C# code:\n\n{methodCode}"
            : $"Generate tests for this C# code, matching the style of the existing tests below.\n\nCode to test:\n{methodCode}\n\nExisting test style:\n{existingTestStyle}";

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userMessage)
        };

        // Slight temperature variation helps produce a broader set of test cases.
        // Full determinism (0f) often produces only the obvious happy path.
        var options = new ChatOptions
        {
            Temperature     = 0.2f,
            MaxOutputTokens = 2048
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

        return ParseTestSuite(accumulated.ToString());
    }

    // ── Prompt construction ───────────────────────────────────────────────────

    private static string BuildSystemPrompt(DevToolkitOptions opts) => $$"""
        You are an expert C# test engineer using {{opts.TestFramework}}.

        Generate a test suite for the provided C# method or class. Include:
        - Happy path test
        - Null or empty input tests (where applicable)
        - Boundary and edge case tests
        - Exception path tests (where the code can throw)
        - Parameterised [Theory] tests where the same logic applies to multiple inputs

        Return a JSON object matching this schema exactly:
        {
          "class_name": "<TestClassName>",
          "tests": [
            {
              "name": "<MethodName_Scenario_ExpectedBehaviour>",
              "description": "<what this test verifies>",
              "code": "<complete [Fact] or [Theory] method body as a C# string>"
            }
          ]
        }

        CRITICAL RULES:
        - Return ONLY valid JSON. Nothing else.
        - Do NOT wrap the output in markdown code fences.
        - Your response must start with { and end with }.
        - Test names must follow: MethodName_Scenario_ExpectedBehaviour
        - Each test must be independent — no shared mutable state between tests
        - The code field contains the complete C# test method including attributes
        - Generate at least 3 tests, no more than 8
        """;

    // ── Parsing ───────────────────────────────────────────────────────────────

    private static TestSuiteResult ParseTestSuite(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("Model returned empty output.");

        var cleaned = OutputCleaner.Clean(raw);

        try
        {
            return JsonSerializer.Deserialize<TestSuiteResult>(cleaned, JsonOptions)
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

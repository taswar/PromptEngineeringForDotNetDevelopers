// TechniqueBenchmark — Chapter 5, Prompt Engineering for C# Developers
//
// Sends a deliberately flawed C# method to the model using four techniques:
//   1. Zero-shot    — task only
//   2. Few-shot     — task + one worked example for format guidance
//   3. Chain-of-thought — task + "think hard" reasoning instruction
//   4. Rubric-based — explicit binary criteria, score-first (uses PromptBuilder)
//
// Then optionally runs a self-consistency check: 5 CoT runs at T=0.7, majority-votes severity.
//
// Default: LM Studio on port 1234 with microsoft/phi-4-mini-instruct
// For Azure AI Foundry: uncomment the Azure block below and set user secrets.

using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using System.ClientModel;
using TechniqueBenchmark;

// ─── Configuration ────────────────────────────────────────────────────────────
var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

// ─── Client Setup ─────────────────────────────────────────────────────────────

// Option A: LM Studio (local, free, no key required)
// Requires: LM Studio running on port 1234 with a model loaded.
// Download from https://lmstudio.ai — load microsoft/phi-4-mini-instruct or similar.
IChatClient client = new OpenAIClient(
        new ApiKeyCredential("lm-studio"),
        new OpenAIClientOptions { Endpoint = new Uri("http://localhost:1234/v1") })
    .GetChatClient("microsoft/phi-4-mini-instruct")
    .AsIChatClient();


// ── Option B: OpenAI API ──────────────────────────────────────────────────────
// dotnet user-secrets set "OPENAI_API_KEY" "sk-..."
// IChatClient client = new OpenAIClient(
//         new ApiKeyCredential(config["OPENAI_API_KEY"]
//             ?? throw new InvalidOperationException("OPENAI_API_KEY not set")))
//     .GetChatClient("gpt-4o-mini")
//     .AsIChatClient();

// Option C: Azure AI Foundry (uncomment to use, comment out Option A above)
// Set secrets first:
//   dotnet user-secrets set "AzureAI:Endpoint" "https://YOUR-ENDPOINT.openai.azure.com"
//   dotnet user-secrets set "AzureAI:Key"      "your-key-here"
//
// IChatClient client = new AzureOpenAIClient(
//         new Uri(config["AzureAI:Endpoint"]!),
//         new ApiKeyCredential(config["AzureAI:Key"]!))
//     .GetChatClient("gpt-4o-mini")
//     .AsIChatClient();

// ─── Settings ─────────────────────────────────────────────────────────────────

// Set to true to run the 5× self-consistency check at the end.
// It makes 5 additional API calls at temperature 0.7 and adds ~30 seconds.
const bool RunSelfConsistency = true;

var options = new ChatOptions { Temperature = 0f, MaxOutputTokens = 1024 };

// ─── The Method Under Review ──────────────────────────────────────────────────
// Two known issues:
//   1. Null reference: user, user.Profile, FirstName, LastName — all unguarded
//   2. Performance: character-by-character string += in a loop (O(n) allocations)
const string MethodUnderReview = """
    public string FormatUserDisplayName(User user)
    {
        var name = user.Profile.FirstName + " " + user.Profile.LastName;
        var result = "";
        for (int i = 0; i < name.Length; i++)
        {
            result += name[i].ToString().ToUpper();
            if (i == 0) result = result.ToUpper();
        }
        return result.Trim();
    }
    """;

// ─── Benchmark ────────────────────────────────────────────────────────────────

PrintHeader("TechniqueBenchmark — Same task, four techniques");
Console.WriteLine("Method under review:");
PrintCode(MethodUnderReview);
Console.WriteLine();
Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine("Known issues:");
Console.WriteLine("  1. Null-reference: user, user.Profile, FirstName, LastName — all unguarded");
Console.WriteLine("  2. Performance:    character-by-character string += loop (O(n) allocations)");
Console.WriteLine();
Console.WriteLine("Watch whether each technique catches both issues — and how it frames them.");
Console.ResetColor();
Console.WriteLine();

// ── Technique 1: Zero-Shot ────────────────────────────────────────────────────
PrintTechniqueHeader(1, "ZERO-SHOT", "Task only. No examples, no reasoning instructions.");

var zeroShotPrompt = BuildZeroShot(MethodUnderReview);
var zeroShot = await client.GetResponseAsync(zeroShotPrompt, options, CancellationToken.None);
Console.WriteLine(zeroShot.Text);
Console.WriteLine();

// ── Technique 2: Few-Shot ─────────────────────────────────────────────────────
PrintTechniqueHeader(2, "FEW-SHOT", "Task + one worked example to constrain output format.");

var fewShotPrompt = BuildFewShot(MethodUnderReview);
var fewShot = await client.GetResponseAsync(fewShotPrompt, options, CancellationToken.None);
Console.WriteLine(fewShot.Text);
Console.WriteLine();

// ── Technique 3: Chain-of-Thought ─────────────────────────────────────────────
PrintTechniqueHeader(3, "CHAIN-OF-THOUGHT", "Task + \"think hard\" reasoning instruction.");

var cotPrompt = BuildChainOfThought(MethodUnderReview);
var cot = await client.GetResponseAsync(cotPrompt, options, CancellationToken.None);
Console.WriteLine(cot.Text);
Console.WriteLine();

// ── Technique 4: Rubric-Based (uses PromptBuilder) ────────────────────────────
PrintTechniqueHeader(4, "RUBRIC-BASED", "Explicit binary criteria — forces objectivity, mitigates sycophancy.");

var rubricCriteria = new[]
{
    "All parameters are validated before use (null checks on user and user.Profile)",
    "No null-dereference risks exist for any property access on user or its sub-objects",
    "No performance issues exist (e.g., string concatenation in loops, O(n²) operations)",
    "The method handles edge cases (null or empty FirstName / LastName)",
    "The method name accurately and completely describes its behavior"
};

// PromptBuilder carried forward from Chapter 4
var rubricPrompt = new PromptBuilder()
    .WithRole("You are a senior C# developer performing a structured code review.")
    .WithTask($"""
        Evaluate the following method against each criterion below.
        Answer YES or NO for each criterion, then provide a one-sentence explanation.
        Do NOT give an overall qualitative judgment before scoring each criterion.
        After scoring all criteria, state: Total YES: X/{rubricCriteria.Length}

        Criteria:
        {string.Join("\n", rubricCriteria.Select((c, i) => $"{i + 1}. {c}"))}
        """)
    .WithContext($"```csharp\n{MethodUnderReview}\n```")
    .Build();

var rubric = await client.GetResponseAsync(rubricPrompt, options, CancellationToken.None);
Console.WriteLine(rubric.Text);
Console.WriteLine();

// ── Optional: Self-Consistency ────────────────────────────────────────────────
PrintTechniqueHeader(5, "SELF-CONSISTENCY (optional)", "5× CoT at T=0.7 — majority-votes severity rating.");

if (!RunSelfConsistency)
{
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("Skipped. Set RunSelfConsistency = true at the top of Program.cs to run.");
    Console.WriteLine("(Makes 5 additional API calls at temperature 0.7. Takes ~30 seconds.)");
    Console.ResetColor();
    Console.WriteLine();
}
#pragma warning disable CS0162 // Unreachable code — intentional; RunSelfConsistency is a reader toggle
else
{
    var consistencyOptions = new ChatOptions { Temperature = 0.7f, MaxOutputTokens = 512 };
    var severityVotes = new List<string>();

    Console.WriteLine("Running 5 times...");
    Console.WriteLine();

    for (int i = 0; i < 5; i++)
    {
        Console.Write($"  Run {i + 1}/5... ");
        var r = await client.GetResponseAsync(cotPrompt, consistencyOptions, CancellationToken.None);
        var severity = ExtractSeverity(r.Text);
        severityVotes.Add(severity);
        Console.WriteLine($"Severity = {severity}");
    }

    var majority = severityVotes
        .GroupBy(v => v)
        .OrderByDescending(g => g.Count())
        .First();

    Console.WriteLine();
    Console.ForegroundColor = majority.Count() >= 4 ? ConsoleColor.Green : ConsoleColor.Yellow;
    Console.WriteLine($"Majority verdict: {majority.Key} ({majority.Count()}/5 votes)");
    Console.ResetColor();

    if (majority.Count() < 4)
        Console.WriteLine("Note: inconsistent severity ratings — consider tightening the prompt criteria.");

    Console.WriteLine();
}
#pragma warning restore CS0162

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("═══ Benchmark complete. ═══");
Console.ResetColor();

// ─── Prompt Builders ──────────────────────────────────────────────────────────

static string BuildZeroShot(string method) => $"""
    You are a senior C# developer performing a code review.
    Review the following method and identify any bugs, null-reference risks, and performance issues.

    ```csharp
    {method}
    ```
    """;

static string BuildFewShot(string method) => $$"""
    You are a senior C# developer performing a code review.
    Review the following method. Identify bugs, null-reference risks, and performance issues.
    Format your response exactly as shown in the example below.

    EXAMPLE:
    INPUT:
    ```csharp
    public string Concat(List<string> items)
    {
        string result = "";
        foreach (var item in items)
            result += item;
        return result;
    }
    ```

    OUTPUT:
    Bug: None
    Null Risk: items is not null-checked before iteration. Will throw NullReferenceException.
    Performance: String concatenation in a foreach loop causes O(n) allocations. Use string.Join() or StringBuilder.
    Severity: Medium

    ---

    INPUT:
    ```csharp
    {{method}}
    ```

    OUTPUT:
    """;

static string BuildChainOfThought(string method) => $"""
    You are a senior C# developer performing a code review.

    Think hard before answering. Work through the method carefully:
    1. Examine every parameter — is it validated before use?
    2. Trace every property access and dereference — could any object be null at that point?
    3. Identify any performance concerns (allocations, algorithmic complexity, LINQ on hot paths).
    4. State your findings in full, then give an overall severity: High / Medium / Low.

    ```csharp
    {method}
    ```
    """;

// ─── Helpers ──────────────────────────────────────────────────────────────────

static string ExtractSeverity(string response)
{
    foreach (var line in response.Split('\n'))
    {
        var trimmed = line.Trim();
        if (trimmed.StartsWith("Severity", StringComparison.OrdinalIgnoreCase)
            && trimmed.Contains(':'))
        {
            var value = trimmed.Split(':', 2)[1].Trim().ToUpperInvariant();
            if (value.Contains("HIGH"))   return "High";
            if (value.Contains("MEDIUM")) return "Medium";
            if (value.Contains("LOW"))    return "Low";
        }
    }
    return "Unknown";
}

static void PrintHeader(string title)
{
    var width = Math.Max(title.Length + 6, 64);
    var line = new string('═', width);
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine(line);
    Console.WriteLine($"  {title}");
    Console.WriteLine(line);
    Console.ResetColor();
    Console.WriteLine();
}

static void PrintTechniqueHeader(int number, string name, string description)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"┌── [{number}] {name} ──");
    Console.ResetColor();
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"│   {description}");
    Console.WriteLine("└" + new string('─', 58));
    Console.ResetColor();
    Console.WriteLine();
}

static void PrintCode(string code)
{
    Console.ForegroundColor = ConsoleColor.DarkCyan;
    foreach (var line in code.Split('\n'))
        Console.WriteLine($"  {line}");
    Console.ResetColor();
}

#nullable enable

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using System.ClientModel;
using PromptBuilderDemo;
// using Azure.AI.OpenAI;   // ← uncomment for Option C (Azure AI Foundry)

// ── Configuration ─────────────────────────────────────────────────────────────
// Secrets are stored via `dotnet user-secrets set` — see README.md for setup.
var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

// ── Provider selection ────────────────────────────────────────────────────────
// Uncomment exactly ONE block below.

// ── Option A: LM Studio (local, free — recommended for getting started) ──────
// Default port is 1234. If you have a port conflict, LM Studio lets you change
// it in Settings → Local Server. Get the exact model id from:
//   GET http://localhost:1234/v1/models
IChatClient client = new OpenAIClient(
        new ApiKeyCredential("lm-studio"),
        new OpenAIClientOptions { Endpoint = new Uri("http://localhost:1234/v1") })
    .GetChatClient("microsoft/phi-4-mini-reasoning")
    .AsIChatClient();

// ── Option B: OpenAI API ──────────────────────────────────────────────────────
// dotnet user-secrets set "OPENAI_API_KEY" "sk-..."
// IChatClient client = new OpenAIClient(
//         new ApiKeyCredential(config["OPENAI_API_KEY"]
//             ?? throw new InvalidOperationException("OPENAI_API_KEY not set")))
//     .GetChatClient("gpt-4o-mini")
//     .AsIChatClient();

// ── Option C: Azure AI Foundry ────────────────────────────────────────────────
// dotnet user-secrets set "AZURE_OPENAI_ENDPOINT" "https://your-resource.openai.azure.com/"
// dotnet user-secrets set "AZURE_OPENAI_KEY" "your-key"
// IChatClient client = new AzureOpenAIClient(
//         new Uri(config["AZURE_OPENAI_ENDPOINT"]
//             ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT not set")),
//         new ApiKeyCredential(config["AZURE_OPENAI_KEY"]
//             ?? throw new InvalidOperationException("AZURE_OPENAI_KEY not set")))
//     .GetChatClient("gpt-4o-mini")
//     .AsIChatClient();

// ── Build the review prompt using PromptBuilder ───────────────────────────────
var reviewPrompt = new PromptBuilder()
    .WithRole("""
        You are a rigorous C# code reviewer for a .NET 10 codebase.
        Be direct — no false positives, no false negatives.
        """)
    .WithContext("""
        The codebase uses Minimal APIs, nullable reference types, and C# 13 features.
        """)
    .WithTask("""
        Review the following C# method. Identify correctness issues,
        null safety problems, and performance concerns.
        """)
    .WithConstraints("""
        Return a numbered list of issues. Maximum 5.
        Each issue: one sentence describing the problem,
        one sentence suggesting a fix.
        """)
    .WithConstraints("If there are no issues, respond with exactly: 'No issues found.'")
    .Build();

// ── The code under review ─────────────────────────────────────────────────────
// Swap this out to test different methods — see the README for more examples.
var codeToReview = """
    public string GetUserFullName(User? user) 
    {
        return user.FirstName + " " + user.LastName;
    }
    """;

// ── Send to the model ─────────────────────────────────────────────────────────
Console.WriteLine("Reviewing code...");
Console.WriteLine(new string('-', 60));
Console.WriteLine(codeToReview.Trim());
Console.WriteLine(new string('-', 60));

var messages = new List<ChatMessage>
{
    new(ChatRole.System, reviewPrompt),
    new(ChatRole.User, $"```csharp\n{codeToReview}\n```")
};

// Temperature = 0 for deterministic structured output.
// Change to 0.7f and run 3 times to observe variance.
var response = await client.GetResponseAsync(
    messages,
    new ChatOptions { Temperature = 0f },
    CancellationToken.None);

Console.WriteLine();
Console.WriteLine("Review:");
Console.WriteLine(response.Text);

// ── Minimal stub so the file compiles without a User class definition ─────────
// In a real codebase this would be an actual entity.
public class User
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

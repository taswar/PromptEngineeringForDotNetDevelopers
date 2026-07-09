// CS1529 trap: using directives for Azure.AI.OpenAI must appear at the file top,
// before any executable statements. Moving them below the var declarations
// causes a compiler error. Keep them here.
using Azure.AI.OpenAI;
using DocumentSummary;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using System.ClientModel;

// ── Configuration ─────────────────────────────────────────────────────────────
// User secrets are the right approach for dev — no keys in source control.
// The same keys are read from environment variables in CI/production.
var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

// ── Backend selection ─────────────────────────────────────────────────────────
// One factory, one switch. Change the backend here to swap providers.
// pick one Backend value from the enum below. The default is LM Studio on localhost.
// For Azure OpenAI, set user secrets for the endpoint, key, and deployment name.
// For OpenAI, set user secrets for the API key and model name.
IChatClient chatClient = CreateChatClient(Backend.LmStudio, config);

// ── Service ───────────────────────────────────────────────────────────────────
var service = new DocumentSummaryService(chatClient);

// Sample document — a realistic .NET release note excerpt.
const string SampleDocument = """
    ## Microsoft .NET 10 Preview 4 — Release Notes

    .NET 10 Preview 4 ships significant improvements across the runtime, libraries, and SDK.

    The JIT compiler now performs aggressive loop unrolling and improved bounds-check elimination,
    yielding 8–12% throughput improvements on compute-intensive workloads in internal benchmarks.

    System.Text.Json adds native support for read-only spans in deserialization hot paths,
    reducing allocations by up to 30% in high-throughput scenarios.

    The SDK tooling introduces a new `dotnet publish --mode aot` shorthand for Native AOT profiles,
    replacing the previous multi-flag incantation. Build times for AOT-compiled console apps
    dropped by roughly 20% due to improved incremental compilation support.

    ASP.NET Core Minimal APIs gain first-class OpenAPI 3.1 schema generation with support for
    discriminated union types, polymorphic serialization, and nullable reference type annotations.

    Breaking changes: The obsoleted System.Runtime.Remoting namespace is now removed entirely.
    Three deprecated overloads in System.Collections.Generic.Dictionary have been removed.
    Developers targeting net10.0 should review the breaking change documentation before upgrading.
    """;

// ── Run ───────────────────────────────────────────────────────────────────────
Console.WriteLine("=== DocumentSummaryService Demo ===");
Console.WriteLine();

try
{
    var summary = await service.SummarizeAsync(SampleDocument);

    Console.WriteLine($"Title:      {summary.Title}");
    Console.WriteLine($"Sentiment:  {summary.Sentiment}");
    Console.WriteLine($"Read time:  ~{summary.EstimatedReadMinutes} min");
    Console.WriteLine();
    Console.WriteLine("Key points:");
    foreach (var point in summary.KeyPoints)
        Console.WriteLine($"  • {point}");
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine($"Summary failed after retries: {ex.Message}");
    return 1;
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine($"Invalid input: {ex.Message}");
    return 1;
}

return 0;

// ── Client factory ────────────────────────────────────────────────────────────

/// <summary>
/// Single factory for every supported <see cref="IChatClient"/> backend.
/// Add a new provider by adding a <see cref="Backend"/> value and a switch arm.
/// </summary>
static IChatClient CreateChatClient(Backend backend, IConfiguration config) => backend switch
{
    // LM Studio on localhost:1234 with the OpenAI-compatible endpoint.
    // Swap the model string to match whatever you have loaded in LM Studio.
    Backend.LmStudio =>
        new OpenAIClient(
            new ApiKeyCredential("lm-studio"),
            new OpenAIClientOptions { Endpoint = new Uri("http://localhost:1234/v1") })
        .GetChatClient("microsoft/phi-4-mini-instruct")
        .AsIChatClient(),

    // Azure OpenAI deployment. Set user secrets before using:
    //   dotnet user-secrets set "Azure:Endpoint"   "https://your-resource.openai.azure.com/"
    //   dotnet user-secrets set "Azure:Key"        "your-api-key"
    //   dotnet user-secrets set "Azure:Deployment" "gpt-4o-mini"
    Backend.Azure =>
        new AzureOpenAIClient(
            new Uri(config["Azure:Endpoint"]
                    ?? throw new InvalidOperationException("Azure:Endpoint not configured.")),
            new ApiKeyCredential(config["Azure:Key"]
                    ?? throw new InvalidOperationException("Azure:Key not configured.")))
        .GetChatClient(config["Azure:Deployment"] ?? "gpt-4o-mini")
        .AsIChatClient(),

    // OpenAI (api.openai.com) using an API key. Set user secrets before using:
    //   dotnet user-secrets set "OpenAI:Key"   "sk-..."
    //   dotnet user-secrets set "OpenAI:Model" "gpt-4o-mini"
    // Uncomment the Backend.OpenAI enum value above to enable this branch.
    Backend.OpenAI =>
        new OpenAIClient(
            new ApiKeyCredential(config["OpenAI:Key"]
                    ?? throw new InvalidOperationException("OpenAI:Key not configured.")))
        .GetChatClient(config["OpenAI:Model"] ?? "gpt-4o-mini")
        .AsIChatClient(),

    _ => throw new ArgumentOutOfRangeException(nameof(backend), backend, "Unsupported backend."),
};

/// <summary>Supported chat backends.</summary>
enum Backend
{
    LmStudio,
    Azure,
    OpenAI
}

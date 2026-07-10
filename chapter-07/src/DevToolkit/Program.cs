// CS1529 trap: Azure.AI.OpenAI using must appear at the file top,
// before any executable statements. Moving it below var declarations
// causes a compiler error. Keep all using directives here.
using Azure.AI.OpenAI;
using DevToolkit;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using System.ClientModel;
using System.Diagnostics;

// ── Configuration ─────────────────────────────────────────────────────────────
var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

// ── Backend selection ─────────────────────────────────────────────────────────
// Change Backend.LmStudio to Backend.Azure or Backend.OpenAI to switch providers.
// All four workflows share this single client.
IChatClient chatClient = CreateChatClient(Backend.LmStudio, config);

// ── Options ───────────────────────────────────────────────────────────────────
// Customise for your codebase — or use Default for any C# project.
var opts = new DevToolkitOptions
{
    CodebaseContext = "C# codebase targeting .NET 10, nullable reference types enabled",
    TestFramework   = "xUnit with FluentAssertions",
    CommitFormat    = "conventional commits"
};

// ── Services — one IChatClient, four workflows ────────────────────────────────
var reviewService = new CodeReviewService(chatClient, opts);
var testService   = new TestGenerationService(chatClient, opts);
var commitService = new CommitMessageService(chatClient, opts);
var docService    = new DocGenerationService(chatClient);

// ── Menu loop ─────────────────────────────────────────────────────────────────
Console.WriteLine("=== Dev Toolkit ===");
Console.WriteLine("Powered by Microsoft.Extensions.AI — Chapter 7");
Console.WriteLine();

while (true)
{
    Console.WriteLine("Pick a workflow:");
    Console.WriteLine("  1. Code review");
    Console.WriteLine("  2. Unit test generation");
    Console.WriteLine("  3. Commit message");
    Console.WriteLine("  4. Documentation (XML docs)");
    Console.WriteLine("  Q. Quit");
    Console.Write("> ");

    var choice = Console.ReadLine()?.Trim().ToUpperInvariant();
    Console.WriteLine();

    switch (choice)
    {
        case "1": await RunCodeReviewAsync(reviewService);      break;
        case "2": await RunTestGenerationAsync(testService);    break;
        case "3": await RunCommitMessageAsync(commitService);   break;
        case "4": await RunDocGenerationAsync(docService);      break;
        case "Q":
        case "QUIT":
            Console.WriteLine("Done. 👋");
            return 0;
        default:
            Console.WriteLine("Unknown option. Try 1, 2, 3, 4, or Q.");
            break;
    }

    Console.WriteLine();
}

// ── Workflow handlers ─────────────────────────────────────────────────────────

static async Task RunCodeReviewAsync(CodeReviewService service)
{
    var code = ReadMultilineInput("Paste the C# code to review:");
    if (string.IsNullOrWhiteSpace(code)) { Console.WriteLine("No input. Skipping."); return; }

    Console.WriteLine();
    try
    {
        var result = await service.ReviewAsync(code);
        Console.WriteLine();
        Console.WriteLine($"Found {result.Findings.Length} finding(s):");
        Console.WriteLine();
        foreach (var f in result.Findings)
        {
            var icon = f.Severity.ToLowerInvariant() switch
            {
                "critical" => "🔴",
                "warning"  => "🟡",
                _          => "🔵"
            };
            Console.WriteLine($"{icon} [{f.Severity.ToUpperInvariant()}] {f.Location}");
            Console.WriteLine($"   Issue: {f.Issue}");
            Console.WriteLine($"   Fix:   {f.Fix}");
            Console.WriteLine();
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Review failed: {ex.Message}");
    }
}

static async Task RunTestGenerationAsync(TestGenerationService service)
{
    var code = ReadMultilineInput("Paste the method or class to generate tests for:");
    if (string.IsNullOrWhiteSpace(code)) { Console.WriteLine("No input. Skipping."); return; }

    Console.WriteLine();
    try
    {
        var result = await service.GenerateTestsAsync(code);
        Console.WriteLine();
        Console.WriteLine($"Generated {result.Tests.Length} test(s) for {result.ClassName}:");
        Console.WriteLine();
        foreach (var test in result.Tests)
        {
            Console.WriteLine($"// {test.Description}");
            Console.WriteLine(test.Code);
            Console.WriteLine();
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Test generation failed: {ex.Message}");
    }
}

static async Task RunCommitMessageAsync(CommitMessageService service)
{
    Console.WriteLine("Options:");
    Console.WriteLine("  1. From git diff (paste diff)");
    Console.WriteLine("  2. From staged changes (runs git diff --staged)");
    Console.Write("> ");
    var sub = Console.ReadLine()?.Trim();
    Console.WriteLine();

    string diff;
    if (sub == "2")
    {
        diff = await GetStagedDiffAsync();
        if (string.IsNullOrWhiteSpace(diff))
        {
            Console.WriteLine("No staged changes found. Stage some files first with git add.");
            return;
        }
        Console.WriteLine($"Got diff ({diff.Length} chars from git diff --staged).");
    }
    else
    {
        diff = ReadMultilineInput("Paste the git diff or describe the change:");
        if (string.IsNullOrWhiteSpace(diff)) { Console.WriteLine("No input. Skipping."); return; }
    }

    Console.WriteLine();
    try
    {
        var result = await service.GenerateCommitMessageAsync(diff);
        Console.WriteLine();
        Console.WriteLine("Subject:");
        Console.WriteLine($"  {result.Subject}");
        Console.WriteLine();
        Console.WriteLine("Body:");
        foreach (var bullet in result.BodyBullets)
            Console.WriteLine($"  - {bullet}");

        if (result.BreakingChanges.Length > 0)
        {
            Console.WriteLine();
            Console.WriteLine("BREAKING CHANGES:");
            foreach (var bc in result.BreakingChanges)
                Console.WriteLine($"  BREAKING CHANGE: {bc}");
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Commit message generation failed: {ex.Message}");
    }
}

static async Task RunDocGenerationAsync(DocGenerationService service)
{
    Console.WriteLine("Options:");
    Console.WriteLine("  1. XML doc comments");
    Console.WriteLine("  2. README Getting Started section");
    Console.Write("> ");
    var sub = Console.ReadLine()?.Trim();
    Console.WriteLine();

    var code = ReadMultilineInput(
        sub == "2"
            ? "Paste the C# class to document:"
            : "Paste the C# method or class to generate XML docs for:");

    if (string.IsNullOrWhiteSpace(code)) { Console.WriteLine("No input. Skipping."); return; }

    Console.WriteLine();
    try
    {
        var result = sub == "2"
            ? await service.GenerateReadmeSectionAsync(code)
            : await service.GenerateXmlDocAsync(code);

        Console.WriteLine();
        Console.WriteLine("Generated documentation:");
        Console.WriteLine();
        Console.WriteLine(result);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Documentation generation failed: {ex.Message}");
    }
}

// ── Utilities ─────────────────────────────────────────────────────────────────

static string ReadMultilineInput(string prompt)
{
    Console.WriteLine(prompt);
    Console.WriteLine("(Paste your content. Enter a line containing only \"---\" to finish.)");
    var lines = new List<string>();
    while (true)
    {
        var line = Console.ReadLine();
        if (line is null || line.Trim() == "---")
            break;
        lines.Add(line);
    }
    return string.Join(Environment.NewLine, lines);
}

/// <summary>Runs git diff --staged and returns the output as a string.</summary>
static async Task<string> GetStagedDiffAsync()
{
    var psi = new ProcessStartInfo("git", "diff --staged")
    {
        RedirectStandardOutput = true,
        RedirectStandardError  = true,   // capture error output so git failures are visible
        UseShellExecute        = false,
        WorkingDirectory       = Environment.CurrentDirectory
    };

    using var process = Process.Start(psi)
        ?? throw new InvalidOperationException("Could not start git process.");

    var diff   = await process.StandardOutput.ReadToEndAsync();
    var errors = await process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();

    if (process.ExitCode != 0)
        throw new InvalidOperationException(
            $"git diff --staged failed (exit {process.ExitCode}): {errors}");

    return diff;
}

// ── Client factory ────────────────────────────────────────────────────────────

/// <summary>
/// Single factory for every supported <see cref="IChatClient"/> backend.
/// Change the <see cref="Backend"/> value above to switch providers.
/// </summary>
static IChatClient CreateChatClient(Backend backend, IConfiguration config) => backend switch
{
    // LM Studio on localhost:1234.
    // Change the model string to match whatever you have loaded in LM Studio.
    Backend.LmStudio =>
        new OpenAIClient(
            new ApiKeyCredential("lm-studio"),
            new OpenAIClientOptions { Endpoint = new Uri("http://localhost:1234/v1") })
        .GetChatClient("microsoft/phi-4-mini-instruct")
        .AsIChatClient(),

    // Azure AI Foundry / Azure OpenAI. Set user secrets before using:
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

    // OpenAI (api.openai.com). Set user secrets before using:
    //   dotnet user-secrets set "OpenAI:Key"   "sk-..."
    //   dotnet user-secrets set "OpenAI:Model" "gpt-4o-mini"
    Backend.OpenAI =>
        new OpenAIClient(
            new ApiKeyCredential(config["OpenAI:Key"]
                    ?? throw new InvalidOperationException("OpenAI:Key not configured.")))
        .GetChatClient(config["OpenAI:Model"] ?? "gpt-4o-mini")
        .AsIChatClient(),

    _ => throw new ArgumentOutOfRangeException(nameof(backend), backend, "Unsupported backend."),
};

/// <summary>Supported chat backends.</summary>
enum Backend { LmStudio, Azure, OpenAI }

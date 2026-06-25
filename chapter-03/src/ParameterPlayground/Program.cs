#nullable enable
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Azure.AI.OpenAI;
using OpenAI;
using System.ClientModel;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

// ─────────────────────────────────────────────────────────────────
// OPTION A: LM Studio (local, free — active by default)
//
// 1. Open LM Studio and load a model in the Local Server panel
// 2. Start the server — it listens on port 1234 by default
// 3. Get the exact model ID from the server panel (or GET /v1/models)
//    and update the string below to match
//
// The ApiKeyCredential value doesn't matter — LM Studio ignores it.
// ─────────────────────────────────────────────────────────────────
// IChatClient client = new OpenAIClient(
//         new ApiKeyCredential("lm-studio"),
//         new OpenAIClientOptions { Endpoint = new Uri("http://localhost:1234/v1") })
//     .GetChatClient("microsoft/phi-4-mini-instruct")
//     .AsIChatClient();

// ─────────────────────────────────────────────────────────────────
// OPTION B: OpenAI API (cloud — costs money per call)
//
// dotnet user-secrets set "OPENAI_API_KEY" "sk-..."
// ─────────────────────────────────────────────────────────────────
// IChatClient client = new OpenAIClient(
//         new ApiKeyCredential(
//             config["OPENAI_API_KEY"]
//                 ?? throw new InvalidOperationException("Set OPENAI_API_KEY in user-secrets")))
//     .GetChatClient("gpt-4o-mini")
//     .AsIChatClient();

// ─────────────────────────────────────────────────────────────────
// OPTION C: Azure AI Foundry
//
// dotnet user-secrets set "AZURE_AI_ENDPOINT" "https://your-resource.services.ai.azure.com/models"
// dotnet user-secrets set "AZURE_AI_KEY" "your-key-here"
// ─────────────────────────────────────────────────────────────────
IChatClient client = new AzureOpenAIClient(
        new Uri(config["AZURE_AI_ENDPOINT"]
            ?? throw new InvalidOperationException(
                "AZURE_AI_ENDPOINT is not set. Run: dotnet user-secrets set \"AZURE_AI_ENDPOINT\" \"<your-endpoint>\"")),
        new ApiKeyCredential(
            config["AZURE_AI_KEY"]
            ?? throw new InvalidOperationException(
                "AZURE_AI_KEY is not set. Run: dotnet user-secrets set \"AZURE_AI_KEY\" \"<your-key>\"")))
    .GetChatClient("gpt-4o-mini")     // deployment name
    .AsIChatClient();

// ─────────────────────────────────────────────────────────────────
// The experiment
//
// The same prompt is sent at three temperature settings.
// Temperature 0  → near-deterministic: expect near-identical output on each run
// Temperature 0.5 → mild variation in phrasing between runs
// Temperature 1.0 → noticeable variation between runs
//
// Run the app multiple times to see T=0 stay stable and T=1.0 vary.
// ─────────────────────────────────────────────────────────────────
var prompt = "Describe what a C# delegate is in exactly one sentence.";
var temperatures = new[] { 0f, 0.5f, 1.0f };

Console.WriteLine("Parameter Playground — Temperature Experiment");
Console.WriteLine($"Prompt: \"{prompt}\"");
Console.WriteLine(new string('─', 60));
Console.WriteLine();

foreach (var temp in temperatures)
{
    var options = new ChatOptions
    {
        Temperature = temp,
        // MaxOutputTokens caps the response length. Keep this generous for
        // REASONING models (e.g. phi-4-mini-reasoning): they spend tokens on
        // hidden reasoning first, so a tiny cap (like 100) can get used up
        // before any visible answer is produced — leaving response.Text empty.
        MaxOutputTokens = 100
    };

    try
    {
        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, prompt)],
            options,
            CancellationToken.None);  // pass a real CancellationToken in ASP.NET/background services

        Console.WriteLine($"Temperature {temp:F1}:");

        // response.Text holds only the final answer (TextContent). Reasoning
        // models emit separate reasoning parts that aren't in .Text. If the
        // visible answer is empty, surface why instead of printing a blank line.
        if (string.IsNullOrWhiteSpace(response.Text))
        {
            var reasoning = string.Concat(response.Messages
                .SelectMany(m => m.Contents)
                .OfType<TextReasoningContent>()
                .Select(r => r.Text));

            Console.WriteLine(string.IsNullOrWhiteSpace(reasoning)
                ? $"[empty answer — finish reason: {response.FinishReason}]"
                : $"[no final answer; model stopped while reasoning — finish reason: {response.FinishReason}]");
        }
        else
        {
            Console.WriteLine(response.Text);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Temperature {temp:F1}: [Error — {ex.Message}]");
    }

    Console.WriteLine();
}

Console.WriteLine(new string('─', 60));
Console.WriteLine("Try running this app 3+ times and compare the T=1.0 outputs.");
Console.WriteLine("Then see the README for 'What to try next' experiments.");

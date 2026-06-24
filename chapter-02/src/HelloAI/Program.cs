// HelloAI — Chapter 2: Setting Up Your AI Development Environment
// ─────────────────────────────────────────────────────────────────────────
// One codebase, three AI backends. Uncomment the provider you want to use,
// leave the other two commented out, then run: dotnet run
//
// All three paths produce identical output — that's the point of IChatClient.
// ─────────────────────────────────────────────────────────────────────────

#nullable enable

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using System.ClientModel;

// ─────────────────────────────────────────────────────────────────
// Configuration — reads user-secrets in dev, env vars in CI/CD
//
// dotnet user-secrets stores keys in %APPDATA%\Microsoft\UserSecrets\
// — NOT in OS environment variables. IConfiguration bridges both.
// Same code works locally AND in Azure Functions / GitHub Actions.
// ─────────────────────────────────────────────────────────────────
var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()     // reads secrets.json set by dotnet user-secrets
    .AddEnvironmentVariables()     // env vars win over user-secrets (great for CI/CD)
    .Build();

// ─────────────────────────────────────────────────────────────────
// OPTION A: Local (Free) — LM Studio
// ─────────────────────────────────────────────────────────────────
// 1. Download LM Studio from https://lmstudio.ai
// 2. Download a model (Phi-4 Mini recommended for 4 GB VRAM;
//    Llama 3.1 8B or Mistral 7B for 8 GB+)
// 3. Go to Local Server, load the model, click Start Server
// 4. Run GET http://localhost:1234/v1/models to find the exact model id
//    and paste it into GetChatClient() below
//
// LM Studio exposes an OpenAI-compatible API at /v1 (NOT Ollama-compatible).
// Port is 5000. The "lm-studio" API key is ignored but must be non-empty.
// ─────────────────────────────────────────────────────────────────
// IChatClient client = new OpenAIClient(
//         new ApiKeyCredential("lm-studio"),                // value is ignored by LM Studio
//         new OpenAIClientOptions { Endpoint = new Uri("http://localhost:1234/v1") })
//     .GetChatClient("microsoft/phi-4-mini-reasoning")      // update to match GET /v1/models output
//     .AsIChatClient();

// ─────────────────────────────────────────────────────────────────
// OPTION B: OpenAI API
// ─────────────────────────────────────────────────────────────────
// Store your API key in user-secrets (never hardcode it):
//   dotnet user-secrets set "OPENAI_API_KEY" "sk-..."
//
// Note: user-secrets are read via IConfiguration (config["..."]), NOT
// via Environment.GetEnvironmentVariable() — they're in a JSON file, not
// OS env vars. Same config object also picks up real env vars for CI/CD.
//
// gpt-4o-mini is the practical choice — capable and cheap.
// ─────────────────────────────────────────────────────────────────
// IChatClient client = new OpenAIClient(
//         new ApiKeyCredential(
//             config["OPENAI_API_KEY"]
//                 ?? throw new InvalidOperationException(
//                     "OPENAI_API_KEY is not set. Run: dotnet user-secrets set \"OPENAI_API_KEY\" \"sk-...\"")))
//     .GetChatClient("gpt-4o-mini")
//     .AsIChatClient();

// ─────────────────────────────────────────────────────────────────
// OPTION C: Azure AI Foundry
// ─────────────────────────────────────────────────────────────────
// Store the *base* endpoint and key in user-secrets (no /chat/completions,
// no ?api-version) — the client appends the path and negotiates the version:
//   dotnet user-secrets set "AZURE_AI_ENDPOINT" "<your-endpoint>"
//   dotnet user-secrets set "AZURE_AI_KEY" "<your-key>"
//
// A cognitiveservices.azure.com resource is Azure OpenAI, so use
// AzureOpenAIClient. It targets a recent api-version, which o-series
// models like o4-mini require (>= 2024-12-01-preview). "o4-mini" below
// is the DEPLOYMENT name from your Foundry/Azure OpenAI deployment.
// ─────────────────────────────────────────────────────────────────
IChatClient client = new Azure.AI.OpenAI.AzureOpenAIClient(
        new Uri(config["AZURE_AI_ENDPOINT"]
            ?? throw new InvalidOperationException(
                "AZURE_AI_ENDPOINT is not set. Run: dotnet user-secrets set \"AZURE_AI_ENDPOINT\" \"<your-endpoint>\"" )),
        new ApiKeyCredential(
            config["AZURE_AI_KEY"]
            ?? throw new InvalidOperationException(
                "AZURE_AI_KEY is not set. Run: dotnet user-secrets set \"AZURE_AI_KEY\" \"<your-key>\"")))
    .GetChatClient("o4-mini")     // deployment name
    .AsIChatClient();

// ─────────────────────────────────────────────────────────────────
// The call — identical regardless of which provider you chose.
//
// GetResponseAsync is the MEAI 10+ name (was CompleteAsync in 9.x).
// ChatRole.User = your message.
// response.Text is the convenience shortcut on ChatResponse.
// ─────────────────────────────────────────────────────────────────
Console.WriteLine("Sending your first message to an LLM...\n");

var response = await client.GetResponseAsync(
    [new ChatMessage(ChatRole.User,
        "Explain what a delegate is in C# in two sentences, like I'm a junior developer.")]);

Console.WriteLine("Response:");
Console.WriteLine(response.Text);

Console.WriteLine("\n✅ If you see a response above, you're good to go. On to Chapter 3!");

// HelloAI — Chapter 1 Sneak Peek
// This is a preview of the code from Chapter 2.
// It won't run yet (you need to set up LM Studio first — that's Chapter 2's job).
// But it shows you what the code looks like. Spoiler: it's just C#.
//
// Three ways to call an LLM. Same code. Different providers.
// Pick one, comment out the others, or try all three.

using Microsoft.Extensions.AI;
using OpenAI;
using Azure.AI.OpenAI;
using System.ClientModel;

// ─────────────────────────────────────────────────────────────────
// OPTION A: Local (Free) — LM Studio
// Download LM Studio from https://lmstudio.ai
// Start the local server, download a model, and enable the server.
//
// LM Studio exposes an OpenAI-compatible API at /v1 (NOT Ollama-compatible),
// so we use the OpenAI client pointed at the local endpoint.
// The API key is ignored by LM Studio but the SDK requires a non-empty value.
//
// Tip: run `GET http://localhost:1234/v1/models` to see the exact model id
// to use below — it must match what LM Studio reports (e.g. the value of "id").
// ─────────────────────────────────────────────────────────────────
IChatClient client = new OpenAIClient(
        new ApiKeyCredential("lm-studio"),               // ignored by LM Studio
        new OpenAIClientOptions { Endpoint = new Uri("http://localhost:1234/v1") })
    .GetChatClient("microsoft/phi-4-mini-reasoning")     // must match LM Studio's model id
    .AsIChatClient();

// ─────────────────────────────────────────────────────────────────
// OPTION B: OpenAI API
// Add using OpenAI; and set OPENAI_API_KEY in your environment:
//   dotnet user-secrets set "OPENAI_API_KEY" "sk-..."
// ─────────────────────────────────────────────────────────────────
// var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
// IChatClient client = new OpenAI.OpenAIClient(openAiKey)
//     .AsChatClient(modelId: "gpt-4o-mini");

// ─────────────────────────────────────────────────────────────────
// OPTION C: Azure AI Foundry
// Add using Azure; using Azure.AI.Inference;
// Set AZURE_AI_ENDPOINT and AZURE_AI_KEY in your environment:
//   dotnet user-secrets set "AZURE_AI_ENDPOINT" "https://your-resource.inference.ai.azure.com"
//   dotnet user-secrets set "AZURE_AI_KEY" "your-key-here"
// ─────────────────────────────────────────────────────────────────
// IChatClient client = new Azure.AI.OpenAI.AzureOpenAIClient(
//         new Uri(config["AZURE_AI_ENDPOINT"]
//             ?? throw new InvalidOperationException(
//                 "AZURE_AI_ENDPOINT is not set. Run: dotnet user-secrets set \"AZURE_AI_ENDPOINT\" \"<your-endpoint>\"")),
//         new ApiKeyCredential(
//             config["AZURE_AI_KEY"]
//             ?? throw new InvalidOperationException(
//                 "AZURE_AI_KEY is not set. Run: dotnet user-secrets set \"AZURE_AI_KEY\" \"<your-key>\"")))
//     .GetChatClient("o4-mini")     // deployment name
//     .AsIChatClient();

// ─────────────────────────────────────────────────────────────────
// Your first LLM call — identical regardless of which provider you chose.
// Note: In MEAI 10+, the method is GetResponseAsync (was CompleteAsync in earlier versions).
// ChatRole.User = your message | ChatRole.System = instructions | ChatRole.Assistant = model reply
// ─────────────────────────────────────────────────────────────────
Console.WriteLine("Sending your first message to an LLM...\n");

var response = await client.GetResponseAsync(
    [new ChatMessage(ChatRole.User,
        "Explain what a delegate is in C# in two sentences, like I'm a junior developer.")]);

Console.WriteLine("Response:");
Console.WriteLine(response.Text);  // ChatResponse.Text is the convenience shortcut

Console.WriteLine("\n✅ If you see a response above, you're good to go. On to Chapter 2!");

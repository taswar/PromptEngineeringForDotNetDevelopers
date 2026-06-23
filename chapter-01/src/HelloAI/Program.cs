// HelloAI — Chapter 1 Sneak Peek
// This is a preview of the code from Chapter 2.
// It won't run yet (you need to set up LM Studio first — that's Chapter 2's job).
// But it shows you what the code looks like. Spoiler: it's just C#.
//
// Three ways to call an LLM. Same code. Different providers.
// Pick one, comment out the others, or try all three.

using Azure;
using Azure.AI.Inference;
using Microsoft.Extensions.AI;

// ─────────────────────────────────────────────────────────────────
// OPTION A: Local (Free) — LM Studio
// Download LM Studio from https://lmstudio.ai
// Start the local server, download a model (e.g. phi-4, llama3.2),
// and enable the server in the LM Studio UI.
//
// Note: LM Studio uses port 1234 by default (not 11434 — that's Ollama).
// We use the OllamaChatClient because LM Studio exposes an Ollama-compatible
// API endpoint. Same wire format, different port.
// ─────────────────────────────────────────────────────────────────
IChatClient client = new OllamaChatClient(
    new Uri("http://localhost:1234"),  // LM Studio default port
    modelId: "phi-4"                  // must match exactly what LM Studio shows
);

// ─────────────────────────────────────────────────────────────────
// OPTION B: OpenAI API
// Set OPENAI_API_KEY in your environment or user-secrets:
//   dotnet user-secrets set "OPENAI_API_KEY" "sk-..."
// ─────────────────────────────────────────────────────────────────
// var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
// IChatClient client = new OpenAI.OpenAIClient(openAiKey)
//     .AsChatClient(modelId: "gpt-4o-mini");

// ─────────────────────────────────────────────────────────────────
// OPTION C: Azure AI Foundry
// Set AZURE_AI_ENDPOINT and AZURE_AI_KEY in your environment:
//   dotnet user-secrets set "AZURE_AI_ENDPOINT" "https://your-resource.inference.ai.azure.com"
//   dotnet user-secrets set "AZURE_AI_KEY" "your-key-here"
// ─────────────────────────────────────────────────────────────────
// var endpoint = new Uri(Environment.GetEnvironmentVariable("AZURE_AI_ENDPOINT")!);
// var credential = new AzureKeyCredential(
//     Environment.GetEnvironmentVariable("AZURE_AI_KEY")!);
// IChatClient client = new ChatCompletionsClient(endpoint, credential)
//     .AsChatClient(modelId: "gpt-4o");

// ─────────────────────────────────────────────────────────────────
// Your first LLM call — identical regardless of which provider you chose
// ─────────────────────────────────────────────────────────────────
Console.WriteLine("Sending your first message to an LLM...\n");

var response = await client.CompleteAsync(
    "Explain what a delegate is in C# in two sentences, like I'm a junior developer.");

Console.WriteLine("Response:");
Console.WriteLine(response.Message.Text);

Console.WriteLine("\n✅ If you see a response above, you're good to go. On to Chapter 2!");

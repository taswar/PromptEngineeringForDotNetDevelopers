// HelloAI — Chapter 1 Sneak Peek
// This is a preview of the code from Chapter 2.
// It won't run yet (you need to set up LM Studio first — that's Chapter 2's job).
// But it shows you what the code looks like. Spoiler: it's just C#.
//
// Three ways to call an LLM. Same code. Different providers.
// Pick one, comment out the others, or try all three.

using Microsoft.Extensions.AI;

// ─────────────────────────────────────────────────────────────────
// OPTION A: Local (Free) — LM Studio
// Download LM Studio from https://lmstudio.ai
// Start the local server and download a model (e.g. phi-4, llama3.2)
// ─────────────────────────────────────────────────────────────────
IChatClient client = new OllamaChatClient(
    new Uri("http://localhost:11434"),
    modelId: "phi4"  // swap this for whatever model you downloaded
);

// ─────────────────────────────────────────────────────────────────
// OPTION B: OpenAI API
// Set OPENAI_API_KEY in your environment or user-secrets
// ─────────────────────────────────────────────────────────────────
// var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
// IChatClient client = new OpenAI.OpenAIClient(openAiKey)
//     .AsChatClient(modelId: "gpt-4o-mini");

// ─────────────────────────────────────────────────────────────────
// OPTION C: Azure AI Foundry
// Set AZURE_AI_ENDPOINT and AZURE_AI_KEY in your environment
// ─────────────────────────────────────────────────────────────────
// var endpoint = new Uri(Environment.GetEnvironmentVariable("AZURE_AI_ENDPOINT")!);
// var credential = new Azure.AzureKeyCredential(
//     Environment.GetEnvironmentVariable("AZURE_AI_KEY")!);
// IChatClient client = new Azure.AI.Inference.ChatCompletionsClient(endpoint, credential)
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

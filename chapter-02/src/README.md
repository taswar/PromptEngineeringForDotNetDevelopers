# Chapter 2 — HelloAI

The first runnable code in the book. Sends a message to an LLM and prints the response.
Works with LM Studio (local, free), OpenAI API, or Azure AI Foundry.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- One of the three provider options set up (see Chapter 2)

## Quick Start

```bash
cd HelloAI
dotnet restore
dotnet run
```

By default, Option A (LM Studio) is active. Make sure LM Studio is running with a model
loaded before you run.

## Switching Providers

Open `Program.cs`. Comment out the active `IChatClient client = ...` block and uncomment
the one for the provider you want. The `GetResponseAsync` call at the bottom does not change.

### Option A — LM Studio (default)

1. Download and install [LM Studio](https://lmstudio.ai)
2. Download a model (Phi-4 Mini for 4 GB VRAM, Llama 3.1 8B for 8 GB+)
3. Go to **Local Server**, load the model, click **Start Server**
4. Verify the model ID: `GET http://localhost:1234/v1/models`
5. Update `GetChatClient("...")` in `Program.cs` to match the reported id

No API key needed. Server runs on port **5000** at `/v1` (OpenAI-compatible).

### Option B — OpenAI API

```bash
dotnet user-secrets set "OPENAI_API_KEY" "sk-..."
```

Then uncomment the Option B block in `Program.cs`.

### Option C — Azure AI Foundry

```bash
dotnet user-secrets set "AZURE_AI_ENDPOINT" "https://your-resource.services.ai.azure.com/models"
dotnet user-secrets set "AZURE_AI_KEY" "your-key-here"
```

Get the endpoint URL from the **deployment page** in Azure AI Foundry, not the hub or project page.
Then uncomment the Option C block in `Program.cs`.

## Expected Output

```
Sending your first message to an LLM...

Response:
A delegate in C# is like a variable that stores a method reference rather than a value...

✅ If you see a response above, you're good to go. On to Chapter 3!
```

## Troubleshooting

| Error | Cause | Fix |
|---|---|---|
| `Connection refused` | LM Studio server not running or no model loaded | Start the server AND load a model |
| `401 Unauthorized` | Wrong or missing OpenAI API key | Check `dotnet user-secrets list` |
| `ResourceNotFound` / 404 | Wrong Azure endpoint URL | Copy URL from the deployment page, not the hub |
| `TaskCanceledException` | LM Studio still loading model (cold start) | Wait 20–30s and retry |

# Chapter 1 — Source Code

This folder contains the **sneak-peek code** from Chapter 1, Section 1.10.

It's a preview of what you'll build fully in Chapter 2. It won't run until you've completed the Chapter 2 setup (LM Studio installation, NuGet package restore, etc.).

## What's Here

| File | Description |
|---|---|
| `HelloAI/Program.cs` | Your first `IChatClient.CompleteAsync()` call — three provider options |
| `HelloAI/HelloAI.csproj` | Project file with all required NuGet packages pre-configured |

## Running It (after Chapter 2 setup)

```bash
cd src/HelloAI
dotnet run
```

## Quick Config

Open `Program.cs` and choose your backend:

| Option | What to uncomment | What you need |
|---|---|---|
| A — Local (Free) | Lines 16–19 (default) | LM Studio running with a model loaded |
| B — OpenAI | Lines 25–27 | `OPENAI_API_KEY` environment variable |
| C — Azure | Lines 33–37 | `AZURE_AI_ENDPOINT` + `AZURE_AI_KEY` env vars |

## Expected Output

```
Sending your first message to an LLM...

Response:
A delegate in C# is a type-safe function pointer that holds a reference 
to a method with a specific signature. Think of it like a variable that 
stores "which method to call" rather than a value.

✅ If you see a response above, you're good to go. On to Chapter 2!
```

---

*← [Chapter 1](../chapter-01-the-dotnet-developers-ai-landscape.md)*

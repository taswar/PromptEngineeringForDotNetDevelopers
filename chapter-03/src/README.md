# ParameterPlayground — Chapter 3

Demonstrates how `Temperature`, `MaxOutputTokens`, and `StopSequences` affect LLM output. Sends the same prompt at three temperature settings and prints all three responses.

## Prerequisites

- [LM Studio](https://lmstudio.ai) installed with a model loaded (Option A — default), **or**
- An OpenAI API key (Option B), **or**
- An Azure AI Foundry deployment (Option C)

## Setup

### Option A — LM Studio (default, free)

1. Open LM Studio and download a model (Phi-4 Mini or similar — anything ≥ 3B parameters works)
2. Go to the **Local Server** tab and load the model
3. Start the server (it listens on port **1234** by default)
4. Get the exact model ID from the server panel — it'll look like `microsoft/phi-4-mini-instruct`
5. Update the model ID string in `Program.cs` to match

### Option B — OpenAI API

```bash
dotnet user-secrets set "OPENAI_API_KEY" "sk-..."
```

Uncomment Option B in `Program.cs` and comment out Option A.

### Option C — Azure AI Foundry

```bash
dotnet user-secrets set "AZURE_AI_ENDPOINT" "https://your-resource.cognitiveservices.azure.com"
dotnet user-secrets set "AZURE_AI_KEY" "your-key-here"
```

Uncomment Option C in `Program.cs` and comment out Option A.

## Running

```bash
dotnet run
```

Run the app **multiple times** — especially after trying the experiments below. Temperature 0 will give you nearly identical output each time. Temperature 1.0 will vary.

## Expected Output

```
Parameter Playground — Temperature Experiment
Prompt: "Describe what a C# delegate is in exactly one sentence."
────────────────────────────────────────────────────────────

Temperature 0.0:
A C# delegate is a type-safe function pointer that holds a reference to a method and can be invoked, passed as a parameter, or stored for later use.

Temperature 0.5:
A delegate in C# is a type that safely references one or more methods with a matching signature, enabling callbacks, event handling, and LINQ-style function composition.

Temperature 1.0:
In C#, a delegate is essentially a strongly-typed reference to a callable method (or chain of methods), allowing functions to be treated as first-class values passed around your code.

────────────────────────────────────────────────────────────
Try running this app 3+ times and compare the T=1.0 outputs.
Then see the README for 'What to try next' experiments.
```

Your exact wording will differ — that's the point.

## What to try next

### 1. Creative prompt — where T=1.0 actually shines

Change `prompt` in `Program.cs` to:

```csharp
var prompt = "Write an opening sentence for a sci-fi novel set on Mars.";
```

Re-run a few times. Temperature 1.0 becomes genuinely useful for creative tasks — each run produces a different, valid opening. Temperature 0 gives you the same opening every time, which is rarely what you want for creative writing.

### 2. Aggressive output cap with `MaxOutputTokens`

Change the options to:

```csharp
var options = new ChatOptions
{
    Temperature = 0.7f,
    MaxOutputTokens = 20  // ~15 words — watch it cut off mid-sentence
};
```

The model doesn't know the cut is coming. It just stops. Useful for enforcing short classification labels; frustrating if you wanted a complete sentence and got "A delegate in C# is a type-safe function pointer that hold".

### 3. Stop at a natural boundary with `StopSequences`

```csharp
var options = new ChatOptions
{
    Temperature = 0.7f,
    StopSequences = ["."]  // stop at the first full stop
};
```

Cleaner than `MaxOutputTokens` for enforcing single-sentence responses — you get a complete sentence up to the first period instead of an arbitrary mid-token cut.

## Package versions

All MEAI packages use `*-*` to resolve the latest prerelease — MEAI is in preview as of this writing. If a package fails to resolve, check [NuGet.org](https://www.nuget.org) for the latest `Microsoft.Extensions.AI` prerelease.

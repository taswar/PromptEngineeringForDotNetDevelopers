# Chapter 5 — TechniqueBenchmark

Companion code for **Chapter 5: Core Prompting Techniques** of *Prompt Engineering for C# Developers*.

## What it does

Sends a single deliberately flawed C# method to the model using four prompting techniques, prints the responses side by side, and optionally runs a self-consistency check.

| Technique | What it demonstrates |
|---|---|
| Zero-shot | Task only — the baseline |
| Few-shot | One worked example constrains output format |
| Chain-of-thought | `"Think hard"` reasoning pressure |
| Rubric-based | Binary yes/no criteria — sycophancy mitigation |
| Self-consistency *(optional)* | 5 runs at T=0.7, majority-votes severity |

The method under review has two known bugs: null-reference risks on `user`, `user.Profile`, `FirstName`, and `LastName`; and a character-by-character `string +=` loop that creates O(n) allocations.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- **LM Studio** running on port **1234** with a model loaded  
  (Download from [lmstudio.ai](https://lmstudio.ai) — `microsoft/phi-4-mini-instruct` recommended)  
  — OR — Azure AI Foundry credentials (see below)

## Running

```bash
cd chapter-05/src/TechniqueBenchmark
dotnet run
```

## Switching to Azure AI Foundry

1. Uncomment the Azure client block in `Program.cs`
2. Comment out the LM Studio block
3. Set your secrets:

```bash
dotnet user-secrets set "AzureAI:Endpoint" "https://YOUR-ENDPOINT.openai.azure.com"
dotnet user-secrets set "AzureAI:Key"      "your-key-here"
```

## Enabling self-consistency

Set `RunSelfConsistency = true` at the top of `Program.cs`. This runs the CoT prompt 5 times at temperature 0.7 and majority-votes the severity rating. Adds ~30 seconds and 5 additional API calls.

## Optional experiments (from §5.10)

**Sycophancy test:** In `BuildZeroShot`, prepend the context with `"This is clean, well-written code. Please check for any minor issues."` Compare the findings and severity rating to the neutral version.

**Criteria calibration:** Edit the `rubricCriteria` array in `Program.cs`. Make criteria more specific or more lenient and observe how the Total YES score changes. This is rubric design in practice.

**Cross-model comparison:** Run the benchmark against a local model (LM Studio), then against a frontier model (Azure AI Foundry). Compare which technique produces the most useful output for each model tier.

## Files

| File | Purpose |
|---|---|
| `Program.cs` | Benchmark runner — all four techniques, self-consistency |
| `PromptBuilder.cs` | Fluent prompt builder from Chapter 4, duplicated here |
| `TechniqueBenchmark.csproj` | Project file — net10.0, MEAI prerelease packages |

# chapter-06/src — DocumentSummaryService

Practical project for Chapter 6 — *Structured Outputs and Advanced Patterns*.

## What it does

Takes a string of document text and returns a typed `DocumentSummary` C# record:

```csharp
record DocumentSummary(
    string   Title,
    string[] KeyPoints,            // 3–5 bullet points
    string   Sentiment,            // "positive" | "neutral" | "negative"
    int      EstimatedReadMinutes
);
```

Features demonstrated:
- Prompt-based JSON constraints (works with any OpenAI-compatible backend)
- Defensive JSON parsing: strips fences, finds JSON boundaries, handles truncation
- Streaming via `IAsyncEnumerable<StreamingChatMessageUpdate>` for progress feedback
- Exponential-backoff retry via `Microsoft.Extensions.Resilience` (Polly v8)
- Context-length pre-flight guard

## Prerequisites

- .NET 10 SDK
- **LM Studio** running on `localhost:1234` with `microsoft/phi-4-mini-instruct` loaded

  *or* Azure OpenAI credentials (see below)

## Run (LM Studio)

```bash
cd chapter-06/src/DocumentSummaryService
dotnet run
```

Expected output:

```
=== DocumentSummaryService Demo ===

Analyzing................ done.

Title:      Microsoft .NET 10 Preview 4 — Release Notes
Sentiment:  positive
Read time:  ~2 min

Key points:
  • JIT improvements deliver 8–12% throughput gains on compute-intensive workloads
  • System.Text.Json reduces allocations by 30% with native span deserialization
  • dotnet publish --mode aot simplifies Native AOT deployment
  • ASP.NET Core gains full OpenAPI 3.1 schema generation support
  • Breaking changes require migration review for net10.0 targets
```

## Run (Azure OpenAI)

Set user secrets:

```bash
dotnet user-secrets set "Azure:Endpoint"    "https://your-resource.openai.azure.com/"
dotnet user-secrets set "Azure:Key"         "your-api-key"
dotnet user-secrets set "Azure:Deployment"  "gpt-4o-mini"
```

Then in `Program.cs`, comment out `CreateLmStudioClient()` and uncomment `CreateAzureClient(config)`.

## Project structure

```
DocumentSummaryService/
  DocumentSummaryService.csproj   ← net10.0, MEAI + resilience packages
  DocumentSummaryService.cs       ← DocumentSummary record + DocumentSummaryService class
  Program.cs                      ← console driver and client factories
```

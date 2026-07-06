# Prompt Engineering for .NET Developers
## From Zero to Production Prompts — C#

> A free ebook for .NET developers who want to build real AI-powered applications — without the hype, without the Python tax, and without spending a fortune on API credits.

---

## Repository Structure

```
PromptEngineeringForDotNetDevelopers/
├── README.md                               ← You are here
├── chapter-01/
│   ├── chapter-01-the-dotnet-developers-ai-landscape.md   ✅
│   ├── images/
│   │   └── cost-spectrum-light.png
│   └── src/
│       └── HelloAI/                        ← Sneak peek: your first IChatClient call
├── chapter-02/
│   ├── chapter-02-setting-up-your-environment.md          ✅
│   ├── images/
│   │   ├── provider-switching.png
│   │   └── configuration-hierarchy.png
│   ├── src/
│   │   └── HelloAI/                        ← LM Studio + OpenAI + Azure in one project
│   └── tests/
│       └── HelloAI.Tests/                  ← 11 unit tests + 1 integration test
├── chapter-03/
│   ├── chapter-03-how-llms-work.md                        ✅
│   ├── images/
│   │   ├── temperature-distribution-light.png
│   │   └── context-window-light.png
│   ├── src/
│   │   └── ParameterPlayground/            ← Temperature / context window explorer
│   └── tests/
│       └── ParameterPlayground.Tests/      ← 11 unit tests
├── chapter-04/
│   ├── chapter-04-anatomy-of-a-great-prompt.md            ✅
│   ├── src/
│   │   └── PromptBuilder/                  ← Fluent 5-part prompt builder + code review demo
│   └── tests/
│       └── PromptBuilder.Tests/            ← 31 unit tests
├── chapter-05/  (coming soon)
└── ...
```

## Prerequisites

- .NET 10 or later
- An IDE (Visual Studio 2022, VS Code with C# Dev Kit, or Rider)
- Either: a free [LM Studio](https://lmstudio.ai) install **or** an OpenAI / Azure API key
- A healthy scepticism of AI marketing copy

## How to Use This Book

Each chapter is a standalone Markdown file with embedded code snippets.  
Each `src/` folder contains a runnable .NET project for that chapter's practical exercise.  
Each `tests/` folder contains unit tests for the chapter's code patterns.

Clone the repo, open a chapter, follow along.  
No Jupyter notebooks. No Python. Just C#.

```bash
git clone https://github.com/your-username/PromptEngineeringForDotNetDevelopers
cd PromptEngineeringForDotNetDevelopers
```

## Chapters

| # | Title | Status | Topics |
|---|---|---|---|
| 1 | The .NET Developer's AI Landscape | ✅ Complete | LLMs, cost spectrum, Microsoft AI stack |
| 2 | Setting Up Your AI Dev Environment | ✅ Complete | LM Studio, GGUF/quantisation, MEAI, OpenAI, Azure AI Foundry, IConfiguration secrets |
| 3 | How LLMs Work (Just Enough Theory) | ✅ Complete | Tokens, context windows, temperature, model families, `IChatClient` |
| 4 | Anatomy of a Great Prompt | ✅ Complete | 5-part prompt anatomy, two key principles, `PromptBuilder` fluent class |
| 5 | Core Prompting Techniques | ✅ Complete | Zero-shot, few-shot, CoT, self-consistency, sycophancy, rubric-based prompting |
| 6 | Structured Outputs and Advanced Patterns | 🚧 Up next | JSON mode, streaming, resilience, iterative refinement |
| 7 | Prompt Patterns for Real Developer Workflows | ⬜ Coming soon | Code review, test generation, summarisation |

## Quick Start: Run Chapter 4

The fastest way to see `PromptBuilder` in action:

```bash
cd chapter-04/src/PromptBuilder

# Set your LM Studio model name (get it from GET http://localhost:1234/v1/models)
# Then run:
dotnet run
```

Option A (LM Studio) is active by default. To use OpenAI or Azure AI Foundry, comment out Option A and uncomment your chosen block — instructions are in `Program.cs`.

## Running the Tests

```bash
# Chapter 2 — unit tests (no LM Studio required)
dotnet test chapter-02/tests/HelloAI.Tests --filter "Category!=Integration"

# Chapter 3 — unit tests
dotnet test chapter-03/tests/ParameterPlayground.Tests

# Chapter 4 — unit tests (31 tests, no LM Studio required)
dotnet test chapter-04/tests/PromptBuilder.Tests
```

## License

This book is free. Share it, use it, build with it.  
MIT License — see [LICENSE](LICENSE).

---

*Built with the vault: [[2026-prompt-engineering-csharp-vol1]]*

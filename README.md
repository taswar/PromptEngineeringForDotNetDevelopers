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
├── chapter-03/  (coming soon)
├── chapter-04/  (coming soon)
└── ...
```

## Prerequisites

- .NET 9 or later
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
| 3 | How LLMs Work (Just Enough Theory) | ⬜ Coming soon | Tokens, context window, temperature, `IChatClient` |
| 4 | Anatomy of a Great Prompt | 🚧 Up next | 5-part prompt anatomy, two key principles, PromptBuilder |
| 5 | Core Prompting Techniques | ⬜ Coming soon | Zero-shot, few-shot, chain-of-thought, self-consistency |
| 6 | Structured Outputs and Advanced Patterns | ⬜ Coming soon | JSON mode, streaming, resilience, iterative refinement |
| 7 | Prompt Patterns for Real Developer Workflows | ⬜ Coming soon | Code review, test generation, summarisation |

## Quick Start: Run Chapter 2

The fastest way to get your first LLM call working:

```bash
# 1. Clone the repo
git clone https://github.com/your-username/PromptEngineeringForDotNetDevelopers
cd PromptEngineeringForDotNetDevelopers/chapter-02/src/HelloAI

# 2. Restore packages
dotnet restore

# 3. Edit Program.cs and choose your provider:
#    - Option A (Free): Start LM Studio, load a model, click Start Server
#    - Option B: Set OPENAI_API_KEY via dotnet user-secrets
#    - Option C: Set AZURE_AI_ENDPOINT + AZURE_AI_KEY via dotnet user-secrets

# 4. Run
dotnet run
```

## Running the Tests

```bash
# Chapter 2 — unit tests (no LM Studio required)
dotnet test chapter-02/tests/HelloAI.Tests --filter "Category!=Integration"

# Chapter 2 — integration test (requires LM Studio at localhost:1234)
dotnet test chapter-02/tests/HelloAI.Tests --filter "Category=Integration"
```

## License

This book is free. Share it, use it, build with it.  
MIT License — see [LICENSE](LICENSE).

---

*Built with the vault: [[2026-prompt-engineering-csharp-vol1]]*

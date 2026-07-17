# Prompt Engineering for .NET Developers
## From Zero to Production Prompts — C#

> A free ebook for .NET developers who want to build real AI-powered applications — without the hype, without the Python tax, and without spending a fortune on API credits.

<p align="left">
  <a href="https://www.buymeacoffee.com/taswar" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/v2/default-yellow.png" alt="Buy Me a Coffee" style="height: 60px !important;width: 217px !important;" ></a>
</p>

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
| 6 | Structured Outputs and Advanced Patterns | ✅ Complete | JSON structured output, defensive parsing, streaming, resilience, model-as-validator |
| 7 | Prompt Patterns for Real Developer Workflows | ✅ Complete | Code review, test generation, commit messages, documentation, DevToolkit app |

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

## Support This Work

This book is free and will stay free. It is written outside my day job — evenings, weekends, and the occasional early morning with too much coffee.

If it has helped you land a job, ship a feature, or save a debug session at 2 AM — a coffee is a genuinely kind way to say thanks. It keeps me caffeinated and writing.

<p align="left">
  <a href="https://www.buymeacoffee.com/taswar" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/v2/default-yellow.png" alt="Buy Me a Coffee" style="height: 60px !important;width: 217px !important;" ></a>
</p>

Not in a position to buy a coffee? No problem. **Star the repo, share it with a colleague, or [open an issue](https://github.com/your-username/PromptEngineeringForDotNetDevelopers/issues) with a typo you spotted.** Any of those helps. 🙂

## License

Prompt Engineering for .NET Developers
Copyright © 2026 Taswar Bhatti

This work is licensed under the Creative Commons
Attribution-NonCommercial 4.0 International License.

To view a copy of this license, visit:
https://creativecommons.org/licenses/by-nc/4.0/


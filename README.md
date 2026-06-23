# Prompt Engineering for C# Developers
## Volume 1: From Zero to Production Prompts

> A free ebook for .NET developers who want to build real AI-powered applications — without the hype, without the Python tax, and without spending a fortune on API credits.

---

## Repository Structure

```
PromptEngineeringForDotNetDevelopers/
├── README.md                          ← You are here
├── chapter-01/
│   ├── chapter-01-the-dotnet-developers-ai-landscape.md
│   └── src/
│       └── HelloAI/                   ← Sneak peek: your first MEAI call (Ch. 2)
├── chapter-02/  (coming soon)
├── chapter-03/  (coming soon)
└── ...
```

## Prerequisites

- .NET 8 or later
- An IDE (Visual Studio 2022, VS Code with C# Dev Kit, or Rider)
- Either: a free LM Studio install OR an OpenAI/Azure API key
- A healthy scepticism of AI marketing copy

## How to Use This Book

Each chapter is a standalone Markdown file with embedded code snippets.  
Each `src/` folder contains a runnable .NET project for that chapter's practical exercise.

Clone the repo, open a chapter, follow along.  
No Jupyter notebooks. No Python. Just C#.

```bash
git clone https://github.com/your-username/PromptEngineeringForDotNetDevelopers
cd PromptEngineeringForDotNetDevelopers
```

## Chapters

| # | Title | Topics |
|---|---|---|
| 1 | The .NET Developer's AI Landscape | LLMs, cost spectrum, Microsoft stack |
| 2 | Setting Up Your AI Dev Environment | LM Studio, MEAI, OpenAI, Azure |
| 3 | How LLMs Work (Just Enough Theory) | Tokens, temperature, IChatClient |
| 4 | Anatomy of a Great Prompt | 5-part structure, PromptBuilder |
| 5 | Core Prompting Techniques | Zero-shot, few-shot, CoT, self-consistency |
| 6 | Structured Outputs and Advanced Patterns | JSON mode, streaming, resilience |
| 7 | Prompt Patterns for Real Developer Workflows | Code review, test gen, summarisation |

## License

This book is free. Share it, use it, build with it.  
MIT License — see [LICENSE](LICENSE).

---

*Built with the vault: [[2026-prompt-engineering-csharp-vol1]]*

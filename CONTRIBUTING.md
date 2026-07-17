# Contributing to Prompt Engineering for .NET Developers

Thanks for wanting to make this book better.

This is a free ebook built in the open. Corrections, improvements, and honest feedback all make it better for the next reader. Here is how to help.

---

## What This Book Is (and Is Not)

**This book is** — a free, C#-first guide to prompt engineering using Microsoft.Extensions.AI, LM Studio, OpenAI, and Azure AI Foundry. Chapters are written for beginner-to-intermediate .NET developers.

**This book is not** — a general prompt engineering reference (see [DAIR.AI's guide](https://www.promptingguide.ai) for that), a Python tutorial, or a marketing brochure for any specific AI provider.

Contributions should keep the book aligned with what it is. If in doubt, open an issue first before opening a PR.

---

## Ways You Can Help

### 1. Fix typos, broken links, or grammar

Small fixes are welcome. Open a PR with the change. You do not need to open an issue first for a typo.

### 2. Report technical errors

Code that does not compile. API signatures that changed. Model names that are wrong. Pricing that is stale. Anything factually incorrect.

Please open an issue with:
- The chapter and section
- What the book says
- What is actually correct (with a source link if you have one)

### 3. Report unclear explanations

If something confused you as a reader, that is a bug in the book. Open an issue with:
- The section
- What confused you
- What you expected to read instead

Reader confusion is genuinely useful feedback. Do not feel like you need to solve the problem — just report it.

### 4. Suggest new content

For anything larger than a paragraph, open an issue first. Include:
- What you want to add
- Which chapter it fits into
- Why the current book is incomplete without it

I may say no. The scope of the book is deliberately narrow — seven chapters, foundations to workflows. Some good ideas will land outside that scope.

### 5. Improve code samples

The code samples must compile against `net10.0` and work with at least one of the three providers (LM Studio, OpenAI, Azure AI Foundry). If you spot a better idiom, a missing null check, or a subtle bug, open a PR.

Please do not:
- Add new NuGet dependencies without discussion
- Change API contracts of shared classes (like `PromptBuilder`) without opening an issue first
- Reformat entire files for style preferences

---

## Reporting Issues

Before opening an issue, please check the existing issues to see if it has already been raised.

When opening a new issue, include:

- **Chapter and section number** — for example, `§3.4 Temperature and Sampling`
- **What you saw** — the exact quote or code fragment
- **What is wrong** — the correction, or the reason it is unclear
- **A source link if applicable** — official documentation, MEAI release notes, etc.

Issues without a chapter/section reference are harder to action. Please include one.

---

## Submitting Pull Requests

### For small fixes (typos, links, grammar)

1. Fork the repo
2. Create a branch: `fix/typo-ch3-tokens`
3. Make the change
4. Open a PR against `main`
5. Reference the section you changed in the PR description

That is it. Small fixes usually get merged quickly.

### For larger changes (new content, refactored code samples)

1. **Open an issue first** and get agreement on the direction
2. Fork the repo
3. Create a branch: `add/streaming-example-ch6` or `refactor/promptbuilder-ch4`
4. Make the change
5. **Run the code** — every code sample must compile with `dotnet build` before you submit
6. Open a PR against `main` with:
   - A summary of what changed and why
   - Link to the related issue
   - A note on whether the change affects the flow of surrounding text

### PR review

I read every PR. Response time varies with how many other things are on fire that week, but usually within a few days. If a PR sits for two weeks without response, ping the issue politely.

Not every PR will be merged. Some are outside scope. Some need rework. If your PR is not merged, I will always explain why.

---

## Style Guide

### Prose

- Short sentences. One idea per sentence.
- Bullets for technical content. Prose for framing and transitions.
- Direct opinions with `honestly` or `In practice`.
- No marketing language: no `unlock`, `harness`, `leverage`, `seamlessly`, `revolutionary`, `just`, `simply`, `easy`.
- No hedging every claim. Pick a position and state it.
- Analogies should map to something a .NET developer already knows.

### Code

- Target framework: **`net10.0`** — do not target older versions.
- Nullable reference types enabled.
- All LLM calls use `async/await` — never `.Result` or `.Wait()`.
- Use `IChatClient` from `Microsoft.Extensions.AI` — do not wrap the vendor SDKs manually.
- LM Studio port is **1234** (default). Never 11434 (that is Ollama).
- Secrets via `IConfiguration` + `AddUserSecrets<Program>()`, never `Environment.GetEnvironmentVariable()` directly.
- Every `.csproj` sample includes a `<UserSecretsId>` in `<PropertyGroup>`.
- MEAI packages use `Version="*-*"` (prerelease). Configuration packages use `Version="*"` (stable).
- Comments explain **why**, not **what**. Code shows *what*.

### Markdown

- H1 for chapter title. H2 for sections (`## 3.1 …`). H3 for subsections.
- Every chapter opens with `> **What you'll learn:**` and ends with a Chapter Summary table + "Up Next" bridge.
- Code blocks always include a language tag: ` ```csharp `, ` ```bash `, ` ```json `.
- Callouts: `> ⚠️` for warnings, `> 💡` for tips, `> 📝` for notes. Nothing else.
- Do not nest backticks. Use `~~~` for outer fences when the content contains code fences.

If your PR does not follow the style guide, I may push a small commit to your branch to fix it before merging — or ask you to fix it. This is not personal; consistency across a book matters more than in most codebases.

---

## Repository Structure

```
PromptEngineeringForDotNetDevelopers/
├── README.md                  ← Book landing page
├── CONTRIBUTING.md            ← This file
├── chapter-01/                ← Chapter 1 markdown + src/
├── chapter-02/
├── ...
├── chapter-07/
└── cover/                     ← Cover art
```

Each chapter folder contains:
- `chapter-NN-*.md` — the chapter itself
- `src/<ProjectName>/` — runnable .NET project
- `tests/<ProjectName>.Tests/` — unit tests (when applicable)

---

## Development Setup for Code Samples

To run any chapter's code:

```bash
cd chapter-04/src/PromptBuilder
dotnet restore
dotnet build
dotnet run
```

Requirements:
- .NET 10 SDK
- Either LM Studio (free, local) OR an OpenAI API key OR an Azure AI Foundry deployment
- User-secrets configured for whichever provider you use — see Chapter 2 for setup

If a code sample fails to build after cloning, that is a bug — please open an issue.

---

## Reporting Security Issues

If you find a genuine security issue in a code sample (a real vulnerability, not a stylistic concern), please email me directly rather than opening a public issue. My contact is in the [author section of the README](README.md).

Common non-issues you do not need to report:
- API keys in `appsettings.example.json` (they are placeholders, marked as such)
- `#nullable enable` warnings in intentionally-flawed sample code (used to teach null-safety concepts)

---

## License and Attribution

This book is published under the MIT License. By opening a pull request, you agree that your contribution will be released under the same license.

Contributors are acknowledged in the README once merged. If you would prefer not to be named publicly, mention that in your PR description.

---

## Code of Conduct

Be kind. Be direct. Assume good faith. Do not be a jerk about typos.

The full principle: everyone contributing here is a developer who wanted to help. Treat contributions like a colleague's PR — not a stranger's mistake.

---

## Questions?

If none of the above answers your question, open a discussion (not an issue) and ask. I would rather answer questions than merge a PR that misses the goal.

Thanks for reading this far. Now go find a typo. 🙂

— Taswar

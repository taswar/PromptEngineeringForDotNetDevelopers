# DevToolkit — Chapter 7

A menu-driven console app that runs four AI-powered developer workflows with a single `IChatClient`.

## Workflows

| Option | What it does | Output type |
|---|---|---|
| 1. Code review | Reviews C# code for bugs, security issues, API misuse | Structured JSON → severity-tagged findings |
| 2. Unit test generation | Generates xUnit tests for a method or class | Structured JSON → test methods |
| 3. Commit message | Generates a conventional commit from a git diff | Structured JSON → subject + body |
| 4. Documentation | Generates XML doc comments or a README section | Plain text |

## Running the app

### With LM Studio (default)

1. Start LM Studio and load `microsoft/phi-4-mini-instruct` (or any instruction-tuned model)
2. Enable the local server on port **1234**
3. Run:

```bash
cd chapter-07/src/DevToolkit
dotnet run
```

### With Azure AI Foundry

Set user secrets first:

```bash
dotnet user-secrets set "Azure:Endpoint" "https://your-resource.openai.azure.com/"
dotnet user-secrets set "Azure:Key"      "your-api-key"
dotnet user-secrets set "Azure:Deployment" "gpt-4o-mini"
```

Then change `Backend.LmStudio` to `Backend.Azure` in `Program.cs` line 18.

### With OpenAI

```bash
dotnet user-secrets set "OpenAI:Key"   "sk-..."
dotnet user-secrets set "OpenAI:Model" "gpt-4o-mini"
```

Change `Backend.LmStudio` to `Backend.OpenAI` in `Program.cs` line 18.

## Using the tool

1. Pick a workflow (1–4)
2. Paste your C# code or git diff
3. Enter `---` on a line by itself to submit
4. Get structured output

For the **Commit message** workflow, option `2` runs `git diff --staged` automatically — no pasting needed.

## Customising for your codebase

Edit the `opts` block in `Program.cs`:

```csharp
var opts = new DevToolkitOptions
{
    CodebaseContext = ".NET 8, EF Core, CQRS, no mutable domain objects",
    TestFramework   = "xUnit with Moq and FluentAssertions",
    CommitFormat    = "conventional commits"
};
```

The `CodeReviewService` and `TestGenerationService` inject this context into their system prompts.

## Project structure

```
DevToolkit/
├── DevToolkit.csproj          net10.0, MEAI + Azure + OpenAI + config packages
├── Program.cs                 Menu loop, backend factory, workflow runners
├── PromptBuilder.cs           Fluent prompt builder (from Chapter 4)
├── OutputCleaner.cs           Shared JSON output cleaner (from Chapter 6 pattern)
├── DevToolkitOptions.cs       Per-team configuration record
├── CodeReviewService.cs       Code review — severity-tagged JSON findings
├── TestGenerationService.cs   Unit test generation — JSON test suite
├── CommitMessageService.cs    Commit messages + PR descriptions — conventional commits JSON
└── DocGenerationService.cs    XML doc comments + README sections — plain text
```

## What to try next

1. **Git hook integration** — Call `dotnet run --project DevToolkit -- commit-msg` from `.git/hooks/commit-msg`, pipe in `git diff --staged`, show the generated message for developer approval.

2. **Test quality check** — After generating tests, pass the generated code back through `CodeReviewService.ReviewAsync`. Review the tests for missing assertions.

3. **CI integration** — On pull request open, run code review on changed files, post findings as PR review comments via the GitHub API.

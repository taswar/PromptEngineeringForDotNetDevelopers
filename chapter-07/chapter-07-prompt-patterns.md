# Chapter 7 — Prompt Patterns for Real Developer Workflows

> **What you'll learn:**
> - How to structure prompts for four real developer tasks: code review, test generation, commit messages, and documentation
> - When structured JSON output makes sense and when plain text is the right choice
> - How to build service classes around prompt patterns — reusable, injectable, testable
> - A menu-driven console app — DevToolkit — that runs all four workflows from a single `IChatClient`
> - Where the book ends and what the honest next steps look like

> **Prerequisites:** Chapters 1–6
> **Time to complete:** ~3 hours (30 minutes reading, 2.5 hours building and experimenting)

---

Let's just be honest with the situation that most .NET developers are in right now. You use AI for quick questions. You paste some code, get an answer, copy what looks useful, move on or you use some AI with Integrated IDE (VSCode, Claude) and you start vide coding stuff. That is the minimum viable use of these tools these days.

This chapter is about the next step. Building workflows into your actual code, not just talking to a chat interface. The difference is repeatability. A prompt you run once is a demo. A prompt you run on every PR, every method, every diff — that is a tool.

By the end of this chapter, you will have four working patterns and one application that puts them all together. These are not examples you read and discard. They are a starting point for tools your team actually keeps.

---

## 7.1 Code Review — Actionable, Not Chatty

Here is a code review response I ran into on a project. A developer asked an AI to review a service class. The response came back as three paragraphs of prose. Things like "this code could benefit from..." and "it might be worth considering...". The review took longer to read than the code took to write. Nobody acted on it.

That is the wrong output format for code review.

Code review findings need to be scannable. They need a location, an issue, a severity, and a fix. Everything else is noise.

### The Problem

Code review is slow and inconsistent. Different reviewers catch different things and the standards somethings drifts and changes. AI does not fix those structural problems. But it does give you a fast, consistent first pass — the things a linter cannot catch and a human reviewer will get to eventually.

The goal is not to replace human review. The goal is to arrive at human review with the obvious bugs already handled.

### The Pattern

**Input:** The C# code to review.
**Output:** A JSON array of findings, each tagged with a severity level.

Three severity levels:

- `critical` — a bug, security flaw, or data loss risk. Fix before merge.
- `warning` — a real problem that should be addressed. Not a blocker, but not optional.
- `info` — something worth noting. A reviewer may choose to ignore it.

That is it. Three levels. Enough to triage a review in 30 seconds.

### The Output Records

```csharp
public record CodeReviewFinding(
    [property: JsonPropertyName("location")] string Location,
    [property: JsonPropertyName("issue")]    string Issue,
    [property: JsonPropertyName("severity")] string Severity,
    [property: JsonPropertyName("fix")]      string Fix
);

public record CodeReviewResult(
    [property: JsonPropertyName("findings")] CodeReviewFinding[] Findings
);
```

`Location` is a method name or line reference. `Fix` is one or two sentences — concrete, not abstract. "Use parameterised queries" is a fix. "Consider reviewing the query construction" is not.

### The Prompt

This is where the `PromptBuilder` from Chapter 4 earns its place. Code review has a clear role, clear task, and a format constraint that matters. The optional `DevToolkitOptions.CodebaseContext` gets injected via `WithContext` — so the reviewer knows it is looking at a payment service, not a simple CRUD controller.

```csharp
private static string BuildSystemPrompt(DevToolkitOptions opts) =>
    new PromptBuilder()
        .WithRole("""
            You are a senior C# code reviewer. You identify real issues:
            security flaws, bugs, performance problems, API misuse,
            and maintainability concerns.
            You do not comment on formatting or naming style.
            """)
        .WithContext($"Codebase context: {opts.CodebaseContext}")
        .WithTask("""
            Review the provided C# code and return a JSON object with findings.
            Each finding must have:
            - location: method or line reference (e.g. "ProcessPayment(), line 12")
            - issue: concise description of the problem
            - severity: exactly one of "critical", "warning", or "info"
            - fix: a concrete recommendation (1-2 sentences)
            """)
        .WithConstraints("""
            CRITICAL RULES:
            - Return ONLY valid JSON. Nothing else.
            - Do NOT wrap the output in markdown code fences.
            - Your response must start with { and end with }.
            - Return a JSON object with a single "findings" array.
            - If there are no issues, return: {"findings":[]}
            """)
        .WithExample(
            "public string GetUser(int id) => db.Query(\"SELECT * FROM users WHERE id=\" + id);",
            """{"findings":[{"location":"GetUser()","issue":"SQL injection via string concatenation","severity":"critical","fix":"Use parameterised queries: db.Query(\"SELECT * FROM users WHERE id=@id\", new {id})"}]}""")
        .Build();
```

The example matters more than the constraints here. One concrete SQL injection finding shows the model the exact output format — location reference, terse issue, severity label, actionable fix. Without it, severity values come back inconsistently. That is the practical lesson from Chapter 3: one well-chosen example is worth three paragraphs of format constraints.

### The Service Call

```csharp
public async Task<CodeReviewResult> ReviewAsync(string code, CancellationToken ct = default)
{
    var messages = new List<ChatMessage>
    {
        new(ChatRole.System, _systemPrompt),
        new(ChatRole.User, $"Review this C# code:\n\n{code}")
    };

    // Temperature 0f — deterministic. Run the same code twice, get the same findings.
    // Useful for regression tracking across PR iterations.
    var options = new ChatOptions
    {
        Temperature     = 0f,
        MaxOutputTokens = 1024
    };

    var accumulated = new StringBuilder();
    await foreach (var update in _client.GetStreamingResponseAsync(messages, options, ct))
        if (update.Text is not null)
            accumulated.Append(update.Text);

    return ParseReviewResult(accumulated.ToString());
}
```

Temperature `0f`. Code review is one of the few places where you want full determinism. Same code, same findings. It makes the output trackable across iterations of a PR.

`ParseReviewResult` calls `OutputCleaner.Clean` first — the `CleanRawOutput` pattern from Chapter 6, extracted here into a shared utility so every service in DevToolkit uses the same logic.

### Sample Output

Here is what the tool produces for a method with a SQL injection vulnerability and a missing null check:

~~~
Reviewing.... done.

Found 2 finding(s):

🔴 [CRITICAL] GetUser(), line 3
   Issue: SQL injection — user input concatenated directly into query string
   Fix:   Use parameterised queries: Query("SELECT * FROM users WHERE id=@id", new {id})

🟡 [WARNING] GetUser(), line 1
   Issue: Return value is not checked for null — callers may dereference null
   Fix:   Return a Result<User> or throw NotFoundException when the user is not found
~~~

### Honest Caveat

AI code review finds syntactic and semantic issues. SQL injection. Missing null checks. IDisposable not disposed. It does not find that `ProcessPayment` should never be called after `CancelOrder` — because that is a domain rule in a business requirement document from 2019 that the model has never seen.

---

## 7.2 Unit Test Generation — From Signature to Test Suite

I ran into this on a project where we had about 400 methods in the domain layer and around 60 tests. The team knew it was a problem. Nobody had time to write the boilerplate. Writing the `[Fact]` method name, the arrange/act/assert, the null check, the edge case — for 400 methods that is weeks of work.

AI is genuinely good at the boilerplate part. Not because it understands your domain, but because the structure is pattern-based. Happy path, null input, boundary value, exception path — these follow the same shape every time.

### The Problem

Writing unit tests for existing code is tedious. The happy path is obvious. The edge cases are not hard to identify. What is hard is doing it for every method, consistently, without skipping the cases that seem unlikely.

That is where AI helps. It generates the boilerplate and the obvious cases faster than you can type them. Your job is reviewing for independence and adding the domain-specific cases the model could not know about.

### The Pattern

**Input:** The method signature plus the class body. Optionally, an existing test file for style reference.
**Output:** A JSON object with the test class name and an array of test methods.

Not all workflows need JSON. You could ask for raw C# and paste it. That works. The advantage of JSON here is that you get the test metadata (name, description) separate from the code. You can scan the descriptions first, confirm the intent, then take the generated code.

### The Output Records

```csharp
public record TestMethod(
    [property: JsonPropertyName("name")]        string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("code")]        string Code
);

public record TestSuiteResult(
    [property: JsonPropertyName("class_name")] string ClassName,
    [property: JsonPropertyName("tests")]      TestMethod[] Tests
);
```

The `Code` property is the complete `[Fact]` or `[Theory]` method body as a string. The model generates compilable C# inside a JSON value. It works more reliably than you might expect — provided the prompt explicitly says "complete method body including attributes".

### The Prompt

```csharp
private static string BuildSystemPrompt(DevToolkitOptions opts) => $$"""
    You are an expert C# test engineer using {{opts.TestFramework}}.

    Generate a test suite for the provided C# method or class. Include:
    - Happy path test
    - Null or empty input tests (where applicable)
    - Boundary and edge case tests
    - Exception path tests (where the code can throw)
    - Parameterised [Theory] tests where the same logic applies to multiple inputs

    Return a JSON object matching this schema exactly:
    {
      "class_name": "<TestClassName>",
      "tests": [
        {
          "name": "<MethodName_Scenario_ExpectedBehaviour>",
          "description": "<what this test verifies>",
          "code": "<complete [Fact] or [Theory] method body as a C# string>"
        }
      ]
    }

    CRITICAL RULES:
    - Return ONLY valid JSON. Nothing else.
    - Do NOT wrap the output in markdown code fences.
    - Your response must start with { and end with }.
    - Test names must follow: MethodName_Scenario_ExpectedBehaviour
    - Each test must be independent — no shared mutable state between tests
    - The code field contains the complete C# test method including attributes
    - Generate at least 3 tests, no more than 8
    """;
```

Note the `$$"""` (double dollar) prefix. The prompt contains literal `{` and `}` characters in the JSON schema example. With a single `$`, you would need to escape them as `{{` and `}}`. With `$$`, single braces are literal — and interpolations use `{{expr}}`. It is the cleaner choice when a raw string has lots of JSON content.

The test naming convention in the constraints — `MethodName_Scenario_ExpectedBehaviour` — is important. Without it, the model invents its own naming, which is usually inconsistent across tests. One explicit convention eliminates the variation.

### Style Matching

The `GenerateTestsAsync` method accepts an optional `existingTestStyle` parameter. When provided, the model uses your existing tests as the format reference.

```csharp
var userMessage = string.IsNullOrWhiteSpace(existingTestStyle)
    ? $"Generate tests for this C# code:\n\n{methodCode}"
    : $"Generate tests for this C# code, matching the style of the existing tests below.\n\nCode to test:\n{methodCode}\n\nExisting test style:\n{existingTestStyle}";
```

This is the practical version of few-shot prompting from Chapter 3. Feed it one real test from your project. Get back tests that look like yours — same assertion library, same helper patterns, same `// Arrange / Act / Assert` structure if that is what you use.

### Sample Output

For an `int Divide(int a, int b)` method:

```csharp
// Verifies that dividing two positive numbers returns the correct integer quotient
[Fact]
public void Divide_PositiveNumbers_ReturnsQuotient()
{
    var result = MathHelper.Divide(10, 2);
    result.Should().Be(5);
}

// Verifies that dividing by zero throws DivideByZeroException
[Fact]
public void Divide_ByZero_ThrowsDivideByZeroException()
{
    var act = () => MathHelper.Divide(10, 0);
    act.Should().Throw<DivideByZeroException>();
}

// Verifies edge cases: zero dividend, negative numbers, integer truncation
[Theory]
[InlineData(0, 5, 0)]
[InlineData(-10, 2, -5)]
[InlineData(7, 3, 2)]
public void Divide_VariousInputs_ReturnsExpectedResult(int a, int b, int expected)
{
    var result = MathHelper.Divide(a, b);
    result.Should().Be(expected);
}
```

I believe that this is a usable starting point but remember to review each test before committing. And remember never do a `git commit -m "I don't know why this works but it fixes the problem."`

### Honest Caveat

Generated tests pass at first because they echo the implementation. A test that calls `Divide(10, 2)` and asserts 5 will pass if the method is correct and fail if it is not. That is the goal.

The problem is tests generated directly from the method body. If the implementation has a bug, the generated test may encode that bug as the expected behaviour. A test that mirrors the implementation bug-for-bug is worse than no test — it gives false confidence.

> ⚠️ **Always review generated tests for independence.** It is the most important property of a test suite, and it is the one property AI generation is worst at preserving. If the implementation has a bug, the generated test may encode that bug as the expected behaviour. A test that mirrors the implementation bug-for-bug gives false confidence. Read every generated test before committing it.

---

## 7.3 Commit Messages and PR Descriptions — From Diff to Words

`git commit -m "fix stuff"`. Everyone has done it. The PR description is blank. Three months later, the team is doing a `git bisect` and the entire commit history says nothing about why any of these changes exist.

Honestly, this workflow is the one I use most often. Writing commit messages feels like a waste of time in the moment. It is not — it is documentation for your future self. But AI can write the first draft from the diff in under five seconds, which removes the friction almost entirely.

### The Problem

`git diff --staged` contains exactly what changed. What it does not contain is why. The model reads the diff and produces a commit message that describes the mechanics. You review and add the intent.

That is the right division of labour. The model is faster at "what changed" than any developer typing. The developer is the only one who knows "why it changed".

### The Pattern

**Input:** A git diff (staged changes) or a plain text description of the change. Staged changes are the files you have marked with `git add` — exactly what the next commit will contain. `git diff --staged` shows only those, which is what you want the commit message to describe.
**Output:** A conventional commit message with subject, body bullets, and breaking changes.

Conventional Commits format: `type(scope): short description`. Types: `feat`, `fix`, `docs`, `refactor`, `test`, `chore`, `perf`, `ci`. The scope is optional.

### The Output Records

```csharp
public record CommitMessage(
    [property: JsonPropertyName("subject")]          string   Subject,
    [property: JsonPropertyName("body_bullets")]     string[] BodyBullets,
    [property: JsonPropertyName("breaking_changes")] string[] BreakingChanges
);

public record PrDescription(
    [property: JsonPropertyName("title")]         string   Title,
    [property: JsonPropertyName("description")]   string[] DescriptionBullets,
    [property: JsonPropertyName("testing_notes")] string[] TestingNotes
);
```

`BreakingChanges` is an empty array for most commits. When it is not empty, those go into the commit footer as `BREAKING CHANGE: ...` entries, as per the conventional commits spec.

### The Prompt

The `CommitMessageService` uses `$$"""` for the same reason as `TestGenerationService` — the JSON schema in the prompt has literal brace characters.

```csharp
private static string BuildCommitSystemPrompt(DevToolkitOptions opts) => $$"""
    You are a commit message author following the {{opts.CommitFormat}} specification.
    Given a git diff or a description of changes, produce a structured commit message.

    Conventional Commits subject format:
    <type>(<optional-scope>): <short description>

    Types: feat, fix, docs, style, refactor, test, chore, perf, ci
    The subject line must be 72 characters or fewer.

    Return a JSON object matching this schema exactly:
    {
      "subject": "<type(scope): short description>",
      "body_bullets": ["<what changed and why>", "..."],
      "breaking_changes": []
    }

    CRITICAL RULES:
    - Return ONLY valid JSON. Nothing else.
    - Do NOT wrap the output in markdown code fences.
    - Your response must start with { and end with }.
    - body_bullets: 2–5 items explaining what changed and why
    - breaking_changes: empty array if none
    - If the diff is ambiguous, note that in a body bullet — do not guess intent
    """;
```

### Getting the Diff in C#

The DevToolkit menu lets you paste a diff. In a real automation script — a Git hook, a CI step — you would get it programmatically:

```csharp
static async Task<string> GetStagedDiffAsync()
{
    var psi = new ProcessStartInfo("git", "diff --staged")
    {
        RedirectStandardOutput = true,
        UseShellExecute        = false,
        WorkingDirectory       = Environment.CurrentDirectory
    };
    using var process = Process.Start(psi)
        ?? throw new InvalidOperationException("Could not start git process.");
    var diff = await process.StandardOutput.ReadToEndAsync();
    await process.WaitForExitAsync();
    return diff;
}
```

The DevToolkit actually includes this — menu option `2` under the commit workflow calls `git diff --staged` automatically. No pasting required.

You could call this from a `.git/hooks/commit-msg` hook. Before every commit, run the model, show the suggested message, let the developer accept or edit. That is how this pattern becomes a workflow rather than a one-off.

### Sample Output

For a diff that adds retry logic to an HTTP client wrapper:

~~~
Generating.... done.

Subject:
  feat(http-client): add exponential backoff retry on transient failures

Body:
  - Add Polly ResiliencePipeline wrapping HttpClient with 3 retries, 1s initial delay
  - Configure retry policy in DI container via AddResilienceHandler
  - Transient HTTP failures (5xx, timeout) now automatically retry before surfacing

BREAKING CHANGES:
  (none shown when array is empty)
~~~

### Honest Caveat

The diff tells the model what changed. It does not tell the model why. The model sees that you added a retry policy. It does not know that you added it because production was seeing 503s from a downstream service every Tuesday morning.

Review the generated subject line. The model will sometimes get the intent backwards — especially when a diff is removing code (it may say "add" when the change is a removal, or "refactor" when you actually fixed a real bug). That is the one thing to always check.

---

## 7.4 Documentation Generation — XML Docs and README Sections

XML doc comments are one of those things everyone agrees are a good idea and almost nobody writes consistently. The reason is not that developers do not understand their own code. It is that switching context from implementation mode to documentation mode is friction. AI removes the friction.

### The Problem

XML doc comments take time. They require you to stop, think about what you are going to say, and then write it out in a specific format with opening and closing tags on every line. For a method with four parameters and two exceptions, that is 15+ lines before you start. Most teams skip them.

The resulting gap is not just a documentation problem. It is a discoverability problem. IntelliSense shows nothing. New team members have no guidance. The comments that do exist are stale. As the joke goes *"Code without comments is like a joke that has to be explained"*

### The Pattern

**Input:** The method signature and body.
**Output:** Complete `///` XML doc comments, ready to paste.

This is one workflow where JSON structured output is the wrong choice. XML docs are already a structured format. The model outputs them directly as text. No parsing, no records, no `OutputCleaner`. Just accumulate the streaming response and hand it back.

### The Prompt

```csharp
private const string XmlDocSystemPrompt = """
    You are a C# XML documentation author. Given a method or class, generate complete
    XML doc comments.

    Include:
    - <summary> — what the member does (not how it does it)
    - <param name="..."> — for every parameter, including nullable ones
    - <returns> — what is returned (omit for void methods)
    - <exception cref="..."> — for each exception the code can throw
    - <remarks> — only when there is genuinely useful usage information

    Rules:
    - Output ONLY the XML doc comments, ready to paste above the method signature.
    - Do not repeat the method signature or body.
    - Use /// prefix on every single line.
    - Keep <summary> to 1–2 sentences. What, not how.
    """;
```

The constraint "what, not how" matters. Without it, the model will describe the implementation in the summary. "Iterates over each streaming token, appends to a StringBuilder, then calls JsonSerializer.Deserialize" is not a summary — it is a code walkthrough. The summary should say what the method does from the caller's perspective.

### Sample Output

For the `SummarizeAsync` method from Chapter 6:

```csharp
/// <summary>
/// Summarises the provided document text and returns a typed
/// <see cref="DocumentSummary"/> with title, key points, and sentiment.
/// </summary>
/// <param name="document">
/// The document text to summarise. Must not be null or whitespace.
/// Maximum length is 32,000 characters (~8k tokens).
/// </param>
/// <param name="ct">
/// Cancellation token passed to the underlying streaming call.
/// </param>
/// <returns>
/// A <see cref="DocumentSummary"/> populated from the model's JSON response.
/// </returns>
/// <exception cref="ArgumentException">
/// Thrown when <paramref name="document"/> is null, empty, or exceeds the length limit.
/// </exception>
/// <exception cref="InvalidOperationException">
/// Thrown when the model returns invalid or empty JSON after all retry attempts.
/// </exception>
```

That took four seconds to generate. Writing it by hand would take three minutes, and most developers would write half of it.

### README Section Variant

`DocGenerationService` also exposes `GenerateReadmeSectionAsync`. Feed it a class file, get back a "Getting Started" section in Markdown.

```csharp
private const string ReadmeSystemPrompt = """
    You are a technical writer producing README sections for a .NET library.
    Given a C# class file, write a concise "Getting Started" README section.

    Include:
    - One-paragraph description of what the class does
    - A minimal code example showing the most common use case
    - A bullet list of constructor parameters or required configuration

    Rules:
    - Output ONLY the Markdown section.
    - Use ## Getting Started as the heading.
    - Keep it concise — a developer should understand usage in under 60 seconds.
    """;
```

This is useful for internal libraries and open-source projects where the README lags behind the code. Run it when you ship a new public class. Review the output. Commit it with the code change.

### Honest Caveat

XML docs generated from the implementation describe what the code does. They do not describe why the code exists. The design decision — why you chose this interface over an alternative, why the retry count is three and not five, why nullable is allowed here but not elsewhere — that context is still yours to write.

Check the `<exception>` elements especially. The model guesses from the method body it can see. It sometimes misses exceptions thrown by dependencies that are not visible in the method signature.

---

## 7.5 Practical: DevToolkit — One App, Four Workflows

Enough explanation. Let's actually build it.

### Project Setup

```
chapter-07/src/DevToolkit/
├── DevToolkit.csproj         — net10.0, MEAI + Azure + OpenAI + config
├── Program.cs                — menu loop, backend factory, workflow runners
├── PromptBuilder.cs          — fluent prompt builder (from Chapter 4, namespace DevToolkit)
├── OutputCleaner.cs          — shared JSON cleaner (CleanRawOutput pattern from Chapter 6)
├── DevToolkitOptions.cs      — per-team configuration record
├── CodeReviewService.cs      — code review: severity-tagged JSON findings
├── TestGenerationService.cs  — test generation: JSON test suite
├── CommitMessageService.cs   — commit messages + PR descriptions: conventional commits JSON
└── DocGenerationService.cs   — XML doc comments + README sections: plain text
```

### DevToolkit.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UserSecretsId>chapter-07-dev-toolkit</UserSecretsId>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.AI"                                  Version="*-*" />
    <PackageReference Include="Microsoft.Extensions.AI.OpenAI"                           Version="*-*" />
    <PackageReference Include="OpenAI"                                                   Version="*-*" />
    <PackageReference Include="Azure.AI.OpenAI"                                          Version="*-*" />
    <PackageReference Include="Microsoft.Extensions.Configuration"                       Version="*"   />
    <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets"           Version="*"   />
    <PackageReference Include="Microsoft.Extensions.Configuration.EnvironmentVariables"  Version="*"   />
  </ItemGroup>
</Project>
```

### Shared Infrastructure

**OutputCleaner** (extracted from the `CleanRawOutput` pattern in Chapter 6):

```csharp
internal static class OutputCleaner
{
    internal static string Clean(string raw)
    {
        var text = raw.Trim();
        if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            text = text["```json".Length..];
        else if (text.StartsWith("```"))
            text = text["```".Length..];
        if (text.EndsWith("```"))
            text = text[..^3];
        text = text.Trim();
        var jsonStart = text.IndexOf('{');
        if (jsonStart > 0) text = text[jsonStart..];
        var jsonEnd = text.LastIndexOf('}');
        if (jsonEnd >= 0 && jsonEnd < text.Length - 1) text = text[..(jsonEnd + 1)];
        return text;
    }
}
```

In Chapter 6, this logic lived inside `DocumentSummaryService`. Here it is extracted into a shared static class. Every service that expects JSON calls `OutputCleaner.Clean` before deserialization. The four failure modes it handles — fences, leading prose, trailing text, whitespace — appear across all three JSON workflows.

**DevToolkitOptions** — per-team customisation passed to all services:

```csharp
public sealed record DevToolkitOptions
{
    public string CodebaseContext { get; init; } = "C# codebase targeting modern .NET";
    public string TestFramework   { get; init; } = "xUnit with FluentAssertions";
    public string CommitFormat    { get; init; } = "conventional commits";

    public static readonly DevToolkitOptions Default = new();
}
```

Pass a customised instance to get more relevant output:

```csharp
var opts = new DevToolkitOptions
{
    CodebaseContext = ".NET 10, EF Core, CQRS, no mutable domain objects",
    TestFramework   = "xUnit with Moq and FluentAssertions",
    CommitFormat    = "conventional commits"
};
```

### Program.cs — The Menu Loop

```csharp
// CS1529 trap: Azure.AI.OpenAI using must appear at the file top,
// before any executable statements.
using Azure.AI.OpenAI;
using DevToolkit;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using System.ClientModel;
using System.Diagnostics;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

// Change Backend.LmStudio to Backend.Azure or Backend.OpenAI to switch providers.
// All four workflows share this single client.
IChatClient chatClient = CreateChatClient(Backend.LmStudio, config);

var opts = new DevToolkitOptions
{
    CodebaseContext = "C# codebase targeting .NET 10, nullable reference types enabled",
    TestFramework   = "xUnit with FluentAssertions",
    CommitFormat    = "conventional commits"
};

var reviewService = new CodeReviewService(chatClient, opts);
var testService   = new TestGenerationService(chatClient, opts);
var commitService = new CommitMessageService(chatClient, opts);
var docService    = new DocGenerationService(chatClient);

Console.WriteLine("=== Dev Toolkit ===");
Console.WriteLine();

while (true)
{
    Console.WriteLine("Pick a workflow:");
    Console.WriteLine("  1. Code review");
    Console.WriteLine("  2. Unit test generation");
    Console.WriteLine("  3. Commit message");
    Console.WriteLine("  4. Documentation (XML docs)");
    Console.WriteLine("  Q. Quit");
    Console.Write("> ");

    var choice = Console.ReadLine()?.Trim().ToUpperInvariant();
    Console.WriteLine();

    switch (choice)
    {
        case "1": await RunCodeReviewAsync(reviewService);    break;
        case "2": await RunTestGenerationAsync(testService);  break;
        case "3": await RunCommitMessageAsync(commitService); break;
        case "4": await RunDocGenerationAsync(docService);    break;
        case "Q": Console.WriteLine("Done. 👋"); return 0;
        default:  Console.WriteLine("Unknown option. Try 1, 2, 3, 4, or Q."); break;
    }
    Console.WriteLine();
}
```

The menu loop delegates to typed workflow handlers. Each handler reads multi-line input (terminated by a line containing `---`), calls the relevant service, and formats the output. The services handle streaming progress via `Console.Error.Write`, so the result display stays clean.

### Multi-line Input Handling

```csharp
static string ReadMultilineInput(string prompt)
{
    Console.WriteLine(prompt);
    Console.WriteLine("(Paste your content. Enter a line with only \"---\" to finish.)");
    var lines = new List<string>();
    while (true)
    {
        var line = Console.ReadLine();
        if (line is null || line.Trim() == "---")
            break;
        lines.Add(line);
    }
    return string.Join(Environment.NewLine, lines);
}
```

The `---` sentinel is pragmatic. It avoids EOF handling differences across terminals and shells. The user pastes, types `---`, presses Enter. Reliable on Windows, macOS, and Linux.

### The Backend Factory

```csharp
static IChatClient CreateChatClient(Backend backend, IConfiguration config) => backend switch
{
    Backend.LmStudio =>
        new OpenAIClient(
            new ApiKeyCredential("lm-studio"),
            new OpenAIClientOptions { Endpoint = new Uri("http://localhost:1234/v1") })
        .GetChatClient("microsoft/phi-4-mini-instruct")
        .AsIChatClient(),

    Backend.Azure =>
        new AzureOpenAIClient(
            new Uri(config["Azure:Endpoint"]
                    ?? throw new InvalidOperationException("Azure:Endpoint not configured.")),
            new ApiKeyCredential(config["Azure:Key"]
                    ?? throw new InvalidOperationException("Azure:Key not configured.")))
        .GetChatClient(config["Azure:Deployment"] ?? "gpt-4o-mini")
        .AsIChatClient(),

    Backend.OpenAI =>
        new OpenAIClient(
            new ApiKeyCredential(config["OpenAI:Key"]
                    ?? throw new InvalidOperationException("OpenAI:Key not configured.")))
        .GetChatClient(config["OpenAI:Model"] ?? "gpt-4o-mini")
        .AsIChatClient(),

    _ => throw new ArgumentOutOfRangeException(nameof(backend), backend, "Unsupported backend."),
};

enum Backend { LmStudio, Azure, OpenAI }
```

One factory, one switch. This is the same pattern from Chapter 6 — and it still composes cleanly here because every workflow is built on `IChatClient`. Add a backend by adding a `Backend` enum value and a switch arm. Everything else is unchanged.

### Running the DevToolkit

```
cd chapter-07/src/DevToolkit
dotnet run
```

The output:

~~~
=== Dev Toolkit ===
Powered by Microsoft.Extensions.AI — Chapter 7

Pick a workflow:
  1. Code review
  2. Unit test generation
  3. Commit message
  4. Documentation (XML docs)
  Q. Quit
>
~~~

Pick 1. Paste some C# code. Type `---` on its own line. The service streams to stderr while your output waits. Then get findings.

### What to Try Next

Three experiments that extend the DevToolkit into real workflow integration:

1. **Add a Git hook.** Create `.git/hooks/prepare-commit-msg`. When the developer's draft message is under 10 characters, call `GetStagedDiffAsync()`, run `CommitMessageService`, and pre-populate the commit message file with the generated subject. The developer reviews and accepts or edits. One line of friction removed.

2. **Add a test quality gate.** After generating tests with `TestGenerationService`, pass the generated test code through `CodeReviewService.ReviewAsync`. You are now running a code review on the generated tests. You will catch test methods with no assertions, setup that belongs in a constructor, and shared state between tests.

3. **Connect to CI.** On pull request open, get the changed files from the PR diff via the GitHub API, run `CodeReviewService` on each file, and post the findings as PR review comments. The setup takes about an hour. The payoff is consistent first-pass review on every PR, forever.

If your team already runs Roslyn analysers or SonarQube, this toolkit fits as a complementary layer — it catches what static analysis misses, without replacing it.

---

## 7.6 What Comes Next

This is where the book ends. Let me be direct about what that means.

The fundamentals are done. You have:

- A mental model of how prompts work and why structure matters (Chapters 1–4)
- Working C# patterns for `IChatClient`, streaming, and multi-backend configuration (Chapters 2–3)
- The `PromptBuilder` for reusable, structured prompt construction (Chapter 4)
- Structured output with defensive parsing and retry logic (Chapters 5–6)
- Service-level patterns for real developer workflows (this chapter)

That is not a toy foundation. Those are production-ready patterns. What you do not have yet is the next layer. Here is what that layer looks like.

### Tool Calling

MEAI supports function calling — you define a C# method as a tool, and the model can invoke it during a conversation. The model decides when to call it. You get back results from your own code as part of the response flow.

This enables workflows like: "Review this code, look up the design document for this service in our internal wiki, then give me findings in context." The model calls your wiki-lookup method. You did not hardcode the lookup into the prompt. The model chose to do it.

The MEAI programming model for tools: register a delegate as an `AIFunction`, add it to `ChatOptions.Tools`, handle `FunctionCallContent` and `FunctionResultContent` in the message loop.

### RAG — Retrieval-Augmented Generation

The prompts in this book inject context directly. That works at small scale. It breaks when your codebase has 50,000 files and your documentation is spread across 200 Confluence pages.

RAG is the standard answer. Index your codebase (or documentation, or database schema) into a vector store. At query time, retrieve the chunks most relevant to the current query. Inject only those chunks into the prompt. The model sees relevant context without you paying the token cost of the entire repository.

`Microsoft.Extensions.AI` has emerging interfaces for vector stores — `IEmbeddingGenerator<string, Embedding<float>>` and vector store connectors. The pattern is the same abstraction you already used: `IChatClient` for inference, vector store for retrieval, same configuration approach.

That is where most teams are heading today. RAG + tool calling covers about 80% of the real production AI use cases in enterprise .NET.

### Microsoft Agent Framework

Agents are models that plan and execute multi-step workflows autonomously. Give an agent a goal and a set of tools. It reasons about the steps, calls the tools, handles errors, and produces a result without you orchestrating each step.

Microsoft Agent Framework is the agent framework for .NET. The same `IChatClient` abstraction you used throughout this book is the foundation — it wraps it with a planning layer, tool registry, and memory.

### Model Context Protocol (MCP)

MCP is the emerging standard for connecting models to external tools and data sources. An MCP server exposes tools (functions the model can call) and resources (data the model can read). Any MCP-compatible client can connect to any MCP server.

In practice: your company can run an internal MCP server that exposes your proprietary APIs, internal databases, and documentation. Any model — GPT-4o, Phi-4, Mistral — can use them through a standard interface. The .NET MCP SDK ships as `ModelContextProtocol` on NuGet (`dotnet add package ModelContextProtocol`). Integration with MEAI is via that package — check the MEAI GitHub for current status as this is moving fast. I also have a talk I did on NDC Manchester that covers MCP and A2A if you are interested, you can watch it at my youtube channel located at https://www.youtube.com/@taswar

### Azure AI Foundry

For production deployments, Azure AI Foundry is where the operational tooling lives:

- **Model catalog** — GPT-4o, Phi-4, Mistral, Llama, and more, all through a consistent API
- **Prompt evaluation** — run your prompts against a labelled test dataset, measure precision and recall
- **PromptFlow** — visual pipeline editor for chaining model calls, tools, and retrieval steps
- **Cost management** — per-deployment token budget tracking and spending alerts
- **Fine-tuning** — adapt a base model to your domain's vocabulary and output format

The `AzureOpenAIClient` from Chapter 2 is already your entry point. The jump to Foundry is an endpoint and deployment name swap. The application code does not change.

### Recommended Next Reads

These are not a curated list for appearances. They are things I have found useful.

**For agents and tool calling in .NET:**

*Building AI Applications with Microsoft Semantic Kernel* — the natural continuation. Covers agents, planners, and the Semantic Kernel memory system. The `IChatClient` patterns from this book carry forward directly. (Note: that book uses "Semantic Kernel" for what this book calls Microsoft Agent Framework — it is the same SDK under the older brand name.)

**For broader AI fundamentals:**

*Microsoft GenAI for Beginners* — free on GitHub (github.com/microsoft/generative-ai-for-beginners). Written by Microsoft Cloud Advocates. Covers the conceptual foundation across languages and platforms. Good for teams where not everyone comes from a .NET background.

**For deeper prompt engineering reference:**

*DAIR.AI Prompt Engineering Guide* (promptingguide.ai) — the reference guide this book pointed toward throughout. Chain-of-thought, ReAct, self-consistency, retrieval-augmented — every technique in this book has a more complete treatment there. Keep it open when you are tuning a prompt that is not working.

**If vibe coding is not your thing:**

*GitHub Spec Kit* (github.com/github/spec-kit) — the antidote to "paste your requirements into a chat window and hope." Spec-Driven Development flips the usual workflow: you write a formal spec first, then generate the implementation from it. The spec becomes executable — it drives code generation rather than just describing it. Use the `specify` CLI to write a constitution (project principles), a spec (what to build and why), and a plan (technical approach). Then hand the structured spec to your coding agent instead of a vague prompt.

If the patterns in this chapter feel too ad-hoc — if you want your AI-assisted development to be more deliberate and traceable — Spec Kit is worth a look. It works with GitHub Copilot, Claude Code, Gemini CLI, and Codex CLI.

---

### Closing

You now have the fundamentals. Real patterns, working C# code, honest caveats. The things that actually show up in production — not the things that look good in a demo.

What comes next is more of the same: a bigger problem space, more moving parts, the same principles applied at scale. Tool calling, RAG, agents — all of them are built on what you already know. The `IChatClient` abstraction from Chapter 2 is still the right place to start. The `PromptBuilder` from Chapter 4 still works. The defensive parsing from Chapter 6 still matters. The scale changes. The principles do not.

Start with one pattern from this chapter. Put it into your actual workflow — not a side project, your actual workflow. See what breaks. Fix it. That is how it works in practice. 🙂

---

## Chapter Summary

| Concept | The short version |
|---|---|
| Code review pattern | `CodeReviewFinding` with `location`, `issue`, `severity`, `fix`. Temperature `0f`. Use `PromptBuilder` with a one-shot SQL injection example. |
| Test generation pattern | `TestSuiteResult` with `TestMethod.Code` as a C# string. Style-match with `existingTestStyle`. Review for independence. |
| Commit message pattern | Feed the diff, get conventional commit JSON. The model sees what changed, not why. Always review the subject line. |
| Documentation pattern | XML docs as plain text — no JSON needed. `<summary>` says what, not how. The why is still yours to write. |
| `$$"""` vs `$"""` | Use `$$"""` when the raw string contains JSON schema with literal `{` `}` characters. Single braces become literal; interpolations become `{{expr}}`. |
| DevToolkit | One `IChatClient`, four services, one menu loop. Customise via `DevToolkitOptions`. Copy, extend, integrate into your actual workflow. |
| `OutputCleaner` | Strip fences, find `{`, trim after `}`. Every service that expects JSON needs it. Extracted from the Chapter 6 `CleanRawOutput` pattern. |
| Temperature choices | `0f` for review and commit messages (deterministic). `0.1f`–`0.2f` for test and doc generation (slight variation improves breadth). |
| What comes next | Tool calling → RAG → Agents → MCP → Azure AI Foundry. Same `IChatClient` abstraction, bigger problem space. |

---

*← [Chapter 6 — Structured Outputs](../chapter-06/chapter-06-structured-outputs.md) | End of book*

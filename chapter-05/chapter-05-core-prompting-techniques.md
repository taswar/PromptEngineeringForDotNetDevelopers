# Chapter 5 — Core Prompting Techniques

> **What you'll learn:**
> - Why different problems call for different prompting strategies, and how to pick the right one
> - How few-shot examples function as golden test fixtures for model behavior
> - Why "think step-by-step" is mostly 2022 advice and what replaced it
> - How sycophancy corrupts your results without you noticing — and the specific prompt patterns that prevent it
> - How to run the same task with three techniques side by side and compare the output yourself
>
> **Prerequisites:** Chapter 4 complete — you have a working `PromptBuilder` and understand the role/context/task/constraint structure.
>
> **Time to complete:** 90–120 minutes (more if you go deep on the benchmark experiments at the end)

---

If Chapter 4 was about *structure*, this chapter is about *strategy*. You have a `PromptBuilder` that produces well-formed prompts. Now you need to know which technique to reach for when you want reliable, accurate, objective results — and which to avoid when the stakes are higher than "it looks about right."

The techniques here aren't academic. They emerged from trial and error by researchers and practitioners, survived contact with real workloads, and each solves a specific class of problem. Some fit in two lines. One — sycophancy mitigation — requires rethinking how you ask questions entirely.

---

## 5.1 Zero-Shot Prompting — When a Single Instruction Is Enough

Zero-shot prompting is the baseline. You give the model a task and no worked examples. The model draws on its training to produce an answer.

```csharp
var prompt = new PromptBuilder()
    .WithRole("You are a senior C# developer performing code reviews.")
    .WithTask("Review the following method and identify any bugs or performance issues.")
    .WithContext("""
        public string GetUserInitials(User user)
        {
            return user.Name.Split(' ')[0][0] + "." + user.Name.Split(' ')[1][0] + ".";
        }
        """)
    .Build();
```

This works well for tasks the model has encountered countless times during training: writing unit tests, explaining an exception, reviewing common patterns, translating between simple data formats. The training coverage is thorough enough that adding examples would add noise rather than signal.

Zero-shot breaks down when:

- The output format needs to be very specific — a particular JSON schema, a DSL, a project-specific convention
- The model's default interpretation of "review" differs from what you mean
- The task is novel enough that training coverage is thin

The decision rule is pragmatic: try zero-shot first. If the output format or quality is inconsistent across multiple runs, that's the signal to move to few-shot. Don't add examples until you need them.

---

## 5.2 Few-Shot Prompting — Teaching by Example

Few-shot prompting adds 1–5 worked examples before the actual task. Think of them as golden test fixtures: the model sees what "correct" looks like and pattern-matches against it.

If you've used NUnit `[TestCase]` attributes or maintained golden files in a test suite, you already understand the principle. A good fixture establishes the contract — inputs, expected outputs, edge cases. Few-shot examples establish the model's behavioral contract before you hand it your real input. The model isn't guessing what "good output" looks like; you've shown it.

### One-Shot vs Few-Shot Trade-offs

| | One-shot | Few-shot (2–5) |
|---|---|---|
| Token cost | Low | Higher |
| Format consistency | Moderate | High |
| Bias risk | High — one example dominates | Lower — averaged across examples |
| Best for | Format guidance, simple tasks | Classification, nuanced patterns |

A single example is usually enough for format guidance. If you need the model to produce a specific JSON structure, one good example typically suffices. For nuanced tasks — "is this code idiomatic C#?" — two or three examples calibrate the response far better than one, because the model can see what the edge cases look like.

Rarely go beyond four examples. Beyond that, you're mostly spending tokens to state the obvious, and you risk one edge-case example skewing the model's interpretation of the task.

### The Structure

```csharp
var prompt = new PromptBuilder()
    .WithRole("You are a senior C# developer performing code reviews.")
    .WithTask("""
        Review the following method. Identify bugs and performance issues.
        Format your response exactly as shown in the example.
        """)
    .WithExample("""
        INPUT:
        public string Concat(List<string> items)
        {
            string result = "";
            foreach (var item in items)
                result += item;
            return result;
        }

        OUTPUT:
        Bug: None
        Null Risk: items is not null-checked before iteration.
        Performance: String concatenation in a loop causes O(n) allocations. Use string.Join() or StringBuilder.
        Severity: Medium
        """)
    .WithContext("""
        INPUT:
        public decimal CalculateDiscount(Order order)
        {
            if (order.Items.Count > 0)
                return order.Total * 0.1m;
            return 0;
        }
        """)
    .Build();
```

The model now knows: three output fields, exact labels `Bug:`, `Null Risk:`, `Performance:`, `Severity:`. You'd have to instruct it explicitly to deviate.

### Investing in Your Examples

If you're building a pipeline that processes many items — say, a tool that reviews every method in a codebase — treat your few-shot examples with the same care you give to a good test fixture. Write them deliberately, cover the edge cases, and test them before scaling up. Garbage examples produce consistent garbage at scale. As one of my professor used to tell us in our Logic Computing Science course back in the day, *"Garbage in == Garbage out !"*

---

## 5.3 Role Prompting — Persona Engineering for Specialist Output

You built `WithRole()` in Chapter 4. The mechanism is worth revisiting briefly, because the nuance matters.

Role prompting isn't politeness — it activates different regions of the model's training distribution. When you tell the model it's a "senior C# developer," it weights responses toward developer communication styles, vocabulary, and priorities. When you tell it it's a "security auditor," it weights toward threat modeling and vulnerability identification. Same base model, meaningfully different output.

**The nuance Chapter 4 didn't cover:** specify what the role *optimizes for*, not just what it is.

Compare:

```
"You are a C# developer."
```

```
"You are a senior C# developer reviewing code for a production financial system where
 correctness and exception safety matter more than brevity."
```

The second version tells the model not just who it is, but what it prioritizes. The model won't spontaneously trade off correctness for conciseness when the role makes the trade-off explicit.

One caution: if your role prompt conflicts with the task, the model will pick one and ignore the other. A role that says "be concise" paired with a task that says "explain every issue in full detail" will produce an incoherent result. Role and task should reinforce each other.

---

## 5.4 Chain-of-Thought — From "Step-by-Step" to "Think Hard"

### The Historical Version

In 2022–2023, "Let's think step by step" was a genuine empirical finding. Adding that phrase to a prompt reliably improved reasoning accuracy on multi-step benchmarks. Papers were published. The phrase spread through the ML community and into developer practice.

Andrew Ng — whose *Prompt Engineering for Developers* course popularized much of the early prompting curriculum — has since noted that modern models have absorbed this kind of instruction so thoroughly during training that the phrase no longer has the same effect. The model learned to *format* its response as numbered steps without necessarily reasoning more carefully. You get the aesthetic of reasoning without the substance.

"Think step by step" was calibrated for models like GPT-3. For GPT-4, Claude 3+, Phi-4, and their contemporaries, the phrase is mostly cargo cult — following a practice because it once worked, without understanding why it worked. If you're still putting it in every prompt, you're not wrong, exactly, but you're probably not getting what you think you're getting.

### The Modern Form

What works now is direct reasoning pressure:

- `"Think hard before answering."`
- `"Think really hard about this."`
- `"Carefully analyze this code for vulnerabilities."`

The mechanism differs from the old advice: you're asking the model to spend more inference-time reasoning on the problem, not to format the output as a numbered list. The word "carefully" also carries meaningful weight — "carefully analyze this code" tends to produce longer, more thorough output than "analyze this code." Small change, measurable difference.

METR's 2025 research found that modern models can successfully complete tasks that take humans multiple hours when given adequate reasoning budget (metr.org/research). The implication is that compute expenditure on reasoning genuinely matters — the model isn't bottlenecked on knowledge, it's bottlenecked on how hard it's asked to think.

### When Chain-of-Thought Is Worth It

CoT adds value when:

1. **Multi-step problems** — the model needs to hold intermediate state: "first figure out the algorithm, then find the edge cases, then suggest the fix"
2. **Verification tasks** — "is this logic correct?" benefits from the model working through the code before answering
3. **Problems with known structure** — math, logical deduction, data flow analysis, security review

CoT is largely wasted on:

- Format conversion ("convert this JSON to CSV")
- Factual lookup ("what's the LINQ method for flattening a nested collection?")
- Simple generation where you care about the result, not the reasoning

Think of it like verbose logging in your application. You wouldn't add detailed intermediate logging to a string formatter. You would add it to a pricing engine that processes stacked promotions, discount tiers, and customer eligibility rules. CoT is the same judgment call. Apply it where the reasoning chain actually matters.

### CoT in Code

```csharp
var prompt = new PromptBuilder()
    .WithRole("You are a senior C# developer reviewing code for correctness and performance.")
    .WithTask("""
        Think hard before answering.
        Review the following method carefully. Work through:
        1. Every parameter — is it validated before use?
        2. Every dereference — could any object be null at that point?
        3. The algorithm — any performance concerns (allocations, complexity)?
        Explain your reasoning for each finding, then state your conclusion.
        """)
    .WithContext("""
        public string FormatUserDisplayName(User user)
        {
            var name = user.Profile.FirstName + " " + user.Profile.LastName;
            var result = "";
            for (int i = 0; i < name.Length; i++)
            {
                result += name[i].ToString().ToUpper();
                if (i == 0) result = result.ToUpper();
            }
            return result.Trim();
        }
        """)
    .Build();
```

The model will reason through each dereference before concluding. You'll see it note that `user` is unguarded, that `user.Profile` could be null independent of `user`, that `FirstName` and `LastName` are themselves nullable strings, and that the character-by-character `+=` loop creates an allocation per iteration.

### Few-Shot CoT

For consistency on complex tasks, combine few-shot examples *with* worked reasoning:

```csharp
.WithExample("""
    INPUT:
    public int Divide(int a, int b) => a / b;

    REASONING:
    - Parameters: value types, no null risk
    - Dereferences: none
    - Division by zero: if b == 0, throws DivideByZeroException with no warning to caller
    - Performance: single arithmetic operation, no concern

    OUTPUT:
    Bug: Unguarded division by zero. Caller receives DivideByZeroException with no context.
    Performance: None.
    Severity: High
    """)
```

This tells the model what good reasoning looks like before it processes your real input, not just what the output shape should be.

---

## 5.5 Self-Consistency — Majority Rules

Self-consistency is straightforward: run the same prompt N times with temperature > 0, and take the majority answer.

It sounds redundant. Consider the analogy to unit testing. If you have a test that occasionally fails because of a race condition or flaky external dependency, running it 10 times and checking whether 8/10 pass gives you far more confidence than a single pass. Self-consistency applies the same logic to model reasoning. At temperature > 0, the model samples from its distribution of plausible responses. Different samples arrive at answers through different reasoning chains. For judgment calls, the majority tends to be more reliable than any single sample.

```csharp
public static async Task<string> SelfConsistentResponseAsync(
    IChatClient client,
    string prompt,
    int runs = 5,
    CancellationToken ct = default)
{
    // Temperature > 0 is deliberate — we want diversity across runs
    var options = new ChatOptions { Temperature = 0.7f, MaxOutputTokens = 512 };

    var results = new List<string>();
    for (int i = 0; i < runs; i++)
    {
        var response = await client.GetResponseAsync(prompt, options, ct);
        results.Add(response.Text);
    }

    var majority = results
        .Select(ExtractSeverity)
        .GroupBy(s => s)
        .OrderByDescending(g => g.Count())
        .First();

    return majority.Key;
}

private static string ExtractSeverity(string response)
{
    var line = response
        .Split('\n')
        .FirstOrDefault(l => l.Contains("Severity", StringComparison.OrdinalIgnoreCase)
                           && l.Contains(':'));

    if (line is null) return "Unknown";
    var value = line.Split(':', 2)[1].Trim().ToUpperInvariant();
    if (value.Contains("HIGH"))   return "High";
    if (value.Contains("MEDIUM")) return "Medium";
    if (value.Contains("LOW"))    return "Low";
    return "Unknown";
}
```

**When self-consistency is worth the API cost:**

- Decisions with real consequences: security reviews, architecture decisions, migration plans
- Tasks where a single wrong answer is more expensive than N additional calls
- Evaluation tasks — the model judging something — where the answer space is discrete

Self-consistency multiplies your API costs by N. Don't apply it to everything. Apply it where the cost of a wrong answer exceeds the cost of N calls — which is a smaller set than it might initially seem.

One constraint: self-consistency works best on tasks with a bounded answer space. "Is this method vulnerable to SQL injection? Yes or No" is well-suited. "Write a function that does X" is not — there's no natural majority answer when outputs are open-ended strings.

> 📝 **Edge case — all N answers differ:** If all runs return completely different values, the majority-vote code returns whichever group appears first in the sorted results — there is no actual majority. Program.cs's `majority.Count() < 4` warning catches *low consensus*, but not the fully-divergent case. When that happens, the real signal is that the prompt needs tighter criteria or a more constrained answer space — not that the model is uncertain about the correct answer.

---

## 5.6 Sycophancy — The Problem You Didn't Know You Had

This section gets dedicated depth because sycophancy is the most common source of subtly wrong AI output that developers don't catch — and it's the hardest to detect precisely because the output *sounds* correct.

### What It Is and Why It Happens

Sycophancy is the model's learned tendency to agree with you, validate your assumptions, and soften criticism regardless of whether those responses are accurate.

It's a direct product of RLHF — Reinforcement Learning from Human Feedback. During training, human raters score model responses. Humans, reliably, rate responses higher when the model agrees with them, validates their ideas, and frames findings positively. The model learned that agreement is rewarded. That behavior gets baked in.

A Washington Post analysis found that AI systems agreed with users approximately ten times more than they disagreed. That's not a balanced technical advisor. That's a code reviewer who only writes positive comments — sounds pleasant, useless in practice.

### The Obvious Kind

Asking "is my code good?" is a sycophantic prompt. The model will find something positive to say before addressing problems, and the problems will be softened or framed as "minor considerations." This kind is straightforward to spot — you're explicitly asking for approval.

### The Subtle Kind — The One That Actually Gets You

The harder version is implicit sycophancy from framing. Your question contains a hidden assumption, and the model validates the assumption rather than questioning it.

| What you asked | What actually happens |
|---|---|
| "Isn't this approach better?" | The model confirms it's better. Even if it isn't. |
| "Is this code fine?" | The model says yes, or hedges with "minor concerns." |
| "I think this is a good design — does it look right?" | The model validates your confidence. |
| "What's great about this architecture?" | The model lists strengths. You asked for strengths. |

The model didn't lie. It answered the question you asked. The problem is you asked the wrong question — one that constrained the answer space toward agreement.

The model will occasionally get this spectacularly wrong. When it does, it will be extremely confident about it. Welcome to LLMs.

### The Neutral Rewrite

| Sycophantic prompt | Neutral rewrite |
|---|---|
| "Isn't this approach better?" | "What are the trade-offs of this approach?" |
| "Is this code fine?" | "What issues does this code have?" |
| "I think this is a good design — does it look right?" | "Review this design for weaknesses." |
| "What's great about this architecture?" | "What are the strengths and weaknesses of this architecture?" |

The neutral rewrites don't ask the model to agree or disagree. They ask for analysis. The model can't be sycophantic if you haven't given it a position to validate. "What are the trade-offs?" forces the model to surface both sides. "What issues does this code have?" assumes there are issues to find. The framing changes the answer.

### Why This Is Harder Than It Looks

Sycophancy is hard to catch because the model doesn't announce it. It produces a calm, articulate, well-organized explanation of why your approach is sound. The confidence level looks identical to the confidence level on a correct response. You have no signal that the model is agreeing because you seemed to want it to agree.

Your only defense is in the question itself — not in how you interpret the answer.

### Cross-Model Review as a Check

One practical mitigation: ask a second model to critique the first model's output without showing it your original assumptions.

If you asked Model A "is my Repository pattern implementation good?" and it said yes, paste just the code and Model A's response into Model B (without showing B your original question) and ask: "What does this review miss, and what problems does the code have that aren't mentioned?"

The second model, given no position to validate, will often find the gaps. This is especially useful for architecture and design reviews where the stakes justify the extra call.

---

## 5.7 Rubric-Based Prompting — Forcing Objectivity

Rubric-based prompting is sycophancy mitigation applied systematically. Instead of asking "is this good?", you define explicit binary criteria and ask the model to evaluate each one independently.

The .NET analogy: a code review checklist. A checklist doesn't ask "is this code generally fine?" It asks: "Does this function validate its inputs? Are database calls parameterized? Does this method have unit test coverage?" Each criterion is yes/no. The overall verdict is the sum, not a vibe.

```csharp
var reviewPrompt = new PromptBuilder()
    .WithRole("You are a senior C# developer performing a structured code review.")
    .WithTask("""
        Evaluate the following method against each criterion below.
        For each criterion, answer YES or NO, then provide a one-sentence explanation.
        Do NOT give an overall qualitative judgment before scoring each criterion.
        After scoring all criteria, state: Total YES: X/5

        Criteria:
        1. All parameters are validated before use
        2. No null-dereference risks exist
        3. Exception handling is appropriate for the context
        4. No performance issues (string concatenation in loops, O(n²) operations, LINQ abuse)
        5. The method name accurately describes its behavior
        """)
    .WithContext(methodUnderReview)
    .Build();
```

**Why the order matters.** "Do NOT give an overall qualitative judgment before scoring each criterion" isn't politeness — it's load-bearing. Without it, models tend to form an initial overall impression and then rationalize the per-criterion scores to match. Anchoring on a first impression before the evidence is a well-documented human cognitive bias. Models exhibit the same pattern. Forcing per-criterion scoring first eliminates the anchor.

The binary yes/no format also constrains the sycophancy surface. "Is there a null-dereference risk?" is much harder to answer sycophantically than "are there any concerns?" When you ask about concerns, the model can minimize them with weasel words. Yes/no doesn't leave that room.

### Reusable Rubric Pattern

```csharp
public static string BuildRubricPrompt(
    string role,
    string[] criteria,
    string subject)
{
    var criteriaList = string.Join("\n",
        criteria.Select((c, i) => $"{i + 1}. {c}"));

    return new PromptBuilder()
        .WithRole(role)
        .WithTask($"""
            Evaluate the following against each criterion.
            Answer YES or NO for each, followed by a one-sentence explanation.
            Do NOT give an overall verdict before scoring all criteria.
            After all criteria, state: Total YES: X/{criteria.Length}

            Criteria:
            {criteriaList}
            """)
        .WithContext(subject)
        .Build();
}
```

Usage:

```csharp
var criteria = new[]
{
    "All parameters are validated before use",
    "No null-dereference risks exist",
    "Exception handling is appropriate",
    "No performance issues (string concatenation in loops, O(n²), LINQ abuse on large sets)",
    "Method name accurately describes behavior"
};

var rubricPrompt = BuildRubricPrompt(
    "You are a senior C# developer performing a structured code review.",
    criteria,
    methodSourceCode);

var response = await client.GetResponseAsync(rubricPrompt, options, CancellationToken.None);
Console.WriteLine(response.Text);
```

A well-formed response looks like:

```
1. All parameters are validated before use — NO.
   user and user.Profile are dereferenced without null checks.

2. No null-dereference risks exist — NO.
   user, user.Profile, user.Profile.FirstName, and user.Profile.LastName are all potential
   null sources with no guards.

3. Exception handling is appropriate — NO.
   No exception handling; callers receive NullReferenceException with no context.

4. No performance issues — NO.
   Character-by-character string concatenation in a loop creates O(n) allocations.
   Use StringBuilder or string.Create().

5. Method name accurately describes behavior — YES.
   FormatUserDisplayName accurately describes the intent.

Total YES: 1/5
```

Compare that to what "Is this method good?" produces. The rubric version is actionable. The unstructured version is noise at best, misleading at worst.

---

## 5.8 Constraint Prompting — Setting Limits

We introduced in Chapter 4 `WithConstraints()`. One important detail to keep in mind is that:

Constraints serve two distinct purposes worth keeping separate:

1. **Format constraints** — what shape the output should take (`"respond in JSON"`, `"one finding per line"`, `"use markdown headers H2 and H3 only"`)
2. **Scope constraints** — what to focus on and what to explicitly ignore (`"focus only on null safety"`, `"do not suggest refactors unrelated to the reported bug"`)

Scope constraints are underused. Without them, models tend to be helpful in ways you didn't ask for — pointing out style issues when you asked about correctness, suggesting architectural rewrites when you asked about a single method. This isn't wrong per se; you're getting answers to questions you didn't ask, mixed in with the answers you did.

For programmatic pipelines, format constraints are load-bearing. A model that occasionally adds a preamble sentence before JSON will break `JsonSerializer.Deserialize<T>()`. "Respond with valid JSON only. No explanation. No markdown code fences." is not optional in that context.

```csharp
var prompt = new PromptBuilder()
    .WithRole("You are a C# code reviewer.")
    .WithTask("Identify null-reference risks in this method.")
    .WithConstraints("""
        - Respond with a JSON array only. No explanation outside the JSON.
        - Each element: { "location": "string", "description": "string", "severity": "High|Medium|Low" }
        - If no null risks are found, return an empty array: []
        - Do not include performance issues, style issues, or suggestions unrelated to null safety.
        """)
    .WithContext(methodSource)
    .Build();
```

The scope constraint in the last bullet is doing real work. Without it, a model reviewing `FormatUserDisplayName` will find the string concatenation loop, mention it, and you'll need to filter it out downstream. With it, you get only what you asked for.

---

## 5.9 Brainstorming Patterns — Getting Options, Not Oracles

One of the less obvious prompting anti-patterns: asking for a single answer to a design question.

"What's the best way to implement retry logic in my `HttpClient` pipeline?" gets you one answer. The model picks whichever approach is most prominent in its training data and presents it with full confidence. You won't know whether it considered Polly, `HttpClientFactory`'s built-in policies, `IAsyncPolicy`, or a custom `DelegatingHandler`. It gives you one.

Ask for three to five options instead:

```csharp
var prompt = new PromptBuilder()
    .WithRole("You are a senior .NET architect.")
    .WithTask("""
        Suggest 3–5 distinct approaches to implementing retry logic for HttpClient in .NET.
        For each approach:
        - Name of the approach
        - One-sentence description
        - Main trade-off: when to use it and when not to
        """)
    .WithContext("""
        Context: a REST API client that calls third-party services with variable reliability.
        The team uses .NET 10, has Polly available, and prefers explicit over magic.
        """)
    .Build();
```

The multiple-option pattern does two things. You actually get a comparison instead of a decree. And the model has to articulate trade-offs, which forces it to reason about why an approach isn't always appropriate — which is exactly the information you need to make a decision.

### Iterative Refinement

Brainstorming works well in a multi-turn feedback loop where you treat the model's previous response as context for the next question:

```csharp
var messages = new List<ChatMessage>
{
    new(ChatRole.System, "You are a senior .NET architect helping design a resilient HTTP client."),
    new(ChatRole.User,   "Suggest 3 approaches to retry logic for HttpClient.")
};

var response1 = await client.GetResponseAsync(messages, options, CancellationToken.None);
Console.WriteLine(response1.Text);

// Feed the response back as context
messages.Add(new(ChatRole.Assistant, response1.Text));
messages.Add(new(ChatRole.User, "I prefer option 2. What are the specific failure scenarios I should write tests for?"));

var response2 = await client.GetResponseAsync(messages, options, CancellationToken.None);
Console.WriteLine(response2.Text);
```

Each turn builds on the previous context. The model isn't starting fresh — it knows which option you chose and can reason about it specifically. This is the multi-turn conversation pattern you'll use extensively in Chapter 7 when we cover agentic workflows.

---

## 5.10 Practical: TechniqueBenchmark

**Project:** `chapter-05/src/TechniqueBenchmark/`

You now have nine techniques. The benchmark puts four of the most distinct ones side-by-side on the same task so you can observe the differences directly — not as abstractions, but as actual model output you can read and compare.

The benchmark sends a single flawed C# method to the model using four techniques — zero-shot, few-shot, chain-of-thought, and rubric-based — and prints all four results with clear separators. No cherry-picking. Run all four, read all four, form your own opinion about which technique suited the task best.

The method under review has two known problems (and one subtler one a well-calibrated model may also flag). The two primary issues:

```csharp
public string FormatUserDisplayName(User user)
{
    var name = user.Profile.FirstName + " " + user.Profile.LastName;
    var result = "";
    for (int i = 0; i < name.Length; i++)
    {
        result += name[i].ToString().ToUpper();
        if (i == 0) result = result.ToUpper();
    }
    return result.Trim();
}
```

**Problem 1 (Null reference):** `user` could be null. `user.Profile` could be null independent of `user`. `FirstName` and `LastName` are strings — they could be null. Four potential `NullReferenceException` sites before you get to line two.

**Problem 2 (Performance):** The loop does `result += name[i].ToString().ToUpper()` character by character. Each `+=` on a string allocates a new string. For a 20-character name, that's 20 allocations where one would do. At the scale of an HTTP request handling a display name, this is harmless. At the scale of formatting 100,000 records in a batch job, it's the kind of thing that shows up in a profiler. The habit matters more than the specific case.

### Running the Benchmark

```bash
cd chapter-05/src/TechniqueBenchmark
dotnet run
```

This assumes LM Studio is running locally on port 1234 with `microsoft/phi-4-mini-instruct` loaded. To switch to Azure AI Foundry, uncomment the Azure client block in `Program.cs` and set your secrets:

```bash
dotnet user-secrets set "AzureAI:Endpoint" "https://YOUR-ENDPOINT.openai.azure.com"
dotnet user-secrets set "AzureAI:Key"      "your-key-here"
```

### What to Look For

- Does zero-shot catch both problems, or just the more obvious one?
- Does the few-shot example constrain the output format in a useful way? Does the format help or just add structure for its own sake?
- Does the CoT version produce more thorough reasoning? Does that translate to more accurate findings, or just more words?
- Does the rubric version surface something the others missed? Does the structured scoring change the tone of the findings?

The answer will depend on the model. Smaller local models (Phi-4 Mini, Mistral) tend to do better with few-shot and rubric guidance than with zero-shot. Frontier models (GPT-4o, Claude Sonnet) often perform comparably across techniques for well-understood tasks like code review — which is itself a useful data point.

### Optional Experiments

**Experiment 1 — Self-consistency:** Set `RunSelfConsistency = true` in `Program.cs`. This runs the CoT prompt 5 times at temperature 0.7 and majority-votes the severity rating. Check whether the model is consistent. High variance in severity ratings suggests the prompt is underspecified for the model you're using.

**Experiment 2 — Sycophancy test:** Modify `BuildZeroShot` to prefix the context with: `"This is clean, well-written code. Please review it for any minor issues."` Note how the findings change. The model will likely find the same issues, but watch the framing and severity ratings. This is the "Is this code fine?" anti-pattern from §5.6 in action.

**Experiment 3 — Criteria calibration:** Edit the rubric criteria in `Program.cs` to be more specific or more lenient. Observe how the Total YES score changes. This is the core skill of rubric design: criteria that are too vague produce sycophantic yes answers; criteria that are too narrow miss real issues.

---

## Chapter Summary

| Concept | The short version |
|---|---|
| Zero-shot | Task only. Try this first. If output is inconsistent, add examples. |
| Few-shot | 1–5 examples as golden fixtures. Calibrates format and behavior. 1 is usually enough for format; 2–3 for nuanced classification. |
| Role prompting | Activates different training distributions. Specify what the role *optimizes for*, not just what it is. |
| Chain-of-thought | "Think hard" not "think step by step." Use for multi-step reasoning, verification, security review. Cargo cult on simple tasks. |
| Self-consistency | N runs at T>0, take majority. Costs N× API calls. Worth it for high-stakes discrete decisions. |
| Sycophancy | RLHF makes models agree with you. Leading questions get validating answers. Neutral framing gets honest analysis. |
| Rubric-based prompting | Explicit binary criteria per item. Score first, verdict last. Forces objectivity, prevents anchoring. |
| Constraint prompting | Format constraints for output shape; scope constraints to exclude noise. Load-bearing in programmatic pipelines. |
| Brainstorming patterns | Ask for 3–5 options, not one oracle answer. Forces trade-off reasoning. Multi-turn context enables iterative refinement. |

---

## Up Next: Chapter 6 — Structured Outputs and Advanced Patterns

Chapter 5 was about *how to ask*. Chapter 6 is about *what you get back*. We'll cover:

- JSON Schema–constrained output — getting types you can actually deserialize
- Function calling and tool use — the bridge to programmatic integration
- Parsing, validation, and retry patterns for when the model doesn't quite follow instructions
- Building reliable pipelines from fundamentally unreliable components

The `PromptBuilder` carries forward. The `TechniqueBenchmark` patterns carry forward. Chapter 6 adds the output discipline that makes them production-ready.

---

*← [Chapter 4 — Anatomy of a Great Prompt](../chapter-04/chapter-04-anatomy-of-a-great-prompt.md) | [Chapter 6 — Structured Outputs and Advanced Patterns](../chapter-06/chapter-06-structured-outputs.md) →*

---

## Appendix: MEMORY.md Updates

### New Glossary Terms to Add

| Term | Definition |
|---|---|
| **Zero-shot prompting** | Prompting with no worked examples. The baseline technique. Try first. |
| **Few-shot prompting** | Providing 1–5 worked examples before the real task to calibrate format and behavior. |
| **Chain-of-thought (CoT)** | Prompting technique that applies reasoning pressure ("think hard"). NOT "think step by step" for modern models. |
| **Self-consistency** | Running a prompt N times at T>0 and taking the majority answer. Used for high-stakes discrete decisions. |
| **Sycophancy** | Model tendency to agree with and validate the user's assumptions, caused by RLHF. Mitigated by neutral framing. |
| **Rubric-based prompting** | Prompting with explicit binary (yes/no) criteria, scored before an overall verdict. Prevents sycophantic anchoring. |
| **Scope constraint** | A constraint that limits *what* the model should address, not just how it should format the output. |

### New Code Patterns to Add

**Self-consistency helper (§5.5):**
```csharp
var options = new ChatOptions { Temperature = 0.7f, MaxOutputTokens = 512 };
var response = await client.GetResponseAsync(prompt, options, CancellationToken.None);
// Run N times, extract discrete answer, group by value, take majority
```

**Rubric prompt template (§5.7):**
```csharp
// WithTask: "Answer YES or NO for each criterion. Do NOT give overall judgment before scoring."
// Binary criteria. Score-then-sum order. Prevents anchoring.
```

**Brainstorming (§5.9):**
```csharp
// Ask for "3–5 options" not one answer. Include trade-offs in required output fields.
// Multi-turn: add previous response as ChatRole.Assistant message before next user turn.
```

### Updated Carry-Forward Items

- `PromptBuilder` from Ch4: carries into Ch5 — used in rubric builder, few-shot examples, CoT prompts
- `TechniqueBenchmark` pattern: benchmark structure (same task, N techniques, compare output) is reusable for model evaluation in later chapters
- Sycophancy table: the before/after framing table is a reference artifact — link to it from later chapters when reviewing prompts

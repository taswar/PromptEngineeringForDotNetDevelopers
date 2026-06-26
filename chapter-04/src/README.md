# Chapter 4 — PromptBuilder: Code Review Assistant

A console app that demonstrates the `PromptBuilder` class from Chapter 4. Paste any C# method and get a structured code review back from the model.

## Prerequisites

- .NET 10 SDK
- One of: LM Studio running locally, an OpenAI API key, or Azure AI Foundry credentials

## Setup

### Option A — LM Studio (recommended, free)

1. Install [LM Studio](https://lmstudio.ai) and download a model (Phi-4 Mini works well)
2. Start the local server (default port: 1234)
3. Check the exact model id: `GET http://localhost:1234/v1/models`
4. Update `Program.cs` with the correct model id
5. Run:

```bash
dotnet run
```

### Option B — OpenAI API

```bash
dotnet user-secrets set "OPENAI_API_KEY" "sk-..."
```

Uncomment Option B in `Program.cs`, comment out Option A, then `dotnet run`.

### Option C — Azure AI Foundry

```bash
dotnet user-secrets set "AZURE_OPENAI_ENDPOINT" "https://your-resource.openai.azure.com/"
dotnet user-secrets set "AZURE_OPENAI_KEY" "your-key"
```

Uncomment Option C in `Program.cs`, comment out Option A, add `using Azure.AI.OpenAI;`, then `dotnet run`.

---

## Expected output

For the default `GetUserFullName(User? user)` input:

```
Reviewing code...
------------------------------------------------------------
public string GetUserFullName(User? user) 
{
    return user.FirstName + " " + user.LastName;
}
------------------------------------------------------------

Review:
1. Null dereference risk: 'user' is nullable but accessed without a null check —
   if user is null, this throws NullReferenceException. Fix: add
   ArgumentNullException.ThrowIfNull(user) or return early on null.
2. Null property access: 'user.FirstName' and 'user.LastName' may be null if the
   User class doesn't guarantee non-null properties — string concatenation silently
   produces "null null". Fix: use null-coalescing operators or ensure properties
   have non-nullable defaults.
```

---

## What to try next

**1. Test the no-issues case**

Replace `codeToReview` in `Program.cs` with a method that has no problems:

```csharp
var codeToReview = """
    public static bool IsValidEmail(string? email) =>
        !string.IsNullOrWhiteSpace(email) && email.Contains('@');
    """;
```

The model should respond with exactly: `No issues found.`

**2. Add a `WithExample` call**

Add this to the `PromptBuilder` chain in `Program.cs`:

```csharp
.WithExample(
    input: "public string GetName() => name;",
    output: "1. Null reference risk: 'name' field may be null — return type should be string? or add a null check.")
```

Run the same input 5 times. Compare output consistency vs. without the example.

**3. Observe temperature variance**

Change `Temperature = 0f` to `Temperature = 0.7f` and run 3 times. The issues found should be the same; the phrasing will vary. This demonstrates why structured output tasks use `Temperature = 0`.

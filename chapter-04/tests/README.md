# Chapter 4 — Tests

Unit tests for the Chapter 4 `PromptBuilder` source code.

## Structure

```
PromptBuilder.Tests/
├── GlobalUsings.cs                 ← Global using statements (xUnit + PromptBuilderDemo)
├── PromptBuilderTests.cs           ← All tests for the PromptBuilder fluent builder
└── PromptBuilder.Tests.csproj
```

---

## Running the Tests

### All tests (fast, no external dependencies)

```bash
dotnet test
```

Expected output:
```
Passed!  - Failed: 0, Passed: 31, Skipped: 0, Total: 31
```

---

## What Each Test Does

### `PromptBuilderTests` — Fluent builder coverage (31 tests)

Tests every public method and the `Build()` assembly logic of the `PromptBuilder` class.

#### Guard / validation tests

| Test | What it verifies |
|---|---|
| `Build_WithNoTask_ThrowsInvalidOperationException` | `Build()` without calling `WithTask()` throws `InvalidOperationException` with a message mentioning "task" |
| `WithRole_EmptyString_ThrowsArgumentException` | `WithRole("")` throws `ArgumentException` |
| `WithContext_EmptyString_ThrowsArgumentException` | `WithContext("")` throws `ArgumentException` |
| `WithTask_EmptyString_ThrowsArgumentException` | `WithTask("")` throws `ArgumentException` |
| `WithExample_EmptyInput_ThrowsArgumentException` | `WithExample("", "x")` throws `ArgumentException` |
| `WithExample_EmptyOutput_ThrowsArgumentException` | `WithExample("x", "")` throws `ArgumentException` |
| `WithRole_WhitespaceOnly_ThrowsArgumentException` (×3) | `WithRole("   ")`, `"\t"`, `"\n"` all throw `ArgumentException` |
| `WithContext_WhitespaceOnly_ThrowsArgumentException` (×3) | Same check for `WithContext` |
| `WithTask_WhitespaceOnly_ThrowsArgumentException` (×3) | Same check for `WithTask` |
| `WithConstraints_WhitespaceOnly_ThrowsArgumentException` (×3) | Same check for `WithConstraints` |
| `WithExample_WhitespaceOnlyInput_ThrowsArgumentException` (×2) | Whitespace `input` arg throws |
| `WithExample_WhitespaceOnlyOutput_ThrowsArgumentException` (×2) | Whitespace `output` arg throws |

#### Output structure tests

| Test | What it verifies |
|---|---|
| `Build_WithTaskOnly_ContainsTaskText` | A task-only prompt includes the exact task string |
| `Build_WithTaskOnly_DoesNotContainRoleOrContextSections` | Task-only output trims to just the task text |
| `Build_WithRoleAndContext_RoleAppearsBeforeContext` | Role index < context index in assembled string |
| `Build_WithContextAndTask_ContextAppearsBeforeTask` | Context index < task index in assembled string |
| `Build_MultipleConstraints_EachAppearsWithBlankLineBetween` | Each constraint block is separated by a blank line (`\r\n\r\n` or `\n\n`) |
| `Build_WithSingleExample_WrapsInDashDelimiters` | Example output contains `---`, `Input: …`, `Output: …` |
| `Build_MultipleExamples_AllWrappedInDashDelimiters` | Two examples produce ≥ 4 `---` delimiter occurrences |
| `Build_FullFivePartPrompt_SectionsAppearInCorrectOrder` | Role → Context → Task → Constraints → Examples (index-order assertion) |

#### Fluency test

| Test | What it verifies |
|---|---|
| `AllMethods_ReturnSameBuilderInstance_SupportingFluentChaining` | `WithRole`, `WithContext`, `WithTask`, `WithConstraints`, and `WithExample` all return `this` |

---

## Related

- **Source code:** `../../src/PromptBuilder/PromptBuilder.cs`
- **Chapter overview:** Prompt anatomy — Role → Context → Task → Constraints → Examples

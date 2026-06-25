# Chapter 3 — Tests

Unit and integration tests for the Chapter 3 code samples.

## Structure

```
ParameterPlayground.Tests/
├── TestHelpers.cs                  ← Shared utilities (mock client, ChatOptions builder)
├── TemperatureRangeTests.cs        ← Temperature property range and type tests
├── ChatOptionsTests.cs             ← ChatOptions property validation tests
├── MockedCallTests.cs              ← GetResponseAsync call patterns (mocked)
├── IntegrationTests.cs             ← Live LM Studio call across all 3 temperatures (skips if offline)
├── GlobalUsings.cs                 ← Global using statements
└── ParameterPlayground.Tests.csproj
```

---

## Running the Tests

### Unit tests (fast, no LM Studio required)

```bash
dotnet test --filter "Category!=Integration"
```

Expected output:
```
Passed!  - Failed: 0, Passed: 11, Skipped: 0, Total: 11
```

### Integration tests (requires LM Studio running)

```bash
# First: start LM Studio, load a model, click "Start Server"
# Server must be running at http://localhost:1234/v1
# (your port may differ — check LM Studio's server panel)

dotnet test --filter "Category=Integration"
```

### All tests

```bash
dotnet test
```

> ⚠️ If LM Studio is not running, the integration test will error with a clear message
> (`LM Studio is not running at localhost:1234`) rather than failing silently.

---

## What Each Test Does

### `TemperatureRangeTests` — Temperature range and type (4 tests)

Tests the `Temperature` property of `ChatOptions` — matching the `new[] { 0f, 0.5f, 1.0f }` array from Program.cs.

| Test | What it verifies |
|---|---|
| `Temperature_Zero_IsMinimum_ValidValue` | `new ChatOptions { Temperature = 0f }` constructs without exception — 0 is the deterministic floor |
| `Temperature_One_IsValidMidpoint` | `0.7f` is accepted — common creative-but-coherent default |
| `Temperature_Two_IsValidMaximum` | `2.0f` is accepted — the SDK doesn't range-validate; the API enforces the ceiling |
| `Temperature_Values_AreFloat_NotDouble` | `ChatOptions.Temperature` is `float?` not `double?` — confirmed via reflection |

---

### `ChatOptionsTests` — ChatOptions properties (3 tests)

Tests the `MaxOutputTokens`, `StopSequences`, and temperature array values used in Program.cs.

| Test | What it verifies |
|---|---|
| `ChatOptions_MaxOutputTokens_CanBeSet` | `MaxOutputTokens = 100` constructs cleanly — the cap used in Program.cs to limit responses to ~75 words |
| `ChatOptions_StopSequences_CanContainPeriod` | `StopSequences = ["."]` is valid — useful for capping single-sentence responses |
| `ChatOptions_AllThreeTemperaturesInArray_AreDistinct` | The `{ 0f, 0.5f, 1.0f }` array has exactly 3 distinct values — duplicate temperatures would produce identical outputs and break the chapter demo |

---

### `MockedCallTests` — GetResponseAsync patterns (4 tests)

Tests the `IChatClient.GetResponseAsync()` call pattern using Moq. **Zero real network traffic.**

| Test | What it verifies |
|---|---|
| `GetResponseAsync_WithOptions_PassesOptionsToClient` | `GetResponseAsync` is called with a non-null `ChatOptions` — the mock verifies options are forwarded |
| `GetResponseAsync_WithCancellationToken_IsPassedThrough` | `CancellationToken.None` is passed explicitly (as in Program.cs) — the three-argument overload is used |
| `GetResponseAsync_ThrowsHttpRequestException_CaughtPerIteration` | A throw at T=0.5f leaves T=0f and T=1.0f unaffected — simulates the per-iteration `try/catch` in Program.cs |
| `GetResponseAsync_EmptyText_HandledGracefully` | An empty `ChatResponse` yields `null/empty` `response.Text` without crashing |

---

### `IntegrationTests` — Live call across all temperatures (1 test, skips if offline)

**Requires LM Studio running with `microsoft/phi-4-mini-instruct` (or another instruction-tuned model) loaded.**

| Test | What it verifies |
|---|---|
| `Phi4_ThreeTemperatures_AllReturnNonEmptyText` | All three temperatures (0f, 0.5f, 1.0f) return non-empty `response.Text` from the real model. Skips gracefully if LM Studio is not reachable |

---

## Shared Utilities (`TestHelpers.cs`)

| Helper | What it does |
|---|---|
| `CreateMockClient(responseText)` | Returns a Moq-backed `IChatClient` whose `GetResponseAsync` always returns a `ChatResponse` with the given text. Default: `"Mock response"` |
| `CreateMock(responseText)` | Same as above but returns the `Mock<IChatClient>` so callers can run `Verify()` assertions |
| `BuildChatOptions(temp, maxTokens)` | Builds a `ChatOptions` with the given temperature and token cap — matches the options block in Program.cs |

---

## Related

- **Source code:** `../../src/ParameterPlayground/Program.cs`
- **Chapter:** `../../chapter-03-how-llms-work.md`
- **Chapter 3 overview:** The temperature experiment — same prompt sent at T=0, T=0.5, and T=1.0 to observe determinism vs. creativity

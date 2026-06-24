# Chapter 2 — Tests

Unit and integration tests for the Chapter 2 code samples.

## Structure

```
HelloAI.Tests/
├── TestHelpers.cs                  ← Shared utilities (mock client, config builder)
├── LmStudioClientTests.cs          ← Provider creation patterns (no API calls)
├── ConfigurationTests.cs           ← IConfiguration wiring and null-guard patterns
├── MockedCallTests.cs              ← GetResponseAsync call patterns (mocked)
├── LmStudioIntegrationTests.cs     ← Live LM Studio call (requires server running)
├── GlobalUsings.cs                 ← Global using statements
└── HelloAI.Tests.csproj
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
# Server must be running at http://localhost:5000/v1

dotnet test --filter "Category=Integration"
```

### All tests

```bash
dotnet test
```

> ⚠️ If LM Studio is not running, the integration test will error with a clear message
> (`LM Studio is not running at localhost:5000`) rather than failing silently.

---

## What Each Test Does

### `LmStudioClientTests` — Provider creation (4 tests)

Tests the `OpenAIClient` construction pattern used in `Program.cs` Option A.
**No real network calls are made** — these verify the client object graph only.

| Test | What it verifies |
|---|---|
| `LmStudio_Client_CanBeCreated_WithExpectedEndpoint` | The `OpenAIClient` + `ApiKeyCredential` + `OpenAIClientOptions` pattern produces a valid `IChatClient` |
| `LmStudio_Endpoint_Uri_IsCorrect` | The endpoint URI is exactly `http://localhost:5000/v1` (guards against port typos) |
| `LmStudio_ModelId_MatchesExpectedPattern` | The model ID follows the `org/model-name` format (e.g., `microsoft/phi-4-mini-reasoning`) |
| `LmStudio_ApiKey_ValueIsIgnoredButMustBeNonEmpty` | `ApiKeyCredential("lm-studio")` construction succeeds — LM Studio ignores the value but the SDK requires a non-empty string |

---

### `ConfigurationTests` — IConfiguration wiring (4 tests)

Tests the secrets management pattern: `IConfiguration` + `AddUserSecrets<Program>()` + `AddEnvironmentVariables()`.

> **Why this matters:** `dotnet user-secrets set` stores values in a JSON file — NOT as OS environment variables. `Environment.GetEnvironmentVariable()` won't read them. These tests verify the `IConfiguration` approach works correctly.

| Test | What it verifies |
|---|---|
| `Config_MissingOpenAiKey_ThrowsInvalidOperationException` | When `OPENAI_API_KEY` is not set, the `?? throw new InvalidOperationException(...)` pattern fires with a message containing the key name |
| `Config_MissingAzureEndpoint_ThrowsInvalidOperationException` | Same null-guard pattern for `AZURE_AI_ENDPOINT` |
| `Config_PresentKey_ReturnsValue` | When the key IS present, `config["OPENAI_API_KEY"]` returns the value without throwing |
| `Config_EnvVar_WinsOver_UserSecrets` | When both user-secrets and env vars have the same key, the env var value wins — because `AddEnvironmentVariables()` is added last in `Program.cs`. This is intentional: CI/CD pipelines inject secrets via env vars and those should override local developer secrets |

---

### `MockedCallTests` — GetResponseAsync patterns (3 tests)

Tests the `IChatClient.GetResponseAsync()` call pattern using Moq. **Zero real network traffic.**

| Test | What it verifies |
|---|---|
| `GetResponseAsync_ReturnsExpectedText` | A mocked client returns the expected `response.Text` value |
| `GetResponseAsync_ModelReturnsEmpty_TextIsNullOrEmpty` | When the model returns an empty response, `response.Text` is null or empty — the app doesn't crash |
| `GetResponseAsync_ModelThrows_HttpRequestException_PropagatesUp` | When the underlying transport throws (e.g., LM Studio not running), the exception propagates — it is not silently swallowed |

---

### `LmStudioIntegrationTests` — Live call (1 test, skips if offline)

**Requires LM Studio running at `localhost:5000` with a model loaded.**

| Test | What it verifies |
|---|---|
| `Phi4Mini_HelloPrompt_ReturnsNonEmptyResponse` | A real call to `microsoft/phi-4-mini-reasoning` returns a non-empty `response.Text`. Skips gracefully if LM Studio is not reachable (catches `HttpRequestException` / `SocketException`) |

---

## Shared Utilities (`TestHelpers.cs`)

| Helper | What it does |
|---|---|
| `CreateMockClient(responseText)` | Returns a Moq-backed `IChatClient` whose `GetResponseAsync` always returns a `ChatResponse` with the given text. Default: `"Mock response"` |
| `BuildConfig(values)` | Builds an `IConfiguration` from an in-memory `Dictionary<string, string?>` — useful for testing configuration-dependent code without touching the filesystem |

---

## Related

- **Source code:** `../src/HelloAI/Program.cs`
- **Chapter:** `../chapter-02-setting-up-your-environment.md`
- **Chapter 2 overview:** Three provider options (LM Studio, OpenAI, Azure AI Foundry) with identical `IChatClient` usage

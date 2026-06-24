using Microsoft.Extensions.Configuration;

namespace HelloAI.Tests;

/// <summary>
/// Tests for the null-guard and IConfiguration wiring patterns from Program.cs.
/// </summary>
public class ConfigurationTests
{
    // ── null-guard tests ────────────────────────────────────────────────────

    [Fact]
    public void Config_MissingOpenAiKey_ThrowsInvalidOperationException()
    {
        // Build a config with NO keys — simulates a fresh machine with no secrets set.
        var config = TestHelpers.BuildConfig(new Dictionary<string, string?>());

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _ = config["OPENAI_API_KEY"]
                ?? throw new InvalidOperationException(
                    "OPENAI_API_KEY is not set. Run: dotnet user-secrets set \"OPENAI_API_KEY\" \"sk-...\""));

        Assert.Contains("OPENAI_API_KEY", ex.Message);
    }

    [Fact]
    public void Config_MissingAzureEndpoint_ThrowsInvalidOperationException()
    {
        var config = TestHelpers.BuildConfig(new Dictionary<string, string?>());

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _ = config["AZURE_AI_ENDPOINT"]
                ?? throw new InvalidOperationException(
                    "AZURE_AI_ENDPOINT is not set. Run: dotnet user-secrets set \"AZURE_AI_ENDPOINT\" \"https://...\""));

        Assert.Contains("AZURE_AI_ENDPOINT", ex.Message);
    }

    [Fact]
    public void Config_PresentKey_ReturnsValue()
    {
        // When the key IS present the null-coalescing ?? throw must NOT fire.
        var config = TestHelpers.BuildConfig(new Dictionary<string, string?>
        {
            ["OPENAI_API_KEY"] = "sk-test-key"
        });

        var value = config["OPENAI_API_KEY"]
            ?? throw new InvalidOperationException("OPENAI_API_KEY is not set");

        Assert.Equal("sk-test-key", value);
    }

    // ── IConfiguration layering test ────────────────────────────────────────

    [Fact]
    public void Config_EnvVar_WinsOver_UserSecrets()
    {
        // AddEnvironmentVariables is called AFTER AddUserSecrets in Program.cs,
        // so environment-variable values override user-secret values.
        // This is intentional: CI/CD pipelines inject secrets via env vars and
        // those must take precedence over any local developer secrets.json file.
        //
        // We simulate both layers with two in-memory sources in the same order
        // that Program.cs registers them.

        const string secretValue = "sk-from-user-secrets";
        const string envValue = "sk-from-env-var";

        var config = new ConfigurationBuilder()
            // Layer 1 — user-secrets (lower priority)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OPENAI_API_KEY"] = secretValue
            })
            // Layer 2 — environment variables (higher priority — added last)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OPENAI_API_KEY"] = envValue
            })
            .Build();

        Assert.Equal(envValue, config["OPENAI_API_KEY"]);
    }
}

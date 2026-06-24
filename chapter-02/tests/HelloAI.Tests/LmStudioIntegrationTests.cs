using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.Net.Sockets;

namespace HelloAI.Tests;

/// <summary>
/// Integration tests that call a real LM Studio server at localhost:5000.
/// These are skipped gracefully when LM Studio is not running.
///
/// Run only in environments where LM Studio is available:
///   dotnet test --filter "Category=Integration"
/// </summary>
public class LmStudioIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Phi4Mini_HelloPrompt_ReturnsNonEmptyResponse()
    {
        IChatClient client = new OpenAIClient(
                new ApiKeyCredential("lm-studio"),
                new OpenAIClientOptions { Endpoint = new Uri("http://localhost:5000/v1") })
            .GetChatClient("microsoft/phi-4-mini-reasoning")
            .AsIChatClient();

        try
        {
            var response = await client.GetResponseAsync(
            [
                new ChatMessage(ChatRole.User,
                    "Explain what a delegate is in C# in two sentences, like I'm a junior developer.")
            ]);

            Assert.NotNull(response.Text);
            Assert.NotEmpty(response.Text);
        }
        catch (HttpRequestException ex) when (
            ex.Message.Contains("Connection refused", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("actively refused", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("No connection", StringComparison.OrdinalIgnoreCase))
        {
            // LM Studio is not running — skip gracefully instead of failing CI.
            // Start LM Studio, load a model, and click "Start Server" to enable this test.
            Skip("LM Studio is not running at localhost:5000. Start the server to run this test.");
        }
        catch (SocketException)
        {
            Skip("LM Studio is not running at localhost:5000 (SocketException).");
        }
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    /// <summary>Throws <see cref="SkipException"/> to skip the test with a message.</summary>
    private static void Skip(string reason) =>
        throw new SkipException(reason);
}

/// <summary>
/// Lightweight skip mechanism — xUnit doesn't have built-in skip-at-runtime
/// before v3, so we use a custom exception + runner attribute combination.
/// For a full solution add the `Xunit.SkippableFact` NuGet package.
/// Here we simply throw a recognisable exception that causes the test to error
/// with a clear message rather than fail with an assertion error.
/// </summary>
public sealed class SkipException(string reason) : Exception(reason);

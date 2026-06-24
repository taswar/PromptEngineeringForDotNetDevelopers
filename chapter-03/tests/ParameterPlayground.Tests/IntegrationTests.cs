using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.Net.Sockets;

namespace ParameterPlayground.Tests;

/// <summary>
/// Integration tests that call a real LM Studio server at localhost:1234.
/// These skip gracefully when LM Studio is not running.
///
/// Run only in environments where LM Studio is available:
///   dotnet test --filter "Category=Integration"
/// </summary>
public class IntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Phi4_ThreeTemperatures_AllReturnNonEmptyText()
    {
        IChatClient client = new OpenAIClient(
                new ApiKeyCredential("lm-studio"),
                new OpenAIClientOptions { Endpoint = new Uri("http://localhost:1234/v1") })
            .GetChatClient("microsoft/phi-4-mini-reasoning")
            .AsIChatClient();

        var prompt = "Describe what a C# delegate is in exactly one sentence.";
        var temperatures = new[] { 0f, 0.5f, 1.0f };

        foreach (var temp in temperatures)
        {
            try
            {
                var options = TestHelpers.BuildChatOptions(temp);
                var response = await client.GetResponseAsync(
                    [new ChatMessage(ChatRole.User, prompt)],
                    options,
                    CancellationToken.None);

                Assert.NotNull(response.Text);
                Assert.NotEmpty(response.Text);
            }
            catch (HttpRequestException ex) when (
                ex.Message.Contains("Connection refused", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("actively refused", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("No connection", StringComparison.OrdinalIgnoreCase))
            {
                Skip("LM Studio is not running at localhost:1234. Start the server to run this test.");
            }
            catch (SocketException)
            {
                Skip("LM Studio is not running at localhost:1234 (SocketException).");
            }
        }
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    /// <summary>Throws <see cref="SkipException"/> to skip the test with a message.</summary>
    private static void Skip(string reason) =>
        throw new SkipException(reason);
}

/// <summary>
/// Lightweight skip mechanism — xUnit v2 has no built-in skip-at-runtime.
/// Throwing this causes the test to error with a clear message rather than
/// fail with an assertion error. Use Xunit.SkippableFact for a full solution.
/// </summary>
public sealed class SkipException(string reason) : Exception(reason);

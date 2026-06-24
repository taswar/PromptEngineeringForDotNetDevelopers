using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Moq;

namespace HelloAI.Tests;

/// <summary>
/// Shared helpers used across all test classes.
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Returns a mocked <see cref="IChatClient"/> whose GetResponseAsync returns a
    /// <see cref="ChatResponse"/> containing <paramref name="responseText"/>.
    /// </summary>
    public static IChatClient CreateMockClient(string responseText = "Mock response")
    {
        var mock = new Mock<IChatClient>();

        var message = new ChatMessage(ChatRole.Assistant, responseText);
        var chatResponse = new ChatResponse(message);

        mock.Setup(c => c.GetResponseAsync(
                It.IsAny<IList<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);

        return mock.Object;
    }

    /// <summary>
    /// Builds an <see cref="IConfiguration"/> from an in-memory dictionary.
    /// </summary>
    public static IConfiguration BuildConfig(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}

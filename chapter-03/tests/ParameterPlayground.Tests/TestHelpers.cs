using Microsoft.Extensions.AI;
using Moq;

namespace ParameterPlayground.Tests;

/// <summary>
/// Shared helpers used across all test classes.
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Returns a Moq-backed <see cref="IChatClient"/> whose GetResponseAsync always
    /// returns a <see cref="ChatResponse"/> containing <paramref name="responseText"/>.
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
    /// Returns a <see cref="Mock{IChatClient}"/> (not just the object) so callers
    /// can run Verify() assertions on it.
    /// </summary>
    public static Mock<IChatClient> CreateMock(string responseText = "Mock response")
    {
        var mock = new Mock<IChatClient>();

        var message = new ChatMessage(ChatRole.Assistant, responseText);
        var chatResponse = new ChatResponse(message);

        mock.Setup(c => c.GetResponseAsync(
                It.IsAny<IList<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);

        return mock;
    }

    /// <summary>
    /// Builds a <see cref="ChatOptions"/> with the given temperature and token cap,
    /// matching the options block used in Program.cs.
    /// </summary>
    public static ChatOptions BuildChatOptions(float temp, int maxTokens = 100) =>
        new ChatOptions
        {
            Temperature = temp,
            MaxOutputTokens = maxTokens
        };
}

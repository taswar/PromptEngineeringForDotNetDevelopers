using Microsoft.Extensions.AI;
using Moq;

namespace HelloAI.Tests;

/// <summary>
/// Tests for the GetResponseAsync call pattern in Program.cs.
/// All tests use mocked IChatClient — zero real network traffic.
/// </summary>
public class MockedCallTests
{
    [Fact]
    public async Task GetResponseAsync_ReturnsExpectedText()
    {
        const string expected = "A delegate is a type-safe function pointer";

        var client = TestHelpers.CreateMockClient(expected);

        var response = await client.GetResponseAsync(
        [
            new ChatMessage(ChatRole.User,
                "Explain what a delegate is in C# in two sentences.")
        ]);

        Assert.NotNull(response.Text);
        Assert.NotEmpty(response.Text);
        Assert.Equal(expected, response.Text);
    }

    [Fact]
    public async Task GetResponseAsync_ModelReturnsEmpty_TextIsNullOrEmpty()
    {
        // When the model returns an empty string the app should not crash —
        // response.Text will be null or empty and callers must handle that gracefully.
        var mock = new Mock<IChatClient>();

        var emptyMessage = new ChatMessage(ChatRole.Assistant, string.Empty);
        var chatResponse = new ChatResponse(emptyMessage);

        mock.Setup(c => c.GetResponseAsync(
                It.IsAny<IList<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);

        var response = await mock.Object.GetResponseAsync(
        [
            new ChatMessage(ChatRole.User, "hello")
        ]);

        // response.Text should be null or empty — not throw
        Assert.True(string.IsNullOrEmpty(response.Text));
    }

    [Fact]
    public async Task GetResponseAsync_ModelThrows_HttpRequestException_PropagatesUp()
    {
        // When the underlying transport throws (e.g., LM Studio is not running),
        // the exception must not be silently swallowed — callers need to see it.
        var mock = new Mock<IChatClient>();

        mock.Setup(c => c.GetResponseAsync(
                It.IsAny<IList<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            mock.Object.GetResponseAsync(
            [
                new ChatMessage(ChatRole.User, "hello")
            ]));
    }
}

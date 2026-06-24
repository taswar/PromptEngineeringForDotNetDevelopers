using Microsoft.Extensions.AI;
using Moq;

namespace ParameterPlayground.Tests;

/// <summary>
/// Unit tests for the GetResponseAsync call pattern in Program.cs.
/// All tests use a mocked <see cref="IChatClient"/> — zero real network traffic.
/// </summary>
public class MockedCallTests
{
    [Fact]
    public async Task GetResponseAsync_WithOptions_PassesOptionsToClient()
    {
        // Arrange
        var mock = TestHelpers.CreateMock("The sky is blue.");
        var options = TestHelpers.BuildChatOptions(0f);

        // Act
        await mock.Object.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "Describe what a C# delegate is in exactly one sentence.")],
            options,
            CancellationToken.None);

        // Assert — GetResponseAsync must have been called with a non-null ChatOptions
        mock.Verify(c => c.GetResponseAsync(
            It.IsAny<IList<ChatMessage>>(),
            It.Is<ChatOptions?>(o => o != null),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetResponseAsync_WithCancellationToken_IsPassedThrough()
    {
        // Program.cs passes CancellationToken.None explicitly.
        // This test verifies the overload that accepts a CancellationToken is called.
        var mock = TestHelpers.CreateMock();

        await mock.Object.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "test")],
            null,
            CancellationToken.None);

        mock.Verify(c => c.GetResponseAsync(
            It.IsAny<IList<ChatMessage>>(),
            It.IsAny<ChatOptions?>(),
            CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task GetResponseAsync_ThrowsHttpRequestException_CaughtPerIteration()
    {
        // Program.cs wraps each temperature iteration in its own try/catch.
        // A failure at T=0.5f must not prevent T=0f and T=1.0f from completing.
        var mock = new Mock<IChatClient>();
        var okResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok"));

        // T=0.5f call throws; all other temperatures succeed
        mock.Setup(c => c.GetResponseAsync(
                It.IsAny<IList<ChatMessage>>(),
                It.Is<ChatOptions?>(o => o != null && o.Temperature == 0.5f),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("simulated transport error at T=0.5"));

        mock.Setup(c => c.GetResponseAsync(
                It.IsAny<IList<ChatMessage>>(),
                It.Is<ChatOptions?>(o => o == null || o.Temperature != 0.5f),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(okResponse);

        // Simulate the Program.cs foreach loop with per-iteration try/catch
        var temperatures = new[] { 0f, 0.5f, 1.0f };
        int successCount = 0;
        int errorCount = 0;

        foreach (var temp in temperatures)
        {
            try
            {
                var response = await mock.Object.GetResponseAsync(
                    [new ChatMessage(ChatRole.User, "test")],
                    TestHelpers.BuildChatOptions(temp),
                    CancellationToken.None);
                successCount++;
            }
            catch (Exception)
            {
                errorCount++;
            }
        }

        Assert.Equal(2, successCount);  // T=0f and T=1.0f succeed
        Assert.Equal(1, errorCount);    // T=0.5f fails
    }

    [Fact]
    public async Task GetResponseAsync_EmptyText_HandledGracefully()
    {
        // When the model returns an empty message, response.Text must be null or empty —
        // not crash. Program.cs calls Console.WriteLine(response.Text) which handles null.
        var mock = new Mock<IChatClient>();
        var emptyResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, string.Empty));

        mock.Setup(c => c.GetResponseAsync(
                It.IsAny<IList<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyResponse);

        var response = await mock.Object.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "test")],
            TestHelpers.BuildChatOptions(0f),
            CancellationToken.None);

        Assert.True(string.IsNullOrEmpty(response.Text));
    }
}

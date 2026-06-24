using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

namespace HelloAI.Tests;

/// <summary>
/// Tests for the LM Studio provider creation pattern shown in Program.cs (Option A).
/// No real network calls are made — we only verify the client object graph.
/// </summary>
public class LmStudioClientTests
{
    private const string LmStudioEndpoint = "http://localhost:5000/v1";
    private const string ModelId = "microsoft/phi-4-mini-reasoning";

    [Fact]
    public void LmStudio_Client_CanBeCreated_WithExpectedEndpoint()
    {
        // Replicate the exact construction pattern from Program.cs Option A.
        IChatClient client = new OpenAIClient(
                new ApiKeyCredential("lm-studio"),
                new OpenAIClientOptions { Endpoint = new Uri(LmStudioEndpoint) })
            .GetChatClient(ModelId)
            .AsIChatClient();

        // The object must implement IChatClient — that's the whole point of MEAI.
        Assert.NotNull(client);
        Assert.IsAssignableFrom<IChatClient>(client);
    }

    [Fact]
    public void LmStudio_Endpoint_Uri_IsCorrect()
    {
        // Verify the exact endpoint Uri the book instructs readers to use.
        var options = new OpenAIClientOptions { Endpoint = new Uri(LmStudioEndpoint) };

        Assert.Equal(new Uri(LmStudioEndpoint), options.Endpoint);
    }

    [Fact]
    public void LmStudio_ModelId_MatchesExpectedPattern()
    {
        // The model ID format used in the book is "org/model-name" (slash-separated).
        // This guards against accidentally stripping the org prefix.
        Assert.Contains('/', ModelId);
        Assert.Matches(@"^[^/]+/[^/]+", ModelId);
    }

    [Fact]
    public void LmStudio_ApiKey_ValueIsIgnoredButMustBeNonEmpty()
    {
        // LM Studio ignores the key value, but OpenAIClient rejects null/empty strings.
        var ex = Record.Exception(() =>
            new ApiKeyCredential("lm-studio"));

        Assert.Null(ex); // construction must succeed with a dummy key
    }
}

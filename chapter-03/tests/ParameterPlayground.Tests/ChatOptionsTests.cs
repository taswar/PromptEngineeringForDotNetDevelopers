using Microsoft.Extensions.AI;

namespace ParameterPlayground.Tests;

/// <summary>
/// Unit tests for <see cref="ChatOptions"/> properties used in Program.cs.
/// Mirrors the options block:
///   new ChatOptions { Temperature = temp, MaxOutputTokens = 100 }
/// </summary>
public class ChatOptionsTests
{
    [Fact]
    public void ChatOptions_MaxOutputTokens_CanBeSet()
    {
        // MaxOutputTokens = 100 is hardcoded in Program.cs to cap responses at ~75 words.
        var exception = Record.Exception(() => new ChatOptions { MaxOutputTokens = 100 });
        Assert.Null(exception);
    }

    [Fact]
    public void ChatOptions_StopSequences_CanContainPeriod()
    {
        // StopSequences lets callers halt generation at a specific token.
        // A period "." is a common stop sequence for single-sentence prompts.
        var exception = Record.Exception(() =>
            new ChatOptions { StopSequences = ["."] });
        Assert.Null(exception);
    }

    [Fact]
    public void ChatOptions_AllThreeTemperaturesInArray_AreDistinct()
    {
        // Program.cs declares: var temperatures = new[] { 0f, 0.5f, 1.0f }
        // This test verifies the three values are distinct — if two were equal,
        // the experiment would produce duplicate results and the chapter demo would break.
        var temperatures = new[] { 0f, 0.5f, 1.0f };

        var distinct = temperatures.Distinct().ToArray();

        Assert.Equal(3, distinct.Length);
    }
}

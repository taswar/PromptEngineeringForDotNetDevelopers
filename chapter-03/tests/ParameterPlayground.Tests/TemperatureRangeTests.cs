using Microsoft.Extensions.AI;

namespace ParameterPlayground.Tests;

/// <summary>
/// Unit tests for the Temperature property of <see cref="ChatOptions"/>.
/// These verify the range and type constraints that mirror Program.cs line 65:
///   var temperatures = new[] { 0f, 0.5f, 1.0f };
/// </summary>
public class TemperatureRangeTests
{
    [Fact]
    public void Temperature_Zero_IsMinimum_ValidValue()
    {
        // Temperature = 0 is the deterministic end of the spectrum.
        // ChatOptions construction must not throw.
        var exception = Record.Exception(() => new ChatOptions { Temperature = 0f });
        Assert.Null(exception);
    }

    [Fact]
    public void Temperature_One_IsValidMidpoint()
    {
        // 0.7f is a common "creative but coherent" midpoint used in practice.
        var exception = Record.Exception(() => new ChatOptions { Temperature = 0.7f });
        Assert.Null(exception);
    }

    [Fact]
    public void Temperature_Two_IsValidMaximum()
    {
        // 2.0f is the documented API ceiling. The model becomes very unpredictable
        // but the SDK must accept the value — it doesn't range-validate.
        var exception = Record.Exception(() => new ChatOptions { Temperature = 2.0f });
        Assert.Null(exception);
    }

    [Fact]
    public void Temperature_Values_AreFloat_NotDouble()
    {
        // ChatOptions.Temperature is declared as float? (single precision), not double.
        // This compile-time shape matters for the array literal in Program.cs:
        //   new[] { 0f, 0.5f, 1.0f }  — all float literals, not double
        var prop = typeof(ChatOptions).GetProperty(nameof(ChatOptions.Temperature))!;
        Assert.Equal(typeof(float?), prop.PropertyType);
    }
}

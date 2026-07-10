namespace DevToolkit;

/// <summary>
/// Per-team context injected into all four DevToolkit workflows.
/// Override any property to give the model codebase-specific information.
/// </summary>
/// <remarks>
/// Pass a customised instance to each service constructor:
/// <code>
/// var opts = new DevToolkitOptions { CodebaseContext = ".NET 8, EF Core, CQRS, no mutable domain objects" };
/// var reviewer = new CodeReviewService(chatClient, opts);
/// </code>
/// </remarks>
public sealed record DevToolkitOptions
{
    /// <summary>
    /// Describes the codebase so the model can tailor findings.
    /// E.g. ".NET 8 web API, EF Core, CQRS pattern, nullable reference types enabled."
    /// </summary>
    public string CodebaseContext { get; init; } = "C# codebase targeting modern .NET";

    /// <summary>Test framework and assertion library in use.</summary>
    public string TestFramework { get; init; } = "xUnit with FluentAssertions";

    /// <summary>Commit message format convention.</summary>
    public string CommitFormat { get; init; } = "conventional commits";

    /// <summary>Default options. No customisation — suitable for any C# project.</summary>
    public static readonly DevToolkitOptions Default = new();
}

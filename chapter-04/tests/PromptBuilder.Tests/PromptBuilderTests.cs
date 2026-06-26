namespace PromptBuilder.Tests;

public class PromptBuilderTests
{
    // ── 1. Build() throws when no task ──────────────────────────────────────

    [Fact]
    public void Build_WithNoTask_ThrowsInvalidOperationException()
    {
        var builder = new PromptBuilderDemo.PromptBuilder();

        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("task", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── 2. Task-only prompt ─────────────────────────────────────────────────

    [Fact]
    public void Build_WithTaskOnly_ContainsTaskText()
    {
        var result = new PromptBuilderDemo.PromptBuilder()
            .WithTask("Summarise this document.")
            .Build();

        Assert.Contains("Summarise this document.", result);
    }

    [Fact]
    public void Build_WithTaskOnly_DoesNotContainRoleOrContextSections()
    {
        var result = new PromptBuilderDemo.PromptBuilder()
            .WithTask("Do the thing.")
            .Build();

        // No role / context text should appear beyond the task itself
        Assert.Equal("Do the thing.", result.Trim());
    }

    // ── 3. Role appears before context ──────────────────────────────────────

    [Fact]
    public void Build_WithRoleAndContext_RoleAppearsBeforeContext()
    {
        var result = new PromptBuilderDemo.PromptBuilder()
            .WithRole("You are an expert editor.")
            .WithContext("The document is a technical spec.")
            .WithTask("Improve clarity.")
            .Build();

        var roleIndex    = result.IndexOf("You are an expert editor.", StringComparison.Ordinal);
        var contextIndex = result.IndexOf("The document is a technical spec.", StringComparison.Ordinal);

        Assert.True(roleIndex < contextIndex, "Role should appear before context in the output.");
    }

    // ── 4. Context appears before task ──────────────────────────────────────

    [Fact]
    public void Build_WithContextAndTask_ContextAppearsBeforeTask()
    {
        var result = new PromptBuilderDemo.PromptBuilder()
            .WithContext("Background: the user is a beginner.")
            .WithTask("Explain recursion simply.")
            .Build();

        var contextIndex = result.IndexOf("Background: the user is a beginner.", StringComparison.Ordinal);
        var taskIndex    = result.IndexOf("Explain recursion simply.", StringComparison.Ordinal);

        Assert.True(contextIndex < taskIndex, "Context should appear before task in the output.");
    }

    // ── 5–7. Empty / whitespace-only argument guards ─────────────────────────

    [Fact]
    public void WithRole_EmptyString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new PromptBuilderDemo.PromptBuilder().WithRole(""));
    }

    [Fact]
    public void WithContext_EmptyString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new PromptBuilderDemo.PromptBuilder().WithContext(""));
    }

    [Fact]
    public void WithTask_EmptyString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new PromptBuilderDemo.PromptBuilder().WithTask(""));
    }

    // ── 8–9. WithExample guards ──────────────────────────────────────────────

    [Fact]
    public void WithExample_EmptyInput_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new PromptBuilderDemo.PromptBuilder().WithExample("", "some output"));
    }

    [Fact]
    public void WithExample_EmptyOutput_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new PromptBuilderDemo.PromptBuilder().WithExample("some input", ""));
    }

    // ── 10. Multiple constraints appear with blank lines between them ────────

    [Fact]
    public void Build_MultipleConstraints_EachAppearsWithBlankLineBetween()
    {
        var result = new PromptBuilderDemo.PromptBuilder()
            .WithTask("Write a haiku.")
            .WithConstraints("Use exactly 17 syllables.")
            .WithConstraints("Do not rhyme.")
            .Build();

        Assert.Contains("Use exactly 17 syllables.", result);
        Assert.Contains("Do not rhyme.", result);

        // A blank line separates the two constraint blocks
        var firstEnd  = result.IndexOf("Use exactly 17 syllables.", StringComparison.Ordinal)
                        + "Use exactly 17 syllables.".Length;
        var secondStart = result.IndexOf("Do not rhyme.", StringComparison.Ordinal);
        var between = result[firstEnd..secondStart];
        // On Windows AppendLine uses \r\n; a blank line is either \r\n\r\n or \n\n
        Assert.True(
            between.Contains("\r\n\r\n", StringComparison.Ordinal) ||
            between.Contains("\n\n",     StringComparison.Ordinal),
            $"Expected a blank line between constraints, but got: {System.Text.Json.JsonSerializer.Serialize(between)}");
    }

    // ── 11. WithExample wraps in --- delimiters ──────────────────────────────

    [Fact]
    public void Build_WithSingleExample_WrapsInDashDelimiters()
    {
        var result = new PromptBuilderDemo.PromptBuilder()
            .WithTask("Translate to French.")
            .WithExample("Hello", "Bonjour")
            .Build();

        Assert.Contains("---", result);
        Assert.Contains("Input: Hello", result);
        Assert.Contains("Output: Bonjour", result);
    }

    // ── 12. Multiple examples all have delimiters ────────────────────────────

    [Fact]
    public void Build_MultipleExamples_AllWrappedInDashDelimiters()
    {
        var result = new PromptBuilderDemo.PromptBuilder()
            .WithTask("Translate to French.")
            .WithExample("Hello", "Bonjour")
            .WithExample("Goodbye", "Au revoir")
            .Build();

        Assert.Contains("Input: Hello",    result);
        Assert.Contains("Output: Bonjour", result);
        Assert.Contains("Input: Goodbye",  result);
        Assert.Contains("Output: Au revoir", result);

        // At least two --- delimiter occurrences per example → 4+ total
        var delimCount = CountOccurrences(result, "---");
        Assert.True(delimCount >= 4, $"Expected at least 4 '---' delimiters, found {delimCount}.");
    }

    // ── 13. Fluent: all methods return the same builder instance ─────────────

    [Fact]
    public void AllMethods_ReturnSameBuilderInstance_SupportingFluentChaining()
    {
        var builder = new PromptBuilderDemo.PromptBuilder();

        Assert.Same(builder, builder.WithRole("Expert"));
        Assert.Same(builder, builder.WithContext("Some context"));
        Assert.Same(builder, builder.WithTask("Do something"));
        Assert.Same(builder, builder.WithConstraints("Be concise"));
        Assert.Same(builder, builder.WithExample("input", "output"));
    }

    // ── 14. Full 5-part prompt — correct order ───────────────────────────────

    [Fact]
    public void Build_FullFivePartPrompt_SectionsAppearInCorrectOrder()
    {
        var result = new PromptBuilderDemo.PromptBuilder()
            .WithRole("You are a poet.")
            .WithContext("The theme is autumn.")
            .WithTask("Write a short poem.")
            .WithConstraints("Keep it under 10 lines.")
            .WithExample("spring", "petals fall")
            .Build();

        var roleIdx        = result.IndexOf("You are a poet.",        StringComparison.Ordinal);
        var contextIdx     = result.IndexOf("The theme is autumn.",   StringComparison.Ordinal);
        var taskIdx        = result.IndexOf("Write a short poem.",    StringComparison.Ordinal);
        var constraintIdx  = result.IndexOf("Keep it under 10 lines.", StringComparison.Ordinal);
        var examplesIdx    = result.IndexOf("Examples:",              StringComparison.Ordinal);

        Assert.True(roleIdx       >= 0, "Role not found");
        Assert.True(contextIdx    >= 0, "Context not found");
        Assert.True(taskIdx       >= 0, "Task not found");
        Assert.True(constraintIdx >= 0, "Constraint not found");
        Assert.True(examplesIdx   >= 0, "Examples section not found");

        Assert.True(roleIdx < contextIdx,    "Role must precede context");
        Assert.True(contextIdx < taskIdx,    "Context must precede task");
        Assert.True(taskIdx < constraintIdx, "Task must precede constraints");
        Assert.True(constraintIdx < examplesIdx, "Constraints must precede examples");
    }

    // ── 15. Whitespace-only strings throw for all setters ────────────────────

    [Theory]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void WithRole_WhitespaceOnly_ThrowsArgumentException(string whitespace)
    {
        Assert.Throws<ArgumentException>(() =>
            new PromptBuilderDemo.PromptBuilder().WithRole(whitespace));
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void WithContext_WhitespaceOnly_ThrowsArgumentException(string whitespace)
    {
        Assert.Throws<ArgumentException>(() =>
            new PromptBuilderDemo.PromptBuilder().WithContext(whitespace));
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void WithTask_WhitespaceOnly_ThrowsArgumentException(string whitespace)
    {
        Assert.Throws<ArgumentException>(() =>
            new PromptBuilderDemo.PromptBuilder().WithTask(whitespace));
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void WithConstraints_WhitespaceOnly_ThrowsArgumentException(string whitespace)
    {
        Assert.Throws<ArgumentException>(() =>
            new PromptBuilderDemo.PromptBuilder().WithConstraints(whitespace));
    }

    [Theory]
    [InlineData("   ", "output")]
    [InlineData("\t",  "output")]
    public void WithExample_WhitespaceOnlyInput_ThrowsArgumentException(string whitespace, string output)
    {
        Assert.Throws<ArgumentException>(() =>
            new PromptBuilderDemo.PromptBuilder().WithExample(whitespace, output));
    }

    [Theory]
    [InlineData("input", "   ")]
    [InlineData("input", "\t")]
    public void WithExample_WhitespaceOnlyOutput_ThrowsArgumentException(string input, string whitespace)
    {
        Assert.Throws<ArgumentException>(() =>
            new PromptBuilderDemo.PromptBuilder().WithExample(input, whitespace));
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static int CountOccurrences(string source, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += value.Length;
        }
        return count;
    }
}

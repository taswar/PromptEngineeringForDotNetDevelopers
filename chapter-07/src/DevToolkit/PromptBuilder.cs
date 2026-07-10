#nullable enable

namespace DevToolkit;

/// <summary>
/// Fluent builder for constructing structured prompts using the 5-part anatomy:
/// Role → Context → Task → Constraints → Examples.
///
/// Build() produces the system prompt string. Pass it to ChatRole.System.
/// The task is the only required part — everything else is optional.
/// </summary>
/// <remarks>
/// Copied from Chapter 4 with namespace changed to DevToolkit.
/// See chapter-04/src/PromptBuilder/PromptBuilder.cs for the original.
/// </remarks>
public sealed class PromptBuilder
{
    private string? _role;
    private string? _context;
    private string? _task;
    private readonly List<string> _constraints = [];
    private readonly List<(string Input, string Output)> _examples = [];

    /// <summary>Sets the model's persona and expert frame.</summary>
    public PromptBuilder WithRole(string role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        _role = role;
        return this;
    }

    /// <summary>Provides background the model can't infer from the task alone.</summary>
    public PromptBuilder WithContext(string context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(context);
        _context = context;
        return this;
    }

    /// <summary>The actual instruction — what the model should do.</summary>
    public PromptBuilder WithTask(string task)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(task);
        _task = task;
        return this;
    }

    /// <summary>
    /// Adds format, length, or scope constraints. Call multiple times to add
    /// distinct constraint groups — each appears on its own line in the output.
    /// </summary>
    public PromptBuilder WithConstraints(string constraints)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(constraints);
        _constraints.Add(constraints);
        return this;
    }

    /// <summary>Adds an input/output example. Improves output consistency.</summary>
    public PromptBuilder WithExample(string input, string output)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(output);
        _examples.Add((input, output));
        return this;
    }

    /// <summary>
    /// Assembles the prompt in role → context → task → constraints → examples order.
    /// Throws <see cref="InvalidOperationException"/> if WithTask() was not called.
    /// </summary>
    public string Build()
    {
        if (_task is null)
            throw new InvalidOperationException(
                "A task is required. Call WithTask() before Build().");

        var sb = new System.Text.StringBuilder();

        if (_role is not null)
            sb.AppendLine(_role).AppendLine();

        if (_context is not null)
            sb.AppendLine(_context).AppendLine();

        sb.AppendLine(_task);

        if (_constraints.Count > 0)
        {
            sb.AppendLine();
            foreach (var c in _constraints)
            {
                sb.AppendLine(c);
                sb.AppendLine();
            }
        }

        if (_examples.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Examples:");
            foreach (var (input, output) in _examples)
            {
                sb.AppendLine("---");
                sb.AppendLine($"Input: {input}");
                sb.AppendLine($"Output: {output}");
                sb.AppendLine("---");
                sb.AppendLine();
            }
        }

        return sb.ToString().TrimEnd();
    }
}

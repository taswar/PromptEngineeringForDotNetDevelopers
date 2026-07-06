namespace TechniqueBenchmark;

/// <summary>
/// Fluent builder for structured prompts. Introduced in Chapter 4; carried forward here.
/// Produces a single string prompt in role/context/task/constraint/example order.
/// </summary>
public sealed class PromptBuilder
{
    private string? _role;
    private string? _task;
    private string? _context;
    private readonly List<string> _examples = new();
    private readonly List<string> _constraints = new();

    public PromptBuilder WithRole(string role)
    {
        _role = role;
        return this;
    }

    public PromptBuilder WithTask(string task)
    {
        _task = task;
        return this;
    }

    public PromptBuilder WithContext(string context)
    {
        _context = context;
        return this;
    }

    public PromptBuilder WithExample(string example)
    {
        _examples.Add(example);
        return this;
    }

    public PromptBuilder WithConstraints(string constraints)
    {
        _constraints.Add(constraints);
        return this;
    }

    public string Build()
    {
        var sb = new System.Text.StringBuilder();

        if (_role is not null)
            sb.AppendLine(_role).AppendLine();

        if (_examples.Count > 0)
        {
            foreach (var example in _examples)
            {
                // Delimiters prevent example content from being interpreted as instructions
                sb.AppendLine("---");
                sb.AppendLine(example);
                sb.AppendLine("---");
                sb.AppendLine();
            }
        }

        if (_constraints.Count > 0)
        {
            sb.AppendLine("Constraints:");
            foreach (var constraint in _constraints)
                sb.AppendLine(constraint);
            sb.AppendLine();
        }

        if (_task is not null)
            sb.AppendLine(_task).AppendLine();

        if (_context is not null)
            sb.AppendLine(_context);

        return sb.ToString().TrimEnd();
    }
}

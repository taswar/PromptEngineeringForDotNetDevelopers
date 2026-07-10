namespace DevToolkit;

/// <summary>
/// Strips common LLM output failure modes before JSON deserialization.
/// The same four failure modes appear across every workflow that expects JSON:
///   1. Markdown code fences  (```json … ```)
///   2. Leading prose before the opening brace
///   3. Trailing text after the closing brace
///   4. Whitespace padding
/// </summary>
/// <remarks>
/// Pattern from Chapter 6's CleanRawOutput — extracted here so every service
/// uses the same logic without duplication.
/// </remarks>
internal static class OutputCleaner
{
    /// <summary>
    /// Cleans <paramref name="raw"/> so it can be passed directly to
    /// <see cref="System.Text.Json.JsonSerializer.Deserialize{T}"/>.
    /// </summary>
    internal static string Clean(string raw)
    {
        var text = raw.Trim();

        // Failure mode 1 — markdown fences. The model wraps JSON in fences even
        // when explicitly told not to. Strip and move on.
        if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            text = text["```json".Length..];
        else if (text.StartsWith("```"))
            text = text["```".Length..];

        if (text.EndsWith("```"))
            text = text[..^3];

        text = text.Trim();

        // Failure mode 2 — leading prose. Find where the JSON object starts.
        var jsonStart = text.IndexOf('{');
        if (jsonStart > 0)
            text = text[jsonStart..];

        // Failure mode 3 & 4 — trailing text or whitespace.
        var jsonEnd = text.LastIndexOf('}');
        if (jsonEnd >= 0 && jsonEnd < text.Length - 1)
            text = text[..(jsonEnd + 1)];

        return text;
    }
}

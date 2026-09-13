namespace GroupLab.Core.Gltd;

/// <summary>
/// TARGET-SCHEMA.md section 10 distinguishes errors, which make a definition invalid, from warnings, which flag a
/// legitimate but unusual choice such as a trimmed page (test 17).
/// </summary>
public enum Severity
{
    Warning,
    Error,
}

/// <summary>
/// One finding about a definition. <see cref="Path"/> is a JSON Pointer into the document, and
/// <see cref="Test"/> names the TARGET-SCHEMA.md section 10 conformance test where one applies.
/// </summary>
public sealed record Diagnostic(Severity Severity, string Code, string Path, string Message, string? Test = null)
{
    public static Diagnostic Error(string code, string path, string message, string? test = null) =>
        new(Severity.Error, code, path, message, test);

    public static Diagnostic Warning(string code, string path, string message, string? test = null) =>
        new(Severity.Warning, code, path, message, test);

    public override string ToString()
    {
        string where = Path.Length == 0 ? "/" : Path;
        string test = Test is null ? "" : $" (test {Test})";
        return $"{(Severity == Severity.Error ? "error" : "warning")} {Code} at {where}: {Message}{test}";
    }
}

using GroupLab.Core.Publication;

namespace GroupLab.Cli;

/// <summary>
/// <c>grouplab publish-owner &lt;source&gt; &lt;public directory&gt; --taken-by &lt;name&gt; --statement &lt;text&gt; [--hold &lt;file&gt; &lt;reason&gt;]...</c>:
/// NOTES-FROM-PLANNING.md entry 34 section 2, photographs published by their copyright holder directly, through
/// <see cref="Intake.PublishOwner"/>. The originals stay where they are; only scrubbed copies are written.
/// </summary>
internal static class OwnerVerb
{
    public static int Run(string source, string target, string[] rest, TextWriter output)
    {
        string? takenBy = null, statement = null;
        var held = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int i = 0; i < rest.Length; i++)
        {
            switch (rest[i])
            {
                case "--taken-by" when i + 1 < rest.Length:
                    takenBy = rest[++i];
                    break;
                case "--statement" when i + 1 < rest.Length:
                    statement = rest[++i];
                    break;
                case "--hold" when i + 2 < rest.Length:
                    held[rest[i + 1]] = rest[i + 2];
                    i += 2;
                    break;
                default:
                    output.WriteLine($"publish-owner: unknown or incomplete option {rest[i]}");
                    return 2;
            }
        }

        var result = Intake.PublishOwner(source, target, takenBy ?? "", statement ?? "", held);
        if (result.Refused is { } reason)
        {
            output.WriteLine($"{result.Submission}: refused, {reason}. Nothing was written.");
            return 1;
        }

        output.WriteLine($"{result.Submission}: published to {result.PublishedDirectory}");
        foreach (var file in result.Files)
        {
            output.WriteLine(file.Held is null
                ? $"  {file.StoredName}: received {file.ReceivedSha256[..12]}, published {file.PublishedSha256![..12]}; removed {(file.Removed.Count == 0 ? "nothing" : string.Join(", ", file.Removed))}"
                : $"  {file.OriginalName}: {file.Held}");
        }

        return 0;
    }
}

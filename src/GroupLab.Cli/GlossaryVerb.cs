using System.Text;
using GroupLab.Core.Marking;

namespace GroupLab.Cli;

/// <summary>
/// Writes `docs/GLOSSARY.md` from the explanations the application shows, NOTES-FROM-PLANNING.md entry 131 section 4.2.
/// <para>
/// <b>Generated rather than written, and that is the point.</b> The same words appear beside the figure on screen, in the report, and on the
/// glossary page the "More" link goes to. Three copies maintained by hand would drift, and the one that drifts is always the one nobody
/// looks at, which is the page a person reaches when they did not understand the figure.
/// </para>
/// </summary>
public static class GlossaryVerb
{
    public static int Run(string docs, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        var folder = new DirectoryInfo(docs);
        if (!folder.Exists)
        {
            error.WriteLine($"glossary: there is no {docs}");
            return 1;
        }

        var text = new StringBuilder();
        text.AppendLine("# What the figures mean");
        text.AppendLine();
        text.AppendLine("Every figure GroupLab shows, in plain words. The same text appears beside the figure in the application, so this page and");
        text.AppendLine("the screen can never say different things: both are written from one list in the source.");
        text.AppendLine();
        text.AppendLine("**Every one of these is an estimate from the shots you fired.** That is not a disclaimer, it is the single most useful thing");
        text.AppendLine("to know about them. One five shot group is not a measurement of a rifle; it is one sample of what the rifle does, and every");
        text.AppendLine("figure below moves about from group to group. Where an interval is shown beside a figure, that interval is the honest width");
        text.AppendLine("of what you actually know.");
        text.AppendLine();

        foreach (var (_, explanation) in FigureExplanations.All.OrderBy(e => e.Value.Name, StringComparer.Ordinal))
        {
            text.AppendLine($"## {explanation.Name}");
            text.AppendLine();
            text.AppendLine($"<a id=\"{explanation.Term}\"></a>");
            text.AppendLine();
            text.AppendLine(explanation.Plain);
            text.AppendLine();
        }

        string path = Path.Combine(folder.FullName, "GLOSSARY.md");
        File.WriteAllText(path, text.ToString().Replace("\r\n", "\n"), new UTF8Encoding(false));
        output.WriteLine($"Wrote {path}: {FigureExplanations.All.Count} figures.");
        return 0;
    }
}

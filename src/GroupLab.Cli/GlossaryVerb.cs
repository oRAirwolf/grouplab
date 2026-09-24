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
        text.AppendLine("# What the words mean");
        text.AppendLine();
        text.AppendLine("Every figure GroupLab shows, and every word a shooter may not know, in plain words. The same text appears wherever the word");
        text.AppendLine("does, in the application and on the website, so no two places can say different things: all are written from one list,");
        text.AppendLine("`src/GroupLab.Core/Marking/glossary.json`.");
        text.AppendLine();
        text.AppendLine("**Every one of these is an estimate from the shots you fired.** That is not a disclaimer, it is the single most useful thing");
        text.AppendLine("to know about them. One five shot group is not a measurement of a rifle; it is one sample of what the rifle does, and every");
        text.AppendLine("figure below moves about from group to group. Where an interval is shown beside a figure, that interval is the honest width");
        text.AppendLine("of what you actually know.");
        text.AppendLine();

        foreach (var term in Glossary.All.OrderBy(e => e.Name, StringComparer.Ordinal))
        {
            text.AppendLine($"## {term.Name}");
            text.AppendLine();
            text.AppendLine($"<a id=\"{term.Term}\"></a>");
            text.AppendLine();
            text.AppendLine(term.Plain);
            text.AppendLine();
            if (term.Precise is { } precise)
            {
                text.AppendLine($"*Precisely:* {precise}");
                text.AppendLine();
            }

            if (term.Article is { } article)
            {
                text.AppendLine($"More in [the research article](https://grouplab.org{article}).");
                text.AppendLine();
            }
        }

        string path = Path.Combine(folder.FullName, "GLOSSARY.md");
        File.WriteAllText(path, text.ToString().Replace("\r\n", "\n"), new UTF8Encoding(false));
        output.WriteLine($"Wrote {path}: {Glossary.All.Count} terms, {FigureExplanations.All.Count} of them figures.");
        return 0;
    }
}

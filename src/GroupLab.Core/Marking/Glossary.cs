using System.Text.Json;
using System.Text.RegularExpressions;

namespace GroupLab.Core.Marking;

/// <summary>One word a shooter may not know: its anchor, its name, the forms it is found by, and what it means.</summary>
/// <param name="Term">The anchor on the glossary page, lower case with hyphens.</param>
/// <param name="Name">What it is called.</param>
/// <param name="Words">The forms it is found by on a screen or a page, matched whole and without regard to case.</param>
/// <param name="Plain">Two or three sentences with no other jargon and no symbols.</param>
/// <param name="Precise">The exact definition, for a reader who wants it, or null.</param>
/// <param name="Article">The published research article that covers it, as a site path, or null.</param>
/// <param name="Figure">The key the analysis screen uses for it, for the terms that are figures, or null.</param>
public sealed record GlossaryTerm(string Term, string Name, IReadOnlyList<string> Words, string Plain, string? Precise, string? Article, string? Figure);

/// <summary>
/// Every word a shooter may not know, NOTES-FROM-PLANNING.md entry 154: Alan asked that anything like "sigma" that a layman would not know
/// explains itself wherever it appears. The list is <c>glossary.json</c> beside this file, and it is the only list: the application, the
/// report, <c>docs/GLOSSARY.md</c>, and the website's glossary page and tooltips all read it, so none of them can say something different.
/// </summary>
public static class Glossary
{
    /// <summary>The glossary page on the website, which the site builds from the same file.</summary>
    public const string Page = "https://grouplab.org/guides/glossary/";

    public static IReadOnlyList<GlossaryTerm> All { get; } = Load();

    private static readonly Regex AnyWord = new(
        @"(?<![\w-])(" + string.Join("|", All.SelectMany(t => t.Words).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(w => w.Length).Select(Regex.Escape)) + @")(?![\w-])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Dictionary<string, GlossaryTerm> ByWord = All
        .SelectMany(t => t.Words.Select(w => (Word: w.ToLowerInvariant(), Term: t)))
        .GroupBy(x => x.Word)
        .ToDictionary(g => g.Key, g => g.First().Term, StringComparer.OrdinalIgnoreCase);

    /// <summary>A term by its anchor, or null.</summary>
    public static GlossaryTerm? ByTerm(string term) => All.FirstOrDefault(t => string.Equals(t.Term, term, StringComparison.Ordinal));

    /// <summary>Every glossary word in a text, longest first where two overlap, with where it is.</summary>
    public static IEnumerable<(GlossaryTerm Term, int Index, int Length)> Matches(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        foreach (Match m in AnyWord.Matches(text))
        {
            yield return (ByWord[m.Value.ToLowerInvariant()], m.Index, m.Length);
        }
    }

    /// <summary>The first glossary term a text names, or null.</summary>
    public static GlossaryTerm? Find(string text) => text is null ? null : Matches(text).Select(m => m.Term).FirstOrDefault();

    /// <summary>Where a term's full entry is.</summary>
    public static string MoreAbout(GlossaryTerm term) => Page + "#" + (term ?? throw new ArgumentNullException(nameof(term))).Term;

    private static IReadOnlyList<GlossaryTerm> Load()
    {
        using var stream = typeof(Glossary).Assembly.GetManifestResourceStream("GroupLab.Core.Marking.glossary.json")
            ?? throw new InvalidOperationException("glossary.json is not embedded");
        using var doc = JsonDocument.Parse(stream);
        var terms = new List<GlossaryTerm>();
        foreach (var t in doc.RootElement.GetProperty("terms").EnumerateArray())
        {
            string? Optional(string name) => t.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            terms.Add(new GlossaryTerm(
                t.GetProperty("term").GetString()!,
                t.GetProperty("name").GetString()!,
                [.. t.GetProperty("words").EnumerateArray().Select(w => w.GetString()!)],
                t.GetProperty("plain").GetString()!,
                Optional("precise"),
                Optional("article"),
                Optional("figure")));
        }

        return terms;
    }
}

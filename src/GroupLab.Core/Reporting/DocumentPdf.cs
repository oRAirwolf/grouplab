using System.Text.RegularExpressions;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Rendering;
using GroupLab.Core.Rendering.Pdf;

namespace GroupLab.Core.Reporting;

/// <summary>A picture for a document, as a JPEG with its size in pixels.</summary>
public sealed record DocumentImage(byte[] Jpeg, int PixelWidth, int PixelHeight);

/// <summary>
/// A Markdown document as a PDF from GroupLab's own writer, NOTES-FROM-PLANNING.md entry 113 section 6: the project's documents meant for
/// reading ship as PDF beside their Markdown, and the user guide is illustrated, so this takes the subset those documents use: headings, bullets,
/// numbered steps, paragraphs and pictures on lines of their own. Inline code, bold and link markup are set as plain text, a link with its
/// address after it. Letter pages, numbered, and anything that does not fit continues on the next page.
/// </summary>
public static partial class DocumentPdf
{
    private const long Inch = 508;
    private const long PageWidth = 17 * Inch / 2;
    private const long PageHeight = 11 * Inch;
    private const long Margin = 3 * Inch / 4;

    private static readonly Rgb Ink = new(20, 20, 20);
    private static readonly Rgb Grey = new(70, 70, 70);
    private static readonly Rgb Rule = new(190, 190, 190);

    private static long Pt(double points) => (long)Math.Round(points * Inch / 72);

    /// <summary>The text as it reads in print: code ticks and bold markers dropped, a link as its text with its address after it.</summary>
    public static string Inline(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string linked = Link().Replace(text, m => m.Groups["text"].Value == m.Groups["url"].Value || m.Groups["url"].Value.StartsWith('#')
            ? m.Groups["text"].Value
            : $"{m.Groups["text"].Value} ({m.Groups["url"].Value})");
        return linked.Replace("**", "", StringComparison.Ordinal).Replace("`", "", StringComparison.Ordinal);
    }

    /// <summary>The picture paths a document names on lines of their own, in order.</summary>
    public static IReadOnlyList<string> Pictures(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        return [.. markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').Select(l => Picture().Match(l.Trim())).Where(m => m.Success).Select(m => m.Groups["path"].Value)];
    }

    public static byte[] Write(string markdown, Func<string, DocumentImage?> images) => PdfWriter.Write(Pages(markdown, images));

    public static IReadOnlyList<Scene> Pages(string markdown, Func<string, DocumentImage?> images)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentNullException.ThrowIfNull(images);
        var pages = new List<List<SceneItem>> { new() };
        long y = Margin, width = PageWidth - (2 * Margin);
        void Need(long height)
        {
            if (y + height > PageHeight - Margin && y > Margin)
            {
                pages.Add([]);
                y = Margin;
            }
        }

        void Lines(string text, long size, Rgb colour, long indent = 0, string? marker = null)
        {
            var wrapped = ReportWriter.Wrap(Inline(text), size, width - indent);
            for (int i = 0; i < wrapped.Count; i++)
            {
                Need(size * 5 / 4);
                y += size * 5 / 4;
                if (i == 0 && marker is not null)
                {
                    pages[^1].Add(new TextRun(SceneLayer.Labels, colour, Margin + indent - Pt(14), y - (size / 4), size, marker, TextAnchor.Left));
                }

                pages[^1].Add(new TextRun(SceneLayer.Labels, colour, Margin + indent, y - (size / 4), size, wrapped[i], TextAnchor.Left));
            }
        }

        var paragraph = new List<string>();
        void Flush()
        {
            if (paragraph.Count > 0)
            {
                Lines(string.Join(" ", paragraph), Pt(10), Grey);
                y += Pt(5);
                paragraph.Clear();
            }
        }

        foreach (string raw in markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            string line = raw.TrimEnd();
            var picture = Picture().Match(line.Trim());
            var numbered = Numbered().Match(line);
            if (line.Length == 0)
            {
                Flush();
            }
            else if (picture.Success)
            {
                Flush();
                if (images(picture.Groups["path"].Value) is { } image)
                {
                    long height = width * image.PixelHeight / image.PixelWidth;
                    Need(height + Pt(20));
                    pages[^1].Add(new ImageBox(SceneLayer.Labels, Margin, y + Pt(4), width, height, image.Jpeg, image.PixelWidth, image.PixelHeight));
                    y += height + Pt(4);
                    Lines(picture.Groups["alt"].Value, Pt(8), Grey);
                    y += Pt(6);
                }
            }
            else if (line.StartsWith('#'))
            {
                Flush();
                int level = line.TakeWhile(c => c == '#').Count();
                long size = level switch { 1 => Pt(20), 2 => Pt(14), _ => Pt(11.5) };
                Need(size * 3);
                y += level == 1 ? 0 : Pt(8);
                Lines(line[level..].Trim(), size, Ink);
                if (level <= 2)
                {
                    pages[^1].Add(new RectFill(SceneLayer.Labels, Rule, Margin, y + 6, width, 3));
                }

                y += Pt(5);
            }
            else if (line.TrimStart().StartsWith("- ", StringComparison.Ordinal))
            {
                Flush();
                int depth = (line.Length - line.TrimStart().Length) / 2;
                Lines(line.TrimStart()[2..], Pt(10), Grey, Pt(16) * (depth + 1), "-");
                y += Pt(2);
            }
            else if (numbered.Success)
            {
                Flush();
                Lines(numbered.Groups["text"].Value, Pt(10), Grey, Pt(18), numbered.Groups["n"].Value + ".");
                y += Pt(2);
            }
            else
            {
                paragraph.Add(line.Trim());
            }
        }

        Flush();
        int count = pages.Count;
        return [.. pages.Select((items, i) =>
        {
            items.Add(new TextRun(SceneLayer.Name, Grey, PageWidth - Margin, PageHeight - (Margin / 2), Pt(8), FormattableString.Invariant($"page {i + 1} of {count}"), TextAnchor.Right));
            return new Scene(PageWidth, PageHeight, 0, items);
        })];
    }

    [GeneratedRegex(@"\[(?<text>[^\]]+)\]\((?<url>[^)]+)\)")]
    private static partial Regex Link();

    [GeneratedRegex(@"^!\[(?<alt>[^\]]*)\]\((?<path>[^)]+)\)$")]
    private static partial Regex Picture();

    [GeneratedRegex(@"^(?<n>\d+)\. (?<text>.+)$")]
    private static partial Regex Numbered();
}

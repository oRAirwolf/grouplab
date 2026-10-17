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
    private const long LetterWidth = 17 * Inch / 2;
    private const long LetterHeight = 11 * Inch;

    /// <summary>A4, 210 by 297 mm, in the same half-dmm units: entry 438's one-page summary comes in both sizes.</summary>
    private const long A4Width = 4200;
    private const long A4Height = 5940;
    private const long Margin = 3 * Inch / 4;

    private static readonly Rgb Ink = new(20, 20, 20);
    private static readonly Rgb Grey = new(70, 70, 70);
    private static readonly Rgb Rule = new(190, 190, 190);

    private static long Pt(double points) => (long)Math.Round(points * Inch / 72);

    /// <summary>The text as it reads in print: code ticks and bold markers dropped, a link as its text with its address after it.</summary>
    public static string Inline(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        // Entry 438: a link within the document or into another guide (USER-GUIDE.md#...) is set as its words alone.
        string linked = Link().Replace(text, m => m.Groups["text"].Value == m.Groups["url"].Value || m.Groups["url"].Value.StartsWith('#')
            || m.Groups["url"].Value.Contains(".md#", StringComparison.Ordinal)
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

    /// <summary>The PDF, with a bookmark for every section and subsection (entry 436 section 4.2).</summary>
    public static byte[] Write(string markdown, Func<string, DocumentImage?> images, bool a4 = false)
    {
        var (pages, bookmarks) = Lay(markdown, images, a4);
        return PdfWriter.Write(pages, bookmarks);
    }

    public static IReadOnlyList<Scene> Pages(string markdown, Func<string, DocumentImage?> images, bool a4 = false) => Lay(markdown, images, a4).Pages;

    /// <summary>The headings at levels 2 and 3, where each one starts, as the PDF's bookmarks.</summary>
    public static IReadOnlyList<PdfBookmark> Bookmarks(string markdown) => Lay(markdown, _ => null, false).Bookmarks;

    private static (IReadOnlyList<Scene> Pages, IReadOnlyList<PdfBookmark> Bookmarks) Lay(string markdown, Func<string, DocumentImage?> images, bool a4)
    {
        long pageWidth = a4 ? A4Width : LetterWidth, pageHeight = a4 ? A4Height : LetterHeight;
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentNullException.ThrowIfNull(images);
        var pages = new List<List<SceneItem>> { new() };
        var bookmarks = new List<PdfBookmark>();
        long y = Margin, width = pageWidth - (2 * Margin);
        void Need(long height)
        {
            if (y + height > pageHeight - Margin && y > Margin)
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

        // Entry 438: a table, its columns sharing the width, the header row in ink with a rule under it and the rest in grey.
        var table = new List<string[]>();
        void Table()
        {
            if (table.Count == 0)
            {
                return;
            }

            int columns = table.Max(r => r.Length);
            long column = width / columns, size = Pt(9.5), line = size * 5 / 4;
            y += Pt(4);
            for (int r = 0; r < table.Count; r++)
            {
                var cells = Enumerable.Range(0, columns)
                    .Select(c => c < table[r].Length ? ReportWriter.Wrap(Inline(table[r][c]), size, column - Pt(8)) : (IReadOnlyList<string>)[]).ToList();
                int rows = Math.Max(1, cells.Max(c => c.Count));
                Need((rows * line) + Pt(6));
                for (int c = 0; c < columns; c++)
                {
                    for (int k = 0; k < cells[c].Count; k++)
                    {
                        pages[^1].Add(new TextRun(SceneLayer.Labels, r == 0 ? Ink : Grey, Margin + (c * column), y + ((k + 1) * line) - (size / 4), size, cells[c][k], TextAnchor.Left));
                    }
                }

                y += (rows * line) + Pt(4);
                if (r == 0)
                {
                    pages[^1].Add(new RectFill(SceneLayer.Labels, Rule, Margin, y - Pt(2), width, 3));
                }
            }

            y += Pt(6);
            table.Clear();
        }

        foreach (string raw in markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            string line = raw.TrimEnd();
            var picture = Picture().Match(line.Trim());
            var numbered = Numbered().Match(line);
            if (line.StartsWith('|'))
            {
                Flush();
                if (line.Trim('|', ' ', '-', ':').Length > 0)
                {
                    table.Add([.. line.Trim().Trim('|').Split('|').Select(c => c.Trim())]);
                }

                continue;
            }

            Table();
            if (line.Length == 0)
            {
                Flush();
            }
            else if (line.Trim() == "<!-- math -->")
            {
                // Entry 438: the page folds these parts under this label; on paper the label is all that marks them.
                Flush();
                Lines("The math, if you want it", Pt(10), Ink);
            }
            else if (line.StartsWith("<!--", StringComparison.Ordinal))
            {
                // A note for the site, such as entry 437's line naming a picture's markings; GuideVerb draws them into the picture.
            }
            else if (picture.Success)
            {
                Flush();
                if (images(picture.Groups["path"].Value) is { } image)
                {
                    // Entry 437 section 1.1: a phone's tall picture is set at most half a page high, at its own proportions, not stretched across.
                    long height = Math.Min(width * image.PixelHeight / image.PixelWidth, pageHeight / 2);
                    long shown = height * image.PixelWidth / image.PixelHeight;
                    Need(height + Pt(20));
                    pages[^1].Add(new ImageBox(SceneLayer.Labels, Margin + ((width - shown) / 2), y + Pt(4), shown, height, image.Jpeg, image.PixelWidth, image.PixelHeight));
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
                if (level is 2 or 3)
                {
                    bookmarks.Add(new PdfBookmark(Inline(line[level..].Trim()), level, pages.Count - 1, y));
                }

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
        Table();
        int count = pages.Count;
        return ([.. pages.Select((items, i) =>
        {
            items.Add(new TextRun(SceneLayer.Name, Grey, pageWidth - Margin, pageHeight - (Margin / 2), Pt(8), FormattableString.Invariant($"page {i + 1} of {count}"), TextAnchor.Right));
            return new Scene(pageWidth, pageHeight, 0, items);
        })], bookmarks);
    }

    [GeneratedRegex(@"\[(?<text>[^\]]+)\]\((?<url>[^)]+)\)")]
    private static partial Regex Link();

    [GeneratedRegex(@"^!\[(?<alt>[^\]]*)\]\((?<path>[^)]+)\)$")]
    private static partial Regex Picture();

    [GeneratedRegex(@"^(?<n>\d+)\. (?<text>.+)$")]
    private static partial Regex Numbered();
}

using System.Text.Json;
using System.Text.RegularExpressions;
using GroupLab.Core.Reporting;
using OpenCvSharp;

namespace GroupLab.Cli;

/// <summary>
/// <c>grouplab user-guide</c>, NOTES-FROM-PLANNING.md entry 113 section 6 and entry 116 section 5: docs/USER-GUIDE.pdf and
/// docs/TESTING-GUIDE.pdf from their Markdown, which is the source, with every picture they name read from the committed renders, scaled to at
/// most 1400 pixels across and set as JPEG. Run it after the renders are regenerated, and commit the PDFs with them.
/// </summary>
public static partial class GuideVerb
{
    public const string Usage = "grouplab user-guide [<docs directory>]";

    /// <summary>The documents that ship as a PDF beside their Markdown.</summary>
    public static IReadOnlyList<string> Guides { get; } = ["USER-GUIDE", "TESTING-GUIDE", "STATISTICS-PRIMER"];

    /// <summary>Pages printed and taken to the range, made the same way, one page each: entry 235's plan for the hole size test.</summary>
    public static IReadOnlyList<string> Printables { get; } = ["RANGE-PLAN-HOLE-SIZE"];

    public static int Run(string docs, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        foreach (string guide in Guides.Concat(Printables))
        {
            string source = Path.Combine(docs, guide + ".md");
            if (!File.Exists(source))
            {
                error.WriteLine($"user-guide: there is no {source}");
                return 2;
            }

            string markdown = File.ReadAllText(source);
            var missing = DocumentPdf.Pictures(markdown).Where(p => !File.Exists(Path.Combine(docs, p))).ToList();
            if (missing.Count > 0)
            {
                error.WriteLine($"user-guide: {guide}.md names pictures that are not there: {string.Join(", ", missing)}");
                return 1;
            }

            var marks = Marks(markdown);
            byte[] pdf = DocumentPdf.Write(markdown, path => Picture(Path.Combine(docs, path), marks.GetValueOrDefault(path)));
            string target = Path.Combine(docs, guide + ".pdf");
            File.WriteAllBytes(target, pdf);
            output.WriteLine($"Wrote {target}, {pdf.Length / 1024} KB.");
            if (guide == "STATISTICS-PRIMER")
            {
                foreach (var (paper, a4) in new[] { ("LETTER", false), ("A4", true) })
                {
                    string summary = Path.Combine(docs, $"STATISTICS-PRIMER-SUMMARY-{paper}.pdf");
                    File.WriteAllBytes(summary, DocumentPdf.Write(Summary(markdown), _ => null, a4));
                    output.WriteLine($"Wrote {summary}.");
                }
            }
        }

        return 0;
    }

    /// <summary>
    /// The controls each picture marks, NOTES-FROM-PLANNING.md entry 437 section 2.2: the line under a picture, "&lt;!-- marks: Find holes |
    /// the review queue --&gt;", in the order the text mentions them.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> Marks(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        return MarksLine().Matches(markdown.Replace("\r\n", "\n", StringComparison.Ordinal))
            .ToDictionary(m => m.Groups["path"].Value, m => (IReadOnlyList<string>)[.. m.Groups["names"].Value.Split('|').Select(n => n.Trim()).Where(n => n.Length > 0)]);
    }

    /// <summary>
    /// Entry 438 section 4.1: the statistics primer's one-page summary, made from the guide itself: each paragraph marked
    /// "&lt;!-- summary --&gt;" under its section's heading, numbered, with where the whole guide is.
    /// </summary>
    public static string Summary(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var items = new List<string>();
        string heading = "";
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("## ", StringComparison.Ordinal))
            {
                heading = lines[i][3..].Trim();
            }
            else if (lines[i].Trim() == "<!-- summary -->")
            {
                var paragraph = lines.Skip(i + 1).TakeWhile(l => l.Trim().Length > 0).Select(l => l.Trim());
                string stop = heading.EndsWith('?') || heading.EndsWith('!') ? "" : ".";
                string item = $"{items.Count + 1}. **{heading}{stop}** {string.Join(" ", paragraph)}";
                // What to do about it: the section's own "At the range" advice, beneath the point.
                var section = lines.Skip(i + 1).TakeWhile(l => !l.StartsWith("## ", StringComparison.Ordinal)).ToList();
                int advice = section.FindIndex(l => l.StartsWith("**At the range.**", StringComparison.Ordinal));
                if (advice >= 0)
                {
                    item += "\n  - " + string.Join(" ", section.Skip(advice).TakeWhile(l => l.Trim().Length > 0).Select(l => l.Trim()));
                }

                items.Add(item);
            }
        }

        return "# Statistics for shooters: five things to know\n\n" + string.Join("\n\n", items)
            + "\n\nThe whole guide, with pictures and the numbers behind every sentence: grouplab.org/guides/statistics-primer/\n";
    }

    /// <summary>A render as a JPEG no wider than 1400 pixels and no taller than 1100, with the controls it marks outlined and numbered as the site draws them.</summary>
    public static DocumentImage? Picture(string path, IReadOnlyList<string>? marks = null)
    {
        using var image = Cv2.ImRead(path, ImreadModes.Color);
        if (image.Empty())
        {
            return null;
        }

        if (marks is { Count: > 0 })
        {
            Mark(image, Regex.Replace(path, @"-(light|dark)-1400x900\.png$", "-bounds.json"), marks);
        }

        // At most 1400 across, and at most 1100 high: a phone's tall picture is set half a page high (DocumentPdf), about 200 dots an inch.
        double k = Math.Min(1.0, Math.Min(1400.0 / image.Width, 1100.0 / image.Height));
        using var scaled = k < 1 ? image.Resize(new Size((int)(image.Width * k), (int)(image.Height * k)), 0, 0, InterpolationFlags.Area) : image.Clone();
        Cv2.ImEncode(".jpg", scaled, out byte[] jpeg, new ImageEncodingParam(ImwriteFlags.JpegQuality, 85));
        return new DocumentImage(jpeg, scaled.Width, scaled.Height);
    }

    /// <summary>
    /// Entry 437 section 3.5: in the PDF the markings are drawn into the picture from the same positions file the site reads: an amber
    /// outline with a white halo, the paper's color, and an amber badge with its number in dark ink on the outline's top left corner, moved
    /// inside the picture at its edge and along the outline when it would sit on another, as the site places them.
    /// </summary>
    private static void Mark(Mat image, string bounds, IReadOnlyList<string> marks)
    {
        if (!File.Exists(bounds))
        {
            return;
        }

        using var file = JsonDocument.Parse(File.ReadAllText(bounds));
        var controls = file.RootElement.GetProperty("controls");
        double k = image.Width / file.RootElement.GetProperty("width").GetDouble();
        int w = file.RootElement.GetProperty("width").GetInt32(), h = file.RootElement.GetProperty("height").GetInt32();
        var amber = new Scalar(47, 145, 224);
        var ink = new Scalar(10, 18, 23);
        const int Badge = 22;
        var placed = new List<(int X, int Y)>();
        for (int n = 1; n <= marks.Count; n++)
        {
            if (!controls.TryGetProperty(marks[n - 1], out var box))
            {
                continue;
            }

            int x = box[0].GetInt32(), y = box[1].GetInt32(), bw = box[2].GetInt32(), bh = box[3].GetInt32();
            int bx = x, by = y;
            for (int i = 0; i < 12; i++)
            {
                bx = Math.Clamp(bx, Badge, w - Badge);
                by = Math.Clamp(by, Badge, h - Badge);
                if (placed.All(p => Math.Abs(bx - p.X) >= 2 * Badge || Math.Abs(by - p.Y) >= 2 * Badge))
                {
                    break;
                }

                if (bx + (2 * Badge) + 4 <= x + bw)
                {
                    bx += (2 * Badge) + 4;
                }
                else
                {
                    by += (2 * Badge) + 4;
                }
            }

            placed.Add((bx, by));
            var outline = new Rect((int)(x * k), (int)(y * k), (int)(bw * k), (int)(bh * k));
            Cv2.Rectangle(image, outline, Scalar.White, (int)Math.Round(8 * k), LineTypes.AntiAlias);
            Cv2.Rectangle(image, outline, amber, (int)Math.Round(5 * k), LineTypes.AntiAlias);
            var centre = new Point(bx * k, by * k);
            Cv2.Circle(image, centre, (int)Math.Round((Badge + 2) * k), Scalar.White, -1, LineTypes.AntiAlias);
            Cv2.Circle(image, centre, (int)Math.Round(Badge * k), amber, -1, LineTypes.AntiAlias);
            string digit = n.ToString(System.Globalization.CultureInfo.InvariantCulture);
            double size = 1.0 * k;
            var text = Cv2.GetTextSize(digit, HersheyFonts.HersheyDuplex, size, (int)Math.Max(1, Math.Round(2 * k)), out _);
            Cv2.PutText(image, digit, new Point(centre.X - (text.Width / 2), centre.Y + (text.Height / 2)), HersheyFonts.HersheyDuplex, size, ink,
                (int)Math.Max(1, Math.Round(2 * k)), LineTypes.AntiAlias);
        }
    }

    [GeneratedRegex(@"^!\[[^\]]*\]\((?<path>[^)]+)\)[ \t]*\n<!-- marks: (?<names>.*?) -->", RegexOptions.Multiline)]
    private static partial Regex MarksLine();
}

using System.Globalization;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Imaging;
using GroupLab.Core.Rendering;

namespace GroupLab.Core.ScaleMarkers;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 372: a scale label, printed on a thermal label printer (Alan's Phomemo M220) and stuck flat on a target
/// where it will not be shot. Its codes run in rows across the label's width only, across the printhead, whose dot pitch is fixed; the feed
/// along the paper is not trusted, so two rows are two separate pieces and no length along the feed is used.
/// <para>
/// Each code is 8 mm, 8 modules of 1 mm (8 dots at 203 dpi), with a millimetre of white round it; a row's two codes sit 5 mm in from each
/// side, so their centres are the label's width less 10 mm apart. A label's width decides that spacing, so each width has its own block
/// of ten tag36h11 identifiers, 470 to 549, below entry 365's markers and above every sheet's: the reader knows the spacing from the
/// identifier alone. Within a block, a running serial gives each label its own pair, so two labels on one target are told apart, until the
/// block comes round again after five rows.
/// </para>
/// </summary>
public static class ScaleLabels
{
    /// <summary>The label widths a layout is made for, millimetres: the M220's range, 20 to 75.</summary>
    public static readonly int[] Widths = [20, 25, 30, 40, 50, 60, 70, 75];

    public const int First = 470, Block = 10, Code = 8, Inset = 5;

    public static int Last => First + (Widths.Length * Block) - 1;

    /// <summary>The two sizes first (entry 372): 50 by 30 mm, and 70 by 80 mm, the roll Alan has loaded.</summary>
    public static readonly (int Width, int Height)[] Sizes = [(70, 80), (50, 30), (20, 30), (25, 25), (30, 20), (40, 30), (40, 60), (60, 40), (75, 50)];

    public static bool IsLabel(int id) => id >= First && id <= Last;

    /// <summary>The code an identifier is: its row (the piece), and its centre across the row, millimetres.</summary>
    public static MarkerTag? Tag(int id)
    {
        if (!IsLabel(id))
        {
            return null;
        }

        int width = Widths[(id - First) / Block], within = (id - First) % Block;
        int pair = within / 2;
        double x = within % 2 == 0 ? Inset : width - Inset;
        // A row is a piece: its block and pair number, so the two codes of one row are one rigid body.
        return new MarkerTag(id, MarkerKind.Label, (((id - First) / Block) * 10) + pair + 1, new PointD(x, 0), Code);
    }

    /// <summary>How many rows of codes a label of this height holds, one or two: a row is 10 mm with its white, and the line under it 4.</summary>
    public static int Rows(int height) => height >= 26 ? 2 : 1;

    /// <summary>The identifiers of a label's rows for its serial: pairs within its width's block, coming round after five rows.</summary>
    public static IReadOnlyList<(int Left, int Right)> Pairs(int width, int height, int serial)
    {
        int block = Array.IndexOf(Widths, width);
        if (block < 0)
        {
            throw new ArgumentException($"There is no scale label layout {width} mm wide; the widths are {string.Join(", ", Widths)}.", nameof(width));
        }

        int rows = Rows(height);
        return [.. Enumerable.Range(0, rows).Select(r => ((serial * rows) + r) % (Block / 2)).Select(p => (First + (block * Block) + (2 * p), First + (block * Block) + (2 * p) + 1))];
    }

    /// <summary>
    /// The labels <paramref name="count"/> from <paramref name="serial"/> on, a page each, exactly the label's size: the rows of codes and the
    /// line "GroupLab scale label S12 · M220". Nothing on it is round, so no part of it can be taken for a hole.
    /// </summary>
    public static IReadOnlyList<Scene> Pages(int width, int height, int serial, int count, string printer)
    {
        var pages = new List<Scene>();
        const double U = 20;
        var black = new Rgb(0, 0, 0);
        for (int n = 0; n < count; n++)
        {
            var items = new List<SceneItem>();
            var pairs = Pairs(width, height, serial + n);
            int rows = pairs.Count;
            double rowPitch = 10.5, top = Math.Max(1, (height - 4 - (rows * rowPitch)) / 2);
            for (int r = 0; r < rows; r++)
            {
                double y = top + 1 + (Code / 2.0) + (r * rowPitch);
                foreach (var (id, x) in new[] { (pairs[r].Left, (double)Inset), (pairs[r].Right, (double)(width - Inset)) })
                {
                    var inked = ScaleMarkerLayout.Inked(id);
                    long module = (long)(Code * U / ScaleMarkerLayout.Modules), left = (long)Math.Round((x - (Code / 2.0)) * U), topU = (long)Math.Round((y - (Code / 2.0)) * U);
                    for (int row = 0; row < ScaleMarkerLayout.Modules; row++)
                    {
                        for (int col = 0; col < ScaleMarkerLayout.Modules;)
                        {
                            if (!inked[row, col])
                            {
                                col++;
                                continue;
                            }

                            int first = col;
                            while (col < ScaleMarkerLayout.Modules && inked[row, col])
                            {
                                col++;
                            }

                            items.Add(new RectFill(SceneLayer.Markers, black, left + (first * module), topU + (row * module), (col - first) * module, module)
                            {
                                Module = new ModuleCell(module, ScaleMarkerLayout.Modules, first, row),
                            });
                        }
                    }
                }
            }

            double size = Math.Min(2.4, (width - 2) / 22.0);
            string line = string.Create(CultureInfo.InvariantCulture, $"GroupLab scale label S{serial + n} · {printer}");
            items.Add(new TextRun(SceneLayer.Labels, black, (long)Math.Round(width / 2.0 * U), (long)Math.Round((height - 1.5) * U), (long)Math.Round(size * U), line, TextAnchor.Centre));
            pages.Add(new Scene((long)Math.Round(width * U), (long)Math.Round(height * U), 0, items));
        }

        return pages;
    }
}

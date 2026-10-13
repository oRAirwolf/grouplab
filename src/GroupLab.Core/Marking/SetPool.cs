using System.Collections.Immutable;
using System.Globalization;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;

namespace GroupLab.Core.Marking;

/// <summary>What pooling the sheets of one set found: which sheets are here, missing or read twice, the bulls the set holds, and the group.</summary>
public sealed record SetPoolResult(int SetSize, IReadOnlyList<int> Found, IReadOnlyList<int> Missing, IReadOnlyList<int> Repeated, int BullsInSet,
    int Shots, GroupFigures? Figures, string Said);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 243 section 3.1: a set of sheets from "Made for your optic" analyzed as one group. The sheets are scanned or
/// photographed in any order; each one's codes say which of the set it is (<see cref="MarkingState.SetSheet"/>), so the set knows how
/// many sheets it has and how many shots to expect, and says which sheets are still missing. The group is every shot about its own bull on
/// its own sheet, docs/STATISTICS.md's rule for groups shot at separate aim points. A sheet read twice counts once.
/// </summary>
public static class SetPool
{
    public static SetPoolResult Pool(TargetDefinition definition, IReadOnlyList<MarkingState> sheets)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(sheets);
        int size = definition.Tiling is { } set ? set.Cols * set.Rows : 1;
        int bulls = definition.Bulls.Count(b => b.Scoring) * size;
        var read = sheets.Where(s => s.SetSheet is not null).ToList();
        var found = read.Select(s => s.SetSheet!.Value).Distinct().Order().ToList();
        var repeated = read.GroupBy(s => s.SetSheet!.Value).Where(g => g.Count() > 1).Select(g => g.Key).Order().ToList();
        var missing = Enumerable.Range(0, size).Except(found).ToList();
        var once = read.GroupBy(s => s.SetSheet!.Value).Select(g => g.First()).ToList();
        var figures = GroupAnalysis.Pooled(once);
        int shots = figures?.Shots ?? 0;
        // Entry 358 section 3: a set of labels says label, not sheet.
        string noun = PageSizes.IsLabel(definition.Page.Size) ? "label" : "sheet";
        return new SetPoolResult(size, found, missing, repeated, bulls, shots, figures, Words(size, found, missing, repeated, bulls, shots, noun));
    }

    /// <summary>How far apart the sheets are laid in <see cref="Composite"/>, page dmm: a page's width and an inch, so no two bulls meet.</summary>
    private const double SheetGap = 254;

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 420 section 1: the pooled set as one marking, so the analysis screen shows it as it shows a sheet. Each
    /// sheet read (once, as <see cref="Pool"/> counts it) is laid beside the last on one page, its bulls where they print and each shot at
    /// its own offset from its own bull in inches, as its sheet measured it with that sheet's own scale; so every figure the screen works
    /// out from the marking is the pooled group's. A bull is named sheet.bull, "2.5" for bull 5 on sheet 2, which is how the Shots list says
    /// which sheet each shot came from, and <see cref="SheetOf"/> reads the sheet back from a bull's index.
    /// </summary>
    public static MarkingState Composite(TargetDefinition definition, IReadOnlyList<MarkingState> sheets)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(sheets);
        var once = sheets.Where(s => s.SetSheet is not null && s.Scale is not null).GroupBy(s => s.SetSheet!.Value).OrderBy(g => g.Key).Select(g => g.First()).ToList();
        double across = definition.Page.Width + SheetGap;
        var bulls = ImmutableList.CreateBuilder<BullAim>();
        var shots = ImmutableList.CreateBuilder<MarkedShot>();
        int next = 1;
        foreach (var sheet in once)
        {
            int k = sheet.SetSheet!.Value;
            var scale = sheet.Scale!;
            PointD Page(PointD image) => scale.ToTarget(image) is var t ? new PointD((k * across) + (t.X * 254), t.Y * 254) : default;
            var at = new Dictionary<int, PointD>();
            foreach (var bull in sheet.Bulls)
            {
                at[bull.Index] = Page(bull.Image);
                bulls.Add(new BullAim(Index(k, bull.Index), string.Create(CultureInfo.InvariantCulture, $"{k + 1}.{bull.Label}"), at[bull.Index], bull.Scoring));
            }

            var marked = sheet.Shots.Where(s => s.IsShot).ToList();
            var offsets = GroupAnalysis.CompositeOffsets(sheet, marked);
            for (int i = 0; i < marked.Count; i++)
            {
                var shot = marked[i];
                var image = shot.Bull is { } b && at.TryGetValue(b, out var origin) && offsets.Count == marked.Count
                    ? new PointD(origin.X + (offsets[i].X * 254), origin.Y + (offsets[i].Y * 254))
                    : Page(shot.Image);
                shots.Add(new MarkedShot(next++, image, shot.Provenance, shot.Exclusion, false, shot.Bull is { } own ? Index(k, own) : null, shot.BullChosen, shot.MeasuredDiameterInches));
            }
        }

        // The sheets measured in real inches stay real; one that was not makes the whole set sheet inches, as a sheet's own figures say.
        bool real = once.Count > 0 && once.All(s => s.Scale is SheetReference { RealInches: true });
        var identity = new HomographyMapping(new Homography([1, 0, 0, 0, 1, 0, 0, 0, 1]));
        string summary = string.Create(CultureInfo.InvariantCulture, $"{once.Count} sheet{(once.Count == 1 ? "" : "s")} of {definition.Name} pooled, each shot about its own bull with its own sheet's scale");
        T? Same<T>(Func<MarkingState, T?> of) => once.Select(of).Distinct().ToList() is [var only] ? only : default;
        return new MarkingState(null, new SheetReference(identity, summary) { PrintScale = real ? 1 : null }, null, bulls.ToImmutable(), shots.ToImmutable(), next,
            summary, Calibre: once.Select(s => s.Calibre).FirstOrDefault(c => c is not null), ShotDistanceInches: Same(s => s.ShotDistanceInches),
            Rifle: Same(s => s.Rifle), Barrel: Same(s => s.Barrel), Load: Same(s => s.Load),
            SheetLabel: string.Create(CultureInfo.InvariantCulture, $"{definition.Name}, {once.Count} of {(definition.Tiling is { } set ? set.Cols * set.Rows : 1)} sheets pooled"));
    }

    /// <summary>Entry 420: which sheet of the set a bull of <see cref="Composite"/>'s marking is on, from its index, counting from 0.</summary>
    public static int SheetOf(int bullIndex) => bullIndex / BullsASheet;

    private const int BullsASheet = 10_000;

    private static int Index(int sheet, int bull) => (sheet * BullsASheet) + bull;

    private static string Words(int size, IReadOnlyList<int> found, IReadOnlyList<int> missing, IReadOnlyList<int> repeated, int bulls, int shots, string noun)
    {
        static string List(IEnumerable<int> sheets)
        {
            var n = sheets.Select(s => (s + 1).ToString(CultureInfo.InvariantCulture)).ToList();
            return n.Count switch
            {
                0 => "",
                1 => n[0],
                _ => string.Join(", ", n.Take(n.Count - 1)) + " and " + n[^1],
            };
        }

        string said = found.Count == 0
            ? string.Create(CultureInfo.InvariantCulture, $"None of the {size} {noun}s of this set has been read yet.")
            : missing.Count == 0
                ? string.Create(CultureInfo.InvariantCulture, $"All {size} {noun}s of the set are here.")
                : string.Create(CultureInfo.InvariantCulture, $"{Upper(noun)}{(found.Count == 1 ? "" : "s")} {List(found)} of {size} {(found.Count == 1 ? "is" : "are")} here; {List(missing)} {(missing.Count == 1 ? "is" : "are")} still missing.");
        said += string.Create(CultureInfo.InvariantCulture, $" {shots} shot{(shots == 1 ? "" : "s")} pooled, of the {bulls} bulls the set holds, one shot to a bull.");
        if (repeated.Count > 0)
        {
            said += string.Create(CultureInfo.InvariantCulture, $" {Upper(noun)}{(repeated.Count == 1 ? "" : "s")} {List(repeated)} {(repeated.Count == 1 ? "was" : "were")} read more than once and count{(repeated.Count == 1 ? "s" : "")} once.");
        }

        return said;
    }

    private static string Upper(string word) => char.ToUpperInvariant(word[0]) + word[1..];
}

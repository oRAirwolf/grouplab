using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;

namespace GroupLab.Core.Evaluation;

/// <summary>What lies under a place on a store-bought blank, as the scanner's colour shows it.</summary>
public enum BlankPlace : byte
{
    Paper,
    Ink,
    RingLine,
    Red,
    Other,
}

/// <summary>One synthetic hole on a real blank: where, what it sits on, and whether it is half of a touching pair.</summary>
public sealed record BlankHole(PointD Image, BlankPlace On, bool Touching);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 331 section 1.2: synthetic holes rendered into the scans of real store-bought blanks (request 58), at known
/// places, so <see cref="AnyTargetHoleFinder"/> can be scored on the real printing before the real shot scans arrive. The blanks and the
/// pictures made from them stay on the computer they were scanned on: this builds a picture in memory from a blank the caller loaded, and
/// nothing is written. Several calibres, a touching pair, holes on paper, on ink, on ring lines and on the red centres, and on a reactive
/// target the bright halo a Shoot-N-C hit shows.
/// </summary>
public static partial class Scoreboard
{
    /// <summary>The calibres each blank is shot with, one picture each.</summary>
    public static IReadOnlyList<double> BlankCalibres { get; } = [0.224, 0.308, 0.45];

    /// <summary>The seeds each calibre's picture is drawn with.</summary>
    public static IReadOnlyList<int> BlankSeeds { get; } = [331, 332];

    /// <summary>How many holes go on each kind of place, where the blank has any of it, and touching pairs besides.</summary>
    private const int PerPlace = 4, Pairs = 2;

    /// <summary>
    /// A picture of the blank with holes of <paramref name="calibreInches"/> at known places. <paramref name="places"/> says, pixel by pixel,
    /// what lies there; <paramref name="reactive"/> paints a hit's bright halo on ink, as a Shoot-N-C target shows.
    /// </summary>
    public static (GrayImage Picture, IReadOnlyList<BlankHole> Holes) BlankPicture(GrayImage blank, BlankPlace[] places, double dpi, int seed, double calibreInches, bool reactive)
    {
        ArgumentNullException.ThrowIfNull(blank);
        ArgumentNullException.ThrowIfNull(places);
        int w = blank.Width, h = blank.Height;
        var random = new Random(seed);
        var render = (byte[])blank.Pixels.Clone();
        double margin = 0.4 * dpi, apart = 2.2 * calibreInches * dpi;
        var chosen = new List<BlankHole>();

        bool Free(double x, double y, double least) => x > margin && y > margin && x < w - margin && y < h - margin
            && chosen.All(c => Math.Sqrt(Math.Pow(c.Image.X - x, 2) + Math.Pow(c.Image.Y - y, 2)) >= least);

        foreach (var kind in new[] { BlankPlace.Paper, BlankPlace.Ink, BlankPlace.RingLine, BlankPlace.Red })
        {
            var candidates = Enumerable.Range(0, places.Length).Where(i => places[i] == kind).ToList();
            for (int placed = 0, attempt = 0; placed < PerPlace && candidates.Count > 0 && attempt < 5000; attempt++)
            {
                int i = candidates[random.Next(candidates.Count)];
                double x = i % w, y = i / w;
                if (!Free(x, y, apart))
                {
                    continue;
                }

                chosen.Add(new BlankHole(new PointD(x, y), kind, false));
                placed++;
            }
        }

        // Touching pairs on paper or ink: two holes whose centres are 0.8 of a calibre apart.
        var open = Enumerable.Range(0, places.Length).Where(i => places[i] is BlankPlace.Paper or BlankPlace.Ink).ToList();
        for (int pair = 0, attempt = 0; pair < Pairs && open.Count > 0 && attempt < 5000; attempt++)
        {
            int i = open[random.Next(open.Count)];
            double x = i % w, y = i / w, angle = random.NextDouble() * Math.PI, step = 0.8 * calibreInches * dpi;
            double x2 = x + (step * Math.Cos(angle)), y2 = y + (step * Math.Sin(angle));
            if (!Free(x, y, apart + step) || !Free(x2, y2, apart))
            {
                continue;
            }

            chosen.Add(new BlankHole(new PointD(x, y), places[i], true));
            chosen.Add(new BlankHole(new PointD(x2, y2), places[i], true));
            pair++;
        }

        // A reactive target's hit lifts the black top layer off in a ragged bright patch around the hole.
        if (reactive)
        {
            foreach (var hole in chosen.Where(c => c.On == BlankPlace.Ink))
            {
                double radius = (0.18 + (0.12 * random.NextDouble())) + (calibreInches / 2);
                double[] lobes = [.. Enumerable.Range(0, 3).Select(_ => 0.15 * random.NextDouble())];
                double[] phases = [.. Enumerable.Range(0, 3).Select(_ => 2 * Math.PI * random.NextDouble())];
                Paint(render, w, h, dpi, hole.Image.X / dpi, hole.Image.Y / dpi, radius * 1.5, (dx, dy) =>
                {
                    double theta = Math.Atan2(dy, dx), edge = radius;
                    for (int k = 0; k < lobes.Length; k++)
                    {
                        edge *= 1 + (lobes[k] * Math.Cos(((k + 2) * theta) + phases[k]));
                    }

                    return Math.Sqrt((dx * dx) + (dy * dy)) <= edge ? (byte)235 : null;
                });
            }
        }

        const double dmm = 254;
        double scale = calibreInches / AnyTargetCalibre;
        var synthetic = chosen.Select(c => SyntheticSheet.SampleHole(random, c.Image.X / dpi * dmm, c.Image.Y / dpi * dmm, c.On != BlankPlace.Paper,
            reactive && c.On == BlankPlace.Ink ? HoleBacking.Dark : HoleBacking.ScannerLid, scale)).ToList();
        double s = dmm / dpi;
        var mapping = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var picture = SyntheticSheet.Compose(new GrayImage(w, h, render), dpi, mapping, w, h, synthetic, [], random);
        return (picture, [.. chosen.Select((c, k) => c with { Image = mapping.ToImage(new PointD(synthetic[k].X, synthetic[k].Y)) })]);
    }

    /// <summary>
    /// One blank scored: per calibre and seed, how many of the synthetic holes the finder found, by what they sit on, and how many marks
    /// were false.
    /// </summary>
    public static IReadOnlyList<BlankScore> ScoreBlank(string name, GrayImage blank, BlankPlace[] places, double dpi, bool reactive, IImagingBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);
        var scores = new List<BlankScore>();
        foreach (double calibre in BlankCalibres)
        {
            foreach (int seed in BlankSeeds)
            {
                var (picture, holes) = BlankPicture(blank, places, dpi, seed, calibre, reactive);
                var finding = AnyTargetHoleFinder.Find(picture, dpi, backend, calibre);
                var marks = finding.Holes.Select(m => m.Image).ToList();
                var (found, falseMarks, _) = Match([.. holes.Select(x => x.Image)], marks, FoundWithinInches * dpi);
                var byPlace = holes.GroupBy(x => x.Touching ? "touching" : x.On.ToString().ToLowerInvariant())
                    .ToDictionary(g => g.Key, g => (Total: g.Count(), Found: g.Count(x => marks.Any(m => Math.Sqrt(Math.Pow(m.X - x.Image.X, 2) + Math.Pow(m.Y - x.Image.Y, 2)) <= FoundWithinInches * dpi))));
                scores.Add(new BlankScore(name, calibre, seed, holes.Count, found, falseMarks, byPlace));
            }
        }

        return scores;
    }
}

/// <summary>One blank's picture scored: the calibre, the seed, the holes and how many were found, the false marks, and the holes by place.</summary>
public sealed record BlankScore(string Blank, double CalibreInches, int Seed, int Holes, int Found, int FalseMarks, IReadOnlyDictionary<string, (int Total, int Found)> ByPlace);

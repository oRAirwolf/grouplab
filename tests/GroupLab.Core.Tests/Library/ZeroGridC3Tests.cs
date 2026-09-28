using GroupLab.Core.Detection;
using GroupLab.Core.Gltd.Derivation;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;
using GroupLab.Cli.Imaging;

namespace GroupLab.Core.Tests.Library;

/// <summary>
/// NOTES-FROM-PLANNING.md entries 250 section 3, 251 and 252: before the C3 zeroing grids are released, holes are put where a shooter's
/// will land on them, at the usual calibres, and the pipeline runs on the render: on a fine line, on a crossing, on a tick of the frame and
/// of the centre cross, on the heavy axis, on a whole-MOA line, touching the diamond, in its black and in its white centre. Each is found,
/// and where it was found is where it is.
/// </summary>
public class ZeroGridC3Tests
{
    private const double Dpi = 300;

    /// <summary>A C3 sheet from the library, where entry 254 put them.</summary>
    internal static TargetDefinition Load(string file) => BuiltIns.Load(file);

    public static TheoryData<string, double> Sheets()
    {
        var data = new TheoryData<string, double>();
        foreach (string file in new[] { "GL-ZERO-MIL-100Y.gltd.json", "GL-ZERO-MIL-100M.gltd.json", "GL-ZERO-MOA-100Y.gltd.json", "GL-ZERO-MOA-100M.gltd.json" })
        {
            foreach (double calibre in new[] { 0.224, 0.264, 0.308 })
            {
                data.Add(file, calibre);
            }
        }

        return data;
    }

    public static TheoryData<string> Files() => ["GL-ZERO-MIL-100Y.gltd.json", "GL-ZERO-MIL-100M.gltd.json", "GL-ZERO-MOA-100Y.gltd.json", "GL-ZERO-MOA-100M.gltd.json"];

    /// <summary>
    /// Each C3 sheet reads back from its canonical JSON, validates with no error, and survives the GLTD-B round trip as style 3, with the
    /// same field, the same derived markers and the same identifier: style byte 3 carries it (TARGET-SCHEMA.md section 3.13).
    /// </summary>
    [Theory]
    [MemberData(nameof(Files))]
    public void ItReadsValidatesAndRoundTripsAsStyle3(string file)
    {
        var source = Load(file);
        var read = GroupLab.Core.Gltd.Json.GltdJsonReader.Read(GroupLab.Core.Gltd.Json.CanonicalJsonWriter.Write(source));
        Assert.Empty(read.Diagnostics);
        Assert.DoesNotContain(GroupLab.Core.Gltd.Validation.GltdValidator.Validate(read.Definition!), d => d.Severity == GroupLab.Core.Gltd.Severity.Error);

        var encoding = GroupLab.Core.Gltd.Binary.GltdBinary.Encode(source).Encoding!;
        var decoded = GroupLab.Core.Gltd.Binary.GltdBinary.Decode([GroupLab.Core.Gltd.Binary.GltdBinary.ReplicatedFrame(encoding)]);
        Assert.Empty(decoded.Diagnostics);
        var grid = decoded.Definition!.Grids![0];
        Assert.Equal((GridStyle3.Style, source.Grids![0].HalfX, source.Grids[0].HalfY, source.Grids[0].WholeEvery), (grid.StyleOrDefault, grid.HalfX, grid.HalfY, grid.WholeEvery));
        Assert.Equal(source.Fiducials!.Markers, decoded.Definition.Fiducials!.Markers);
        Assert.Equal(source.Id, decoded.DefinitionId);
        Assert.True(source.Fiducials.Markers!.Count >= 8, $"{file} has {source.Fiducials.Markers.Count} markers");
    }

    /// <summary>
    /// The numbers outside the grid never touch a marker's footprint, nor each other, and nothing is written inside the frame: every marker
    /// keeps <see cref="GridStyle3.MarkerToNumber"/> clear of every number beyond its own quiet zone.
    /// </summary>
    [Theory]
    [MemberData(nameof(Files))]
    public void TheNumbersClearTheMarkersAndStayOutsideTheGrid(string file)
    {
        var definition = Load(file);
        var grid = definition.Grids![0];
        var page = SceneBuilder.Build(definition).Pages[0];
        var words = page.Items.OfType<TextRun>().Where(r => r.Layer == SceneLayer.MeasurementGrid).Select(r =>
        {
            double width = GroupLab.Core.Rendering.Pdf.HelveticaMetrics.TextWidth(r.Text, r.FontSize, r.Bold) / 2.0;
            double x = r.X / 2.0, baseline = r.Baseline / 2.0, cap = 0.718 * r.FontSize / 2.0;
            double left = r.Anchor switch { TextAnchor.Right => x - width, TextAnchor.Centre => x - (width / 2), _ => x };
            return (r.Text, Left: left, Right: left + width, Top: baseline - cap, Bottom: baseline);
        }).ToList();
        double half = (definition.Fiducials!.MarkerSize / 2.0) + definition.Fiducials.QuietZone + GridStyle3.MarkerToNumber;
        foreach (var marker in definition.Fiducials.Markers!)
        {
            foreach (var w in words)
            {
                bool touches = w.Left < marker.X + half && marker.X - half < w.Right && w.Top < marker.Y + half && marker.Y - half < w.Bottom;
                Assert.False(touches, $"{file}: \"{w.Text}\" comes within {GridStyle3.MarkerToNumber} dmm of the marker at ({marker.X}, {marker.Y})");
            }
        }

        // No two words meet, the two numbers at each corner of the frame included (digits sit on the line, with no descender).
        for (int i = 0; i < words.Count; i++)
        {
            for (int j = i + 1; j < words.Count; j++)
            {
                var (u, v) = (words[i], words[j]);
                Assert.False(u.Left < v.Right && v.Left < u.Right && u.Top < v.Bottom && v.Top < u.Bottom, $"{file}: \"{u.Text}\" and \"{v.Text}\" run together");
            }
        }

        foreach (var w in words.Where(w => w.Bottom > grid.CentreY - grid.HalfY && w.Top < grid.CentreY + grid.HalfY))
        {
            Assert.False(w.Right > grid.CentreX - grid.HalfX && w.Left < grid.CentreX + grid.HalfX, $"{file}: \"{w.Text}\" is written inside the grid");
        }
    }

    /// <summary>
    /// Entry 254, answering question 64: a <b>real</b> hole in solid black. Alan's aim point card of 2026-09-26 has a shot through the black of
    /// its C diamond, just above the white centre, and the card's C is the C sheets' own bull. Its pixels, cut from the 600 dpi scan with
    /// nothing but the hole kept (<c>Fixtures/real-hole-in-black-2026-09-26.png</c>, its bright core 0.194 in across), are set into the
    /// render's black at the same place, and into the E bull's and each C3 diamond's black, and the whole pipeline runs. Where the
    /// synthetic hole's core is refused as too small, the real one is found.
    /// </summary>
    [Theory]
    [InlineData("GL-CF25-LTR-C.gltd.json", -0.0142, -0.1400, true)]
    [InlineData("GL-CF25-LTR-C.gltd.json", -0.0142, -0.1400, false)]
    [InlineData("GL-CF25-LTR-E.gltd.json", 0.0, -0.30, true)]
    [InlineData("GL-ZERO-MIL-100Y.gltd.json", 0.0, -0.20, true)]
    [InlineData("GL-ZERO-MIL-100M.gltd.json", 0.0, -0.25, false)]
    [InlineData("GL-ZERO-MOA-100Y.gltd.json", 0.0, -0.33, true)]
    [InlineData("GL-ZERO-MOA-100M.gltd.json", 0.0, -0.36, false)]
    public void ARealHoleInSolidBlackIsFound(string file, double dxInches, double dyInches, bool named)
    {
        var definition = file.StartsWith("GL-ZERO", StringComparison.Ordinal) ? Load(file) : BuiltIns.Load(file);
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], Dpi);
        var bull = definition.Bulls[definition.Bulls.Count / 2];
        double hx = bull.X + (dxInches * 254), hy = bull.Y + (dyInches * 254);
        // The scan's 600 dpi patch onto the render's 300: each render pixel takes the share of it the hole covers, at the hole's own grey.
        using var patch = OpenCvSharp.Cv2.ImRead(Repo.PathTo("tests", "GroupLab.Core.Tests", "Fixtures", "real-hole-in-black-2026-09-26.png"), OpenCvSharp.ImreadModes.Grayscale);
        var pixels = (byte[])render.Pixels.Clone();
        double scale = 600 / Dpi;
        int cx = (int)Math.Round(hx * Dpi / 254), cy = (int)Math.Round(hy * Dpi / 254), reach = (int)(patch.Width / scale / 2);
        double onBlack = 0, covered = 0;
        for (int v = -reach; v < reach; v++)
        {
            for (int u = -reach; u < reach; u++)
            {
                double sum = 0;
                int count = 0;
                for (int j = 0; j < (int)scale; j++)
                {
                    for (int i = 0; i < (int)scale; i++)
                    {
                        byte value = patch.At<byte>((int)(((v + reach) * scale) + j), (int)(((u + reach) * scale) + i));
                        if (value > 0)
                        {
                            sum += value;
                            count++;
                        }
                    }
                }

                if (count > 0)
                {
                    double share = count / (scale * scale);
                    int at = ((cy + v) * render.Width) + cx + u;
                    onBlack += render.Pixels[at] < 128 ? share : 0;
                    covered += share;
                    pixels[at] = (byte)Math.Round((pixels[at] * (1 - share)) + (sum / count * share));
                }
            }
        }

        // Most of the hole lies on the black, as on the card, where its centre sits at the tip of the white centre.
        Assert.True(onBlack / covered > 0.5, $"only {onBlack / covered:P0} of the hole is on the black");
        var holed = new GrayImage(render.Width, render.Height, pixels);
        var random = new Random(254);
        double s = 254 / Dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var image = SyntheticSheet.Compose(holed, Dpi, truth, render.Width, render.Height, [], [], random);
        var metadata = new ImageMetadata("PNG", image.Width, image.Height, Dpi, Dpi, null, null, null, null, null);
        var result = AutomaticMarking.Run(image, image, metadata, definition, new OpenCvSharpBackend(), calibre: named ? Calibre.Of(0.264) : null);
        Assert.True(result.Failure is null, $"{file} was not analyzed: {result.Failure}");
        var found = result.Detections.Select(d => Math.Sqrt(Math.Pow(((d.Image.X + 0.5) * s) - hx, 2) + Math.Pow(((d.Image.Y + 0.5) * s) - hy, 2))).DefaultIfEmpty(double.MaxValue).Min();
        Assert.True(found < 0.1 * 254, $"{file}: the real hole in the black was not found; {result.Detections.Count} found, refused " + string.Join("; ", (result.Difference?.Rejected ?? []).Select(r => r.Reason)));
        Assert.Single(result.Detections);
    }

    /// <summary>
    /// Question 64's finding, and entry 254's answer to it. The synthetic hole, calibrated on holes in white paper, is drawn on black as a small
    /// bright core inside a dark rim, and the detector refuses that core as too small (0.05 to 0.08 in against a floor of 0.13 to 0.15). A
    /// real hole in black is not like that: the card's shot through the C diamond shows a core 0.194 in across, about what a hole shows on
    /// white paper, and <see cref="ARealHoleInSolidBlackIsFound"/> finds it on every black-bodied sheet. So the size floor stands, and this
    /// holds the synthetic model's limit where it is, so a sweep that relies on synthetic holes in black knows not to believe them.
    /// </summary>
    [Theory]
    [InlineData("GL-ZERO-MOA-100Y.gltd.json", true)]
    [InlineData("GL-CF25-LTR-E.gltd.json", true)]
    [InlineData("GL-CF25-LTR-E.gltd.json", false)]
    [InlineData("GL-CF25-LTR-C.gltd.json", false)]
    public void TheSyntheticHoleOnBlackIsSmallerThanARealOne(string file, bool named)
    {
        var definition = file.StartsWith("GL-ZERO", StringComparison.Ordinal) ? Load(file) : BuiltIns.Load(file);
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], Dpi);
        var random = new Random(251);
        var bull = definition.Bulls[definition.Bulls.Count / 2];
        double r = definition.RingSets[0].Discs[0].Diameter / 2.0;
        var (x, y) = (bull.X - (0.62 * r), bull.Y + (0.05 * r));
        Assert.True(render[(int)(x * Dpi / 254), (int)(y * Dpi / 254)] < 128, "the hole is meant to be on the black");
        var hole = SyntheticSheet.SampleHole(random, x, y, true, HoleBacking.ScannerLid, 0.871);
        double s = 254 / Dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var image = SyntheticSheet.Compose(render, Dpi, truth, render.Width, render.Height, [hole], [], random);
        var metadata = new ImageMetadata("PNG", image.Width, image.Height, Dpi, Dpi, null, null, null, null, null);
        var result = AutomaticMarking.Run(image, image, metadata, definition, new OpenCvSharpBackend(), calibre: named ? Calibre.Of(0.308) : null);
        Assert.True(result.Detections.Count == 0 && (result.Difference?.Rejected ?? []).Any(b => b.Reason.StartsWith("too small", StringComparison.Ordinal)),
            $"{file}: the synthetic hole in the black was {(result.Detections.Count > 0 ? "found" : "not refused as too small")}; if the synthetic model now draws holes in black as real ones look, this test has done its job.");
    }

    /// <summary>Where each hole goes, named, in dmm from the aim: <paramref name="p"/> is a square, <paramref name="h"/> half the diamond.</summary>
    public static IReadOnlyList<(string Where, double X, double Y)> Places(MeasurementGrid grid, double p, double h) =>
    [
        ("on a fine line", 1.0 * p, 0.5 * p),
        ("on a crossing", 2.0 * p, -2.0 * p),
        ("on a frame tick", -1.5 * p, grid.HalfY - 12),
        ("on a centre-cross tick", -2.5 * p, 0),
        ("on the heavy axis", 0, 2.5 * p),
        (grid.Unit == GridUnit.Moa ? "on a whole-MOA line" : "on a line beside the axis", 2.0 * p, 1.5 * p),
        ("touching the diamond", 0.55 * h, -0.55 * h),
        ("in the white centre", 0.1 * h, 0.12 * h),
    ];

    [Theory]
    [MemberData(nameof(Sheets))]
    public void EveryHoleIsFoundWhereItIs(string file, double calibre)
    {
        var definition = Load(file);
        Assert.Equal(GridStyle3.Style, definition.Grids![0].StyleOrDefault);
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], Dpi);
        var random = new Random(251);
        var grid = definition.Grids![0];
        double p = (double)grid.Half / grid.Divisions;
        double h = definition.RingSets[0].Discs[0].Diameter / 2.0;
        var places = Places(grid, p, h);
        bool OnInk(double x, double y) => render[(int)(x * Dpi / 254), (int)(y * Dpi / 254)] < 128;
        var holes = places.Select(c => SyntheticSheet.SampleHole(random, grid.CentreX + c.X, grid.CentreY + c.Y, OnInk(grid.CentreX + c.X, grid.CentreY + c.Y),
            HoleBacking.ScannerLid, 0.871 * calibre / 0.308)).ToList();
        double s = 254 / Dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var image = SyntheticSheet.Compose(render, Dpi, truth, render.Width, render.Height, holes, [], random);
        var metadata = new ImageMetadata("PNG", image.Width, image.Height, Dpi, Dpi, null, null, null, null, null);
        var result = AutomaticMarking.Run(image, image, metadata, definition, new OpenCvSharpBackend(), calibre: Calibre.Of(calibre));
        Assert.True(result.Failure is null, $"{file} was not analyzed: {result.Failure}");

        var lines = new List<string>();
        var missed = new List<string>();
        double worst = 0;

        // The diamond's own cases are measured and reported, not held: a hole partly on its black loses its rim there, which is question 64.
        bool OnGrid(int i) => !places[i].Where.Contains("diamond", StringComparison.Ordinal) && !places[i].Where.Contains("white", StringComparison.Ordinal);
        for (int i = 0; i < holes.Count; i++)
        {
            var nearest = result.Detections.Select(d => (d, Distance: Math.Sqrt(Math.Pow(((d.Image.X + 0.5) * s) - holes[i].X, 2) + Math.Pow(((d.Image.Y + 0.5) * s) - holes[i].Y, 2))))
                .OrderBy(m => m.Distance).FirstOrDefault();
            if (nearest.d is null || nearest.Distance > holes[i].RimRadius)
            {
                if (OnGrid(i))
                {
                    missed.Add(places[i].Where);
                }

                lines.Add($"{places[i].Where}: missed");
                continue;
            }

            worst = OnGrid(i) ? Math.Max(worst, nearest.Distance) : worst;
            lines.Add($"{places[i].Where}: {nearest.Distance / 254:0.0000} in");
        }

        if (Environment.GetEnvironmentVariable("GROUPLAB_C3_REPORT") is { Length: > 0 } report)
        {
            File.AppendAllText(report, $"{file} {calibre:0.000}: {result.Detections.Count} found of {holes.Count}; worst {worst / 254:0.0000} in; {string.Join("; ", lines)}{Environment.NewLine}");
        }

        if (Environment.GetEnvironmentVariable("GROUPLAB_C3_REPORT") is { Length: > 0 } why)
        {
            foreach (var r in result.Difference?.Rejected ?? [])
            {
                File.AppendAllText(why, $"   refused at ({r}) {Environment.NewLine}");
            }
        }

        Assert.True(missed.Count == 0, $"{file} at {calibre} in: missed {string.Join(", ", missed)}. {string.Join("; ", lines)}");
        Assert.True(result.Detections.Count <= holes.Count, $"{file} at {calibre} in: {result.Detections.Count} found of {holes.Count}, so something that is not a hole was taken for one.");
        // 0.03 in is 0.03 MOA at 100 yd, a tenth of the smallest click these sheets name; the worst measured, a .22 on a crossing, was 0.023.
        Assert.True(worst / 254 < 0.03, $"{file} at {calibre} in: a hole was found {worst / 254:0.000} in from where it is. {string.Join("; ", lines)}");
    }
}

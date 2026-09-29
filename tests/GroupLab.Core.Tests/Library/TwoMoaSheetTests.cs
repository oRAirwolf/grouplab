using GroupLab.Cli.Imaging;
using GroupLab.Cli.Library;
using GroupLab.Core.Detection;
using GroupLab.Core.Gltd;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Derivation;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Gltd.Validation;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Measurement;
using GroupLab.Core.Printing;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;
using GroupLab.Core.Trace;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Library;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 289: the 2 MOA sheets Unholy asked for, nine 2.00 in bulls 3 by 3 on a 63.4 mm grid with a load block, as one
/// page and as a set of three, on Letter and A4, with the plain, C and E bulls. Each is checked as the library holds it, drawn, read back
/// from its own render with holes in it, and registered from a photograph on the markers it carries.
/// </summary>
public class TwoMoaSheetTests(ITestOutputHelper output)
{
    private const double Dpi = 300;

    /// <summary>The photograph gate, 0.005 in, in dmm: five times conformance test 43's, as the surface tests use.</summary>
    private const double PhotographGate = SyntheticScanCheck.Gate * 5;

    private static readonly IReadOnlyList<TargetDefinition> Library = SheetIdentification.Candidates([Repo.PathTo("targets")]);

    public static TheoryData<string> Sheets()
    {
        var data = new TheoryData<string>();
        foreach (var sheet in LibraryBuilder.TwoMoaSheets)
        {
            data.Add(sheet.Stem);
        }

        return data;
    }

    [Fact]
    public void TheLibraryHoldsTwelveSheetsBothPageSizesAllThreeBullsOnePageAndASet()
    {
        Assert.Equal(12, LibraryBuilder.TwoMoaSheets.Length);
        foreach (var (stem, page, set, style, name) in LibraryBuilder.TwoMoaSheets)
        {
            var d = BuiltIns.Load(stem + ".gltd.json");
            Assert.Equal(name, d.Name);
            Assert.Equal(page == "letter" ? PageSize.Letter : PageSize.A4, d.Page.Size);
            Assert.Equal(set, d.Tiling is not null);
            Assert.Equal(style switch { 'C' => "c", 'E' => "e", _ => "std" }, d.RingSets.Single().Key);
        }

        // Every combination once.
        Assert.Equal(12, LibraryBuilder.TwoMoaSheets.Select(s => (s.Page, s.Set, s.Style)).Distinct().Count());
        Assert.All(LibraryBuilder.TwoMoaSheets, s => Assert.Contains(s.Style, "PCE"));
    }

    [Theory]
    [MemberData(nameof(Sheets))]
    public void EachPageIsNineTwoInchBullsOnThe634Grid(string stem)
    {
        var d = BuiltIns.Load(stem + ".gltd.json");
        var bulls = d.Bulls;
        Assert.Equal(9, bulls.Count);
        Assert.All(bulls, b => Assert.True(b.Scoring));
        Assert.Equal(Enumerable.Range(1, 9).Select(i => i.ToString(System.Globalization.CultureInfo.InvariantCulture)), bulls.Select(b => b.Label));
        var xs = bulls.Select(b => b.X).Distinct().Order().ToList();
        var ys = bulls.Select(b => b.Y).Distinct().Order().ToList();
        Assert.Equal([634, 634], xs.Zip(xs.Skip(1), (a, b) => b - a));
        Assert.Equal([634, 634], ys.Zip(ys.Skip(1), (a, b) => b - a));

        // Centered across the page, as the designer centers every grid.
        Assert.InRange(xs[0] + xs[2] - d.Page.Width, -1, 1);

        Assert.Equal(508, d.RingSets.Single().Discs[0].Diameter);
        Assert.Contains("1.91 MOA at 100 yd", d.Description, StringComparison.Ordinal);
        Assert.Contains("1.75 MOA at 100 m", d.Description, StringComparison.Ordinal);
        Assert.Contains("requested by Unholy", d.Description, StringComparison.Ordinal);
        Assert.Empty(GltdValidator.Validate(d));

        // The load block is the declared exclusion zone at the foot, as on GL-CF25-LTR-D.
        Assert.NotNull(d.DataBlock);
        Assert.Equal(d.Page.Height - 120 - 310, d.DataBlock.Y);
    }

    [Fact]
    public void ThePlainBullIsTwiceTheOneMoaBullAndTheCAndEBullsAreSizedToIt()
    {
        Assert.Equal([(508, "black"), (492, "paper"), (254, "black"), (238, "paper"), (50, "black")],
            BuiltIns.Load("GL-CF9-LTR.gltd.json").RingSets.Single().Discs.Select(x => (x.Diameter, x.Ink)));

        // The 1 MOA sheets' outer ring is 8 dmm wide (254 to 238); both rings here are.
        Assert.Equal(254 - 238, 508 - 492);

        Assert.Equal([(508, "black"), (182, "paper"), (50, "black")], BuiltIns.Load("GL-CF9-LTR-E.gltd.json").RingSets.Single().Discs.Select(x => (x.Diameter, x.Ink)));
        // The C diamond, 2.00 in point to point, in the aim point card's proportions.
        var c = BuiltIns.Load("GL-CF9-LTR-C.gltd.json").RingSets.Single().Discs;
        Assert.Equal([(508, DiscShape.Square), (146, DiscShape.Square), (41, DiscShape.Circle)], c.Select(x => (x.Diameter, x.Shape)));
    }

    [Fact]
    public void TheCAndESheetsAndTheSetsSharePlainPagesPositions()
    {
        foreach (string page in (string[])["LTR", "A4"])
        {
            string setStem = page == "LTR" ? "GL-CF9-T" : "GL-CF9-TA4";
            var plain = BuiltIns.Load($"GL-CF9-{page}.gltd.json");
            foreach (string other in (string[])[$"GL-CF9-{page}-C", $"GL-CF9-{page}-E", setStem, setStem + "-C", setStem + "-E"])
            {
                var d = BuiltIns.Load(other + ".gltd.json");
                Assert.Equal(plain.Bulls.Select(b => (b.X, b.Y)), d.Bulls.Select(b => (b.X, b.Y)));
                Assert.Equal(plain.DataBlock, d.DataBlock);
                Assert.Equal(plain.Codes!.Positions, d.Codes!.Positions);
            }
        }
    }

    /// <summary>
    /// The designer's engine lays out the same page from the same numbers, with the top pair of codes only. Its markers are the intersections
    /// alone; the library's sheets add the outer edge midpoints.
    /// </summary>
    [Theory]
    [InlineData("letter", "GL-CF9-LTR")]
    [InlineData("a4", "GL-CF9-A4")]
    public void TheDesignerDrawsTheSamePage(string page, string stem)
    {
        Assert.Contains(508, ParametricSheet.RingSizes);
        var design = ParametricSheet.Design(new SheetSpec("2 MOA", page, 3, 3, 634 / 254.0, 508, 0, true, TopCodesOnly: true));
        Assert.True(design.Printable, string.Join(" ", design.Checks.Select(c => c.Sentence)));
        var library = BuiltIns.Load(stem + ".gltd.json");
        Assert.Equal(library.Bulls.Select(b => (b.X, b.Y)), design.Definition!.Bulls.Select(b => (b.X, b.Y)));
        Assert.Equal(library.DataBlock, design.Definition.DataBlock);
        Assert.Subset(library.Fiducials!.Markers!.Select(m => (m.X, m.Y)).ToHashSet(), design.Definition.Fiducials!.Markers!.Select(m => (m.X, m.Y)).ToHashSet());
    }

    /// <summary>
    /// Entry 289 section 1: markers at the lattice's intersections, all but the two top corners on Letter, which sit against the top codes.
    /// The load block owns the bottom band, so the page carries the top pair of codes, as GL-CF25-LTR-D does; on A4 the bottom pair would
    /// fit and take the lattice's two bottom corners, so A4 keeps the top pair too and the grid sits lower, with every intersection. The
    /// intersections alone left too few for a bowed sheet, so <c>grid-boundary-edge-1</c> adds the midpoints of the outer cell edges, on
    /// every bull style: 26 on Letter, 28 on A4. The midpoints between neighboring bulls stay free.
    /// </summary>
    [Theory]
    [InlineData("GL-CF9-LTR", 26)]
    [InlineData("GL-CF9-LTR-C", 26)]
    [InlineData("GL-CF9-LTR-E", 26)]
    [InlineData("GL-CF9-T", 26)]
    [InlineData("GL-CF9-A4", 28)]
    [InlineData("GL-CF9-A4-C", 28)]
    [InlineData("GL-CF9-TA4-E", 28)]
    public void TheMarkersAreTheIntersectionsAndTheOuterEdgeMidpoints(string stem, int markers)
    {
        var d = BuiltIns.Load(stem + ".gltd.json");
        Assert.Equal("grid-boundary-edge-1", d.Fiducials!.Scheme);
        Assert.Equal(markers, d.Fiducials.Markers!.Count);
        Assert.Equal(2, d.Codes!.Count);
        int left = d.Bulls.Min(b => b.X) - 317, top = d.Bulls.Min(b => b.Y) - 317;
        var intersections = (from i in Enumerable.Range(0, 4) from j in Enumerable.Range(0, 4) select (X: left + (634 * i), Y: top + (634 * j))).ToHashSet();
        var midpoints = Enumerable.Range(0, 3).SelectMany(i => new[]
        {
            (X: left + 317 + (634 * i), Y: top), (X: left + 317 + (634 * i), Y: top + (3 * 634)),
            (X: left, Y: top + 317 + (634 * i)), (X: left + (3 * 634), Y: top + 317 + (634 * i)),
        }).ToHashSet();
        if (markers == 26)
        {
            intersections.ExceptWith([(left, top), (left + (3 * 634), top)]);
        }

        Assert.Equal(intersections.Union(midpoints).ToHashSet(), d.Fiducials.Markers.Select(m => (m.X, m.Y)).ToHashSet());

        // Stored in raster order, as every rule stores them; a single page's ids count up from 0, and a set's are unique over the set.
        Assert.Equal(d.Fiducials.Markers.OrderBy(m => m.Y).ThenBy(m => m.X).Select(m => (m.X, m.Y)), d.Fiducials.Markers.Select(m => (m.X, m.Y)));
        if (d.Tiling is null)
        {
            Assert.Equal(Enumerable.Range(0, markers), d.Fiducials.Markers.Select(m => m.Id));
        }
    }

    /// <summary>The new rule is carried in the codes: a body decoded from a printed frame derives the same markers and the same identifier.</summary>
    [Theory]
    [MemberData(nameof(Sheets))]
    public void TheEdgeRuleRoundTripsThroughTheCodes(string stem)
    {
        var d = BuiltIns.Load(stem + ".gltd.json");
        var encoded = GltdBinary.Encode(d).Encoding!;
        Assert.Equal(d.Id, encoded.DefinitionId);
        var decoded = GltdBinary.Decode([GltdBinary.ReplicatedFrame(encoded)]).Definition!;
        Assert.Equal("grid-boundary-edge-1", decoded.Fiducials!.Scheme);
        Assert.Equal(encoded.Body, GltdBinary.Encode(decoded).Encoding!.Body);
        Assert.Equal(FiducialDerivation.Derive(d).Markers!.Positions, FiducialDerivation.Derive(decoded).Markers!.Positions);
    }

    /// <summary>
    /// Every page of every sheet, the three of a set included, rendered as it prints at 300 dpi with holes on it, names itself and its place
    /// in the set from its own codes, and every hole is found.
    /// </summary>
    [Theory]
    [MemberData(nameof(Sheets))]
    public void EveryPageReadsItselfBackWithEveryHole(string stem)
    {
        var definition = BuiltIns.Load(stem + ".gltd.json");
        var pages = SceneBuilder.Build(definition).Pages;
        Assert.Equal(definition.Tiling is null ? 1 : 3, pages.Count);
        var backend = new OpenCvSharpBackend();
        foreach (var page in pages)
        {
            var render = SceneRasterizer.Rasterize(page, Dpi);
            // The holes of EverySheetDetectsTests, which reads page 1 of every sheet in the library: up to five scoring bulls, each hole a little
            // off center, the same on every page of a set, which differ only in their codes and their markers' identifiers.
            var random = new Random(189);
            bool OnInk(double x, double y) => render[(int)(x * Dpi / 254), (int)(y * Dpi / 254)] < 128;
            var holes = definition.Bulls.Where(b => b.Scoring).Take(5).Select(b => ((double)b.X + random.Next(-40, 41), (double)b.Y + random.Next(-40, 41))).ToList()
                .Select(p => SyntheticSheet.SampleHole(random, p.Item1, p.Item2, OnInk(p.Item1, p.Item2), HoleBacking.ScannerLid, 0.871)).ToList();
            double s = 254 / Dpi;
            var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
            var image = SyntheticSheet.Compose(render, Dpi, truth, render.Width, render.Height, holes, [], random);

            var identity = SheetIdentification.Identify(image, Library, backend, new TraceRecorder());
            Assert.True(identity.Failure is null, $"{stem} page {page.TileIndex + 1} did not name itself: {identity.Failure}");
            Assert.Equal(GltdBinary.Encode(definition).Encoding!.DefinitionId, identity.DefinitionId);
            Assert.Equal(page.TileIndex, identity.TileIndex);

            var metadata = new ImageMetadata("PNG", image.Width, image.Height, Dpi, Dpi, null, null, null, null, null);
            var result = AutomaticMarking.Run(image, image, metadata, definition, backend);
            Assert.True(result.Failure is null, $"{stem} page {page.TileIndex + 1} was not analyzed: {result.Failure}");
            string lost = string.Join("; ", holes.Select((hole, k) => (hole, k, near: result.Detections.Select(x => Math.Sqrt(Math.Pow((x.Image.X * 254 / Dpi) - hole.X, 2) + Math.Pow((x.Image.Y * 254 / Dpi) - hole.Y, 2))).DefaultIfEmpty(1e9).Min()))
                .Where(h => h.near > 30).Select(h => $"bull {h.k + 1} at ({h.hole.X - definition.Bulls[h.k].X:0}, {h.hole.Y - definition.Bulls[h.k].Y:0}) rim {h.hole.RimRadius:0.0} ink {OnInk(h.hole.X, h.hole.Y)} nearest {h.near:0}"));
            Assert.True(result.Detections.Count == holes.Count, $"{stem} page {page.TileIndex + 1}: {result.Detections.Count} holes found of {holes.Count}. Lost: {lost}");
            Assert.Equal(definition.Tiling is null ? null : page.TileIndex, result.SetSheet);
        }
    }

    /// <summary>A set holds 27 bulls, and a missing page is named, as for the large format sets.</summary>
    [Fact]
    public void ASetOfThreeHolds27BullsAndNamesAMissingSheet()
    {
        var d = BuiltIns.Load("GL-CF9-T.gltd.json");
        var pooled = SetPool.Pool(d, []);
        Assert.Equal(3, pooled.SetSize);
        Assert.Equal(27, pooled.BullsInSet);
        Assert.Contains("of the 27 bulls the set holds", pooled.Said, StringComparison.Ordinal);
    }

    /// <summary>
    /// Entry 289 section 1: whether the markers register a photograph within the gates. Each page is photographed synthetically, as the surface
    /// tests do: the main camera's lens, tilted up to 12 degrees, flat and bowed a quarter inch, with the measured corner noise, and again
    /// with a quarter of its markers lost, as an oblique frame can lose them; the registration is the one every photograph gets, and every
    /// bull comes back within the photograph gate. The 5x5 is shot the same way beside it. Measured on 2026-09-29 over 60 frames each: with
    /// the fourteen intersections alone three of thirty bowed Letter frames missed the gate, the worst 0.0060 in; with the edge midpoints the
    /// worst is 0.0015 in flat and 0.0020 in bowed on Letter, 0.0013 and 0.0018 on A4, against the 5x5's 0.0014 and 0.0023.
    /// </summary>
    [Theory]
    [InlineData("GL-CF9-LTR.gltd.json")]
    [InlineData("GL-CF9-A4.gltd.json")]
    [InlineData("GL-CF25-LTR.gltd.json")]
    public void TheMarkersRegisterAPhotographWithinTheGate(string file)
    {
        var d = BuiltIns.Load(file);
        const int width = 3000, height = 4000;
        double worst = 0, worstLost = 0;
        for (int seed = 1; seed <= 12; seed++)
        {
            var random = new Random(seed);
            double tilt = (seed % 4) * 4 * Math.PI / 180, bow = seed % 2 == 0 ? 0 : 0.25 * 254;
            double curvature = 8 * bow / (d.Page.Width * (double)d.Page.Width);
            var truth = SyntheticSurface.Camera(width, height, 2600, -0.05, 0.065, 2600, tilt, d.Page.Width, d.Page.Height, Math.PI / 2,
                [0, curvature * SurfaceModel.BendLength, 0, 0]);
            var all = SyntheticSurface.Corners(d, truth, 0, SyntheticSurface.MeasuredCornerNoisePixels, random, width, height);
            Assert.Equal(d.Fiducials!.Markers!.Count, all.Count);
            worst = Math.Max(worst, Worst(d, truth, all, width, height));

            var kept = all.OrderBy(_ => random.Next()).Take(all.Count - (all.Count / 4)).ToList();
            worstLost = Math.Max(worstLost, Worst(d, truth, kept, width, height));
        }

        output.WriteLine($"{file}: {d.Fiducials!.Markers!.Count} markers, worst bull {worst / 254:0.00000} in, with a quarter lost {worstLost / 254:0.00000} in, gate {PhotographGate / 254:0.000} in");
        Assert.True(worst < PhotographGate, $"{file}: {worst / 254:0.00000} in");
        Assert.True(worstLost < PhotographGate, $"{file} with a quarter of its markers lost: {worstLost / 254:0.00000} in");
    }

    /// <summary>
    /// The registration a photograph gets: the Phase 0 lens model fitted to the corners, then the generalised cylinder started from it,
    /// kept only where the F test says the bend is real, as <see cref="SurfaceSelection"/> decides for every photograph.
    /// </summary>
    private static double Worst(TargetDefinition d, SurfaceModel truth, List<MarkerMatch> matches, int width, int height)
    {
        List<PointD> image = [.. matches.SelectMany(m => m.ImageCorners)], page = [.. matches.SelectMany(m => m.PageCorners)];
        var lens = LensFit.Fit(image, page, HomographyEstimate.Fit(image, page)!, width, height);
        var frame = new SurfaceFrame("2 MOA", image, page, [.. image.Select(_ => true)], SurfaceFit.StartFromLens(lens, 2556, d.Page.Width / 2.0, d.Page.Height / 2.0),
            0, 0, d.Page.Width, d.Page.Height);
        var fit = SurfaceFit.Fit([frame], shareCamera: false)[0];
        var choice = SurfaceSelection.Choose(fit, image, page, width, height);
        IPageMapping mapping = choice.PreferSurface || choice.Planar is null ? fit.Mapping : choice.Planar;
        return d.Bulls.Max(b =>
        {
            var declared = new PointD(b.X, b.Y);
            var back = mapping.ToPage(SyntheticSurface.Image(truth, 0, d.Page.Width, d.Page.Height, declared));
            return Math.Sqrt(Math.Pow(back.X - declared.X, 2) + Math.Pow(back.Y - declared.Y, 2));
        });
    }

    /// <summary>
    /// Entry 289 section 1: on A4 the grid sits about 8 mm from the side edges. The nearest ink to a side edge is a marker, 7.9 mm in, as on the
    /// 5x5 on A4 (8.0 mm), and inside the renderer's 6 mm tight edge on neither; nothing in the grid had to shrink. It prints on a printer
    /// that leaves the common quarter inch unprinted at each side, as every A4 sheet in the library does.
    /// </summary>
    [Fact]
    public void OnA4TheGridClearsThePrintersMargins()
    {
        double Nearest(string file) => SceneBuilder.Build(BuiltIns.Load(file)).Pages.Min(p =>
            p.Items.Min(i => { var e = PrintFit.Extent(i); return Math.Min(e.Left, p.Width - e.Right); })) / 20.0;

        double twoMoa = Nearest("GL-CF9-A4.gltd.json"), fiveByFive = Nearest("GL-CF25-A4.gltd.json"), check = Nearest("GL-SCALE-A4-1.gltd.json");
        output.WriteLine($"nearest ink to a side edge on A4: 2 MOA {twoMoa:0.0} mm, 5x5 {fiveByFive:0.0} mm, printer check {check:0.0} mm");
        Assert.InRange(twoMoa, 7.5, 8.5);
        Assert.True(twoMoa >= fiveByFive - 0.2, $"{twoMoa} mm against the 5x5's {fiveByFive} mm");
        Assert.True(twoMoa * 10 > GltdValidator.TightEdge, "inside the renderer's tight edge");

        // A printer leaving a quarter inch at each side, 6.35 mm, on A4 at 600 dpi.
        const int dpi = 600;
        int w = (int)Math.Round(2100 * dpi / 254.0), h = (int)Math.Round(2970 * dpi / 254.0), margin = (int)Math.Round(0.25 * dpi);
        var printer = new PrinterPage("Quarter inch", dpi, dpi, w, h, margin, margin, w - (2 * margin), h - (2 * margin));
        foreach (string file in (string[])["GL-CF9-A4.gltd.json", "GL-CF9-A4-C.gltd.json", "GL-CF9-A4-E.gltd.json", "GL-CF9-TA4.gltd.json"])
        {
            Assert.Null(PrintFit.Refusal(SceneBuilder.Build(BuiltIns.Load(file)).Pages, printer));
        }
    }

    /// <summary>The print screen lists them under centerfire load development, designed for 100 yd.</summary>
    [Fact]
    public void ThePrintScreenListsThemAsCenterfireLoadDevelopment()
    {
        var sheets = TargetLibrary.Load(Repo.PathTo("targets")).Where(s => s.File.StartsWith("GL-CF9-", StringComparison.Ordinal)).ToList();
        Assert.Equal(12, sheets.Count);
        Assert.All(sheets, s => Assert.Equal("Centerfire load development", s.Family));
        Assert.All(sheets, s => Assert.Equal("100 yd", s.DesignedFor));
        Assert.Equal(6, sheets.Count(s => s.Sheets == 3));
    }
}

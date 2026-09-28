using System.Text.Json;
using GroupLab.Cli.Imaging;
using GroupLab.Cli.Library;
using GroupLab.Core.Gltd;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Derivation;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Gltd.Validation;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;
using GroupLab.Core.Trace;

namespace GroupLab.Core.Tests.Library;

/// <summary>The twenty built-in sheets and two tile presets against the reference tools and the specification.</summary>
public class BuiltInLibraryTests
{
    private static readonly Lazy<IReadOnlyList<BuiltInTarget>> Targets = new(() => LibraryBuilder.Build(LayoutsPath));

    private static readonly Lazy<JsonDocument> Layouts = new(() => JsonDocument.Parse(File.ReadAllBytes(LayoutsPath)));

    private static string LayoutsPath => Repo.PathTo("tools", "layout", "layouts.json");

    public static TheoryData<string> Names()
    {
        var data = new TheoryData<string>();
        foreach (var d in ReferenceFixtures.Definitions)
        {
            data.Add(d.GetProperty("name").GetString()!);
        }

        return data;
    }

    [Fact]
    public void LibraryHasTwentySheetsAndTwoPresetsInCheckPyOrder()
    {
        Assert.Equal(ReferenceFixtures.Definitions.Select(d => d.GetProperty("name").GetString()), Targets.Value.Select(t => t.Name));
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void BodyAndIdentifierMatchCheckPy(string name)
    {
        var fixture = ReferenceFixtures.Named(name);

        // Entry 226 section 1: the zeroing sheets were redrawn, so check.py's zeroing sheets are the frozen ones they were printed from.
        var definition = IsZero(name) ? Frozen(fixture) : Target(name).Definition;
        var encoding = GltdBinary.Encode(definition).Encoding!;

        Assert.Equal(fixture.GetProperty("bodyHex").GetString(), Convert.ToHexStringLower(encoding.Body));
        Assert.Equal(fixture.GetProperty("id").GetString(), IsZero(name) ? encoding.DefinitionId : definition.Id);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void CommittedFileIsTheCanonicalBuild(string name)
    {
        var target = Target(name);
        string path = Committed(target);

        Assert.True(File.Exists(path), $"{path} is missing; run grouplab library build.");
        Assert.Equal(CanonicalJsonWriter.Write(target.Definition), File.ReadAllBytes(path));
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void SheetValidatesWithNoErrors(string name)
    {
        var read = GltdJsonReader.Read(CanonicalJsonWriter.Write(Target(name).Definition));
        Assert.Empty(read.Diagnostics);

        var diagnostics = GltdValidator.Validate(read.Definition!);

        Assert.DoesNotContain(diagnostics, d => d.Severity == Severity.Error);

        // The redrawn zeroing grids reach up to the codes, so their top marker row survives only between them (entry 226 section 1).
        string[] expectedRowBands = IsZero(name) ? ["/grids/0/top"] : [];
        Assert.Equal(expectedRowBands, diagnostics.Where(d => d.Code == "validate.markerRowBand").Select(d => d.Path).Order());

        // Every built-in brackets every bull since the geometry change of docs/NOTES-FROM-PLANNING.md entry 13, so test
        // 26f is an error and the narrow marker bands are the library's only findings (TARGET-SCHEMA.md section 7).
        Assert.All(diagnostics, d => Assert.Equal("validate.markerRowBand", d.Code));
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void GeometryMatchesLayoutsJson(string name)
    {
        var d = IsZero(name) ? Frozen(ReferenceFixtures.Named(name)) : Target(name).Definition;
        string layoutName = name.Replace(" (3x2)", "", StringComparison.Ordinal);
        var root = Layouts.Value.RootElement;
        var layout = root.GetProperty("layouts").EnumerateArray().Concat(root.GetProperty("zero").EnumerateArray())
            .Single(e => e.GetProperty("name").GetString() == layoutName);

        Assert.Equal(layout.GetProperty("qr").EnumerateArray().Select(p => new PointDmm(p[0].GetInt32(), p[1].GetInt32())), d.Codes!.Positions);
        var rect = layout.GetProperty("data_block_rect");
        if (rect.ValueKind == JsonValueKind.Array)
        {
            var b = d.DataBlock!;
            Assert.Equal(rect.EnumerateArray().Select(v => v.GetInt32()), [b.X, b.Y, b.X + b.Width, b.Y + b.Height]);
        }
        else
        {
            Assert.Null(d.DataBlock);
        }

        if (layout.TryGetProperty("xs", out var xs))
        {
            var scoring = d.Bulls.Where(b => b.Scoring).ToList();
            Assert.Equal(xs.EnumerateArray().Select(v => v.GetInt32()), scoring.Select(b => b.X).Distinct());
            Assert.Equal(layout.GetProperty("ys").EnumerateArray().Select(v => v.GetInt32()), scoring.Select(b => b.Y).Distinct());
            Assert.Equal(layout.GetProperty("sighter_x").EnumerateArray().Select(v => v.GetInt32()), d.Bulls.Where(b => !b.Scoring).Select(b => b.X));
            Assert.Equal(layout.GetProperty("markers").GetInt32(), d.Fiducials!.Markers!.Count);
            Assert.Equal(layout.GetProperty("fid_scheme").GetString(), d.Fiducials.Scheme);

            // cells.sighterGap is declared exactly where layout.py sets a gap other than the convention (sections 3.6 and 7).
            var gap = layout.GetProperty("sighter_gap");
            Assert.Equal(gap.ValueKind == JsonValueKind.Number ? gap.GetInt32() : null, d.Cells?.SighterGap);
        }
        else
        {
            var grid = d.Grids!.Single();
            Assert.Equal((layout.GetProperty("cx").GetInt32(), layout.GetProperty("cy").GetInt32()), (grid.CentreX, grid.CentreY));
            Assert.Equal((grid.CentreX, grid.CentreY), (d.Bulls.Single().X, d.Bulls.Single().Y));
            Assert.Equal(layout.GetProperty("marks").EnumerateArray().Select(m => new PointDmm(m[0].GetInt32(), m[1].GetInt32())),
                d.Fiducials!.Markers!.Select(m => new PointDmm(m.X, m.Y)));
        }
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void Test1SheetRoundTripsToItsProjection(string name)
    {
        var source = GltdJsonReader.Read(File.ReadAllBytes(Committed(Target(name)))).Definition!;
        var encoding = GltdBinary.Encode(source).Encoding!;

        var decoded = GltdBinary.Decode([GltdBinary.ReplicatedFrame(encoding)]);

        Assert.Empty(decoded.Diagnostics);
        var projection = decoded.Definition!;
        Assert.Equal(encoding.Body, GltdBinary.Encode(projection).Encoding!.Body);
        Assert.Equal(source.Id, decoded.DefinitionId);
        Assert.Equal(source.Bulls.Select(b => (b.X, b.Y, b.Scoring)), projection.Bulls.Select(b => (b.X, b.Y, b.Scoring)));
        Assert.Equal(source.Codes!.Positions, projection.Codes!.Positions);
        Assert.Equal(source.Fiducials!.Markers, projection.Fiducials!.Markers);
        Assert.Equal(source.Page, projection.Page);
        Assert.Equal(source.Tiling, projection.Tiling);
        Assert.Equal(source.DataBlock is null, projection.DataBlock is null);
    }

    [Theory]
    [InlineData("GL-LR300-T")]
    [InlineData("GL-LR300-TA4")]
    [InlineData("GL-LR300-T (3x2)")]
    [InlineData("GL-LR300-TA4 (3x2)")]
    public void Tests32To34TilesShareOneBodyAndCarryAssemblyUniqueMarkers(string name)
    {
        var d = Target(name).Definition;
        var encoding = GltdBinary.Encode(d).Encoding!;
        int tiles = d.Tiling!.Cols * d.Tiling.Rows;
        var positions = FiducialDerivation.Derive(d).Markers!.Positions;
        var ids = new List<int>();

        for (int tile = 0; tile < tiles; tile++)
        {
            byte[] frame = GltdBinary.ReplicatedFrame(encoding, (byte)tile);
            var decoded = GltdBinary.Decode([frame]);

            // Test 32: every tile is the same definition. Test 33: the header byte alone tells them apart.
            Assert.Equal(encoding.Body, frame[FrameCodec.HeaderLength..]);
            Assert.Equal(d.Id, decoded.DefinitionId);
            Assert.Equal(tile, decoded.TileIndex);

            // Test 34: identifiers are unique across the whole assembly.
            var assignment = MarkerIds.Assign(positions, d.Tiling, tile, MarkerIds.DictionarySize(d.Fiducials!.Family));
            Assert.False(assignment.Wrapped);
            ids.AddRange(assignment.Markers.Select(m => m.Id));
            if (tile == 0)
            {
                Assert.Equal(d.Fiducials.Markers, assignment.Markers);
            }
        }

        Assert.Equal(Enumerable.Range(0, positions.Count * tiles), ids.Order());
    }

    [Fact]
    public void Test34AnAssemblyBeyondTheDictionaryWrapsAndWarns()
    {
        var d = Target("GL-LR300-T").Definition;
        var huge = d with { Tiling = d.Tiling! with { Cols = 16, Rows = 16 }, Fiducials = d.Fiducials! with { Markers = null } };

        var assignment = MarkerIds.Assign(FiducialDerivation.Derive(huge).Markers!.Positions, huge.Tiling, 255, 587);
        var diagnostics = GltdValidator.Validate(huge);

        Assert.True(assignment.Wrapped);
        Assert.All(assignment.Markers, m => Assert.InRange(m.Id, 0, 586));
        Assert.Contains(diagnostics, x => x.Code == "validate.markerIdsWrap" && x.Test == "34" && x.Severity == Severity.Warning);
    }

    /// <summary>
    /// The zeroing grids as design C3 (entries 251, 252 and 254): grid style 3, squares of 0.2 mil or 0.5 MOA, as far as Letter holds with the
    /// numbers outside, every line within half a dmm of its true angle (test 36), the legend's three lines and every line's number printed,
    /// and two codes.
    /// </summary>
    [Theory]
    [InlineData("GL-ZERO-MIL-100Y", 1.0, 1.0, 0.2)]
    [InlineData("GL-ZERO-MIL-100M", 0.8, 0.8, 0.2)]
    [InlineData("GL-ZERO-MOA-100Y", 3.0, 3.5, 0.5)]
    [InlineData("GL-ZERO-MOA-100M", 3.0, 3.0, 0.5)]
    public void TheZeroingGridsAreC3AndSayTheirScale(string name, double across, double upDown, double square)
    {
        var d = Target(name).Definition;
        var g = d.Grids!.Single();
        double unit = GridStyle2.UnitDmm(g)!.Value;

        Assert.Equal(GridStyle3.Style, g.Style);
        Assert.Equal(across, g.HalfX / unit, 2);
        Assert.Equal(upDown, g.HalfY / unit, 2);
        Assert.Equal(square, 1.0 / g.WholeEvery!.Value, 3);
        Assert.Equal(2, d.Codes!.Count);
        Assert.Null(d.DataBlock);

        var offsets = MeasurementGridLines.Offsets(g.Half, g.Divisions);
        for (int i = 0; i <= g.Divisions; i++)
        {
            Assert.True(Math.Abs(offsets[i] - (unit * i / g.WholeEvery.Value)) <= 0.5, $"{name} line {i}: {offsets[i]} against {unit * i / g.WholeEvery.Value:0.000}.");
        }

        var page = GroupLab.Core.Rendering.SceneBuilder.Build(d).Pages[0];
        var text = page.Items.OfType<GroupLab.Core.Rendering.TextRun>().Select(r => r.Text).ToList();
        var (heading, squareWords, tick) = GridStyle3.Legend(g);
        Assert.Contains(heading, text);
        Assert.Contains(squareWords, text);
        Assert.Contains(tick, text);
        foreach (var (i, _) in GridStyle3.Lines(g, g.HalfX).Concat(GridStyle3.Lines(g, g.HalfY)))
        {
            Assert.Contains(GridStyle3.Number(g, i), text);
        }
    }

    /// <summary>The style 2 grids, printed from 2026-09-27 until C3 replaced them, frozen so their printouts still read (entry 254).</summary>
    [Fact]
    public void TheStyle2ZeroingGridsAreFrozen()
    {
        var files = Directory.EnumerateFiles(Repo.PathTo("targets", "frozen", "zero-grid-2"), "*.gltd.json").ToList();
        Assert.Equal(4, files.Count);
        foreach (string file in files)
        {
            var frozen = GltdJsonReader.Read(File.ReadAllBytes(file)).Definition!;
            Assert.Equal(GridStyle2.Style, frozen.Grids!.Single().StyleOrDefault);
            Assert.Equal(Path.GetFileName(file), frozen.Id + ".gltd.json");
            Assert.Equal(frozen.Id, GltdBinary.Encode(frozen).Encoding!.DefinitionId);
        }
    }

    [Fact]
    public void TheFrozenZeroingGridsAreTheOnesPrintedBefore()
    {
        foreach (var fixture in ReferenceFixtures.Definitions.Where(f => IsZero(f.GetProperty("name").GetString()!)))
        {
            var frozen = Frozen(fixture);
            Assert.Equal(1, frozen.Grids!.Single().StyleOrDefault);
            Assert.Equal(Array.Empty<Diagnostic>(), GltdValidator.Validate(frozen).Where(x => x.Severity == Severity.Error));
        }
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 243 section 1.4: the three large format sheets are 2 by 2 sets of Letter or A4 sheets with the same bull
    /// and spacing and at least as many bulls in the set, and a sheet printed from one of the originals still names itself.
    /// </summary>
    [Fact]
    public void TheLargeFormatSheetsAreTwoByTwoSetsAndThePrintedOnesStillRead()
    {
        var library = LibraryBuilder.Library(LayoutsPath);
        var candidates = SheetIdentification.Candidates([Repo.PathTo("targets")]);
        foreach (var (source, stem, _, page) in LibraryBuilder.RedrawnLarge)
        {
            var original = Targets.Value.Single(t => t.FileName == source + ".gltd.json").Definition;
            Assert.DoesNotContain(library, t => t.FileName == source + ".gltd.json");
            var redrawn = library.Single(t => t.FileName == stem + ".gltd.json").Definition;

            Assert.Equal((2, 2), (redrawn.Tiling!.Cols, redrawn.Tiling.Rows));
            Assert.Equal(page, redrawn.Page.Size.ToString().ToLowerInvariant());
            Assert.Equal(original.RingSets.Single().Discs, redrawn.RingSets.Single().Discs);
            int Pitch(TargetDefinition d) => d.Bulls.Where(b => b.Scoring).Select(b => b.X).Distinct().Order().Take(2).Aggregate((a, b) => b - a);
            Assert.Equal(Pitch(original), Pitch(redrawn));
            Assert.True(4 * redrawn.Bulls.Count(b => b.Scoring) >= original.Bulls.Count(b => b.Scoring));
            Assert.DoesNotContain(GltdValidator.Validate(redrawn), d => d.Severity == Severity.Error);

            var render = SceneRasterizer.Rasterize(SceneBuilder.Build(original).Pages[0], 150);
            var identity = SheetIdentification.Identify(render, candidates, new OpenCvSharpBackend(), new TraceRecorder());
            Assert.True(identity.Failure is null, $"{source}: {identity.Failure}");
            Assert.Equal(original.Id, identity.DefinitionId);
        }
    }

    private static bool IsZero(string name) => name.StartsWith("GL-ZERO-", StringComparison.Ordinal);

    private static TargetDefinition Frozen(JsonElement fixture) =>
        GltdJsonReader.Read(File.ReadAllBytes(Repo.PathTo("targets", "frozen", "zero-grid-1", fixture.GetProperty("id").GetString() + ".gltd.json"))).Definition!;

    private static BuiltInTarget Target(string name) => Targets.Value.Single(t => t.Name == name);

    /// <summary>
    /// Where the committed file of a reference sheet is: in <c>targets/</c>, or for the large format sheets entry 243 section 1.4 redrew as
    /// 2 by 2 sets, frozen under the identifier printed on them.
    /// </summary>
    private static string Committed(BuiltInTarget target) =>
        LibraryBuilder.RedrawnLarge.Any(r => target.FileName == r.Source + ".gltd.json")
            ? Repo.PathTo("targets", "frozen", "large-format-1", target.Definition.Id + ".gltd.json")
            : Repo.PathTo("targets", target.FileName);
}

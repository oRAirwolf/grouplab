using System.Text.Json;
using GroupLab.Cli.Library;
using GroupLab.Core.Gltd;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Derivation;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Gltd.Validation;
using GroupLab.Core.Tests.Support;

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
        var target = Target(name);

        var encoding = GltdBinary.Encode(target.Definition).Encoding!;

        Assert.Equal(fixture.GetProperty("bodyHex").GetString(), Convert.ToHexStringLower(encoding.Body));
        Assert.Equal(fixture.GetProperty("id").GetString(), target.Definition.Id);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void CommittedFileIsTheCanonicalBuild(string name)
    {
        var target = Target(name);
        string path = Repo.PathTo("targets", target.FileName);

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
        string[] expectedRowBands = name is "GL-ZERO-MOA-100Y" or "GL-ZERO-MIL-100M" ? ["/grids/0/bottom", "/grids/0/top"] : [];
        Assert.Equal(expectedRowBands, diagnostics.Where(d => d.Code == "validate.markerRowBand").Select(d => d.Path).Order());

        // Every built-in brackets every bull since the geometry change of docs/NOTES-FROM-PLANNING.md entry 13, so test
        // 26f is an error and the narrow marker bands are the library's only findings (TARGET-SCHEMA.md section 7).
        Assert.All(diagnostics, d => Assert.Equal("validate.markerRowBand", d.Code));
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void GeometryMatchesLayoutsJson(string name)
    {
        var d = Target(name).Definition;
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
        var source = GltdJsonReader.Read(File.ReadAllBytes(Repo.PathTo("targets", Target(name).FileName))).Definition!;
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

    private static BuiltInTarget Target(string name) => Targets.Value.Single(t => t.Name == name);
}

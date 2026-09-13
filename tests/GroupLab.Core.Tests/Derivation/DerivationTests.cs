using System.Text.Json;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Derivation;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Derivation;

/// <summary>The derivation rules against the validated solvers, and conformance tests 36, 37 and 37a.</summary>
public class DerivationTests
{
    private static readonly Lazy<JsonDocument> Markers = new(() =>
        JsonDocument.Parse(File.ReadAllBytes(Repo.PathTo("tests", "GroupLab.Core.Tests", "Fixtures", "layout-markers.json"))));

    private static readonly Lazy<JsonDocument> Layouts = new(() =>
        JsonDocument.Parse(File.ReadAllBytes(Repo.PathTo("tools", "layout", "layouts.json"))));

    public static TheoryData<string> SheetNames()
    {
        var data = new TheoryData<string>();
        foreach (var d in ReferenceFixtures.Definitions)
        {
            data.Add(d.GetProperty("name").GetString()!);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(SheetNames))]
    public void DecodedReferenceFrameCarriesTheSolverMarkers(string name)
    {
        var fixture = ReferenceFixtures.Named(name);
        var decoded = GltdBinary.Decode([Convert.FromHexString(fixture.GetProperty("frameHex").GetString()!)]).Definition!;
        var expected = SolverEntry(name.Replace(" (3x2)", "", StringComparison.Ordinal));

        var derived = FiducialDerivation.Derive(decoded);

        Assert.Null(derived.Error);
        var solverMarks = expected.GetProperty("marks").EnumerateArray()
            .Select(m => new PointDmm(m[0].GetInt32(), m[1].GetInt32()))
            .OrderBy(p => p.Y).ThenBy(p => p.X);
        Assert.Equal(solverMarks, derived.Markers!.Positions);
        Assert.Equal(expected.GetProperty("dropped").GetInt32(), derived.Markers.Dropped);
        if (expected.TryGetProperty("candidatesX", out var cx))
        {
            Assert.Equal(cx.GetArrayLength() * expected.GetProperty("candidatesY").GetArrayLength(), derived.Markers.Candidates);
        }

        var stored = decoded.Fiducials!.Markers!;
        Assert.Equal(derived.Markers.Positions, stored.Select(m => new PointDmm(m.X, m.Y)));
        Assert.Equal(stored.Count, stored.Select(m => m.Id).Distinct().Count());
        if (decoded.Tiling is null)
        {
            Assert.Equal(Enumerable.Range(0, stored.Count), stored.Select(m => m.Id));
        }
    }

    [Fact]
    public void Test37aTiesTowardZeroOnGlZeroMil100Y()
    {
        Assert.Equal([0, 91, 183, 274, 366, 457, 549, 640, 732], MeasurementGridLines.Offsets(732, 8));
    }

    [Fact]
    public void TieRuleIsTheIntegerFormOfSection2()
    {
        Assert.Equal(0, DerivedRounding.Divide(1, 2));
        Assert.Equal(1, DerivedRounding.Divide(3, 2));
        Assert.Equal(2, DerivedRounding.Divide(16, 10));
        Assert.Equal(1, DerivedRounding.Divide(15, 10));
        Assert.Equal(-1, DerivedRounding.Divide(-3, 2));
        Assert.Equal(145, DerivedRounding.Boundary(727, 1, 5));
        Assert.Equal(582, DerivedRounding.Boundary(727, 4, 5));
    }

    [Fact]
    public void Test36EveryZeroingLineIsWithinHalfADmmOfItsTrueAngle()
    {
        foreach (var z in Layouts.Value.RootElement.GetProperty("zero").EnumerateArray())
        {
            int half = z.GetProperty("half").GetInt32(), divisions = z.GetProperty("divisions").GetInt32();
            double unit = z.GetProperty("unit_dmm").GetDouble(), halfUnits = z.GetProperty("half_units").GetDouble();
            var offsets = MeasurementGridLines.Offsets(half, divisions);

            Assert.Equal(z.GetProperty("offsets").EnumerateArray().Select(o => o.GetInt32()), offsets);
            for (int i = 0; i <= divisions; i++)
            {
                double trueOffset = unit * halfUnits * i / divisions;
                Assert.True(Math.Abs(offsets[i] - trueOffset) <= 0.5,
                    $"{z.GetProperty("name").GetString()} line {i}: {offsets[i]} against {trueOffset:0.000}.");
            }
        }
    }

    [Fact]
    public void Test37LinePositionsAreSymmetricAndMonotone()
    {
        foreach (var (half, divisions) in new[] { (798, 6), (732, 8), (727, 5), (800, 8) })
        {
            var positions = MeasurementGridLines.Positions(1079, half, divisions);
            for (int i = 1; i < positions.Count; i++)
            {
                Assert.True(positions[i] > positions[i - 1]);
            }

            for (int i = 0; i < positions.Count; i++)
            {
                Assert.Equal(1079 - positions[i], positions[^(i + 1)] - 1079);
            }
        }
    }

    [Fact]
    public void DataBlockCellsReproduceSection310()
    {
        var reference = DataBlockCells.Derive(new DataBlock(120, 2364, 1919, 310, DataBlockLayout.Fields3x3, FieldSet.Standard9, 280, "black", null, null, null));
        Assert.Equal([0, 540, 1079], reference.Cells.Take(3).Select(c => c.X - 120));
        Assert.Equal([540, 539, 540], reference.Cells.Take(3).Select(c => c.Width));
        Assert.Equal([2369, 2469, 2569], reference.Cells.Where((_, i) => i % 3 == 0).Select(c => c.Y));
        Assert.All(reference.Cells, c => Assert.Equal(100, c.Height));
        Assert.Equal(["date", "distance", "cartridge", "bullet", "powder", "brass", "primer", "seating", "notes"], reference.Cells.Select(c => c.Key));
        Assert.Equal(new DataFieldCell("reserve", 1759, 2379, 280, 280), reference.Reserve);

        var zero = DataBlockCells.Derive(new DataBlock(120, 2464, 1919, 210, DataBlockLayout.Fields3x2, FieldSet.Standard6, 210, "black", null, null, null));
        Assert.Equal([0, 563, 1126], zero.Cells.Take(3).Select(c => c.X - 120));
        Assert.Equal(1689, zero.Cells[2].X + zero.Cells[2].Width - 120);
        Assert.Equal(6, zero.Cells.Count);
        Assert.Equal(new DataFieldCell("reserve", 1829, 2464, 210, 210), zero.Reserve);
    }

    [Fact]
    public void Corners1TiesTowardZeroOnAnOddModule()
    {
        // 65 x 3 = 195 dmm of footprint: the top centre is 120 + 97.5, which rounds down.
        Assert.Equal(new PointDmm(217, 217), Corners1.Positions(2159, 2794, 0, 2, 3)[0]);
    }

    private static JsonElement SolverEntry(string name) =>
        Markers.Value.RootElement.GetProperty("layouts").EnumerateArray()
            .Concat(Markers.Value.RootElement.GetProperty("zero").EnumerateArray())
            .Single(e => e.GetProperty("name").GetString() == name);
}

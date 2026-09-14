using System.Text.Json.Nodes;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 25 section 1: units change display and never storage, the angular figures follow docs/STATISTICS.md
/// section 12.5 and need a shot distance, "mil" is the milliradian a turret is marked in, and a file means the same whatever the units.
/// </summary>
public class UnitsTests
{
    [Fact]
    public void LengthsAndDistancesConvertExactlyAndRoundTrip()
    {
        Assert.Equal(2.54, UnitSettings.FromInches(1, LinearUnit.Centimetre), 15);
        Assert.Equal(25.4, UnitSettings.FromInches(1, LinearUnit.Millimetre), 15);
        Assert.Equal(100, UnitSettings.DistanceFromInches(3600, DistanceUnit.Yard), 12);
        Assert.Equal(100, UnitSettings.DistanceFromInches(UnitSettings.DistanceToInches(100, DistanceUnit.Metre), DistanceUnit.Metre), 12);
        foreach (var unit in Enum.GetValues<LinearUnit>())
        {
            Assert.Equal(0.914, UnitSettings.ToInches(UnitSettings.FromInches(0.914, unit), unit), 14);
        }

        Assert.Equal("2.32 cm", UnitSettings.Metric.Length(0.914));
        Assert.Equal("0.914 in", UnitSettings.Imperial.Length(0.914));
        Assert.Equal("23.2 mm", (UnitSettings.Metric with { Linear = LinearUnit.Millimetre }).Length(0.914));
        Assert.Equal("100 yd", UnitSettings.Imperial.DistanceText(3600));
    }

    /// <summary>Section 12.5's anchor, "1 inch at 100 yards is exactly 1.000000 SMOA and 0.954930 MOA", through the setting.</summary>
    [Fact]
    public void AngularFiguresFollowSection125AndNeedADistance()
    {
        double hundredYards = UnitSettings.DistanceToInches(100, DistanceUnit.Yard);
        Assert.Equal(1.000000, (UnitSettings.Imperial with { Angular = AngularUnit.Smoa }).Angle(1, hundredYards)!.Value, 6);
        Assert.Equal(0.954930, UnitSettings.Imperial.Angle(1, hundredYards)!.Value, 6);
        Assert.Equal(2000 * Math.Atan(1.0 / 7200), UnitSettings.Metric.Angle(1, hundredYards)!.Value, 12);
        Assert.Equal("mil", UnitSettings.Symbol(UnitSettings.Metric.Angular));
        Assert.Null(UnitSettings.Imperial.Angle(1, null));
        Assert.Null(UnitSettings.Imperial.AngleText(1, null));
        Assert.DoesNotContain(AngularUnit.Mil, UnitSettings.AngularChoices);
    }

    [Fact]
    public void TheFirstRunDefaultFollowsTheRegion()
    {
        Assert.Equal(UnitSettings.Imperial, UnitSettings.ForRegion("US"));
        Assert.Equal(UnitSettings.Imperial, UnitSettings.ForRegion("us"));
        Assert.Equal(UnitSettings.Metric, UnitSettings.ForRegion("DE"));
        Assert.Equal(UnitSettings.Metric, UnitSettings.ForRegion(null));
    }

    /// <summary>The classic failure entry 25 names: a saved file that reads differently depending on a setting on the machine that opens it.</summary>
    [Fact]
    public void AMarkingFileHoldsCanonicalValuesWhateverTheUnitsAndRecordsThemBeside()
    {
        var session = new MarkingSession();
        session.SetScale(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1));
        session.SetShotDistance(UnitSettings.DistanceToInches(300, DistanceUnit.Metre));
        foreach (var p in new PointD[] { new(40, 40), new(70, 45), new(55, 80), new(30, 65), new(62, 58) })
        {
            session.AddShot(p);
        }

        var metric = JsonNode.Parse(MarkingFile.Write(session.State, displayUnits: UnitSettings.Metric))!.AsObject();
        var imperial = JsonNode.Parse(MarkingFile.Write(session.State, displayUnits: UnitSettings.Imperial))!.AsObject();
        Assert.Equal("Centimetre", (string?)metric["displayUnits"]!["linear"]);
        Assert.Equal("Mrad", (string?)metric["displayUnits"]!["angular"]);
        Assert.Equal("Yard", (string?)imperial["displayUnits"]!["distance"]);
        metric.Remove("displayUnits");
        imperial.Remove("displayUnits");
        Assert.Equal(imperial.ToJsonString(), metric.ToJsonString());
        Assert.Equal(300 / 0.0254, (double)metric["shotDistanceInches"]!, 9);
        Assert.Equal(session.State.ShotDistanceInches, MarkingFile.Read(MarkingFile.Write(session.State, displayUnits: UnitSettings.Metric)).State.ShotDistanceInches);
        Assert.Equal("a single 2.54 cm reference length, which assumes the photograph is square on and the sheet flat", session.State.Scale!.Describe(UnitSettings.Metric));
    }
}

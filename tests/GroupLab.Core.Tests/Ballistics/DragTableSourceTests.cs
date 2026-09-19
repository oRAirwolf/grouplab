using System.Globalization;
using System.Text.Json;
using GroupLab.Core.Ballistics;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Ballistics;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 111 section 1: the G1 and G7 tables carried are the values two independent transcriptions of the standard
/// functions agree on, py-ballisticcalc 2.3.1's and poncelet's from JBM Ballistics' McCoy tables, both committed in reference/drag-tables as
/// the strings they publish. The carried tables are held to each, point by point, at the four decimals they publish; the Mach points too.
/// </summary>
public class DragTableSourceTests
{
    private static List<(double Mach, double Cd)> Transcription(string file, string table) =>
        [.. JsonDocument.Parse(File.ReadAllText(Repo.PathTo("reference", "drag-tables", file))).RootElement.GetProperty(table).EnumerateArray()
            .Select(p => (double.Parse(p[0].GetString()!, CultureInfo.InvariantCulture), double.Parse(p[1].GetString()!, CultureInfo.InvariantCulture)))];

    [Theory]
    [InlineData("G1", 79)]
    [InlineData("G7", 84)]
    public void TheCarriedTableIsWhatBothTranscriptionsAgreeOn(string table, int points)
    {
        var carried = DragTables.For(table == "G1" ? DragModel.G1 : DragModel.G7);
        Assert.Equal(points, carried.Count);
        foreach (string source in new[] { "py-ballisticcalc.json", "poncelet.json" })
        {
            var transcribed = Transcription(source, table);
            Assert.Equal(points, transcribed.Count);
            for (int i = 0; i < points; i++)
            {
                Assert.True(Math.Abs(carried[i].Mach - transcribed[i].Mach) < 5e-5 && Math.Abs(carried[i].Cd - transcribed[i].Cd) < 5e-5,
                    $"{table} point {i}: carried ({carried[i].Mach}, {carried[i].Cd}), {source} ({transcribed[i].Mach}, {transcribed[i].Cd})");
            }
        }
    }

    /// <summary>
    /// The intentional difference from ballistics.js the transcription check does not cover, since it flies the JavaScript's own tables: its G1
    /// is not the standard function above Mach 0.85, and its G7 agrees below Mach 3.5.
    /// </summary>
    [Fact]
    public void TheJavaScriptsG1IsNotTheStandardAndItsG7IsBelowMachThreePointFive()
    {
        Assert.Equal(0.4805, DragTables.Cd(1.0, DragModel.G1), 12);
        Assert.Equal(0.5210, DragTables.Cd(1.0, DragModel.G1, javaScript: true), 12);
        Assert.Equal(0.6625, DragTables.Cd(1.4, DragModel.G1), 12);
        Assert.Equal(DragTables.Cd(0.8, DragModel.G1), DragTables.Cd(0.8, DragModel.G1, javaScript: true), 12);
        foreach (var (mach, _) in DragTables.JavaScriptG7.Where(p => p.Mach < 3.5))
        {
            Assert.Equal(DragTables.Cd(mach, DragModel.G7), DragTables.Cd(mach, DragModel.G7, javaScript: true), 12);
        }
    }
}

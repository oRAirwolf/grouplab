using System.Reflection;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Marking;
using GroupLab.Core.Tests.Support;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// docs/PROOF-CHECKLIST.md row 8, subgroups within one sheet, against its proposed gate: each subgroup's figures equal its shots analyzed as a
/// session of their own. A ladder sheet is read on the promise that the 41.5 grain row says what a sheet holding only that row would say;
/// nothing held the subgroup to that, only to its own count of shots. No real ladder sheet has been shot, so Alan's 2026-09-26 6 ARC scan
/// (dominus-k, 25 shots, from the test-data release) is read as one: its five rows as five loads, one shot excluded as a pulled shot, as a
/// person would mark it. Each row's figures are then held to the same scan with every other row's shots deleted, analysed as a session.
/// <para>
/// The sheet of its own is told which bulls were aimed at, the row's five, as Shots per bull lets a person say. Without it, four of the first
/// row's five shots go to the row below once the shots that held that row are gone (measured 2026-10-08): the image alone cannot say which
/// row a lone group was fired at, and that is what entry 141's aimed-at bulls are for.
/// </para>
/// </summary>
public class LadderSubgroupTests(ITestOutputHelper output)
{
    private const string File = "load-sheet-6arc-dominus-k-2026-09-26.png";
    private const string Published = "d1fdd053251649a0e2929a7ed1d83a5043036bd658e9995e114ea4cb74be4d9d";

    [Fact]
    public void EachRowOfALadderSheetReadsAsASheetOfItsOwn()
    {
        string path = TestData.Path(File, @"C:\Dev\grouplab-originals\range-2026-09-26\rebuilt");
        if (!System.IO.File.Exists(path))
        {
            Assert.True(true, "skipped: the 2026-09-26 dominus-k scan is not on this machine");
            return;
        }

        Assert.True(TestData.Matches(path, Published), $"{path} is not the published copy of the dominus-k scan");
        var definition = GltdJsonReader.ReadFile(Repo.PathTo("targets", "GL-CF25-LTR-D.gltd.json")).Definition!;
        var (grey, metadata) = ImageLoader.Load(path);
        var (value, _) = ImageLoader.LoadMaxChannel(path);
        var result = AutomaticMarking.Run(grey, value, metadata, definition, new OpenCvSharpBackend(), calibre: Calibre.Of(0.243));
        Assert.Null(result.Failure);

        var sheet = new MarkingSession();
        sheet.LoadDetections(result.Scale!, result.Bulls, result.Detections, result.Assignment, result.Rejected ?? [], result.Summary);
        sheet.SetCalibre(Calibre.Of(0.243));
        sheet.SetShotDistance(3600);
        var scoring = sheet.State.Bulls.Where(b => b.Scoring).OrderBy(b => b.Index).ToList();
        Assert.Equal(25, scoring.Count);
        string[] loads = ["27.0 gr", "27.3 gr", "27.6 gr", "27.9 gr", "28.2 gr"];
        for (int k = 0; k < scoring.Count; k++)
        {
            sheet.SetSubgroup(scoring[k].Index, loads[k / 5]);
        }

        // One shot left out, as a person marks a pulled shot: the subgroup and the sheet of its own both leave it out.
        var pulled = sheet.State.Shots.First(s => s.IsShot && s.Bull == scoring[7].Index);
        sheet.SetExclusion(pulled.Id, ExclusionReason.PulledShot);

        var report = GroupAnalysis.Subgroups(sheet.State)!;
        Assert.Equal(loads, report.Subgroups.Select(g => g.Name));
        Assert.Null(report.ComparisonUnavailable);
        Assert.Equal(sheet.State.Shots.Count(s => s.IsShot && s.Exclusion is null && s.Bull is { } b && scoring.Any(x => x.Index == b)), report.Subgroups.Sum(g => g.Shots));

        foreach (var subgroup in report.Subgroups)
        {
            var own = new MarkingSession(sheet.State);
            own.ClearSubgroups();
            foreach (var shot in own.State.Shots.Where(s => s.Bull is not { } b || !subgroup.Bulls.Contains(b)).ToList())
            {
                own.DeleteShot(shot.Id);
            }

            own.SetAssignmentRule(AimedBulls.For(own.State.Bulls, subgroup.Bulls));
            Assert.All(own.State.Shots.Where(s => s.IsShot), s => Assert.Equal(sheet.State.Find(s.Id)!.Bull, s.Bull));

            var alone = GroupAnalysis.Analyse(own.State);
            Assert.Null(alone.Problem);
            Assert.Equal(subgroup.Shots, alone.Counted!.Shots);
            Same(subgroup.Name, alone.Counted, subgroup.Figures!);
            output.WriteLine($"{subgroup.Name}: {subgroup.Shots} shots, mean radius {subgroup.Figures!.MeanRadius?.Value:0.000} in, the same as its sheet of its own");
        }
    }

    /// <summary>Every figure the two carry, number for number to 1e-12 relative, words for words, recursing into the estimates and tests.</summary>
    private static void Same(string where, object? expected, object? actual)
    {
        if (expected is null || actual is null)
        {
            Assert.True(expected is null && actual is null, $"{where}: one is missing ({expected ?? "null"} against {actual ?? "null"})");
            return;
        }

        switch (expected)
        {
            case double e:
                double a = (double)actual;
                Assert.True(e.Equals(a) || Math.Abs(e - a) <= 1e-12 * Math.Max(Math.Abs(e), Math.Abs(a)), $"{where}: {e:R} on its own, {a:R} as a subgroup");
                return;
            case string or int or bool or ulong or Enum:
                Assert.True(Equals(expected, actual), $"{where}: {expected} on its own, {actual} as a subgroup");
                return;
        }

        foreach (var property in expected.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetIndexParameters().Length == 0 && p.Name != "EqualityContract"))
        {
            Same($"{where}.{property.Name}", property.GetValue(expected), property.GetValue(actual));
        }
    }
}

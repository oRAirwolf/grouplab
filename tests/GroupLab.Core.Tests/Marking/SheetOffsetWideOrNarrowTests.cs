using GroupLab.Cli;
using GroupLab.Core.Detection;
using GroupLab.Core.Marking;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 143, question 46: is the sheet offset better solved over every printed bull, or only over the bulls the
/// shooter says they aimed at?
/// <para>
/// <b>Why it is a real question and not a tidy-up.</b> <see cref="ImpactOffsets.Solve"/>'s own comment says only the aimed bulls are
/// candidates and that this "is not a refinement". <see cref="MarkingSession"/> passes it every scoring bull, because
/// <see cref="AimedBulls.For"/> lists them all and gives nought shots to the ones nobody aimed at. Narrowing it to a count above zero,
/// which is what the comment describes, put five of twenty shots on bulls nobody aimed at in `SheetOffsetAssignmentTests`.
/// </para>
/// <para>
/// So the code and its documentation disagree, and the measurement decides which to change. Scan 5 is the sheet for it: a load the rifle
/// was not zeroed for, twenty shots at bulls 2 to 5 of every row, and an offset the shooter's own table pins exactly.
/// </para>
/// </summary>
public class SheetOffsetWideOrNarrowTests
{
    private const string Folder = @"C:\Dev\grouplab-range-2026-09-20\scans";

    private const string Scan5 = "5-600-dpi09202026.png";

    /// <summary>Bulls 2, 3, 4 and 5 of every row on a five by five sheet, which is Alan's table for scan 5.</summary>
    private static bool Aimed(string label) => int.TryParse(label, out int n) && n % 5 != 1;

    [Fact]
    public void TheWideAndNarrowSolvesAreMeasuredAgainstTheShootersOwnTable()
    {
        string path = Path.Combine(Folder, Scan5);
        if (!File.Exists(path))
        {
            Assert.True(true, $"skipped: {path} is not on this machine, so the two solves were not measured");
            return;
        }

        var result = AnalyzeVerb.Analyze(GroupLab.Tests.Support.Temp.Readable(path), null, out string? failure,
            [Repo.PathTo("targets")], Calibre.Parse("0.224", out _));
        Assert.Null(failure);
        Assert.NotNull(result);

        var scale = result!.Automatic.Scale!;
        var bulls = result.Automatic.Bulls.Where(b => b.Scoring).OrderBy(b => b.Index).ToList();
        var holes = result.Automatic.Detections
            .Select(d => scale.ToTarget(d.Image))
            .Select(p => new Offset(p.X * 254, p.Y * 254))
            .ToList();

        var bullOffsets = bulls.Select(b => new Offset(b.Declared!.Value.X, b.Declared!.Value.Y)).ToList();
        var everyBull = Enumerable.Range(0, bulls.Count).ToList();
        var aimedOnly = Enumerable.Range(0, bulls.Count).Where(i => Aimed(bulls[i].Label)).ToList();

        Assert.Equal(20, holes.Count);
        Assert.Equal(20, aimedOnly.Count);

        var wide = ImpactOffsets.Solve("every printed bull", holes, bullOffsets, everyBull);
        var narrow = ImpactOffsets.Solve("the bulls he aimed at", holes, bullOffsets, aimedOnly);

        // What the shooter's own table implies: each of the twenty shots was fired at one of the twenty aimed bulls, so the offset is the
        // mean displacement of the holes from those bulls. It needs no solver and no assumption beyond the table itself.
        var aimedBulls = aimedOnly.Select(i => bullOffsets[i]).ToList();
        var truth = new Offset(
            holes.Average(h => h.X) - aimedBulls.Average(b => b.X),
            holes.Average(h => h.Y) - aimedBulls.Average(b => b.Y));

        static double Miss(Offset a, Offset b) =>
            Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y))) / 254;

        double wideMiss = Miss(wide.Shift, truth), narrowMiss = Miss(narrow.Shift, truth);

        // MEASURED 2026-09-23, and it answers question 46 in a way neither side of it expected.
        //
        //   the shooter's table implies   -1.163, -1.105 in
        //   over every printed bull       -0.995, -0.924 in, out by 0.247 in, certain FALSE
        //   over the aimed bulls only     -0.995, -0.924 in, out by 0.247 in, certain TRUE
        //
        // The two solves return the SAME shift. They differ only in confidence, and MarkingSession applies an offset only where it is
        // certain. So the wide solve does nothing at all, the assignment falls through to the matching, which does consider only the
        // aimed bulls, and scan 5 comes out exactly as the shooter's table says.
        //
        // Narrowing the solve does not improve the offset. It makes GroupLab confident of an offset a quarter of an inch from the truth,
        // and acting on it is what put five of twenty shots on bulls nobody aimed at.
        Assert.Equal(wide.Shift.X, narrow.Shift.X, 6);
        Assert.Equal(wide.Shift.Y, narrow.Shift.Y, 6);
        Assert.False(wide.Certain, "the wide solve has become certain, so it would now move shots by an offset that is not the shooter's");
        Assert.True(narrow.Certain);
        Assert.True(Math.Abs(wideMiss - narrowMiss) < 1e-9);

        // The finding that matters most, and the reason neither reading should be trusted to move a shot on this sheet.
        Assert.True(wideMiss > 0.2 && wideMiss < 0.3,
            $"the solved offset is {wideMiss:0.000} in from the one the shooter's table implies, where 0.247 in was measured. "
            + "If that gap has closed, the solver has improved and this test should be re-read rather than silenced.");
    }
}

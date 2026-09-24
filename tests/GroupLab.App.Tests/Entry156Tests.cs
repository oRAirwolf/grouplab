using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 156 on screen: the Ballistics screen's hit probability fills itself from what GroupLab measured, shows the
/// first and second round with the dope, each with its interval, says what dominates the interval and what costs the most, states its
/// assumptions, and draws the scatter and the curve. The presets set every uncertainty at once and an edit makes them custom.
/// </summary>
public class Entry156Tests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static (MainWindow Window, string Path) Open()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        var rifle = new Rifle("Tikka T3x", 0.25, AngularUnit.Moa) { SightHeightInches = 1.75, ZeroDistanceYards = 100 };
        var load = new Load("H4350 41.5", null)
        {
            MuzzleVelocityFps = 2710, MuzzleVelocitySdFps = 10, BallisticCoefficient = 0.326, DragModel = GroupLab.Core.Ballistics.DragModel.G7,
            BcReference = GroupLab.Core.Ballistics.ReferenceAtmosphere.Icao, BulletWeightGrains = 140,
        };
        window.Book = RecordBook.Empty.With(rifle).With(load);
        window.Session.SetShotDistance(3600);
        window.Session.SetEquipment(rifle, null, "H4350 41.5");
        window.CalibreAnswered();
        window.Analyse();
        Settle();
        window.ShowBallistics();
        Settle();
        return (window, path);
    }

    private static void Close(MainWindow window, string path)
    {
        window.Close();
        GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
    }

    /// <summary>Sections 1, 3 and 4: the answer from the analysed group, with the dope, both rounds and their intervals, the costs and the assumptions.</summary>
    [AvaloniaFact]
    public void TheChanceOfAHitComesFromTheMeasuredGroupWithItsInterval()
    {
        var (window, path) = Open();
        try
        {
            // Section 1: the distance starts at the one the group was shot at.
            Assert.Equal("100", window.HitDistanceShown);
            window.WorkOutHit("600", 0, "30", "30");
            Settle();
            var text = window.HitShown.ToList();
            Assert.StartsWith("From the group open in the analysis: sigma ", window.HitFromWords, StringComparison.Ordinal);
            Assert.Contains(text, t => t.StartsWith("At 600 yd: elevation ", StringComparison.Ordinal) && t.Contains(" up", StringComparison.Ordinal));
            var first = Assert.Single(text, t => t.StartsWith("First round on a 30.000 in circle at 600 yd: ", StringComparison.Ordinal));
            Assert.Matches(@": (more than |under )?[\d.]+ percent \([\d.]+ to [\d.]+\)\.$", first);
            Assert.Contains(text, t => t.StartsWith("Second round, corrected from where the first landed: ", StringComparison.Ordinal));
            Assert.Contains(text, t => t.StartsWith("Most of that interval is ", StringComparison.Ordinal));
            Assert.Contains("What costs the most", text);
            Assert.Contains(text, t => t.StartsWith("The wind call, drawn per string, ", StringComparison.Ordinal));
            Assert.Contains(text, t => t.StartsWith("The rifle's own dispersion, drawn per shot, ", StringComparison.Ordinal));
            Assert.Contains(text, t => t.StartsWith("All together the first round spreads ", StringComparison.Ordinal));
            Assert.Contains(MainWindow.HitAssumptions, text);
            Assert.Contains(text, t => t.StartsWith("10,000 strings, seed 20260924: the same seed gives the same answer.", StringComparison.Ordinal));
            Assert.Contains("first-round impacts", window.HitDrawings.Scatter, StringComparison.Ordinal);
            Assert.StartsWith("First-round hit probability against distance", window.HitDrawings.Curve, StringComparison.Ordinal);

            // Section 4 item 3: never more than two significant figures anywhere a probability is printed.
            Assert.DoesNotMatch(@"\d\.\d\d\d? percent", string.Join(" ", text));
        }
        finally
        {
            Close(window, path);
        }
    }

    /// <summary>Section 8 item 2: a preset sets every uncertainty at once, its situation is said in a sentence, and an edit makes it custom.</summary>
    [AvaloniaFact]
    public void APresetSetsEveryUncertaintyAndAnEditMakesItCustom()
    {
        var (window, path) = Open();
        try
        {
            Assert.Equal(("Lasered distance, estimated wind", "3"), window.HitPresetShown);
            window.WorkOutHit("600", 0, "30", "30", preset: 0);
            Settle();
            Assert.Equal(("Known distance, measured air", "1.5"), window.HitPresetShown);
            window.EditWindCall("2.5");
            Settle();
            Assert.Equal(("Custom", "2.5"), window.HitPresetShown);

            // The unit toggle rewrites the figures and keeps the preset as it was.
            window.UseUnits(UnitSettings.Metric);
            Settle();
            Assert.Equal(("Custom", "4.023"), window.HitPresetShown);
            window.UseUnits(UnitSettings.Imperial);
            Settle();
            Assert.Equal(("Custom", "2.5"), window.HitPresetShown);
        }
        finally
        {
            Close(window, path);
        }
    }

    /// <summary>Section 3 item 2 and a typed precision: a string's figures, and the interval saying the typed figure's own uncertainty is left out.</summary>
    [AvaloniaFact]
    public void AStringAndATypedPrecisionSayWhatTheyAre()
    {
        var (window, path) = Open();
        try
        {
            window.WorkOutHit("600", 1, "20", "30", from: 2, precision: "0.5", shots: "5");
            var text = window.HitShown.ToList();
            Assert.StartsWith("Typed, so its own uncertainty is not known", window.HitFromWords, StringComparison.Ordinal);
            Assert.Contains(text, t => t.StartsWith("First round on a 20.000 by 30.000 in rectangle at 600 yd: ", StringComparison.Ordinal));
            Assert.Contains(text, t => t.StartsWith("In a string of 5 shots fired on one reading: at least one hit ", StringComparison.Ordinal) && t.Contains(" hits expected (", StringComparison.Ordinal));
            Assert.Contains(text, t => t.Contains("the precision was typed, so its own uncertainty is not in it", StringComparison.Ordinal));
            Assert.Contains("The precision was typed, so its own uncertainty is not known and is left out of the interval.", text);

            window.WorkOutHit("600", 2, "18", "30", from: 2, precision: "0.5");
            Assert.Contains(window.HitShown, t => t.StartsWith("First round on an IPSC outline 18.000 by 30.000 in at 600 yd: ", StringComparison.Ordinal));

            // Section 1: a target's size may be an angle, turned into a length at the distance. Two MOA at 600 yd is 12.566 in.
            window.WorkOutHit("600", 0, "2", "2", from: 2, precision: "0.5", sizeUnit: 1);
            Assert.Contains(window.HitShown, t => t.StartsWith("First round on a 12.566 in circle at 600 yd: ", StringComparison.Ordinal));
        }
        finally
        {
            Close(window, path);
        }
    }

    /// <summary>Section 1: every figure behind the disclosure, and the precision and the preset, explain themselves from the glossary of entry 154.</summary>
    [AvaloniaFact]
    public void TheInputsExplainThemselves()
    {
        var (window, path) = Open();
        try
        {
            var explained = window.GetLogicalDescendants().OfType<TextBlock>().Where(b => b.Classes.Contains(TermHelp.Class)).Select(b => b.Tag as string).ToHashSet();
            foreach (string term in new[] { "rifle-precision", "wind-call", "range-error", "zero-error", "confidence-preset", "trials", "string", "hit-probability" })
            {
                Assert.Contains(term, explained);
            }
        }
        finally
        {
            Close(window, path);
        }
    }
}

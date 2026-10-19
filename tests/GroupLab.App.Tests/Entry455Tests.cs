using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Ballistics;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Statistics;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 455: Point blank, Ballistics' third view, used as a person would: the Test load's rifle and load, a mean
/// radius typed, the whitetail chosen, and the answer read off the screen; then the zero kept on the rifle and put back.
/// </summary>
public class Entry455Tests
{
    private static void Settle()
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    private static MainWindow Ready()
    {
        var (window, _, _) = Entry109Tests.Sheet();
        window.Width = 1440;
        window.Height = 900;
        window.Book = RecordBook.Empty.With(new Rifle("Test rifle", 0.1, AngularUnit.Mrad)).With(new Load("Test load", null) { MuzzleVelocitySdFps = 10 });
        window.ShowBallistics();
        Settle();
        window.SetBallisticFields("Test rifle", "Test load", "1.75", "100", "2710", "0.326", model: 2, reference: 1, weight: "140");
        window.KeepBallistics();
        Settle();
        return window;
    }

    [AvaloniaFact]
    public void ThePointBlankViewGivesTheBandHowToSetItAndTheRifleZeroBesideIt()
    {
        var window = Ready();
        try
        {
            window.ShowBallisticView(MainWindow.BallisticView.PointBlank);
            window.SetPointBlank("0.63", "90", "Whitetail deer");
            window.FillPointBlank();
            Settle();
            var text = window.PointBlankText.ToList();
            Assert.Equal("Dead on 0 to 263 yd", text[0]);
            Assert.Contains(text, t => t.StartsWith("Zero 2.6 in high at 100 yd: 0.72 mil, 7 clicks up from your 100 yd zero.", StringComparison.Ordinal));
            Assert.Contains(text, t => t.StartsWith("Your rifle's 100 yd zero: dead on 0 to 184 yd", StringComparison.Ordinal));
            Assert.Contains(text, t => t.StartsWith("The classic figure, for reference:", StringComparison.Ordinal));
            Assert.Contains(PointBlankTargets.CheckTheLaw, text);
            Assert.Contains(PointBlankReport.NotCovered, text);
            Assert.Contains(PointBlankReport.ExpansionNotEntered, text);

            // A hundred percent is refused in words, never worked out.
            window.SetPointBlank("0.63", "100", "Whitetail deer");
            window.FillPointBlank();
            Assert.Contains(PointBlankModel.NoPromise, window.PointBlankText);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TheWindowThatCannotBeHeldSaysSoAndThePracticalTargetsCarryNoLawLine()
    {
        var window = Ready();
        try
        {
            window.ShowBallisticView(MainWindow.BallisticView.PointBlank);
            window.SetPointBlank("0.63", "90", "Whitetail deer", PointBlankMode.Window);
            window.FillPointBlank();
            Assert.Contains(window.PointBlankText, t => t.StartsWith("No single zero keeps 9 in 10 across 150 to 300 yd.", StringComparison.Ordinal));

            window.SetPointBlank("0.63", "90", "IPSC target, A zone");
            window.FillPointBlank();
            Assert.DoesNotContain(PointBlankTargets.CheckTheLaw, window.PointBlankText);
            Assert.Contains(window.PointBlankText, t => t.Contains("15 by 32.5 cm", StringComparison.Ordinal) || t.Contains("5.9 by 12.8 in", StringComparison.Ordinal));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void KeepingTheZeroMovesTheRifleAndPuttingItBackRestoresIt()
    {
        var window = Ready();
        try
        {
            window.ShowBallisticView(MainWindow.BallisticView.PointBlank);
            window.SetPointBlank("0.63", "90", "Whitetail deer");
            window.FillPointBlank();
            double far = Math.Round(window.PointBlankShown!.FarZeroYards!.Value);
            window.KeepPointBlankZero();
            Settle();
            var rifle = window.Book.FindRifle("Test rifle")!;
            Assert.Equal(far, rifle.ZeroDistanceYards);
            Assert.Equal(100, Assert.Single(rifle.PreviousZeros).ZeroDistanceYards);

            // With the new zero, the same point blank needs no change.
            Assert.Contains(window.PointBlankText, t => t.StartsWith($"Your {far:0} yd zero already gives this.", StringComparison.Ordinal) || t.StartsWith("Zero 0", StringComparison.Ordinal));

            window.UndoPointBlankZero();
            Settle();
            rifle = window.Book.FindRifle("Test rifle")!;
            Assert.Equal(100, rifle.ZeroDistanceYards);
            Assert.Empty(rifle.PreviousZeros);
        }
        finally
        {
            window.Close();
        }
    }
}

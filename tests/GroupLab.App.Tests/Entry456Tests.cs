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
/// NOTES-FROM-PLANNING.md entry 456 on the computer: the rifle page's Scope and mount part, the dope table holding in the reticle past the
/// last dialable range, the sight scale factor said beside the clicks it corrects, and Top Gun on the rifle page.
/// </summary>
public class Entry456Tests
{
    private static void Settle()
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    private static readonly Rifle Scoped = new("Test rifle", 0.1, AngularUnit.Mrad)
    {
        SightHeightInches = 1.75, ZeroDistanceYards = 100, ScopeTravel = 20, ScopeTravelKind = ScopeTravelKind.Total, RailCant = 6,
        SightScaleFactor = 0.98, WeightAsFiredLb = 11,
    };

    private static readonly Load TestLoad = new("Test load", null)
    {
        MuzzleVelocityFps = 2710, MuzzleVelocitySdFps = 10, BallisticCoefficient = 0.326, DragModel = DragModel.G7, BcReference = ReferenceAtmosphere.Icao,
        BulletWeightGrains = 140,
    };

    [AvaloniaFact]
    public void TheRiflePageGivesTheMountFiguresTheTrackingHelperAndTopGun()
    {
        var (window, _, _) = Entry109Tests.Sheet();
        try
        {
            window.Book = RecordBook.Empty.With(Scoped).With(TestLoad);
            window.EditRifle("Test rifle");
            Settle();
            var text = window.ScopeMountText.ToList();
            Assert.Contains("Scope and mount", text);
            Assert.Contains(MainWindow.PartHeading, text);
            Assert.Contains(text, t => t.StartsWith("The zero uses 1.2 mil of elevation with Test load.", StringComparison.Ordinal));
            Assert.Contains("Half the travel: 10.0 mil either way from the middle.", text);
            Assert.Contains(text, t => t.StartsWith("With the 6.0 mil rail, ", StringComparison.Ordinal) && t.Contains("which reaches", StringComparison.Ordinal));
            Assert.Contains("Top Gun expects about 1.04 MOA average group for Test load (Bryan Litz's trend, not a limit).", text);
            Assert.Contains(window.EquipmentFieldKeys, k => k == "sightScaleFactor");
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>A rifle's page opened again starts at its top, where the scope unit and click are, not where the last one was left.</summary>
    [AvaloniaFact]
    public void ARiflePageOpensAtItsTopAfterScrolling()
    {
        var (window, _, _) = Entry109Tests.Sheet();
        try
        {
            window.Book = RecordBook.Empty.With(Scoped).With(TestLoad);
            window.EditRifle("Test rifle");
            Settle();
            window.ShowScopeAndMount();
            Settle();
            Assert.True(window.EquipmentFormOffset > 100, $"the page did not scroll ({window.EquipmentFormOffset})");
            window.EditRifle("Test rifle");
            Settle();
            Assert.Equal(0, window.EquipmentFormOffset);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TheDopeHoldsInTheReticlePastTheElevationLeftAndSaysTheFactorIsApplied()
    {
        var (window, _, _) = Entry109Tests.Sheet();
        try
        {
            // 6 mil of travel and no rail cant: 3 mil up from the middle, less the zero's, runs out inside the table's 600 yd.
            window.Book = RecordBook.Empty.With(Scoped with { ScopeTravel = 6, RailCant = 0 }).With(TestLoad);
            window.ShowBallistics();
            Settle();
            window.SetBallisticFields("Test rifle", "Test load", "1.75", "100", "2710", "0.326", model: 2, reference: 1, weight: "140");
            Settle();
            window.FillDope();
            var rows = window.DopeRows.ToList();
            Assert.Contains(rows, r => r.Contains("out of elevation, hold", StringComparison.Ordinal));
            Assert.Contains(rows, r => r.Contains("SSF 0.980 applied", StringComparison.Ordinal));

            // A row the turret can still reach gives its clicks.
            Assert.Contains(rows, r => r.Contains("clicks up", StringComparison.Ordinal));
        }
        finally
        {
            window.Close();
        }
    }
}

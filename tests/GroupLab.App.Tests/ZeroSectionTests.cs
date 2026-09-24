using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.App.Theme;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entries 91, 92 and 93: the zero correction in its own section above the group statistics, with a refusal and a
/// shot count where the offset is smaller than the shots can resolve; and the concept's chrome, whose rail is built and whose destinations
/// are not.
/// </summary>
public class ZeroSectionTests
{
    private static MainWindow NewWindow()
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        return new MainWindow(store) { Width = 1400, Height = 900 };
    }

    /// <summary>A marking of ten shots on a circle, centred <paramref name="offsetInches"/> right of the point of aim.</summary>
    private static void Mark(MainWindow window, double offsetInches, double sigmaInches)
    {
        var session = window.Session;
        session.SetScale(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1));
        session.SetPointOfAim(new PointD(0, 0));
        double radius = sigmaInches * Math.Sqrt(2);
        for (int k = 0; k < 10; k++)
        {
            double angle = 2 * Math.PI * k / 10;
            session.AddShot(new PointD(100 * (offsetInches + (radius * Math.Cos(angle))), 100 * radius * Math.Sin(angle)));
        }

        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void AnOffsetInsideTheSamplingErrorIsRefusedWithAShotCount()
    {
        var window = NewWindow();
        window.Show();
        Mark(window, offsetInches: 0.10, sigmaInches: 0.27);

        var lines = window.ZeroText.ToList();

        // Entry 169 section 2: the offset in a compact block, one row an axis, in the length unit, MOA and mil.
        Assert.Contains(lines, t => t.StartsWith("Windage, ", StringComparison.Ordinal));
        Assert.Contains(lines, t => t.StartsWith("Elevation, ", StringComparison.Ordinal));
        Assert.Contains(lines, t => t.StartsWith("Not distinguishable from zero at 10 shots", StringComparison.Ordinal));
        Assert.Contains(lines, t => t.Contains("shots would settle it", StringComparison.Ordinal));
        Assert.DoesNotContain(lines, t => t.StartsWith("Dial", StringComparison.Ordinal));
        window.Close();
    }

    [AvaloniaFact]
    public void AnOffsetLargerThanTheSamplingErrorIsACorrectionAndTheTurretGoesTheOtherWay()
    {
        var window = NewWindow();
        window.Show();
        Mark(window, offsetInches: 1.2, sigmaInches: 0.27);

        var lines = window.ZeroText.ToList();

        var dial = Assert.Single(lines, t => t.StartsWith("Dial", StringComparison.Ordinal));
        Assert.Contains("left", dial, StringComparison.Ordinal);
        Assert.Contains(lines, t => t.StartsWith("Windage, right", StringComparison.Ordinal));
        window.Close();
    }

    /// <summary>Entry 97 section 2: with a rifle and the distance, the correction is in the scope's clicks, with what rounding leaves.</summary>
    [AvaloniaFact]
    public void WithARifleAndADistanceTheCorrectionIsInClicks()
    {
        var window = NewWindow();
        window.Show();
        Mark(window, offsetInches: 1.2, sigmaInches: 0.27);
        Assert.Contains(window.ZeroText, t => t.StartsWith("Set the shot distance to see MOA, mil and clicks", StringComparison.Ordinal));

        window.Session.SetShotDistance(3600);
        Dispatcher.UIThread.RunJobs();
        Assert.Contains(window.ZeroText, t => t.EndsWith("Choose a rifle to have it in clicks.", StringComparison.Ordinal));

        window.Session.SetEquipment(new Rifle("Tikka T3x", 0.25, GroupLab.Core.Statistics.AngularUnit.Moa), null, null);
        Dispatcher.UIThread.RunJobs();
        var dial = Assert.Single(window.ZeroText, t => t.StartsWith("Dial", StringComparison.Ordinal));
        // Entry 169 section 2.2: the clicks with the click value stated, and what rounding leaves behind the line's "why".
        Assert.Equal("Dial 5 clicks left, at 0.25 MOA a click.", dial);
        Assert.Contains(window.ZeroText, t => t.StartsWith("Rounding left to whole clicks leaves", StringComparison.Ordinal));
        window.Close();
    }

    /// <summary>
    /// Entry 93 section 4: the rail and its destinations. Entry 112 built the library and the report; the Reports slot says a report is written
    /// from an analysis, where its button is.
    /// </summary>
    [AvaloniaFact]
    public void TheRailGoesToEachDestination()
    {
        var window = NewWindow();
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var rail = window.GetLogicalDescendants().OfType<Button>().Where(b => b.Classes.Contains(AppStyles.RailButton)).ToList();

        // Entry 109 section 2: the marking screen, the library, Print, records and reports, and the gear at the foot; entry 112 section 4 adds
        // Ballistics after the records, and entry 131 section 7 adds Equipment after Compare loads.
        Assert.Equal(8, rail.Count);
        rail[1].RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Assert.True(window.ShowingLibrary);
        rail[4].RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Assert.True(window.ShowingBallistics);
        rail[5].RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Assert.True(window.ShowingCompare);
        rail[6].RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Assert.True(window.ShowingEquipment);
        rail[^1].RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Assert.True(window.ShowingSettings);
        rail[0].RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Assert.False(window.ShowingSettings);
        window.Close();
    }

    /// <summary>Entry 95 section 2: the rounds fired are one field, and a disagreeing count heads the review queue.</summary>
    [AvaloniaFact]
    public void RoundsFiredThatDisagreeWithTheMarksHeadTheQueue()
    {
        var window = NewWindow();
        window.Show();
        Mark(window, offsetInches: 0.1, sigmaInches: 0.27);

        window.Session.SetExpectedShots(11);
        Dispatcher.UIThread.RunJobs();

        var first = window.ReviewItems.First();
        Assert.Equal(ReviewKind.Count, first.Kind);
        Assert.StartsWith("You fired 11 and 10 are marked.", first.Sentence, StringComparison.Ordinal);
        window.Close();
    }

    /// <summary>Entry 93 section 2: the breadcrumb says what is open and what is on it.</summary>
    [AvaloniaFact]
    public void TheBreadcrumbNamesTheDocumentAndItsCounts()
    {
        var window = NewWindow();
        window.Show();
        Dispatcher.UIThread.RunJobs();
        Assert.Contains("no image open", window.BreadcrumbText, StringComparison.Ordinal);

        Mark(window, offsetInches: 0.1, sigmaInches: 0.27);

        Assert.Contains("10 shots", window.BreadcrumbText, StringComparison.Ordinal);
        window.Close();
    }
}

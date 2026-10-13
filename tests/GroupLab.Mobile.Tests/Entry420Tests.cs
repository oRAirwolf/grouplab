using System.Collections.Immutable;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 420 section 1.3: the phone's set checklist (entry 259 screen 6) opens the pooled group on the result screen,
/// the same plot the desktop draws, every shot about its own bull and each sheet in its own shade, with the checklist as its way back.
/// </summary>
public class Entry420Tests
{
    [AvaloniaFact]
    public void ThePooledGroupOpensOnTheResultScreenWithEachSheetInItsShade()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        var definition = GroupLab.Cli.Library.TargetGenerator.Generate(new GroupLab.Cli.Library.GeneratorRequest(100, 4, null, 25, "letter")).Design!.Definition!;
        var identity = new HomographyMapping(new Homography([1, 0, 0, 0, 1, 0, 0, 0, 1]));
        var scoring = definition.Bulls.Where(b => b.Scoring).ToList();
        foreach (int sheet in new[] { 0, 2 })
        {
            var bulls = scoring.Select((b, i) => new BullAim(i, (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), new PointD(b.X, b.Y))).ToImmutableList();
            var shots = bulls.Select((b, i) => new MarkedShot(i + 1, new PointD(b.Image.X + 20 + sheet, b.Image.Y - 10), ShotProvenance.Automatic, Bull: b.Index)).ToImmutableList();
            var state = new MarkingState(null, new SheetReference(identity, "test"), null, bulls, shots, shots.Count + 1, Calibre: Calibre.Of(0.308), ShotDistanceInches: 3600, SetSheet: sheet);
            Assert.NotNull(PhoneAnalysis.Save(state, definition, UnitSettings.Imperial, null));
        }

        var page = new SetPage(definition, null, UnitSettings.Imperial, () => { }, () => { });
        var window = new Window { Width = 400, Height = 800, Content = page };
        window.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();
            var open = window.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetAutomationId(b) == "set-pooled-group");
            open.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            var result = Assert.IsType<ResultView>(page.Content);
            Assert.Equal(2 * scoring.Count, result.Plot.Shots.Count(s => !s.Excluded));
            Assert.Equal([0, 2], result.Plot.PooledSheets);
            Assert.Contains(result.Plot.Legend, l => l.StartsWith($"Sheet 3: {scoring.Count} shots", StringComparison.Ordinal));
        }
        finally
        {
            window.Close();
        }
    }
}

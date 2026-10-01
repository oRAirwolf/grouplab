using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.App.Theme;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 309 section 2 on the phone: Compare's "Each load's group" card, one plot a load at one scale in the load's
/// color, a tap stacking them on one center with a button a load to hide it, and the range chart naming each load in the same color.
/// </summary>
public class Entry309Tests
{
    private static readonly TestPhone ThePhone = new();

    private static ComparePage Compare(Window window)
    {
        if (Phone.Platform is null)
        {
            Phone.Start(ThePhone, Avalonia.Application.Current!, () => "US", null);
        }

        var platform = (TestPhone)Phone.Platform!;
        string copy = Path.Combine(platform.CacheFolder, "chosen-309.png");
        File.Copy(Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png"), copy, overwrite: true);
        var result = PhoneAnalysis.Run(copy, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, null, CancellationToken.None);
        Assert.Null(result.Failure);
        var store = PhoneAnalysis.Store();
        var saved = store.Get(result.SessionId!.Value)!;
        long first = store.Save(saved with { Id = 0, CreatedUtc = "2026-09-30T04:40:00Z", Load = "H4350 41.5" });
        long second = store.Save(saved with { Id = 0, CreatedUtc = "2026-09-30T05:01:00Z", Load = "H4350 42.0" });
        var page = new ComparePage([store.Get(first)!, store.Get(second)!], UnitSettings.Imperial, () => { });
        window.Content = page;
        Dispatcher.UIThread.RunJobs();
        return page;
    }

    [AvaloniaFact]
    public void EachLoadsGroupIsSideBySideAndATapStacksThem()
    {
        var window = new Window { Width = 412, Height = 915 };
        window.Show();
        var page = Compare(window);
        var card = page.Groups!;
        Assert.False(card.Stacked);
        Assert.Equal(2, card.Plots.Count);
        Assert.Equal(card.Plots[0].Extent, card.Plots[1].Extent);
        Assert.All(card.Plots, p => Assert.Single(p.Show));

        card.Toggle();
        Dispatcher.UIThread.RunJobs();
        Assert.True(card.Stacked);
        var stacked = Assert.Single(card.Plots);
        Assert.Equal(2, stacked.Show.Count);
        Assert.True(stacked.Width > 2 * 150 - 1);

        // A load hidden and shown again, and a tap puts them side by side.
        card.SetHidden(1, true);
        Assert.Single(card.Plots[0].Show);
        card.SetHidden(1, false);
        Assert.Equal(2, card.Plots[0].Show.Count);
        card.Toggle();
        Assert.False(card.Stacked);
        Assert.Equal(2, card.Plots.Count);
        window.Close();
    }

    [AvaloniaFact]
    public void TheRangeChartNamesEachLoadInItsColor()
    {
        var window = new Window { Width = 412, Height = 915 };
        window.Show();
        var page = Compare(window);
        var chart = page.GetVisualDescendants().OfType<IntervalChart>().Single();
        Assert.True(chart.RowsAreLoads);
        var colours = Tokens.Loads(page.ActualThemeVariant);
        Assert.Equal(colours[0], ((ISolidColorBrush)LoadGroups.Brush(0, page.ActualThemeVariant)).Color);
        Assert.NotEqual(colours[0], colours[1]);

        // The verdict names each load after its marker, in the same order.
        var swatches = page.GetVisualDescendants().OfType<LoadSwatch>().ToList();
        Assert.Contains(swatches, s => s.Index == 0);
        Assert.Contains(swatches, s => s.Index == 1);
        window.Close();
    }

    /// <summary>
    /// Entry 309 section 1: Capture is Home A, the first screen. The mark, the caliber and distance row, Take a picture, Choose a photo and
    /// Print a target, and the getting started card opening the phone's part of the guide; a first caliber is asked for in a sheet, and only
    /// then.
    /// </summary>
    [AvaloniaFact]
    public void CaptureIsHomeAAndAsksForACaliberOnlyTheFirstTime()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(ThePhone, Avalonia.Application.Current!, () => "US", null);
        }

        var (was, wasInches) = Phone.Settings.LoadShotSetup();
        Phone.Settings.SaveShotSetup("", null);
        var window = new Window { Width = 412, Height = 915 };
        try
        {
            var page = new CapturePage();
            window.Content = page;
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.Single(page.GetVisualDescendants().OfType<BrandMark>());
            var buttons = page.GetVisualDescendants().OfType<Button>().ToList();
            static string Words(Button b) => b.Content as string ?? (b.Content as TextBlock)?.Text ?? "";
            foreach (string label in new[] { "Take a picture", "Choose a photo", "Print a target", "Change" })
            {
                Assert.Contains(buttons, b => Words(b) == label);
            }

            // The getting started card opens the phone's part of the guide.
            var platform = (TestPhone)Phone.Platform!;
            var started = buttons.First(b => b.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Getting started on your phone"));
            started.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Assert.Contains(platform.Asked, a => a == ("open", CapturePage.GettingStartedAddress));

            // No caliber yet: Take a picture asks first, and nothing opens until it is answered.
            bool went = false;
            page.AskFirst(() => went = true);
            Assert.True(page.Asking);
            Assert.False(went);
            // Entry 328: the sheet scrolls, so its fields are in the visual tree once it has been laid out.
            page.UpdateLayout();
            var calibre = page.GetVisualDescendants().OfType<AutoCompleteBox>().Single();
            calibre.Text = ".308";
            Dispatcher.UIThread.RunJobs();
            buttons = page.GetVisualDescendants().OfType<Button>().ToList();
            buttons.First(b => Words(b) == "Continue").RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Assert.False(page.Asking);
            Assert.True(went);
            Assert.Equal(".308", Phone.Settings.LoadShotSetup().Calibre);

            // Remembered: the next time it goes straight on.
            went = false;
            page.AskFirst(() => went = true);
            Assert.False(page.Asking);
            Assert.True(went);
        }
        finally
        {
            window.Close();
            Phone.Settings.SaveShotSetup(was, wasInches);
        }
    }
}

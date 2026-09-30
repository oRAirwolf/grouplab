using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 312, Alan's iPad screenshots of build 134: Compare uses the whole width on a tablet and has a way back at the
/// top, the verdict reads in plain words first, the numeric fields ask for the number pad, and the caliber box can be changed without
/// deleting everything.
/// </summary>
public class Entry312Tests
{
    private static readonly TestPhone ThePhone = new();

    private static ComparePage Compare(Window window, Action? sessions = null)
    {
        if (Phone.Platform is null)
        {
            Phone.Start(ThePhone, Avalonia.Application.Current!, () => "US", null);
        }

        var platform = (TestPhone)Phone.Platform!;
        string copy = Path.Combine(platform.CacheFolder, "chosen-312.png");
        File.Copy(Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png"), copy, overwrite: true);
        var result = PhoneAnalysis.Run(copy, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, null, CancellationToken.None);
        Assert.Null(result.Failure);
        var store = PhoneAnalysis.Store();
        var saved = store.Get(result.SessionId!.Value)!;
        long first = store.Save(saved with { Id = 0, CreatedUtc = "2026-09-30T02:35:00Z", Load = "H4350 41.5" });
        long second = store.Save(saved with { Id = 0, CreatedUtc = "2026-09-30T03:55:00Z", Load = "H4350 42.0" });
        var page = new ComparePage([store.Get(first)!, store.Get(second)!], UnitSettings.Imperial, () => { }, sessions);
        window.Content = page;
        Dispatcher.UIThread.RunJobs();
        return page;
    }

    [AvaloniaFact]
    public void OnATabletThePlotsAndTheChartUseTheWholeWidth()
    {
        // An iPad mini on its side, then a phone held upright, which stays as it was.
        var window = new Window { Width = 1133, Height = 744 };
        window.Show();
        var page = Compare(window);
        Dispatcher.UIThread.RunJobs();
        var plots = page.Groups!.Plots;
        Assert.Equal(2, plots.Count);
        Assert.True(plots[0].Width > 400, $"the plots are {plots[0].Width} across on a card {page.Groups.Bounds.Width} wide");
        Assert.Equal(plots[0].Width, plots[0].Height);
        Assert.Equal(plots[0].Width, plots[1].Width);
        Assert.Equal(plots[0].TranslatePoint(default, page)!.Value.Y, plots[1].TranslatePoint(default, page)!.Value.Y);
        var chart = page.GetVisualDescendants().OfType<IntervalChart>().Single();
        Assert.True(chart.Bounds.Width > ComparePage.Reading, $"the chart is {chart.Bounds.Width} across");
        window.Close();

        var phone = new Window { Width = 412, Height = 915 };
        phone.Show();
        page = Compare(phone);
        Dispatcher.UIThread.RunJobs();
        Assert.All(page.Groups!.Plots, p => Assert.Equal(150, p.Width));
        phone.Close();
    }

    [AvaloniaFact]
    public void TheVerdictReadsInPlainWordsWithTheFiguresBehindDetails()
    {
        var window = new Window { Width = 412, Height = 915 };
        window.Show();
        var page = Compare(window);
        var details = page.GetLogicalDescendants().OfType<Expander>().Single(e => e.Header is TextBlock { Text: "Details" });
        Assert.False(details.IsExpanded);
        var inside = details.GetLogicalDescendants().OfType<TextBlock>().ToHashSet();
        var shown = page.GetLogicalDescendants().OfType<TextBlock>().Where(t => !inside.Contains(t)).Select(t => t.Text ?? "").ToList();
        Assert.Contains(shown, t => t.StartsWith("Their spreads ", StringComparison.Ordinal) || t.Contains(" spreads ", StringComparison.Ordinal));
        Assert.Contains(shown, t => t.StartsWith("To tell a 10 percent difference", StringComparison.Ordinal));
        Assert.DoesNotContain(shown, t => t.Contains("95 percent interval", StringComparison.Ordinal) || t.Contains("80 percent power", StringComparison.Ordinal));
        Assert.Contains(inside, t => t.Text?.Contains("95 percent interval", StringComparison.Ordinal) == true);
        window.Close();
    }

    [AvaloniaFact]
    public void CompareHasAWayBackToSessionsAtTheTop()
    {
        var window = new Window { Width = 744, Height = 1133 };
        window.Show();
        bool back = false;
        var page = Compare(window, () => back = true);
        var buttons = page.GetLogicalDescendants().OfType<Button>().ToList();
        var title = page.GetLogicalDescendants().OfType<TextBlock>().First(t => t.Text == "Compare loads");
        var toSessions = buttons.First(b => b.Content is TextBlock { Text: "Back to Sessions" } || b.Content as string == "Back to Sessions");
        Assert.True(toSessions.TranslatePoint(default, page)!.Value.Y < title.TranslatePoint(default, page)!.Value.Y);
        toSessions.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Assert.True(back);
        window.Close();
    }
}

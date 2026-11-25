using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Tests.Support;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// Report 57's suggestion, Alan's yes to the general case (his queue of 2026-11-24, part 2), on the phone: Sessions, Compare loads has Pool the
/// chosen beside Compare, read by the computer's own planner. Two loose sheets pool as separate sheets, saved as a pool of pictures and opened
/// as one pooled group; what cannot pool (one sheet, different distances) is said in amber under the button and opens nothing.
/// </summary>
public class Report57PhonePoolTests
{
    private static Window Started()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        var window = new Window { Width = 412, Height = 915 };
        window.Show();
        return window;
    }

    private static long Analyzed(string name)
    {
        string copy = Path.Combine(((TestPhone)Phone.Platform!).CacheFolder, name);
        File.Copy(Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png"), copy, overwrite: true);
        var result = PhoneAnalysis.Run(copy, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, null, CancellationToken.None);
        Assert.Null(result.Failure);
        return result.SessionId!.Value;
    }

    private static T? ById<T>(Control root, string id) where T : Control =>
        root.GetLogicalDescendants().OfType<T>().FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == id);

    private static void Press(Control root, string id)
    {
        ById<Button>(root, id)!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }

    private static void Tick(Control root, long id, bool on)
    {
        ById<CheckBox>(root, $"sessions-choose-{id}")!.IsChecked = on;
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void TwoLooseSheetsPoolAsOneGroupAndWhatCannotPoolIsSaidInAmber()
    {
        var window = Started();
        var store = PhoneAnalysis.Store();
        long first = Analyzed("pool-57-a.png");
        long second = Analyzed("pool-57-b.png");
        var record = store.Get(second)!;
        var state = MarkingFile.Read(record.MarkingJson).State;
        long far = store.Save(record with { Id = 0, CreatedUtc = "2099-02-01T00:00:00Z", MarkingJson = MarkingFile.Write(state with { ShotDistanceInches = 7200 }) });
        int pools = store.Pools().Count;
        try
        {
            var page = new SessionsPage();
            window.Content = page;
            Dispatcher.UIThread.RunJobs();
            Press(page, "sessions-compare-loads");

            // One loose sheet: refused in amber, and the page stays.
            Tick(page, first, true);
            Press(page, "sessions-pool-chosen");
            Assert.Equal(PoolRefusal.OneSheet, ById<TextBlock>(page, "sessions-pool-refused")!.Text);
            Assert.Equal(pools, store.Pools().Count);

            // A sheet shot at another distance: refused the same way.
            Tick(page, far, true);
            Press(page, "sessions-pool-chosen");
            Assert.Equal(PoolRefusal.DifferentDistances, ById<TextBlock>(page, "sessions-pool-refused")!.Text);

            // Two loose sheets of one design at one distance: saved as a pool of pictures and opened as one pooled group.
            Tick(page, far, false);
            Tick(page, second, true);
            Press(page, "sessions-pool-chosen");
            Assert.Equal(pools + 1, store.Pools().Count);
            var pool = store.Pools().First(p => p.Sheets.Contains(DataExport.Identity(store.Get(first)!)));
            Assert.True(pool.Pictures);
            Assert.Equal(2, pool.Sheets.Count);
            var view = page.GetLogicalDescendants().OfType<ResultView>().Single();
            Assert.Contains(view.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text is { } x && x.Contains("pooled", StringComparison.Ordinal));
            store.DeletePool(pool.Id);
        }
        finally
        {
            foreach (var p in store.Pools().Where(p => p.Sheets.Contains(DataExport.Identity(store.Get(first)!))))
            {
                store.DeletePool(p.Id);
            }

            store.Delete(first);
            store.Delete(second);
            store.Delete(far);
            window.Close();
        }
    }
}

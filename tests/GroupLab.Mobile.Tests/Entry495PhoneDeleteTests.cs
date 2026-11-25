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
/// Entry 495's sheet question on the phone (Alan's queue of 2026-11-24, part 1): a saved session's result offers Delete this session, which
/// asks first, naming each pooled set that uses it as Session records does on the computer; Delete takes it out of the store and out of the
/// pool, which keeps its other sheets, and Keep it leaves everything as it was. A pooled result offers only Delete this pooled set.
/// </summary>
public class Entry495PhoneDeleteTests
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

    private static PhoneResult Analyzed(string name)
    {
        string copy = Path.Combine(((TestPhone)Phone.Platform!).CacheFolder, name);
        File.Copy(Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png"), copy, overwrite: true);
        var result = PhoneAnalysis.Run(copy, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, null, CancellationToken.None);
        Assert.Null(result.Failure);
        return result;
    }

    private static T? ById<T>(Control root, string id) where T : Control =>
        root.GetLogicalDescendants().OfType<T>().FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == id);

    private static void Press(Control root, string id)
    {
        ById<Button>(root, id)!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }

    private static string Question(Control root) => ById<TextBlock>(root, "result-session-delete-question")!.Text!;

    [AvaloniaFact]
    public void ASessionNoPoolUsesIsAskedAboutPlainlyAndKeepItKeepsIt()
    {
        var window = Started();
        var result = Analyzed("delete-495-a.png");
        long id = result.SessionId!.Value;
        var view = new ResultView(result, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, () => { });
        window.Content = view;
        Dispatcher.UIThread.RunJobs();

        Press(view, "result-session-delete");
        Assert.Equal("Delete this session?", Question(view));
        Press(view, "result-session-delete-no");
        Assert.NotNull(PhoneAnalysis.Store().Get(id));

        Press(view, "result-session-delete");
        Press(view, "result-session-delete-yes");
        Assert.Null(PhoneAnalysis.Store().Get(id));
        Assert.Contains(view.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text == ResultView.SessionDeletedWords);
        Assert.Null(ById<Button>(view, "result-session-delete"));
        window.Close();
    }

    [AvaloniaFact]
    public void ASessionAPoolUsesNamesThePoolAndLeavesItsOtherSheets()
    {
        var window = Started();
        var store = PhoneAnalysis.Store();
        var first = Analyzed("delete-495-b.png");
        var second = Analyzed("delete-495-c.png");
        var third = Analyzed("delete-495-d.png");
        string[] sheets = [.. new[] { first, second, third }.Select(r => DataExport.Identity(store.Get(r.SessionId!.Value)!))];
        long pool = store.SavePool(new PoolRecord(0, Guid.NewGuid().ToString("N"), "2026-11-24T12:00:00Z", sheets, PoolRecord.ChoicesFor(pictures: true)));

        var view = new ResultView(first, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, () => { });
        window.Content = view;
        Dispatcher.UIThread.RunJobs();
        Press(view, "result-session-delete");
        string question = Question(view);
        Assert.StartsWith("Delete this session? It is in a pooled set: Pooled set of 3 sheets", question, StringComparison.Ordinal);
        Assert.EndsWith("That pool keeps its other sheets.", question, StringComparison.Ordinal);
        Press(view, "result-session-delete-yes");

        try
        {
            Assert.Null(store.Get(first.SessionId!.Value));
            Assert.Equal(sheets[1..], store.GetPool(pool)!.Sheets);
            Assert.NotNull(store.Get(second.SessionId!.Value));
        }
        finally
        {
            // The phone's store is shared by every test in the run, and other tests count the pools in it.
            store.DeletePool(pool);
            store.Delete(second.SessionId!.Value);
            store.Delete(third.SessionId!.Value);
            window.Close();
        }
    }
}

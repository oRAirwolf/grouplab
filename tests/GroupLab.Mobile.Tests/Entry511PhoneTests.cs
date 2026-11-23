using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 511 section 1 (suggestion 56), on the phone: the session's own name on the result page, the same label the
/// computer's "Name this session" keeps, and Rename a session under Sessions; the name comes first, with the target after it.
/// </summary>
public class Entry511PhoneTests
{
    private static readonly TestPhone ThePhone = new();

    private static Window Started()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(ThePhone, Avalonia.Application.Current!, () => "US", null);
        }

        var window = new Window { Width = 412, Height = 915 };
        window.Show();
        return window;
    }

    private static PhoneResult Analyzed()
    {
        string copy = Path.Combine(((TestPhone)Phone.Platform!).CacheFolder, "chosen-511.png");
        File.Copy(Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png"), copy, overwrite: true);
        var result = PhoneAnalysis.Run(copy, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, null, CancellationToken.None);
        Assert.Null(result.Failure);
        return result;
    }

    private static T ById<T>(Control root, string id) where T : Control =>
        root.GetLogicalDescendants().OfType<T>().First(c => AutomationProperties.GetAutomationId(c) == id);

    private static void Press(Control root, string id)
    {
        ById<Button>(root, id).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void TheResultPageNamesTheSessionAndSavesTheName()
    {
        var window = Started();
        var result = Analyzed();
        long id = result.SessionId!.Value;
        var view = new ResultView(result, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, () => { });
        window.Content = view;
        Dispatcher.UIThread.RunJobs();

        var card = ById<Border>(view, "result-session-name-card");
        Assert.Contains(card.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text == "Name this session");
        var box = ById<TextBox>(view, "result-session-name");
        Assert.Equal("", box.Text);
        box.Text = "Rifle A";
        box.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter, Source = box });
        Dispatcher.UIThread.RunJobs();

        var saved = PhoneAnalysis.Store().Get(id)!;
        Assert.Equal("Rifle A", SessionNames.LabelOf(saved.MarkingJson));
        Assert.EndsWith(", Rifle A", saved.SheetName, StringComparison.Ordinal);
        Assert.StartsWith("Rifle A, ", view.TargetName, StringComparison.Ordinal);

        // A name given from Sessions while the result is open is taken here too.
        view.TakeName("Rifle D");
        Assert.Equal("Rifle D", view.NameShown);
        Assert.StartsWith("Rifle D, ", view.TargetName, StringComparison.Ordinal);
        window.Close();
    }

    [AvaloniaFact]
    public void SessionsRenamesASessionAndListsItByItsNameFirst()
    {
        var window = Started();
        long id = Analyzed().SessionId!.Value;
        var page = new SessionsPage();
        window.Content = page;
        Dispatcher.UIThread.RunJobs();

        Press(page, "sessions-rename");
        Press(page, $"sessions-rename-{id}");
        var box = ById<TextBox>(page, "sessions-rename-box");
        box.Text = "Rifle C";
        Press(page, "sessions-rename-save");

        var saved = PhoneAnalysis.Store().Get(id)!;
        Assert.Equal("Rifle C", SessionNames.LabelOf(saved.MarkingJson));
        Assert.Equal("Rifle C", MarkingFile.Read(saved.MarkingJson).State.SheetLabel);
        // Back on Rename a session, and then on Sessions, the session is called by its name.
        Assert.Contains(page.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text == "Rifle C");
        Press(page, "sessions-rename-back");
        var list = ById<Border>(page, "sessions-list");
        Assert.Contains(list.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text == "Rifle C");

        // An empty name takes it away.
        Press(page, "sessions-rename");
        Press(page, $"sessions-rename-{id}");
        ById<TextBox>(page, "sessions-rename-box").Text = "";
        Press(page, "sessions-rename-save");
        Assert.Null(SessionNames.LabelOf(PhoneAnalysis.Store().Get(id)!.MarkingJson));
        window.Close();
    }
}

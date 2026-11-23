using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.App.Theme;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 511 section 1 (suggestion 56): Session records filled with rows all named after the same target, and the box
/// that names a session sat under the load as "This sheet's own label", where the person asking did not find it. It leads the analysis
/// column as "Name this session", beside the target's name; each Session records row has Rename in its menu and on F2; and the name comes
/// first in the row, the tab and Compare, with the target's name after it.
/// </summary>
public class Entry511Tests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    [AvaloniaFact]
    public void TheNameBoxLeadsTheAnalysisColumnAndTheNameComesFirstInTheTabAndTheRecords()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.CalibreAnswered();
            window.Analyse();
            Settle();
            var headings = window.GetLogicalDescendants().OfType<TextBlock>().Where(t => Entry109Tests.Shown(t)).Select(t => t.Text ?? "").ToList();
            int name = headings.IndexOf("Name this session");
            Assert.True(name >= 0, "the heading Name this session is not shown");
            Assert.True(name < headings.IndexOf("Load"), "Name this session is not above the load");
            Assert.DoesNotContain("This sheet's own label", headings);
            Assert.StartsWith("on GroupLab 5x5", window.SessionNameShown.Beside, StringComparison.Ordinal);

            window.TypeSessionName("Rifle A");
            Settle();
            Assert.Equal("Rifle A", window.Session.State.SheetLabel);
            Assert.StartsWith("Rifle A, GroupLab 5x5", window.TabsShown.Single(t => t.Chosen).Name, StringComparison.Ordinal);
            window.SaveWaitingNow();

            window.ShowSessions();
            Settle();
            Assert.Contains(" | Rifle A, GroupLab 5x5", Assert.Single(window.SessionRowTexts), StringComparison.Ordinal);
            var compared = SessionNames.For(window.Sessions!.List());
            Assert.Equal("Rifle A", Assert.Single(compared).Name);
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    [AvaloniaFact]
    public void ARowIsRenamedFromItsMenuOrF2AndTheOpenTabTakesTheName()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.CalibreAnswered();
            window.Analyse();
            long id = window.CurrentSession!.Value;
            window.ShowSessions();
            Settle();
            Assert.Equal(["Open", "Rename"], window.SessionRowMenu(0));

            // F2 on the row: a box with the name, and Enter saves it.
            window.RenameSessionRow(0);
            Settle();
            var box = Assert.IsType<TextBox>(window.RenamingBox);
            box.Text = "Rifle B";
            box.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter, Source = box });
            Settle();
            Assert.Null(window.RenamingBox);
            Assert.Contains(" | Rifle B, GroupLab 5x5", Assert.Single(window.SessionRowTexts), StringComparison.Ordinal);
            Assert.Equal("Rifle B", MarkingFile.Read(window.Sessions!.Get(id)!.MarkingJson).State.SheetLabel);
            // The same session open in its tab took the name, so its next save keeps it.
            Assert.Equal("Rifle B", window.Session.State.SheetLabel);
            Assert.Equal("Rifle B", window.SessionNameShown.Name);
            Assert.False(window.HasUnsavedWork);

            // The menu's Rename, and Escape leaves the name as it was.
            window.ClickSessionRowMenu(0, "Rename");
            Settle();
            box = Assert.IsType<TextBox>(window.RenamingBox);
            Assert.Equal("Rifle B", box.Text);
            box.Text = "Not this";
            box.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape, Source = box });
            Settle();
            Assert.Contains(" | Rifle B, GroupLab 5x5", Assert.Single(window.SessionRowTexts), StringComparison.Ordinal);

            // An empty name takes it away, and the row is called by its target again.
            window.RenameSessionRow(0);
            Settle();
            window.FinishRenaming(id, "");
            Settle();
            Assert.Contains(" | GroupLab 5x5", Assert.Single(window.SessionRowTexts), StringComparison.Ordinal);
            Assert.DoesNotContain("Rifle B", Assert.Single(window.SessionRowTexts), StringComparison.Ordinal);
            Assert.Null(window.Session.State.SheetLabel);
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}

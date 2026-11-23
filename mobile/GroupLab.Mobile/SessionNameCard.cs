using Avalonia.Controls;
using Avalonia.Input;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Records;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 511 section 1 (suggestion 56): a session's own name on the phone, the same label the computer's analysis
/// screen keeps under "Name this session". It is on the result page, and Sessions has Rename for a session not open; either writes the
/// name into the session's record, so it synchronizes as any change to the session does and reads the same on the computer.
/// </summary>
internal static class SessionNameCard
{
    /// <summary>The words under the box.</summary>
    internal const string Hint = "such as Rifle A, or the letter in its serial box";

    /// <summary>
    /// The card: a heading, the box holding the name now, and the target's name beside it. <paramref name="named"/> is told the new name,
    /// null for none, when the box is left or Enter is pressed with a name that differs from <paramref name="current"/>.
    /// </summary>
    internal static Border Card(string? current, string targetName, Action<string?> named, string id, out TextBox nameBox)
    {
        var box = new TextBox { Text = current ?? "", PlaceholderText = Hint, MinHeight = Screens.Touch }.Id(id);
        Avalonia.Automation.AutomationProperties.SetName(box, "Name this session");
        string? last = current;
        void Take()
        {
            string? now = string.IsNullOrWhiteSpace(box.Text) ? null : box.Text.Trim();
            if (now != last)
            {
                last = now;
                named(now);
            }
        }

        box.LostFocus += (_, _) => Take();
        box.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                Take();
            }
        };
        nameBox = box;
        return Screens.Card(Screens.Heading("Name this session"), box, Screens.Dim("on " + targetName));
    }

    /// <summary>
    /// Sessions' Rename: every session saved on this phone, newest first; a tap gives its name box, Save name writes it and comes back to
    /// the list. A pooled set has no name of its own, so it is not listed.
    /// </summary>
    internal static Control Page(Action back)
    {
        var column = new StackPanel { Spacing = 8 };
        column.Children.Add(Screens.BackButton("Back to Sessions", back).Id("sessions-rename-back"));
        column.Children.Add(Screens.Title("Rename a session"));
        column.Children.Add(Screens.Line("Tap a session to give it a name of its own. Sessions and Compare show the name first, with the target after it."));
        IReadOnlyList<SessionSummary> saved;
        try
        {
            saved = PhoneAnalysis.Store().List();
        }
        catch (Microsoft.Data.Sqlite.SqliteException e)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "session.list", e);
            saved = [];
        }

        var rows = new StackPanel();
        var page = new ContentControl();
        foreach (var (s, name) in saved.Zip(SessionNames.For(saved)))
        {
            long id = s.Id;
            rows.Children.Add(Screens.Row(name.Name, name.Sheet, () => page.Content = One(id, () => page.Content = Page(back)), explain: false).Id($"sessions-rename-{id}"));
        }

        column.Children.Add(rows.Children.Count > 0 ? new Border { Child = rows, Classes = { PhoneStyles.Card } }.Id("sessions-rename-list")
            : Screens.Line("There are no sessions on this phone yet."));
        page.Content = Screens.Page(column);
        return page;
    }

    /// <summary>One session's name box, Save name and Back.</summary>
    private static Control One(long id, Action back)
    {
        var record = PhoneAnalysis.Store().Get(id);
        string? label = record is null ? null : SessionNames.LabelOf(record.MarkingJson);
        string target = record is null ? "" : SessionNames.TargetOf(record.SheetName, label);
        var said = Screens.Line("");
        var box = new TextBox { Text = label ?? "", PlaceholderText = Hint, MinHeight = Screens.Touch }.Id("sessions-rename-box");
        Avalonia.Automation.AutomationProperties.SetName(box, "Name this session");
        var column = new StackPanel { Spacing = 12 };
        column.Children.Add(Screens.BackButton("Back to Rename a session", back).Id("sessions-rename-one-back"));
        column.Children.Add(Screens.Title("Name this session"));
        column.Children.Add(Screens.Dim("on " + target));
        column.Children.Add(box);
        column.Children.Add(Screens.Primary("Save name", () =>
        {
            if (Rename(id, box.Text) is null)
            {
                said.Text = "This session could not be renamed on the phone.";
                return;
            }

            back();
        }).Id("sessions-rename-save"));
        column.Children.Add(said);
        return Screens.Page(column);
    }

    /// <summary>The session renamed in the phone's records; null where the database would not take it.</summary>
    internal static SessionRecord? Rename(long id, string? name)
    {
        try
        {
            var renamed = PhoneAnalysis.Store().Rename(id, name);
            // A result of the same session open as a target takes the name too, so its next save keeps it.
            string? label = renamed is null ? null : SessionNames.LabelOf(renamed.MarkingJson);
            foreach (var open in Shell.Current?.Targets.All ?? [])
            {
                if (open.SessionId == id)
                {
                    open.TakeName(label);
                }
            }

            DiagnosticLog.Info("session.rename", ("session", id), ("named", !string.IsNullOrWhiteSpace(name)));
            return renamed;
        }
        catch (Microsoft.Data.Sqlite.SqliteException e)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "session.rename", e);
            return null;
        }
    }
}

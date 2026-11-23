using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 511 section 1 (suggestion 56): a session's own name, given from Session records as well as from the analysis
/// screen. Session records filled with rows all named after the same target, and the way to name one was a box the person asking did not
/// find. Each session row's menu has Rename, and F2 on a row does the same; the name is the label the analysis screen keeps, so a session
/// named in either place reads the same in the other, in its tab, in Compare and on the phone once it has synchronized.
/// </summary>
public sealed partial class MainWindow
{
    /// <summary>The words under a session being named.</summary>
    internal const string NameSessionHint = "such as Rifle A, or the letter in its serial box";

    private TextBox? renamingBox;

    /// <summary>The box a session row is being named in, for the headless tests.</summary>
    internal TextBox? RenamingBox => renamingBox;

    /// <summary>A session row's menu, Open and Rename, and F2 on the row to rename it.</summary>
    private void OfferRename(DockPanel row, Button open, long id, string? label)
    {
        var openItem = new MenuItem { Header = "Open" };
        openItem.Click += (_, _) => Leaving(() => OpenSession(id));
        var renameItem = new MenuItem { Header = "Rename", InputGesture = new KeyGesture(Key.F2) };
        renameItem.Click += (_, _) => StartRenaming(row, open, id, label);
        open.ContextMenu = new ContextMenu { ItemsSource = new[] { openItem, renameItem } };
        open.AddHandler(KeyDownEvent, (_, e) =>
        {
            if (e.Key == Key.F2)
            {
                e.Handled = true;
                StartRenaming(row, open, id, label);
            }
        }, RoutingStrategies.Tunnel);
    }

    /// <summary>The row gives way to a box holding its name, with Save name and Cancel; Enter saves and Escape leaves it as it was.</summary>
    private void StartRenaming(DockPanel row, Button open, long id, string? label)
    {
        int at = row.Children.IndexOf(open);
        if (sessions is null || at < 0)
        {
            return;
        }

        var box = new TextBox { Text = label ?? "", PlaceholderText = NameSessionHint, MinWidth = 240, VerticalAlignment = VerticalAlignment.Center };
        Avalonia.Automation.AutomationProperties.SetName(box, "Name this session");
        box.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                FinishRenaming(id, box.Text);
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                renamingBox = null;
                FillSessions();
            }
        };
        var buttons = Row(Button("Save name", () => FinishRenaming(id, box.Text)), Button("Cancel", () =>
        {
            renamingBox = null;
            FillSessions();
        }));
        var editor = new DockPanel { Margin = new Avalonia.Thickness(Tokens.Space4, 0) };
        DockPanel.SetDock(buttons, Dock.Right);
        editor.Children.Add(buttons);
        editor.Children.Add(box);
        row.Children[at] = editor;
        renamingBox = box;
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            box.Focus();
            box.SelectAll();
        });
    }

    /// <summary>
    /// The name saved, or taken away when the box is empty. A copy of the session open in a tab takes the same name, so a later save of the
    /// tab never brings the old one back.
    /// </summary>
    internal void FinishRenaming(long id, string? name)
    {
        renamingBox = null;
        if (sessions?.Rename(id, name) is { } renamed)
        {
            string? label = SessionNames.LabelOf(renamed.MarkingJson);
            RenameOpenCopies(id, label);
            DiagnosticLog.Info("session.rename", ("session", id), ("named", label is not null));
            status.Text = label is null
                ? "The session has no name of its own now, so it is called by its target again."
                : $"The session is now called {label}.";
        }

        FillSessions();
    }

    /// <summary>Every open copy of session <paramref name="id"/>, the target showing and the tabs parked, given the same name.</summary>
    private void RenameOpenCopies(long id, string? label)
    {
        if (currentSession == id && !showingPool && session.State.SheetLabel != label)
        {
            bool wasSaved = !HasUnsavedWork;
            session.SetSheetLabel(label);
            // Already written to the record: saved again at once, so the tab does not say it has unsaved work it does not have.
            if (wasSaved)
            {
                SaveSession();
                ShowSaved();
            }
        }

        foreach (var tab in targets.All)
        {
            var parked = new List<object?> { tab.Parked };
            if (tab.Set is { } set)
            {
                parked.AddRange(set.Members.Select(m => m.Parked));
            }

            foreach (var copy in parked.OfType<ParkedTarget>())
            {
                if (copy.CurrentSession == id && !ReferenceEquals(copy.Session, session) && copy.Session.State.SheetLabel != label)
                {
                    copy.Session.SetSheetLabel(label);
                    tab.Name = label is { Length: > 0 } ? label + ", " + TargetName(copy.Session.State, copy.PlotDefinition) : tab.Name;
                }
            }
        }
    }

    /// <summary>Starts naming the session at <paramref name="row"/> in Session records, as F2 there does, for the headless tests.</summary>
    internal void RenameSessionRow(int row)
    {
        var rows = sessionRows.Children.OfType<DockPanel>().Where(r => r.Children.OfType<Button>().Any(b => b.Classes.Contains(AppStyles.TableRow))).ToList();
        var open = rows[row].Children.OfType<Button>().First(b => b.Classes.Contains(AppStyles.TableRow));
        open.Focus();
        open.RaiseEvent(new KeyEventArgs { RoutedEvent = KeyDownEvent, Key = Key.F2, Source = open });
    }

    /// <summary>The menu items of the session row at <paramref name="row"/>, for the headless tests.</summary>
    internal IReadOnlyList<string> SessionRowMenu(int row)
    {
        var rows = sessionRows.Children.OfType<DockPanel>().Where(r => r.Children.OfType<Button>().Any(b => b.Classes.Contains(AppStyles.TableRow))).ToList();
        var open = rows[row].Children.OfType<Button>().First(b => b.Classes.Contains(AppStyles.TableRow));
        return [.. (open.ContextMenu?.ItemsSource ?? Array.Empty<object>()).OfType<MenuItem>().Select(m => m.Header as string ?? "")];
    }

    /// <summary>Clicks a session row's menu item by its words, for the headless tests.</summary>
    internal void ClickSessionRowMenu(int row, string header)
    {
        var rows = sessionRows.Children.OfType<DockPanel>().Where(r => r.Children.OfType<Button>().Any(b => b.Classes.Contains(AppStyles.TableRow))).ToList();
        var open = rows[row].Children.OfType<Button>().First(b => b.Classes.Contains(AppStyles.TableRow));
        var item = open.ContextMenu!.ItemsSource!.OfType<MenuItem>().First(m => (m.Header as string) == header);
        item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
    }

    /// <summary>The target's name alone: its design's, its picture's file, or "Marked by hand".</summary>
    private string TargetName(MarkingState state, GroupLab.Core.Gltd.Model.TargetDefinition? definition = null) =>
        (definition ?? plotDefinition)?.Name ?? (state.ImagePath is { } path ? Path.GetFileName(path) : "Marked by hand");

    /// <summary>The analysis screen's name box and the target's name beside it, for the headless tests.</summary>
    internal (string Name, string Beside) SessionNameShown => (sheetLabelBox.Text ?? "", sheetTargetName.Text ?? "");

    /// <summary>Types a name in the analysis screen's box and presses Enter, for the headless tests.</summary>
    internal void TypeSessionName(string name)
    {
        sheetLabelBox.Text = name;
        sheetLabelBox.RaiseEvent(new KeyEventArgs { RoutedEvent = KeyDownEvent, Key = Key.Enter, Source = sheetLabelBox });
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using GroupLab.App.Diagnostics;

namespace GroupLab.Mobile;

/// <summary>
/// Concept A, several targets open at once (Alan, 2026-10-07), on the phone: the target's name at the top of its result or marking screen
/// is a control. It opens a sheet of the open targets, each with a small picture, its state and a close button; a tap on a row switches to
/// it, and closing one with marks not saved asks first. The list itself is <see cref="OpenTargets{T}"/>, held by <see cref="Shell"/>.
/// </summary>
internal static class OpenTargetsSheet
{
    /// <summary>The thumbnail's side, a thumb's width.</summary>
    private const double Thumb = 56;

    /// <summary>
    /// The name at the top of a target's screen, which opens the sheet: the name, how many are open where more than one is, and a chevron
    /// that says it opens. <paramref name="big"/> is the result's page title; the marking screen's is a heading over its step.
    /// </summary>
    public static Button Switcher(bool big)
    {
        var name = new TextBlock { TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, Classes = { big ? PhoneStyles.Title : PhoneStyles.Heading } };
        var count = new TextBlock { TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, Classes = { PhoneStyles.Dim } };
        var chevron = GroupLab.App.Theme.Icons.Draw(ChevronDown, 14);
        chevron.VerticalAlignment = VerticalAlignment.Center;
        var after = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center, Children = { count, chevron } };
        Grid.SetColumn(after, 1);
        var button = new Button
        {
            Content = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 8, Children = { name, after } },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Classes = { PhoneStyles.Row },
        };

        void Refresh()
        {
            var owner = Owner(button);
            button.IsVisible = owner is not null;
            if (owner is null)
            {
                return;
            }

            int open = Shell.Current?.Targets is { } targets ? Math.Max(targets.Count, targets.Contains(owner) ? 0 : 1) : 1;
            name.Text = owner.TargetName;
            count.Text = OpenTargetWords.Count(open);
            count.IsVisible = count.Text.Length > 0;
            Avalonia.Automation.AutomationProperties.SetName(button, OpenTargetWords.SwitcherName(owner.TargetName, open));
        }

        button.AttachedToVisualTree += (_, _) => Refresh();
        button.Click += (_, _) => Open(button);
        return button;
    }

    /// <summary>The open target a control sits in: the outermost result round it, since a result made inside another is still that target.</summary>
    internal static ResultView? Owner(Control control) => control.GetLogicalAncestors().OfType<ResultView>().LastOrDefault();

    /// <summary>The sheet, over the screen the control <paramref name="from"/> sits on.</summary>
    internal static void Open(Control from)
    {
        if (Owner(from) is not { } owner || Shell.Current is not { } shell
            || from.FindLogicalAncestorOfType<UserControl>() is not { Content: Control behind } host)
        {
            return;
        }

        // A result put on screen some other way is open too, from where it is.
        if (!shell.Targets.Contains(owner))
        {
            shell.OpenTarget(owner, shell.Showing);
        }

        shell.Targets.SwitchTo(owner);
        DiagnosticLog.Info("targets.sheet", ("open", shell.Targets.Count));
        Show(shell, host, behind, owner);
    }

    /// <summary>The screen behind the sheet put back as it was, out of the layer it sat in.</summary>
    private static void Restore(UserControl host, Control behind)
    {
        host.Content = null;
        (behind.Parent as Panel)?.Children.Remove(behind);
        host.Content = behind;
    }

    private static void Show(Shell shell, UserControl host, Control behind, ResultView owner)
    {
        host.Content = null;
        (behind.Parent as Panel)?.Children.Remove(behind);
        var rows = new StackPanel { Spacing = 4 };
        var targets = shell.Targets.All.ToList();
        for (int i = 0; i < targets.Count; i++)
        {
            rows.Children.Add(Row(shell, host, behind, owner, targets[i], i));
        }

        var another = Screens.Choice(OpenTargetWords.Another, () =>
        {
            Restore(host, behind);
            shell.AnotherTarget();
        }).Id("open-targets-another");
        var body = new StackPanel { Spacing = 8, Children = { new Border { Child = rows, Classes = { PhoneStyles.Card } }, another } };
        host.Content = ProblemSheet.Over(behind, OpenTargetWords.SheetTitle, body, [another], () => Restore(host, behind));
    }

    /// <summary>One open target: its picture, its name and state, a tap to switch to it, and its close button.</summary>
    private static Control Row(Shell shell, UserControl host, Control behind, ResultView owner, ResultView target, int index)
    {
        bool showing = ReferenceEquals(target, owner);
        string detail = OpenTargetWords.Detail(target, showing);
        var words = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center, Children = { Screens.Line(target.TargetName), Screens.Quiet(detail) } };
        Grid.SetColumn(words, 1);
        var pick = new Button
        {
            Content = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 12, Children = { Picture(target), words } },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Classes = { PhoneStyles.Row },
        }.Id("open-targets-row-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Avalonia.Automation.AutomationProperties.SetName(pick, target.TargetName + ", " + detail);
        pick.Click += (_, _) =>
        {
            Restore(host, behind);
            if (!showing)
            {
                shell.SwitchTo(target);
            }
        };

        var close = new Button
        {
            Content = new TextBlock { Text = "×", FontSize = 22, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
            MinWidth = Screens.Touch,
            MinHeight = Screens.Touch,
            Padding = new Thickness(0),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Center,
        }.Id("open-targets-close-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Avalonia.Automation.AutomationProperties.SetName(close, OpenTargetWords.CloseName(target));
        close.Click += (_, _) =>
        {
            if (OpenTargetWords.AsksBeforeClosing(target))
            {
                Ask(shell, host, behind, owner, target);
            }
            else
            {
                Close(shell, host, behind, owner, target);
            }
        };
        Grid.SetColumn(close, 1);
        return new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 4, Children = { pick, close } };
    }

    /// <summary>A target with marks not saved asks before it is closed; keeping it open goes back to the list.</summary>
    private static void Ask(Shell shell, UserControl host, Control behind, ResultView owner, ResultView target)
    {
        host.Content = null;
        (behind.Parent as Panel)?.Children.Remove(behind);
        var close = ProblemSheet.Choice(OpenTargetWords.CloseConfirm, () => Close(shell, host, behind, owner, target), primary: true).Id("open-targets-close-confirm");
        var keep = ProblemSheet.Choice(OpenTargetWords.KeepOpen, () => Show(shell, host, behind, owner)).Id("open-targets-keep");
        host.Content = ProblemSheet.Over(behind, OpenTargetWords.CloseQuestion(target), new StackPanel { Spacing = 8, Children = { Screens.Line(OpenTargetWords.CloseWhy), close, keep } },
            [close, keep], () => Show(shell, host, behind, owner));
    }

    /// <summary>
    /// A target closed. Closing the one on screen shows the next open one, or the start of its place where none is left; closing another
    /// leaves the list showing, one shorter.
    /// </summary>
    private static void Close(Shell shell, UserControl host, Control behind, ResultView owner, ResultView target)
    {
        Restore(host, behind);
        shell.CloseTarget(target);
        if (!ReferenceEquals(target, owner) && shell.Targets.Contains(owner) && host.Content is Control still)
        {
            Show(shell, host, still, owner);
        }
    }

    /// <summary>The target's picture, small, the way up its screen shows it; the aim mark where it has none.</summary>
    private static Control Picture(ResultView target)
    {
        Control inside;
        if (target.Thumbnail() is { } bitmap)
        {
            var image = new Image { Source = bitmap, Stretch = Stretch.UniformToFill };
            int turns = GroupLab.Core.Marking.ViewRotation.Normalise(target.PictureTurns);
            inside = turns == 0 ? image : new LayoutTransformControl { LayoutTransform = new RotateTransform(90 * turns), Child = image };
        }
        else
        {
            var mark = GroupLab.App.Theme.Icons.Draw(GroupLab.App.Theme.Icons.Aim, 24);
            mark.HorizontalAlignment = HorizontalAlignment.Center;
            mark.VerticalAlignment = VerticalAlignment.Center;
            inside = mark;
        }

        return new Border { Width = Thumb, Height = Thumb, CornerRadius = new CornerRadius(8), ClipToBounds = true, Child = inside, Classes = { PhoneStyles.Tile }, Padding = new Thickness(0) };
    }

    /// <summary>A small picture of a sheet, decoded at the thumbnail's size; null where the file cannot be read.</summary>
    internal static Bitmap? Decode(string? path)
    {
        if (path is null || !File.Exists(path))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(path);
            return Bitmap.DecodeToWidth(stream, (int)(Thumb * 2));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or NotSupportedException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "targets.thumbnail", e);
            return null;
        }
    }

    /// <summary>A chevron pointing down, in the icons' 16 square: the name opens a list.</summary>
    private const string ChevronDown = "M2,5 L3.4,3.6 L8,8.2 L12.6,3.6 L14,5 L8,11 Z";
}

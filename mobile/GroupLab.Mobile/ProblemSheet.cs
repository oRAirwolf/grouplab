using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using GroupLab.App.Diagnostics;
using Avalonia.Media;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 356 section 1: on the phone a dialog is a sheet in the middle of the screen, over what was there, with its
/// title, a × to dismiss it, and its choices stacked, the most useful first. It stays until a choice is made or it is dismissed. A keyboard
/// on a tablet takes Enter for the first choice and Escape to dismiss, as the desktop does.
/// </summary>
internal static class ProblemSheet
{
    /// <summary>The scrim's colour: the screen behind, dimmed, as the canvas's phone boards draw it.</summary>
    private static readonly IBrush Scrim = new SolidColorBrush(Color.FromArgb(178, 8, 9, 11));

    /// <summary>
    /// The sheet over <paramref name="behind"/>: <paramref name="body"/> under the title, the first of <paramref name="choices"/> taken by
    /// Enter, and <paramref name="dismissed"/> by the × and by Escape.
    /// </summary>
    public static Control Over(Control behind, string title, Control body, IReadOnlyList<Button> choices, Action dismissed)
    {
        ArgumentNullException.ThrowIfNull(choices);
        var dismiss = new Button { Content = "×", FontSize = 22, MinWidth = 44, MinHeight = 44, Background = Brushes.Transparent, BorderThickness = new Thickness(0), VerticalAlignment = VerticalAlignment.Top, Classes = { PhoneStyles.SheetDismiss } };
        Avalonia.Automation.AutomationProperties.SetName(dismiss, GroupLab.Core.Registration.OpeningWords.Dismiss);
        dismiss.Click += (_, _) => dismissed();
        var heading = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 10 };
        var titleText = new TextBlock { Text = title, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, Classes = { PhoneStyles.Heading } };
        Grid.SetColumn(dismiss, 1);
        heading.Children.Add(titleText);
        heading.Children.Add(dismiss);
        var column = new StackPanel { Spacing = 11, Children = { heading, body } };
        var card = new Border
        {
            Child = new ScrollViewer { Content = column },
            Margin = new Thickness(16, 56, 16, 16),
            VerticalAlignment = VerticalAlignment.Top,
            Classes = { PhoneStyles.Card },
        };
        Avalonia.Automation.AutomationProperties.SetName(card, title);
        var layer = new Panel { Children = { behind, new Border { Background = Scrim }, card } };
        layer.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                dismissed();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter && e.Source is not Button && choices.Count > 0)
            {
                choices[0].RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                e.Handled = true;
            }
        };
        return layer;
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 363 section 3.4, the phone half of entry 356 section 3: a failure that stops the work, on the page that
    /// <paramref name="near"/> sits on, as the centred sheet with its choices, the first the most useful; every choice and the × put the page
    /// back. Where <paramref name="near"/> is on no page yet, <paramref name="line"/> says it instead, as before.
    /// </summary>
    public static void Stop(Control near, TextBlock? line, string title, string why, params (string Words, Action Chosen)[] choices)
    {
        ArgumentNullException.ThrowIfNull(near);
        if (near.FindLogicalAncestorOfType<UserControl>(includeSelf: true) is not { Content: Control behind } page)
        {
            if (line is not null)
            {
                line.Text = why;
            }

            return;
        }

        if (line is not null)
        {
            line.Text = "";
        }

        page.Content = null;
        void Close()
        {
            if (page.Content is Panel layer)
            {
                layer.Children.Remove(behind);
            }

            page.Content = behind;
        }

        var all = choices.Length == 0 ? [("Close", () => { })] : choices;
        var buttons = all.Select((c, i) => Choice(c.Words, () =>
        {
            Close();
            c.Chosen();
        }, primary: i == 0)).ToList();
        var body = new StackPanel { Spacing = 11, Children = { Screens.Line(why) } };
        foreach (var button in buttons)
        {
            body.Children.Add(button);
        }

        page.Content = Over(behind, title, body, buttons, Close);
        DiagnosticLog.Info("problem.sheet", ("title", title));
    }

    /// <summary>A choice on a sheet: a big button with its name for a screen reader.</summary>
    public static Button Choice(string words, Action chosen, bool primary = false)
    {
        var button = primary ? Screens.Primary(words, chosen) : Screens.Choice(words, chosen);
        Avalonia.Automation.AutomationProperties.SetName(button, words);
        return button;
    }
}

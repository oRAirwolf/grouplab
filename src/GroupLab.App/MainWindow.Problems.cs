using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 356: what stops the work, or needs the person's decision, is said in the middle of the window, over the
/// work, until a choice is made or it is dismissed; what needs nothing stays in the status line. One layer holds whichever is open: the
/// calm "Which target is this?" of section 5, the problem dialog of board B, or a plain one for any other failure (section 3).
/// <para>
/// Keyboard: Enter takes the first choice, Escape dismisses, Tab moves between the choices, and every button carries the name a screen
/// reader says.
/// </para>
/// </summary>
public sealed partial class MainWindow
{
    /// <summary>The scrim and the card in the middle of it; one at a time.</summary>
    private readonly Panel problemLayer = new() { IsVisible = false };

    /// <summary>The choices of the dialog open now, first first, with what each does.</summary>
    private List<(string Label, Action Act)> problemChoices = [];

    /// <summary>What dismissing the dialog open now does, beyond closing it.</summary>
    private Action? problemDismissed;

    /// <summary>The title of the dialog open now, or null, for the headless tests.</summary>
    internal string? ProblemTitle { get; private set; }

    /// <summary>The choices of the dialog open now, as their buttons say them, for the headless tests.</summary>
    internal IReadOnlyList<string> ProblemChoices => [.. problemChoices.Select(c => c.Label)];

    /// <summary>Every button of the dialog open now, its × included, for the headless tests.</summary>
    internal IReadOnlyList<Button> ProblemButtons => [.. problemLayer.GetLogicalDescendants().OfType<Button>()];

    /// <summary>Whether a dialog is open in the middle of the window.</summary>
    internal bool ProblemOpen => problemLayer.IsVisible;

    private Panel BuildProblemLayer()
    {
        problemLayer.Background = new SolidColorBrush(Tokens.Scrim);
        return problemLayer;
    }

    /// <summary>
    /// The layer moved, once, into the window's overlay, which is drawn over everything in the window: the side columns' scrolling panels
    /// were drawn over a layer that was only last among the window's children. It follows the window's size from then on.
    /// </summary>
    private void RaiseProblemLayer()
    {
        if (problemLayer.Parent is Avalonia.Controls.Primitives.OverlayLayer || Avalonia.Controls.Primitives.OverlayLayer.GetOverlayLayer(this) is not { } overlay)
        {
            return;
        }

        (problemLayer.Parent as Panel)?.Children.Remove(problemLayer);
        overlay.Children.Add(problemLayer);
        void Fit()
        {
            problemLayer.Width = ClientSize.Width;
            problemLayer.Height = ClientSize.Height;
        }

        Fit();
        PropertyChanged += (_, e) =>
        {
            if (e.Property == ClientSizeProperty)
            {
                Fit();
            }
        };
    }

    /// <summary>Presses the named choice of the dialog open now, as a click does, for the headless tests and the keyboard.</summary>
    internal void PressProblemChoice(string label)
    {
        var choice = problemChoices.FirstOrDefault(c => c.Label == label);
        if (choice.Act is null)
        {
            return;
        }

        DiagnosticLog.Info("problem.choice", ("title", ProblemTitle), ("choice", label));
        NoteProblem(ProblemTitle ?? "", label);
        CloseProblem();
        choice.Act();
    }

    /// <summary>Dismisses the dialog open now, as Escape and its × do.</summary>
    internal void DismissProblem()
    {
        if (!ProblemOpen)
        {
            return;
        }

        DiagnosticLog.Info("problem.dismiss", ("title", ProblemTitle));
        var then = problemDismissed;
        CloseProblem();
        then?.Invoke();
    }

    private void CloseProblem()
    {
        problemLayer.IsVisible = false;
        problemLayer.Children.Clear();
        problemChoices = [];
        problemDismissed = null;
        ProblemTitle = null;
    }

    /// <summary>
    /// Enter and Escape while a dialog is open; true where the key belongs to the dialog, so nothing behind it takes it. A choice reached
    /// with Tab takes its own Enter, as any button does.
    /// </summary>
    private bool ProblemKey(KeyEventArgs e)
    {
        if (!ProblemOpen)
        {
            return false;
        }

        switch (e.Key)
        {
            case Key.Escape:
                DismissProblem();
                e.Handled = true;
                return true;
            case Key.Enter when !(focusFromKeyboard && e.Source is Button):
                if (problemChoices.Count > 0)
                {
                    PressProblemChoice(problemChoices[0].Label);
                }

                e.Handled = true;
                return true;
            default:
                return e.Key is not Key.Tab;
        }
    }

    /// <summary>The card, centred over the scrim, with its title row and the dismiss button every dialog has.</summary>
    private Border ProblemCard(string title, double width, Control body)
    {
        var dismiss = new Button { Content = "×", FontSize = Tokens.TitleSize, Classes = { AppStyles.Link }, MinWidth = 32, MinHeight = 32, VerticalAlignment = VerticalAlignment.Top };
        AutomationProperties.SetName(dismiss, OpeningWords.Dismiss);
        dismiss.Click += (_, _) => DismissProblem();
        var heading = new DockPanel();
        DockPanel.SetDock(dismiss, Dock.Right);
        heading.Children.Add(dismiss);
        heading.Children.Add(new TextBlock { Text = title, FontSize = Tokens.TitleSize, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center });
        var column = new StackPanel { Spacing = Tokens.Space12 };
        column.Children.Add(heading);
        column.Children.Add(body);
        var card = new Border
        {
            Child = new ScrollViewer { Content = column },
            Width = width,
            MaxHeight = 760,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Classes = { AppStyles.ProblemCard },
        };
        AutomationProperties.SetName(card, title);
        return card;
    }

    /// <summary>Puts a card up, with its choices in order and what dismissing it does, and gives the first choice the focus.</summary>
    private void OpenProblem(string title, Border card, List<(string Label, Action Act)> choices, Action? dismissed = null)
    {
        CloseProblem();
        ProblemTitle = title;
        problemChoices = choices;
        problemDismissed = dismissed;
        // Centred over the picture, clear of the right column where the window is wide enough, as the boards place it over the sheet: the
        // headless renderer drew the right column's scrolling panel over anything laid across it.
        if (ClientSize.Width - Tokens.RightColumnWidth - 64 >= card.Width + Tokens.Space24)
        {
            card.Margin = new Thickness(0, 0, Tokens.RightColumnWidth, 0);
        }

        problemLayer.Children.Add(card);
        RaiseProblemLayer();
        problemLayer.IsVisible = true;
        DiagnosticLog.Info("problem.open", ("title", title), ("choices", choices.Count));
        NoteProblem(title, null);
        Avalonia.Threading.Dispatcher.UIThread.Post(() => card.GetLogicalDescendants().OfType<Button>().FirstOrDefault(b => !b.Classes.Contains(AppStyles.Link))?.Focus());
    }

    /// <summary>A choice's button: its words, its screen reader name, and what it does through the dialog, so the dialog closes first.</summary>
    private Button ProblemButton(string label, bool primary = false, string? link = null)
    {
        var button = new Button { Content = label, MinHeight = 42, Padding = new Thickness(Tokens.Space16, 0), VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(0) };
        if (primary)
        {
            button.Classes.Add(AppStyles.Primary);
        }

        if (link is not null)
        {
            button.Classes.Add(AppStyles.Link);
            button.MinHeight = 0;
        }

        AutomationProperties.SetName(button, label);
        button.Click += (_, _) => PressProblemChoice(label);
        return button;
    }

    /// <summary>
    /// Entry 356 section 3: a failure that stops the work, said in the middle of the window: a title, one or two sentences on why, and the
    /// choices, the most useful first. With no choice given, OK closes it.
    /// </summary>
    internal void ShowProblem(string title, string why, params (string Label, Action Act)[] choices)
    {
        var list = choices.Length > 0 ? choices.ToList() : [("OK", () => { })];
        var body = new StackPanel { Spacing = Tokens.Space16 };
        body.Children.Add(new TextBlock { Text = why, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } });
        var row = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = Tokens.Space8, LineSpacing = Tokens.Space8 };
        for (int k = 0; k < list.Count; k++)
        {
            row.Children.Add(ProblemButton(list[k].Label, primary: k == 0));
        }

        body.Children.Add(row);
        OpenProblem(title, ProblemCard(title, 520, body), list);
    }

    /// <summary>
    /// Entry 356 section 3: a failure that stops the work, in the middle of the window and in the status line, which keeps its record of it.
    /// </summary>
    internal void Failed(string title, string said, params (string Label, Action Act)[] choices)
    {
        status.Text = said;
        ShowProblem(title, said, choices);
    }

    /// <summary>
    /// Entry 356 section 5, board "Not an error": a picture that is no GroupLab sheet and no store-bought target GroupLab knows. Not an error,
    /// so no warning mark and no amber bar: "Which target is this?", marking it by hand first.
    /// </summary>
    private void AskWhichTargetIsThis(GrayImage g, GrayImage v, ImageMetadata m)
    {
        status.Text = OpeningWords.WhichTitle;
        var list = new List<(string Label, Action Act)>
        {
            (OpeningWords.ByHand.Label, MarkByHand),
            (OpeningWords.StoreBought.Label, () =>
            {
                AddStoreTarget();
                DiagnosticLog.Info("opening.store-bought");
            }),
            (OpeningWords.GroupLabSheet.Label, () => OfferTheSheet(g, v, m, null)),
        };
        var body = new StackPanel { Spacing = Tokens.Space12 };
        body.Children.Add(new TextBlock { Text = OpeningWords.WhichSays, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } });
        var choices = new StackPanel { Spacing = Tokens.Space8 };
        foreach (var (choice, icon, first) in new[] { (OpeningWords.ByHand, Icons.Length, true), (OpeningWords.StoreBought, Icons.Impact, false), (OpeningWords.GroupLabSheet, Icons.Library, false) })
        {
            var words = new StackPanel { Spacing = Tokens.Space4, VerticalAlignment = VerticalAlignment.Center };
            words.Children.Add(new TextBlock { Text = choice.Label, FontWeight = first ? FontWeight.SemiBold : FontWeight.Medium, TextWrapping = TextWrapping.Wrap });
            words.Children.Add(new TextBlock { Text = choice.Says, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } });
            var face = new DockPanel();
            var mark = Icons.Draw(icon);
            mark.Margin = new Thickness(0, 0, Tokens.Space12, 0);
            mark.VerticalAlignment = VerticalAlignment.Center;
            DockPanel.SetDock(mark, Dock.Left);
            face.Children.Add(mark);
            face.Children.Add(words);
            var button = new Button
            {
                Content = face,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(Tokens.Space12),
                Margin = new Thickness(0),
            };
            if (first)
            {
                button.Classes.Add(AppStyles.Chosen);
            }

            AutomationProperties.SetName(button, choice.Label);
            string label = choice.Label;
            button.Click += (_, _) => PressProblemChoice(label);
            choices.Children.Add(button);
        }

        body.Children.Add(choices);
        body.Children.Add(new TextBlock { Text = OpeningWords.WhichFooter, TextWrapping = TextWrapping.Wrap, FontSize = Tokens.SecondarySize, Classes = { AppStyles.Faint } });
        OpenProblem(OpeningWords.WhichTitle, ProblemCard(OpeningWords.WhichTitle, 500, body), list);
        DiagnosticLog.Info("opening.which-target");
    }

    /// <summary>Marking by hand: the length tool, for the one true length the scale needs, and the words for it.</summary>
    private void MarkByHand()
    {
        sheetChooser.IsVisible = false;
        pendingDetection = null;
        SetTool(MarkingTool.Length);
        status.Text = OpeningWords.ByHandStatus;

        // Entry 365: markers in the photo set the scale by themselves.
        ScaleFromMarkers();
    }
}

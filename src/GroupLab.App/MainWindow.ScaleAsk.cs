using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using GroupLab.App.Theme;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 517 section 1: a person who chose to mark a picture by hand was told only in the status line to set a scale,
/// and the Scale part of the right column sat under the setup, out of view and empty until two points had been tapped. Now "Set the scale"
/// sits at the top of the right column from the moment marking by hand is chosen until a scale is set: what the scale is, the three ways to
/// give it, and the length or size to type once the points are placed, which move into the card while it shows.
/// </summary>
public sealed partial class MainWindow
{
    private readonly Border scaleAsk = new() { IsVisible = false, Classes = { AppStyles.ResultCard } };

    /// <summary>Where the chosen tool's inputs sit while the card shows.</summary>
    private readonly StackPanel scaleAskInputs = new() { Spacing = Tokens.Space8 };

    /// <summary>What the tool in hand asks for, inside the card.</summary>
    private readonly TextBlock scaleAskHow = new() { TextWrapping = TextWrapping.Wrap, IsVisible = false, Classes = { AppStyles.Secondary } };

    /// <summary>Whether marking by hand was chosen for the picture open now.</summary>
    private bool scaleAskWanted;

    /// <summary>The right column's own place for the scale's inputs, under its Scale heading, and where in it they go back to.</summary>
    private Panel? scaleHome;

    private int scaleHomeAt;

    /// <summary>Whether "Set the scale" is showing, for the headless tests.</summary>
    internal bool ScaleAskShown => scaleAsk.IsVisible;

    /// <summary>The card, for the headless tests.</summary>
    internal Border ScaleAskCard => scaleAsk;

    private Border BuildScaleAsk()
    {
        var column = new StackPanel { Spacing = Tokens.Space8 };
        column.Children.Add(new TextBlock { Text = OpeningWords.ScaleAskTitle, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Section } });
        column.Children.Add(new TextBlock { Text = OpeningWords.ScaleAskSays, TextWrapping = TextWrapping.Wrap });
        foreach (var (words, act) in new (string, Action)[]
        {
            (OpeningWords.ScaleAskLength, () => SetTool(MarkingTool.Length)),
            (OpeningWords.ScaleAskCorners, () => SetTool(MarkingTool.Rectangle)),
            (OpeningWords.ScaleAskPaper, () =>
            {
                SetTool(MarkingTool.Rectangle);
                FindPaper();
            }),
        })
        {
            var button = Button(words, act);
            button.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            AutomationProperties.SetName(button, words);
            column.Children.Add(button);
        }

        column.Children.Add(scaleAskHow);
        column.Children.Add(scaleAskInputs);
        scaleAsk.Child = column;
        AutomationProperties.SetName(scaleAsk, OpeningWords.ScaleAskTitle);
        return scaleAsk;
    }

    /// <summary>Marking by hand was chosen: ask for the scale where the person is looking, until there is one.</summary>
    private void AskForScale()
    {
        scaleAskWanted = true;
        ShowScaleAsk();
    }

    /// <summary>A new picture: nothing is asked of it yet.</summary>
    private void ForgetScaleAsk()
    {
        scaleAskWanted = false;
        ShowScaleAsk();
    }

    /// <summary>Shows the card while marking by hand has no scale, moving the tool's inputs into it, and puts them back once it has one.</summary>
    private void ShowScaleAsk()
    {
        bool show = scaleAskWanted && session.State.Scale is null && grey is not null;
        scaleHome ??= scaleInputs.Parent as Panel;
        if (show)
        {
            scaleAskHow.Text = canvas.Tool is MarkingTool.Length or MarkingTool.Rectangle ? ToolStatus(canvas.Tool) : "";
            scaleAskHow.IsVisible = scaleAskHow.Text.Length > 0;
        }

        if (show == scaleAsk.IsVisible)
        {
            return;
        }

        scaleAsk.IsVisible = show;
        if (show && scaleHome is not null && scaleInputs.Parent == scaleHome)
        {
            scaleHomeAt = scaleHome.Children.IndexOf(scaleInputs);
            scaleHome.Children.Remove(scaleInputs);
            scaleAskInputs.Children.Add(scaleInputs);
        }
        else if (!show && scaleHome is not null && scaleInputs.Parent == scaleAskInputs)
        {
            scaleAskInputs.Children.Remove(scaleInputs);
            scaleHome.Children.Insert(Math.Clamp(scaleHomeAt, 0, scaleHome.Children.Count), scaleInputs);
        }

        if (show)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => scaleAsk.BringIntoView());
        }
    }
}

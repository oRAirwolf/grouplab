using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.App.Theme;
using GroupLab.Core.Imaging;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 163: the first real user's feedback on the marking screen. He is the first person other than the developer to
/// use GroupLab for real, so what he found outranks any guess this project had made about what a new user needs.
/// </summary>
public class FirstUserTests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static string Repository([System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));

    private static string Source() => File.ReadAllText(Path.Combine(Repository(), "src", "GroupLab.App", "MainWindow.cs"));

    /// <summary>
    /// Section 7.2: the default tool on open pans on a drag over the sheet and selects on a click on a mark, and detection no longer
    /// switches to another tool. A drag that starts on a mark pans and leaves the mark where it was.
    /// </summary>
    [AvaloniaFact]
    public void TheDefaultToolPansOnADragAndSelectsOnAClick()
    {
        var (window, _, _) = Entry109Tests.Sheet();
        try
        {
            var canvas = window.Canvas;
            Assert.Equal(MarkingTool.Pan, canvas.Tool);
            canvas.FitToView();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Settle();

            var shot = window.Session.State.Shots.First(s => s.IsShot);
            var at = canvas.TranslatePoint(canvas.ToControl(shot.Image), window)!.Value;

            // A click on the mark selects it.
            window.MouseDown(at, MouseButton.Left);
            window.MouseUp(at, MouseButton.Left);
            Settle();
            Assert.Equal(shot.Id, canvas.Selected);

            // A drag that starts on the mark pans, and the mark does not move.
            var before = window.Session.State.Find(shot.Id)!.Image;
            var offsetBefore = canvas.ToControl(before);
            window.MouseDown(at, MouseButton.Left);
            window.MouseMove(at + new Point(60, 40));
            window.MouseUp(at + new Point(60, 40), MouseButton.Left);
            Settle();
            Assert.Equal(before, window.Session.State.Find(shot.Id)!.Image);
            Assert.NotEqual(offsetBefore, canvas.ToControl(before));
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Section 7.3: pan and select are on neighbouring keys under the left hand, C and V, and neither conflicts with another shortcut, the
    /// review keys or bull entry. P still pans, so nobody who learned it is broken.
    /// </summary>
    [AvaloniaFact]
    public void PanAndSelectAreNeighboursAndNothingElseUsesThem()
    {
        var (window, _, _) = Entry109Tests.Sheet();
        try
        {
            window.Canvas.Tool = MarkingTool.Select;
            window.KeyPressQwerty(PhysicalKey.C, RawInputModifiers.None);
            Settle();
            Assert.Equal(MarkingTool.Pan, window.Canvas.Tool);
            window.KeyPressQwerty(PhysicalKey.V, RawInputModifiers.None);
            Settle();
            Assert.Equal(MarkingTool.Select, window.Canvas.Tool);
            window.KeyPressQwerty(PhysicalKey.P, RawInputModifiers.None);
            Settle();
            Assert.Equal(MarkingTool.Pan, window.Canvas.Tool);
        }
        finally
        {
            window.Close();
        }

        // Nothing else reads a bare C: not a review key, not bull entry, which takes digits and S.
        string source = Source();
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(source, @"case Key\.C\b"));
        Assert.DoesNotContain("Key.C => 'C'", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// Section 7.4: the setup block is first in the panel, above the review, and every empty needed field carries both the outline and the
    /// word "needed". Saying "not known" clears the mark.
    /// </summary>
    [AvaloniaFact]
    public void TheSetupBlockIsFirstAndSaysWhatIsNeeded()
    {
        var (window, _, _) = Entry109Tests.Sheet();
        try
        {
            var setup = window.GetLogicalDescendants().OfType<StackPanel>().Single(p => p.Name == "SetupBlock");
            var column = (StackPanel)setup.Parent!;
            var review = column.Children.OfType<TextBlock>().First(t => t.Text == "Review");
            Assert.True(column.Children.IndexOf(setup) < column.Children.IndexOf(review), "the setup block is below the review");

            Assert.Contains("caliber", window.StillNeeded());
            Assert.Contains("shot distance", window.StillNeeded());
            var words = setup.GetLogicalDescendants().OfType<TextBlock>().Where(t => t.Text == "needed" && t.IsVisible).ToList();
            var frames = setup.GetLogicalDescendants().OfType<Border>().Where(b => b.Classes.Contains(AppStyles.Needed)).ToList();
            Assert.Equal(window.StillNeeded().Count, words.Count);
            Assert.Equal(window.StillNeeded().Count, frames.Count);

            // "Not known" is an answer, and it clears the mark.
            var notKnown = setup.GetLogicalDescendants().OfType<Button>().Where(b => (b.Content as string) == "Not known").ToList();
            Assert.Equal(3, notKnown.Count);
            foreach (var button in notKnown)
            {
                button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            }

            Settle();
            Assert.Empty(window.StillNeeded());
            Assert.DoesNotContain(setup.GetLogicalDescendants().OfType<Border>(), b => b.Classes.Contains(AppStyles.Needed));
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Section 7.5: every explanation on the analysis screen is collapsed on first open, to its line, with a "why" to open it. The judgement
    /// cards open as their verdict alone.
    /// </summary>
    [AvaloniaFact]
    public void EveryExplanationOnTheAnalysisScreenIsCollapsedOnFirstOpen()
    {
        var (window, _, _) = Entry109Tests.Sheet();
        try
        {
            window.Session.SetShotDistance(3600);
            window.Session.SetCalibre(GroupLab.Core.Marking.Calibre.Of(0.308));
            window.Analyse();
            Settle();

            var bodies = window.GetLogicalDescendants().OfType<StackPanel>().Where(p => p.Classes.Contains(AppStyles.WhyBody)).ToList();
            Assert.NotEmpty(bodies);
            Assert.All(bodies, b => Assert.False(b.IsVisible, "an explanation is open on first view"));

            foreach (var card in window.GetLogicalDescendants().OfType<Border>().Where(b => (b.Name ?? "").EndsWith("Card", StringComparison.Ordinal)))
            {
                var shown = card.GetLogicalDescendants().OfType<TextBlock>().Where(t => Entry109Tests.Shown(t) && t.Text is { Length: > 12 }).ToList();
                Assert.True(shown.Count <= 1, $"{card.Name} shows {shown.Count} sentences before anybody asked why");
            }
        }
        finally
        {
            window.Close();
        }
    }
}

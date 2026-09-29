using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 290 section 2 item 4, the part any machine can prove: the shared screens start on a phone that is neither
/// Android nor iOS, every tab opens, and a committed sample is analyzed end to end through the path a picked picture takes, finding every
/// shot the desktop finds. The iOS Simulator job proves the same on iOS, and the Android head on Android.
/// </summary>
public class SharedScreensTests
{
    private static readonly TestPhone ThePhone = new();

    private static Shell Started()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(ThePhone, Avalonia.Application.Current!, () => "US", null);
        }

        var shell = new Shell();
        var window = new Window { Width = 412, Height = 915, Content = shell };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return shell;
    }

    [AvaloniaFact]
    public void EveryTabOpens()
    {
        var shell = Started();
        foreach (var place in Enum.GetValues<Shell.Place>())
        {
            shell.Show(place);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(place, shell.Showing);
        }
    }

    [AvaloniaFact]
    public void APickedPictureIsAnalyzedEndToEnd()
    {
        Started();
        string sample = Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png");
        string copy = Path.Combine(ThePhone.CacheFolder, "chosen.png");
        File.Copy(sample, copy, overwrite: true);

        var result = PhoneAnalysis.Run(copy, new ShotSetup(null, null), UnitSettings.Imperial, null, CancellationToken.None);

        Assert.Null(result.Failure);
        Assert.NotNull(result.Definition);
        Assert.Equal(25, result.State.Shots.Count(s => s.IsShot));
        Assert.NotNull(result.SessionId);
    }

    /// <summary>
    /// Every button on the result of a real analysis, pressed one at a time on a fresh result, opens what it opens without an exception:
    /// the shots, zero from this group, Fudd buster mode, Ballistics with the group, the bulls, sharing (to the stand-in's share sheet).
    /// </summary>
    [AvaloniaFact]
    public void EveryButtonOnTheResultCanBePressed()
    {
        var shell = Started();
        string copy = Path.Combine(ThePhone.CacheFolder, "chosen.png");
        File.Copy(Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png"), copy, overwrite: true);
        var result = PhoneAnalysis.Run(copy, new ShotSetup(GroupLab.Core.Marking.Calibre.Of(0.308), 3600), UnitSettings.Imperial, null, CancellationToken.None);
        Assert.Null(result.Failure);

        var window = (Window)TopLevel.GetTopLevel(shell)!;
        List<string> Buttons()
        {
            var view = new ResultView(result, new ShotSetup(GroupLab.Core.Marking.Calibre.Of(0.308), 3600), UnitSettings.Imperial, () => { });
            window.Content = view;
            Dispatcher.UIThread.RunJobs();
            return view.GetVisualDescendants().OfType<Button>().Where(b => b.IsEffectivelyEnabled && b.IsEffectivelyVisible)
                .Select(Words).Where(w => w.Length > 0).Distinct().ToList();
        }

        var names = Buttons();
        Assert.True(names.Count >= 8, "the result has " + names.Count + " buttons: " + string.Join(", ", names));
        var pressed = new List<string>();
        foreach (string name in names)
        {
            var view = new ResultView(result, new ShotSetup(GroupLab.Core.Marking.Calibre.Of(0.308), 3600), UnitSettings.Imperial, () => { });
            window.Content = view;
            Dispatcher.UIThread.RunJobs();
            if (view.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => Words(b) == name) is not { } button)
            {
                continue;
            }

            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            pressed.Add(name);
        }

        Assert.Equal(names.Count, pressed.Count);
        window.Content = shell;
    }

    /// <summary>Entry 268 on a head with no native idle screen: black over everything, and Close gives the screen back.</summary>
    [AvaloniaFact]
    public void TheIdleScreenIsBlackAndGivesTheScreenBack()
    {
        var shell = Started();
        var before = shell.Content;
        shell.ShowIdle();
        Dispatcher.UIThread.RunJobs();
        var idle = Assert.IsType<IdleScreen>(shell.Content);
        Assert.Equal(Avalonia.Media.Colors.Black, ((Avalonia.Media.ISolidColorBrush)idle.Background!).Color);
        Assert.False(idle.Speaking);
        Assert.Same(Avalonia.Media.Brushes.Black, TopLevel.GetTopLevel(shell)!.Background);
        idle.RaiseEvent(new Avalonia.Input.TappedEventArgs(Avalonia.Input.InputElement.TappedEvent, null!));
        Assert.True(idle.Speaking);
        idle.GetVisualDescendants().OfType<Button>().Single().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Same(before, shell.Content);
        Assert.NotSame(Avalonia.Media.Brushes.Black, TopLevel.GetTopLevel(shell)!.Background);
    }

    private static string Words(Button button) =>
        string.Join(" ", button.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).Where(t => !string.IsNullOrWhiteSpace(t)));
}

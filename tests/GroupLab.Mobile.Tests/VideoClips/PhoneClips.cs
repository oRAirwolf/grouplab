using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using GroupLab.App;
using GroupLab.Mobile.Dev;
using GroupLab.Tests.Support;

namespace GroupLab.Mobile.Tests.VideoClips;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 475: the phone's clips for the launch videos, made only when <c>GROUPLAB_VIDEO_CLIPS</c> names their folder.
/// The phone is GroupLab's own phone screens drawn headless at the Fold 7's size in the emulator walk, 411 by 960, scaled to its 1080 by 2520
/// pixels, in the dark theme; taps are shown as touches. Not the emulator: the picture a person takes is handed to Capture as the walk hands
/// it, and the reading that follows is GroupLab's own, drawn as it happens.
/// </summary>
public class PhoneClips
{
    internal const double Scale = 2.625;
    internal const int Wide = 411, Tall = 960;

    private static readonly TestPhone ThePhone = new();

    internal static string Repository([System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", ".."));

    /// <summary>A phone at the Fold 7's size, dark, its first run answered, showing <paramref name="place"/>.</summary>
    internal static (Window Window, Shell Shell) Phone(Shell.Place place = Shell.Place.Capture, double wide = Wide, double tall = Tall)
    {
        if (GroupLab.Mobile.Phone.Platform is null)
        {
            GroupLab.Mobile.Phone.Start(ThePhone, Application.Current!, () => "US", null);
        }

        Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;
        Scenario.AnswerFirstRun(GroupLab.Mobile.Phone.Settings);
        GroupLab.Mobile.Phone.Settings.SaveShotSetup(".308", 3600);
        var shell = new Shell();
        var window = new Window
        {
            Width = Math.Ceiling(wide * Scale / 2) * 2,
            Height = Math.Ceiling(tall * Scale / 2) * 2,
            RequestedThemeVariant = ThemeVariant.Dark,
            Content = new LayoutTransformControl { LayoutTransform = new ScaleTransform(Scale, Scale), Child = shell },
        };
        window.Show();
        shell.Show(place);
        ScreenClip.Settle();
        HideOldErrorNote(window);
        return (window, shell);
    }

    /// <summary>
    /// The note offering to send an error report from an earlier run floats over the page; in the test process it is about an earlier test
    /// process that was stopped, never about the clip, so it is hidden as the phone's layout tests hide it.
    /// </summary>
    internal static void HideOldErrorNote(Window window)
    {
        foreach (var note in window.GetLogicalDescendants().OfType<TextBlock>().Where(t => t.Text?.StartsWith("GroupLab hit an error", StringComparison.Ordinal) == true).ToList())
        {
            if (Avalonia.VisualTree.VisualExtensions.FindAncestorOfType<Border>(note) is { } card && card.Classes.Contains(PhoneStyles.Card))
            {
                card.IsVisible = false;
            }
        }
    }

    internal static void Close(Window window)
    {
        window.Close();
        Application.Current!.RequestedThemeVariant = ThemeVariant.Default;
    }

    /// <summary>The first showing control that says <paramref name="words"/>, or the button it is the words of.</summary>
    internal static Control Find(Visual root, string words)
    {
        var found = root.GetLogicalDescendants().OfType<Control>().Where(c => c.IsEffectivelyVisible).FirstOrDefault(c => c switch
        {
            Button { Content: string s } => s.Contains(words, StringComparison.Ordinal),
            TextBlock t => t.Text?.Contains(words, StringComparison.Ordinal) == true,
            _ => false,
        }) ?? throw new InvalidOperationException($"nothing showing says \"{words}\"");
        return found.GetLogicalAncestors().OfType<Button>().FirstOrDefault() ?? found;
    }

    /// <summary>Hands the sample sheet to Capture as a picture taken, and records the reading until the result is up.</summary>
    internal static void Read(ScreenClip? clip, Shell shell, Window window)
    {
        string path = Path.Combine(GroupLab.Mobile.Phone.Platform!.FilesFolder, "clip-sample.png");
        File.Copy(Path.Combine(Repository(), "samples", "gl-cf25-ltr-d-25-shots-600-dpi.png"), path, overwrite: true);
        var handle = new PhotoHandle(null, null, null, new FileInfo(path).Length, ".png", () => Task.FromResult<Stream?>(File.OpenRead(path)));
        if (CapturePage.Latest is { } capture)
        {
            capture.TypedCalibre = ".308";
        }

        CapturePage.SharedPicture!([handle]);
        ScreenClip.Settle();
        clip?.Redraw();
        clip?.Hold(0.2);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        bool used = false;
        while (clock.Elapsed < TimeSpan.FromMinutes(3))
        {
            Thread.Sleep(40);
            ScreenClip.Settle();
            HideOldErrorNote(window);
            clip?.Redraw();
            clip?.Hold(0.1);
            var shown = window.GetLogicalDescendants().ToList();
            if (shown.OfType<ResultView>().Any())
            {
                break;
            }

            if (!used && shown.OfType<FeedbackView>().Any())
            {
                used = true;
                if (clip is null)
                {
                    var use = Find(window, "Use this picture");
                    use.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                }
                else
                {
                    clip.Hold(0.8);
                    clip.Click(Find(window, "Use this picture"), hold: 0.3);
                }
            }
        }

        ScreenClip.Settle();
        HideOldErrorNote(window);
        clip?.Redraw();
    }

    /// <summary>Video 1: a sheet handed to the phone, read, and its holes appearing with their numbers.</summary>
    [AvaloniaFact]
    public void V1PhotoToHoles()
    {
        ScreenClip.SkipUnlessAsked();
        var (window, shell) = Phone();
        try
        {
            using var clip = new ScreenClip(window, "v1-photo-to-holes", "On the phone: the sample sheet taken in, read, and the picture with its holes found and numbered", touches: true);
            Read(clip, shell, window);
            clip.Hold(1.5);
            if (window.GetLogicalDescendants().OfType<Control>().FirstOrDefault(c => c.GetType().Name == "SheetPicture") is { } picture)
            {
                clip.Act(() => picture.BringIntoView(), 3);
            }
        }
        finally
        {
            Close(window);
        }
    }

    /// <summary>Video 1: the phone's result screen, the group and its figures, scrolled through slowly.</summary>
    [AvaloniaFact]
    public void V1PhoneResult()
    {
        ScreenClip.SkipUnlessAsked();
        var (window, shell) = Phone();
        try
        {
            Read(null, shell, window);
            using var clip = new ScreenClip(window, "v1-phone-result", "The phone's result: the group on the sheet, then its figures scrolled through", touches: true);
            clip.Hold(2);
            if (window.GetLogicalDescendants().OfType<ScrollViewer>().Where(s => s.IsEffectivelyVisible).OrderByDescending(s => s.Extent.Height).FirstOrDefault() is { } scroller)
            {
                double end = Math.Min(scroller.Extent.Height - scroller.Viewport.Height, Tall * 1.4);
                for (int i = 1; i <= 150; i++)
                {
                    double t = i / 150.0;
                    scroller.Offset = new Vector(0, end * t * t * (3 - (2 * t)));
                    clip.Redraw();
                    clip.Hold(1.0 / ScreenClip.Fps);
                }
            }

            clip.Hold(3);
        }
        finally
        {
            Close(window);
        }
    }

    /// <summary>Video 3, step 9: the phone's result, then down to its zero correction with the clicks.</summary>
    [AvaloniaFact]
    public void V3Step9PhoneZero()
    {
        ScreenClip.SkipUnlessAsked();
        var (window, shell) = Phone();
        try
        {
            Read(null, shell, window);
            using var clip = new ScreenClip(window, "v3-step9-phone-zero", "The phone's result, then its zero correction: where the group sits and the clicks to dial", touches: true);
            clip.Hold(2);
            var zero = window.GetLogicalDescendants().OfType<TextBlock>().Where(t => t.IsEffectivelyVisible)
                .FirstOrDefault(t => t.Text?.Contains("click", StringComparison.OrdinalIgnoreCase) == true || t.Text?.StartsWith("Zero", StringComparison.Ordinal) == true);
            if (zero is not null)
            {
                clip.Act(() => zero.BringIntoView(), 3);
            }
        }
        finally
        {
            Close(window);
        }
    }
}

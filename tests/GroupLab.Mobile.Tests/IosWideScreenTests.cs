using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 290 section 6: an iPhone on its side, which pads 62 points each side for the camera's island. On iOS the bar
/// along the bottom reaches both side edges, and the result goes side by side by the screen's width; on Android both stay as they were.
/// </summary>
public class IosWideScreenTests
{
    private static readonly TestPhone ThePhone = new();

    private static readonly Thickness OnItsSide = new(62, 0, 62, 20);

    private static Shell Padded()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(ThePhone, Avalonia.Application.Current!, () => "US", null);
        }

        var shell = new Shell();
        var window = new Window { Width = 956, Height = 440, Content = shell };
        window.Show();
        shell.Padding = OnItsSide;
        Dispatcher.UIThread.RunJobs();
        return shell;
    }

    /// <summary>The bar along the bottom, which the first run's questions may be covering.</summary>
    private static Border Bar(Shell shell) =>
        (Border)typeof(Shell).GetField("nav", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(shell)!;

    [AvaloniaFact]
    public void OnIosTheBarReachesTheSideEdgesAndOnAndroidItDoesNot()
    {
        bool before = Shell.BarToBottomEdge;
        try
        {
            Shell.BarToBottomEdge = true;
            var nav = Bar(Padded());
            Assert.Equal(new Thickness(-62, 0, -62, -20), nav.Margin);
            Assert.Equal(62, nav.Padding.Left);
            Assert.Equal(62, nav.Padding.Right);

            Shell.BarToBottomEdge = false;
            nav = Bar(Padded());
            Assert.Equal(default, nav.Margin);
        }
        finally
        {
            Shell.BarToBottomEdge = before;
        }
    }

    [AvaloniaFact]
    public void OnIosTheResultGoesSideBySideByTheScreensWidth()
    {
        bool before = Shell.WideByScreen;
        try
        {
            Padded();
            string sample = Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png");
            string copy = Path.Combine(ThePhone.CacheFolder, "wide.png");
            File.Copy(sample, copy, overwrite: true);
            var result = PhoneAnalysis.Run(copy, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, null, CancellationToken.None);
            Assert.Null(result.Failure);

            // The width left inside the safe area of a Pro Max on its side, and the height above the bar.
            bool SideBySide(bool byScreen)
            {
                Shell.WideByScreen = byScreen;
                var view = new ResultView(result, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, () => { });
                var window = new Window { Width = 832, Height = 330, Content = view };
                window.Show();
                Dispatcher.UIThread.RunJobs();
                bool wide = view.GetVisualDescendants().OfType<GridSplitter>().Any();
                window.Close();
                return wide;
            }

            Assert.True(SideBySide(byScreen: true));
            Assert.False(SideBySide(byScreen: false));
        }
        finally
        {
            Shell.WideByScreen = before;
        }
    }
}

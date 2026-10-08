using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using GroupLab.App;

namespace GroupLab.App.Tests;

/// <summary>
/// Error report issue 25 (nightly 176): a click with the hand tool where no hole, line or bull was near threw "Sequence contains no
/// elements" from the search for the nearest line, because that search ran over value tuples.
/// </summary>
public class CrashReport25Tests
{
    [AvaloniaFact]
    public void AClickNearNothingLightsNothingAndDoesNotThrow()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            var canvas = window.Canvas;
            canvas.Tool = MarkingTool.Pan;
            canvas.FitToView();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            Assert.NotEmpty(window.Session.State.Shots);

            // The canvas's own top left corner is the margin around the sheet, with nothing on it.
            var at = canvas.TranslatePoint(new Point(2, 2), window)!.Value;
            window.MouseDown(at, MouseButton.Left);
            window.MouseUp(at, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();

            // No bull is lit; the window may still keep a shot chosen in its list, which is its own business.
            Assert.Null(canvas.LitBull);
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}

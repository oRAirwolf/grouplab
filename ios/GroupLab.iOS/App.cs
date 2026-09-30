using GroupLab.App.Diagnostics;
using GroupLab.Mobile;

namespace GroupLab.iOS;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 290 section 2 item 3: the application. The start itself is the shared mobile project's
/// (<see cref="Phone.Start"/>), as on Android; this is iOS's part: its platform, its region and the system log.
/// </summary>
public sealed class App : Avalonia.Application
{
    public override void Initialize() => Phone.Initialize(this);

    public override void OnFrameworkInitializationCompleted()
    {
        bool selfTest = SelfTest.Asked();
        bool tour = !selfTest && SelfTest.TourAsked();
        if (selfTest || tour)
        {
            // The simulator's self-test answers the first run's questions with no, so the screens behind them can be opened.
            SelfTest.Prepare();
        }

        // Entry 290 section 6: the bar along the bottom reaches the bottom edge, under the home indicator, as an iPhone's bars do.
        Shell.BarToBottomEdge = true;

        // Entry 290 section 6: the result goes side by side on a wide screen by the screen's width, as iOS's size classes are read.
        Shell.WideByScreen = true;
        Phone.Start(new IosPhone(), this, IosPhone.Region, (level, line) =>
        {
            // The system log, which `xcrun simctl spawn booted log stream` and Console.app read; DEBUG lines never reach here.
            Console.WriteLine($"{Phone.LogTag} {level.ToString().ToUpperInvariant()} {line}");
        });

        // Entry 292 section 2: pictures opened in GroupLab or shared into it from another app, and whether the device is online.
        Connection.Start();
        IncomingPhotos.Listen(this);

        base.OnFrameworkInitializationCompleted();
        if (selfTest)
        {
            SelfTest.Start();
        }
        else if (tour)
        {
            SelfTest.StartTour();
        }
        else if (SelfTest.IdleAsked())
        {
            SelfTest.StartIdle();
        }
    }
}

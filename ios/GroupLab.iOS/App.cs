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
        if (selfTest)
        {
            // The simulator's self-test answers the first run's questions with no, so the screens behind them can be opened.
            SelfTest.Prepare();
        }

        Phone.Start(new IosPhone(), this, IosPhone.Region, (level, line) =>
        {
            // The system log, which `xcrun simctl spawn booted log stream` and Console.app read; DEBUG lines never reach here.
            Console.WriteLine($"{Phone.LogTag} {level.ToString().ToUpperInvariant()} {line}");
        });

        base.OnFrameworkInitializationCompleted();
        if (selfTest)
        {
            SelfTest.Start();
        }
    }
}

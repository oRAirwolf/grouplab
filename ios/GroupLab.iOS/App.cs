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

#if GROUPLAB_DEV
        // Entry 315 section 2, GroupLab Dev only: a scenario named by --scenario, or waiting in Documents/scenario, read before the start.
        bool scenario = !selfTest && !tour && GroupLab.Mobile.Dev.Scenario.Prepare(IosPhone.Documents, SelfTest.Value("--scenario"));
#endif

        // Entry 290 section 6: the bar along the bottom reaches the bottom edge, under the home indicator, as an iPhone's bars do.
        Shell.BarToBottomEdge = true;

        // Entry 290 section 6: the result goes side by side on a wide screen by the screen's width, as iOS's size classes are read.
        Shell.WideByScreen = true;

        // Entry 312 section 4: the iOS decimal pad has no minus sign, so a field that may be below zero keeps the full keyboard.
        Screens.NumberPadHasMinus = false;

        // Entry 350: Done and a tap outside a field ask UIKit itself to put the keyboard away, so the number pad goes even where taking the
        // focus did not: resignFirstResponder sent with no target reaches whatever holds the keyboard. (UIWindow's EndEditing is not in
        // these bindings, which kept nightlies 154 to 156 from building for the iPhone.)
        KeyboardRoom.HideSystemKeyboard = () =>
            UIKit.UIApplication.SharedApplication.SendAction(new ObjCRuntime.Selector("resignFirstResponder"), null, null, null);

        // Entry 315 section 4: the device's thermal state (nominal, fair, serious, critical) for the log and the diagnostics overlay.
        DeviceHealth.Heat = () => Foundation.NSProcessInfo.ProcessInfo.ThermalState.ToString().ToLowerInvariant();
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
#if GROUPLAB_DEV
        else if (scenario)
        {
            GroupLab.Mobile.Dev.Scenario.StartIfPrepared();
        }
#endif
    }
}

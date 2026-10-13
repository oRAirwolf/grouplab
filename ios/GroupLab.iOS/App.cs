using GroupLab.App.Diagnostics;
using GroupLab.Mobile;

namespace GroupLab.iOS;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 290 section 2 item 3: the application. The start itself is the shared mobile project's
/// (<see cref="Phone.Start"/>), as on Android; this is iOS's part: its platform, its region and the system log.
/// </summary>
public sealed class App : Avalonia.Application
{
    // Entry 415 section 1: the notice that the text size setting changed, held so it is heard for as long as GroupLab runs.
    private static Foundation.NSObject? textSizeChanged;

    public override void Initialize()
    {
        // Entry 415 section 1, Alan: the iPhone follows its own text size, as Android does (entry 411). Set before the styles are built,
        // so they are built at it, and again whenever Settings changes it while GroupLab is open, so nobody has to restart.
        GroupLab.App.Theme.TextSize.Follow(SystemTextSize);
        textSizeChanged ??= UIKit.UIApplication.Notifications.ObserveContentSizeCategoryChanged((_, _) =>
            Avalonia.Threading.Dispatcher.UIThread.Post(() => GroupLab.App.Theme.TextSize.Follow(SystemTextSize)));
        Phone.Initialize(this);
    }

    /// <summary>
    /// <paramref name="size"/> at the iPhone's text size setting (Dynamic Type): iOS's own factor for body text, which is the same for every
    /// size, from about 0.82 at the smallest to 1.35 at the largest ordinary size and 3.1 at the largest accessibility size, held at Android's
    /// largest so the screens the sweeps hold there hold here (<see cref="GroupLab.App.Theme.TextSize.ByFactor"/>). Read each time.
    /// </summary>
    internal static double SystemTextSize(double size) =>
        GroupLab.App.Theme.TextSize.ByFactor((double)UIKit.UIFontMetrics.DefaultMetrics.GetScaledValue(new System.Runtime.InteropServices.NFloat(100)) / 100)(size);

#if GROUPLAB_DEV
    /// <summary>Entry 353: the sheet presented over GroupLab Dev in any window, the photo picker above all; null where none is up.</summary>
    private static UIKit.UIViewController? Sheet()
    {
        foreach (var window in UIKit.UIApplication.SharedApplication.ConnectedScenes.ToArray().OfType<UIKit.UIWindowScene>().SelectMany(s => s.Windows))
        {
            var presented = window.RootViewController?.PresentedViewController;
            while (presented?.PresentedViewController is { } above)
            {
                presented = above;
            }

            if (presented is { IsBeingDismissed: false })
            {
                return presented;
            }
        }

        return null;
    }

#endif
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

        // Entry 353: the photo picker over the screen, which the real taps' scenario has to see and close, as the self-test closes it.
        // Looked for in every window of every scene, and one already going away is not counted, so a close is not repeated on it.
        GroupLab.Mobile.Dev.Scenario.SystemSheetUp = () => Sheet() is not null;
        GroupLab.Mobile.Dev.Scenario.CloseSystemSheet = () =>
        {
            if (Sheet() is not { } sheet)
            {
                return false;
            }

            bool canceled = PhotoPickers.CancelOpen();
            if (!canceled)
            {
                (sheet.PresentingViewController ?? sheet).DismissViewController(false, null);
            }

            DiagnosticLog.Info("scenario.sheet", ("closed", sheet.GetType().Name), ("asCancel", canceled));
            return true;
        };
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

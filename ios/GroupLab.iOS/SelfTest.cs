using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Marking;
using GroupLab.Mobile;

namespace GroupLab.iOS;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 290 section 2 item 4: the proof, on the iOS Simulator in CI, that the application runs. Started only by the
/// launch argument <c>--self-test</c> (or GROUPLAB_SELFTEST=1 in its environment), which nothing on a phone can pass. It opens every place
/// along the bottom and waits while the workflow photographs each one, checks the imaging on a rendered GroupLab sheet, analyzes the committed
/// sample scan through the phone's own pipeline, and then reads the same scan as a chosen picture through the Capture screen, and writes
/// what each check measured to Documents/selftest/results.json and the log. .github/workflows/ios-app.yml drives it and reads the file.
/// </summary>
internal static class SelfTest
{
    internal const string Argument = "--self-test";

    private static string Folder => Path.Combine(IosPhone.Documents, "selftest");

    /// <summary>The launch argument that shows the black idle screen (entry 268, entry 290 section 2 item 6), for a sitting on the iPad.</summary>
    internal const string IdleArgument = "--idle";

    /// <summary>
    /// Entry 290 section 6: the short tour for the iPad and for landscape, which nobody had photographed. Every place along the bottom and
    /// the sample's result, each photographed; with <see cref="LandscapeArgument"/> the screen is turned on its side first.
    /// </summary>
    internal const string TourArgument = "--screens";

    internal const string LandscapeArgument = "--landscape";

    internal static bool Asked() => Has(Argument) || Environment.GetEnvironmentVariable("GROUPLAB_SELFTEST") == "1";

    internal static bool TourAsked() => Has(TourArgument);

    internal static bool IdleAsked() => Has(IdleArgument);

    /// <summary>
    /// Whether the application was started with <paramref name="argument"/>: iOS gives the arguments of <c>xcrun simctl launch</c> to
    /// NSProcessInfo, and not always to Main; GROUPLAB_ARGUMENTS in the environment (SIMCTL_CHILD_GROUPLAB_ARGUMENTS) is read too.
    /// </summary>
    private static bool Has(string argument) =>
        Program.Arguments.Contains(argument) || Environment.GetCommandLineArgs().Contains(argument)
        || Foundation.NSProcessInfo.ProcessInfo.Arguments.Contains(argument)
        || (Environment.GetEnvironmentVariable("GROUPLAB_ARGUMENTS") ?? "").Split(' ').Contains(argument);

    /// <summary>
    /// A test sitting's idle screen: shown once the application is up, with the screen kept on only while it is showing, since the sitting
    /// watches it; once it is closed, iOS may dim and lock the screen again as it always does.
    /// </summary>
    internal static void StartIdle() => DispatcherTimer.RunOnce(() =>
    {
        Shell.Current?.ShowIdle();
        KeepAwakeWhile(() => Find<IdleScreen>() is not null);
    }, TimeSpan.FromSeconds(2));

    /// <summary>Keeps the screen from dimming while <paramref name="driving"/> holds, and gives the choice back to iOS the moment it does not.</summary>
    private static void KeepAwakeWhile(Func<bool> driving)
    {
        UIKit.UIApplication.SharedApplication.IdleTimerDisabled = true;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        timer.Tick += (_, _) =>
        {
            if (!driving())
            {
                UIKit.UIApplication.SharedApplication.IdleTimerDisabled = false;
                timer.Stop();
            }
        };
        timer.Start();
    }

    /// <summary>Before the start: the first run's questions answered no, imperial units, and the sample's caliber and distance typed in.</summary>
    internal static void Prepare()
    {
        Directory.CreateDirectory(Folder);
        File.Delete(Path.Combine(Folder, "results.json"));
        File.WriteAllText(Path.Combine(Folder, "status"), "starting");
        var store = new AppSettingsStore(Path.Combine(IosPhone.Documents, "settings.json"));
        store.SaveSending(GroupLab.Core.Publication.SendingChoice.Never, null);
        store.SaveErrorChoice(ErrorReportChoice.Never);
        store.SaveSurveyChoice(GroupLab.Core.Survey.SurveyChoice.No);
        store.SaveUnits(UnitSettings.Imperial);

        // Entry 294 section 1: the first run's "Is your scope in mil or MOA?", answered MOA with inches, as Imperial already is.
        store.SaveScopeAnswer(ScopeAnswer.Moa, GroupLab.Core.Marking.LinearUnit.Inch);
        store.SaveShotSetup(SelfTestChecks.SampleCalibre, SelfTestChecks.SampleDistanceInches);
    }

    internal static void Start() => _ = Task.Run(RunAsync);

    private static async Task RunAsync()
    {
        var checks = new List<SelfTestCheck>();
        double budget = 0;

        // The self-test is a sitting: the screen stays on while it runs, and only then.
        bool running = true;
        await OnUi(() => KeepAwakeWhile(() => running));
        try
        {
            Say("running");
            await WaitFor(() => Shell.Current is not null, TimeSpan.FromSeconds(60));
            await Task.Delay(TimeSpan.FromSeconds(3));

            // Every place along the bottom, in order, each photographed by the workflow before the next.
            int n = 1;
            foreach (var place in Enum.GetValues<Shell.Place>())
            {
                var check = await OnScreen(() =>
                {
                    Shell.Current!.Show(place);
                }, () => Shell.Current!.Showing == place, $"{n++:00}-{place.ToString().ToLowerInvariant()}", "screen " + place);
                checks.Add(check);
            }

            await OnUi(() => Shell.Current!.Show(Shell.Place.Capture));

            // Entry 290 section 2 item 5: the camera's place, which on the simulator is the files picker, pressed while the Capture screen
            // shows its start; and the camera's picture written as a JPEG.
            checks.Add(await CameraSelfTest.Fallback(80));
            checks.Add(await CameraSelfTest.Jpeg());

            // Entry 292 sections 2.1 and 2.2: Choose a photo opens the Photos picker and From another app opens Files; each Cancel comes back.
            checks.Add(await PhotosSelfTest.Picker(PhotoSource.Photos, "81-photos-picker"));
            checks.Add(await PhotosSelfTest.Picker(PhotoSource.OtherApp, "82-files-picker"));

            // Entry 290 section 2 item 6: a GroupLab PDF to iOS's share sheet and to its print sheet, each opened and closed.
            checks.Add(await SheetsSelfTest.Sheet(print: false, "83-share-sheet"));
            checks.Add(await SheetsSelfTest.Sheet(print: true, "84-print-sheet"));
            budget = Phone.Platform.MemoryBudgetMegabytes();
            checks.Add(new SelfTestCheck("opencv linked")
            {
                Passed = NativeOpenCv.Linked,
                Skipped = !NativeOpenCv.Linked,
                Detail = NativeOpenCv.Linked ? "OpenCV is linked into the application" : "this build was made without OpenCV for iOS, so the imaging checks are skipped",
            });
            if (NativeOpenCv.Linked)
            {
                checks.Add(SelfTestChecks.Run("native library", check =>
                {
                    var (handle, export) = NativeOpenCv.Probe();
                    check.Numbers["handle"] = handle ? 1 : 0;
                    check.Numbers["export"] = export ? 1 : 0;
                    string version = "";
                    try
                    {
                        version = OpenCvSharp.Cv2.GetVersionString() ?? "";
                    }
                    finally
                    {
                        check.Detail = $"the executable's handle {(handle ? "opened" : "did not open")}, core_Mat_new1 {(export ? "found" : "not found")} in it; "
                            + $"the resolver was asked {NativeOpenCv.Asked} times, last for {NativeOpenCv.LastAsked ?? "nothing"}";
                    }

                    check.Passed = handle && export && version.Length > 0;
                    check.Detail += "; OpenCV " + version;
                }));
                checks.AddRange(await Task.Run(SelfTestChecks.Imaging));
                string sample = Path.Combine(Folder, "sample.png");
                checks.Add(await Task.Run(() => SelfTestChecks.Pipeline(sample)));
                // Entry 290 section 2 item 6: a picture on the pasteboard, pressed in while Capture still shows its start.
                checks.Add(await SheetsSelfTest.Paste(sample, 59));
                checks.Add(await Chosen(sample, n));

                // Entry 313 section 1.5: Cancel pressed while the codes of a large picture are read brings the start back within a second.
                checks.Add(await SelfTestCancel.Run(sample, 62));

                // Entry 292 section 2.3: a picture opened in GroupLab from another app, and one shared into it, each read into analysis.
                checks.Add(await PhotosSelfTest.OpenIn(sample, 60));
                checks.Add(await PhotosSelfTest.Shared(sample, 61));

                // Entry 290 section 6: every feature the parity table had not seen on iOS, opened from the result and the places along the bottom.
                checks.AddRange(await ParityTour.Run(sample));

                // Entry 307: Your data exported to the share sheet, and the file opened in GroupLab again brings its import plan. After the
                // tour, which starts from the result this would leave behind.
                checks.Add(await DataSelfTest.RoundTrip());
            }

            // Entry 268 on iOS: the black idle screen over everything, as the --idle sitting shows it.
            checks.Add(await OnScreen(() => Shell.Current!.ShowIdle(), () => Find<IdleScreen>() is not null, "90-idle", "idle screen", words: false));
            checks.Add(await IdleLayers());
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            checks.Add(new SelfTestCheck("self-test") { Detail = SelfTestChecks.Describe(e) });
        }

        string results = SelfTestChecks.Json("ios", NativeOpenCv.Linked, budget, checks);
        File.WriteAllText(Path.Combine(Folder, "results.json"), results);
        foreach (var check in checks)
        {
            Console.WriteLine("GroupLab SELFTEST " + SelfTestChecks.Line(check));
            DiagnosticLog.Info("selftest.check", ("name", check.Name), ("passed", check.Passed), ("skipped", check.Skipped));
        }

        running = false;
        Say("done");
    }

    internal static void StartTour() => _ = Task.Run(TourAsync);

    /// <summary>The tour <see cref="TourArgument"/> asks for: the places along the bottom and a result, on whatever screen this is.</summary>
    private static async Task TourAsync()
    {
        var checks = new List<SelfTestCheck>();
        bool running = true;
        await OnUi(() => KeepAwakeWhile(() => running));
        try
        {
            Say("running");
            await WaitFor(() => Shell.Current is not null, TimeSpan.FromSeconds(60));
            await Task.Delay(TimeSpan.FromSeconds(3));
            if (Has(LandscapeArgument))
            {
                checks.Add(await Landscape());
            }

            checks.Add(await Screen());
            int n = 1;
            foreach (var place in Enum.GetValues<Shell.Place>())
            {
                checks.Add(await OnScreen(() => Shell.Current!.Show(place), () => Shell.Current!.Showing == place,
                    $"{n++:00}-{place.ToString().ToLowerInvariant()}", "screen " + place));
            }

            if (NativeOpenCv.Linked)
            {
                checks.Add(await Chosen(Path.Combine(Folder, "sample.png"), n));
            }
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            checks.Add(new SelfTestCheck("tour") { Detail = SelfTestChecks.Describe(e) });
        }

        File.WriteAllText(Path.Combine(Folder, "results.json"), SelfTestChecks.Json("ios", NativeOpenCv.Linked, 0, checks));
        foreach (var check in checks)
        {
            Console.WriteLine("GroupLab SELFTEST " + SelfTestChecks.Line(check));
        }

        running = false;
        Say("done");
    }

    /// <summary>Turns the screen on its side, as a person turning the phone would, and waits until the Shell is wider than it is tall.</summary>
    private static async Task<SelfTestCheck> Landscape()
    {
        var check = new SelfTestCheck("landscape");
        string? refused = null;
        static bool Turned() => Shell.Current is { } shell && shell.Bounds.Width > shell.Bounds.Height;

        // The workflow may have turned the simulator already, the one way an iPad showing apps in windows can be turned.
        if (await OnUi(Turned))
        {
            check.Passed = true;
            check.Detail = "the screen was already in landscape";
            return check;
        }

        await OnUi(() =>
        {
            var scene = UIKit.UIApplication.SharedApplication.ConnectedScenes.ToArray().OfType<UIKit.UIWindowScene>().FirstOrDefault();
            scene?.Windows.FirstOrDefault()?.RootViewController?.SetNeedsUpdateOfSupportedInterfaceOrientations();
            scene?.RequestGeometryUpdate(new UIKit.UIWindowSceneGeometryPreferencesIOS(UIKit.UIInterfaceOrientationMask.LandscapeRight),
                error => refused = error.LocalizedDescription);
        });
        check.Passed = await WaitFor(Turned, TimeSpan.FromSeconds(10));
        if (!check.Passed)
        {
            // An iPad showing apps in windows refuses the request; the device's own orientation is set instead, as a turned device sets it.
            await OnUi(() => UIKit.UIDevice.CurrentDevice.SetValueForKey(
                Foundation.NSNumber.FromInt32((int)UIKit.UIInterfaceOrientation.LandscapeRight), new Foundation.NSString("orientation")));
            check.Passed = await WaitFor(Turned, TimeSpan.FromSeconds(10));
        }

        await Task.Delay(TimeSpan.FromSeconds(2));
        check.Detail = check.Passed ? "the screen turned to landscape" : "the screen did not turn" + (refused is null ? "" : ": " + refused);
        return check;
    }

    /// <summary>The screen's size and safe area, so a screenshot can be read against them.</summary>
    private static Task<SelfTestCheck> Screen() => OnUi(() =>
    {
        var check = new SelfTestCheck("screen size") { Passed = true };
        var root = UIKit.UIApplication.SharedApplication.ConnectedScenes.ToArray().OfType<UIKit.UIWindowScene>()
            .SelectMany(w => w.Windows).FirstOrDefault()?.RootViewController?.View;
        var safe = root?.SafeAreaInsets ?? default;
        var shell = Shell.Current!;
        check.Numbers["width"] = Math.Round(shell.Bounds.Width);
        check.Numbers["height"] = Math.Round(shell.Bounds.Height);
        check.Detail = $"{UIKit.UIDevice.CurrentDevice.Model}, the Shell {shell.Bounds.Width:0}x{shell.Bounds.Height:0} with padding {shell.Padding}; "
            + $"safe area {safe.Top:0} top, {safe.Left:0} left, {safe.Bottom:0} bottom, {safe.Right:0} right";
        return check;
    });

    /// <summary>
    /// The sample read as a chosen picture: through the Capture screen's own path, the one the files picker feeds, to the picture check,
    /// then on to the result, each photographed; the saved session's shots and mean radius are what the check reads.
    /// </summary>
    private static async Task<SelfTestCheck> Chosen(string sample, int n)
    {
        var check = new SelfTestCheck("chosen picture");
        if (!File.Exists(sample))
        {
            check.Skipped = true;
            check.Detail = "the sample scan was not put in the application's files";
            return check;
        }

        // Handed over as the files picker hands a picture over (PhotoIntake.FromFilePicker): unread, with its size and kind. Followed to
        // the result as every picture handed over is (PhotosSelfTest.Analyzed), so a result left on screen by the check before is not taken
        // for this one's.
        var handle = new PhotoHandle(null, null, null, new FileInfo(sample).Length, Path.GetExtension(sample).ToLowerInvariant(),
            () => Task.FromResult<Stream?>(File.OpenRead(sample)));
        return await PhotosSelfTest.Analyzed(check, n, async () =>
        {
            await OnUi(() => CapturePage.SharedPicture?.Invoke([handle]));
            return null;
        });
    }

    /// <summary>
    /// Entry 290 section 2 item 6: what lies behind the idle screen, from the Shell up to the window, with each one's background and
    /// padding, so a strip left light behind the status bar or the home indicator can be traced; ios/selftest.py judges the screenshot.
    /// </summary>
    private static Task<SelfTestCheck> IdleLayers() => OnUi(() =>
    {
        var check = new SelfTestCheck("idle screen layers");
        var parts = new List<string>();
        for (Avalonia.Visual? v = Shell.Current; v is not null; v = v.GetVisualParent())
        {
            string background = v switch
            {
                Avalonia.Controls.Primitives.TemplatedControl t => t.Background?.ToString() ?? "none",
                Panel panel => panel.Background?.ToString() ?? "none",
                Border border => border.Background?.ToString() ?? "none",
                _ => "-",
            };
            string padding = v is Decorator d ? d.Padding.ToString() : v is Avalonia.Controls.Primitives.TemplatedControl tc ? tc.Padding.ToString() : "-";
            parts.Add($"{v.GetType().Name} {v.Bounds.Width:0}x{v.Bounds.Height:0} at {v.Bounds.X:0},{v.Bounds.Y:0}, background {background}, padding {padding}");
        }

        var window = UIKit.UIApplication.SharedApplication.ConnectedScenes.ToArray().OfType<UIKit.UIWindowScene>().SelectMany(w => w.Windows).FirstOrDefault();
        var root = window?.RootViewController?.View;
        parts.Add($"UIWindow {window?.Frame.Width:0}x{window?.Frame.Height:0}, background {window?.BackgroundColor?.ToString() ?? "none"}");
        parts.Add($"{root?.GetType().Name} {root?.Frame.Width:0}x{root?.Frame.Height:0} at {root?.Frame.Y:0}, background {root?.BackgroundColor?.ToString() ?? "none"}, safe area {root?.SafeAreaInsets.Top:0} top {root?.SafeAreaInsets.Bottom:0} bottom");
        check.Passed = true;
        check.Detail = string.Join(" | ", parts);
        return check;
    });

    /// <summary>Shows something, checks it is there, and waits while the workflow photographs it.</summary>
    private static async Task<SelfTestCheck> OnScreen(Action show, Func<bool> shown, string picture, string name, bool words = true)
    {
        var check = new SelfTestCheck(name);
        try
        {
            await OnUi(show);
            await Task.Delay(TimeSpan.FromSeconds(2));
            int texts = await OnUi(() => Shell.Current!.GetVisualDescendants().OfType<TextBlock>().Count(t => t.IsEffectivelyVisible && !string.IsNullOrWhiteSpace(t.Text)));
            bool there = await OnUi(shown);
            check.Numbers["texts"] = texts;
            bool taken = await Photographed(picture);
            check.Passed = there && (!words || texts > 5);
            check.Detail = $"{(there ? "opened" : "did not open")}, {texts} lines of text on screen, {(taken ? "photographed" : "not photographed")} as {picture}.png";
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            check.Detail = SelfTestChecks.Describe(e);
        }

        return check;
    }

    /// <summary>
    /// Asks the workflow for a screenshot by naming it in Documents/selftest/showing, and waits until it answers in Documents/selftest/taken.
    /// Without a workflow it goes on after a while, so a self-test started by hand still finishes.
    /// </summary>
    internal static async Task<bool> Photographed(string picture)
    {
        string taken = Path.Combine(Folder, "taken");
        await File.WriteAllTextAsync(Path.Combine(Folder, "showing"), picture);
        Console.WriteLine("GroupLab SELFTEST SHOWING " + picture);
        return await WaitFor(() => File.Exists(taken) && File.ReadAllText(taken).Trim() == picture, TimeSpan.FromSeconds(90), onUi: false);
    }

    private static void Say(string status) => File.WriteAllText(Path.Combine(Folder, "status"), status);

    /// <summary>The first control of a kind on screen; on the interface thread only.</summary>
    internal static T? Find<T>()
        where T : Control => Shell.Current?.GetVisualDescendants().OfType<T>().FirstOrDefault();

    internal static Task OnUi(Action action) => Dispatcher.UIThread.InvokeAsync(action).GetTask();

    internal static Task<T> OnUi<T>(Func<T> function) => Dispatcher.UIThread.InvokeAsync(function).GetTask();

    internal static async Task<bool> WaitFor(Func<bool> condition, TimeSpan most, bool onUi = true)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (clock.Elapsed < most)
        {
            if (onUi ? await OnUi(condition) : condition())
            {
                return true;
            }

            await Task.Delay(500);
        }

        return false;
    }
}

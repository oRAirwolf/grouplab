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

    internal static bool Asked() => Has(Argument) || Environment.GetEnvironmentVariable("GROUPLAB_SELFTEST") == "1";

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
            budget = Phone.Platform.MemoryBudgetMegabytes();
            checks.Add(new SelfTestCheck("opencv linked")
            {
                Passed = NativeOpenCv.Linked,
                Skipped = !NativeOpenCv.Linked,
                Detail = NativeOpenCv.Linked ? "OpenCV is linked into the application" : "this build was made without OpenCV for iOS, so the imaging checks are skipped",
            });
            if (NativeOpenCv.Linked)
            {
                checks.AddRange(await Task.Run(SelfTestChecks.Imaging));
                string sample = Path.Combine(Folder, "sample.png");
                checks.Add(await Task.Run(() => SelfTestChecks.Pipeline(sample)));
                checks.Add(await Chosen(sample, n));
            }

            // Entry 268 on iOS: the black idle screen over everything, as the --idle sitting shows it.
            checks.Add(await OnScreen(() => Shell.Current!.ShowIdle(), () => Find<IdleScreen>() is not null, "90-idle", "idle screen", words: false));
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            checks.Add(new SelfTestCheck("self-test") { Detail = e.GetType().Name + ": " + e.Message });
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

        try
        {
            long before = PhoneAnalysis.Store().List().Select(s => s.Id).DefaultIfEmpty(0).Max();
            string copy = Path.Combine(Phone.Platform.CacheFolder, "shared" + Path.GetExtension(sample));
            File.Copy(sample, copy, overwrite: true);
            await OnUi(() => CapturePage.SharedPicture?.Invoke(copy));
            bool checkShown = await WaitFor(() => Find<FeedbackView>() is not null || Find<ResultView>() is not null, TimeSpan.FromMinutes(5));
            check.Numbers["pictureCheckShown"] = checkShown ? 1 : 0;
            if (checkShown && await OnUi(() => Find<FeedbackView>() is not null))
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
                await Photographed($"{n++:00}-picture-check");
                await OnUi(() =>
                {
                    var use = Find<FeedbackView>()?.GetVisualDescendants().OfType<Button>()
                        .FirstOrDefault(b => b.Content is TextBlock { Text: "Use this picture" or "Use it anyway" });
                    use?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                });
            }

            bool resultShown = await WaitFor(() => Find<ResultView>() is not null, TimeSpan.FromSeconds(60));
            check.Numbers["resultShown"] = resultShown ? 1 : 0;
            if (resultShown)
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
                await Photographed($"{n++:00}-result");
            }

            var saved = PhoneAnalysis.Store().List().Where(s => s.Id > before).OrderBy(s => s.Id).LastOrDefault();
            check.Numbers["shots"] = saved?.ShotCount ?? 0;
            if (saved?.MeanRadiusInches is { } radius)
            {
                check.Numbers["meanRadius"] = Math.Round(radius, 5);
            }

            check.Passed = resultShown && saved is { ShotCount: SelfTestChecks.SampleShots };
            check.Detail = saved is null
                ? "no session was saved"
                : $"the chosen picture was analyzed and saved as {saved.SheetName}, {saved.ShotCount} shots, mean radius {saved.MeanRadiusInches:0.000} in; result shown: {resultShown}";
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            check.Detail = e.GetType().Name + ": " + e.Message;
        }

        return check;
    }

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
            check.Detail = e.GetType().Name + ": " + e.Message;
        }

        return check;
    }

    /// <summary>
    /// Asks the workflow for a screenshot by naming it in Documents/selftest/showing, and waits until it answers in Documents/selftest/taken.
    /// Without a workflow it goes on after a while, so a self-test started by hand still finishes.
    /// </summary>
    private static async Task<bool> Photographed(string picture)
    {
        string taken = Path.Combine(Folder, "taken");
        await File.WriteAllTextAsync(Path.Combine(Folder, "showing"), picture);
        Console.WriteLine("GroupLab SELFTEST SHOWING " + picture);
        return await WaitFor(() => File.Exists(taken) && File.ReadAllText(taken).Trim() == picture, TimeSpan.FromSeconds(90), onUi: false);
    }

    private static void Say(string status) => File.WriteAllText(Path.Combine(Folder, "status"), status);

    /// <summary>The first control of a kind on screen; on the interface thread only.</summary>
    private static T? Find<T>()
        where T : Control => Shell.Current?.GetVisualDescendants().OfType<T>().FirstOrDefault();

    private static Task OnUi(Action action) => Dispatcher.UIThread.InvokeAsync(action).GetTask();

    private static Task<T> OnUi<T>(Func<T> function) => Dispatcher.UIThread.InvokeAsync(function).GetTask();

    private static async Task<bool> WaitFor(Func<bool> condition, TimeSpan most, bool onUi = true)
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

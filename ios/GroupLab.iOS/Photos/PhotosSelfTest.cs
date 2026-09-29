using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Foundation;
using GroupLab.Mobile;
using PhotosUI;
using UIKit;

namespace GroupLab.iOS;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 292 section 2: what the simulator can prove of pictures from anywhere. Choose a photograph opens the Photos
/// picker and From another app opens Files, each without a crash, and each Cancel comes back with nothing; a picture opened in GroupLab from
/// another app ("Open in GroupLab") is read straight into analysis through the address iOS hands the application; and a picture shared from
/// another app, left in the app group exactly as the share extension leaves one, is read into analysis when iOS opens
/// <c>grouplab://shared</c>, opened through iOS as the extension opens it. Choosing a photograph in the pickers, a photograph kept only in
/// iCloud, and the share sheet itself need a person and a device (docs/IOS-PLAN.md, "The first TestFlight sitting").
/// </summary>
internal static class PhotosSelfTest
{
    /// <summary>A picker opened from the Capture screen's buttons' own call, photographed, and canceled as its Cancel does.</summary>
    internal static async Task<SelfTestCheck> Picker(PhotoSource source, string picture)
    {
        var check = new SelfTestCheck(source == PhotoSource.Photos ? "photos picker" : "files picker");
        try
        {
            var picking = await SelfTest.OnUi(() => Phone.Platform.PickPhotos(source, null));
            bool shown = await SelfTest.WaitFor(() => IosPhone.Top() is PHPickerViewController or UIDocumentPickerViewController, TimeSpan.FromSeconds(20));
            string kind = await SelfTest.OnUi(() => IosPhone.Top()?.GetType().Name ?? "nothing");
            bool right = kind == (source == PhotoSource.Photos ? nameof(PHPickerViewController) : nameof(UIDocumentPickerViewController));
            bool taken = shown && await SelfTest.Photographed(picture);
            bool canceled = await SelfTest.OnUi(PhotoPickers.CancelOpen);
            bool answered = await Task.WhenAny(picking, Task.Delay(TimeSpan.FromSeconds(10))) == picking;
            int chosen = answered ? (await picking).Count : -1;
            await SelfTest.WaitFor(() => IosPhone.Top() is not (PHPickerViewController or UIDocumentPickerViewController), TimeSpan.FromSeconds(10));
            bool alive = await SelfTest.OnUi(() => Shell.Current is not null);
            check.Numbers["shown"] = shown ? 1 : 0;
            check.Numbers["chosen"] = chosen;
            check.Passed = shown && right && canceled && answered && chosen == 0 && alive;
            check.Detail = $"{(shown ? kind + " opened" : "no picker opened")}, {(taken ? "photographed" : "not photographed")}; Cancel "
                + $"{(answered ? $"came back with {chosen} photographs" : "did not come back")}, and GroupLab {(alive ? "is still there" : "is gone")}";
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            check.Detail = SelfTestChecks.Describe(e);
        }

        return check;
    }

    /// <summary>
    /// "Open in GroupLab": the sample copied into Documents/Inbox, where iOS puts a picture opened in GroupLab, and its address handed to
    /// the application delegate as iOS hands it, which Avalonia turns into the activation GroupLab listens for.
    /// </summary>
    internal static async Task<SelfTestCheck> OpenIn(string sample, int n)
    {
        var check = new SelfTestCheck("open in GroupLab");
        if (!File.Exists(sample))
        {
            check.Skipped = true;
            check.Detail = "the sample scan was not put in the application's files";
            return check;
        }

        string inbox = Directory.CreateDirectory(Path.Combine(IosPhone.Documents, "Inbox")).FullName;
        string copy = Path.Combine(inbox, "open-in" + Path.GetExtension(sample));
        File.Copy(sample, copy, overwrite: true);
        return await Analyzed(check, n, async () =>
        {
            bool handled = await SelfTest.OnUi(() =>
            {
                var application = UIApplication.SharedApplication;
                using var options = new NSDictionary();
                return application.Delegate is AppDelegate app && app.OpenUrl(application, NSUrl.FromFilename(copy), options);
            });
            check.Numbers["handled"] = handled ? 1 : 0;
            check.Numbers["leftInInbox"] = File.Exists(copy) ? 1 : 0;
            return handled ? null : "the application delegate did not take the address";
        });
    }

    /// <summary>
    /// A picture shared from another app: the sample left in the app group exactly as the share extension leaves one, then
    /// <c>grouplab://shared</c> opened through iOS with the call the extension makes.
    /// </summary>
    internal static async Task<SelfTestCheck> Shared(string sample, int n)
    {
        var check = new SelfTestCheck("shared from another app");
        string? incoming = Handoff.Incoming();
        check.Numbers["appGroup"] = incoming is null ? 0 : 1;
        if (incoming is null)
        {
            check.Skipped = true;
            check.Detail = "the app group is not available to this build, so a share cannot be handed over here; the TestFlight sitting checks it";
            return check;
        }

        if (!File.Exists(sample))
        {
            check.Skipped = true;
            check.Detail = "the sample scan was not put in the application's files";
            return check;
        }

        string batch = Handoff.Begin(incoming);
        File.Copy(sample, Path.Combine(batch, Handoff.Name(0, Path.GetExtension(sample))));
        Handoff.Finish(batch);
        return await Analyzed(check, n, async () =>
        {
            // Opened through iOS by the call the extension makes, UIApplication's openURL. The simulator's own "simctl openurl" puts up
            // iOS's "Open in GroupLab?" question, which nothing here can answer; an application opening its own address is not asked.
            var opened = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await SelfTest.OnUi(() =>
            {
                using var address = new NSUrl(Handoff.Address);
                bool can = UIApplication.SharedApplication.CanOpenUrl(address);
                check.Numbers["canOpen"] = can ? 1 : 0;
                UIApplication.SharedApplication.OpenUrl(address, new UIApplicationOpenUrlOptions(), success => opened.TrySetResult(success));
            });
            Console.WriteLine("GroupLab SELFTEST OPENURL " + Handoff.Address);
            bool answered = await Task.WhenAny(opened.Task, Task.Delay(TimeSpan.FromSeconds(20))) == opened.Task;
            bool done = answered && await opened.Task;
            check.Numbers["openedByIos"] = done ? 1 : 0;
            return done ? null : "iOS did not open grouplab://shared";
        });
    }

    /// <summary>
    /// Hands a picture over by <paramref name="hand"/> and follows it through the picture check to the result, as the chosen picture check
    /// does; passed where a session with the sample's shots was saved. <paramref name="hand"/> returns why it could not, or null.
    /// </summary>
    private static async Task<SelfTestCheck> Analyzed(SelfTestCheck check, int n, Func<Task<string?>> hand)
    {
        try
        {
            await SelfTest.OnUi(() => Shell.Current!.Show(Shell.Place.Capture));
            long before = PhoneAnalysis.Store().List().Select(s => s.Id).DefaultIfEmpty(0).Max();

            // The result of the check before may still be on screen: only a picture check or a result made after the handing over counts.
            var (oldCheck, oldResult) = await SelfTest.OnUi(() => (SelfTest.Find<FeedbackView>(), SelfTest.Find<ResultView>()));
            bool NewCheck() => SelfTest.Find<FeedbackView>() is { } shown && !ReferenceEquals(shown, oldCheck);
            bool NewResult() => SelfTest.Find<ResultView>() is { } shown && !ReferenceEquals(shown, oldResult);
            if (await hand() is { } why)
            {
                check.Detail = why;
                return check;
            }

            bool checkShown = await SelfTest.WaitFor(() => NewCheck() || NewResult(), TimeSpan.FromMinutes(5));
            check.Numbers["pictureCheckShown"] = checkShown ? 1 : 0;
            if (checkShown && await SelfTest.OnUi(NewCheck))
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
                await SelfTest.OnUi(() =>
                {
                    var use = SelfTest.Find<FeedbackView>()?.GetVisualDescendants().OfType<Button>()
                        .FirstOrDefault(b => b.Content is TextBlock { Text: "Use this picture" or "Use it anyway" });
                    use?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                });
            }

            bool resultShown = await SelfTest.WaitFor(NewResult, TimeSpan.FromSeconds(60));
            check.Numbers["resultShown"] = resultShown ? 1 : 0;
            if (resultShown)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
                await SelfTest.Photographed($"{n:00}-{check.Name.Replace(' ', '-').ToLowerInvariant()}");
            }

            GroupLab.Core.Records.SessionSummary? saved = null;
            await SelfTest.WaitFor(() => (saved = PhoneAnalysis.Store().List().Where(s => s.Id > before).OrderBy(s => s.Id).LastOrDefault()) is not null,
                TimeSpan.FromSeconds(20), onUi: false);
            check.Numbers["shots"] = saved?.ShotCount ?? 0;
            check.Passed = resultShown && saved is { ShotCount: SelfTestChecks.SampleShots };
            check.Detail = saved is null
                ? $"no session was saved; the picture check {(checkShown ? "was" : "was not")} shown"
                : $"the picture was read into analysis and saved as {saved.SheetName}, {saved.ShotCount} shots, mean radius {saved.MeanRadiusInches:0.000} in; result shown: {resultShown}";
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            check.Detail = SelfTestChecks.Describe(e);
        }

        return check;
    }
}

using System.Globalization;
using Avalonia.Controls;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 219 item A4: the first place along the bottom. The caliber and the distance, remembered from the last
/// target, then take a picture with the capture screen or choose one already on the phone; either is analyzed at the working size, the
/// result shown, and saved in Sessions. The caliber comes first because it is the one answer that changes what GroupLab finds (entry 131
/// section 6.3), as on the desktop.
/// <para>
/// Entry 309 section 1, Alan: "Lets use A as the capture page and make it the default landing page." So the tab's start is Home A: the
/// GroupLab mark and word, one line saying what GroupLab does, one row with the caliber and distance remembered from the last target and
/// Change, Take a picture, Choose a photo and Print a target, the "Getting started on your phone" card, and grouplab.org and the version at
/// the foot. It does not ask every time: only where no caliber has been set yet does Take a picture or Choose a photo ask for it first, in a
/// sheet over the page, then go straight on.
/// </para>
/// </summary>
public sealed class CapturePage : UserControl
{
    /// <summary>Entry 309 section 1.3: the phone's part of the user guide, since grouplab.org has no page of its own for it.</summary>
    internal const string GettingStartedAddress = "https://grouplab.org/guides/user-guide/#13-on-the-phone";

    private readonly TextBlock setupSummary = Screens.Line("");
    private readonly Border setupFields;
    private readonly Button change = Screens.Choice("Change", () => { }).Id("capture-change");
    private readonly Border ask;
    private readonly StackPanel askFields = new() { Spacing = 8 };
    private readonly TextBlock askSaid = Screens.Line("");
    private Action? afterAsk;
    private readonly TextBlock status = Screens.Line("");
    private readonly AutoCompleteBox calibre = new AutoCompleteBox
    {
        ItemsSource = CartridgeTable.Suggest(""),
        FilterMode = AutoCompleteFilterMode.Contains,
        PlaceholderText = "Caliber, e.g. 6.5 Creedmoor or .308",
        MinHeight = Screens.Touch,
    }.Id("capture-caliber");

    private readonly TextBox distance = new TextBox { MinHeight = Screens.Touch, PlaceholderText = "Distance" }.Id("capture-distance");
    private readonly Control start;

    public CapturePage()
    {
        // Entry 258: a picture shared into GroupLab from another application is read as a chosen one; entry 292 section 1.3, several at
        // once are a set, one per sheet.
        SharedPicture = photos =>
        {
            Shell.Current?.Show(Shell.Place.Capture);
            _ = Opened(photos);
        };
#if GROUPLAB_DEV
        TestPicture = file => _ = Picked(file);
        TestCamera = manual =>
        {
            Shell.Current?.Show(Shell.Place.Capture);
            Phone.Settings.SaveCaptureManual(manual);
            Camera();
        };
#endif
        var units = Phone.Settings.LoadUnits();
        var (typed, inches) = Phone.Settings.LoadShotSetup();
        calibre.Text = typed ?? "";
        distance.PlaceholderText = $"Distance in {UnitSettings.Symbol(units.Distance)}";
        distance.Text = inches is { } d ? UnitSettings.DistanceFromInches(d, units.Distance).ToString("0.#", CultureInfo.CurrentCulture) : "";
        // Entry 309 section 1: Home A. The caliber and distance as one row, remembered, with Change opening the two fields beneath it.
        setupFields = Screens.Card(calibre, distance);
        setupFields.IsVisible = false;
        change.Click += (_, _) =>
        {
            if (setupFields.IsVisible && Setup() is null)
            {
                return;
            }

            setupFields.IsVisible = !setupFields.IsVisible;
            Summarize();
        };
        var setupRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 8 };
        var said = new StackPanel { Spacing = 2, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Children = { Screens.Dim("Caliber and distance"), setupSummary } };
        Grid.SetColumn(change, 1);
        setupRow.Children.Add(said);
        setupRow.Children.Add(change);
        Summarize();

        var choose = Screens.Choice("Choose a photo", () => AskFirst(() => _ = Choose())).Id("capture-choose-photo");
        var print = Screens.Choice("Print a target", () => Shell.Current?.Show(Shell.Place.Targets)).Id("capture-print-target");
        var pair = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = 8, Children = { choose, print } };
        Grid.SetColumn(print, 1);

        // Entry 292 sections 1.2 and 258: the other ways a picture comes in, kept one tap away under the two.
        var others = new WrapPanel
        {
            Children =
            {
                Link("From another app", () => AskFirst(() => _ = Choose(PhotoSource.OtherApp))),
                Link("Paste a picture", () => AskFirst(() => _ = Paste())),
            },
        };

        // Entry 233 and entry 271 section 4: the two things a good picture still needs, which the camera's live checks do not look for.
        var tips = MoreFold.Make(Phone.Settings, "capture.tips",
        [
            Screens.Dim("Shade the whole sheet or none of it: a shadow across part of it can hide a hole. Hold it down outside the printed area, because torn tape can look like one."),
            Screens.Dim(DetectionAdvice.OncePerPrinter + (Phone.Settings.LoadChosenPrinter() is { } printer
                ? string.Create(CultureInfo.CurrentCulture, $" Photographs are corrected for {printer.Name}'s {printer.Scale * 100:0.0} percent.")
                : "")),
        ], Screens.Touch);

        // Entry 313 section 1.1: a canceled picture, kept to read again or to choose its sheet.
        keptCard = Screens.Card(
            Screens.Line("Reading the last picture was stopped. It is kept until you take another."),
            Screens.Primary("Read it again", ReadKeptAgain),
            Screens.Choice("Choose which sheet it is", () => _ = ChooseKeptSheet()),
            Screens.Choice("Forget it", Forget));
        keptCard.IsVisible = false;

        var gettingStarted = Screens.Row("Getting started on your phone", "Print, shoot, photograph, read.", () => Phone.Platform.OpenAddress(GettingStartedAddress));
        start = Screens.Page(new StackPanel
        {
            Spacing = 12,
            Children =
            {
                new GroupLab.App.BrandMark { Lockup = true, Height = 36, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, Margin = new Avalonia.Thickness(0, 8, 0, 0) },
                Screens.Line("Photograph a target and read your group. On a GroupLab sheet, the scale and every hole are found for you."),
                keptCard,
                Screens.Card(setupRow, setupFields),
                Screens.Primary("Take a picture", () => AskFirst(Camera)).Id("capture-take-picture"),
                pair,
                others,
                status,
                Screens.Card(gettingStarted, Screens.Dim("Tips for a good picture"), tips),
                Link("grouplab.org", () => Phone.Platform.OpenAddress("https://grouplab.org/")),
                Screens.Quiet($"Free and open source · GPL-3.0 · {AppInfo.ShortVersion}"),
            },
        });

        // The question for a first caliber, in a sheet over the page (entry 309 section 1.2).
        ask = Screens.Card(
            Screens.Heading("Which caliber are you shooting?"),
            Screens.Dim("GroupLab asks once and remembers it for the next target. The distance may stay empty if you do not know it; both can be changed on the result."),
            askFields,
            askSaid,
            Screens.Primary("Continue", Continue).Id("capture-ask-continue"),
            Screens.Choice("Cancel", () => CloseAsk(false)).Id("capture-ask-cancel"));
        ask.IsVisible = false;
        ask.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom;
        ask.Margin = new Avalonia.Thickness(8);
        home = new Grid { Children = { start, ask } };
        Content = home;
    }

    /// <summary>The start with the question sheet over it.</summary>
    private readonly Grid home;

    private static Button Link(string words, Action chosen)
    {
        var link = new Button { Content = words, MinHeight = Screens.Touch, Margin = new Avalonia.Thickness(0, 0, 16, 0), Classes = { GroupLab.App.Theme.AppStyles.Link } };
        link.Click += (_, _) => chosen();
        return link;
    }

    /// <summary>The caliber and distance row as it reads: "6 ARC, 100 yd", with "distance not known" and "no caliber yet" where empty.</summary>
    private void Summarize()
    {
        string c = string.IsNullOrWhiteSpace(calibre.Text) ? "No caliber yet" : calibre.Text.Trim();
        string d = string.IsNullOrWhiteSpace(distance.Text) ? "distance not known" : $"{distance.Text.Trim()} {UnitSettings.Symbol(Phone.Settings.LoadUnits().Distance)}";
        setupSummary.Text = $"{c}, {d}";
        change.Content = setupFields.IsVisible ? "Done" : "Change";
    }

    /// <summary>Whether a caliber has been set, on this page or remembered from the last target.</summary>
    private bool HasCalibre()
    {
        if (string.IsNullOrWhiteSpace(calibre.Text) && Phone.Settings.LoadShotSetup().Calibre is { Length: > 0 } remembered)
        {
            calibre.Text = remembered;
        }

        return !string.IsNullOrWhiteSpace(calibre.Text);
    }

    /// <summary>Entry 309 section 1.2: straight on where a caliber is set; otherwise the question first, then straight on.</summary>
    internal void AskFirst(Action then)
    {
        if (HasCalibre())
        {
            then();
            return;
        }

        afterAsk = then;
        Move(askFields);
        ask.IsVisible = true;
        DiagnosticLog.Info("capture.ask", ("calibre", false));
    }

    /// <summary>Whether the question for a first caliber is showing, for the tests.</summary>
    internal bool Asking => ask.IsVisible;

    private void Continue()
    {
        if (!HasCalibre())
        {
            askSaid.Text = "Type the caliber, for example 6.5 Creedmoor or .308.";
            return;
        }

        if (Setup() is null)
        {
            // Why the caliber could not be read, said in the sheet, where it is being typed.
            askSaid.Text = status.Text;
            return;
        }

        askSaid.Text = "";

        CloseAsk(true);
    }

    private void CloseAsk(bool goOn)
    {
        ask.IsVisible = false;
        Move((Panel)setupFields.Child!);
        Summarize();
        var then = afterAsk;
        afterAsk = null;
        if (goOn)
        {
            then?.Invoke();
        }
    }

    /// <summary>The two fields, into the question sheet or back onto the page.</summary>
    private void Move(Panel to)
    {
        foreach (var field in new Control[] { calibre, distance })
        {
            (field.Parent as Panel)?.Children.Remove(field);
            to.Children.Add(field);
        }
    }

    /// <summary>The caliber and distance as typed, remembered for the next target; null where the caliber cannot be read, with why.</summary>
    private ShotSetup? Setup()
    {
        var units = Phone.Settings.LoadUnits();
        var chosen = Calibre.Parse(calibre.Text, out string? why);
        if (why is not null)
        {
            status.Text = why;
            return null;
        }

        double? inches = double.TryParse(distance.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out double value) && value > 0
            ? UnitSettings.DistanceToInches(value, units.Distance)
            : null;
        Phone.Settings.SaveShotSetup(calibre.Text, inches);
        return new ShotSetup(chosen, inches);
    }

    /// <summary>The last result on this tab, which the Result button returns to (entry 281 section 1.3).</summary>
    private Control? lastResult;

    /// <summary>
    /// Entry 281 section 1.3: Capture and the result as two buttons always in view above the page, so the camera and the last result are
    /// each one press away and the result never hides the way back to the camera behind a scroll. The page under them scrolls on its own.
    /// </summary>
    private DockPanel WithBar(Control page, bool onResult)
    {
        if (page.Parent is Panel old)
        {
            old.Children.Remove(page);
        }

        var camera = Screens.Choice("Camera", Camera).Id("capture-show-camera");
        var result = Screens.Choice("Result", () =>
        {
            if (lastResult is { } shown)
            {
                Content = WithBar(shown, true);
            }
        }).Id("capture-show-result");
        // Entry 291 section 2.3: the one showing looks selected, in the primary color, and never greyed out as if it could not be pressed.
        result.IsEnabled = lastResult is not null;
        (onResult ? result : camera).Classes.Add(PhoneStyles.Primary);
        Avalonia.Automation.AutomationProperties.SetHelpText(onResult ? result : camera, "Showing now");
        var bar = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = 8, Margin = new Avalonia.Thickness(12, 8, 12, 4) };
        Grid.SetColumn(result, 1);
        bar.Children.Add(camera);
        bar.Children.Add(result);
        var dock = new DockPanel();
        DockPanel.SetDock(bar, Dock.Top);
        dock.Children.Add(bar);
        dock.Children.Add(page);
        return dock;
    }

    /// <summary>The start of the tab, with the two buttons above it once there is a result to go back to.</summary>
    private void ShowStart() => Content = lastResult is null ? home : WithBar(home, false);

    /// <summary>A result shown, and kept as the one the Result button returns to.</summary>
    internal void ShowResult(Control result)
    {
        lastResult = result;
        Content = WithBar(result, true);
        DiagnosticLog.Info("camera.shutter", ("step", "shown")); // entry 283: the last step, timed by the log's own clock
    }

    private void Camera()
    {
        if (Setup() is not { } setup)
        {
            return;
        }

        if (!Phone.Platform.CameraAllowed())
        {
            status.Text = "GroupLab needs the camera to take the picture. Allow it, then press Take a picture again.";
            return;
        }

        // Entry 260, Capture B: the camera fills the screen and the bar along the bottom is hidden while it does; Back, Android's back and
        // leaving for a photograph all bring them back.
        Shell.Current?.Immersive(true);
        Content = Phone.Platform.Camera((path, torch) => _ = Analyze(path, setup, torch), () => CloseCamera(), () =>
        {
            CloseCamera();
            _ = Choose();
        }, lastResult is null ? null : () =>
        {
            CloseCamera();
            if (lastResult is { } shown)
            {
                Content = WithBar(shown, true);
            }
        });
    }

    /// <summary>Whether the camera is showing; closes it and returns to the start where it was.</summary>
    internal bool CloseCamera()
    {
        if (!Phone.Platform.IsCamera(Content))
        {
            return false;
        }

        Shell.Current?.Immersive(false);
        ShowStart();
        return true;
    }

#if GROUPLAB_DEV
    /// <summary>
    /// Entry 246, GroupLab Dev only: a picture in the application's own files, named by a test over adb, read exactly as a chosen photograph
    /// is, so a device can be measured without the system's picker, which would show the owner's own pictures.
    /// </summary>
    internal static Action<string>? TestPicture { get; private set; }

    /// <summary>Entry 260, GroupLab Dev only: opens the camera, in Manual where asked, for the device check.</summary>
    internal static Action<bool>? TestCamera { get; private set; }

    private async Task Picked(string file)
    {
        if (Setup() is not { } setup || !File.Exists(file))
        {
            DiagnosticLog.Info("phone.test.picture", ("found", File.Exists(file)));
            return;
        }

        string copy = Path.Combine(Phone.Platform.CacheFolder, "chosen" + Path.GetExtension(file));
        File.Copy(file, copy, overwrite: true);
        await Analyze(copy, setup);
    }
#endif

    /// <summary>Entry 258: set by the Capture page, for pictures shared into GroupLab from another application or opened with it.</summary>
    internal static Action<IReadOnlyList<PhotoHandle>>? SharedPicture { get; private set; }

    /// <summary>
    /// Pictures that arrived from another application, fetched with a progress line (entry 292 section 1.4) and read as chosen photographs
    /// with the caliber and distance as typed.
    /// </summary>
    private async Task Opened(IReadOnlyList<PhotoHandle> photos)
    {
        if (Setup() is not { } setup)
        {
            return;
        }

        await AnalyzeAll(await PhotoPages.Read(this, photos, "shared", words => status.Text = words), setup);
    }

    /// <summary>
    /// Entry 258: a picture copied in another application, pasted, the phone's version of pasting an image into the desktop's window. The
    /// clipboard holds its content address; the picture is copied into the cache and read as a chosen one.
    /// </summary>
    private async Task Paste()
    {
        if (Setup() is not { } setup)
        {
            return;
        }

        if (await Phone.Platform.PastePicture(Phone.Platform.CacheFolder) is not { } copy)
        {
            status.Text = "There is no picture to paste. Copy one in another app first, then press Paste a picture.";
            return;
        }

        DiagnosticLog.Info("phone.paste", ("type", Path.GetExtension(copy)));
        await Analyze(copy, setup);
    }

    /// <summary>Entry 292 section 1.1: Choose a photograph opens the system photo picker, or the apps that offer pictures where there is none.</summary>
    private Task Choose() => Choose(PhotoSource.Photos);

    private async Task Choose(PhotoSource source)
    {
        if (Setup() is not { } setup)
        {
            return;
        }

        // The picker hands a content address, not a path; the picture is copied into the cache, analyzed, and the copy deleted.
        status.Text = "";
        await AnalyzeAll(await PhotoPages.Pick(this, source, "chosen", words => status.Text = words), setup);
    }

    /// <summary>
    /// Entry 292 section 1.3: several pictures at once are a set, one per sheet. Each is read and saved in turn and the last one's result
    /// shown, which leads to the set where they are its sheets. A reduced copy is said to be one before it is read (section 1.4).
    /// </summary>
    private async Task AnalyzeAll(IReadOnlyList<PickedPhoto> photos, ShotSetup setup)
    {
        for (int i = 0; i < photos.Count; i++)
        {
            if (photos[i].Reduced is { } words && !await PhotoPages.UseReduced(this, words))
            {
                PhotoPages.Forget(photos.Skip(i));
                status.Text = "Share the photo into GroupLab from the app that keeps it, or download it to the phone and choose it again.";
                return;
            }

            if (i == photos.Count - 1)
            {
                await Analyze(photos[i].Path, setup);
            }
            else if (!await ReadAhead(photos[i].Path, setup, i, photos.Count))
            {
                PhotoPages.Forget(photos.Skip(i + 1));
                return;
            }
        }
    }

    /// <summary>One sheet of several read and saved without showing its result, with its own progress and Cancel; false where canceled.</summary>
    private async Task<bool> ReadAhead(string photo, ShotSetup setup, int index, int count)
    {
        // Entry 313 section 1: a Cancel that always works and a time limit, as for one picture.
        var reading = new Reading("phone.set.read");
        var (page, line, stop) = Screens.Progress(string.Create(CultureInfo.CurrentCulture, $"Reading sheet {index + 1} of {count}"));
        stop.Click += (_, _) =>
        {
            reading.Stop();
            line.Text = "Canceling…";
        };
        var before = Content;
        Content = page;
        var units = Phone.Settings.LoadUnits();
        ReadOutcome<PhoneResult> outcome;
        using (Phone.Platform.KeepRunning("Reading the sheet"))
        {
            outcome = await reading.Run(token => PhoneAnalysis.Run(photo, setup, units, Phone.Survey, token, words => Said(reading, line, words)), Reading.Limit, Late);
        }

        Delete(photo);
        switch (outcome.End)
        {
            case ReadEnd.Canceled:
                Content = before;
                return false;
            case ReadEnd.Done:
                DiagnosticLog.Info("phone.set.read", ("sheet", index + 1), ("of", count), ("found", outcome.Value!.Definition is not null));
                return true;
            default:
                // Timed out or failed, which the log has: the set goes on with its next sheet, as it did after an error.
                DiagnosticLog.Info("phone.set.read", ("sheet", index + 1), ("of", count), ("ended", outcome.End));
                return true;
        }
    }

    /// <summary>A reading's progress line, left saying "Canceling…" once Cancel is pressed.</summary>
    private static void Said(Reading reading, TextBlock line, string words) => Dispatcher.UIThread.Post(() =>
    {
        if (!reading.Stopping)
        {
            line.Text = words;
        }
    });

    /// <summary>A result that arrived after its reading was stopped: nobody will see it, so its session and working copy go.</summary>
    private static void Late(PhoneResult? late)
    {
        if (late is null)
        {
            return;
        }

        if (late.SessionId is { } id)
        {
            PhoneAnalysis.Store().Delete(id);
        }

        PhoneAnalysis.Discard(late.Image);
    }

    /// <summary>A picture deleted, where nothing else still has it open.</summary>
    private static void Delete(string photo)
    {
        try
        {
            File.Delete(photo);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            DiagnosticLog.Info("phone.photo.delete", ("error", e.GetType().Name));
        }
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 313 section 1.1: the picture whose reading was canceled, kept on the start with a line saying so, to
    /// read again or to choose its sheet, until another picture is taken or it is forgotten.
    /// </summary>
    private sealed record KeptPicture(string Path, bool Torch);

    private KeptPicture? kept;

    /// <summary>The kept picture's card on the start; hidden while there is none.</summary>
    private readonly Border keptCard;

    /// <summary>Whether a canceled picture is being kept, for the tests and the self-test.</summary>
    internal bool KeepingPicture => kept is not null;

    /// <summary>Whether the start of the tab is what shows, with or without the two buttons above it.</summary>
    internal bool AtStart => ReferenceEquals(Content, home) || (Content is DockPanel dock && dock.Children.Contains(home));

    private void Keep(string photo, bool torch)
    {
        Forget();
        kept = new KeptPicture(photo, torch);
        keptCard.IsVisible = true;
    }

    /// <summary>The kept picture let go of, and deleted.</summary>
    private void Forget()
    {
        if (kept is { } old)
        {
            Delete(old.Path);
        }

        kept = null;
        keptCard.IsVisible = false;
    }

    /// <summary>The kept picture read again from the start, with the caliber and distance as they are now.</summary>
    private void ReadKeptAgain()
    {
        if (kept is not { } picture || Setup() is not { } setup)
        {
            return;
        }

        kept = null;
        keptCard.IsVisible = false;
        _ = Analyze(picture.Path, setup, picture.Torch);
    }

    /// <summary>The kept picture offered with the sheets to choose from, its codes not read again (entry 313 section 1.2).</summary>
    private async Task ChooseKeptSheet()
    {
        if (kept is not { } picture || Setup() is not { } setup)
        {
            return;
        }

        kept = null;
        keptCard.IsVisible = false;
        await Unread(picture.Path, picture.Torch, setup, "Choose which sheet it is.");
    }

    /// <summary>A picture prepared without reading its codes and shown with the sheets to choose from and <paramref name="said"/>.</summary>
    private async Task Unread(string photo, bool torch, ShotSetup setup, string said)
    {
        var reading = new Reading("phone.unread");
        var (page, _, stop) = Screens.Progress("Getting the picture ready");
        stop.Click += (_, _) => reading.Stop();
        Content = page;
        var units = Phone.Settings.LoadUnits();
        var outcome = await reading.Run(_ => PhoneAnalysis.Unread(photo, said), Reading.Limit, Late);
        if (outcome.End is ReadEnd.Canceled or ReadEnd.TimedOut)
        {
            Keep(photo, torch);
            ShowStart();
            return;
        }

        Delete(photo);
        var result = outcome.Value ?? new PhoneResult(MarkingState.Empty, null, "The picture could not be read: " + outcome.Error?.Message, null);
        ShowResult(new ResultView(result, setup, units, ShowStart));
    }

    private async Task Analyze(string photo, ShotSetup setup, bool torch = false)
    {
        // A new picture replaces a kept one; reading the kept one again is this picture.
        if (kept is { } old && old.Path != photo)
        {
            Forget();
        }

        // Entry 313 section 1: Cancel always works, at once, from any stage, and a reading has a time limit. The reading runs on a pool
        // thread; Cancel returns to the start with the picture kept and leaves the reading to stop at its next check.
        var reading = new Reading("phone.detect");
        var (page, line, stop) = Screens.Progress("Reading the sheet");
        stop.Click += (_, _) =>
        {
            reading.Stop();
            line.Text = "Canceling…";
        };
        Shell.Current?.Immersive(false);
        Content = page;
        var units = Phone.Settings.LoadUnits();
        string? onCodes = GroupLab.Core.Trace.StageWords.During("S0.identify");
        bool readingCodes = false;
        ReadOutcome<PhoneResult> outcome;
        using (Phone.Platform.KeepRunning("Reading the sheet"))
        {
            outcome = await reading.Run(token => PhoneAnalysis.Run(photo, setup, units, Phone.Survey, token, words =>
            {
                readingCodes = words == onCodes;
                Said(reading, line, words);
            }, torch), Reading.Limit, Late);
        }

        PhoneResult result;
        switch (outcome.End)
        {
            case ReadEnd.Canceled:
                // Entry 243 section 3.2 kept nothing from a canceled reading; entry 313 section 1.1 keeps the picture, to read again or to
                // choose its sheet. The working copy goes with the reading when it stops.
                DiagnosticLog.Info("phone.detect.cancel", ("ms", (long)outcome.Took.TotalMilliseconds));
                Keep(photo, torch);
                ShowStart();
                return;
            case ReadEnd.TimedOut:
                // Entry 313 section 1.2: stopped, and said what was tried, with the sheets to choose from.
                await Unread(photo, torch, setup, readingCodes
                    ? "The codes could not be read in time. Try again closer, or choose the sheet."
                    : "The picture could not be read in time. Try again closer and in even light, or choose the sheet.");
                return;
            case ReadEnd.Failed:
                // Every error ends the reading with a message; the log and the error report have where it happened.
                result = new PhoneResult(MarkingState.Empty, null, "The picture could not be analyzed: " + outcome.Error!.Message, null);
                break;
            default:
                result = outcome.Value!;
                break;
        }

        // The photograph itself is never kept: the session keeps its working copy.
        Delete(photo);

        // Entry 260, Feedback B: every picture is checked before its result, taken or chosen; taking it again forgets this one.
        void Show() => ShowResult(new ResultView(result, setup, units, ShowStart));
        DiagnosticLog.Info("camera.shutter", ("step", "analyzed"));
        Dispatcher.UIThread.Post(() => Content = result.Check is { } check
            ? new FeedbackView(check, result.Image?.Path, Show, () =>
            {
                if (result.SessionId is { } id)
                {
                    PhoneAnalysis.Store().Delete(id);
                }

                PhoneAnalysis.Discard(result.Image);
                DiagnosticLog.Info("phone.retake", ("score", check.Score));
                Camera();
            }, result.State.ViewQuarterTurns)
            : WithResult(new ResultView(result, setup, units, ShowStart)));

        Control WithResult(ResultView view)
        {
            DiagnosticLog.Info("camera.shutter", ("step", "shown"));
            lastResult = view;
            return WithBar(view, true);
        }
    }
}

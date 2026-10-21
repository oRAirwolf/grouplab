using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Detection;

namespace GroupLab.Mobile;

/// <summary>What Next does on a step for whoever would rather not do it themselves.</summary>
internal enum DemoAct
{
    None,

    /// <summary>Opens the sample sheet as a chosen picture, from the application's own files, and waits for its result.</summary>
    OpenSample,

    /// <summary>Presses the control the step outlines.</summary>
    Press,

    /// <summary>The page's own way back, to the screen before it.</summary>
    Back,
}

/// <summary>
/// One step of the phone's guided demo, NOTES-FROM-PLANNING.md entry 446 (entry 439 section 5): the place along the bottom it is on, the
/// control it outlines (its automation id, as the scenarios name it), what it says, and what Next does.
/// </summary>
internal sealed record PhoneDemoStep(Shell.Place Place, string Control, string Title, string Says, DemoAct Act = DemoAct.None);

/// <summary>
/// Entry 446: the desktop's guided demo on the phone's own screens. Each step outlines the real control with a numbered badge and a card
/// that says what it is; Next does the step for whoever would rather not, and the card's close button or the phone's Back ends it.
/// Nothing the demo makes is kept unless the last step's box is ticked.
/// </summary>
internal sealed class PhoneDemo
{
    /// <summary>
    /// The tour, in order, as data. The scenario step "demo" and the headless tests walk every step and fail when one names a control that is
    /// not on its screen, so a renamed button breaks a test rather than the tour.
    /// </summary>
    internal static IReadOnlyList<PhoneDemoStep> Steps { get; } =
    [
        new(Shell.Place.Capture, "capture-choose-photo", "A sample sheet",
            "On the range you photograph your sheet, or choose a picture you already have here. For this tour GroupLab has made a sample sheet with 25 shots, so it is nobody's target. Next opens it.",
            DemoAct.OpenSample),
        new(Shell.Place.Capture, "result-plot", "GroupLab reads it by itself",
            "The codes in the corners say which sheet it is and how big. GroupLab found every hole, gave each one to its bull, and drew every shot on one bull from its own aim point."),
        new(Shell.Place.Capture, "result-figures", "The group's figures",
            "Each figure comes with the range it could really be. Tap a number to switch between inches, MOA and mils; a section opens to say why."),
        new(Shell.Place.Capture, "result-fix-holes", "Fix a hole",
            "Anything GroupLab is unsure of is shown, never guessed at quietly. Fix holes moves, adds or removes a hole under a crosshair, with zoom and undo."),
        new(Shell.Place.Capture, "result-shots", "Shots",
            "Every shot's offset and clicks. Next opens the list.", DemoAct.Press),
        new(Shell.Place.Capture, "shots-table", "Each shot",
            "Tap a shot to call it a flyer, leave it out of the figures with your reason, or delete it as not a hole. Next goes back to the result.", DemoAct.Back),
        new(Shell.Place.Capture, "result-zero", "Zero from this group",
            "How far the group's center is from your aim, in your scope's clicks. When the shots cannot tell a click from luck, it says to leave it, and why."),
        new(Shell.Place.Capture, "result-share-picture", "Share a picture",
            "The target with its results box and the mean radius circle, to save or send. Next opens it.", DemoAct.Press),
        new(Shell.Place.Capture, "share-picture", "Your picture, your choice",
            "Drag the box anywhere and tap it to choose its lines. Nothing leaves the phone until you press Save to gallery or Share. Next goes back.", DemoAct.Back),
        new(Shell.Place.Sessions, "sessions-list", "Sessions",
            "Every target you analyze is kept here by its load, date and time, saved as you go, to open again or compare loads."),
        new(Shell.Place.Ballistics, "ballistics-tabs", "Ballistics",
            "The dope table, the trajectory, and the chance of a hit on a plate or a ring at your distance, from your own group and rifle."),
        new(Shell.Place.Targets, "targets-optic", "Targets",
            "Every sheet GroupLab prints, and one made for your optic: say how far and the magnification, and its bulls are sized to center on. The tour stops here: printing is always your own press."),
        new(Shell.Place.Settings, "settings-demo", "Where to get help",
            "The user guide and glossary are on grouplab.org, and Settings sends GroupLab's maker what went wrong. You can take this tour again here."),
    ];

    private readonly Shell shell;
    private readonly Border spot = new() { IsHitTestVisible = false, Classes = { AppStyles.DemoHalo } };
    // The badge grows with the phone's text size, so its number is never cut off at the largest.
    private readonly Border badge = new() { IsHitTestVisible = false, MinWidth = 26, MinHeight = 26, Padding = new Thickness(6, 1), Classes = { AppStyles.DemoBadge } };
    private readonly TextBlock badgeText = new() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.DemoBadgeText } };
    private readonly Border card = new() { Classes = { PhoneStyles.Card } };
    // Entry 465 item 6A, as on the desktop: a small diamond in the card's own colours, half behind its edge, pointing at the outline.
    private readonly Border pointer = new()
    {
        Width = 14,
        Height = 14,
        Padding = default,
        CornerRadius = new CornerRadius(2),
        IsHitTestVisible = false,
        IsVisible = false,
        RenderTransform = new RotateTransform(45),
        RenderTransformOrigin = RelativePoint.Center,
        Classes = { PhoneStyles.Card },
    };
    private readonly ScrollViewer cardScroll = new() { HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
    private readonly TextBlock count = Screens.Quiet("");
    private readonly TextBlock title = Screens.Heading("");
    private readonly TextBlock says = Screens.Line("");
    private readonly CheckBox keep = new() { Content = new TextBlock { Text = DemoWords.Keep, TextWrapping = TextWrapping.Wrap }, MinHeight = Screens.Touch };
    private readonly Button next;
    private readonly Border offer = new() { IsVisible = false, Classes = { PhoneStyles.Card } };
    private int at;
    private string? sample;
    private HashSet<ResultView> before = [];
    private string? calibreBefore;

    internal PhoneDemo(Shell shell)
    {
        this.shell = shell;
        spot.Child = new Border { Classes = { AppStyles.DemoSpot } };
        badge.Child = badgeText;
        next = Screens.Primary(DemoWords.Next, () => _ = Next()).Id("demo-next");
        var end = Screens.Choice(DemoWords.End, () => End(keep: false)).Id("demo-end");
        cardScroll.Content = new StackPanel { Spacing = 10, Children = { count, title, says, keep, next, end } };
        card.Child = cardScroll;
        card.Id("demo-card");
        Layer.Children.Add(spot);
        Layer.Children.Add(badge);
        Layer.Children.Add(pointer);
        Layer.Children.Add(card);
        offer.Child = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                Screens.Heading("A guided demo"),
                Screens.Line(DemoWords.OfferSays),
                Screens.Primary(DemoWords.Offer, () =>
                {
                    CloseOffer();
                    _ = Start();
                }).Id("demo-offer-take"),
                Screens.Choice(DemoWords.NotNow, CloseOffer).Id("demo-offer-not-now"),
            },
        };
        offer.Id("demo-offer");
        Layer.Children.Add(offer);
        // The outline follows its control as the screen lays out, scrolls or turns; the rest of the screen stays usable around the card.
        shell.LayoutUpdated += (_, _) => Place();
    }

    /// <summary>The layer over the page area: the outline, the badge and the card. It takes no taps but the card's own.</summary>
    internal Canvas Layer { get; } = new() { IsVisible = false };

    internal bool Running => Layer.IsVisible && card.IsVisible;

    internal bool OfferShown => Layer.IsVisible && offer.IsVisible;

    internal int At => at;

    /// <summary>The control the step showing outlines, or none when it is not on the screen: what the headless walk checks.</summary>
    internal Control? Spotlit { get; private set; }

    internal CheckBox Keep => keep;

    internal Border Card => card;

    /// <summary>The sample's picture while the demo has it.</summary>
    internal string? SamplePath => sample;

    /// <summary>
    /// Entry 446 section 1: from the start, or from the step a tour was left at, doing the steps before it for the person so the screen is
    /// where that step expects.
    /// </summary>
    internal async Task Start(int from = 0)
    {
        if (Running)
        {
            return;
        }

        Phone.Settings.SaveDemoOffered();
        before = [.. shell.Targets.All];
        // The start of Capture, where Choose a photo is, with any target already open kept open.
        shell.AnotherTarget();
        offer.IsVisible = false;
        keep.IsChecked = false;
        card.IsVisible = true;
        Layer.IsVisible = true;
        at = 0;
        DiagnosticLog.Info("demo.start", ("from", from + 1));
        for (int i = 0; i < Math.Clamp(from, 0, Steps.Count - 1); i++)
        {
            Show(i);
            if (!await Do(i).ConfigureAwait(true))
            {
                End(keep: false);
                return;
            }
        }

        Show(Math.Clamp(from, 0, Steps.Count - 1));
    }

    /// <summary>Next: the step's own action, for whoever has not done it, then the next step; on the last, the end.</summary>
    internal async Task Next()
    {
        if (!Running)
        {
            return;
        }

        if (at == Steps.Count - 1)
        {
            End(keep: keep.IsChecked == true, finished: true);
            return;
        }

        next.IsEnabled = false;
        bool done = await Do(at).ConfigureAwait(true);
        next.IsEnabled = true;
        if (!Running)
        {
            return;
        }

        if (!done)
        {
            End(keep: false);
            return;
        }

        Show(at + 1);
    }

    private async Task<bool> Do(int index)
    {
        var step = Steps[index];
        switch (step.Act)
        {
            case DemoAct.OpenSample:
                return await OpenSample().ConfigureAwait(true);
            case DemoAct.Press:
                if (Find(step.Control) is Button button)
                {
                    button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                }

                return true;
            case DemoAct.Back:
                shell.WayBack()?.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                return true;
            default:
                return true;
        }
    }

    private void Show(int index)
    {
        at = index;
        var step = Steps[index];
        Phone.Settings.SaveDemoStep(index);
        if (shell.Showing != step.Place)
        {
            shell.Show(step.Place);
        }

        string of = DemoWords.Of(index + 1, Steps.Count);
        count.Text = of;
        title.Text = step.Title;
        says.Text = step.Says;
        badgeText.Text = (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        bool last = index == Steps.Count - 1;
        keep.IsVisible = last;
        ((TextBlock)next.Content!).Text = last ? DemoWords.Finish : DemoWords.Next;
        // A screen reader hears the step, with its number, as the card's name, and again on Next, which takes the focus.
        AutomationProperties.SetName(card, $"{of}: {step.Title}. {step.Says}");
        AutomationProperties.SetHelpText(next, $"{of}: {step.Title}. {step.Says}");
        DiagnosticLog.Info("demo.step", ("step", index + 1));
        Spotlit = Find(step.Control);
        Spotlit?.BringIntoView();
        cardScroll.Offset = default;
        Place();
        next.Focus();
    }

    /// <summary>The control named on show on the screen, never one of the demo's own.</summary>
    private Control? Find(string id) =>
        shell.GetVisualDescendants().OfType<Control>().FirstOrDefault(c => c.IsEffectivelyVisible && !Layer.IsVisualAncestorOf(c)
            && AutomationProperties.GetAutomationId(c) == id);

    /// <summary>
    /// Entry 437 section 3's marking around the real control, as on the desktop: an amber outline with a halo and the step's number in a badge
    /// on its top left corner. The card goes below the control where it fits, else above, else into the larger room left, where it scrolls,
    /// so at the largest text size it is all still there and never covers its own outline.
    /// </summary>
    private void Place()
    {
        double width = Layer.Bounds.Width, height = Layer.Bounds.Height;
        if (width < 1 || height < 1)
        {
            return;
        }

        double margin = Tokens.Space12, cardWidth = Math.Min(width - 2 * margin, 440);
        if (OfferShown)
        {
            Set(pointer, Visual.IsVisibleProperty, false);
            Set(offer, Layoutable.WidthProperty, cardWidth);
            Set(offer, Canvas.LeftProperty, (width - cardWidth) / 2);
            Set(offer, Canvas.TopProperty, Math.Max(margin, (height - offer.Bounds.Height) / 3));
            return;
        }

        if (!Running)
        {
            return;
        }

        // A control laid out only after its screen was shown is found on a later pass, and brought into view then.
        if (Spotlit is null && Find(Steps[at].Control) is { } found)
        {
            Spotlit = found;
            found.BringIntoView();
        }

        var target = Spotlit is { } c && c.IsEffectivelyVisible && c.Bounds.Width > 1 && c.TranslatePoint(default, Layer) is { } point
            ? new Rect(point, c.Bounds.Size).Intersect(new Rect(0, 0, width, height)) : (Rect?)null;
        if (target is { Width: < 1 } or { Height: < 1 })
        {
            target = null;
        }

        Set(spot, Visual.IsVisibleProperty, target is not null);
        Set(badge, Visual.IsVisibleProperty, target is not null);
        Rect? outlined = null;
        Set(card, Layoutable.WidthProperty, cardWidth);
        // The words' own height as the last layout found it inside the card's scroll, so nothing is measured here: measuring the card's
        // words again on every pass, against the scroll's own measuring, laid the whole screen out again for ever.
        var frame = card.Padding + card.BorderThickness;
        double want = cardScroll.Extent.Height + frame.Top + frame.Bottom;
        double x = (width - cardWidth) / 2, room = height - 2 * margin, y = height - margin - Math.Min(want, room);
        if (target is { } r)
        {
            var ring = r.Inflate(4);
            outlined = ring;
            Set(spot, Canvas.LeftProperty, ring.X);
            Set(spot, Canvas.TopProperty, ring.Y);
            Set(spot, Layoutable.WidthProperty, ring.Width);
            Set(spot, Layoutable.HeightProperty, ring.Height);
            double bw = Math.Max(26, badge.Bounds.Width), bh = Math.Max(26, badge.Bounds.Height);
            Set(badge, Canvas.LeftProperty, Math.Clamp(ring.X - bw / 2, 0, Math.Max(0, width - bw)));
            Set(badge, Canvas.TopProperty, Math.Clamp(ring.Y - bh / 2, 0, Math.Max(0, height - bh)));
            double below = height - margin - (ring.Bottom + margin), above = ring.Y - 2 * margin;
            if (want <= below)
            {
                y = ring.Bottom + margin;
                room = below;
            }
            else if (want <= above)
            {
                y = ring.Y - margin - want;
                room = above;
            }
            else if (below >= above)
            {
                y = ring.Bottom + margin;
                room = Math.Max(below, 3 * Screens.Touch);
            }
            else
            {
                room = Math.Max(above, 3 * Screens.Touch);
                y = Math.Max(margin, ring.Y - margin - Math.Min(want, room));
            }
        }

        double tall = Math.Max(3 * Screens.Touch, room);
        double top = Math.Clamp(y, margin, Math.Max(margin, height - margin - Math.Min(want, tall)));
        Set(card, Layoutable.MaxHeightProperty, tall);
        Set(card, Canvas.LeftProperty, x);
        Set(card, Canvas.TopProperty, top);

        // The pointer, on the card's edge toward the outline: the top where the card is below it, the bottom where it is above, and none
        // where the card, scrolling at the largest text, has had to sit over part of it.
        var placed = new Rect(x, top, cardWidth, Math.Min(want, tall));
        var tip = outlined is { } part
            ? DemoCardPlacement.PointerFor(placed, part, placed.Y >= part.Bottom ? DemoCardSide.Below : DemoCardSide.Above)
            : null;
        Set(pointer, Visual.IsVisibleProperty, tip is not null);
        if (tip is { } tipAt)
        {
            Set(pointer, Canvas.LeftProperty, tipAt.X - pointer.Width / 2);
            Set(pointer, Canvas.TopProperty, tipAt.Y - pointer.Height / 2);
        }
    }

    /// <summary>The card's pointer, for the headless walk.</summary>
    internal Border Pointer => pointer;

    /// <summary>A value set only where it changes, so placing the card never asks for another layout of its own accord.</summary>
    private static void Set(AvaloniaObject on, AvaloniaProperty property, object value)
    {
        object? was = on.GetValue(property);
        if (was is double before && value is double after ? !(Math.Abs(before - after) < 0.5) : !Equals(was, value))
        {
            on.SetValue(property, value);
        }
    }

    /// <summary>
    /// Entry 446 section 1: the sample, made by GroupLab (<see cref="DemoSample"/>, the desktop's sheet), written into the application's own
    /// files and opened as a chosen picture is, never from the camera; then its result, waited for.
    /// </summary>
    private async Task<bool> OpenSample()
    {
        string folder = Path.Combine(PhoneAnalysis.Files, "demo");
        Directory.CreateDirectory(folder);
        sample = Path.Combine(folder, DemoSample.FileName);
        PhoneAnalysis.Library();
        string sheet = Path.Combine(PhoneAnalysis.Files, "targets", DemoSample.SheetFile);
        if (!File.Exists(sheet) || CapturePage.SharedPicture is not { } shared)
        {
            DiagnosticLog.Info("demo.sample", ("ready", false));
            return false;
        }

        if (GroupLab.Core.Gltd.Json.GltdJsonReader.ReadFile(sheet).Definition is not { } definition)
        {
            return false;
        }

        await Task.Run(() =>
        {
            var image = DemoSample.Make(definition);
            using var mat = OpenCvSharp.Mat.FromPixelData(image.Height, image.Width, OpenCvSharp.MatType.CV_8UC1, image.Pixels);
            OpenCvSharp.Cv2.ImWrite(sample, mat);
        }).ConfigureAwait(true);

        // The demo names the caliber, as the desktop's does, so no question stops it; the person's own is put back when the result shows.
        calibreBefore = null;
        if (CapturePage.Latest is { } capture)
        {
            calibreBefore = capture.TypedCalibre;
            capture.TypedCalibre = ".308 Winchester";
        }

        string path = sample;
        shared([new PhotoHandle(null, null, null, new FileInfo(path).Length, ".png", () => Task.FromResult<Stream?>(File.OpenRead(path)))]);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (clock.Elapsed < TimeSpan.FromSeconds(180) && Running)
        {
            if (shell.Targets.All.Any(t => !before.Contains(t)) && Find("result-plot") is not null)
            {
                RestoreCalibre();
                return true;
            }

            // A picture check before the result, where one shows, is answered as the person would to see the result.
            if (shell.GetVisualDescendants().OfType<FeedbackView>().FirstOrDefault() is { } check)
            {
                check.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => b.Content is TextBlock { Text: "Use this picture" or "Use it anyway" })
                    ?.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            }

            await Task.Delay(200).ConfigureAwait(true);
        }

        RestoreCalibre();
        DiagnosticLog.Info("demo.sample", ("ready", false), ("seconds", (int)clock.Elapsed.TotalSeconds));
        return false;
    }

    private void RestoreCalibre()
    {
        if (calibreBefore is not null && CapturePage.Latest is { } capture)
        {
            capture.TypedCalibre = calibreBefore;
        }

        calibreBefore = null;
    }

    /// <summary>
    /// Entry 446 section 3: nothing the demo made is kept unless the person chose to keep it: its target closes with nothing asked, the
    /// sessions it saved and their pictures are removed, and so is the sample.
    /// </summary>
    internal void End(bool keep, bool finished = false)
    {
        if (!Running)
        {
            return;
        }

        Layer.IsVisible = false;
        card.IsVisible = false;
        Spotlit = null;
        RestoreCalibre();
        Phone.Settings.SaveDemoStep(finished ? 0 : at);
        DiagnosticLog.Info("demo.end", ("step", at + 1), ("kept", keep));
        var made = shell.Targets.All.Where(t => !before.Contains(t)).ToList();
        if (keep)
        {
            shell.Show(Shell.Place.Sessions);
            shell.Toast(DemoWords.KeptOnPhone);
            return;
        }

        var store = PhoneAnalysis.Store();
        foreach (var view in made)
        {
            long? id = view.SessionId;
            shell.CloseTarget(view);
            if (id is { } session)
            {
                string? picture = store.Get(session)?.ImagePath;
                store.Delete(session);
                if (picture is not null && Path.GetDirectoryName(picture) is { } folder && folder.StartsWith(PhoneAnalysis.SessionsFolder, StringComparison.Ordinal))
                {
                    try
                    {
                        Directory.Delete(folder, recursive: true);
                    }
                    catch (IOException)
                    {
                        // A picture still held open is left for the next start, which clears the sessions folder of what no session names.
                    }
                }
            }
        }

        try
        {
            if (sample is not null && File.Exists(sample))
            {
                File.Delete(sample);
            }
        }
        catch (IOException)
        {
            // Written over the next time the demo starts.
        }

        sample = null;
        shell.Show(Shell.Place.Capture);
        shell.Toast(DemoWords.Ended);
    }

    /// <summary>Entry 446 section 1: offered once, after the first run's questions, whatever the answer; Settings offers it any time.</summary>
    internal void OfferIfDue()
    {
        if (Phone.Settings.LoadDemoOffered())
        {
            return;
        }

        Phone.Settings.SaveDemoOffered();
        card.IsVisible = spot.IsVisible = badge.IsVisible = false;
        offer.IsVisible = true;
        Layer.IsVisible = true;
        Place();
    }

    private void CloseOffer()
    {
        offer.IsVisible = false;
        if (!card.IsVisible)
        {
            Layer.IsVisible = false;
        }
    }

    /// <summary>Entry 446 section 1: the Settings card, from the start, and from the step a tour was left at when there is one.</summary>
    internal static Control SettingsCard(Shell? shell)
    {
        int left = Phone.Settings.LoadDemoStep();
        var column = new StackPanel { Spacing = 8 };
        column.Children.Add(Screens.Primary(DemoWords.SettingsRow, () => _ = shell?.Demo.Start()).Id("settings-demo"));
        if (left > 0)
        {
            column.Children.Add(Screens.Choice(DemoWords.CarryOn(left + 1, Steps.Count), () => _ = shell?.Demo.Start(left)).Id("settings-demo-carry-on"));
        }

        column.Children.Add(Screens.Dim(DemoWords.SettingsSays));
        return Screens.Card(column);
    }
}

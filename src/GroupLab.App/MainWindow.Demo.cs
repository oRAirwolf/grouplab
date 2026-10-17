using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;

namespace GroupLab.App;

/// <summary>
/// One step of the guided demo, NOTES-FROM-PLANNING.md entry 439 section 4.1: the screen it is on, the control it points at (named as
/// <see cref="ControlNames"/> names it), what it says, and what Next does for the person who would rather not do it themselves.
/// </summary>
internal sealed record DemoStep(Destination Screen, string Control, string Title, string Says, Func<MainWindow, Task>? Do = null);

/// <summary>The demo's words, in one place.</summary>
internal static class DemoWords
{
    public const string TabName = "Demo: sample sheet";
    public const string Offer = "Walk me through a demo (about five minutes)";
    public const string OfferSays = "GroupLab can show you how it works on a sample sheet it makes for the tour, nobody's target: reading it, the analysis, " +
        "printing, ballistics and where to get help. Nothing the demo makes is kept unless you ask.";
    public const string NotNow = "Not now";
    public const string SettingsRow = "Walk me through a demo";
    public const string SettingsSays = "A guided tour of GroupLab on a sample sheet, about five minutes. You can end it at any step.";
    public const string Back = "Back";
    public const string Next = "Next";
    public const string Finish = "Finish";
    public const string End = "End the tour";
    public const string Keep = "Keep this demo in Sessions";
    public const string Ended = "The demo has ended, and what it made has been removed.";
    public const string Kept = "The demo has ended. Its session is kept in Session records.";

    public static string Of(int step, int count) => $"{step} of {count}";

    public static string CarryOn(int step, int count) => $"Carry on from step {step} of {count}";
}

public sealed partial class MainWindow
{
    /// <summary>
    /// Entry 439 section 2: the tour, in order, as data. Each step names a control the person can find on the screen it shows; a test walks
    /// every step headless and fails when one names a control that is not there, so a renamed button breaks a test rather than the tour.
    /// </summary>
    internal static IReadOnlyList<DemoStep> DemoSteps { get; } =
    [
        new(Destination.Analyse, "Picture", "A sample sheet",
            "This is a GroupLab sheet with 25 shots, made by GroupLab for this tour, so it is nobody's target. It has opened in a tab of its own."),
        new(Destination.Analyse, "StatusLine", "GroupLab reads it by itself",
            "The codes in the corners say which sheet it is and how big. GroupLab found every hole and gave each one to its bull. This line says what it did."),
        new(Destination.Analyse, "Next unsure hole", "The review queue",
            "Anything GroupLab is unsure of waits for you, never guessed at quietly. Next unsure hole shows the next one to settle.",
            w => { w.NextUnsureHole(); return Task.CompletedTask; }),
        new(Destination.Analyse, "CalibreBox", "Name the caliber",
            "Tell GroupLab the caliber if you know it. It then knows how big a hole should be, and measures from each bullet's center.",
            w => { w.Session.SetCalibre(Calibre.Of(0.308)); w.CalibreAnswered(); return Task.CompletedTask; }),
        new(Destination.Analyse, "Accept and analyze", "Accept and analyze",
            "When the marks look right, Accept and analyze saves the sheet as a session and shows the analysis.",
            w => { if (!w.Analysing) { w.Analyse(); } return Task.CompletedTask; }),
        new(Destination.Analyse, "Plot", "The group, and its figures",
            "Every shot is drawn on one bull from its own aim point, with the group's center and circles. The figures beside it stay in view.",
            w => { w.SetEveryWhy(true); return Task.CompletedTask; }),
        new(Destination.Analyse, "Show work", "Each figure, with its range",
            "Every figure comes with the range it could really be, and its why is one click away. The statistics guide on grouplab.org explains them from the beginning.",
            w => { w.SetEveryWhy(null); return Task.CompletedTask; }),
        new(Destination.Analyse, "Plot", "Click a hole",
            "Click a hole on the plot, or its row in the shot table, to edit it: move it, call it a flyer, or leave it out of the figures, with your reason.",
            w => { w.PickShots([]); return Task.CompletedTask; }),
        new(Destination.Analyse, "ZeroCorrection", "The zero correction",
            "How far the group's center is from your aim, in your scope's clicks. When the shots cannot tell a click from luck, it says to leave it, and why."),
        new(Destination.Sessions, "Session records", "Sessions and the report",
            "Every accepted sheet is kept here by its load, date and time. The analysis's Report button makes one dated page to print or share."),
        new(Destination.Library, "Print…", "Print a sheet",
            "Targets holds every sheet. Check your printer once, then print at actual size. The tour stops here: printing is always your own press."),
        new(Destination.Ballistics, "BallisticViewHit", "Ballistics",
            "The dope table, and the chance of a hit on a plate or a ring at your distance, worked out from your own group and rifle."),
        new(Destination.Settings, "Report a problem…", "Where to get help",
            "The user guide and glossary are on grouplab.org, and Report a problem sends the developer what went wrong. You can take this tour again from Settings."),
    ];

    private readonly Canvas demoLayer = new() { IsVisible = false };
    private readonly Border demoSpot = new() { IsHitTestVisible = false, Classes = { AppStyles.DemoHalo } };
    private readonly Border demoBadge = new() { IsHitTestVisible = false, Width = 22, Height = 22, Classes = { AppStyles.DemoBadge } };
    private readonly TextBlock demoBadgeText = new() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.DemoBadgeText } };
    private readonly Border demoCard = new() { Width = 360, Classes = { AppStyles.ProblemCard } };
    private readonly TextBlock demoTitle = new() { TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Title } };
    private readonly TextBlock demoSays = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock demoCount = new() { Classes = { AppStyles.Faint } };
    private readonly Button demoBack = new() { Content = DemoWords.Back };
    private readonly Button demoNext = new() { Content = DemoWords.Next, Classes = { AppStyles.Primary } };
    private readonly Button demoEnd = new() { Content = DemoWords.End, Classes = { AppStyles.Secondary } };
    private readonly CheckBox demoKeep = new() { Content = DemoWords.Keep };
    private readonly Border demoOffer = new() { Width = 440, IsVisible = false, Classes = { AppStyles.ProblemCard } };
    private Button? demoCarryOn;
    private int demoAt;
    private string? demoPath;
    private HashSet<long> demoSessionsBefore = [];

    internal bool DemoRunning => demoLayer.IsVisible && demoCard.IsVisible;

    /// <summary>Whether the first run's offer of the demo is showing.</summary>
    internal bool DemoOfferShown => demoLayer.IsVisible && demoOffer.IsVisible;

    internal int DemoAt => demoAt;

    /// <summary>The control the step showing points at, or none when it is not on the screen: what the headless walk checks.</summary>
    internal Control? DemoSpotlit { get; private set; }

    internal CheckBox DemoKeep => demoKeep;

    private Canvas BuildDemoLayer()
    {
        demoSpot.Child = new Border { Classes = { AppStyles.DemoSpot } };
        demoBadge.Child = demoBadgeText;
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = Tokens.Space8, Children = { demoBack, demoNext } };
        var foot = new DockPanel { LastChildFill = false };
        DockPanel.SetDock(demoEnd, Dock.Right);
        foot.Children.Add(buttons);
        foot.Children.Add(demoEnd);
        demoCard.Child = new StackPanel { Spacing = Tokens.Space12, Children = { demoCount, demoTitle, demoSays, demoKeep, foot } };
        demoBack.Click += async (_, _) => await DemoBack().ConfigureAwait(true);
        demoNext.Click += async (_, _) => await DemoNext().ConfigureAwait(true);
        demoEnd.Click += (_, _) => EndDemo(keep: false);
        demoLayer.Children.Add(demoSpot);
        demoLayer.Children.Add(demoBadge);
        demoLayer.Children.Add(demoCard);
        var take = new Button { Content = DemoWords.Offer, Classes = { AppStyles.Primary } };
        var notNow = new Button { Content = DemoWords.NotNow, Classes = { AppStyles.Secondary } };
        take.Click += async (_, _) =>
        {
            CloseDemoOffer();
            await StartDemo().ConfigureAwait(true);
        };
        notNow.Click += (_, _) => CloseDemoOffer();
        demoOffer.Child = new StackPanel
        {
            Spacing = Tokens.Space12,
            Children =
            {
                new TextBlock { Text = "A guided demo", Classes = { AppStyles.Title } },
                new TextBlock { Text = DemoWords.OfferSays, TextWrapping = TextWrapping.Wrap },
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = Tokens.Space8, Children = { take, notNow } },
            },
        };
        demoLayer.Children.Add(demoOffer);
        // The spotlight follows its control as the window lays out, scrolls or resizes; the rest of the window stays usable.
        LayoutUpdated += (_, _) => PlaceDemo();
        AddHandler(KeyDownEvent, (_, e) =>
        {
            if (e.Key == Key.Escape && DemoOfferShown)
            {
                CloseDemoOffer();
                e.Handled = true;
            }
            else if (DemoRunning && e.Key == Key.Escape)
            {
                EndDemo(keep: false);
                e.Handled = true;
            }
        }, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        return demoLayer;
    }

    /// <summary>
    /// Entry 439 sections 1 and 2: opens the sample in a tab of its own and shows the first step, or the step a tour was left at, doing the
    /// steps before it for the person so the screen is where that step expects.
    /// </summary>
    internal async Task StartDemo(int at = 0)
    {
        if (DemoRunning || sessions is null)
        {
            return;
        }

        settingsStore.SaveDemoOffered();
        demoSessionsBefore = [.. sessions.List().Select(s => s.Id)];
        demoPath = WriteDemoSheet();
        if (TabHoldsATarget && AddTarget(choose: false) is null)
        {
            return;
        }

        targets.Current.Demo = true;
        if (!await OpenImageSafely(demoPath).ConfigureAwait(true) || !await QuietWithin().ConfigureAwait(true))
        {
            EndDemo(keep: false);
            return;
        }

        // The demo reads its sheet even where opening a picture does not (the headless tests turn that off), since step 2 is about it.
        if (session.State.Shots.Count == 0)
        {
            await Detect(automatic: true).ConfigureAwait(true);
        }

        UpdateCurrentTab();
        demoKeep.IsChecked = false;
        demoOffer.IsVisible = false;
        demoCard.IsVisible = true;
        demoLayer.IsVisible = true;
        demoAt = 0;
        for (int i = 0; i < Math.Clamp(at, 0, DemoSteps.Count - 1); i++)
        {
            await DoDemoStep(i).ConfigureAwait(true);
        }

        ShowDemoStep(Math.Clamp(at, 0, DemoSteps.Count - 1));
    }

    /// <summary>Next: the step's own action, for whoever has not done it, then the next step; on the last, the end.</summary>
    internal async Task DemoNext()
    {
        if (!DemoRunning)
        {
            return;
        }

        if (demoAt == DemoSteps.Count - 1)
        {
            EndDemo(keep: demoKeep.IsChecked == true, finished: true);
            return;
        }

        await DoDemoStep(demoAt).ConfigureAwait(true);
        ShowDemoStep(demoAt + 1);
    }

    internal Task DemoBack()
    {
        if (DemoRunning && demoAt > 0)
        {
            ShowDemoStep(demoAt - 1);
        }

        return Task.CompletedTask;
    }

    private async Task DoDemoStep(int index)
    {
        if (DemoSteps[index].Do is { } act)
        {
            Go(DemoSteps[index].Screen);
            await act(this).ConfigureAwait(true);
        }
    }

    private void ShowDemoStep(int index)
    {
        demoAt = index;
        var step = DemoSteps[index];
        settingsStore.SaveDemoStep(index);
        if (destination != step.Screen)
        {
            Go(step.Screen);
        }

        demoCount.Text = DemoWords.Of(index + 1, DemoSteps.Count);
        demoTitle.Text = step.Title;
        demoSays.Text = step.Says;
        demoBadgeText.Text = (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        bool last = index == DemoSteps.Count - 1;
        demoKeep.IsVisible = last;
        demoNext.Content = last ? DemoWords.Finish : DemoWords.Next;
        demoBack.IsEnabled = index > 0;
        // A screen reader hears the step as the card's name when Next, which takes the focus, is announced.
        Avalonia.Automation.AutomationProperties.SetName(demoCard, $"{DemoWords.Of(index + 1, DemoSteps.Count)}: {step.Title}. {step.Says}");
        Avalonia.Automation.AutomationProperties.SetHelpText(demoNext, $"{step.Title}. {step.Says}");
        DiagnosticLog.Info("demo.step", ("step", index + 1));
        DemoSpotlit = FindDemoControl(step.Control);
        DemoSpotlit?.BringIntoView();
        PlaceDemo();
        demoNext.Focus();
    }

    /// <summary>The named control on show in the window, never one of the demo's own.</summary>
    private Control? FindDemoControl(string name) =>
        this.GetVisualDescendants().OfType<Control>().FirstOrDefault(c => c.IsEffectivelyVisible && c.Opacity > 0 && !demoLayer.IsVisualAncestorOf(c)
            && ControlNames.Of(c) == name);

    /// <summary>
    /// Entry 437 section 3's marking, around the real control: an amber outline with a halo, and the step's number in a badge on its top left
    /// corner. The card sits below the control where there is room, else above, else beside it, so it never covers its own spotlight.
    /// </summary>
    private void PlaceDemo()
    {
        if (DemoOfferShown)
        {
            demoOffer.Measure(Size.Infinity);
            Avalonia.Controls.Canvas.SetLeft(demoOffer, Math.Max(Tokens.Space12, (demoLayer.Bounds.Width - demoOffer.DesiredSize.Width) / 2));
            Avalonia.Controls.Canvas.SetTop(demoOffer, Math.Max(Tokens.Space12, (demoLayer.Bounds.Height - demoOffer.DesiredSize.Height) / 3));
            return;
        }

        if (!DemoRunning)
        {
            return;
        }

        double width = demoLayer.Bounds.Width, height = demoLayer.Bounds.Height;
        // A control laid out only after its screen was shown is found on the next pass.
        DemoSpotlit ??= FindDemoControl(DemoSteps[demoAt].Control);
        demoCard.Measure(Size.Infinity);
        var card = demoCard.DesiredSize;
        var target = DemoSpotlit is { } c && c.IsEffectivelyVisible && c.Bounds.Width > 1 && c.TranslatePoint(default, demoLayer) is { } at
            ? new Rect(at, c.Bounds.Size) : (Rect?)null;
        demoSpot.IsVisible = demoBadge.IsVisible = target is not null;
        double x = (width - card.Width) / 2, y = Tokens.Space24 * 3;
        if (target is { } r)
        {
            var ring = r.Inflate(4);
            Avalonia.Controls.Canvas.SetLeft(demoSpot, ring.X);
            Avalonia.Controls.Canvas.SetTop(demoSpot, ring.Y);
            demoSpot.Width = ring.Width;
            demoSpot.Height = ring.Height;
            Avalonia.Controls.Canvas.SetLeft(demoBadge, Math.Clamp(ring.X - 11, 0, Math.Max(0, width - 22)));
            Avalonia.Controls.Canvas.SetTop(demoBadge, Math.Clamp(ring.Y - 11, 0, Math.Max(0, height - 22)));
            if (ring.Bottom + Tokens.Space12 + card.Height <= height)
            {
                y = ring.Bottom + Tokens.Space12;
                x = ring.X;
            }
            else if (ring.Y - Tokens.Space12 - card.Height >= 0)
            {
                y = ring.Y - Tokens.Space12 - card.Height;
                x = ring.X;
            }
            else
            {
                y = Math.Clamp(ring.Y, 0, Math.Max(0, height - card.Height));
                x = ring.Right + Tokens.Space12 + card.Width <= width ? ring.Right + Tokens.Space12 : ring.X - Tokens.Space12 - card.Width;
            }
        }

        Avalonia.Controls.Canvas.SetLeft(demoCard, Math.Clamp(x, Tokens.Space12, Math.Max(Tokens.Space12, width - card.Width - Tokens.Space12)));
        Avalonia.Controls.Canvas.SetTop(demoCard, Math.Clamp(y, Tokens.Space12, Math.Max(Tokens.Space12, height - card.Height - Tokens.Space12)));
    }

    /// <summary>
    /// Entry 439 section 3.2: nothing the demo made is kept unless the person chose to keep it: its tab closes with nothing asked, the
    /// sessions it saved are removed, and so is its picture.
    /// </summary>
    internal void EndDemo(bool keep, bool finished = false)
    {
        if (!DemoRunning)
        {
            return;
        }

        demoLayer.IsVisible = false;
        demoCard.IsVisible = false;
        DemoSpotlit = null;
        settingsStore.SaveDemoStep(finished ? 0 : demoAt);
        ShowCarryOn();
        DiagnosticLog.Info("demo.end", ("step", demoAt + 1), ("kept", keep));
        if (keep)
        {
            foreach (var tab in targets.All.Where(t => t.Demo))
            {
                tab.Demo = false;
            }

            UpdateCurrentTab();
            Go(Destination.Sessions);
            toaster.Say(DemoWords.Kept);
            return;
        }

        foreach (var tab in targets.All.Where(t => t.Demo).ToList())
        {
            if (ReferenceEquals(tab, targets.Current))
            {
                MarkingIsSaved();
                saveSoon?.Stop();
                LeavePicture(closing: false);
                ForgetSetMembers(tab);
                targets.Close(tab);
                ShowTarget(targets.Current);
            }
            else
            {
                tab.Unsaved = false;
                tab.Parked = null;
                targets.Close(tab);
            }
        }

        if (sessions is not null)
        {
            foreach (var s in sessions.List().Where(s => !demoSessionsBefore.Contains(s.Id)).ToList())
            {
                if (sessions.Get(s.Id) is { } record && string.Equals(record.ImagePath, demoPath, StringComparison.OrdinalIgnoreCase))
                {
                    sessions.Delete(s.Id);
                }
            }
        }

        try
        {
            if (demoPath is not null && File.Exists(demoPath))
            {
                File.Delete(demoPath);
            }
        }
        catch (IOException)
        {
            // A picture still held open is removed the next time the demo starts, which writes over it.
        }

        ShowTabs();
        Go(Destination.Analyse);
        toaster.Say(DemoWords.Ended);
    }

    /// <summary>
    /// Entry 439 section 1.1: offered after the first run's questions, once, whatever the answer; Settings and the menu offer it any time.
    /// </summary>
    internal void OfferDemoIfDue()
    {
        if (sessions is null || settingsStore.LoadDemoOffered())
        {
            return;
        }

        settingsStore.SaveDemoOffered();
        demoCard.IsVisible = demoSpot.IsVisible = demoBadge.IsVisible = false;
        demoOffer.IsVisible = true;
        demoLayer.IsVisible = true;
        PlaceDemo();
    }

    private void CloseDemoOffer()
    {
        demoOffer.IsVisible = false;
        if (!demoCard.IsVisible)
        {
            demoLayer.IsVisible = false;
        }
    }

    /// <summary>Entry 439 section 1.2: Settings offers the demo from the start, and from the step a tour was left at when there is one.</summary>
    private Control DemoSettingsRow()
    {
        var start = new Button { Content = DemoWords.SettingsRow };
        start.Click += async (_, _) => await StartDemo().ConfigureAwait(true);
        demoCarryOn = new Button { Classes = { AppStyles.Secondary } };
        demoCarryOn.Click += async (_, _) => await StartDemo(settingsStore.LoadDemoStep()).ConfigureAwait(true);
        ShowCarryOn();
        return new StackPanel
        {
            Spacing = Tokens.Space8,
            Children =
            {
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = Tokens.Space8, Children = { start, demoCarryOn } },
                Line(DemoWords.SettingsSays),
            },
        };
    }

    private void ShowCarryOn()
    {
        if (demoCarryOn is null)
        {
            return;
        }

        int at = settingsStore.LoadDemoStep();
        demoCarryOn.IsVisible = at > 0;
        demoCarryOn.Content = DemoWords.CarryOn(at + 1, DemoSteps.Count);
    }

    /// <summary>
    /// Entry 439: the sample is made here, by GroupLab, as the tests make theirs: the 25-bull Letter sheet as it prints, with one hole a bull
    /// drawn the way a scanner sees one. So it is nobody's target, every platform has it, and no file has to be shipped.
    /// </summary>
    private string WriteDemoSheet()
    {
        var definition = targetsPanel.Sheets.First(s => s.File == "GL-CF25-LTR.gltd.json").Definition;
        const double Dpi = 300;
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], Dpi);
        double s = 254 / Dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var random = new Random(439);
        var holes = definition.Bulls.Where(b => b.Scoring)
            .Select(b => SyntheticSheet.SampleHole(random, b.X + random.Next(-45, 46), b.Y + random.Next(-45, 46), onInk: false, HoleBacking.ScannerLid, 0.871)).ToList();
        var image = SyntheticSheet.Compose(render, Dpi, truth, render.Width, render.Height, holes, [], random);
        string folder = Path.Combine(Path.GetDirectoryName(settingsStore.Path) ?? Path.GetTempPath(), "demo");
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "grouplab-demo-sample-sheet.png");
        using var mat = OpenCvSharp.Mat.FromPixelData(image.Height, image.Width, OpenCvSharp.MatType.CV_8UC1, image.Pixels);
        OpenCvSharp.Cv2.ImWrite(path, mat);
        return path;
    }
}

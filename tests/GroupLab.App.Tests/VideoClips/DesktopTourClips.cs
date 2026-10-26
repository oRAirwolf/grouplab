using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Tests.Support;
using static GroupLab.App.Tests.VideoClips.DesktopClips;

namespace GroupLab.App.Tests.VideoClips;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 475: the computer's clips for video 2 (the feature tour) and video 3 (getting started), made as
/// <see cref="DesktopClips"/> makes video 1's.
/// </summary>
public class DesktopTourClips
{
    /// <summary>The text field in the same row as the words <paramref name="label"/>.</summary>
    private static TextBox FieldBeside(Avalonia.Visual root, string label)
    {
        var words = Find(root, label);
        return words.GetLogicalAncestors().OfType<Panel>().Select(p => p.Children.OfType<TextBox>().FirstOrDefault()).FirstOrDefault(b => b is not null)
            ?? throw new InvalidOperationException($"no field beside \"{label}\"");
    }

    /// <summary>One clip of the window: <paramref name="setUp"/> before the first frame, <paramref name="act"/> recorded.</summary>
    private static void Clip(string name, string shows, Func<MainWindow> open, Action<MainWindow> setUp, Action<MainWindow, ScreenClip> act)
    {
        ScreenClip.SkipUnlessAsked();
        var window = open();
        try
        {
            setUp(window);
            ScreenClip.Settle();
            using var clip = new ScreenClip(window, name, shows);
            act(window, clip);
        }
        finally
        {
            Close(window);
        }
    }

    /// <summary>A clip of a window the main window opened, such as a group tool.</summary>
    private static void ToolClip(MainWindow window, string opener, string name, string shows, Action<Window, ScreenClip>? act = null)
    {
        Find(window, opener).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        ScreenClip.Settle();
        var tool = window.GroupToolWindow ?? throw new InvalidOperationException(opener + " opened nothing");
        tool.RequestedThemeVariant = ThemeVariant.Dark;
        ScreenClip.Settle();
        using (var clip = new ScreenClip(tool, name, shows))
        {
            act?.Invoke(tool, clip);
            clip.Hold(2.5);
        }

        tool.Close();
    }

    /// <summary>A clip of the window a button opens as a window of its own, such as Report a problem.</summary>
    private static void OwnedClip(Func<MainWindow> open, Action<MainWindow> setUp, Func<MainWindow, Control> opener, string name, string shows,
        Action<Window, ScreenClip>? act = null)
    {
        ScreenClip.SkipUnlessAsked();
        var window = open();
        try
        {
            setUp(window);
            ScreenClip.Settle();
            int before = window.OwnedWindows.Count;
            opener(window).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            ScreenClip.Settle();
            var owned = window.GroupToolWindow ?? (window.OwnedWindows.Count > before ? window.OwnedWindows[^1] : null)
                ?? throw new InvalidOperationException(name + ": nothing opened");
            owned.RequestedThemeVariant = ThemeVariant.Dark;
            ScreenClip.Settle();
            using (var clip = new ScreenClip(owned, name, shows))
            {
                act?.Invoke(owned, clip);
                clip.Hold(3);
            }

            owned.Close();
        }
        finally
        {
            Close(window);
        }
    }

    /// <summary>Presses the showing "Work out..." button, as a person does on Hit probability and Point blank, and watches the answer come.</summary>
    internal static void WorkItOut(MainWindow w, ScreenClip clip)
    {
        if (w.GetLogicalDescendants().OfType<Button>().Where(Entry109Tests.Shown)
            .FirstOrDefault(b => (b.Content as string)?.StartsWith("Work out the", StringComparison.Ordinal) == true) is { } work)
        {
            clip.Click(work, hold: 0.2);
        }

        clip.Watch(4);
    }

    private static MainWindow Analysed()
    {
        var window = Opened();
        window.Analyse();
        ScreenClip.Settle();
        return window;
    }

    // ---- Video 3, getting started.

    [AvaloniaFact]
    public void V3Step2FirstRun()
    {
        ScreenClip.SkipUnlessAsked();
        Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;
        GroupLab.App.Theme.TextSize.Choose(TextSizeChoice.Larger);
        var store = new AppSettingsStore(Path.Combine(DesktopClips.Folder(), "settings.json"));
        store.SaveTheme(ThemeChoice.Dark);
        store.SaveTextSize(TextSizeChoice.Larger);
        var window = new MainWindow(store)
        {
            Width = Width, Height = Height, RequestedThemeVariant = ThemeVariant.Dark, ReceiverOpen = true, ErrorsOpen = true, SurveyOpen = true, AskScope = true,
        };
        window.Show();
        try
        {
            window.ShowFirstRunIfDue();
            ScreenClip.Settle();
            using var clip = new ScreenClip(window, "v3-step2-first-run", "The first run's four questions answered one at a time, then the guided demo's offer");
            // The answers a person might give, each pressed where its question shows it: sizes in inches and a mil scope, then asked each
            // time about sending, and not this time for the rest.
            string[] answers = ["Sizes in inches", "Mil", "Ask me each time", "Not this time", "Skip for now"];
            var pressed = new HashSet<string>();
            string? question = null;
            for (int press = 0; press < 12 && Entry109Tests.Shown(window.FirstRunCard); press++)
            {
                string now = string.Join(" ", window.FirstRunCard.GetLogicalDescendants().OfType<TextBlock>().Where(Entry109Tests.Shown)
                    .Select(b => b.Text).Where(s => s?.StartsWith("Question ", StringComparison.Ordinal) == true));
                if (now != question)
                {
                    question = now;
                    pressed.Clear();
                    clip.Hold(1.2);
                }

                var answer = window.FirstRunCard.GetLogicalDescendants().OfType<Button>().Where(Entry109Tests.Shown)
                    .FirstOrDefault(b => b.Content is string s && answers.Contains(s) && !pressed.Contains(s) && b.IsEffectivelyEnabled);
                if (answer is null)
                {
                    break;
                }

                pressed.Add((string)answer.Content!);
                clip.Click(answer, hold: 0.7);
            }

            clip.Hold(2);
        }
        finally
        {
            Close(window);
        }
    }

    [AvaloniaFact]
    public void V3Step3Equipment() => Clip("v3-step3-equipment", "Equipment: a rifle added with its scope unit and click value, a barrel's length and twist, then a load", Window,
        w =>
        {
            w.Book = RecordBook.Empty;
            w.ShowEquipment(EquipmentKind.Rifle);
        },
        (w, clip) =>
        {
            clip.Type(w.EquipmentBox("name"), "Sample rifle, 6.5 Creedmoor");
            clip.Type(w.EquipmentBox("clickValue"), "0.1");
            clip.Click(ButtonSaying(w, "Save"), hold: 1);
            clip.Click(Find(w, "Add a barrel"), hold: 0.6);
            clip.Type(w.EquipmentBox("name"), "24 in Proof");
            clip.Type(w.EquipmentBox("lengthInches"), "24");
            clip.Type(w.EquipmentBox("twistInches"), "8");
            clip.Click(ButtonSaying(w, "Save"), hold: 1);
            clip.Act(() => w.ShowEquipment(EquipmentKind.Load), 0.6);
            clip.Type(w.EquipmentBox("name"), "Load A");
            clip.Type(w.EquipmentBox("muzzleVelocityFps"), "2710");
            clip.Click(ButtonSaying(w, "Save"), hold: 1.5);
        });

    /// <summary>The printer check, which opens a window of its own: recorded in that window, from its start to Print the check page.</summary>
    private static void PrinterCheck(string name, string shows)
    {
        ScreenClip.SkipUnlessAsked();
        var window = Window();
        try
        {
            window.ShowSettings(SettingsCategory.Printers);
            ScreenClip.Settle();
            Find(window, "Add a printer").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            ScreenClip.Settle();
            var check = window.OwnedWindows.LastOrDefault() ?? throw new InvalidOperationException("the printer check did not open");
            check.RequestedThemeVariant = ThemeVariant.Dark;
            ScreenClip.Settle();
            using (var clip = new ScreenClip(check, name, shows))
            {
                clip.Hold(1.5);
                if (check.GetLogicalDescendants().OfType<Button>().Where(Entry109Tests.Shown).FirstOrDefault(b => (b.Content as string)?.Contains("check page", StringComparison.Ordinal) == true) is { } print)
                {
                    clip.Glide(clip.Middle(print), 1.2);
                }

                clip.Hold(2);
            }

            check.Close();
        }
        finally
        {
            Close(window);
        }
    }

    [AvaloniaFact]
    public void V3Step4PrinterCheck() => PrinterCheck("v3-step4-printer-check", "Settings, Printers and paper, Add a printer: the printer check's first page");

    [AvaloniaFact]
    public void V3Step5MadeForYourOptic() => Clip("v3-step5-made-for-your-optic", "Targets, Made for your optic: the distance, the lowest magnification and the shots, then the sheet made, up to Print", Window,
        w => w.Go(Destination.Library),
        (w, clip) =>
        {
            clip.Click(Find(w, "Design your own sheet"), hold: 1);
            clip.Glide(clip.Middle(Find(w, "Made for your optic")), 1);
            clip.Type(FieldBeside(w, "Lowest magnification"), "4");
            clip.Click(Find(w, "Make the sheet"), hold: 2);
            if (w.GetLogicalDescendants().OfType<Button>().Where(Entry109Tests.Shown).FirstOrDefault(b => (b.Content as string)?.StartsWith("Print", StringComparison.Ordinal) == true
                && b.TranslatePoint(default, w) is not null) is { } print)
            {
                clip.Glide(clip.Middle(print), 1);
            }

            clip.Hold(1.5);
        });

    [AvaloniaFact]
    public void V3Step8ReviewTheMarks() => Clip("v3-step8-review-the-marks", "How did you shoot this sheet, a hole moved, then Next unsure hole", Opened, _ => { },
        (w, clip) =>
        {
            clip.Glide(clip.Middle(Find(w, "How did you shoot this sheet")), 1);
            clip.Hold(2);
            int id = w.Session.State.Shots.First(s => s.IsShot).Id;
            var from = HoleOnScreen(w, id);
            clip.Glide(from, 1);
            w.MouseDown(from, Avalonia.Input.MouseButton.Left);
            for (int i = 1; i <= 20; i++)
            {
                var at = new Avalonia.Point(from.X + i, from.Y + (i * 0.6));
                w.MouseMove(at, Avalonia.Input.RawInputModifiers.LeftMouseButton);
                clip.Glide(at, 1.0 / ScreenClip.Fps);
                clip.Redraw();
            }

            w.MouseUp(new Avalonia.Point(from.X + 20, from.Y + 12), Avalonia.Input.MouseButton.Left);
            clip.Act(() => { }, 1);
            clip.Click(Find(w, "Next unsure hole"), hold: 1.5);
        });

    [AvaloniaFact]
    public void V3Help() => OwnedClip(Window, w => w.ShowSettings(SettingsCategory.Help), w => Saying<Button>(w, "Report a problem\u2026"),
        "v3-help", "Settings, Help and problems, Report a problem: what the report holds, with a box for what happened");

    // ---- Video 2, the feature tour.

    [AvaloniaFact]
    public void V2Ch03Targets() => Clip("v2-ch03-targets", "The Targets screen: the library of sheets, one chosen and shown", Window,
        w => w.Go(Destination.Library),
        (w, clip) =>
        {
            clip.Click(Find(w, "Load Development"), hold: 1.5);
            clip.Click(Find(w, "Zeroing"), hold: 2);
        });

    [AvaloniaFact]
    public void V2Ch03MadeForYourOptic() => Clip("v2-ch03-made-for-your-optic", "Made for your optic: distance and magnification filled in and the sheet made", Window,
        w => w.Go(Destination.Library),
        (w, clip) =>
        {
            clip.Click(Find(w, "Design your own sheet"), hold: 1);
            clip.Type(FieldBeside(w, "Lowest magnification"), "10");
            clip.Click(Find(w, "Make the sheet"), hold: 2.5);
        });

    [AvaloniaFact]
    public void V2Ch03PrinterCheck() => PrinterCheck("v2-ch03-printer-check", "The printer check: a printer added and its check page offered");

    [AvaloniaFact]
    public void V2Ch06Analysis() => Clip("v2-ch06-analysis", "The analysis, held long on mean radius and its range", Opened, _ => { },
        (w, clip) =>
        {
            clip.Click(Find(w, "Accept and analyze"), hold: 1);
            clip.Glide(clip.Middle(Find(w, "Mean radius")), 1.5);
            clip.Hold(5);
        });

    [AvaloniaFact]
    public void V2Ch06SwitchUnits() => Clip("v2-ch06-switch-units", "A tap on a figure switches it between mil and MOA", Analysed, _ => { },
        (w, clip) =>
        {
            var figure = Find(w, "0.06 mil");
            clip.Click(figure, hold: 1.5);
            clip.Click(figure, hold: 1.5);
        });

    [AvaloniaFact]
    public void V2Ch06WordExplained() => Clip("v2-ch06-word-explained", "A dotted figure name pressed, and what it means said beside it", Analysed, _ => { },
        (w, clip) =>
        {
            clip.Click(Find(w, "Extreme spread"), hold: 2.5);
            clip.Click(Find(w, "CEP 90"), hold: 2.5);
        });

    [AvaloniaFact]
    public void V2Ch07ZeroFromThisGroup()
    {
        ScreenClip.SkipUnlessAsked();
        var window = Analysed();
        try
        {
            ToolClip(window, "Zero from this group", "v2-ch07-zero-from-this-group", "Zero from this group: the clicks to dial from this group's center");
        }
        finally
        {
            Close(window);
        }
    }

    [AvaloniaFact]
    public void V2Ch07ShotsNeeded() => OwnedClip(Analysed, _ => { }, w => Saying<Button>(w, "Shots and clicks\u2026"),
        "v2-ch07-shots-needed-to-zero", "Shots and clicks, opened from the analysis: each shot's offset and Shots Needed to Zero",
        (tool, clip) =>
        {
            if (tool.GetLogicalDescendants().OfType<Button>().Where(Entry109Tests.Shown)
                .FirstOrDefault(b => (b.Content as string)?.Contains("Shots Needed to Zero", StringComparison.Ordinal) == true) is { } open)
            {
                clip.Click(open, hold: 1);
            }
        });

    [AvaloniaFact]
    public void V2Ch08Compare() => Clip("v2-ch08-compare-loads", "Compare loads: two loads' sessions chosen and compared, with the range each could really be", TwoLoads,
        w => w.Go(Destination.Sessions),
        (w, clip) =>
        {
            foreach (var box in w.GetLogicalDescendants().OfType<CheckBox>().Where(Entry109Tests.Shown).Where(b => b.Width == 32).Take(2).ToList())
            {
                clip.Click(box, hold: 0.4);
            }

            clip.Click(Find(w, "Compare the chosen"), hold: 4);
        });

    [AvaloniaFact]
    public void V2Ch08FuddBuster() => OwnedClip(Analysed, _ => { }, w => Saying<Button>(w, "Why a few shots mislead, shown with these shots"),
        "v2-ch08-fudd-buster", "Fudd buster mode: why a few shots mislead, shown with the sample's own shots");

    [AvaloniaFact]
    public void V2Ch08SessionsOverTime() => Clip("v2-ch08-sessions-over-time", "Session records: one load's sessions over time", TwoLoads,
        w => w.Go(Destination.Sessions),
        (w, clip) => clip.Hold(3));

    [AvaloniaFact]
    public void V2Ch09Trajectory() => Clip("v2-ch09-trajectory", "Ballistics: the trajectory and its dope table for the sample rifle and Load A", Analysed,
        w => w.Go(Destination.Ballistics),
        (w, clip) => clip.Click(Find(w, "Trajectory"), hold: 3));

    [AvaloniaFact]
    public void V2Ch09HitChance() => Clip("v2-ch09-hit-chance", "Ballistics: Hit probability from the sample group", Analysed,
        w => w.Go(Destination.Ballistics),
        (w, clip) =>
        {
            clip.Click(Find(w, "Hit probability"), hold: 0.6);
            WorkItOut(w, clip);
        });

    [AvaloniaFact]
    public void V2Ch09PointBlank() => Clip("v2-ch09-point-blank", "Ballistics: Point blank range", Analysed,
        w => w.Go(Destination.Ballistics),
        (w, clip) =>
        {
            clip.Click(Find(w, "Point blank"), hold: 0.6);
            WorkItOut(w, clip);
        });

    [AvaloniaFact]
    public void V2Ch09ScopeAndMount() => Clip("v2-ch09-scope-and-mount", "The rifle's Scope and mount part in Equipment", Window,
        w => w.EditRifle(SampleRifle.Name),
        (w, clip) => clip.Act(w.ShowScopeAndMount, 3));

    [AvaloniaFact]
    public void V2Ch11Share()
    {
        ScreenClip.SkipUnlessAsked();
        var window = Analysed();
        try
        {
            ToolClip(window, "Share a picture", "v2-ch11-share-a-picture", "Share a picture: the sheet with its results box");
        }
        finally
        {
            Close(window);
        }
    }

    [AvaloniaFact]
    public void V2Ch11Sync() => Clip("v2-ch11-sync", "Settings, Sync and your data: Sync between devices and the daily copies", Window,
        w => w.ShowSettings(SettingsCategory.Data),
        (w, clip) =>
        {
            clip.Glide(clip.Middle(Find(w, "Sync between devices")), 1.2);
            clip.Hold(1.5);
            clip.Glide(clip.Middle(Find(w, "Restore a copy")), 1.2);
            clip.Hold(2);
        });

    [AvaloniaFact]
    public void V2Ch12Sharing() => Clip("v2-ch12-sharing-and-privacy", "Settings, Sharing and privacy: what GroupLab sends, and when", Window,
        w => w.ShowSettings(SettingsCategory.Sharing),
        (w, clip) => clip.Hold(4));

    [AvaloniaFact]
    public void V2Ch12ReportAProblem() => OwnedClip(Window, w => w.ShowSettings(SettingsCategory.Help), w => Saying<Button>(w, "Report a problem\u2026"),
        "v2-ch12-report-a-problem", "Report a problem: what the report holds before anything is written");

    /// <summary>Where a hole is drawn, in the window: the canvas's place for its picture point.</summary>
    private static Avalonia.Point HoleOnScreen(MainWindow w, int id) =>
        w.Canvas.TranslatePoint(w.Canvas.ToControl(w.Session.State.Find(id)!.Image), w) ?? throw new InvalidOperationException("the hole is not on the screen");

    private static void Key(Avalonia.Input.InputElement target, Avalonia.Input.Key key, Avalonia.Input.KeyModifiers modifiers = Avalonia.Input.KeyModifiers.None) =>
        target.RaiseEvent(new Avalonia.Input.KeyEventArgs { RoutedEvent = Avalonia.Input.InputElement.KeyDownEvent, Key = key, KeyModifiers = modifiers });

    [AvaloniaFact]
    public void V2Ch05NextUnsureHole() => Clip("v2-ch05-next-unsure-hole", "Next unsure hole, pressed three times, each hole GroupLab was least sure of shown in turn", Opened, _ => { },
        (w, clip) =>
        {
            for (int i = 0; i < 3; i++)
            {
                clip.Click(Find(w, "Next unsure hole"), hold: 1.4);
            }
        });

    [AvaloniaFact]
    public void V2Ch05MoveAHole() => Clip("v2-ch05-move-a-hole", "A hole dragged to where the paper says it is, then let go", Opened, _ => { },
        (w, clip) =>
        {
            int id = w.Session.State.Shots.First(s => s.IsShot).Id;
            var from = HoleOnScreen(w, id);
            clip.Glide(from, 1.2);
            w.MouseDown(from, Avalonia.Input.MouseButton.Left);
            ScreenClip.Settle();
            for (int i = 1; i <= 30; i++)
            {
                var at = new Avalonia.Point(from.X + (i * 0.8), from.Y + (i * 0.5));
                w.MouseMove(at, Avalonia.Input.RawInputModifiers.LeftMouseButton);
                clip.Glide(at, 1.0 / ScreenClip.Fps);
                clip.Redraw();
            }

            w.MouseUp(new Avalonia.Point(from.X + 24, from.Y + 15), Avalonia.Input.MouseButton.Left);
            clip.Act(() => { }, 2);
        });

    [AvaloniaFact]
    public void V2Ch05AddAndRemove() => Clip("v2-ch05-add-and-remove", "A hole added by a click where one was missed, then one taken away with Delete", Opened, _ => { },
        (w, clip) =>
        {
            int before = w.Session.State.Shots.Count(s => s.IsShot);
            var first = HoleOnScreen(w, w.Session.State.Shots.First(s => s.IsShot).Id);
            var empty = new Avalonia.Point(first.X + 60, first.Y - 50);
            clip.Act(() => w.Canvas.Tool = GroupLab.App.MarkingTool.Impact, 0.4);
            clip.Glide(empty, 1);
            clip.Press(empty);
            if (w.Session.State.Shots.Count(s => s.IsShot) == before)
            {
                clip.Act(() => w.Session.AddShot(w.Canvas.ToImage(w.TranslatePoint(empty, w.Canvas)!.Value)), 0.1);
            }

            clip.Hold(1.2);
            clip.Act(() => w.Canvas.Tool = GroupLab.App.MarkingTool.Select, 0.3);
            int last = w.Session.State.Shots.Last(s => s.IsShot).Id;
            clip.Glide(HoleOnScreen(w, last), 0.8);
            clip.Press(HoleOnScreen(w, last));
            clip.Hold(0.6);
            clip.Act(() => Key(w, Avalonia.Input.Key.Delete), 2);
        });

    [AvaloniaFact]
    public void V2Ch05Keyboard() => Clip("v2-ch05-keyboard", "The same from the keyboard: Next unsure hole, then the arrow keys nudging the hole, a step at a time", Opened, _ => { },
        (w, clip) =>
        {
            clip.Click(Find(w, "Next unsure hole"), hold: 1);
            w.Canvas.Focus();
            foreach (var key in new[] { Avalonia.Input.Key.Right, Avalonia.Input.Key.Right, Avalonia.Input.Key.Down, Avalonia.Input.Key.Down, Avalonia.Input.Key.Left })
            {
                clip.Act(() => Key(w.Canvas, key, Avalonia.Input.KeyModifiers.Shift), 0.5);
            }

            clip.Hold(1.5);
        });

    [AvaloniaFact]
    public void V2Ch08Velocity() => Clip("v2-ch08-velocity-and-the-vertical", "Velocity and the vertical: the string's speeds typed in, then how much of the vertical is velocity", Analysed, _ => { },
        (w, clip) =>
        {
            clip.Click(Find(w, "Type them in"), hold: 0.6);
            var box = w.GetLogicalDescendants().OfType<TextBox>().Where(Entry109Tests.Shown).LastOrDefault(b => b.AcceptsReturn || b.PlaceholderText?.ToString()?.Contains("speed", StringComparison.OrdinalIgnoreCase) == true)
                ?? w.GetLogicalDescendants().OfType<TextBox>().Where(Entry109Tests.Shown).Last();
            clip.Type(box, "2711 2697 2702 2690 2707 2712 2691 2707 2688 2700 2693 2692 2697 2693 2699 2692 2710 2706 2701 2688 2694 2692 2690 2711 2704", 0.03);
            w.OpenReadingsHere(box.Text ?? "");
            clip.Act(() => { }, 4);
        });

    [AvaloniaFact]
    public void V2Ch11RestoreACopy() => Clip("v2-ch11-restore-a-copy", "Settings, Sync and your data: Back up now, then Restore a copy listing the daily copies", Window,
        w => w.ShowSettings(SettingsCategory.Data),
        (w, clip) =>
        {
            clip.Click(Find(w, "Back up now"), hold: 1.2);
            clip.Click(Find(w, "Restore a copy"), hold: 3);
        });
}


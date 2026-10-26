using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.App.Theme;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;
using GroupLab.Tests.Support;

namespace GroupLab.App.Tests.VideoClips;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 475: the computer's clips for the launch videos, made only when <c>GROUPLAB_VIDEO_CLIPS</c> names their folder
/// (<c>scripts/video-clips/make-clips.ps1</c>). The window is 2560 by 1440 in the dark theme at the larger text size; the data are GroupLab's own
/// sample sheet and sample records, named plainly. Each clip is one action with a still second at each end.
/// </summary>
public class DesktopClips
{
    internal const int Width = 2560, Height = 1440;

    internal static readonly Rifle SampleRifle = new("Sample rifle, .308 Win", 0.1, AngularUnit.Mrad)
    {
        SightHeightInches = 1.75, ZeroDistanceYards = 100, Cartridge = ".308 Winchester",
    };

    internal static readonly Load LoadA = new("Load A", null)
    {
        MuzzleVelocityFps = 2650, MuzzleVelocitySdFps = 9, BallisticCoefficient = 0.243, DragModel = Core.Ballistics.DragModel.G7, BulletWeightGrains = 175,
        BcReference = Core.Ballistics.ReferenceAtmosphere.Icao,
    };

    internal static readonly Load LoadB = new("Load B", null)
    {
        MuzzleVelocityFps = 2710, MuzzleVelocitySdFps = 14, BallisticCoefficient = 0.235, DragModel = Core.Ballistics.DragModel.G7, BulletWeightGrains = 168,
        BcReference = Core.Ballistics.ReferenceAtmosphere.Icao,
    };

    /// <summary>The repository, from this file's place in it.</summary>
    internal static string Repository([System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", ".."));

    internal static string Sample => Path.Combine(Repository(), "samples", "gl-cf25-ltr-d-25-shots-600-dpi.png");

    /// <summary>A fresh window on a settings file of its own, dark, at the larger text size, with the sample records.</summary>
    internal static MainWindow Window()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;
        TextSize.Choose(TextSizeChoice.Larger);
        var store = new AppSettingsStore(Path.Combine(Folder(), "settings.json"));
        store.SaveUnits(UnitSettings.Imperial);
        store.SaveTextSize(TextSizeChoice.Larger);
        store.SaveTheme(ThemeChoice.Dark);
        store.SaveEveryFigure(true);
        var window = new MainWindow(store) { Width = Width, Height = Height, DetectOnOpen = false, RequestedThemeVariant = ThemeVariant.Dark };
        window.Show();
        window.Book = RecordBook.Empty.With(SampleRifle).With(LoadA).With(LoadB);
        ScreenClip.Settle();
        return window;
    }

    /// <summary>The sample sheet opened and its holes found, ready to be accepted.</summary>
    internal static MainWindow Opened()
    {
        var window = Window();
        window.OpenImage(Sample);
        var (grey, metadata) = ImageLoader.Load(Sample);
        var (value, _) = ImageLoader.LoadMaxChannel(Sample);
        var identity = GroupLab.Core.Registration.SheetIdentification.Identify(grey,
            GroupLab.Core.Registration.SheetIdentification.Candidates([Path.Combine(Repository(), "targets")]), new OpenCvSharpBackend(), new GroupLab.Core.Trace.TraceRecorder());
        var result = AutomaticMarking.Run(grey, value, metadata, identity.Definition!, new OpenCvSharpBackend(), calibre: Calibre.Of(0.308));
        window.ApplyDetection(result);
        window.Session.SetCalibre(Calibre.Of(0.308));
        window.Session.SetShotDistance(3600);
        window.Session.SetExpectedShots(25);
        window.Session.SetEquipment(SampleRifle, null, LoadA.Name);
        window.CalibreAnswered();
        ScreenClip.Settle();
        return window;
    }

    /// <summary>The first visible control whose words (its text, content or screen reader name) contain <paramref name="words"/>.</summary>
    internal static Control Find(Visual root, string words) =>
        root.GetLogicalDescendants().OfType<Control>().Where(Entry109Tests.Shown).FirstOrDefault(c => c switch
        {
            Button { Content: string s } => s.Contains(words, StringComparison.Ordinal),
            ContentControl { Content: string s } => s.Contains(words, StringComparison.Ordinal),
            TextBlock t => t.Text?.Contains(words, StringComparison.Ordinal) == true,
            _ => Avalonia.Automation.AutomationProperties.GetName(c)?.Contains(words, StringComparison.Ordinal) == true,
        }) ?? throw new InvalidOperationException($"nothing showing says \"{words}\"");

    /// <summary>The first showing button whose words are exactly <paramref name="words"/>.</summary>
    internal static Button ButtonSaying(Avalonia.Visual root, string words) =>
        root.GetLogicalDescendants().OfType<Button>().Where(Entry109Tests.Shown).FirstOrDefault(b => b.Content as string == words)
            ?? throw new InvalidOperationException($"no button says \"{words}\"");

    /// <summary>The first showing control of type <typeparamref name="T"/> whose content is exactly <paramref name="words"/>.</summary>
    internal static T Saying<T>(Avalonia.Visual root, string words)
        where T : ContentControl =>
        root.GetLogicalDescendants().OfType<T>().Where(Entry109Tests.Shown).FirstOrDefault(b => b.Content as string == words)
            ?? throw new InvalidOperationException($"no {typeof(T).Name} says \"{words}\"");

    /// <summary>A folder of the clip's own for its settings, records and copies, deleted when the clip ends.</summary>
    internal static string Folder() => Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"grouplab-clips-{Guid.NewGuid():N}")).FullName;

    internal static void Close(MainWindow window)
    {
        window.Close();
        if (Path.GetFileName(Path.GetDirectoryName(window.SettingsStore.Path)) is { } own && own.StartsWith("grouplab-clips-", StringComparison.Ordinal))
        {
            Temp.Delete(Path.GetDirectoryName(window.SettingsStore.Path)!);
        }

        TextSize.Choose(TextSizeChoice.Normal);
        Application.Current!.RequestedThemeVariant = ThemeVariant.Default;
    }

    /// <summary>Video 1: the desktop analysis, the group, the mean radius circle and the figures with their ranges, held on the figures.</summary>
    [AvaloniaFact]
    public void V1Analysis()
    {
        ScreenClip.SkipUnlessAsked();
        var window = Opened();
        try
        {
            using var clip = new ScreenClip(window, "v1-analysis", "The desktop analysis after Accept: the group, the mean radius circle and the figures, held on mean radius");
            clip.Click(Find(window, "Accept and analyze"), hold: 1.2);
            clip.Glide(clip.Middle(Find(window, "Mean radius")), 1.2);
            clip.Hold(3);
        }
        finally
        {
            Close(window);
        }
    }

    /// <summary>The sample accepted with Load A, and a second sheet, the same picture as a copy of its own with every hole a third further
    /// from its bull, accepted with Load B: two sessions to compare.</summary>
    internal static MainWindow TwoLoads()
    {
        var window = Opened();
        window.Analyse();
        ScreenClip.Settle();
        string copy = Path.Combine(Path.GetTempPath(), $"grouplab-clips-load-b-{Guid.NewGuid():N}.png");
        File.Copy(Sample, copy);
        File.AppendAllBytes(copy, [0]);
        window.OpenImage(copy);
        var (grey, metadata) = ImageLoader.Load(copy);
        var (value, _) = ImageLoader.LoadMaxChannel(copy);
        var identity = GroupLab.Core.Registration.SheetIdentification.Identify(grey,
            GroupLab.Core.Registration.SheetIdentification.Candidates([Path.Combine(Repository(), "targets")]), new OpenCvSharpBackend(), new GroupLab.Core.Trace.TraceRecorder());
        var found = AutomaticMarking.Run(grey, value, metadata, identity.Definition!, new OpenCvSharpBackend(), calibre: Calibre.Of(0.308));
        var bulls = found.Bulls.ToDictionary(b => b.Index, b => b.Image);
        var wider = found with
        {
            Detections = [.. found.Detections.Select(d => d.Assignment.Bull is int bull && bulls.TryGetValue(bull, out var c)
                ? d with { Image = new PointD(c.X + ((d.Image.X - c.X) * 1.35), c.Y + ((d.Image.Y - c.Y) * 1.35)) }
                : d)],
        };
        window.ApplyDetection(wider);
        window.Session.SetCalibre(Calibre.Of(0.308));
        window.Session.SetShotDistance(3600);
        window.Session.SetExpectedShots(25);
        window.Session.SetEquipment(SampleRifle, null, LoadB.Name);
        window.CalibreAnswered();
        window.Analyse();
        ScreenClip.Settle();
        return window;
    }

    /// <summary>Video 1: Compare loads, Load A and Load B with their ranges.</summary>
    [AvaloniaFact]
    public void V1Compare()
    {
        ScreenClip.SkipUnlessAsked();
        var window = TwoLoads();
        try
        {
            window.Go(Destination.Sessions);
            ScreenClip.Settle();
            using var clip = new ScreenClip(window, "v1-compare", "Session records: the two loads' sessions ticked, then Compare the chosen, with each load's group and its range");
            var boxes = window.GetLogicalDescendants().OfType<CheckBox>().Where(Entry109Tests.Shown).Where(b => b.Width == 32).Take(2).ToList();
            foreach (var box in boxes)
            {
                clip.Click(box, hold: 0.4);
            }

            clip.Click(Find(window, "Compare the chosen"), hold: 1);
            clip.Hold(3);
        }
        finally
        {
            Close(window);
        }
    }

    /// <summary>Video 1: the zero correction with its clicks, on the computer.</summary>
    [AvaloniaFact]
    public void V1Zero()
    {
        ScreenClip.SkipUnlessAsked();
        var window = Opened();
        try
        {
            window.Analyse();
            ScreenClip.Settle();
            using var clip = new ScreenClip(window, "v1-zero", "The zero correction beside the analysis: where the group sits from the aim, on the paper and in mil, and the clicks to dial");
            clip.Glide(clip.Middle(Find(window, "Zero correction")), 1.2);
            clip.Hold(1);
            clip.Glide(clip.Middle(Find(window, "Dial 1 click")), 1.0);
            clip.Hold(3);
        }
        finally
        {
            Close(window);
        }
    }

    /// <summary>Video 1 montage: the Targets screen choosing a sheet.</summary>
    [AvaloniaFact]
    public void V1MontageTargets()
    {
        ScreenClip.SkipUnlessAsked();
        var window = Window();
        try
        {
            window.Go(Destination.Library);
            ScreenClip.Settle();
            using var clip = new ScreenClip(window, "v1-montage-targets", "The Targets screen: a sheet chosen from the library and its preview");
            clip.Click(Find(window, "Load Development"), hold: 1.5);
        }
        finally
        {
            Close(window);
        }
    }

    /// <summary>Video 1 montage: Ballistics, the hit chance.</summary>
    [AvaloniaFact]
    public void V1MontageHitChance()
    {
        ScreenClip.SkipUnlessAsked();
        var window = Opened();
        try
        {
            window.Analyse();
            window.Go(Destination.Ballistics);
            ScreenClip.Settle();
            using var clip = new ScreenClip(window, "v1-montage-hit-chance", "Ballistics: Hit probability for the sample group with Load A");
            clip.Click(Find(window, "Hit probability"), hold: 0.6);
            DesktopTourClips.WorkItOut(window, clip);
        }
        finally
        {
            Close(window);
        }
    }

    /// <summary>Video 1 montage: the share picture with its results box.</summary>
    [AvaloniaFact]
    public void V1MontageShare()
    {
        ScreenClip.SkipUnlessAsked();
        var window = Opened();
        try
        {
            window.Analyse();
            ScreenClip.Settle();
            Find(window, "Share a picture").RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            ScreenClip.Settle();
            var share = window.GroupToolWindow ?? throw new InvalidOperationException("Share a picture did not open");
            share.Width = Width;
            share.Height = Height;
            share.RequestedThemeVariant = ThemeVariant.Dark;
            ScreenClip.Settle();
            using (var clip = new ScreenClip(share, "v1-montage-share", "Share a picture: the sheet with its results box"))
            {
                clip.Hold(2);
            }

            share.Close();
        }
        finally
        {
            Close(window);
        }
    }
}


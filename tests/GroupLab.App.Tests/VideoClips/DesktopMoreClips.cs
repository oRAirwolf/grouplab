using System.Globalization;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Tests.Support;
using OpenCvSharp;
using static GroupLab.App.Tests.VideoClips.DesktopClips;

namespace GroupLab.App.Tests.VideoClips;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 481 part A: the computer's clips entry 475 left for a later run, made as <see cref="DesktopClips"/> makes the
/// rest: the pooled set (video 1's montage and chapter 8), chapter 8's Garmin Xero file and Top Gun, chapter 10's store-bought target, and
/// video 3 step 9's computer half.
/// </summary>
public class DesktopMoreClips
{
    /// <summary>The 25 speeds the velocity clip types, as a Garmin Xero export of the string, a shot every 40 seconds.</summary>
    internal static string XeroFile(string folder)
    {
        double[] speeds = [2711, 2697, 2702, 2690, 2707, 2712, 2691, 2707, 2688, 2700, 2693, 2692, 2697, 2693, 2699, 2692, 2710, 2706, 2701, 2688, 2694, 2692, 2690, 2711, 2704];
        double average = speeds.Average();
        var start = new TimeSpan(9, 30, 0);
        var lines = new List<string> { "Range day", "#,Speed (FPS),\u0394 Avg (FPS),KE (FT-LB),Power Factor (kgr\u22c5ft/s),Time,Clean Bore,Cold Bore,Shot Notes" };
        for (int i = 0; i < speeds.Length; i++)
        {
            double v = speeds[i];
            lines.Add(string.Create(CultureInfo.InvariantCulture,
                $"{i + 1},\"{v:#,##0.0}\",{v - average:0.0},{175 * v * v / 450240:0},{175 * v / 1000:0.0},{start + TimeSpan.FromSeconds(40 * i):hh\\:mm\\:ss},,,"));
        }

        lines.Add("-,,,");
        lines.Add(string.Create(CultureInfo.InvariantCulture, $"AVERAGE SPEED,{average:0.0}"));
        string path = Path.Combine(folder, "Xero string 1.csv");
        File.WriteAllText(path, string.Join("\n", lines) + "\n");
        return path;
    }

    /// <summary>Pumps the window until <paramref name="task"/> ends, two minutes at most.</summary>
    private static void Finish(Task? task)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (task is { IsCompleted: false } && clock.Elapsed < TimeSpan.FromMinutes(2))
        {
            ScreenClip.Settle();
            Thread.Sleep(10);
        }

        ScreenClip.Settle();
        if (task is { IsCompleted: false })
        {
            throw new TimeoutException("the window's work did not finish in two minutes");
        }
    }

    /// <summary>Three sheets of the 2 MOA set dropped on one tab, read and pooled, as entry 423 has it; the window showing the pool.</summary>
    private static MainWindow PooledSet()
    {
        var window = Window();
        window.DetectOnOpen = true;
        string folder = Folder();
        var set = Entry423Tests.TwoMoaSet();
        window.OpenDropped([.. new[] { 0, 1, 2 }.Select(k => Entry423Tests.Sheet(set, folder, k))]);
        Finish(window.Dropping);
        if (window.SetPoolShown is null)
        {
            throw new InvalidOperationException("the three sheets were not pooled");
        }

        return window;
    }

    /// <summary>Video 1's montage: a pooled set of three sheets, two to three seconds.</summary>
    [AvaloniaFact]
    public void V1MontagePooledSet()
    {
        ScreenClip.SkipUnlessAsked();
        var window = PooledSet();
        try
        {
            using var clip = new ScreenClip(window, "v1-montage-pooled-set", "Three sheets of one set read in one tab and pooled into one group");
            clip.Hold(1.5);
        }
        finally
        {
            Close(window);
        }
    }

    /// <summary>Chapter 8: one sheet of a set alone, then Back to the set, where the three pool into one group.</summary>
    [AvaloniaFact]
    public void V2Ch08PooledSet()
    {
        ScreenClip.SkipUnlessAsked();
        var window = PooledSet();
        try
        {
            window.ShowSetSheet(0);
            ScreenClip.Settle();
            // A sheet shown alone opens at its marking; its analysis is where "\u2039 Back to the set" is.
            window.Analyse();
            ScreenClip.Settle();
            using var clip = new ScreenClip(window, "v2-ch08-pooled-set", "A set of three sheets: one sheet alone, then Back to the set, where the three pool into one group");
            clip.Hold(1.2);
            clip.Act(window.BackToSet, 4);
            if (window.SetPoolShown is null || window.CurrentSet?.Showing is not null)
            {
                throw new InvalidOperationException("Back to the set did not show the pool");
            }
        }
        finally
        {
            Close(window);
        }
    }

    /// <summary>Chapter 8: a Garmin Xero file dropped on the velocity block, then Match to shots.</summary>
    [AvaloniaFact]
    public void V2Ch08XeroFile()
    {
        ScreenClip.SkipUnlessAsked();
        var window = Opened();
        string folder = Folder();
        try
        {
            window.Analyse();
            ScreenClip.Settle();
            string file = XeroFile(folder);
            using var clip = new ScreenClip(window, "v2-ch08-xero-file", "Velocity and the vertical: a Garmin Xero export dropped in, its 25 speeds read, then Match to shots");
            clip.Glide(clip.Middle(Find(window, "Type them in")), 1.2);
            clip.Act(() => Finish(window.ReadingsFromFile(file)), 1.6);
            // The button, not the status line, which says "Press Match to shots." too.
            var match = Saying<Button>(window, VelocityBlock.MatchToShots);
            clip.Act(() => match.BringIntoView(new Avalonia.Rect(0, -200, match.Bounds.Width, match.Bounds.Height + 400)), 0.6);
            clip.Click(match, hold: 4);
            if (!window.GetLogicalDescendants().OfType<TextBlock>().Any(t => t.Text?.Contains("Matched as Ballistics would propose", StringComparison.Ordinal) == true))
            {
                throw new InvalidOperationException("Match to shots was not pressed");
            }
        }
        finally
        {
            Close(window);
            GroupLab.Tests.Support.Temp.Delete(folder);
        }
    }

    /// <summary>Chapter 8: Compare loads, with Top Gun's average five-shot group for each load beside it.</summary>
    [AvaloniaFact]
    public void V2Ch08TopGun()
    {
        ScreenClip.SkipUnlessAsked();
        var window = TwoLoads();
        try
        {
            // Top Gun needs the rifle's weight as fired; the sample rifle is given a plain one.
            window.Book = window.Book.With(SampleRifle with { WeightAsFiredLb = 11.5 });
            window.Go(Destination.Sessions);
            ScreenClip.Settle();
            using var clip = new ScreenClip(window, "v2-ch08-top-gun", "Compare loads with Top Gun: each load's group beside the average five-shot group Top Gun expects from the rifle and load");
            foreach (var box in window.GetLogicalDescendants().OfType<CheckBox>().Where(Entry109Tests.Shown).Where(b => b.Width == 32).Take(2).ToList())
            {
                clip.Click(box, hold: 0.4);
            }

            clip.Click(Find(window, "Compare the chosen"), hold: 1.5);
            var heading = window.GetLogicalDescendants().OfType<TextBlock>().Where(Entry109Tests.Shown).FirstOrDefault(t => t.Text == "Top Gun")
                ?? throw new InvalidOperationException("Compare loads showed no Top Gun");
            clip.Act(() => heading.BringIntoView(), 4);
        }
        finally
        {
            Close(window);
        }
    }

    /// <summary>Chapter 10: a store-bought target opened and recognized, its bull placed and its printed size the scale, with the warning.</summary>
    [AvaloniaFact]
    public void V2Ch10StoreBoughtTarget()
    {
        ScreenClip.SkipUnlessAsked();
        string blank = Path.Combine(@"C:\Dev\grouplab-local\commercial-targets", "bc-34805-shoot-n-c-8in-bull", "blank.png");
        Xunit.Assert.SkipWhen(!File.Exists(blank), "the store-bought target's scan is on Alan's computer only");
        var window = Window();
        string folder = Folder();
        try
        {
            string picture = Path.Combine(folder, "store-bought target.jpg");
            using (var scan = Cv2.ImRead(blank, ImreadModes.Color))
            using (var small = new Mat())
            {
                Cv2.Resize(scan, small, new OpenCvSharp.Size(0, 0), 1 / 6.0, 1 / 6.0, InterpolationFlags.Area);
                Cv2.ImWrite(picture, small);
            }

            window.DetectOnOpen = true;
            using var clip = new ScreenClip(window, "v2-ch10-store-bought-target", "A store-bought target opened: GroupLab names it, places its bull and takes its printed size as the scale, with the warning to check it");
            clip.Act(() =>
            {
                window.OpenImage(picture);
                Finish(window.DetectionTask);
            }, 4);
        }
        finally
        {
            Close(window);
            GroupLab.Tests.Support.Temp.Delete(folder);
        }
    }

    /// <summary>Video 3, step 9's computer half: Session records, and the session opened again on the computer.</summary>
    [AvaloniaFact]
    public void V3Step9ComputerSession()
    {
        ScreenClip.SkipUnlessAsked();
        var window = Opened();
        try
        {
            window.Analyse();
            ScreenClip.Settle();
            window.Go(Destination.Sessions);
            ScreenClip.Settle();
            using var clip = new ScreenClip(window, "v3-step9-computer-session", "Session records on the computer: the session opened again, its group and figures as they were");
            clip.Hold(1);
            var row = window.GetLogicalDescendants().OfType<Button>().Where(Entry109Tests.Shown)
                .FirstOrDefault(b => ToolTip.GetTip(b) is string tip && tip.StartsWith("Open the session", StringComparison.Ordinal))
                ?? throw new InvalidOperationException("Session records listed nothing");
            clip.Click(row, hold: 4);
        }
        finally
        {
            Close(window);
        }
    }
}

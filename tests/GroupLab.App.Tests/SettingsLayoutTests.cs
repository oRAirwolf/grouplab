using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using GroupLab.App.Theme;
using GroupLab.Core.Updates;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 119 section 6.4 and entry 125 sections 3 and 4: the settings page rendered at the two sizes, with the labels
/// held against the controls beside them and no gap left open by a line with nothing in it.
/// <para>
/// Alan's screenshot of an installed nightly showed "Train" and "Check" sitting above the middle of the boxes beside them, while the Units
/// rows above lined up, and an empty space between the Check row and the privacy note. Both were true: the update rows were two wrapping rows
/// rather than a grid, and the line that says what the last check found was an empty text block until a check had run.
/// </para>
/// <para>
/// The renders go to <c>docs/figures/screens/current/</c> when <c>GROUPLAB_SCREENS_TO_DOCS=1</c>, and they are rendered as a nightly build so
/// the page shows what a tester's copy shows rather than what a working copy does.
/// </para>
/// </summary>
public class SettingsLayoutTests(ITestOutputHelper output) : IDisposable
{
    private readonly BuildIdentity _wasBuild = MainWindow.ThisBuild;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        MainWindow.ThisBuild = _wasBuild;
    }

    [AvaloniaTheory]
    [InlineData(1280, 720)]
    [InlineData(2560, 1440)]
    public void TheSettingsPageLinesUpAtEverySize(int width, int height)
    {
        MainWindow.ThisBuild = BuildIdentity.Read("0.2.0-nightly.14+9db6500", "nightly");

        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        var window = new MainWindow(store) { Width = width, Height = height };
        window.Show();
        window.ShowSettings();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();

        // Entry 125 section 1: the page must describe a nightly as a nightly. This is the line Alan copies into a report.
        Assert.Contains("nightly build", MainWindow.BuildLine(), StringComparison.Ordinal);
        Assert.Contains("0.2.0-nightly.14", MainWindow.BuildLine(), StringComparison.Ordinal);
        Assert.DoesNotContain("development build", MainWindow.BuildLine(), StringComparison.Ordinal);

        // Entry 125 section 3: every label on the page sits against the middle of the control beside it, the way the Units rows do.
        var labels = window.GetLogicalDescendants().OfType<TextBlock>()
            .Where(t => t.Classes.Contains(AppStyles.Label) && t.IsEffectivelyVisible && t.Bounds.Height > 0)
            .ToList();
        Assert.True(labels.Count >= 5, $"the settings page showed {labels.Count} field labels");

        foreach (var label in labels)
        {
            var beside = Beside(window, label);
            if (beside is null)
            {
                continue;
            }

            double labelMiddle = label.Bounds.Center.Y + Offset(label);
            double controlMiddle = beside.Bounds.Center.Y + Offset(beside);
            output.WriteLine($"{width}x{height} {label.Text}: label centre {labelMiddle:0.0}, control centre {controlMiddle:0.0}");
            Assert.True(Math.Abs(labelMiddle - controlMiddle) <= 2.5,
                $"\"{label.Text}\" sits at {labelMiddle:0.0} and the control beside it at {controlMiddle:0.0}, so the label is not lined up with it");
        }

        // Entry 125 section 3: the line under the Check row says what the last check found. On a fresh settings file there has been no check,
        // so it says nothing and takes no room, rather than holding a gap open.
        Assert.False(window.ShowingUpdateState, "the update line is showing with nothing in it, which is the gap Alan saw");
        Assert.Equal("", window.UpdateStateText);

        if (Environment.GetEnvironmentVariable("GROUPLAB_SCREENS_TO_DOCS") == "1")
        {
            string into = Path.Combine(Repository(), "docs", "figures", "screens", "current", $"settings-light-{width}x{height}.png");
            Directory.CreateDirectory(Path.GetDirectoryName(into)!);
            using var frame = window.CaptureRenderedFrame();
            frame?.Save(into, new PngBitmapEncoderOptions());
            output.WriteLine("wrote " + into);
        }

        window.Close();
    }

    /// <summary>
    /// After a check the line says what was found, and on the next launch it says when that was. That is entry 119 section 6.2's "what
    /// happened last time", which is what the gap was reserved for and never held.
    /// </summary>
    [AvaloniaFact]
    public void TheLineUnderTheCheckRowRemembersWhatTheLastCheckFound()
    {
        MainWindow.ThisBuild = BuildIdentity.Read("0.2.0-nightly.14+9db6500", "nightly");
        string settings = Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json");

        var window = new MainWindow(new AppSettingsStore(settings)) { Width = 1400, Height = 900 };
        window.Show();
        window.ShowSettings();
        Dispatcher.UIThread.RunJobs();
        Assert.False(window.ShowingUpdateState);

        // Nothing answers, so the check finds nothing; what matters here is that it is remembered either way.
        window.CheckForUpdates();
        Dispatcher.UIThread.RunJobs();
        Assert.True(window.ShowingUpdateState);
        string found = window.UpdateStateText;
        Assert.NotEqual("", found);
        window.Close();

        var later = new MainWindow(new AppSettingsStore(settings)) { Width = 1400, Height = 900 };
        later.Show();
        later.ShowSettings();
        Dispatcher.UIThread.RunJobs();

        Assert.True(later.ShowingUpdateState);
        Assert.StartsWith("Checked ", later.UpdateStateText, StringComparison.Ordinal);
        Assert.Contains(found, later.UpdateStateText, StringComparison.Ordinal);
        later.Close();

        GroupLab.Tests.Support.Temp.DeleteFile(settings);
    }

    /// <summary>The control a label names: the next visible thing to its right in the same grid row.</summary>
    private static Control? Beside(MainWindow window, TextBlock label) =>
        label.Parent is Grid grid
            ? grid.Children.OfType<Control>().FirstOrDefault(c => !ReferenceEquals(c, label) && Grid.GetRow(c) == Grid.GetRow(label) && c.Bounds.Height > 0)
            : null;

    /// <summary>Where a control sits in the window, since bounds are given inside the parent.</summary>
    private static double Offset(Control control)
    {
        double y = 0;
        for (var at = control.Parent as Control; at is not null; at = at.Parent as Control)
        {
            y += at.Bounds.Y;
        }

        return y;
    }

    private static string Repository([System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));
}

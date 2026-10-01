using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Ballistics;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 323, Desktop B: "Velocity and the vertical" as a block of its own directly after the Group block, its states
/// and their buttons, and the velocity band on the composite plot with a switch in the key that is shown only where there is a band, on by
/// default, and remembered.
/// </summary>
public class Entry323Tests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static readonly Load WithBc = new("H4350 41.5", null) { BallisticCoefficient = 0.243, DragModel = DragModel.G7, BulletWeightGrains = 175 };

    [AvaloniaFact]
    public void TheBlockFollowsTheGroupBlockAndItsBandIsSwitchedAndRemembered()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        string store = window.SettingsStore.Path;
        try
        {
            window.Book = RecordBook.Empty.With(WithBc);
            window.Session.SetShotDistance(3600);
            window.Session.SetEquipment(null, null, WithBc.Name);
            window.CalibreAnswered();
            window.Analyse();
            Settle();

            // State 3: no readings yet, the approved sentence and a button to the chronograph entry; no band and no switch for one.
            var block = Assert.IsType<VelocityBlock>(window.Velocity);
            Assert.Equal(VelocityBlockState.NoReadings, block.State);
            Assert.Contains(Words(window), t => t == VelocityBlock.NoReadingsSentence);
            Assert.Contains(Buttons(window), b => b == "Add readings");
            Assert.False(window.VelocityBandToggle.IsVisible);
            Assert.Null(window.Plot.VelocityBand);

            // Directly after the Group block, before Advanced.
            var view = window.GetLogicalDescendants().OfType<StackPanel>().Single(p => p.Name == "velocityBlock");
            var figures = (StackPanel)((Control)view.Parent!).Parent!;
            int group = figures.Children.ToList().FindIndex(c => c is Border { Child: TextBlock { Text: "Group" } });
            int velocity = figures.Children.IndexOf((Control)view.Parent!);
            int advanced = figures.Children.ToList().FindIndex(c => c is Expander);
            Assert.True(group >= 0 && velocity > group && velocity < advanced, $"Group at {group}, the block at {velocity}, Advanced at {advanced}");
            Assert.True(figures.Children.Skip(group + 1).Take(velocity - group - 1).All(c => c is not Border { Child: TextBlock }), "nothing headed sits between the Group block and this one");

            window.VelocityAction(VelocityBlockState.NoReadings);
            Settle();
            Assert.True(window.ShowingBallistics);

            // Readings kept on the session, each beside its shot: a result, the band on the plot, and its switch on by default.
            int shots = window.Session.State.Shots.Count(s => s.IsShot);
            window.ReadChronograph("Chronograph", "2026-10-01", string.Join(", ", Enumerable.Range(0, shots).Select(i => 2700 + ((i * 7) % 23) - 11)));
            window.AcceptChronograph();
            window.ShowBallistics(false);
            Settle();
            block = Assert.IsType<VelocityBlock>(window.Velocity);
            Assert.True(block.HasResult, block.Sentence);
            Assert.Equal(VelocityBlock.WhyClosing, block.Why[^1]);
            Assert.Equal(shots, block.Dots.Count);
            Assert.NotNull(block.SlopeSentence);
            Assert.True(window.VelocityBandToggle.IsVisible);
            Assert.True(window.VelocityBandToggle.IsChecked == true);
            Assert.Equal(block.Band, window.Plot.VelocityBand);
            Assert.Contains(window.Plot.Legend, l => l.StartsWith(VelocityBlock.BandLabel, StringComparison.Ordinal));
            Assert.Contains(window.Plot.Legend, l => l.StartsWith(VelocityBlock.MeasuredLabel, StringComparison.Ordinal));
            Assert.Contains(Words(window), t => t == block.Headline);

            // "why" starts closed and opens the command's lines.
            var why = Words(window, visibleOnly: true);
            Assert.DoesNotContain(VelocityBlock.WhyClosing, why);

            // Off, and remembered: the key drops the band, and a new window starts with it off.
            window.VelocityBandToggle.IsChecked = false;
            Settle();
            Assert.False(window.Plot.Shown.VelocityBand);
            Assert.DoesNotContain(window.Plot.Legend, l => l.StartsWith(VelocityBlock.BandLabel, StringComparison.Ordinal));
            Assert.False(window.SettingsStore.LoadPlotMarks().VelocityBand);
            window.VelocityBandToggle.IsChecked = true;
            Settle();
            Assert.True(window.SettingsStore.LoadPlotMarks().VelocityBand);
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.DeleteFile(store);
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>State 4: without the load's BC the block says what the solver needs, and its button opens the load in Equipment.</summary>
    [AvaloniaFact]
    public void WithoutTheBcTheButtonOpensTheLoad()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        string store = window.SettingsStore.Path;
        try
        {
            window.Book = RecordBook.Empty.With(new Load("Plain", null));
            window.Session.SetShotDistance(3600);
            window.Session.SetEquipment(null, null, "Plain");
            window.CalibreAnswered();
            window.Analyse();
            Settle();
            int shots = window.Session.State.Shots.Count(s => s.IsShot);
            window.ReadChronograph("Chronograph", "2026-10-01", string.Join(", ", Enumerable.Range(0, shots).Select(i => 2700 + (i % 5))));
            window.AcceptChronograph();
            window.ShowBallistics(false);
            Settle();
            Assert.Equal(VelocityBlockState.NoBc, window.Velocity!.State);
            Assert.Contains(Words(window), t => t == VelocityBlock.NeedsSentence);
            Assert.False(window.VelocityBandToggle.IsVisible);
            window.VelocityAction(VelocityBlockState.NoBc);
            Settle();
            Assert.True(window.ShowingEquipment);
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.DeleteFile(store);
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    private static List<string> Words(MainWindow window, bool visibleOnly = false) =>
        [.. window.GetLogicalDescendants().OfType<StackPanel>().Single(p => p.Name == "velocityBlock").GetLogicalDescendants().OfType<TextBlock>()
            .Where(t => !visibleOnly || t.GetLogicalAncestors().OfType<Control>().All(a => a.IsVisible)).Select(t => t.Text ?? "")];

    private static List<string> Buttons(MainWindow window) =>
        [.. window.GetLogicalDescendants().OfType<StackPanel>().Single(p => p.Name == "velocityBlock").GetLogicalDescendants().OfType<Button>().Select(b => b.Content as string ?? "")];
}

using Avalonia.Controls;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Marking;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 407 section 1 (concept 4A, Alan's pick, his own top complaint from the phone on 2026-10-05): which bull each
/// shot belongs to, said by the picture itself. A line from every bull to each of its holes, on unless "Lines to bulls" hides them; the unsure
/// ones dashed in orange with a "?"; a click on a bull chooses it, and a click on a hole then gives it to that bull or takes it away. The side
/// panel says what the phone says under its picture: how many shots the chosen bull holds, how many holes are unsure, and two buttons, Add a
/// hole and Next unsure hole.
/// <para>
/// The lasso stays (section 1.6): the review queue's keyboard path and several tests put many shots on a bull with it at once, and choosing a
/// bull and clicking each hole is one hole a click. It is no longer the way the panel describes first.
/// </para>
/// </summary>
public sealed partial class MainWindow
{
    private readonly CheckBox linesToBulls = new() { Content = "Lines to bulls" };
    private readonly TextBlock bullBanner = new() { TextWrapping = Avalonia.Media.TextWrapping.Wrap, Classes = { AppStyles.Warn } };
    private readonly TextBlock bullHolds = new() { TextWrapping = Avalonia.Media.TextWrapping.Wrap };
    private readonly TextBlock unsureCount = new() { TextWrapping = Avalonia.Media.TextWrapping.Wrap, Classes = { AppStyles.Secondary } };
    private Button? bullDone;
    private int? lastUnsure;

    private void BuildBullLines(StackPanel panel)
    {
        bullLinesHeading = Ruled("Which bull each shot belongs to");
        panel.Children.Add(bullLinesHeading);
        linesToBulls.IsChecked = settingsStore.LoadLinesToBulls();
        canvas.LinesToBulls = linesToBulls.IsChecked == true;
        linesToBulls.IsCheckedChanged += (_, _) =>
        {
            canvas.LinesToBulls = linesToBulls.IsChecked == true;
            settingsStore.SaveLinesToBulls(canvas.LinesToBulls);
            canvas.InvalidateVisual();
        };
        ToolTip.SetTip(linesToBulls, "A line from each bull to every hole given to it. Unsure ones are dashed in orange with a question mark.");
        panel.Children.Add(linesToBulls);
        panel.Children.Add(Line("Click a bull to choose it, then click a hole to give it to that bull, or one of its holes to take it away."));
        panel.Children.Add(bullBanner);
        panel.Children.Add(bullHolds);
        panel.Children.Add(unsureCount);
        bullDone = Button("Done", () => canvas.Pick(null));
        bullButtons = Row(Button("Add a hole", () => SetTool(MarkingTool.Impact)), Button("Next unsure hole", NextUnsureHole), bullDone);
        panel.Children.Add(bullButtons);
        canvas.PickingChanged += (_, _) => ShowBullLines(session.State);
    }

    /// <summary>What the block says, rebuilt with the screen.</summary>
    private void ShowBullLines(MarkingState state)
    {
        int unsure = BullLines.Unsure(state).Count;
        unsureCount.Text = BullChoosing.UnsureCount(unsure);
        if (canvas.PickingBull is { } bull && state.Bulls.FirstOrDefault(b => b.Index == bull) is { } aim)
        {
            bullBanner.Text = BullChoosing.Banner(aim.Label);
            bullHolds.Text = BullChoosing.Holds(state, bull);
        }
        else
        {
            bullBanner.Text = "";
            bullHolds.Text = "";
        }

        bullBanner.IsVisible = bullBanner.Text.Length > 0;
        bullHolds.IsVisible = bullHolds.Text.Length > 0;
        if (bullDone is not null)
        {
            bullDone.IsVisible = canvas.PickingBull is not null;
        }
    }

    /// <summary>Entry 407 section 1.4: selects the next unsure hole, lights its bull and brings it into view.</summary>
    internal void NextUnsureHole()
    {
        var state = session.State;
        if (BullLines.NextUnsure(state, lastUnsure) is not { } next || state.Find(next) is not { } shot)
        {
            status.Text = "No unsure holes.";
            return;
        }

        lastUnsure = next;
        canvas.Pick(null);
        canvas.Selected = next;
        canvas.LitBull = shot.Bull;
        canvas.CentreOn(shot.Image);
        var line = BullLines.For(state).FirstOrDefault(l => l.Shot == next);
        status.Text = $"{ShotName(next)}: {(line is null ? "unsure" : BullLines.Words(line.Doubt))}.";
        DiagnosticLog.Info("marking.next-unsure", ("left", BullLines.Unsure(state).Count));
        Refresh();
        LandOnShot(next);
    }

    /// <summary>For the screenshot walk: the block scrolled into view.</summary>
    internal void BringBullLinesIntoView()
    {
        bullDone?.BringIntoView();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        linesToBulls.BringIntoView();
    }

    /// <summary>For the headless tests: the block's words, the banner first.</summary>
    internal IReadOnlyList<string> BullLinesText => [.. new[] { bullBanner.Text, bullHolds.Text, unsureCount.Text }.Where(t => !string.IsNullOrEmpty(t)).Select(t => t!)];

    /// <summary>For the headless tests: turns the lines switch.</summary>
    internal bool LinesShown
    {
        get => linesToBulls.IsChecked == true;
        set => linesToBulls.IsChecked = value;
    }
}

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Tests.Support;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 465 4B: the scale's pill has left the header for the top of the analysis's side panel, with the line under
/// it saying how the scale was found, on one sheet and on a set's tab, and the path in the header keeps its room.
/// </summary>
public class ScalePillTests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    /// <summary>The pill is in the side panel, leading it, and nowhere in the header.</summary>
    private static void AtTheTopOfTheSidePanel(MainWindow window)
    {
        Assert.DoesNotContain(window.AnalysisActions, window.ScalePill.GetLogicalAncestors());
        var block = Assert.IsType<StackPanel>(window.ScalePill.GetLogicalParent());
        Assert.Equal("ScaleSaid", block.Name);
        var side = Assert.IsType<StackPanel>(block.GetLogicalParent());
        Assert.Same(block, side.Children[0]);
        Assert.True(window.ScalePill.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void OnOneSheetThePillLeadsTheSidePanelAndSaysTheMarkersSetIt()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.Analyse();
            Settle();
            AtTheTopOfTheSidePanel(window);
            Assert.Equal(ScaleSaid.Checked, window.ScalePillText);
            var scale = Assert.IsType<SheetReference>(window.Session.State.Scale);
            Assert.Equal(ScaleSaid.How([scale], UnitSettings.Imperial), window.ScaleHowText);
            Assert.Contains("printed markers on the sheet", window.ScaleHowText, StringComparison.Ordinal);
        }
        finally
        {
            window.Close();
            File.Delete(path);
        }
    }

    /// <summary>
    /// At 1280 by 720, a set's tab of three sheets: the pill says the markers on all three set the scale, as each sheet's own scale says,
    /// and the header's path is laid out whole beside its buttons.
    /// </summary>
    [AvaloniaFact]
    public async Task OnASetsTabTheLineSpeaksForEverySheetAndThePathHasItsRoom()
    {
        var (window, folder) = Entry423Tests.Window(detect: true);
        try
        {
            window.Width = 1280;
            window.Height = 720;
            var set = Entry423Tests.TwoMoaSet();
            window.OpenDropped([.. new[] { 0, 1, 2 }.Select(k => Entry423Tests.Sheet(set, folder, k))]);
            await Entry423Tests.Settle(window);
            var shown = Assert.IsType<SetGroup>(window.CurrentSet);
            Assert.True(window.Analysing);
            AtTheTopOfTheSidePanel(window);
            var scales = shown.Members.Select(m => MainWindow.MemberState(m)!.Scale).ToList();
            Assert.All(scales, s => Assert.IsType<SheetReference>(s));
            Assert.Equal(ScaleSaid.Checked, window.ScalePillText);
            Assert.Equal(ScaleSaid.How(scales, UnitSettings.Imperial), window.ScaleHowText);
            Assert.StartsWith("From the printed markers on all 3 sheets", window.ScaleHowText, StringComparison.Ordinal);

            // The set's name in the path is shown whole, and nothing in the header is cut off or runs past the window.
            var crumb = window.GetLogicalDescendants().OfType<Button>().First(b => b.IsEffectivelyVisible && Equals(b.Content, set.Name));
            double wants = crumb.DesiredSize.Width - crumb.Margin.Left - crumb.Margin.Right;
            Assert.True(wants <= crumb.Bounds.Width + 0.5, $"the path's crumb wants {wants} and has {crumb.Bounds.Width}");
            var laid = LayoutAudit.Findings(window);
            Assert.True(laid.Count == 0, string.Join("; ", laid));
        }
        finally
        {
            window.Close();
            Temp.Delete(folder);
        }
    }
}

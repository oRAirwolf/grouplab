using Avalonia.Controls;
using Avalonia.Layout;
using GroupLab.App.Theme;
using GroupLab.Core.Marking;

namespace GroupLab.App;

/// <summary>
/// Entry 422 (Jylee): "it isn't immediately clear what's happening when you click on a bull or an impact, the side panel also doesn't jump
/// to the right section cleanly". A click on the picture now lands the side panel on the section it changed, flashes that section, and
/// says on the status line what the click did.
/// </summary>
public partial class MainWindow
{
    private readonly ScrollViewer sideScroll = new() { HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
    private Border sideFlash = new();
    private Landing? landing;
    private Control? shotsHeading;
    private Control? bullLinesHeading;
    private Control? bullButtons;
    private Control? loadsLabel;

    /// <summary>The bull the last shift or command click added to the load's bulls or took out of them.</summary>
    private (int Index, bool Added)? lastLoadBull;

    /// <summary>Whether something the canvas said is on the status line from this click, so the click's own sentence does not cover it.</summary>
    private bool noticeShown;

    /// <summary>The side panel's column, with the strip that flashes behind it.</summary>
    private ScrollViewer SidePanel(StackPanel panel)
    {
        var palette = Tokens.For(ActualThemeVariant);
        sideFlash = Landing.Strip(palette.AmberTint, palette.AmberTintBorder);
        sideFlash.HorizontalAlignment = HorizontalAlignment.Stretch;
        var content = new Grid { Children = { sideFlash, panel } };
        sideScroll.Content = content;
        landing = new Landing(sideScroll, content, sideFlash);
        return sideScroll;
    }

    /// <summary>For the headless tests: the side panel's scroller and what its last landing did.</summary>
    internal ScrollViewer SideScroll => sideScroll;

    internal Landing? SideLanding => landing;

    /// <summary>For the headless tests: what the last click asked the side panel to land on, its heading and its row.</summary>
    internal (Control Heading, Control Row, Control RowEnd)? LandedOn { get; private set; }

    private void LandOn(Control heading, Control last, Control row, Control rowEnd)
    {
        LandedOn = (heading, row, rowEnd);
        landing?.On(heading, last, row, rowEnd);
    }

    private void ShowNotice(string note)
    {
        status.Text = note;
        noticeShown = true;
        Avalonia.Threading.Dispatcher.UIThread.Post(() => noticeShown = false, Avalonia.Threading.DispatcherPriority.Background);
    }

    /// <summary>Lands on what a click on the picture changed, and says what it did unless the canvas has said something already.</summary>
    private void OnPictureClick(PictureClick did)
    {
        var state = session.State;
        string? says = null;
        // The strip takes the theme as it is now, so a theme changed since start-up flashes in its own colours.
        var palette = Tokens.For(ActualThemeVariant);
        sideFlash.Background = new Avalonia.Media.SolidColorBrush(palette.AmberTint);
        sideFlash.BorderBrush = new Avalonia.Media.SolidColorBrush(palette.AmberTintBorder);
        switch (did)
        {
            case PictureClick.ShotSelected or PictureClick.HoleAdded when canvas.Selected is { } id:
                LandOnShot(id);
                says = did == PictureClick.HoleAdded ? ClickWords.HoleAdded(ShotNameLower(id)) : ClickWords.ShotSelected(ShotNameLower(id));
                break;

            case PictureClick.BullChosen when canvas.PickingBull is { } bull && state.Bulls.FirstOrDefault(b => b.Index == bull) is { } aim:
                if (bullLinesHeading is not null && bullButtons is not null)
                {
                    LandOn(bullLinesHeading, bullButtons, bullBanner, bullHolds.IsVisible ? bullHolds : bullBanner);
                }

                says = ClickWords.BullChosen(aim.Label);
                break;

            case PictureClick.BullForLoad when lastLoadBull is { } bull && state.Bulls.FirstOrDefault(b => b.Index == bull.Index) is { } aim:
                if (loadsLabel is not null)
                {
                    LandOn(loadsLabel, ruleSays, pickBulls, pickBulls);
                }

                says = ClickWords.BullForLoad(aim.Label, bull.Added, bullSelection.Count);
                break;
        }

        if (says is not null && !noticeShown)
        {
            status.Text = says;
        }
    }

    /// <summary>Lands the side panel on a shot's open row under Shots.</summary>
    private void LandOnShot(int id)
    {
        if (shotsHeading is null)
        {
            return;
        }

        var rows = shotList.Children.ToList();
        int at = rows.FindIndex(c => c is Grid g && g.Children.OfType<Button>().Any(b => b.Tag is int tag && tag == id));
        if (at < 0)
        {
            return;
        }

        var end = at + 1 < rows.Count && rows[at + 1] is not Grid ? rows[at + 1] : rows[at];
        LandOn(shotsHeading, shotList, rows[at], end);
    }
}

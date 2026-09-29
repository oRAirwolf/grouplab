using Avalonia.Controls;
using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile;

/// <summary>
/// Zero from this group, NOTES-FROM-PLANNING.md entry 280 section 2 (entry 278 feature h, board ZeroFrom): the group's center from the aim
/// point, the clicks with the scope named, how well the center is known at this many shots (entry 53 section 3's rule: dial an axis only where
/// its interval excludes zero), the way on to Shots Needed to Zero, and the offset handed to Ballistics as its zero offset.
/// </summary>
internal sealed class ZeroFromPage : UserControl
{
    public ZeroFromPage(MarkingState state, UnitSettings units, Action shotsToZero, Action back)
    {
        var column = new StackPanel { Spacing = 12 };
        column.Children.Add(Screens.Title("Zero from this group"));
        // Entry 280 section 2: the words are the desktop window's own (ResultWords).
        // Entry 294 section 1: somebody with rifles of each is asked which rifle a session with none was shot with.
        var words = ResultWords.ZeroFrom(state, units, state.Rifle is null && Phone.Settings.LoadScopeAnswer() == ScopeAnswer.Both);
        if (words.Refusal is { } refusal)
        {
            column.Children.Add(Screens.Line(refusal));
        }
        else
        {
            column.Children.Add(Screens.Card([.. words.Axes.Select(a => (Control)UnitTap.Attach(Screens.Line(a)))]));
            column.Children.Add(Screens.Dim(words.Scope));
            column.Children.Add(Screens.Dim(words.HowWell));
            column.Children.Add(Screens.Line(words.Verdict));
        }

        column.Children.Add(Screens.Choice("Open in Shots Needed to Zero", shotsToZero));
        // Entry 280 section 2: the offset handed to Ballistics, whose dope then includes it at every range.
        column.Children.Add(Screens.Choice("Use as the zero offset in Ballistics", () => Shell.Current?.ShowBallistics(state, zeroOffset: true)));
        column.Children.Add(Screens.Choice("Back to the result", back));
        Content = Screens.Page(column);
    }
}

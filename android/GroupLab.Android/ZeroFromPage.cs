using System.Globalization;
using Avalonia.Controls;
using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.Android;

/// <summary>
/// Zero from this group, NOTES-FROM-PLANNING.md entry 280 section 2 (entry 278 feature h, board ZeroFrom): the group's center from the aim
/// point, the clicks with the scope named, how well the center is known at this many shots (entry 53 section 3's rule: dial an axis only where
/// its interval excludes zero), and the way on to Shots Needed to Zero. Handing the offset to Ballistics as its zero offset waits for the
/// solver to carry one.
/// </summary>
internal sealed class ZeroFromPage : UserControl
{
    public ZeroFromPage(MarkingState state, UnitSettings units, Action shotsToZero, Action back)
    {
        var column = new StackPanel { Spacing = 12 };
        column.Children.Add(Screens.Title("Zero from this group"));
        if (Zeroing.For(state) is not { } zero)
        {
            column.Children.Add(Screens.Line("This group has no aim point to measure a zero from, or too few shots for one."));
        }
        else
        {
            string Axis(ZeroAxis axis, string name)
            {
                string amount = units.Length(Math.Abs(axis.OffsetInches)) + " " + axis.Sits;
                string dial = axis.Clicks is { } c ? c.Describe() : "dial " + axis.Dial;
                return axis.Distinguishable
                    ? $"{name}: the group sits {amount}; {dial}."
                    : $"{name}: the group sits {amount}, too little to dial at {zero.Shots} shots"
                      + (axis.ShotsToSettle is { } n ? string.Create(CultureInfo.CurrentCulture, $"; about {n} shots would settle it.") : ".");
            }

            column.Children.Add(Screens.Card(Screens.Line(Axis(zero.Elevation, "Up and down")), Screens.Line(Axis(zero.Windage, "Across"))));
            column.Children.Add(Screens.Dim(state.Rifle is { } rifle
                ? $"Clicks at {rifle.DescribeClick()}, the click value of {rifle.Name}."
                : "Choose a rifle with its click value to see clicks."));
            column.Children.Add(Screens.Dim(string.Create(CultureInfo.CurrentCulture,
                $"How well the center is known: the smallest offset these {zero.Shots} shots can call is {units.Length(zero.DetectableInches)}; an axis is worth dialing only where the group sits further off than that.")));
            column.Children.Add(Screens.Line(zero.Worth ? "Worth dialing." : "Neither axis is far enough off to be worth dialing at this many shots."));
        }

        column.Children.Add(Screens.Choice("Open in Shots Needed to Zero", shotsToZero));
        // Entry 280 section 2: the offset handed to Ballistics, whose dope then includes it at every range.
        column.Children.Add(Screens.Choice("Use as the zero offset in Ballistics", () => Shell.Current?.ShowBallistics(state, zeroOffset: true)));
        column.Children.Add(Screens.Choice("Back to the result", back));
        Content = Screens.Page(column);
    }
}

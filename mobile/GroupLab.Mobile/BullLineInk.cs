using System.Globalization;
using Avalonia;
using Avalonia.Media;
using GroupLab.App.Theme;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 407 section 1: the lines from each bull to its holes, drawn the same on the result's picture and in Fix holes.
/// A plain line is thin and grey; an unsure one (<see cref="BullLines"/>) dashed in orange with a "?" beside its hole; the lit or chosen bull's
/// lines blue and thicker, and while a bull is chosen every other line dims. Before this the lines were orange-red and the lit bull's amber,
/// which an unsure orange line could not have stood out from.
/// </summary>
internal static class BullLineInk
{
    private static readonly IBrush Chosen = new SolidColorBrush(Tokens.MarkChosen);
    private static readonly IBrush Unsure = new SolidColorBrush(Tokens.MarkUnsure);
    private static readonly IBrush Plain = new SolidColorBrush(Tokens.MarkFaint, 0.85);
    private static readonly IBrush Dim = new SolidColorBrush(Tokens.MarkFaint, 0.35);
    private static readonly DashStyle Dashes = new([3, 2], 0);

    /// <summary>The blue of the lit or chosen bull, for its ring and its holes.</summary>
    internal static IBrush Lit => Chosen;

    /// <param name="state">The marking.</param>
    /// <param name="toScreen">Image pixels to the control's own units.</param>
    /// <param name="lit">The bull lit or chosen, blue; null for none.</param>
    /// <param name="picking">Whether that bull is chosen to give holes to, which dims every other line.</param>
    /// <param name="linesOn">"Lines to bulls"; off, only the lit bull's lines are drawn.</param>
    /// <param name="moved">Where a hole being moved is now, to draw its line to.</param>
    internal static void Draw(DrawingContext context, MarkingState state, Func<PointD, Point> toScreen, int? lit, bool picking, bool linesOn, Func<int, PointD?>? moved = null)
    {
        foreach (var line in BullLines.For(state))
        {
            bool mine = lit == line.Bull;
            if (!linesOn && !mine)
            {
                continue;
            }

            var from = toScreen(line.From);
            var to = toScreen(moved?.Invoke(line.Shot) ?? line.To);
            var pen = mine ? new Pen(Chosen, 3)
                : picking ? new Pen(Dim, 1.5, line.Unsure ? Dashes : null)
                : line.Unsure ? new Pen(Unsure, 2.5, Dashes)
                : new Pen(Plain, 1.5);
            context.DrawLine(pen, from, to);
            if (line.Unsure && !picking && !mine)
            {
                var mark = new FormattedText("?", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(Typeface.Default.FontFamily, FontStyle.Normal, FontWeight.Bold), 15, Unsure);
                context.DrawText(mark, new Point(to.X - 22, to.Y - 22));
            }
        }
    }
}

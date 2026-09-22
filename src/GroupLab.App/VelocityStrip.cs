using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using GroupLab.App.Theme;
using GroupLab.Core.Records;

namespace GroupLab.App;

/// <summary>
/// How much do these shots vary in velocity, and how well do this many shots pin that down? NOTES-FROM-PLANNING.md entry 141 section 5.2.5.
/// <para>
/// <b>Two numbers are printed on every chronograph ever sold, and both of them mislead.</b> An SD of 10 ft/s over ten shots is not a rifle
/// that holds 10 ft/s; the same rifle measured again could read 7 or 19, and the number 10 says nothing about that. And the extreme spread
/// of ten shots is expected to be larger than the extreme spread of five from the same rifle, so a shooter who fires more shots and reports
/// a bigger ES has not found a worse load.
/// </para>
/// <para>
/// So the picture draws the readings themselves, with the mean and one SD each side marked on them, and the caption carries the SD's own
/// interval and says out loud what the extreme spread depends on. The readings are dots rather than a bar because a string where one shot
/// is 40 ft/s off is a different thing from a string spread evenly, and the two have the same SD.
/// </para>
/// </summary>
internal sealed class VelocityStrip : Control
{
    private const double PadLeft = 16;

    private const double PadRight = 16;

    private const double PadTop = 14;

    private const double Tall = 96;

    /// <summary>The velocities, in the order the chronograph recorded them, in feet per second.</summary>
    public IReadOnlyList<double> VelocitiesFps { get; set; } = [];

    /// <summary>How a velocity reads in the person's units, set by the window.</summary>
    public Func<double, string> Speed { get; set; } = fps => string.Create(CultureInfo.InvariantCulture, $"{fps:0} ft/s");

    /// <summary>The same for a difference between two velocities, which in every unit is the unit itself.</summary>
    public Func<double, string> SpeedDifference { get; set; } = fps => string.Create(CultureInfo.InvariantCulture, $"{fps:0.0} ft/s");

    public VelocityStrip()
    {
        ClipToBounds = true;
    }

    protected override Size MeasureOverride(Size availableSize) => new(availableSize.Width, Tall);

    /// <summary>The readings' mean and SD, or null where there are fewer than two readings.</summary>
    public VelocitySpread? Spread => Chronograph.Spread(VelocitiesFps);

    /// <summary>
    /// What the picture says in words, for a screen reader and for the headless tests. It never gives an SD without the range that many
    /// shots pins it to, and never gives an extreme spread without saying what it depends on.
    /// </summary>
    public string Description
    {
        get
        {
            if (VelocitiesFps.Count == 0)
            {
                return "No velocities recorded for these shots.";
            }

            if (Spread is not { } spread)
            {
                return $"One reading, {Speed(VelocitiesFps[0])}. Two or more are needed for a spread.";
            }

            string said = $"{spread.Readings} readings: mean {Speed(spread.MeanFps)}, SD {SpeedDifference(spread.SdFps)}";
            if (Chronograph.SdInterval(spread.Readings, spread.SdFps) is { } interval)
            {
                said += $" ({SpeedDifference(interval.LowerFps)} to {SpeedDifference(interval.UpperFps)})";
            }

            said += ".";
            if (Chronograph.ExtremeSpreadFps(VelocitiesFps) is { } extreme)
            {
                said += $" Extreme spread {SpeedDifference(extreme)}, which grows with the number of shots on its own, so it can only be compared with another string of {spread.Readings}.";
            }

            return said;
        }
    }

    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (VelocitiesFps.Count == 0 || Bounds.Width < PadLeft + PadRight + 40)
        {
            return;
        }

        var palette = Tokens.For(ActualThemeVariant);
        double left = PadLeft, right = Bounds.Width - PadRight;
        double middle = PadTop + 30;

        double low = VelocitiesFps.Min(), high = VelocitiesFps.Max();
        if (high - low < 1e-9)
        {
            low -= 5;
            high += 5;
        }

        double margin = (high - low) * 0.12;
        low -= margin;
        high += margin;
        double X(double fps) => left + ((fps - low) / (high - low) * (right - left));

        Marks.Line(context, Marks.Faint, new Point(left, middle), new Point(right, middle), 1);

        if (Spread is { } spread)
        {
            // One SD each side of the mean: where about two thirds of the shots belong, drawn on the shots rather than instead of them.
            double half = spread.SdFps / (high - low) * (right - left);
            double centre = X(spread.MeanFps);
            context.FillRectangle(new SolidColorBrush(Tokens.MarkTeal, 0.16), new Rect(centre - half, middle - 13, 2 * half, 26));
            Marks.Line(context, Marks.Teal, new Point(centre, middle - 15), new Point(centre, middle + 15), 1.5);
        }

        foreach (double velocity in VelocitiesFps)
        {
            Marks.Dot(context, Marks.Impact, new Point(X(velocity), middle), 3.5);
        }

        // The two ends only, because the extreme spread is the distance between them and a label on every dot would hide it.
        foreach ((double fps, bool start) in new[] { (VelocitiesFps.Min(), true), (VelocitiesFps.Max(), false) })
        {
            var text = new FormattedText(Speed(fps), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface(Tokens.Sans), Tokens.DetailSize, new SolidColorBrush(palette.Dim));
            double at = start ? X(fps) - (text.Width / 2) : X(fps) - (text.Width / 2);
            context.DrawText(text, new Point(Math.Clamp(at, 0, Math.Max(0, Bounds.Width - text.Width)), middle + 20));
        }
    }
}

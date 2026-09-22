using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using GroupLab.App.Theme;
using GroupLab.Core.Statistics;

namespace GroupLab.App;

/// <summary>
/// Did the group open up as it was shot? NOTES-FROM-PLANNING.md entry 141 section 5.2.3.
/// <para>
/// <b>It draws nothing unless the order is actually known.</b> A sheet does not record what order it was shot in, and the order only exists
/// where a chronograph string has been mapped to the shots. Numbering the holes left to right and calling that the shot order would produce
/// a chart that looks exactly like this one and means nothing, which is worse than no chart.
/// </para>
/// <para>
/// The caption carries the test rather than the picture, for the same reason the spread strips do: ten shots in a random order will look like
/// a barrel warming often enough to convince somebody, so the words say whether the shots can support the shape the eye is seeing.
/// </para>
/// </summary>
internal sealed class ShotOrderChart : Control
{
    private const double PadLeft = 44;

    private const double PadRight = 16;

    private const double PadTop = 10;

    private const double PadBottom = 24;

    /// <summary>Each shot's distance from the group's centre, inches, in the order it was fired. Empty where the order is not known.</summary>
    public IReadOnlyList<double> Radii { get; set; } = [];

    public OrderTrend? Trend { get; set; }

    public Func<double, string> Length { get; set; } = inches => string.Create(CultureInfo.InvariantCulture, $"{inches:0.000} in");

    public ShotOrderChart()
    {
        ClipToBounds = true;
    }

    protected override Size MeasureOverride(Size availableSize) => new(availableSize.Width, Radii.Count == 0 ? 0 : 150);

    /// <summary>What the picture says in words, for a screen reader and for the headless tests.</summary>
    public string Description
    {
        get
        {
            if (Radii.Count == 0)
            {
                return "The order these shots were fired in is not known, so there is nothing to show.";
            }

            if (Trend is not { } trend)
            {
                return string.Create(CultureInfo.InvariantCulture,
                    $"{Radii.Count} shots in the order fired. Fewer than {ShotOrderTrend.FewestShots} cannot show a trend at all.");
            }

            string shape = trend.Correlation > 0 ? "opened up" : "tightened";
            return trend.PValue < 0.05
                ? string.Create(CultureInfo.InvariantCulture,
                    $"The group {shape} as it was shot, and {trend.Shots} shots are enough to say so: a group whose order carried nothing would look this ordered about {trend.PValue:P1} of the time.")
                : string.Create(CultureInfo.InvariantCulture,
                    $"It {shape} a little, but {trend.Shots} shots cannot tell that from chance: a group whose order carried nothing looks at least this ordered about {trend.PValue:P0} of the time.");
        }
    }

    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Radii.Count < 2 || Bounds.Width < PadLeft + PadRight + 40)
        {
            return;
        }

        var palette = Tokens.For(ActualThemeVariant);
        double left = PadLeft, right = Bounds.Width - PadRight, top = PadTop, bottom = Bounds.Height - PadBottom;
        double most = Radii.Max();
        if (most <= 0)
        {
            return;
        }

        double X(int i) => left + (i * (right - left) / Math.Max(1, Radii.Count - 1));
        double Y(double r) => bottom - (r / most * (bottom - top));

        Marks.Line(context, Marks.Faint, new Point(left, bottom), new Point(right, bottom), 1);

        // The mean radius as a rule, so a dot above or below it reads as further out or closer in than this group's own average.
        double mean = Radii.Average();
        Marks.Line(context, Marks.Teal, new Point(left, Y(mean)), new Point(right, Y(mean)), 1, Marks.Dashed);
        var meanText = new FormattedText(Length(mean), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(Tokens.Mono), Tokens.DetailSize, new SolidColorBrush(palette.Dim));
        context.DrawText(meanText, new Point(0, Y(mean) - (meanText.Height / 2)));

        for (int i = 0; i < Radii.Count; i++)
        {
            if (i > 0)
            {
                Marks.Line(context, Marks.Faint, new Point(X(i - 1), Y(Radii[i - 1])), new Point(X(i), Y(Radii[i])), 1);
            }

            Marks.Dot(context, Marks.Impact, new Point(X(i), Y(Radii[i])), 4);
        }

        foreach (int at in new[] { 0, Radii.Count - 1 })
        {
            var label = new FormattedText((at + 1).ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, new Typeface(Tokens.Sans), Tokens.DetailSize, new SolidColorBrush(palette.Dim));
            context.DrawText(label, new Point(X(at) - (label.Width / 2), bottom + 4));
        }
    }
}

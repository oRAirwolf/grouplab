using System.Text.RegularExpressions;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 141 section 5.2.6: the charts read as one set, not as four people's work.
/// <para>
/// <b>Why this reads the source rather than the pixels.</b> Two charts drawn in different colours are not wrong on any screen a test can
/// assert about; they are wrong together, across screens a person visits minutes apart, and by then nothing fails. A shooter who has learnt
/// that the teal bar is the interval on Compare loads has to learn it again on Session records if the next chart picks a different colour,
/// and the only place that decision is visible is the line that draws it.
/// </para>
/// <para>
/// So: every chart that draws a measurement with an interval draws the interval in teal and the measurement in the impact colour, and takes
/// every text size from the scale. <see cref="TypeScaleTests"/> holds the sizes for the whole application; this holds the one convention
/// that makes the charts a set.
/// </para>
/// </summary>
public class ChartConsistencyTests
{
    /// <summary>The charts that draw a measurement with an interval around it, entry 141 sections 5.2.2 to 5.2.6.</summary>
    private static readonly string[] DotAndWhisker = ["IntervalChart.cs", "SessionsOverTime.cs"];

    /// <summary>Every chart control, including the ones with no interval to draw.</summary>
    private static readonly string[] EveryChart =
        ["IntervalChart.cs", "SessionsOverTime.cs", "SpreadStrips.cs", "ShotOrderChart.cs", "VelocityStrip.cs", "CompositePlot.cs"];

    private static string Read(string name) =>
        File.ReadAllText(Path.Combine(TypeScaleTests.AppSource, name));

    [Fact]
    public void EveryDotAndWhiskerChartDrawsTheIntervalInTealAndTheMeasurementInTheImpactColour()
    {
        foreach (string name in DotAndWhisker)
        {
            string text = Read(name);
            Assert.True(text.Contains("Marks.Teal", StringComparison.Ordinal), $"{name} draws no interval in teal");
            Assert.True(text.Contains("Marks.Dot(context, Marks.Impact", StringComparison.Ordinal), $"{name} does not draw its measurement in the impact colour");
            Assert.False(text.Contains("Marks.Line(context, Marks.Impact", StringComparison.Ordinal), $"{name} draws a line in the impact colour, which is the measurement's");
        }
    }

    /// <summary>
    /// No chart reaches for a colour of its own. Anything new belongs in the palette with a name, where the next chart can use the same one.
    /// </summary>
    [Fact]
    public void NoChartMixesItsOwnColour()
    {
        var literal = new Regex(@"Color\.(FromRgb|FromArgb|Parse)|new SolidColorBrush\(Color", RegexOptions.None, TimeSpan.FromSeconds(5));
        foreach (string name in EveryChart)
        {
            var found = literal.Match(Read(name));
            Assert.False(found.Success, $"{name} mixes its own colour: {found.Value}");
        }
    }

    /// <summary>Every chart's text comes from the scale, and from the one sans face the rest of the application uses.</summary>
    [Fact]
    public void EveryChartTakesItsTextFromTheScale()
    {
        foreach (string name in EveryChart)
        {
            string text = Read(name);
            if (!text.Contains("FormattedText", StringComparison.Ordinal))
            {
                continue;
            }

            Assert.True(
                text.Contains("Tokens.DetailSize", StringComparison.Ordinal) || text.Contains("Tokens.SecondarySize", StringComparison.Ordinal),
                $"{name} draws text at a size that is not on the scale");
            Assert.True(
                text.Contains("Tokens.Sans", StringComparison.Ordinal) || text.Contains("Tokens.Mono", StringComparison.Ordinal),
                $"{name} draws text in a face that is not the application's");
        }
    }
}

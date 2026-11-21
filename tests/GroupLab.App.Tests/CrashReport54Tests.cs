using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using GroupLab.App;
using GroupLab.App.Theme;

namespace GroupLab.App.Tests;

/// <summary>
/// Error report 54 (Discord, nightly 236 on a Mac): the Session records date column was a fixed 96 pixels, so a date with its time,
/// "2026-11-15, 21:06", was cut off, and the mean radius with its interval was cut off at 220. Both columns now take their widest text.
/// </summary>
public class CrashReport54Tests
{
    private static double ColumnWidth(string columns, int index) =>
        double.Parse(columns.Split(',')[index], System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>The width a cell's text asks for, measured the way the row draws it, with the cell's right margin.</summary>
    private static double Needs(string text)
    {
        var block = new TextBlock { Text = text, FontSize = Tokens.DetailSize, FontFamily = Tokens.Mono, Margin = new Thickness(0, 0, Tokens.Space8, 0) };
        block.Measure(Size.Infinity);
        return block.DesiredSize.Width;
    }

    [AvaloniaFact]
    public void ADateWithItsTimeFitsItsColumn()
    {
        string columns = MainWindow.SessionColumnsFor(["2026-11-15, 21:06", "2026-09-24"], ["0.515 in (0.432 to 0.637)"]);
        Assert.True(ColumnWidth(columns, 0) >= Needs("2026-11-15, 21:06"), columns);
        Assert.True(ColumnWidth(columns, 6) >= Needs("0.515 in (0.432 to 0.637)"), columns);
    }

    [AvaloniaFact]
    public void ShortTextKeepsTheOldWidthsAndLongTextIsCapped()
    {
        string plain = MainWindow.SessionColumnsFor(["2026-09-24"], ["0.5 in"]);
        Assert.Equal(96, ColumnWidth(plain, 0));
        Assert.Equal(220, ColumnWidth(plain, 6));

        // A shot date typed by hand can be any length; the sheet's name keeps its room and the rest is trimmed as before.
        string typed = MainWindow.SessionColumnsFor([new string('x', 200)], [new string('9', 200)]);
        Assert.Equal(200, ColumnWidth(typed, 0));
        Assert.Equal(340, ColumnWidth(typed, 6));
        Assert.Equal("*", typed.Split(',')[1]);
    }
}

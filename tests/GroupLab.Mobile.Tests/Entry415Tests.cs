using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App.Theme;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 415 section 1, Alan: the iPhone follows its own text size setting (Dynamic Type), as Android does since entry
/// 411. iOS grows body text by one factor for every size; GroupLab follows it through every ordinary size and holds the accessibility sizes
/// at Android's largest, size by size, so the iPhone's largest is exactly the size <see cref="QualitySweepTests"/> and
/// <see cref="MobileSweepTests"/> already hold at 360 wide, narrower than any iPhone that runs iOS 26 (375).
/// </summary>
public class Entry415Tests
{
    // iOS's body text, 17 points at the default size: xSmall 14, xxxLarge 23, and the accessibility sizes up to AX5's 53.
    private const double XSmall = 14.0 / 17;
    private const double XxxLarge = 23.0 / 17;
    private const double Ax5 = 53.0 / 17;

    // Every size the phone's styles and screens set: the tokens and the few written in code (44, 30, 12).
    private static readonly double[] Sizes =
    [
        Tokens.DetailSize, 12, Tokens.LabelSize, Tokens.HeadingSize, Tokens.TitleSize, Tokens.ValueSize, Tokens.LeadValueSize, 30, 44,
    ];

    [Fact]
    public void ThroughTheOrdinarySizesEverySizeFollowsIosFactor()
    {
        // Text up to a value's size follows iOS's own factor exactly at every ordinary size.
        foreach (double factor in new[] { XSmall, 1.0, 19.0 / 17, 21.0 / 17, XxxLarge })
        {
            Assert.All(Sizes.Where(s => s <= Tokens.ValueSize), s => Assert.Equal(s * factor, TextSize.ByFactor(factor)(s), 6));
        }

        // Android's largest grows large text far less than small (30 to 38, 1.27 times), so iOS's xxxLarge, 1.35 times, already passes it
        // for the lead figure and the two large percentages, and those are held there.
        Assert.Equal(new[] { Tokens.LeadValueSize, 30, 44 }, Sizes.Where(s => s * XxxLarge > TextSize.AndroidLargest(s)));
        Assert.All(Sizes, s => Assert.Equal(Math.Min(s * XxxLarge, TextSize.AndroidLargest(s)), TextSize.ByFactor(XxxLarge)(s), 6));
    }

    [Fact]
    public void TheiPhonesLargestIsAndroidsLargestSizeBySize()
    {
        Assert.All(Sizes, s => Assert.Equal(TextSize.AndroidLargest(s), TextSize.ByFactor(Ax5)(s), 6));

        // The cap holds from where iOS passes Android, and above it nothing grows further.
        Assert.All(Sizes, s => Assert.True(TextSize.ByFactor(Ax5)(s) <= TextSize.ByFactor(2 * Ax5)(s) + 1e-9 && TextSize.ByFactor(2 * Ax5)(s) <= TextSize.AndroidLargest(s) + 1e-9));
        Assert.All(Sizes, s => Assert.True(TextSize.ByFactor(XxxLarge)(s) <= TextSize.ByFactor(Ax5)(s)));
    }

    [Fact]
    public void AFactorThatIsNoNumberLeavesTheSizesAlone()
    {
        Assert.All(new[] { 0, -1, double.NaN, double.PositiveInfinity }, f => Assert.Equal(Tokens.TitleSize, TextSize.ByFactor(f)(Tokens.TitleSize)));
    }

    /// <summary>
    /// Entry 413's rule on the iPhone too: at its largest size the bar shows its icons alone, from an iPhone SE's width to the largest
    /// iPhone's, which the simulator's sweep may be given.
    /// </summary>
    [AvaloniaTheory]
    [InlineData(375)]
    [InlineData(402)]
    [InlineData(440)]
    public void AtTheiPhonesLargestTheBarShowsItsIconsAlone(int width)
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        GroupLab.Mobile.Dev.Scenario.AnswerFirstRun(Phone.Settings);
        var window = new Window { Width = width, Height = 800, Content = new Shell() };
        window.Show();
        try
        {
            ((Shell)window.Content).Show(Shell.Place.Targets);
            Dispatcher.UIThread.RunJobs();
            var bar = window.GetVisualDescendants().OfType<NavBar>().Single();
            Assert.False(bar.IconsOnly);

            TextSize.Follow(TextSize.ByFactor(Ax5));
            Dispatcher.UIThread.RunJobs();
            Assert.True(bar.IconsOnly);

            TextSize.Follow(size => size);
            Dispatcher.UIThread.RunJobs();
            Assert.False(bar.IconsOnly);
        }
        finally
        {
            TextSize.Follow(size => size);
            window.Close();
        }
    }
}

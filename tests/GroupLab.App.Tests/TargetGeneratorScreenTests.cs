using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 226 section 4: the target generator on the Targets screen. The distance, the lowest magnification and the
/// shots make a sheet sized for the optic, the screen says why, and a set of several sheets previews each one; a request no page can hold
/// says what would fit instead of leaving nothing and no reason.
/// </summary>
public class TargetGeneratorScreenTests
{
    private static PrintPanel Window()
    {
        var window = TargetsScreen.Open();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    [AvaloniaFact]
    public void TwentyFiveShotsAtTenPowerMakeOneSheetAndSayWhy()
    {
        var window = Window();
        var made = window.Generate("100", "10", "", 25);
        Dispatcher.UIThread.RunJobs();

        Assert.NotNull(made?.Design);
        Assert.NotNull(window.Designed);
        Assert.Equal(25, window.Designed!.Definition.Bulls.Count(b => b.Scoring));
        Assert.NotNull(window.PreviewSource);
        Assert.Contains("arcminutes", window.Designed.Definition.Name + " " + string.Join(" ", made!.Explanation), StringComparison.Ordinal);
        window.Close();
    }

    [AvaloniaFact]
    public void LowPowerNeedsSeveralSheetsAndEachIsAPage()
    {
        var window = Window();
        var made = window.Generate("100", "4", "", 25);
        Dispatcher.UIThread.RunJobs();

        Assert.True(made!.Sheets > 1);
        Assert.Equal(made.Sheets, window.Designed!.Sheets);
        window.Turn(1);
        Dispatcher.UIThread.RunJobs();
        Assert.NotNull(window.PreviewSource);
        window.Close();
    }

    [AvaloniaFact]
    public void ARequestNoPageHoldsSaysWhatWouldFit()
    {
        var window = Window();
        var made = window.Generate("50", "1", "2", 10);
        Dispatcher.UIThread.RunJobs();

        Assert.Null(made!.Design);
        Assert.Null(window.Designed);
        Assert.Contains(made.Explanation, s => s.Contains("larger page", StringComparison.Ordinal));
        window.Close();
    }

    /// <summary>Entry 226 section 5: a roll sheet says what a phone gives it whole; a tiled target offers its sheets on one page with cut lines.</summary>
    [AvaloniaFact]
    public void ALargeSheetSaysHowItCanBeReadAndATiledOneOffersCutLines()
    {
        var window = Window();
        window.Select("GL-LR300-R42.gltd.json");
        Dispatcher.UIThread.RunJobs();
        Assert.Contains("50 MP", window.SummaryText, StringComparison.Ordinal);
        Assert.False(window.OneSheetOffered);

        window.Select("GL-LR300-T.gltd.json");
        Dispatcher.UIThread.RunJobs();
        Assert.True(window.OneSheetOffered);
        Assert.Contains("cut lines", window.SummaryText, StringComparison.Ordinal);

        window.Select("GL-CF25-LTR.gltd.json");
        Dispatcher.UIThread.RunJobs();
        Assert.DoesNotContain("flatbed", window.SummaryText, StringComparison.Ordinal);
        window.Close();
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 243 section 4 items 3 and 4: C's diamond is offered in Made for your optic and in the designer, and E's
    /// disc in the designer, each beside the rings and neither in their place.
    /// </summary>
    [AvaloniaFact]
    public void TheDiamondAndEAreOfferedBesideTheRings()
    {
        var window = Window();
        var made = window.Generate("100", "10", "", 25, diamond: true);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(GroupLab.Core.Gltd.Model.DiscShape.Square, made!.Design!.Definition!.RingSets.Single().Discs[0].Shape);
        Assert.Equal(GroupLab.Core.Gltd.Model.DiscShape.Square, window.Designed!.Definition.RingSets.Single().Discs[0].Shape);

        window.ChooseBull(2);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(GroupLab.Core.Gltd.Model.DiscShape.Square, window.Designed!.Definition.RingSets.Single().Discs[0].Shape);
        window.ChooseBull(1);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal([91, 25], window.Designed!.Definition.RingSets.Single().Discs.Skip(1).Select(d => d.Diameter));
        window.ChooseBull(0);
        Dispatcher.UIThread.RunJobs();
        Assert.True(window.Designed!.Definition.RingSets.Single().Discs.Count > 3, "the rings are still the default");
        window.Close();
    }
}

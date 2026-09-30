using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Cli.Library;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 303 item 1: mil bulls are an option of the designer, not new built-in sheets. A bull's size can be given in
/// inches, MOA or mil, the angles at the sheet's distance.
/// </summary>
public class Entry303Tests
{
    [Fact]
    public void ASizeInMilOrMoaIsReadAtTheDistance()
    {
        Assert.Equal(3.6, ParametricSheet.BullInches(1, "mil", 100)!.Value, 6);
        Assert.Equal(7.2, ParametricSheet.BullInches(1, "mil", 200)!.Value, 6);
        Assert.Equal(2.0944, ParametricSheet.BullInches(2, "MOA", 100)!.Value, 4);
        Assert.Equal(2.5, ParametricSheet.BullInches(2.5, "in", null)!.Value, 6);
        Assert.Null(ParametricSheet.BullInches(1, "mil", null));
        Assert.Null(ParametricSheet.BullInches(0, "in", null));
    }

    [AvaloniaFact]
    public void TheDesignerMakesAnEBullOfAQuarterMilAtTheSheetsDistance()
    {
        var panel = TargetsScreen.Open();
        try
        {
            panel.SetDesign("letter", 5, 5, "1.50", 254, 0, false, distance: "100");
            panel.SetBullSize("0.25", "mil", 1);
            Dispatcher.UIThread.RunJobs();
            Assert.Contains(panel.DesignChecks, c => c.Text.StartsWith("0.25 mil at 100 yd is 0.90 in across", StringComparison.Ordinal));
            var definition = panel.Designed!.Definition;
            Assert.Equal(229, definition.RingSets.Single().Discs.Max(d => d.Diameter));

            // A size in mil with no distance says what it needs and leaves nothing to print.
            panel.SetDesign("letter", 5, 5, "1.50", 254, 0, false, distance: "");
            panel.SetBullSize("0.25", "mil", 1);
            Assert.Null(panel.Designed);
            Assert.Contains(panel.DesignChecks, c => c.Text.Contains("distance", StringComparison.Ordinal));
        }
        finally
        {
            panel.Close();
        }
    }
}

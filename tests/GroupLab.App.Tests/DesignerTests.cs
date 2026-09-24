using System.Runtime.CompilerServices;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Rendering;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entries 99 and 100: the parametric editor in the print screen. A layout that works becomes the selected sheet and
/// saves like a library sheet; a tight spacing is a warning in amber with its number and no verdict; a layout that cannot work is refused in
/// red and leaves nothing to print.
/// </summary>
public class DesignerTests
{
    private static string Repository([CallerFilePath] string here = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));

    private static PrintPanel Window()
    {
        var window = TargetsScreen.Open();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    [AvaloniaFact]
    public void AWorkingLayoutBecomesTheSheetAndSavesAsAPdf()
    {
        var window = Window();
        window.SetDesign("letter", 6, 5, "1.20", 222, 0, false);
        Dispatcher.UIThread.RunJobs();

        Assert.NotNull(window.Designed);
        Assert.Equal(30, window.Designed!.Definition.Bulls.Count);
        Assert.DoesNotContain(window.DesignChecks, c => c.Kind == "error");
        Assert.Contains(window.DesignChecks, c => c.Text.StartsWith("Print at actual size", StringComparison.Ordinal));
        Assert.NotNull(window.PreviewSource);

        string path = Path.Combine(Path.GetTempPath(), $"grouplab-design-{Guid.NewGuid():N}.pdf");
        try
        {
            Assert.True(window.SavePdf(path));
            Assert.True(new FileInfo(path).Length > 1000);
        }
        finally
        {
            GroupLab.Tests.Support.Temp.DeleteFile(path);
            window.Close();
        }
    }

    /// <summary>Entry 99 section 4 item 1's own example, a one-inch rifle on a tight grid: amber, with the number, and the sheet still printable.</summary>
    [AvaloniaFact]
    public void ATightSpacingForTheStatedGroupIsAnAmberWarningAndStillPrints()
    {
        var window = Window();
        window.SetDesign("letter", 6, 6, "1.00", 127, 0, false, group: "1.0", distance: "100");
        Dispatcher.UIThread.RunJobs();

        var warning = Assert.Single(window.DesignChecks, c => c.Kind == "warning");
        Assert.Contains("would land nearer a neighbouring bull than its own", warning.Text, StringComparison.Ordinal);
        Assert.NotNull(window.Designed);
        window.Close();
    }

    [AvaloniaFact]
    public void ALayoutThatCannotWorkIsRefusedInRedAndLeavesNothingToPrint()
    {
        var window = Window();
        window.SetDesign("letter", 5, 9, "1.50", 254, 3, true);
        Dispatcher.UIThread.RunJobs();

        Assert.Contains(window.DesignChecks, c => c.Kind == "error" && c.Text.Contains("more height than a letter page has", StringComparison.Ordinal));
        Assert.Null(window.Designed);
        Assert.Null(window.PreviewSource);
        window.Close();
    }
}

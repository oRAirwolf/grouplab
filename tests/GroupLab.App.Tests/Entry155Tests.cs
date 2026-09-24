using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.App.Theme;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 155: the target library and the print window did the same job, so they are one screen, Targets. The library's
/// grouped list survives; choosing a sheet fills its print panel beside it at once, and nothing the print window did is lost.
/// </summary>
public class Entry155Tests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    /// <summary>Section 1: choosing a sheet in the list fills the panel and the preview, with no second window.</summary>
    [AvaloniaFact]
    public void ChoosingASheetFillsItsPrintPanelAtOnce()
    {
        var panel = TargetsScreen.Open(1280, 720);
        var main = (MainWindow)TopLevel.GetTopLevel(panel)!;
        var tiled = panel.Sheets.First(s => s.File == "GL-LR300-T.gltd.json");
        main.ChooseLibrarySheet(tiled.Definition.Name);
        Settle();
        Assert.Same(tiled, main.LibrarySelected);
        Assert.NotNull(panel.PreviewSource);
        Assert.Equal(4, panel.Render()!.Pages.Count);
        Assert.Contains(panel.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text == tiled.Definition.Name);
        panel.Close();
    }

    /// <summary>Section 2: everything the print window did is on the merged screen, and a design opens in the same panel.</summary>
    [AvaloniaFact]
    public void NothingThePrintWindowDidIsLost()
    {
        var panel = TargetsScreen.Open(1280, 720);
        panel.Select("GL-CF25-LTR-D.gltd.json");
        Settle();
        var buttons = panel.GetLogicalDescendants().OfType<Button>().Select(b => b.Content as string).Where(c => c is not null).ToList();
        foreach (string action in new[] { "Open to print", "Save PDF…", "Print a volunteer pack", "Previous sheet", "Next sheet" })
        {
            Assert.Contains(action, buttons);
        }

        Assert.Equal(OperatingSystem.IsWindows(), buttons.Contains("Print…"));
        var texts = panel.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToList();
        Assert.Contains(PrintPanel.ScaleWords, texts);
        Assert.Equal(OperatingSystem.IsWindows(), texts.Contains(PrintPanel.UnconfirmedWords));
        var toggles = panel.GetLogicalDescendants().OfType<Avalonia.Controls.Primitives.ToggleButton>().Select(t => t.Content as string).ToList();
        Assert.Contains("Print the actual-size instruction along the bottom of each sheet", toggles);
        Assert.Contains("Blank, to write on at the range", toggles);
        Assert.Contains("Filled in now, from these fields", toggles);

        // The designer opens in the same panel, from the screen's own button.
        var main = (MainWindow)TopLevel.GetTopLevel(panel)!;
        var design = main.GetLogicalDescendants().OfType<Button>().Single(b => b.Content as string == "Design your own sheet" && Entry109Tests.Shown(b));
        design.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Settle();
        Assert.NotEmpty(panel.DesignChecks);
        panel.Close();
    }

    /// <summary>Section 6.3: no code path opens a separate print window. The type is a panel, and nothing in the application is a print window.</summary>
    [AvaloniaFact]
    public void NoPrintWindowExists()
    {
        Assert.True(typeof(PrintPanel).IsSubclassOf(typeof(UserControl)));
        Assert.DoesNotContain(typeof(MainWindow).Assembly.GetTypes(), t => t.IsSubclassOf(typeof(Window)) && t.Name.Contains("Print", StringComparison.Ordinal));

        var panel = TargetsScreen.Open(1280, 720);
        var main = (MainWindow)TopLevel.GetTopLevel(panel)!;
        main.ShowLibrary(false);
        Assert.Same(panel, main.PrintFromLibrary());
        Settle();
        Assert.True(main.ShowingLibrary);
        Assert.Same(main, TopLevel.GetTopLevel(panel));
        panel.Close();
    }
}

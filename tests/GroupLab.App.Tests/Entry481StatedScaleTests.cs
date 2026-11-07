using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.Core.Marking;
using GroupLab.Tests.Support;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 481 part E, question 108: a flatbed scan with no scale offers the resolution its file states under the scale
/// tools, with the number shown; accepting it sets the scale and goes on to the point of aim, and nothing is set until it is accepted.
/// </summary>
public class Entry481StatedScaleTests
{
    [AvaloniaFact]
    public void AScanOffersItsOwnResolutionAsTheScaleOnlyWhenAsked()
    {
        string folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"grouplab-481e-{Guid.NewGuid():N}")).FullName;
        var store = new AppSettingsStore(Path.Combine(folder, "settings.json"));
        store.SaveUnits(UnitSettings.Imperial);
        var window = new MainWindow(store) { Width = 1400, Height = 900, DetectOnOpen = false };
        window.Show();
        try
        {
            window.OpenImage(StatedDpiPng.Write(Path.Combine(folder, "blank scan.png"), 1700, 2200, 200));
            Dispatcher.UIThread.RunJobs();
            Assert.Null(window.Session.State.Scale);

            var length = window.GetLogicalDescendants().OfType<ToggleButton>().Single(b => AutomationProperties.GetName(b) == "Scale: length");
            length.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            Assert.Contains(window.ScaleInputs.Children.OfType<TextBlock>(), t => t.Text?.StartsWith("200 dpi, stated by the file", StringComparison.Ordinal) == true);
            Assert.Null(window.Session.State.Scale);

            var use = window.ScaleInputs.GetLogicalDescendants().OfType<Button>().Single(b => b.Content as string == "Use the scan's 200 dpi as the scale");
            use.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(200, Math.Round(StatedResolutionScale.DotsPerInchOf(window.Session.State.Scale)!.Value));
            Assert.Equal(MarkingTool.Aim, window.ToolNow);
            Assert.Equal(ScaleSaid.FromScanResolution, ScaleSaid.Pill(window.Session.State.Scale));
        }
        finally
        {
            window.Close();
            Temp.Delete(folder);
        }
    }
}

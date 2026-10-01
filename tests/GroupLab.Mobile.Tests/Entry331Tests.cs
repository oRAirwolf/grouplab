using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile.Tests;

/// <summary>NOTES-FROM-PLANNING.md entry 331 section 2: the phone's chronograph page offers a file beside the box, as the desktop does.</summary>
public class Entry331Tests
{
    [AvaloniaFact]
    public void TheReadingsPageOffersAFile()
    {
        var page = VelocityPages.Chronograph(1, MarkingState.Empty, () => { });
        var ids = page.GetLogicalDescendants().OfType<Control>().Select(AutomationProperties.GetAutomationId).OfType<string>().ToList();
        Assert.Contains("chrono-import", ids);
        Assert.Contains("chrono-read", ids);
    }
}

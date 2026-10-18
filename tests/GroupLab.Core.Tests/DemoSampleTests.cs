using GroupLab.Core.Detection;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 446 section 2: the demo's sample is made in one place, so the desktop and both phones make the same sheet:
/// the 25-bull Letter sheet at 300 dpi, the same pixels every time.
/// </summary>
public class DemoSampleTests
{
    [Fact]
    public void TheSampleIsTheSameSheetEveryTime()
    {
        var definition = BuiltIns.Load(DemoSample.SheetFile);
        var first = DemoSample.Make(definition);
        var second = DemoSample.Make(definition);
        Assert.Equal((2550, 3300), (first.Width, first.Height));
        Assert.Equal(first.Pixels, second.Pixels);
        Assert.Equal(25, definition.Bulls.Count(b => b.Scoring));
    }
}

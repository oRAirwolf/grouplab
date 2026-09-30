using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Rendering;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 297: every sheet on the Targets screen, built-in or designed, offers its bulls in black, blue or red; the
/// preview and the PDF follow the choice at once, and the choice is remembered for that sheet.
/// </summary>
public class Entry297Tests
{
    [AvaloniaFact]
    public void TheBullColorReachesThePreviewAndThePdfAndIsRememberedPerSheet()
    {
        var panel = TargetsScreen.Open();
        try
        {
            panel.Select("GL-CF25-LTR.gltd.json");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(BullColour.Black, panel.Colour);

            panel.ChooseColour(BullColour.Red);
            Dispatcher.UIThread.RunJobs();
            var pages = panel.Render()!.Pages;
            Assert.Contains(pages[0].Items, i => i.Layer == SceneLayer.Bulls && i.Colour == BullColours.Of(BullColour.Red));
            Assert.All(pages[0].Items.Where(i => i.Layer is SceneLayer.Markers or SceneLayer.Codes), i => Assert.Equal(new Rgb(0, 0, 0), i.Colour));
            Assert.IsType<Bitmap>(panel.PreviewSource);

            // Another sheet starts black, and the first comes back red.
            panel.Select("GL-CF25-LTR-C.gltd.json");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(BullColour.Black, panel.Colour);
            panel.Select("GL-CF25-LTR.gltd.json");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(BullColour.Red, panel.Colour);
        }
        finally
        {
            panel.Close();
        }
    }
}

using System.Text;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Rendering;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 358 section 2: on the Targets screen, a thermal printer chosen under "Print on" turns the preview into the
/// one-bit image as it will print, takes the bull colors away, says before printing what a narrow head would cut off, and saves the PDF as
/// the dots at actual size.
/// </summary>
public class Entry358ThermalTests
{
    [AvaloniaFact]
    public void AThermalPrinterShowsTheDotsSaysWhatIsCutOffAndSavesTheDots()
    {
        var panel = TargetsScreen.Open();
        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-thermal-{Guid.NewGuid():N}");
        try
        {
            DotPreview? dots = null;
            panel.DotsShown += d => dots = d;
            panel.Select("GL-CF25-LTR.gltd.json");
            Dispatcher.UIThread.RunJobs();
            Assert.Null(panel.Head);
            Assert.Null(panel.ThermalShown);

            panel.ChoosePrintOn(1);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(203.2, panel.Head!.DotsPerInch);
            var print = Assert.IsType<GroupLab.Core.Printing.Thermal.ThermalPage>(panel.ThermalShown);
            Assert.NotNull(dots);
            Assert.Equal(832, print.Image.Width);
            Assert.Contains("would be cut off", panel.StatusText, StringComparison.Ordinal);
            Assert.Equal(StatusKind.Alert, panel.StatusState);
            Assert.All(panel.Render()!.Pages[0].Items.Where(i => i.Layer == SceneLayer.Bulls), i => Assert.Equal(new GroupLab.Core.Gltd.Binary.Rgb(0, 0, 0), i.Colour));

            string pdf = Path.Combine(folder, "thermal.pdf");
            Assert.True(panel.SavePdf(pdf));
            string text = Encoding.Latin1.GetString(File.ReadAllBytes(pdf));
            Assert.Contains("/BitsPerComponent 1", text, StringComparison.Ordinal);
            Assert.Contains("/PrintScaling /None", text, StringComparison.Ordinal);

            // An office printer again: the vectors come back.
            panel.ChoosePrintOn(0);
            Dispatcher.UIThread.RunJobs();
            Assert.Null(dots);
            Assert.Null(panel.ThermalShown);
        }
        finally
        {
            panel.Close();
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }
    }
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 363 section 2a: Save for a printer app writes a picture and a PDF of the sheet at 300 dpi, named so they
/// cannot be mistaken for an office printer's PDF, and the darkness test page beside it; the Phomemo M834 is a choice of its own.
/// </summary>
public class Entry363PrinterAppTests
{
    [AvaloniaFact]
    public void SaveForAPrinterAppWritesThePictureAndThePdfAndTheDarknessPage()
    {
        var panel = TargetsScreen.Open();
        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-printer-app-{Guid.NewGuid():N}");
        try
        {
            panel.Select("GL-SCALE-LTR-1.gltd.json");
            Dispatcher.UIThread.RunJobs();
            Assert.Contains(PrintPanel.ThermalChoices, c => c.Words.StartsWith("Phomemo M834, 300 dpi", StringComparison.Ordinal) && c.Head.DotsPerInch == 300);

            Assert.True(panel.SaveForApp(Path.Combine(folder, "GL-SCALE-LTR-1-thermal-300dpi.png")));
            byte[] png = File.ReadAllBytes(Path.Combine(folder, "GL-SCALE-LTR-1-thermal-300dpi.png"));
            Assert.Equal(300, GroupLab.Core.Printing.Thermal.ThermalPng.Dpi(png)!.Value, 1);
            Assert.True(File.Exists(Path.Combine(folder, "GL-SCALE-LTR-1-thermal-300dpi.pdf")));
            Assert.Contains("never fit to page", panel.StatusText, StringComparison.Ordinal);

            Assert.True(panel.SaveForApp(Path.Combine(folder, "darkness-test-thermal-300dpi.png"), darkness: true));
            Assert.True(File.Exists(Path.Combine(folder, "darkness-test-thermal-300dpi.pdf")));
        }
        finally
        {
            panel.Close();
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }
    }
}

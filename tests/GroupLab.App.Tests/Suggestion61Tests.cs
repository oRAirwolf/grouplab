using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Core.Reporting;
using GroupLab.Core.Statistics;

namespace GroupLab.App.Tests;

/// <summary>
/// Suggestion 61 (Discord, Alan's yes on 2026-11-24): Compare loads on the computer has Export, a PDF of the comparison or a picture of the
/// same page, made from the screen's own loads, words and footing.
/// </summary>
public sealed class Suggestion61Tests
{
    [AvaloniaFact]
    public void CompareLoadsExportsAPdfAndAPictureOfTheSamePage()
    {
        var (window, path, _) = Entry109Tests.Sheet(1280, 720);
        string folder = Path.GetDirectoryName(path)!;
        try
        {
            window.Session.SetCalibre(Calibre.Of(0.308));
            window.Session.SetShotDistance(3600);
            window.Session.SetEquipment(new Rifle("Tikka T3x", 0.25, AngularUnit.Moa), null, "H4350 41.5");
            window.CalibreAnswered();
            window.Analyse();
            Dispatcher.UIThread.RunJobs();
            long first = window.CurrentSession!.Value;
            long second = window.Sessions!.Save(window.Sessions.Get(first)! with { Id = 0, CreatedUtc = "2099-01-01T00:00:00Z", ShotDate = "2099-01-01", Load = "Factory 150" });
            window.ChooseSession(first, true);
            window.ChooseSession(second, true);
            window.CompareChosen();
            Dispatcher.UIThread.RunJobs();

            Assert.Contains(window.GetLogicalDescendants().OfType<Button>(), b => b.Content as string == "Export");
            var page = window.BuildComparisonPage()!;
            Assert.Equal(["H4350 41.5", "Factory 150"], page.Loads.Select(l => l.Name));
            Assert.StartsWith("2 loads, ", page.Summary, StringComparison.Ordinal);
            Assert.Equal(window.Comparison!.Headline, page.Report.Headline);
            Assert.EndsWith(ComparisonPages.Branding, page.Footer, StringComparison.Ordinal);

            string pdf = Path.Combine(folder, "comparison.pdf"), png = Path.Combine(folder, "comparison.png");
            window.WriteComparison(pdf, picture: false);
            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(pdf), 0, 4));
            window.WriteComparison(png, picture: true);
            Assert.Equal([0x89, (byte)'P', (byte)'N', (byte)'G'], File.ReadAllBytes(png).Take(4));
            Assert.Equal("Comparison picture saved to " + png, window.StatusText);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(folder);
        }
    }
}

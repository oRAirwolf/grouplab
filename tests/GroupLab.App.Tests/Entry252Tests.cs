using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Statistics;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 252 section 3, suggested by Jylee: "Shots Needed to Zero" in the analysis screen's full figures, beside the
/// full CEP table. It takes the analyzed group and the rifle's click value, works the answer out off the interface thread, and says the
/// shots for 90, 95 and 99 percent on both goals, with the trials and the seed, or that it is exact.
/// </summary>
public class Entry252Tests
{
    private static void Settle()
    {
        Dispatcher.UIThread.RunJobs();
    }

    private static MainWindow Analysed(Rifle rifle)
    {
        var (window, _, _) = Entry109Tests.Sheet();
        window.Book = RecordBook.Empty.With(rifle);
        window.Session.SetShotDistance(3600);
        window.Session.SetEquipment(rifle, null, null);
        window.CalibreAnswered();
        window.Analyse();
        Settle();
        window.FullFiguresPanel.IsExpanded = true;
        Settle();
        return window;
    }

    private static List<string> Answer(MainWindow window)
    {
        window.ShotsToZeroWork.Wait(TimeSpan.FromSeconds(30));
        Settle();
        return [.. window.ShotsToZeroText];
    }

    [AvaloniaFact]
    public void ItSaysTheShotsForEachGoalFromTheRiflesClickValue()
    {
        var window = Analysed(new Rifle("Tikka T3x", 0.25, AngularUnit.Moa));
        try
        {
            var text = Answer(window);
            Assert.Contains(window.FullFiguresText, t => t == "Shots Needed to Zero");
            Assert.Contains(text, t => t.StartsWith("From the group's sigma of ", StringComparison.Ordinal) && t.Contains("clicks of 0.25 MOA, from Tikka T3x", StringComparison.Ordinal));
            Assert.Contains(text, t => t.StartsWith("On the closest click, both axes: 90 percent at ", StringComparison.Ordinal));
            Assert.Contains(text, t => t.StartsWith("Within 1 click, both axes: 90 percent at ", StringComparison.Ordinal));
            Assert.Contains(text, t => t.StartsWith("4,000 trials of the sigma the group could really have, seed 41", StringComparison.Ordinal));

            // Photographed for a person to look at, as the analysis screen's own tests do; nothing is asserted about the pixels.
            window.Width = 1400;
            window.Height = 1400;
            window.AdvancedPanel.IsExpanded = true;
            Settle();
            window.BringShotsToZeroIntoView();
            Settle();
            string output = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "out", "screens"));
            Directory.CreateDirectory(output);
            window.CaptureRenderedFrame()!.Save(Path.Combine(output, "shots-to-zero.png"), new Avalonia.Media.Imaging.PngBitmapEncoderOptions());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ARifleWithNoClickValueAsksForOne()
    {
        var window = Analysed(new Rifle("No scope yet", 0, AngularUnit.Moa));
        try
        {
            var text = Answer(window);
            Assert.Contains("Your scope's click", text);
            Assert.Contains(text, t => t.StartsWith("On the closest click, both axes: ", StringComparison.Ordinal));
            // Entry 294 section 1: the rifle's scope is in MOA, so the click offered is its usual quarter minute.
            Assert.Contains(text, t => t.Contains("clicks of 0.25 MOA", StringComparison.Ordinal));
        }
        finally
        {
            window.Close();
        }
    }
}

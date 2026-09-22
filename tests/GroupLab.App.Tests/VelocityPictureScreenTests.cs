using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 141 section 5.2.5 on the screen it lives on: the velocity picture in the Ballistics screen's chronograph
/// section, drawn from the string in hand.
/// <para>
/// <b>What is held here is that it appears only where there is something to draw</b>, and that the caption on the screen is the same one
/// the control's own tests hold, rather than a second wording that could drift away from it.
/// </para>
/// </summary>
public class VelocityPictureScreenTests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    [AvaloniaFact]
    public void ThePictureAppearsOnceAStringIsReadAndCarriesTheSdsOwnRange()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.Book = RecordBook.Empty.With(new Load("H4350 41.5", null));
            window.Session.SetShotDistance(3600);
            window.Session.SetEquipment(null, null, "H4350 41.5");
            window.CalibreAnswered();
            window.Analyse();
            window.ShowBallistics();
            Settle();

            // Nothing read yet: no picture at all, rather than an empty one.
            Assert.Empty(window.GetLogicalDescendants().OfType<VelocityStrip>());
            Assert.Equal("", window.VelocityPictureText);

            int shots = window.Session.State.Shots.Count(s => s.IsShot);
            var readings = Enumerable.Range(0, shots).Select(i => 2700 + ((i * 7) % 23)).ToList();
            window.ReadChronograph(list: string.Join(", ", readings));
            Settle();

            var strip = Assert.Single(window.GetLogicalDescendants().OfType<VelocityStrip>());
            Assert.Equal(shots, strip.VelocitiesFps.Count);

            string said = window.VelocityPictureText;
            Assert.Contains("How much do these shots vary in velocity?", said, StringComparison.Ordinal);
            Assert.Contains(strip.Description, said, StringComparison.Ordinal);
            Assert.Matches(@"SD \d+\.\d ft/s \(\d+\.\d ft/s to \d+\.\d ft/s\)", said);
            Assert.Contains("grows with the number of shots on its own", said, StringComparison.Ordinal);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>A single reading is not a spread, so there is nothing to draw and nothing is drawn.</summary>
    [AvaloniaFact]
    public void OneReadingDrawsNothing()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.Book = RecordBook.Empty.With(new Load("H4350 41.5", null));
            window.Session.SetEquipment(null, null, "H4350 41.5");
            window.CalibreAnswered();
            window.Analyse();
            window.ShowBallistics();
            window.ReadChronograph(list: "2705");
            Settle();

            Assert.Empty(window.GetLogicalDescendants().OfType<VelocityStrip>());
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}

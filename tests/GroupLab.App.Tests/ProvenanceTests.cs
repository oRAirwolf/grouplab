using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App.Theme;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 46 section 3: shot provenance stays off the image and is in the panel, as a count line above the figures
/// and a faint word on each row of the shot list, because it is the record of where a human judgement entered the measurement.
/// </summary>
public sealed class ProvenanceTests : IDisposable
{
    private readonly string root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"grouplab-provenance-{Guid.NewGuid():N}")).FullName;

    public void Dispose()
    {
        Directory.Delete(root, recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void TheCountLineNamesOnlyTheKindsOfPlacementPresent()
    {
        Assert.Equal("12 shots: 9 detected, 2 corrected, 1 placed by hand", MainWindow.PlacedLine(12, 9, 2, 1));
        Assert.Equal("5 shots: 5 placed by hand", MainWindow.PlacedLine(5, 0, 0, 5));
        Assert.Equal("1 shot: 1 detected", MainWindow.PlacedLine(1, 1, 0, 0));
    }

    [AvaloniaFact]
    public void ThePanelCountsDetectedCorrectedAndHandPlacedShotsAndEachRowSaysWhich()
    {
        string image = Path.Combine(root, "sheet.png");
        using (var mat = new OpenCvSharp.Mat(400, 700, OpenCvSharp.MatType.CV_8UC3, OpenCvSharp.Scalar.All(230)))
        {
            OpenCvSharp.Cv2.ImWrite(image, mat);
        }

        var store = new AppSettingsStore(Path.Combine(root, "settings.json"));
        store.SaveUnits(UnitSettings.Imperial);
        var window = new MainWindow(store) { Width = 1400, Height = 900 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.OpenImage(image);

        // Two detected shots, one of which the user then moves, and one placed by hand, all on one bull.
        window.Session.LoadDetections(new LengthReference(new PointD(100, 100), new PointD(300, 100), 2), [new BullAim(0, "1", new PointD(215, 205))], [(new PointD(210, 200), 0), (new PointD(225, 212), 0)], "test");
        int moved = window.Session.State.Shots.First(s => s.Provenance == ShotProvenance.Automatic).Id;
        window.Session.MoveShot(moved, new PointD(206, 198));
        window.Session.AddShot(new PointD(220, 215));
        Dispatcher.UIThread.RunJobs();

        var text = window.GetLogicalDescendants().OfType<TextBlock>().ToList();
        Assert.Contains(text, t => t.Text == "3 shots: 1 detected, 1 corrected, 1 placed by hand.");
        Assert.DoesNotContain(text, t => t.Text?.StartsWith("Placed:", StringComparison.Ordinal) == true);
        var words = text.Where(t => t.Classes.Contains(AppStyles.Faint)).Select(t => t.Text).ToList();
        Assert.Equal(["corrected", "detected", "by hand"], words);
        window.Close();
    }
}

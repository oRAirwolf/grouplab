using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Mobile;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 457 section 3, crash report 36: on the phone, a tap on the result's sheet near no hole, no aim line and no
/// bull raised "NoElements" (the nearest of none), where it should light nothing.
/// </summary>
public class Entry457Tests
{
    [AvaloniaFact]
    public void ATapNearNothingOnTheSheetLightsNothingAndRaisesNothing()
    {
        var bitmap = new WriteableBitmap(new Avalonia.PixelSize(400, 300), new Avalonia.Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        var shots = new List<MarkedShot> { new(1, new PointD(300, 200), ShotProvenance.Manual, Bull: 1) };
        var picture = new ResultView.SheetPicture(bitmap, 0, () => shots);
        var window = new Window { Width = 400, Height = 300, Content = picture };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.Null(picture.BullAt(new Avalonia.Point(5, 5)));
            Assert.Equal(1, picture.BullAt(picture.Bounds.Width > 0 ? new Avalonia.Point(picture.Bounds.Width * 0.75, picture.Bounds.Height * 0.67) : default));
        }
        finally
        {
            window.Close();
        }
    }
}

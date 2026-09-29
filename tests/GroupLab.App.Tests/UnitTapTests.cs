using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;
using OpenCvSharp;

namespace GroupLab.App.Tests;

/// <summary>NOTES-FROM-PLANNING.md entries 272 and 273: tapping a number switches its kind of unit everywhere, and remembers it.</summary>
public class UnitTapTests
{
    private static readonly BullAim[] Bulls = [new(0, "1", new PointD(200, 200)), new(1, "2", new PointD(500, 200))];

    private static readonly (int X, int Y, int Bull)[] Holes =
    [
        (205, 190, 0), (188, 207, 0), (214, 212, 0), (196, 196, 0), (230, 200, 0),
        (510, 205, 1), (492, 194, 1), (503, 214, 1), (487, 209, 1), (499, 188, 1),
    ];

    private static IEnumerable<TextBlock> Values(MainWindow window) =>
        window.GetLogicalDescendants().OfType<TextBlock>().Where(t => t.Classes.Contains(UnitTap.Value));

    [AvaloniaFact]
    public void TappingANumberSwitchesThatNumberAloneAndIsRememberedForItsFigure()
    {
        string settings = Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json");
        string path = Path.Combine(Path.GetTempPath(), $"grouplab-tap-{Guid.NewGuid():N}.png");
        var store = new AppSettingsStore(settings);
        store.SaveUnits(UnitSettings.Imperial);
        var window = new MainWindow(store) { Width = 1400, Height = 900 };
        try
        {
            window.Show();
            using (var image = new Mat(600, 800, MatType.CV_8UC3, new Scalar(235, 235, 235)))
            {
                foreach (var (x, y, _) in Holes)
                {
                    Cv2.Circle(image, new OpenCvSharp.Point(x, y), 9, new Scalar(30, 30, 30), -1);
                }

                Cv2.ImWrite(path, image);
            }

            window.OpenImage(path);
            window.Session.LoadDetections(new LengthReference(new PointD(100, 100), new PointD(300, 100), 2), Bulls,
                [.. Holes.Select(h => (new PointD(h.X, h.Y), (int?)h.Bull))], "test");
            window.Session.SetShotDistance(3600);
            Dispatcher.UIThread.RunJobs();

            // The mean radius is an angle at 100 yd, and a value that shows a unit is tappable; its label is not.
            var mean = Values(window).First(t => t.Text!.EndsWith(" MOA", StringComparison.Ordinal));
            Assert.Equal(UnitKind.Angle, UnitTap.KindOf(mean.Text));
            Assert.DoesNotContain(window.GetLogicalDescendants().OfType<TextBlock>(), t => t.Classes.Contains(UnitTap.Value) && t.Classes.Contains(TermHelp.Class));

            // Entry 280 section 1: the tap switches this number only; the setting and every other angle stay as they were.
            mean.RaiseEvent(new TappedEventArgs(InputElement.TappedEvent, null!));
            Dispatcher.UIThread.RunJobs();
            Assert.EndsWith(" mil", mean.Text, StringComparison.Ordinal);
            Assert.Equal(UnitSettings.Imperial, store.LoadUnits());
            Assert.Contains(Values(window), t => t != mean && t.Text!.EndsWith(" MOA", StringComparison.Ordinal));
            Assert.EndsWith(" now in mil · remembered", window.StatusText, StringComparison.Ordinal);
            Assert.True(store.LoadUnitTapped());

            // Remembered for that figure: the panel drawn again shows it in mil, and the others still in MOA.
            int mils = Values(window).Count(t => t.Text!.EndsWith(" mil", StringComparison.Ordinal));
            window.Session.SetShotDistance(3700);
            window.Session.SetShotDistance(3600);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(mils, Values(window).Count(t => t.Text!.EndsWith(" mil", StringComparison.Ordinal)));
            Assert.Contains(Values(window), t => t.Text!.EndsWith(" MOA", StringComparison.Ordinal));

            // A size on the paper switches that size alone, to centimeters.
            var paper = Values(window).First(t => UnitTap.KindOf(t.Text) == UnitKind.Length && UnitSwitch.SymbolIn(t.Text, UnitKind.Length) == "in");
            paper.RaiseEvent(new TappedEventArgs(InputElement.TappedEvent, null!));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("cm", UnitSwitch.SymbolIn(paper.Text, UnitKind.Length));
            Assert.Equal(LinearUnit.Inch, store.LoadUnits().Linear);
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.DeleteFile(path);
            GroupLab.Tests.Support.Temp.DeleteFile(settings);
        }
    }

    [Fact]
    public void AValueIsTappableOnlyWhereItShowsAUnit()
    {
        Assert.Equal(UnitKind.Angle, UnitTap.KindOf("0.13 MOA"));
        Assert.Equal(UnitKind.Angle, UnitTap.KindOf("0.04 mil"));
        Assert.Equal(UnitKind.Length, UnitTap.KindOf("1.234 in on the paper at 100 yd"));
        Assert.Equal(UnitKind.Length, UnitTap.KindOf("0.35 × 0.30 cm"));
        Assert.Equal(UnitKind.Distance, UnitTap.KindOf("91.4 m"));
        Assert.Null(UnitTap.KindOf("24 shots"));
        Assert.Null(UnitTap.KindOf("Mean radius"));
        Assert.Null(UnitTap.KindOf("12 minutes"));
    }
}

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Reporting;
using GroupLab.Core.Statistics;
using OpenCvSharp;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 280 section 2: row 10 of the phone parity canvas on the desktop, "the same content in the desktop's layout".
/// Shots A, Zero from this group and Share A as windows, the several aim points under the shot table, and the one-page report beside the full
/// one; each says what the phone says, from the same shared code.
/// </summary>
public class Entry280Tests
{
    private static readonly BullAim[] Bulls = [new(0, "1", new PointD(200, 200)), new(1, "2", new PointD(500, 200))];

    private static readonly (int X, int Y, int Bull)[] Holes =
    [
        (205, 190, 0), (188, 207, 0), (214, 212, 0), (196, 196, 0), (230, 200, 0),
        (510, 205, 1), (492, 194, 1), (503, 214, 1), (487, 209, 1), (499, 188, 1),
    ];

    private static void Settle() => Dispatcher.UIThread.RunJobs();

    /// <summary>A photograph of a target GroupLab did not print, two aim points placed by hand and five shots on each, 100 pixels to the inch.</summary>
    private static (MainWindow Window, string Path) Marked()
    {
        string path = Path.Combine(Path.GetTempPath(), $"grouplab-entry280-{Guid.NewGuid():N}.png");
        using (var image = new Mat(600, 800, MatType.CV_8UC3, new Scalar(235, 235, 235)))
        {
            foreach (var (x, y, _) in Holes)
            {
                Cv2.Circle(image, new OpenCvSharp.Point(x, y), 9, new Scalar(30, 30, 30), -1);
            }

            Cv2.ImWrite(path, image);
        }

        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        var window = new MainWindow(store) { Width = 1400, Height = 900 };
        window.Show();
        window.OpenImage(path);
        window.Session.LoadDetections(new LengthReference(new PointD(100, 100), new PointD(300, 100), 2), Bulls,
            [.. Holes.Select(h => (new PointD(h.X, h.Y), (int?)h.Bull))], "test");
        window.Session.Load(window.Session.State with { ShotDistanceInches = 3600, Rifle = new Rifle("Test rifle", 0.25, AngularUnit.Moa) });
        window.CalibreAnswered();
        window.Analyse();
        Settle();
        return (window, path);
    }

    private static void Done(MainWindow window, string path)
    {
        window.Close();
        GroupLab.Tests.Support.Temp.DeleteFile(path);
    }

    [AvaloniaFact]
    public void ShotsAListsEveryShotWithItsClicksAndCountedLeavesOneOut()
    {
        var (window, path) = Marked();
        try
        {
            var shots = window.ShotsWindow();
            shots.Show();
            Settle();
            var state = window.Session.State;
            var text = MainWindow.WindowText(shots).ToList();
            Assert.Contains(ResultWords.ShotsSummary(GroupAnalysis.Analyse(state), UnitSettings.Imperial)!, text);
            Assert.Contains(ResultWords.ShotsIntro(state), text);
            var rows = ShotOffsets.Table(state);
            Assert.Equal(10, rows.Count);
            Assert.All(rows, r => Assert.Contains(ResultWords.Offset(r, UnitSettings.Imperial), text));
            Assert.All(rows, r => Assert.Contains(ResultWords.Clicks(r), text));

            // Unticking Counted leaves the shot out of every figure, and it stays in the table, struck through.
            var counted = shots.GetLogicalDescendants().OfType<CheckBox>().First();
            counted.IsChecked = false;
            Settle();
            Assert.Equal(ExclusionReason.ByShooter, window.Session.State.Shots.Single(s => s.Id == rows[0].ShotId).Exclusion);
            var after = MainWindow.WindowText(shots).ToList();
            Assert.Contains(after, t => t.EndsWith("; 1 left out by the shooter, still on the record.", StringComparison.Ordinal));
            Assert.Contains(shots.GetLogicalDescendants().OfType<TextBlock>(), t => t.TextDecorations == Avalonia.Media.TextDecorations.Strikethrough);
            shots.Close();
        }
        finally
        {
            Done(window, path);
        }
    }

    [AvaloniaFact]
    public void ZeroFromThisGroupSaysWhatThePhoneSaysAndCarriesItsOffsetIntoBallistics()
    {
        var (window, path) = Marked();
        try
        {
            var zero = window.ZeroFromWindow();
            zero.Show();
            Settle();
            var words = ResultWords.ZeroFrom(window.Session.State, UnitSettings.Imperial);
            var text = MainWindow.WindowText(zero).ToList();
            Assert.All(words.Axes, a => Assert.Contains(a, text));
            Assert.Contains(words.Scope, text);
            Assert.Contains(words.Verdict, text);
            Assert.Contains(zero.GetLogicalDescendants().OfType<Button>(), b => Equals(b.Content, "Use as the zero offset in Ballistics"));
            Assert.Contains(zero.GetLogicalDescendants().OfType<Button>(), b => Equals(b.Content, "Open in Shots Needed to Zero"));
            zero.Close();

            window.UseZeroOffset(window.Session.State);
            Settle();
            Assert.True(window.ShowingBallistics);
        }
        finally
        {
            Done(window, path);
        }
    }

    [AvaloniaFact]
    public void EachAimPointPlacedByHandHasItsOwnFiguresUnderTheShotTable()
    {
        var (window, path) = Marked();
        try
        {
            var text = window.AimPointsText.ToList();
            var state = window.Session.State;
            foreach (var (aim, own) in GroupAnalysis.ByAimPoint(state))
            {
                Assert.Contains(ResultWords.AimPoint(aim, own, UnitSettings.Imperial), text);
            }

            Assert.Contains(ResultWords.AimPointsPooled, text);
        }
        finally
        {
            Done(window, path);
        }
    }

    [AvaloniaFact]
    public void ShareAPutsTheBoxAndTheCircleOnThePictureAndSavesItAsAPng()
    {
        var (window, path) = Marked();
        try
        {
            var share = window.ShareWindow();
            share.Show();
            Settle();
            var picture = window.SharePictureShown!;
            Assert.Equal(2, picture.Circles.Count);
            Assert.Equal(4, picture.Lines.Count); // the title, the shots, mean radius and extreme spread
            Assert.StartsWith("Marked by hand, ", picture.Lines[0], StringComparison.Ordinal);
            byte[] png = picture.Png(800);
            using var decoded = Cv2.ImDecode(png, ImreadModes.Color);
            Assert.Equal(800, decoded.Width);
            Assert.Equal(600, decoded.Height);

            // The crop around the group is a square about the holes.
            var crop = share.GetLogicalDescendants().OfType<ComboBox>().Single(c => c.ItemsSource is string[] items && items[0].StartsWith("Crop", StringComparison.Ordinal));
            crop.SelectedIndex = 1;
            Settle();
            Assert.NotNull(picture.Crop);
            Assert.Equal(picture.Crop!.Value.Width, picture.Crop.Value.Height);
            share.Close();
        }
        finally
        {
            Done(window, path);
        }
    }

    [AvaloniaFact]
    public void TheOnePageReportIsBesideTheFullOneAndIsOnePage()
    {
        var (window, path) = Marked();
        string pdf = Path.Combine(Path.GetTempPath(), $"grouplab-entry280-{Guid.NewGuid():N}.pdf");
        try
        {
            var report = window.GetLogicalDescendants().OfType<Button>().Single(b => Equals(b.Content, "Report"));
            var items = ((MenuFlyout)report.Flyout!).Items.OfType<MenuItem>().Select(i => i.Header as string).ToList();
            Assert.Equal(["Full report…", "One-page report…"], items);

            var built = window.BuildOnePageReport();
            Assert.Equal(GroupAnalysis.Analyse(window.Session.State).Counted!.Shots + " counted", built.Figures[0].Size);
            Assert.NotNull(built.Picture);
            window.WriteOnePageReport(pdf, built);
            Assert.Single(System.Text.RegularExpressions.Regex.Matches(System.Text.Encoding.Latin1.GetString(File.ReadAllBytes(pdf)), "/Type /Page(?!s)"));
        }
        finally
        {
            GroupLab.Tests.Support.Temp.DeleteFile(pdf);
            Done(window, path);
        }
    }
}

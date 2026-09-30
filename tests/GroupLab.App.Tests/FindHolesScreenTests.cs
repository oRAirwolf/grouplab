using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Evaluation;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using OpenCvSharp;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 318 section 2 on the desktop: on a target GroupLab did not print, once the scale is set by hand, "Find holes
/// (Experimental)" sits under the scale, always offered and always labelled Experimental, and its proposals become the marking's marks, the
/// unsure ones in the review, all taken back by one Undo.
/// </summary>
public class FindHolesScreenTests
{
    private static MainWindow NewWindow()
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        return new MainWindow(store) { Width = 1400, Height = 900 };
    }

    [AvaloniaFact]
    public void OnceTheScaleIsSetFindHolesIsOfferedAsExperimentalAndItsProposalsBecomeMarks()
    {
        // The synthetic black-bull target at 200 pixels an inch, written as a picture.
        var (picture, holes) = Scoreboard.AnyTargetPicture("black bulls", Scoreboard.AnyTargetSeeds[0]);
        string path = Path.Combine(Path.GetTempPath(), $"grouplab-findholes-{Guid.NewGuid():N}.png");
        using (var mat = Mat.FromPixelData(picture.Height, picture.Width, MatType.CV_8UC1, picture.Pixels))
        {
            Cv2.ImWrite(path, mat);
        }

        var window = NewWindow();
        window.Show();
        window.OpenImage(path);
        Dispatcher.UIThread.RunJobs();
        Assert.False(window.FindHolesOffered);
        Assert.DoesNotContain(Buttons(window), b => Equals(b.Content, FindHoles.Label));

        window.Session.SetScale(new LengthReference(new PointD(0, 0), new PointD(Scoreboard.AnyTargetDpi, 0), 1));
        window.Session.SetCalibre(Calibre.Of(Scoreboard.AnyTargetCalibre));
        window.RefreshForTests();
        Dispatcher.UIThread.RunJobs();
        Assert.True(window.FindHolesOffered);
        var button = Assert.Single(Buttons(window), b => Equals(b.Content, FindHoles.Label));
        Assert.Contains("(Experimental)", (string)button.Content!, StringComparison.Ordinal);
        Assert.Contains(window.ScaleInputs.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text == FindHoles.Explanation);

        window.FindHolesForTests();
        Dispatcher.UIThread.RunJobs();
        var shots = window.Session.State.Shots;
        Assert.NotEmpty(shots);
        Assert.All(shots, s => Assert.True(s.Provenance == ShotProvenance.Automatic && s.Proposal is not null));
        var (found, _, _) = Scoreboard.Match(holes, [.. shots.Select(s => s.Image)], Scoreboard.FoundWithinInches * Scoreboard.AnyTargetDpi);
        Assert.True(found >= 8, $"{found} of {holes.Count} holes proposed");

        // Every doubtful one is an item in the review, with its own title.
        int doubted = shots.Count(s => ReviewQueue.StillDoubted(window.Session.State, s));
        Assert.Equal(doubted, window.ReviewItems.Count(i => i.Kind == ReviewKind.Proposed && !i.Resolved));

        window.Session.Undo();
        Assert.Empty(window.Session.State.Shots);
        window.Close();
    }

    private static IEnumerable<Button> Buttons(MainWindow window) => window.GetLogicalDescendants().OfType<Button>();
}

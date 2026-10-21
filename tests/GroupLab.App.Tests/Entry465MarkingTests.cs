using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 465 section 1A on the computer: the marking screen's own two pictures, the full sheet with the shot on bull
/// 14 selected and the hand-marked target, drawn as a person sees them at 1280 by 720, with no tag on another tag, a bull's number or a hole.
/// <see cref="GroupLab.Core.Tests.Marking.MarkTagsTests"/> holds the placing itself.
/// </summary>
public class Entry465MarkingTests
{
    private static (TagLayout Layout, IReadOnlyList<TagHole> Holes) Drawn(MainWindow window)
    {
        window.Canvas.FitToView();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        Assert.NotNull(window.CaptureRenderedFrame());
        return window.Canvas.LastTags ?? throw new InvalidOperationException("the picture drew no tags");
    }

    [AvaloniaFact]
    public void TheFullSheetWithBull14SelectedHasNoTagOnAnotherOrOnAHole()
    {
        var (window, path, _) = Entry109Tests.Sheet(1280, 720);
        try
        {
            window.Session.SetCalibre(Calibre.Of(0.308));
            var state = window.Session.State;
            var fourteen = state.Bulls.Single(b => b.Label == "14");
            window.Canvas.Selected = state.Shots.First(s => s.IsShot && s.Bull == fourteen.Index).Id;
            window.RefreshForTests();
            Assert.NotEmpty(window.Canvas.BullRings);
            var (layout, holes) = Drawn(window);
            Assert.Equal(state.Bulls.Count, layout.Bulls.Count);
            Assert.Equal(state.Shots.Count(s => s.IsShot), layout.Holes.Count);
            var overlaps = MarkTags.Overlaps(layout, holes);
            Assert.True(overlaps.Count == 0, string.Join("; ", overlaps));

            // Bull 14's number at its top left, where 14 and its hole's 14 used to sit on each other.
            var number = layout.Bulls.Single(b => b.Index == fourteen.Index).Box;
            var centre = window.Canvas.ToControl(fourteen.Image);
            Assert.True(number.Right < centre.X && number.Bottom < centre.Y);
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    [AvaloniaFact]
    public void TheHandMarkedTargetFansItsTagsOutOnLeaders()
    {
        var (window, path, _) = Entry109Tests.Sheet(1280, 720);
        string other = Entry109Tests.PlainSample();
        try
        {
            window.OpenImage(other);
            window.Session.SetCalibre(Calibre.Of(0.308));
            Dispatcher.UIThread.RunJobs();
            var c0 = Entry109Tests.PlainCentres[0];
            window.Session.SetScale(new LengthReference(new PointD(c0.X - Entry109Tests.PlainOuter, c0.Y), new PointD(c0.X + Entry109Tests.PlainOuter, c0.Y), 2.0 * Entry109Tests.PlainOuter / Entry109Tests.PlainDpi));
            foreach (var centre in Entry109Tests.PlainCentres)
            {
                window.Session.AddBull(centre);
            }

            foreach (var (x, y) in Entry109Tests.PlainHoles)
            {
                window.Session.AddShot(new PointD(x, y));
            }

            window.Canvas.Selected = window.Session.State.Shots[0].Id;
            window.RefreshForTests();
            var (layout, holes) = Drawn(window);
            Assert.Equal(Entry109Tests.PlainHoles.Length, layout.Holes.Count);
            var overlaps = MarkTags.Overlaps(layout, holes);
            Assert.True(overlaps.Count == 0, string.Join("; ", overlaps));
            Assert.All(layout.Holes, t => Assert.NotNull(t.LeaderFrom));
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(other)!);
        }
    }
}

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using OpenCvSharp;
using Window = Avalonia.Controls.Window;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 291 section 2: the result screen. The picture upright as the sheet is, whichever way the phone was held,
/// across the width with nothing empty after it; the holes fixed on their own page and never by touching the result; the Camera and Result
/// buttons showing which is on screen without looking disabled.
/// </summary>
public class ResultScreenTests
{
    private static readonly TestPhone ThePhone = new();

    private static Window Started()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(ThePhone, Avalonia.Application.Current!, () => "US", null);
        }

        var window = new Window { Width = 412, Height = 915 };
        window.Show();
        return window;
    }

    /// <summary>The sample sheet at half its resolution, turned by <paramref name="turns"/> quarter turns clockwise, with no orientation tag.</summary>
    private static string Turned(int turns)
    {
        using var sheet = Cv2.ImRead(Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png"), ImreadModes.Color);
        using var half = new Mat();
        Cv2.Resize(sheet, half, new OpenCvSharp.Size(0, 0), 0.5, 0.5, InterpolationFlags.Area);
        using var turned = new Mat();
        if (turns == 0)
        {
            half.CopyTo(turned);
        }
        else
        {
            Cv2.Rotate(half, turned, turns switch { 1 => RotateFlags.Rotate90Clockwise, 2 => RotateFlags.Rotate180, _ => RotateFlags.Rotate90Counterclockwise });
        }

        string path = Path.Combine(ThePhone.CacheFolder, $"held-{turns}.png");
        Cv2.ImWrite(path, turned);
        return path;
    }

    private static PhoneResult Analyzed(string photo)
    {
        var result = PhoneAnalysis.Run(photo, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, null, CancellationToken.None);
        Assert.Null(result.Failure);
        return result;
    }

    [AvaloniaTheory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void TheSheetIsShownUprightWhicheverWayThePhoneWasHeldAcrossTheWidthWithNothingEmptyAfterIt(int held)
    {
        var window = Started();
        var result = Analyzed(Turned(held));
        var sheet = Assert.IsType<SheetReference>(result.State.Scale);
        using var bitmap = new Avalonia.Media.Imaging.Bitmap(result.State.ImagePath!);
        double w = bitmap.PixelSize.Width, h = bitmap.PixelSize.Height;

        // The page's top middle is drawn above its bottom middle, and straight above it: the sheet upright, as the registration found it.
        int turns = result.State.ViewQuarterTurns;
        var top = ViewRotation.ToDisplay(sheet.Mapping.ToImage(new PointD(1080, 300)), turns, w, h);
        var bottom = ViewRotation.ToDisplay(sheet.Mapping.ToImage(new PointD(1080, 2500)), turns, w, h);
        Assert.True(bottom.Y - top.Y > 4 * Math.Abs(bottom.X - top.X), $"held {held}: turned {turns}, top {top}, bottom {bottom}");

        var view = new ResultView(result, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, () => { });
        window.Content = view;
        Dispatcher.UIThread.RunJobs();
        var picture = view.GetVisualDescendants().OfType<ResultView.SheetPicture>().Single();
        Assert.Equal(turns, picture.Turns);
        Assert.True(picture.Shown.Height > picture.Shown.Width, "a portrait sheet is shown portrait");

        // Across the width of its column, and exactly as tall as the picture: no box a screen high with the picture in a corner of it.
        var column = (Control)picture.GetVisualParent()!;
        Assert.InRange(picture.Bounds.Width, column.Bounds.Width - 1, column.Bounds.Width + 1);
        Assert.InRange(picture.Bounds.Height, (picture.Bounds.Width * picture.Shown.Height / picture.Shown.Width) - 1, (picture.Bounds.Width * picture.Shown.Height / picture.Shown.Width) + 1);
        window.Close();
    }

    [AvaloniaFact]
    public void HolesAreFixedOnTheirOwnPageUnderACrosshairAndOnlyDoneOrAYesKeepsTheChanges()
    {
        var window = Started();
        var result = Analyzed(Turned(0));
        var view = new ResultView(result, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, () => { });
        window.Content = view;
        Dispatcher.UIThread.RunJobs();

        // The result's picture takes no touch: it has nothing to move a hole with.
        var picture = view.GetVisualDescendants().OfType<ResultView.SheetPicture>().Single();
        Assert.False(picture.Focusable);

        FixHolesPage Open()
        {
            Press(view, "Fix holes");
            return view.GetVisualDescendants().OfType<FixHolesPage>().Single();
        }

        int Shots() => view.GetVisualDescendants().OfType<ResultView.SheetPicture>().Any()
            ? PhoneAnalysis.Store().Get(SessionId(view))!.ShotCount
            : -1;

        // A hole added and then undone is no change, and Back goes straight back.
        var page = Open();
        var viewer = page.Picture;
        viewer.CentreOn(new PointD(50, 50));
        page.Press();
        Assert.Equal(26, page.State.Shots.Count(s => s.IsShot));
        page.Undo();
        Assert.False(page.Changed);
        page.Leave();
        Dispatcher.UIThread.RunJobs();
        Assert.Empty(view.GetVisualDescendants().OfType<FixHolesPage>());

        // A change and then Back asks; throwing it away leaves the result as it was.
        page = Open();
        page.Picture.CentreOn(new PointD(50, 50));
        page.Press();
        page.Leave();
        Dispatcher.UIThread.RunJobs();
        Assert.True(page.Asking);
        Press(page, "Throw them away");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(25, Shots());

        // Move: pick up the hole under the crosshair, drag its circle (entry 309 section 3.3), put it down; Remove another; zoom in; Done
        // measures again.
        page = Open();
        var shots = page.State.Shots.Where(s => s.IsShot).ToList();
        page.Picture.ZoomBy(4);
        page.Picture.CentreOn(shots[0].Image);

        // Entry 309 section 3.1: each circle is the bullet's size, so it grows with the zoom rather than staying a fixed mark.
        double radius = page.Picture.HoleRadius(shots[0].Image);
        Assert.NotEqual(9, radius);
        page.Picture.ZoomBy(1.5);
        page.Picture.CentreOn(shots[0].Image);
        Assert.InRange(page.Picture.HoleRadius(shots[0].Image) / radius, 1.49, 1.51);
        page.Move();
        page.Picture.DragHeld(new Avalonia.Vector(12, 0));
        Assert.Equal(page.Picture.HeldAt, page.Picture.Centre);
        Assert.True(page.Picture.HeldMovedInches > 0);
        page.Press();
        var moved = page.State.Shots.Single(s => s.Id == shots[0].Id).Image;
        Assert.True(moved.X - shots[0].Image.X > 1, $"moved from {shots[0].Image} to {moved}");
        page.Picture.CentreOn(shots[1].Image);
        page.Remove();
        Assert.Equal(24, page.State.Shots.Count(s => s.IsShot));
        Press(page, "Done");
        Dispatcher.UIThread.RunJobs();
        Assert.Empty(view.GetVisualDescendants().OfType<FixHolesPage>());
        Assert.Equal(24, Shots());
        window.Close();
    }

    /// <summary>
    /// Error report 27 and entry 401 (nightly 183, Android): a hole added in Fix holes asks which bull it was fired at, and whatever the
    /// answer, GroupLab's choice, another bull or the ×, the page went blank: the sheet had taken the page's picture into its own layer and
    /// handing it back failed, so nothing could be seen, fixed or kept. Each answer now gives the page back whole, and Done counts the hole.
    /// </summary>
    [AvaloniaFact]
    public void AnsweringWhichBullAHoleWasFiredAtGivesFixHolesBackWholeAndDoneCountsIt()
    {
        var window = Started();
        var result = Analyzed(Turned(0));
        var view = new ResultView(result, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, () => { });
        window.Content = view;
        Dispatcher.UIThread.RunJobs();

        bool Whole(FixHolesPage page) => page.GetVisualDescendants().OfType<MarkingAPage.Viewer>().Any(v => v.IsEffectivelyVisible)
            && page.GetVisualDescendants().OfType<Button>().Any(b => b.Name == "fix-done" || Words(b) == "Done");

        static string Named(Button b) => Avalonia.Automation.AutomationProperties.GetName(b) is { Length: > 0 } name ? name : b.Content as string ?? "";

        Button Answer(FixHolesPage page, Func<Button, bool> which) =>
            Avalonia.LogicalTree.LogicalExtensions.GetLogicalDescendants(page).OfType<Button>().First(which);

        foreach (string answer in new[] { "choice", "another", "dismiss" })
        {
            Press(view, "Fix holes");
            var page = view.GetVisualDescendants().OfType<FixHolesPage>().Single();
            page.Picture.CentreOn(new PointD(50, 50));
            page.Press();
            Dispatcher.UIThread.RunJobs();
            var bulls = Avalonia.LogicalTree.LogicalExtensions.GetLogicalDescendants(page).OfType<Button>().Where(b => Named(b).StartsWith("Bull ", StringComparison.Ordinal)).ToList();
            Assert.True(bulls.Count >= 2, "the question of which bull was not asked: bulls " + page.State.Bulls.Count + ", content " + page.Content?.GetType().Name + ", buttons " + string.Join(" | ", Avalonia.LogicalTree.LogicalExtensions.GetLogicalDescendants(page).OfType<Button>().Select(Named)));
            var button = answer switch
            {
                "choice" => Answer(page, b => Named(b).EndsWith("(GroupLab's choice)", StringComparison.Ordinal)),
                "another" => Answer(page, b => Named(b).StartsWith("Bull ", StringComparison.Ordinal) && !Named(b).EndsWith("(GroupLab's choice)", StringComparison.Ordinal)),
                _ => Answer(page, b => b.Content as string == "×"),
            };
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();

            Assert.True(Whole(page), $"Fix holes is not whole after answering with {answer}");
            Assert.Equal(26, page.State.Shots.Count(s => s.IsShot));
            Press(page, "Done");
            Assert.Empty(view.GetVisualDescendants().OfType<FixHolesPage>());
            Assert.Equal(26, PhoneAnalysis.Store().Get(SessionId(view))!.ShotCount);
            Assert.NotEmpty(view.GetVisualDescendants().OfType<ResultView.SheetPicture>());

            // Back to 25 for the next answer: the hole is removed in Fix holes again.
            Press(view, "Fix holes");
            page = view.GetVisualDescendants().OfType<FixHolesPage>().Single();
            var added = page.State.Shots.Where(s => s.IsShot).MaxBy(s => s.Id)!;
            page.Picture.CentreOn(added.Image);
            page.Remove();
            Press(page, "Done");
            Assert.Equal(25, PhoneAnalysis.Store().Get(SessionId(view))!.ShotCount);
        }

        window.Close();
    }

    [AvaloniaFact]
    public void TheButtonForWhatIsShowingLooksSelectedAndNotDisabled()
    {
        var window = Started();
        var capture = new CapturePage();
        window.Content = capture;
        capture.ShowResult(new TextBlock { Text = "a result" });
        Dispatcher.UIThread.RunJobs();
        var buttons = capture.GetVisualDescendants().OfType<Button>().ToDictionary(Words, b => b);
        Assert.True(buttons["Result"].IsEffectivelyEnabled);
        Assert.Contains(PhoneStyles.Primary, buttons["Result"].Classes);
        Assert.DoesNotContain(PhoneStyles.Primary, buttons["Capture"].Classes);
        window.Close();
    }

    /// <summary>
    /// Error report 26 (nightly 180, Android): with a result open, Capture shows the start inside the bar; closing that last target then
    /// showed the start alone while it was still in the bar's panel, and Avalonia refused it. Now the start is shown, with no bar.
    /// </summary>
    [AvaloniaFact]
    public void ClosingTheLastTargetAfterCaptureShowsTheStartAlone()
    {
        var window = Started();
        var capture = new CapturePage();
        window.Content = capture;
        var result = new TextBlock { Text = "a result" };
        capture.ShowResult(result);
        Dispatcher.UIThread.RunJobs();
        Press(capture, "Capture");
        Assert.Contains(capture.GetVisualDescendants().OfType<Button>(), b => Words(b) == "Result");

        capture.Closed(result);
        capture.ShowStart();
        Dispatcher.UIThread.RunJobs();
        Assert.DoesNotContain(capture.GetVisualDescendants().OfType<Button>(), b => Words(b) == "Result");
        Assert.IsNotType<DockPanel>(capture.Content);
        window.Close();
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 318 section 1: a shot placed inside a mark much bigger than the bullet is on the phone's result, ringed on
    /// the picture and said in a sentence with the queue's own choices, and it goes when the person says the shot is on the hole.
    /// </summary>
    [AvaloniaFact]
    public void AMarkMuchBiggerThanTheBulletIsRingedAndSaidUntilThePersonSettlesIt()
    {
        var window = Started();
        var session = new MarkingSession();
        session.Open(Turned(0));
        session.SetCalibre(Calibre.Of(0.243));
        var scale = new LengthReference(new PointD(0, 0), new PointD(300, 0), 1);
        var assigned = new[] { new GroupLab.Core.Detection.AssignedShot(0, 0, 5, 0, 5, 3000, false), new GroupLab.Core.Detection.AssignedShot(1, 1, 5, 1, 5, 3000, false) };
        session.LoadDetections(scale, [new BullAim(0, "1", new PointD(300, 300)), new BullAim(1, "2", new PointD(700, 300))],
        [
            new DetectedShot(new PointD(300, 300), assigned[0], 0.22, new DetectedOversize(2.6, false, Joined: true, AcrossInches: 0.535, AcrossHoles: 2.3)),
            new DetectedShot(new PointD(700, 300), assigned[1], 0.24, null),
        ], new GroupLab.Core.Detection.ShotAssignmentResult(GroupLab.Core.Detection.AssignmentMethod.OneToOne, "test", assigned), [], "test");
        int flagged = session.State.Shots[0].Id;
        var view = new ResultView(new PhoneResult(session.State, null, null, null), new ShotSetup(Calibre.Of(0.243), 3600), UnitSettings.Imperial, () => { });
        window.Content = view;
        Dispatcher.UIThread.RunJobs();

        string[] Said() => [.. view.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? "")];
        var picture = view.GetVisualDescendants().OfType<ResultView.SheetPicture>().Single();
        Assert.Equal([flagged], picture.Flagged);
        Assert.Contains(Said(), t => t.Contains("2.2 times your bullet across", StringComparison.Ordinal));
        Assert.Contains("1 mark to check", Said());

        Press(view, "It is on the hole");
        Assert.Empty(view.GetVisualDescendants().OfType<ResultView.SheetPicture>().Single().Flagged);
        string[] Shown() => [.. view.GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsEffectivelyVisible).Select(t => t.Text ?? "")];
        Assert.DoesNotContain(Shown(), t => t.Contains("times your bullet across", StringComparison.Ordinal));

        // Entry 374 section 4: the answered mark is folded up under one line, and Ask again brings it back to check.
        Assert.Contains("No marks left to check", Shown());
        Press(view, "Show the 1 answered mark");
        Assert.Contains(Shown(), t => t.Contains("times your bullet across", StringComparison.Ordinal));
        Press(view, ResultView.AskAgain);
        Assert.Equal([flagged], view.GetVisualDescendants().OfType<ResultView.SheetPicture>().Single().Flagged);
        Assert.Contains("1 mark to check", Said());
        window.Close();
    }

    private static long SessionId(ResultView view) =>
        (long)typeof(ResultView).GetField("sessionId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(view)!;

    private static void Press(Control within, string words)
    {
        var button = within.GetVisualDescendants().OfType<Button>().First(b => Words(b) == words);
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }

    private static string Words(Button button) =>
        string.Join(" ", button.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).Where(t => !string.IsNullOrWhiteSpace(t)));
}

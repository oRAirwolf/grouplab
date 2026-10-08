using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using OpenCvSharp;
using Window = Avalonia.Controls.Window;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// Concept A, several targets open at once (Alan, 2026-10-07), the phone's half: the open targets as a list, switching between them, closing
/// one and asking first where it has marks not saved, and the words of each row.
/// </summary>
public class OpenTargetsTests
{
    private sealed class Fake(string name, bool marking = false, bool unsaved = false, int shots = 0, long? session = null) : IOpenTarget
    {
        public string TargetName { get; } = name;

        public bool Marking { get; set; } = marking;

        public bool UnsavedMarks { get; set; } = unsaved;

        public int Shots { get; } = shots;

        public long? SessionId { get; } = session;
    }

    [Fact]
    public void OpeningAddsOnceAndMakesItTheOneShowing()
    {
        var open = new OpenTargets<Fake>();
        var a = new Fake("A");
        var b = new Fake("B");
        Assert.Empty(open.Opened(a));
        Assert.Empty(open.Opened(b));
        Assert.Same(b, open.Current);
        Assert.Empty(open.Opened(a));
        Assert.Equal([a, b], open.All);
        Assert.Same(a, open.Current);
    }

    [Fact]
    public void SwitchingOnlyToATargetThatIsOpen()
    {
        var open = new OpenTargets<Fake>();
        var a = new Fake("A");
        var b = new Fake("B");
        open.Opened(a);
        open.Opened(b);
        Assert.True(open.SwitchTo(a));
        Assert.Same(a, open.Current);
        Assert.False(open.SwitchTo(new Fake("C")));
        Assert.Same(a, open.Current);
    }

    [Fact]
    public void ClosingTheOneShowingShowsTheNextThenThePreviousThenNone()
    {
        var open = new OpenTargets<Fake>();
        var a = new Fake("A");
        var b = new Fake("B");
        var c = new Fake("C");
        open.Opened(a);
        open.Opened(b);
        open.Opened(c);
        open.SwitchTo(b);
        Assert.Same(c, open.Close(b));
        Assert.Same(a, open.Close(c));
        Assert.Null(open.Close(a));
        Assert.Equal(0, open.Count);
    }

    [Fact]
    public void ClosingAnotherLeavesTheOneShowing()
    {
        var open = new OpenTargets<Fake>();
        var a = new Fake("A");
        var b = new Fake("B");
        open.Opened(a);
        open.Opened(b);
        Assert.Same(b, open.Close(a));
        Assert.Equal([b], open.All);
        Assert.Same(b, open.Close(new Fake("never open")));
    }

    [Fact]
    public void OnlyAMarkingWithMarksNotSavedAsksBeforeClosing()
    {
        Assert.False(OpenTargetWords.AsksBeforeClosing(new Fake("analysis")));
        Assert.False(OpenTargetWords.AsksBeforeClosing(new Fake("marking, nothing marked", marking: true)));
        Assert.True(OpenTargetWords.AsksBeforeClosing(new Fake("marking with marks", marking: true, unsaved: true)));
        var open = new OpenTargets<Fake>();
        open.Opened(new Fake("a"));
        open.Opened(new Fake("b", marking: true, unsaved: true));
        Assert.Equal(1, open.Unsaved);
    }

    [Fact]
    public void ARowSaysItsStateItsShotsAndWhetherItIsShowing()
    {
        Assert.Equal("Analysis · 5 shots", OpenTargetWords.Detail(new Fake("a", shots: 5), showing: false));
        Assert.Equal("Analysis · 1 shot · showing now", OpenTargetWords.Detail(new Fake("a", shots: 1), showing: true));
        Assert.Equal("Marking, not saved", OpenTargetWords.Detail(new Fake("m", marking: true), showing: false));
        Assert.Equal("", OpenTargetWords.Count(1));
        Assert.Equal("3 open", OpenTargetWords.Count(3));
        Assert.Equal("Close GroupLab 5x5?", OpenTargetWords.CloseQuestion(new Fake("GroupLab 5x5")));
        Assert.Equal("Close GroupLab 5x5", OpenTargetWords.CloseName(new Fake("GroupLab 5x5")));
    }

    [Fact]
    public void ASessionIsFoundSoItIsNeverOpenTwice()
    {
        var open = new OpenTargets<Fake>();
        var saved = new Fake("saved", session: 7);
        open.Opened(new Fake("unsaved"));
        open.Opened(saved);
        Assert.Same(saved, open.Find(7));
        Assert.Null(open.Find(8));
        Assert.Null(open.Find(null));
    }

    [Fact]
    public void PastTheMostTheOldestThatLosesNothingIsClosedNeverOneWithMarksNotSaved()
    {
        var open = new OpenTargets<Fake>();
        var marking = new Fake("marking", marking: true, unsaved: true);
        open.Opened(marking);
        var first = new Fake("first", session: 1);
        open.Opened(first);
        for (int i = 2; i < OpenTargets<Fake>.Most; i++)
        {
            Assert.Empty(open.Opened(new Fake("t" + i)));
        }

        var newest = new Fake("newest");
        Assert.Equal([first], open.Opened(newest));
        Assert.Equal(OpenTargets<Fake>.Most, open.Count);
        Assert.Contains(marking, open.All);
        Assert.Same(newest, open.Current);
        Assert.Equal("first was closed to make room; it is still in Sessions.", OpenTargetWords.MadeRoom(first));
        Assert.Equal("t2 was closed to make room.", OpenTargetWords.MadeRoom(new Fake("t2")));
    }

    [Fact]
    public void WithEveryOtherAskingNoneIsClosedToMakeRoom()
    {
        var open = new OpenTargets<Fake>();
        for (int i = 0; i < OpenTargets<Fake>.Most; i++)
        {
            open.Opened(new Fake("m" + i, marking: true, unsaved: true));
        }

        Assert.Empty(open.Opened(new Fake("one more")));
        Assert.Equal(OpenTargets<Fake>.Most + 1, open.Count);
    }

    private static readonly TestPhone ThePhone = new();

    /// <summary>A small picture of a sheet, written once, to mark by hand.</summary>
    private static string Picture()
    {
        string path = Path.Combine(ThePhone.CacheFolder, "open-targets.png");
        if (!File.Exists(path))
        {
            using var sheet = new Mat(600, 800, MatType.CV_8UC3, Scalar.White);
            Cv2.Circle(sheet, new OpenCvSharp.Point(400, 300), 80, Scalar.Black, 3);
            Cv2.ImWrite(path, sheet);
        }

        return path;
    }

    /// <summary>A target marked by hand with <paramref name="shots"/> shots, as a result.</summary>
    private static ResultView Marked(int shots)
    {
        var session = new MarkingSession();
        session.Open(Picture());
        session.SetCalibre(Calibre.Of(0.308));
        session.SetScale(new LengthReference(new PointD(100, 100), new PointD(700, 100), 6));
        session.AddBull(new PointD(400, 300));
        for (int i = 0; i < shots; i++)
        {
            session.AddShot(new PointD(390 + (10 * i), 295 + (5 * i)));
        }

        return new ResultView(new PhoneResult(session.State, null, null, null), new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, () => { });
    }

    private static Button Find(Window window, string id) =>
        window.GetVisualDescendants().OfType<Button>().Single(b => b.IsEffectivelyVisible && AutomationProperties.GetAutomationId(b) == id);

    private static void Press(Window window, string id)
    {
        Find(window, id).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Settle(window);
    }

    private static void Settle(Window window)
    {
        for (int i = 0; i < 3; i++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
        }
    }

    private static bool OnScreen(Window window, Control view) => window.GetVisualDescendants().Contains(view);

    [AvaloniaFact]
    public void TwoTargetsOpenSwitchKeepTheirOwnMarksAndClosingOneWithMarksNotSavedAsksFirst()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(ThePhone, Avalonia.Application.Current!, () => "US", null);
        }

        GroupLab.Mobile.Dev.Scenario.AnswerFirstRun(Phone.Settings);
        var shell = new Shell();
        var window = new Window { Width = 320, Height = 568, Content = shell };
        window.Show();
        try
        {
            shell.Show(Shell.Place.Capture);
            var capture = Assert.IsType<CapturePage>(shell.PageContent);
            var first = Marked(3);
            var second = Marked(5);
            capture.ShowResult(first);
            capture.ShowResult(second);
            Settle(window);
            Assert.Equal([first, second], shell.Targets.All);
            Assert.Same(second, shell.Targets.Current);

            // The name at the top says two are open and opens the list, each row with its state and the one showing.
            Assert.Contains(Find(window, "result-open-targets").GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "2 open");
            Press(window, "result-open-targets");
            string[] Words() => [.. window.GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsEffectivelyVisible).Select(t => t.Text ?? "")];
            Assert.Contains(OpenTargetWords.SheetTitle, Words());
            Assert.Contains("Analysis · 3 shots", Words());
            Assert.Contains("Analysis · 5 shots · showing now", Words());
            Assert.Empty(MobileSweepTests.Problems(window, "the open targets at 320 wide"));

            // A tap on a row switches to it, as it was left.
            Press(window, "open-targets-row-0");
            Assert.True(OnScreen(window, first));
            Assert.False(OnScreen(window, second));
            Assert.Same(first, shell.Targets.Current);

            // Marking on the first: an aim point added and not saved.
            var marking = new MarkingAPage(Picture(), null, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, _ => { }, () => { },
                MarkedState());
            first.Content = marking;
            Settle(window);
            Assert.Equal(OpenTargetWords.MarkingNotSaved, OpenTargetWords.State(first));
            Assert.False(first.UnsavedMarks);
            var set = window.GetVisualDescendants().OfType<Button>().First(b => b.IsEffectivelyVisible && (b.Content as TextBlock)?.Text == "Set the aim point");
            set.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle(window);
            Assert.True(first.UnsavedMarks);

            // Switched away from the marking, its marks stay with it, and the updater still sees them.
            Press(window, "marking-open-targets");
            Press(window, "open-targets-row-1");
            Assert.True(OnScreen(window, second));
            Assert.Equal(1, WorkInProgress.HiddenUnsaved);
            Assert.True(WorkInProgress.Unsaved);
            Press(window, "result-open-targets");
            Press(window, "open-targets-row-0");
            Assert.True(OnScreen(window, marking));
            Assert.True(first.UnsavedMarks);

            // Closing it asks first; keeping it open goes back to the list, closing loses the marks and shows the other.
            Press(window, "marking-open-targets");
            Press(window, "open-targets-close-0");
            Assert.Contains(OpenTargetWords.CloseQuestion(first), Words());
            Press(window, "open-targets-keep");
            Assert.Equal(2, shell.Targets.Count);
            Assert.Contains(OpenTargetWords.SheetTitle, Words());
            Press(window, "open-targets-close-0");
            Press(window, "open-targets-close-confirm");
            Assert.Equal([second], shell.Targets.All);
            Assert.True(OnScreen(window, second));
            Assert.Equal(0, WorkInProgress.HiddenUnsaved);

            // The last one closed with nothing to lose closes at once, and Capture's start is shown.
            Press(window, "result-open-targets");
            Press(window, "open-targets-close-0");
            Assert.Equal(0, shell.Targets.Count);
            Assert.False(OnScreen(window, second));
            Assert.True(capture.AtStart);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>A marking with its scale set, so Marking A opens at the aim point.</summary>
    private static MarkingState MarkedState()
    {
        var session = new MarkingSession();
        session.Open(Picture());
        session.SetCalibre(Calibre.Of(0.308));
        session.SetScale(new LengthReference(new PointD(100, 100), new PointD(700, 100), 6));
        return session.State;
    }
}

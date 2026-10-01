using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.StoreTargets;
using OpenCvSharp;
using Window = Avalonia.Controls.Window;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entries 340 and 341 on the phone: a picture that names no GroupLab sheet but is a store-bought target GroupLab
/// knows goes straight to Marking A with its bull placed and its printed size as the scale, the warning and "Check the scale" under it; a
/// family member the picture cannot tell from its other size asks "Which target is this?" first, the last answer first; the result carries
/// the warning, and a GroupLab sheet's never does.
/// </summary>
public class StoreTargetPhoneTests
{
    private const string SixInch = "bc-34550-shoot-n-c-6in-bull";
    private const string EightInch = "bc-34805-shoot-n-c-8in-bull";

    private static TestPhone Started()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        return (TestPhone)Phone.Platform!;
    }

    private static StoreTargetCandidate C(string id, int features, double layout, double ppi) =>
        new(StoreTargetLibrary.Find(id)!, features, new Homography([ppi, 0, 100, 0, ppi, 100, 0, 0, 1]), layout);

    /// <summary>What the phone's analysis returns for a picture that named no sheet, with what recognition decided.</summary>
    private static PhoneResult Unnamed(TestPhone phone, StoreTargetRecognition seen)
    {
        string folder = Path.Combine(phone.FilesFolder, "sessions", $"store-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "target.jpg");
        using (var blank = new Mat(1000, 1000, MatType.CV_8UC3, Scalar.All(230)))
        {
            Cv2.ImWrite(path, blank);
        }

        var session = new MarkingSession();
        session.Open(path);
        var working = new WorkingImage(path, ImageMetadataReader.Read(File.ReadAllBytes(path)), 1000, 1000);
        return new PhoneResult(session.State, null, "GroupLab could not read the square codes that name the sheet.", null, working, AskWhichSheet: true,
            Recognized: new StoreTargetSeen(seen, 1000, 1000));
    }

    private static T Shown<T>(Window window) where T : Control => window.GetVisualDescendants().OfType<T>().First();

    private static Button ById(Control control, string id) =>
        control.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetAutomationId(b) == id);

    [AvaloniaFact]
    public void ARecognizedTargetGoesStraightToMarkingWithItsBullAndScaleAndTheWarning()
    {
        var phone = Started();
        var seen = StoreTargetRecognizer.Decide([C(EightInch, 300, 0.99, 100)]);
        var window = new Window { Width = 412, Height = 915, Content = new ResultView(Unnamed(phone, seen), new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, () => { }) };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var page = Shown<MarkingAPage>(window);
        Assert.Equal(EightInch, Assert.IsType<RectangleReference>(page.State.Scale).PrintedTarget);
        Assert.Single(page.State.Bulls);
        Assert.True(page.StoreScaleInPlay);
        Assert.Contains(page.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == StoreTargetMatch.Warning && t.IsVisible);
        Assert.Contains(page.GetVisualDescendants().OfType<TextBlock>(), t => (t.Text ?? "").StartsWith("Recognized the Shoot-N-C 8 in bullseye", StringComparison.Ordinal));

        // The scale check is one tap away: back to the scale, where a known length replaces the printed size.
        var check = ById(page, "marking-check-scale");
        Assert.True(check.IsVisible);
        check.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.False(page.StoreScaleInPlay);
        window.Close();
    }

    [AvaloniaFact]
    public void AFamilyThePictureCannotTellApartAsksWhichFirstAndTheAnswerSetsTheScale()
    {
        var phone = Started();
        var seen = StoreTargetRecognizer.Decide([C(EightInch, 200, 0.988, 60), C(SixInch, 72, 0.928, 80)]);
        Phone.Settings.SaveFamilyAnswer(StoreTargetLibrary.ShootNCBullseye, SixInch);
        var window = new Window { Width = 412, Height = 915, Content = new ResultView(Unnamed(phone, seen), new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, () => { }) };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var question = Shown<WhichTargetPage>(window);
        Assert.Contains(question.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == FamilyQuestion.Title);
        Assert.Equal([SixInch, EightInch], question.Answers.Select(a => a.Match.Target.Id));
        Assert.Equal(2, question.GetVisualDescendants().OfType<TargetOutline>().Count());
        Assert.True(ById(question, "which-target-not-sure").IsVisible);

        ById(question, "which-target-" + EightInch).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        var page = Shown<MarkingAPage>(window);
        var scale = Assert.IsType<RectangleReference>(page.State.Scale);
        Assert.Equal(EightInch, scale.PrintedTarget);
        var a = scale.ToTarget(new PointD(200, 200));
        var b = scale.ToTarget(new PointD(260, 200));
        Assert.Equal(1.0, b.X - a.X, 6);
        Assert.Equal(EightInch, Phone.Settings.LoadFamilyAnswer(StoreTargetLibrary.ShootNCBullseye));
        window.Close();
    }

    [AvaloniaFact]
    public void NotSurePlacesTheBullAndStartsAtTheScale()
    {
        var phone = Started();
        var seen = StoreTargetRecognizer.Decide([C(EightInch, 200, 0.988, 60), C(SixInch, 72, 0.928, 80)]);
        var window = new Window { Width = 412, Height = 915, Content = new ResultView(Unnamed(phone, seen), new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, () => { }) };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        ById(Shown<WhichTargetPage>(window), "which-target-not-sure").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        var page = Shown<MarkingAPage>(window);
        Assert.Null(page.State.Scale);
        Assert.Single(page.State.Bulls);
        Assert.Contains(page.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == FamilyQuestion.NotSureSaid);
        window.Close();
    }

    /// <summary>
    /// The whole way, where the blanks are on this computer (never on CI): the phone's analysis of a picture of the 6 in bullseye names no
    /// GroupLab sheet and recognizes the target, with its bull where the fingerprint has it.
    /// </summary>
    [AvaloniaFact]
    public void ThePhonesAnalysisRecognizesAStoreBoughtTarget()
    {
        string blank = Path.Combine(@"C:\Dev\grouplab-local\commercial-targets", SixInch, "blank.png");
        if (!File.Exists(blank))
        {
            return;
        }

        var phone = Started();
        string picture = Path.Combine(phone.CacheFolder, $"store-{Guid.NewGuid():N}.jpg");
        using (var scan = Cv2.ImRead(blank, ImreadModes.Color))
        using (var small = new Mat())
        {
            Cv2.Resize(scan, small, new OpenCvSharp.Size(0, 0), 1 / 6.0, 1 / 6.0, InterpolationFlags.Area);
            Cv2.ImWrite(picture, small);
        }

        var result = PhoneAnalysis.Run(picture, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, null, CancellationToken.None);
        Assert.True(result.AskWhichSheet);
        var seen = Assert.IsType<StoreTargetSeen>(result.Recognized);
        Assert.Equal(SixInch, seen.Recognition.Named?.Target.Id);
        var bull = Assert.Single(seen.Recognition.Named!.Bulls(seen.Width, seen.Height));
        var truth = StoreTargetLibrary.Find(SixInch)!.Fingerprint.Bulls[0];
        Assert.True(Math.Abs(bull.X - (truth.X * 100)) < 2 && Math.Abs(bull.Y - (truth.Y * 100)) < 2, $"bull at {bull}");
    }

    [AvaloniaFact]
    public void TheResultCarriesTheWarningForAStoreBoughtTargetAndNeverForAGroupLabSheet()
    {
        var phone = Started();
        var marked = Unnamed(phone, StoreTargetRecognizer.Decide([]));
        var session = new MarkingSession(marked.State);
        session.PlaceStoreTarget(StoreTargetRecognizer.Decide([C(EightInch, 300, 0.99, 100)]).Named!, 1000, 1000);
        session.AddShot(new PointD(500, 500));
        session.AddShot(new PointD(520, 510));
        var setup = new ShotSetup(Calibre.Of(0.308), 3600);
        var window = new Window { Width = 412, Height = 915, Content = new ResultView(new PhoneResult(session.State, null, null, null), setup, UnitSettings.Imperial, () => { }) };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == StoreTargetMatch.Warning);
        Assert.True(ById(Shown<ResultView>(window), "result-check-scale").IsVisible);
        window.Close();

        session.SetScale(new SheetReference(new HomographyMapping(Homography.Identity), "test"));
        window = new Window { Width = 412, Height = 915, Content = new ResultView(new PhoneResult(session.State, null, null, null), setup, UnitSettings.Imperial, () => { }) };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        Assert.DoesNotContain(window.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == StoreTargetMatch.Warning);
        Assert.DoesNotContain(window.GetVisualDescendants().OfType<Button>(), b => AutomationProperties.GetAutomationId(b) == "result-check-scale");
        window.Close();
    }
}

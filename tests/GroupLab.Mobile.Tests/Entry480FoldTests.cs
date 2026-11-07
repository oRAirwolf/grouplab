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
/// NOTES-FROM-PLANNING.md entry 480 section 4: an iPhone Duo folded and unfolded mid-task keeps everything, as the Galaxy Z Fold 7 does
/// (entry 199). Folding changes only the window's size, so the test changes it between the Duo's outer screen and its inner screen upright and
/// on its side, on the result, in Fix holes, on Capture and in Ballistics' rifle form, and checks the session, the marks, the zoom, the place
/// under the crosshair and half-typed words are all still there. The sizes are the reported ones in points; the simulator's leg in
/// ios-app.yml records the real ones.
/// </summary>
public class Entry480FoldTests
{
    private static readonly TestPhone ThePhone = new();

    /// <summary>Outer, inner upright, inner on its side, and folded again.</summary>
    internal static readonly (double Width, double Height)[] Duo = [(466, 678), (626, 890), (890, 626), (466, 678)];

    private static Window Started()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(ThePhone, Avalonia.Application.Current!, () => "US", null);
        }

        var window = new Window { Width = Duo[0].Width, Height = Duo[0].Height };
        window.Show();
        return window;
    }

    private static void Fold(Window window, (double Width, double Height) size)
    {
        window.Width = size.Width;
        window.Height = size.Height;
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    private static string Words(Button b) => b.Content as string ?? (b.Content as TextBlock)?.Text ?? Avalonia.Automation.AutomationProperties.GetName(b) ?? "";

    private static void Press(Control within, string words)
    {
        var button = within.GetVisualDescendants().OfType<Button>().First(b => Words(b) == words || Words(b).StartsWith(words + ",", StringComparison.Ordinal));
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }

    private static long SessionId(ResultView view) =>
        (long)typeof(ResultView).GetField("sessionId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(view)!;

    private static PhoneResult Sample()
    {
        using var sheet = Cv2.ImRead(Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png"), ImreadModes.Color);
        using var half = new Mat();
        Cv2.Resize(sheet, half, new OpenCvSharp.Size(0, 0), 0.5, 0.5, InterpolationFlags.Area);
        string path = Path.Combine(ThePhone.CacheFolder, "duo.png");
        Cv2.ImWrite(path, half);
        var result = PhoneAnalysis.Run(path, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, null, CancellationToken.None);
        Assert.Null(result.Failure);
        return result;
    }

    [AvaloniaFact]
    public void FoldingAndUnfoldingKeepsTheResultTheMarksTheZoomAndWhatWasTyped()
    {
        var window = Started();
        try
        {
            // The result: the same screen, the same saved session and the same holes at every size.
            var view = new ResultView(Sample(), new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, () => { });
            window.Content = view;
            Dispatcher.UIThread.RunJobs();
            long id = SessionId(view);
            int shots = PhoneAnalysis.Store().Get(id)!.ShotCount;
            foreach (var size in Duo)
            {
                Fold(window, size);
                Assert.Same(view, window.Content);
                Assert.Equal(id, SessionId(view));
                Assert.Equal(shots, PhoneAnalysis.Store().Get(id)!.ShotCount);
                Assert.NotEmpty(view.GetVisualDescendants().OfType<ResultView.SheetPicture>());

                // Section 3: on the inner screen on its side the sheet and the numbers sit side by side; upright and folded, one above the other.
                // Side by side, the sheet takes one column; one above the other, it takes the page's width.
                bool wide = view.GetVisualDescendants().OfType<ResultView.SheetPicture>().First().Bounds.Width < view.Bounds.Width * 0.75;
                Assert.True(wide == (size.Width > size.Height), $"{size}: side by side is {wide}; sheet {view.GetVisualDescendants().OfType<ResultView.SheetPicture>().First().Bounds}, view {view.Bounds}, pictures {view.GetVisualDescendants().OfType<ResultView.SheetPicture>().Count()}");
            }

            // Fix holes, zoomed in on one place: the marks, the zoom and the place under the crosshair stay.
            Press(view, "Fix holes");
            var page = view.GetVisualDescendants().OfType<FixHolesPage>().Single();
            page.Picture.ZoomBy(3);
            page.Picture.CentreOn(new PointD(900, 700));
            Dispatcher.UIThread.RunJobs();
            double zoom = page.Picture.Zoom;
            var centre = page.Picture.Centre;
            int marks = page.State.Shots.Count;
            foreach (var size in Duo)
            {
                Fold(window, size);
                Assert.Same(page, view.GetVisualDescendants().OfType<FixHolesPage>().Single());
                Assert.Equal(marks, page.State.Shots.Count);
                Assert.Equal(zoom, page.Picture.Zoom, 6);
                Assert.True(Math.Abs(page.Picture.Centre.X - centre.X) < 1 && Math.Abs(page.Picture.Centre.Y - centre.Y) < 1,
                    $"{size}: the crosshair moved from {centre} to {page.Picture.Centre}");
            }

            // Capture: the same page, not a new one.
            var capture = new CapturePage();
            window.Content = capture;
            Dispatcher.UIThread.RunJobs();
            foreach (var size in Duo)
            {
                Fold(window, size);
                Assert.Same(capture, window.Content);
            }

            // Ballistics' rifle form with a name half typed: the words stay in the same box.
            var ballistics = new BallisticsPage(openForm: "rifle");
            window.Content = ballistics;
            Dispatcher.UIThread.RunJobs();
            var name = Avalonia.LogicalTree.LogicalExtensions.GetLogicalDescendants(ballistics).OfType<TextBox>().First();
            name.Text = "Half typed rif";
            foreach (var size in Duo)
            {
                Fold(window, size);
                Assert.Same(ballistics, window.Content);
                Assert.Contains(Avalonia.LogicalTree.LogicalExtensions.GetLogicalDescendants(ballistics).OfType<TextBox>(), t => t.Text == "Half typed rif");
            }
        }
        finally
        {
            window.Close();
        }
    }
}

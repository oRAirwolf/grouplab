using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.Core.Ballistics;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 422 on the phone: a bull tapped in Bull by bull lands the picture whole in view, after the page has laid
/// out, and it flashes, or holds a tint where the phone asks for less motion.
/// </summary>
public class Entry422Tests
{
    private static readonly TestPhone ThePhone = new();

    private static void Settle()
    {
        Avalonia.Headless.AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaTheory]
    [InlineData(900, false)]
    [InlineData(640, true)]
    public void ABullChosenInBullByBullLandsThePictureWholeInView(int height, bool lessMotion)
    {
        if (Phone.Platform is null)
        {
            Phone.Start(ThePhone, Avalonia.Application.Current!, () => "US", null);
        }

        var was = Landing.ReducedMotion;
        var window = new Window { Width = 360, Height = height, FontSize = 16 };
        window.Show();
        string copy = Path.Combine(ThePhone.CacheFolder, "chosen-422.png");
        File.Copy(Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png"), copy, overwrite: true);
        try
        {
            Landing.ReducedMotion = () => lessMotion;
            var setup = new ShotSetup(Calibre.Of(0.308), 3600);
            var result = PhoneAnalysis.Run(copy, setup, UnitSettings.Imperial, null, CancellationToken.None);
            Assert.Null(result.Failure);
            var view = new ResultView(result, setup, UnitSettings.Imperial, () => { });
            window.Content = view;
            Settle();

            var scroll = view.PageScroll;
            scroll.Offset = new Vector(0, scroll.Extent.Height);
            Settle();
            var figures = view.GetLogicalDescendants().OfType<FiguresView>().Single();
            figures.BullChosen!(result.State.Bulls.First().Index);
            Settle();

            var picture = view.GetVisualDescendants().OfType<Control>().First(c => c.GetType().Name == "SheetPicture");
            var presenter = scroll.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ScrollContentPresenter>().First();
            double top = picture.TranslatePoint(default, presenter)!.Value.Y;
            Assert.True(top >= -0.5 && top + picture.Bounds.Height <= scroll.Viewport.Height + 0.5,
                $"the picture is from {top:0} to {top + picture.Bounds.Height:0} and the page shows 0 to {scroll.Viewport.Height:0}");
            Assert.Equal((true, !lessMotion, lessMotion), view.PageLanding!.Last);
        }
        finally
        {
            Landing.ReducedMotion = was;
            window.Close();
        }
    }
}

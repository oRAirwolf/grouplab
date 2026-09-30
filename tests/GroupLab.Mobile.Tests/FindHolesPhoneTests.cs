using System.Collections.Immutable;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.Core.Evaluation;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using OpenCvSharp;
using Window = Avalonia.Controls.Window;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 318 section 2 on the phone: "Find holes (Experimental)" is offered in Marking A, at the holes, in GroupLab Dev
/// only, and its proposals are found holes on the page, to keep or remove there, before anything is measured.
/// </summary>
public class FindHolesPhoneTests
{
    private static TestPhone Started()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        return (TestPhone)Phone.Platform!;
    }

    /// <summary>Marking A at the holes on the synthetic black-bull target, 200 pixels an inch, its scale and two aim points set.</summary>
    private static (MarkingAPage Page, IReadOnlyList<PointD> Holes, Window Window) AtTheHoles(TestPhone phone)
    {
        var (picture, holes) = Scoreboard.AnyTargetPicture("black bulls", Scoreboard.AnyTargetSeeds[0]);
        string path = Path.Combine(phone.CacheFolder, $"findholes-{Guid.NewGuid():N}.png");
        using (var mat = Mat.FromPixelData(picture.Height, picture.Width, MatType.CV_8UC1, picture.Pixels))
        {
            Cv2.ImWrite(path, mat);
        }

        var existing = MarkingState.Empty with
        {
            ImagePath = path,
            Scale = new LengthReference(new PointD(0, 0), new PointD(Scoreboard.AnyTargetDpi, 0), 1),
            Calibre = Calibre.Of(Scoreboard.AnyTargetCalibre),
            Bulls = ImmutableList.Create(new BullAim(0, "1", new PointD(350, 450)), new BullAim(1, "2", new PointD(950, 450))),
        };
        var page = new MarkingAPage(path, null, new ShotSetup(Calibre.Of(Scoreboard.AnyTargetCalibre), 3600), UnitSettings.Imperial, _ => { }, () => { }, existing);
        var window = new Window { Width = 412, Height = 915, Content = page };
        window.Show();
        page.Next();
        Dispatcher.UIThread.RunJobs();
        return (page, holes, window);
    }

    private static Button FindButton(MarkingAPage page) =>
        page.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetAutomationId(b) == "marking-find-holes");

    [AvaloniaFact]
    public void InGroupLabDevItProposesTheHolesAsFoundHolesOnThePage()
    {
        var phone = Started();
        phone.IsDevBuild = true;
        try
        {
            var (page, holes, window) = AtTheHoles(phone);
            Assert.True(page.FindHolesOffered);
            var button = FindButton(page);
            Assert.True(button.IsVisible);
            Assert.Equal(FindHoles.Label, Assert.IsType<TextBlock>(button.Content).Text);
            Assert.Contains("(Experimental)", FindHoles.Label, StringComparison.Ordinal);

            page.FindHolesNow();
            var shots = page.State.Shots;
            Assert.NotEmpty(shots);
            Assert.All(shots, s => Assert.True(s.Provenance == ShotProvenance.Automatic && s.Proposal is not null));
            var (found, _, _) = Scoreboard.Match(holes, [.. shots.Select(s => s.Image)], Scoreboard.FoundWithinInches * Scoreboard.AnyTargetDpi);
            Assert.True(found >= 8, $"{found} of {holes.Count} holes proposed");
            Assert.Contains(page.GetVisualDescendants().OfType<TextBlock>(), t => (t.Text ?? "").StartsWith("Find holes proposed", StringComparison.Ordinal));
            window.Close();
        }
        finally
        {
            phone.IsDevBuild = false;
        }
    }

    [AvaloniaFact]
    public void ThePublishedApplicationDoesNotOfferIt()
    {
        var phone = Started();
        phone.IsDevBuild = false;
        var (page, _, window) = AtTheHoles(phone);
        Assert.False(page.FindHolesOffered);
        Assert.False(FindButton(page).IsVisible);

        _ = page.FindHolesAsync();
        Dispatcher.UIThread.RunJobs();
        Assert.Empty(page.State.Shots);
        window.Close();
    }
}

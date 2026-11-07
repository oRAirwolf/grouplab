using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.Core.Marking;
using GroupLab.Tests.Support;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 481 part E, question 108: marking a flatbed scan by hand on the phone, the scale step offers the resolution
/// the file states; accepting it sets the scale and goes on to the aim point, and a picture that states nothing believable offers nothing.
/// </summary>
public class Entry481StatedScalePhoneTests
{
    private static TestPhone Started()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        return (TestPhone)Phone.Platform!;
    }

    [AvaloniaFact]
    public void TheScaleStepOffersTheScansOwnResolution()
    {
        var phone = Started();
        string folder = Directory.CreateDirectory(Path.Combine(phone.CacheFolder, $"stated-{Guid.NewGuid():N}")).FullName;
        try
        {
            string scan = StatedDpiPng.Write(Path.Combine(folder, "scan.png"), 1700, 2200, 300);
            var page = new MarkingAPage(scan, null, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, _ => { }, () => { });
            var window = new Window { Width = 412, Height = 915, Content = page };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.True(page.StatedScaleOffered);
            Assert.Null(page.State.Scale);

            page.UseStatedScale(StatedResolutionScale.Offer(Core.Imaging.ImageMetadataReader.Read(File.ReadAllBytes(scan)), 1700)!);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(300, Math.Round(StatedResolutionScale.DotsPerInchOf(page.State.Scale)!.Value));
            Assert.False(page.StatedScaleOffered);
            window.Close();

            // A file stating the 72 dpi a program writes when it has nothing to say is offered nothing.
            string plain = StatedDpiPng.Write(Path.Combine(folder, "plain.png"), 800, 600, 72);
            var other = new MarkingAPage(plain, null, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, _ => { }, () => { });
            var second = new Window { Width = 412, Height = 915, Content = other };
            second.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.False(other.StatedScaleOffered);
            second.Close();
        }
        finally
        {
            Temp.Delete(folder);
        }
    }
}

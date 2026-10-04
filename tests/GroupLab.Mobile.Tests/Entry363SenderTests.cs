using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Core.Publication;
using Cv2 = OpenCvSharp.Cv2;
using Mat = OpenCvSharp.Mat;
using MatType = OpenCvSharp.MatType;
using Scalar = OpenCvSharp.Scalar;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 363 section 3.5, question 81 A: the phone's sender, off until the stores' privacy answers are updated, then
/// the desktop's package and queue, over Wi-Fi only unless mobile data is allowed.
/// </summary>
public class Entry363SenderTests
{
    private static readonly TestPhone ThePhone = new();

    private static (MarkingState State, string Folder) Target()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(ThePhone, Avalonia.Application.Current!, () => "US", null);
        }

        string picture = Path.Combine(ThePhone.CacheFolder, "sender-363.png");
        using (var image = new Mat(600, 600, MatType.CV_8UC3, new Scalar(230, 230, 230)))
        {
            Cv2.ImWrite(picture, image);
        }

        var session = new MarkingSession();
        session.Open(picture);
        session.SetScale(new LengthReference(new(0, 0), new(100, 0), 1));
        session.AddShot(new(300, 300));
        session.AddShot(new(320, 310));
        return (session.State, Path.Combine(Path.GetDirectoryName(Phone.Settings.Path)!, "pending-targets"));
    }

    [AvaloniaFact]
    public void WhileTheSwitchIsOffNothingIsShownOrSent()
    {
        var (state, _) = Target();
        Phone.Settings.SaveSending(SendingChoice.Always, ConsentLevel.Testing);
        SharingSwitches.TargetsFromPhoneOverride = false;
        try
        {
            Assert.Null(PhoneSending.After(state, state));
        }
        finally
        {
            SharingSwitches.TargetsFromPhoneOverride = null;
        }
    }

    [AvaloniaFact]
    public void AskShowsTheQuestionAndOnMobileDataATargetWaitsForWifi()
    {
        var (state, pending) = Target();
        SharingSwitches.TargetsFromPhoneOverride = true;
        ThePhone.Unmetered = false;
        try
        {
            Phone.Settings.SaveMobileData(false);
            Phone.Settings.SaveSending(SendingChoice.Ask, null);
            var asked = Assert.IsAssignableFrom<Control>(PhoneSending.After(state, state));
            var ids = asked.GetLogicalDescendants().OfType<Button>().Select(Avalonia.Automation.AutomationProperties.GetAutomationId).ToList();
            Assert.Contains("result-send-testing", ids);
            Assert.Contains("result-send-not", ids);

            int before = Directory.Exists(pending) ? Directory.GetDirectories(pending).Length : 0;
            Phone.Settings.SaveSending(SendingChoice.Always, ConsentLevel.Testing);
            var line = Assert.IsType<TextBlock>(PhoneSending.After(state, state));
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            for (int i = 0; i < 100 && line.Text != "Kept to send on Wi-Fi."; i++)
            {
                Thread.Sleep(50);
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            }

            Assert.Equal("Kept to send on Wi-Fi.", line.Text);
            Assert.Equal(before + 1, Directory.GetDirectories(pending).Length);
        }
        finally
        {
            SharingSwitches.TargetsFromPhoneOverride = null;
            ThePhone.Unmetered = null;
            Phone.Settings.SaveSending(SendingChoice.Unset, null);
        }
    }

    [AvaloniaFact]
    public void ThePackageIsTheDesktopsWithItsShots()
    {
        var (state, _) = Target();
        var package = PhoneSending.Package(state, state, ConsentLevel.Testing);
        Assert.NotNull(package);
        Assert.NotEmpty(package.Image);
        Assert.Contains("\"shots\"", package.Json, StringComparison.Ordinal);
    }
}

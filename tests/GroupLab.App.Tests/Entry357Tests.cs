using System.Text.Json.Nodes;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Core.Publication;
using GroupLab.Core.Updates;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 357 section 1 on the desktop, in both states of its switch. Off, which is how the build ships until Alan
/// confirms the store answers: the three choices in their old words, no Do not send this picture, and nothing sent on leaving a picture
/// whatever the settings file says. On: the four choices, the one-time question for somebody who chose every target automatically, every
/// picture sent when it is left with its state and picture code, Do not send this picture keeping one back, and a picture open when
/// GroupLab stopped sent at the next start. Every request goes to the recording outside world.
/// </summary>
public class Entry357Tests
{
    private static RecordedOutsideWorld Outside => TestDefaults.Outside;

    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static PostAnswer Accepted(string id) => new(200, $"{{\"ok\":true,\"id\":\"{id}\"}}");

    private static (MainWindow Window, string Path) Ready(SendingChoice choice, ConsentLevel? level, bool everything)
    {
        Outside.Forget();
        SharingSwitches.EverythingOverride = everything;
        var (window, path, _) = Entry109Tests.Sheet();
        window.ReceiverOpen = true;
        window.SettingsStore.SaveSending(choice, level);
        window.Session.SetCalibre(Calibre.Of(0.308));
        window.Session.SetShotDistance(3600);
        window.CalibreAnswered();
        Settle();
        return (window, path);
    }

    private static void Finish(MainWindow window, string path)
    {
        string settings = window.SettingsStore.Path;
        window.Close();
        Settle();
        Outside.Forget();
        SharingSwitches.EverythingOverride = null;
        GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        foreach (string left in Directory.EnumerateFileSystemEntries(Path.GetDirectoryName(settings)!, Path.GetFileNameWithoutExtension(settings) + ".pending-targets*"))
        {
            if (Directory.Exists(left))
            {
                GroupLab.Tests.Support.Temp.Delete(left);
            }
            else
            {
                File.Delete(left);
            }
        }
    }

    /// <summary>A second picture to open, which leaves the first.</summary>
    private static string Another(string beside)
    {
        string path = Path.Combine(Path.GetDirectoryName(beside)!, "another.png");
        using var image = new OpenCvSharp.Mat(400, 300, OpenCvSharp.MatType.CV_8UC3, new OpenCvSharp.Scalar(220, 220, 220));
        OpenCvSharp.Cv2.ImWrite(path, image);
        return path;
    }

    [AvaloniaFact]
    public void WithTheSwitchOffTheChoicesAndWhatIsSentAreAsBefore()
    {
        var (window, path) = Ready(SendingChoice.Everything, ConsentLevel.Testing, everything: false);
        try
        {
            window.FillSendingSettings();
            var words = window.SendingSettingsText.ToList();
            Assert.Contains("Send every target automatically", words);
            Assert.DoesNotContain(words, w => w.Contains("everything I open", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(words, w => w.Contains("finished targets only", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(SharingWords.DoNotSendThis, window.MenuItems);

            // A settings file that says everything, from a build with the switch on, sends nothing here on leaving a picture.
            Outside.Answer = (_, _, _) => Accepted("T-357-OFF");
            window.OpenImage(Another(path));
            Settle();
            Assert.False(window.SendingEverything);
            Assert.Empty(Outside.Posted);
        }
        finally
        {
            Finish(window, path);
        }
    }

    [AvaloniaFact]
    public void WithTheSwitchOnTheFourChoicesAreOfferedInTheirNewWords()
    {
        var (window, path) = Ready(SendingChoice.Ask, ConsentLevel.Testing, everything: true);
        try
        {
            window.FillSendingSettings();
            var words = window.SendingSettingsText.ToList();
            string[] four = ["Send everything I open, to help improve GroupLab", "Send finished targets only", "Ask me each time", "Never"];
            Assert.Equal(four, words.Where(four.Contains).ToArray());
            Assert.DoesNotContain("Send every target automatically", words);
        }
        finally
        {
            Finish(window, path);
        }
    }

    /// <summary>Nobody is moved silently: somebody who chose every target automatically is asked once, and keeps finished targets until they choose.</summary>
    [AvaloniaFact]
    public void SomebodyWhoSentEveryTargetIsAskedOnceAndKeepsTheirChoice()
    {
        var (window, path) = Ready(SendingChoice.Always, ConsentLevel.Publishable, everything: true);
        try
        {
            Assert.True(window.SettingsStore.EverythingQuestionDue());
            window.ShowFirstRunIfDue();
            Settle();
            Assert.True(window.FirstRunShown);
            Assert.Equal((SendingChoice.Always, (ConsentLevel?)ConsentLevel.Publishable), window.SettingsStore.LoadSending());

            window.PressSend("Send finished targets only");
            Settle();
            Assert.False(window.FirstRunShown);
            Assert.Equal((SendingChoice.Always, (ConsentLevel?)ConsentLevel.Publishable), window.SettingsStore.LoadSending());
            Assert.False(window.SettingsStore.EverythingQuestionDue());

            window.ShowFirstRunIfDue();
            Settle();
            Assert.False(window.FirstRunShown);
        }
        finally
        {
            Finish(window, path);
        }
    }

    [AvaloniaFact]
    public void WithTheSwitchOffSomebodyWhoSentEveryTargetIsNotAsked()
    {
        var (window, path) = Ready(SendingChoice.Always, ConsentLevel.Publishable, everything: false);
        try
        {
            window.ShowFirstRunIfDue();
            Settle();
            Assert.False(window.FirstRunShown);
        }
        finally
        {
            Finish(window, path);
        }
    }

    /// <summary>Under "Send everything I open" a picture goes when it is left, in the state it was left in, with the code its versions share.</summary>
    [AvaloniaFact]
    public void APictureGoesWhenItIsLeftWithItsStateAndPictureCode()
    {
        var (window, path) = Ready(SendingChoice.Everything, ConsentLevel.Testing, everything: true);
        try
        {
            Outside.Answer = (_, _, _) => Accepted("2026-10-03_0000abcd");
            Assert.Contains(SharingWords.DoNotSendThis, window.MenuItems);
            window.OpenImage(Another(path));
            Settle();
            for (int i = 0; i < 50 && Outside.Posted.Count == 0; i++)
            {
                Thread.Sleep(20);
                Settle();
            }

            var (_, package, _) = Assert.Single(Outside.Posted);
            var submission = JsonNode.Parse(package)!["submission"]!;
            Assert.Equal("stopped-at-review", (string?)submission["state"]);
            Assert.Equal(TargetPackages.PictureCode(window.SettingsStore.LoadPictureSalt(), File.ReadAllBytes(path)), (string?)submission["picture"]);
            Assert.Equal("2026-10-03_0000abcd", window.SettingsStore.LoadPictureReference((string)submission["picture"]!));
        }
        finally
        {
            Finish(window, path);
        }
    }

    [AvaloniaFact]
    public void DoNotSendThisPictureKeepsItBackUntilItIsLeft()
    {
        var (window, path) = Ready(SendingChoice.Everything, ConsentLevel.Testing, everything: true);
        try
        {
            Outside.Answer = (_, _, _) => Accepted("2026-10-03_0000abce");
            window.DoNotSendThisPicture();
            Assert.True(window.NotSendingThisPicture);
            string next = Another(path);
            window.OpenImage(next);
            Settle();
            Assert.Empty(Outside.Posted);
            Assert.False(window.NotSendingThisPicture);
        }
        finally
        {
            Finish(window, path);
        }
    }

    /// <summary>A picture open when GroupLab stopped without closing is sent at the next start, as unread.</summary>
    [AvaloniaFact]
    public void APictureOpenWhenGroupLabStoppedGoesAtTheNextStart()
    {
        var (window, path) = Ready(SendingChoice.Everything, ConsentLevel.Testing, everything: true);
        try
        {
            Outside.Answer = (_, _, _) => Accepted("2026-10-03_0000abd0");
            string next = Another(path);
            window.OpenImage(next);
            Settle();
            for (int i = 0; i < 50 && Outside.Posted.Count == 0; i++)
            {
                Thread.Sleep(20);
                Settle();
            }

            Outside.Forget();
            // The next start, as a window with the same settings finds the note the picture left.
            var again = new MainWindow(window.SettingsStore) { ReceiverOpen = true };
            Outside.Answer = (_, _, _) => Accepted("2026-10-03_0000abcf");
            again.SendPictureLeftOpen();
            _ = again.RetryPendingAsync();
            Settle();
            for (int i = 0; i < 50 && Outside.Posted.Count == 0; i++)
            {
                Thread.Sleep(20);
                Settle();
            }

            var (_, package, _) = Assert.Single(Outside.Posted);
            Assert.Equal("unread", (string?)JsonNode.Parse(package)!["submission"]!["state"]);
        }
        finally
        {
            Finish(window, path);
        }
    }
}

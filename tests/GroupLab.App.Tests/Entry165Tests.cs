using System.Text.Json.Nodes;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Core.Publication;
using GroupLab.Core.Updates;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 165 section 7, the application half: nothing is sent without a yes, Never sends nothing, Not this time is
/// final for the target, the question and the first run screen never appear while the receiver is closed, the first run screen asks once and
/// writes the same setting Settings shows, and a target that could not go is kept and sent on the next try. Every request goes to the
/// recording outside world, so no test reaches the network.
/// </summary>
public class Entry165Tests
{
    private static RecordedOutsideWorld Outside => TestDefaults.Outside;

    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static PostAnswer Accepted(string id) => new(200, $"{{\"ok\":true,\"id\":\"{id}\"}}");

    /// <summary>The detected sheet, with calibre and distance answered so Accept and analyze runs, the receiver open, and the choice set.</summary>
    private static (MainWindow Window, string Path) Ready(SendingChoice choice, ConsentLevel? level, bool open = true)
    {
        Outside.Forget();
        var (window, path, _) = Entry109Tests.Sheet();
        window.ReceiverOpen = open;
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
        Outside.Forget();
        GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        string pending = Path.Combine(Path.GetDirectoryName(settings)!, Path.GetFileNameWithoutExtension(settings) + ".pending-targets");
        if (Directory.Exists(pending))
        {
            GroupLab.Tests.Support.Temp.Delete(pending);
        }
    }

    /// <summary>Section 7 item 1: under Ask every time nothing goes until Send is pressed, and Send cannot be pressed before a level is chosen.</summary>
    [AvaloniaFact]
    public void NothingIsSentWithoutAYes()
    {
        var (window, path) = Ready(SendingChoice.Ask, null);
        try
        {
            Outside.Answer = (_, _, _) => Accepted("T-165-ASK");
            window.Analyse();
            Settle();
            Assert.True(window.QuestionShown);
            Assert.Contains("Help improve GroupLab's detection?", window.SendText);
            Assert.False(window.SendEnabled);

            window.PressSend("What gets sent");
            Settle();
            Assert.Empty(Outside.Posted);
            Assert.Contains(window.SendText, t => t.StartsWith("About ", StringComparison.Ordinal) && t.Contains("MB", StringComparison.Ordinal));

            window.PressSend("Testing only");
            Settle();
            Assert.True(window.SendEnabled);
            Assert.Empty(Outside.Posted);
            Assert.Contains(window.SendText, t => t == "You agree to: " + ReceiverTerms.Current.TestingText);

            window.PressSend("Send");
            Settle();
            var (address, package, image) = Assert.Single(Outside.Posted);
            Assert.Equal(ReceiverTerms.Current.AppReceiver, address);
            var json = JsonNode.Parse(package)!;
            Assert.Equal("testing", (string)json["consent"]!["level"]!);
            Assert.Equal(image.LongLength, (long)json["manifest"]!["image"]!["bytes"]!);
            Assert.False(window.QuestionShown);
            Assert.Contains("Sent to the project. Its reference is T-165-ASK.", window.SendText);
            Assert.Equal(["T-165-ASK"], window.SettingsStore.LoadSent());

            // Accepting the same target again does not ask again.
            window.Analyse();
            Settle();
            Assert.False(window.QuestionShown);
            Assert.Single(Outside.Posted);
        }
        finally
        {
            Finish(window, path);
        }
    }

    /// <summary>Section 7 item 1 and section 1: Never asks nothing and sends nothing; Send every target automatically sends without asking.</summary>
    [AvaloniaFact]
    public void NeverSendsNothingAndAlwaysSendsWithoutAsking()
    {
        var (window, path) = Ready(SendingChoice.Never, ConsentLevel.Publishable);
        try
        {
            Outside.Answer = (_, _, _) => Accepted("T-165-ALWAYS");
            window.Analyse();
            Settle();
            Assert.False(window.QuestionShown);
            Assert.Empty(Outside.Posted);

            window.SettingsStore.SaveSending(SendingChoice.Always, ConsentLevel.Publishable);
            window.Analyse();
            Settle();
            Assert.False(window.QuestionShown);
            var (_, package, _) = Assert.Single(Outside.Posted);
            Assert.Equal("publishable", (string)JsonNode.Parse(package)!["consent"]!["level"]!);
            Assert.Equal(ReceiverTerms.Current.PublishableText, (string)JsonNode.Parse(package)!["consent"]!["text"]!);
        }
        finally
        {
            Finish(window, path);
        }
    }

    /// <summary>Section 1 item 4: Not this time is final for that target, and nothing is sent.</summary>
    [AvaloniaFact]
    public void NotThisTimeIsFinalForTheTarget()
    {
        var (window, path) = Ready(SendingChoice.Ask, ConsentLevel.Testing);
        try
        {
            window.Analyse();
            Settle();
            Assert.True(window.QuestionShown);
            Assert.True(window.SendEnabled);
            window.PressSend("Not this time");
            Settle();
            Assert.False(window.QuestionShown);
            window.Analyse();
            Settle();
            Assert.False(window.QuestionShown);
            Assert.Empty(Outside.Posted);
        }
        finally
        {
            Finish(window, path);
        }
    }

    /// <summary>Section 7 item 6: while the receiver is closed there is no question, no first run screen and no request, whatever is set.</summary>
    [AvaloniaFact]
    public void NothingAppearsWhileTheReceiverIsClosed()
    {
        foreach (var (choice, level) in new (SendingChoice, ConsentLevel?)[] { (SendingChoice.Always, ConsentLevel.Testing), (SendingChoice.Ask, null), (SendingChoice.Unset, null) })
        {
            var (window, path) = Ready(choice, level, open: false);
            try
            {
                Outside.Answer = (_, _, _) => Accepted("T-165-CLOSED");
                window.ShowFirstRunIfDue();
                window.Analyse();
                Settle();
                Assert.False(window.FirstRunShown);
                Assert.False(window.QuestionShown);
                Assert.Empty(Outside.Posted);
                window.FillSendingSettings();
                Assert.Contains(window.SendingSettingsText, t => t.Contains("not open yet", StringComparison.Ordinal));
            }
            finally
            {
                Finish(window, path);
            }
        }
    }

    /// <summary>
    /// Section 1.1 and section 9: the first run screen asks once, will not send every target before a level is chosen, and writes the one
    /// setting the Settings section then shows.
    /// </summary>
    [AvaloniaFact]
    public void TheFirstRunScreenAsksOnceAndSettingsShowsTheAnswer()
    {
        var (window, path) = Ready(SendingChoice.Unset, null);
        try
        {
            window.ShowFirstRunIfDue();
            Settle();
            Assert.True(window.FirstRunShown);

            window.PressSend("Send every target automatically");
            Settle();
            Assert.True(window.FirstRunShown);
            Assert.Equal((SendingChoice.Unset, (ConsentLevel?)null), window.SettingsStore.LoadSending());

            window.PressSend("Testing only");
            window.PressSend("Ask me each time");
            Settle();
            Assert.False(window.FirstRunShown);
            Assert.Equal((SendingChoice.Ask, (ConsentLevel?)ConsentLevel.Testing), window.SettingsStore.LoadSending());

            window.ShowFirstRunIfDue();
            Assert.False(window.FirstRunShown);

            var text = window.SendingSettingsText.ToList();
            Assert.Contains("Send every target automatically", text);
            Assert.Contains("Ask me each time", text);
            Assert.Contains("Never", text);
            Assert.Contains("Testing only. " + ReceiverTerms.Current.TestingText, text);
            Assert.Contains("May be published. " + ReceiverTerms.Current.PublishableText, text);
            Assert.Contains("• " + TargetPackages.WhatIsSent[0], text);
            Assert.Contains("No targets have been sent from this computer.", text);
            Assert.Empty(Outside.Posted);
        }
        finally
        {
            Finish(window, path);
        }
    }

    /// <summary>
    /// Section 6: a target that met no answer is kept and tried again, and sent then; one refused for a reason retrying cannot fix is let go at
    /// once; and one kept seven days is let go without being sent.
    /// </summary>
    [AvaloniaFact]
    public async Task ATargetThatCouldNotGoIsKeptAndSentOnTheNextTry()
    {
        var (window, path) = Ready(SendingChoice.Always, ConsentLevel.Testing);
        try
        {
            Outside.Answer = null;
            window.Analyse();
            Settle();
            Assert.Single(Outside.Posted);
            Assert.Contains(window.SendText, t => t.Contains("it is kept and will be tried again", StringComparison.Ordinal));
            window.FillSendingSettings();
            Assert.Contains(window.SendingSettingsText, t => t.StartsWith("1 target is waiting to be sent", StringComparison.Ordinal));

            Outside.Answer = (_, _, _) => Accepted("T-165-RETRY");
            await window.RetryPendingAsync();
            Settle();
            Assert.Equal(2, Outside.Posted.Count);
            Assert.Equal(Outside.Posted[0].Package, Outside.Posted[1].Package);
            Assert.Equal(["T-165-RETRY"], window.SettingsStore.LoadSent());
            Assert.DoesNotContain(window.SendingSettingsText, t => t.Contains("waiting to be sent", StringComparison.Ordinal));
            Assert.Contains(window.SendingSettingsText, t => t.StartsWith("1 target has been sent from this computer: T-165-RETRY.", StringComparison.Ordinal));
        }
        finally
        {
            Finish(window, path);
        }

        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-pending-{Guid.NewGuid():N}");
        try
        {
            Outside.Forget();
            var sender = new TargetSender(Outside, folder);
            var package = new TargetPackage([1, 2, 3], "target.png", "{}");
            Outside.Answer = (_, _, _) => new PostAnswer(400, "{\"ok\":false,\"error\":\"The package is not one GroupLab reads.\",\"retry\":false}");
            var refused = await sender.SendAsync(package, "https://example.invalid/app", CancellationToken.None);
            Assert.Equal(SendResult.Refused, refused.Result);
            Assert.Empty(sender.Pending());

            string old = Path.Combine(folder, "20260901000000-0badf00d");
            Directory.CreateDirectory(old);
            File.WriteAllBytes(Path.Combine(old, "image"), [1, 2, 3]);
            File.WriteAllText(Path.Combine(old, "name.txt"), "target.png");
            File.WriteAllText(Path.Combine(old, "package.json"), "{}");
            Outside.Forget();
            var outcomes = await sender.RetryAsync("https://example.invalid/app", new DateTime(2026, 9, 24, 0, 0, 0, DateTimeKind.Utc), CancellationToken.None);
            Assert.Empty(outcomes);
            Assert.Empty(Outside.Posted);
            Assert.Empty(sender.Pending());
        }
        finally
        {
            Outside.Forget();
            if (Directory.Exists(folder))
            {
                GroupLab.Tests.Support.Temp.Delete(folder);
            }
        }
    }
}

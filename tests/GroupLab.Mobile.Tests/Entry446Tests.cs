using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.Mobile.Dev;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 446: the phone's "Walk me through a demo". Every step outlines a control that is on its screen, the sample is
/// GroupLab's own and comes from the application's files, nothing is kept unless chosen, the card fits at the largest text size, and a
/// screen reader hears each step with its number.
/// </summary>
public class Entry446Tests
{
    private static (Window Window, Shell Shell) Shown()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        Scenario.AnswerFirstRun(Phone.Settings);
        Phone.Settings.SaveDemoStep(0);
        var shell = new Shell();
        var window = new Window { Width = 402, Height = 874, Content = shell };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, shell);
    }

    private static async Task Until(Func<bool> done, int seconds = 20)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (!done() && clock.Elapsed < TimeSpan.FromSeconds(seconds))
        {
            Dispatcher.UIThread.RunJobs();
            await Task.Delay(100);
        }
    }

    [AvaloniaFact]
    public async Task EveryStepOutlinesItsControlAndNothingIsLeftBehind()
    {
        var (window, _) = Shown();
        try
        {
            var results = await Scenario.Run("""{ "name": "the demo", "steps": [ { "do": "demo" } ] }""");
            var step = results["steps"]!.AsArray().Single()!;
            Assert.True(step["ok"]!.GetValue<bool>(), step["detail"]!.GetValue<string>());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task TheSampleIsMadeByGroupLabAndKeptOnlyWhenChosen()
    {
        var (window, shell) = Shown();
        int before = PhoneAnalysis.Store().List().Count;
        try
        {
            await shell.Demo.Start(PhoneDemo.Steps.Count - 1);
            Assert.True(shell.Demo.Running);
            Assert.Equal(PhoneDemo.Steps.Count - 1, shell.Demo.At);
            string sample = shell.Demo.SamplePath!;
            Assert.StartsWith(Path.Combine(PhoneAnalysis.Files, "demo"), sample, StringComparison.Ordinal);
            Assert.True(shell.Demo.Keep.IsVisible, "the last step offers to keep what the demo made");
            shell.Demo.Keep.IsChecked = true;
            await shell.Demo.Next();
            Assert.False(shell.Demo.Running);
            Assert.Equal(before + 1, PhoneAnalysis.Store().List().Count);
            Assert.Equal(0, Phone.Settings.LoadDemoStep());
        }
        finally
        {
            foreach (var s in PhoneAnalysis.Store().List().OrderByDescending(s => s.Id).Take(PhoneAnalysis.Store().List().Count - before).ToList())
            {
                PhoneAnalysis.Store().Delete(s.Id);
            }

            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task EndingPartWayRemovesWhatItMadeAndRemembersTheStep()
    {
        var (window, shell) = Shown();
        int before = PhoneAnalysis.Store().List().Count;
        try
        {
            await shell.Demo.Start();
            await shell.Demo.Next();
            await shell.Demo.Next();
            Assert.Equal(2, shell.Demo.At);
            Assert.Equal(before + 1, PhoneAnalysis.Store().List().Count);
            string sample = shell.Demo.SamplePath!;
            // The phone's Back ends the tour, as its close button does.
            Assert.True(shell.Back());
            Assert.False(shell.Demo.Running);
            Assert.Equal(before, PhoneAnalysis.Store().List().Count);
            Assert.False(File.Exists(sample));
            Assert.Equal(0, shell.Targets.Count);
            Assert.Equal(2, Phone.Settings.LoadDemoStep());

            // Settings offers it from the start and from where it was left, on its General page (entry 468).
            shell.ShowSettings(GroupLab.App.SettingsCategory.General);
            Dispatcher.UIThread.RunJobs();
            var carry = shell.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetAutomationId(b) == "settings-demo-carry-on");
            Assert.Equal(DemoWords.CarryOn(3, PhoneDemo.Steps.Count), ((TextBlock)carry.Content!).Text);
            Assert.Single(shell.GetVisualDescendants().OfType<Button>(), b => AutomationProperties.GetAutomationId(b) == "settings-demo");
        }
        finally
        {
            shell.Demo.End(keep: false);
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task TheCardFitsAtTheLargestTextSizeAndSaysItsStep()
    {
        var (window, shell) = Shown();
        AndroidText.LargestUntilClosed(window);
        try
        {
            await shell.Demo.Start();
            for (int i = 0; i < PhoneDemo.Steps.Count; i++)
            {
                var card = shell.Demo.Card;
                await Until(() => shell.Demo.Spotlit is not null && !double.IsNaN(Canvas.GetLeft(card)) && card.Bounds.Width > 0);
                await Task.Delay(200);
                Dispatcher.UIThread.RunJobs();
                var layer = shell.Demo.Layer.Bounds;
                double top = Canvas.GetTop(card), left = Canvas.GetLeft(card);
                Assert.True(left >= 0 && left + card.Bounds.Width <= layer.Width + 0.5, $"step {i + 1}: the card runs off the side, {left} + {card.Bounds.Width} in {layer.Width}");
                Assert.True(top >= 0 && top + card.Bounds.Height <= layer.Height + 0.5, $"step {i + 1}: the card runs off the top or the bottom, {top} + {card.Bounds.Height} in {layer.Height}");
                // The card scrolls where its words are taller than the room, so every word and both buttons are still reachable.
                var scroll = card.GetVisualDescendants().OfType<ScrollViewer>().First();
                Assert.True(scroll.Extent.Height <= scroll.Viewport.Height + 0.5 || scroll.Viewport.Height >= 2 * Screens.Touch, $"step {i + 1}: no room to scroll");
                string name = AutomationProperties.GetName(card) ?? "";
                Assert.StartsWith($"{DemoWords.Of(i + 1, PhoneDemo.Steps.Count)}: {PhoneDemo.Steps[i].Title}.", name, StringComparison.Ordinal);
                await shell.Demo.Next();
            }

            Assert.False(shell.Demo.Running);
        }
        finally
        {
            shell.Demo.End(keep: false);
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TheOfferComesOnceAndBackClosesIt()
    {
        var (window, shell) = Shown();
        try
        {
            var store = Phone.Settings;
            if (File.Exists(store.Path) && System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(store.Path)) is System.Text.Json.Nodes.JsonObject file)
            {
                file.Remove("demoOffered");
                File.WriteAllText(store.Path, file.ToJsonString());
            }

            Assert.False(store.LoadDemoOffered());
            shell.Demo.OfferIfDue();
            Assert.True(shell.Demo.OfferShown);
            Assert.True(store.LoadDemoOffered());
            Assert.True(shell.Back());
            Assert.False(shell.Demo.OfferShown);
            shell.Demo.OfferIfDue();
            Assert.False(shell.Demo.OfferShown);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void EveryStepIsOnAPlaceAndTheSampleSheetIsTheDesktops()
    {
        Assert.Equal(PhoneDemo.Steps.Count, PhoneDemo.Steps.Select(s => s.Title).Distinct().Count());
        Assert.All(PhoneDemo.Steps, s => Assert.False(string.IsNullOrWhiteSpace(s.Says)));
        Assert.Equal(DemoAct.OpenSample, PhoneDemo.Steps[0].Act);
        Assert.Equal(GroupLab.Core.Detection.DemoSample.SheetFile, GroupLab.Core.Survey.Benchmark.SheetFile);
        Assert.Contains(PhoneDemo.Steps, s => s.Place == Shell.Place.Sessions);
        Assert.Contains(PhoneDemo.Steps, s => s.Place == Shell.Place.Ballistics);
        Assert.Contains(PhoneDemo.Steps, s => s.Place == Shell.Place.Targets);
    }
}

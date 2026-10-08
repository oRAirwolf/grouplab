using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.Mobile.Dev;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 388 sections 1 and 4: the emulator's two walks run here first, headlessly, as the sweep is
/// (<see cref="PhoneSweepScenarioTests"/>). The first emulator run of the new screens (37735635656) waited for the second target's result
/// and found the first one still on screen, pictured the reading instead, and left it running; every later start of GroupLab Dev on that
/// emulator then ran nothing. Here every step must work, and each wait must find the screen it was written for.
/// </summary>
public class PhoneScreensScenarioTests
{
    private static async Task<System.Text.Json.Nodes.JsonObject> RunWalk(string walk, params (string From, string As)[] files)
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        Scenario.AnswerFirstRun(Phone.Settings);
        Directory.CreateDirectory(Scenario.Folder);
        foreach (var (from, name) in files)
        {
            File.Copy(from, Path.Combine(Scenario.Folder, name), overwrite: true);
        }

        var window = new Window { Width = 412, Height = 915, Content = new Shell() };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try
        {
            return await Scenario.Run(File.ReadAllText(Repo.PathTo("scripts", "scenarios", walk)));
        }
        finally
        {
            window.Close();
            foreach (var (_, name) in files)
            {
                GroupLab.Tests.Support.Temp.DeleteFile(Path.Combine(Scenario.Folder, name));
            }
        }
    }

    private static List<string> Failed(System.Text.Json.Nodes.JsonObject results) => [.. results["steps"]!.AsArray()
        .Where(s => !s!["ok"]!.GetValue<bool>() && !PhoneSweepScenarioTests.MayFindNothing.Any(m => s["detail"]!.GetValue<string>().Contains(m, StringComparison.Ordinal)))
        .Select(s => $"{s!["step"]} {s["do"]}: {s["detail"]}")];

    [AvaloniaFact]
    public async Task TheScreenshotWalkReachesEveryScreenItPictures()
    {
        var results = await RunWalk("phone-screens.json",
            (Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png"), "sample.png"),
            (Repo.PathTo("scripts", "scenarios", "stand-in-poster.jpg"), "stand-in-poster.jpg"));
        var failed = Failed(results);
        Assert.True(failed.Count == 0, string.Join(Environment.NewLine, failed));
        foreach (string screen in new[] { "scale-markers", "fingerprint", "pairing", "open-targets" })
        {
            Assert.True(File.Exists(Path.Combine(Scenario.Results, screen + ".png")), screen + " was not pictured");
            Assert.True(File.Exists(Path.Combine(Scenario.Results, screen + ".quality.json")), screen + " has no quality file");
        }
    }

    [AvaloniaFact]
    public async Task TheBaselineWalkReadsThreeTargetsAndGoesRoundThem()
    {
        var results = await RunWalk("phone-perf.json", (Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png"), "scan.png"));
        var failed = Failed(results);
        Assert.True(failed.Count == 0, string.Join(Environment.NewLine, failed));
        Assert.Equal(6, results["steps"]!.AsArray().Count(s => s!["do"]!.GetValue<string>() == "press" && s["detail"]!.GetValue<string>().StartsWith("open-targets-row-", StringComparison.Ordinal)));
    }
}

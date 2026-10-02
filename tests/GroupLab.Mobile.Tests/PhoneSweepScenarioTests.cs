using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.Mobile.Dev;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 342, worker A item 2: the screen sweep the iOS simulator runs in CI (scripts/scenarios/phone-sweep.json, from
/// ios-app.yml) run here first, headlessly, so a renamed button or a page that no longer opens fails on the push that did it rather than on
/// the next simulator run. Every step must work but pressing past the picture check, which shows only when a picture needs a word.
/// </summary>
public class PhoneSweepScenarioTests
{
    /// <summary>The steps allowed to find nothing: the picture check appears only when a picture needs a word.</summary>
    internal static readonly string[] MayFindNothing = ["Use this picture|Use it anyway"];

    [AvaloniaFact]
    public async Task TheSimulatorsSweepWorksStepByStep()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        Scenario.AnswerFirstRun(Phone.Settings);
        Directory.CreateDirectory(Scenario.Folder);
        File.Copy(Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png"), Path.Combine(Scenario.Folder, "sample.png"), overwrite: true);
        var shell = new Shell();
        var window = new Window { Width = 402, Height = 874, Content = shell };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try
        {
            var results = await Scenario.Run(File.ReadAllText(Repo.PathTo("scripts", "scenarios", "phone-sweep.json")));
            var failed = results["steps"]!.AsArray()
                .Where(s => !s!["ok"]!.GetValue<bool>() && !MayFindNothing.Any(m => s["detail"]!.GetValue<string>().Contains(m, StringComparison.Ordinal)))
                .Select(s => $"{s!["step"]} {s["do"]}: {s["detail"]}").ToList();
            Assert.True(failed.Count == 0, string.Join(Environment.NewLine, failed));
            Assert.True(Directory.EnumerateFiles(Scenario.Results, "*.png").Count() >= 11, "fewer screenshots than the sweep takes");
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.DeleteFile(Path.Combine(Scenario.Folder, "sample.png"));
        }
    }
}

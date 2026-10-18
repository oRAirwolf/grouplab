using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.Mobile.Dev;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 315 section 2 and its amendment, sections 1 and 5: GroupLab Dev runs a scenario file with nobody's hands and
/// writes what each step did, and the shared library the public application is built from carries none of it.
/// </summary>
public class ScenarioTests
{
    /// <summary>
    /// Entry 447 section 3: while a scenario runs, a real press and release on the window are logged with where they landed and the control
    /// they landed on, so a lost tap on the simulator says whether it reached the application at all.
    /// </summary>
    [AvaloniaFact]
    public async Task APressDuringAScenarioIsLoggedWithWhereItLanded()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        Scenario.AnswerFirstRun(Phone.Settings);
        var shell = new Shell();
        var window = new Window { Width = 412, Height = 915, Content = shell };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try
        {
            var running = Scenario.Run("""{ "name": "presses", "steps": [ { "do": "open", "place": "capture" }, { "do": "sleep", "seconds": 2 }, { "do": "log", "lines": 50 } ] }""");
            await Task.Delay(700);
            Dispatcher.UIThread.RunJobs();
            var target = shell.GetVisualDescendants().OfType<Button>().First(b => AutomationProperties.GetAutomationId(b) == "capture-change");
            var at = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), window)!.Value;
            // A press is judged against what was last drawn, so a frame is drawn first.
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            window.MouseDown(at, MouseButton.Left);
            window.MouseUp(at, MouseButton.Left);
            await running;
            string log = File.ReadAllText(Path.Combine(Scenario.Results, "log.txt"));
            Assert.True(log.Contains("scenario.pointer", StringComparison.Ordinal), log[^Math.Min(log.Length, 1500)..]);
            Assert.Contains("what=pressed", log, StringComparison.Ordinal);
            Assert.Contains("what=released", log, StringComparison.Ordinal);
            Assert.Contains("id=capture-change", log, StringComparison.Ordinal);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void ThePublicLibraryHasNoneOfTheDeveloperTools()
    {
        var developer = typeof(Phone).Assembly.GetTypes()
            .Where(t => t.Namespace is { } space && space.StartsWith("GroupLab.Mobile.Dev", StringComparison.Ordinal)).Select(t => t.FullName).ToList();
        Assert.True(developer.Count == 0, "the public build carries GroupLab Dev's tools: " + string.Join(", ", developer));
        Assert.Null(typeof(Phone).Assembly.GetType("GroupLab.Mobile.Dev.Scenario"));

        // And every file of them is compiled only into GroupLab Dev.
        foreach (string file in Directory.EnumerateFiles(Repo.PathTo("mobile", "GroupLab.Mobile", "Dev"), "*.cs"))
        {
            Assert.True(File.ReadAllText(file).StartsWith("#if GROUPLAB_DEV", StringComparison.Ordinal), Path.GetFileName(file) + " is not compiled out of the public build");
        }
    }

    [Fact]
    public void AScenarioIsReadWithItsStepsAndABadOneSaysWhy()
    {
        var read = Scenario.Parse("""{ "name": "n", "stopOnFailure": false, "steps": [ { "do": "Open", "place": "settings" }, { "do": "sleep", "seconds": 1 } ] }""", out _);
        Assert.NotNull(read);
        Assert.Equal("n", read.Value.Name);
        Assert.False(read.Value.StopOnFailure);
        Assert.Equal(["open", "sleep"], read.Value.Steps.Select(s => s.Do));
        Assert.Equal("settings", read.Value.Steps[0].Text("place"));
        Assert.Null(Scenario.Parse("""{ "steps": [ { "place": "x" } ] }""", out string? why));
        Assert.Contains("\"do\"", why, StringComparison.Ordinal);
        Assert.Null(Scenario.Parse("not json", out why));
        Assert.Equal("result", Scenario.Name("../../result", "x"));
        Assert.Equal("x", Scenario.Name("///", "x"));
    }

    [AvaloniaFact]
    public async Task AScenarioOpensAPlacePressesAButtonAndWritesWhatItDid()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        // As a scenario's start does: the first run's questions answered, so the screens behind them can be opened.
        Scenario.AnswerFirstRun(Phone.Settings);
        var shell = new Shell();
        var window = new Window { Width = 412, Height = 915, Content = shell };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try
        {
            var results = await Scenario.Run("""
                { "name": "settings", "stopOnFailure": false, "steps": [
                  { "do": "open", "place": "settings" },
                  { "do": "wait", "text": "Send diagnostics", "seconds": 5 },
                  { "do": "tree", "name": "settings" },
                  { "do": "screenshot", "name": "settings" },
                  { "do": "press", "name": "Send diagnostics" },
                  { "do": "press", "name": "No such button" },
                  { "do": "fly" },
                  { "do": "log", "lines": 20 } ] }
                """);
            var steps = results["steps"]!.AsArray().Select(s => (s!["do"]!.GetValue<string>(), s["ok"]!.GetValue<bool>())).ToList();
            (string, bool)[] expected = [("open", true), ("wait", true), ("tree", true), ("screenshot", true), ("press", true), ("press", false), ("fly", false), ("log", true)];
            Assert.True(steps.SequenceEqual(expected), results.ToJsonString() + "\n" + File.ReadAllText(Path.Combine(Scenario.Results, "settings.json")));
            Assert.False(results["ok"]!.GetValue<bool>());
            Assert.Equal("done", File.ReadAllText(Path.Combine(Scenario.Results, "status")));
            Assert.True(new FileInfo(Path.Combine(Scenario.Results, "settings.png")).Length > 1000);
            var tree = JsonNode.Parse(File.ReadAllText(Path.Combine(Scenario.Results, "settings.json")))!.AsArray();
            Assert.Contains(tree, e => e!["text"]?.GetValue<string>() == "Send diagnostics" && e["type"]!.GetValue<string>() == "Button");
            Assert.Contains("scenario.step", File.ReadAllText(Path.Combine(Scenario.Results, "log.txt")), StringComparison.Ordinal);
            Assert.True(File.Exists(Path.Combine(Scenario.Results, "results.json")));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void AScenarioRunsOnceAndOnlyFromItsOwnFolder()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        Directory.CreateDirectory(Scenario.Folder);
        Assert.Null(Scenario.Requested("../settings.json"));
        Assert.Null(Scenario.Requested("missing.json"));
        string waiting = Path.Combine(Scenario.Folder, Scenario.Waiting);
        File.WriteAllText(waiting, """{ "steps": [] }""");
        Assert.Equal(waiting, Scenario.Requested(null));

        // Read once before the start: renamed, so the next start does not run it again.
        Assert.True(Scenario.Prepare(Phone.Platform!.FilesFolder, null));
        Assert.False(File.Exists(waiting));
        Assert.True(File.Exists(waiting + ".ran"));
        Assert.Null(Scenario.Requested(null));
        Assert.NotEqual(GroupLab.App.ScopeAnswer.Unset, Phone.Settings.LoadScopeAnswer());
        File.Delete(waiting + ".ran");
    }
}

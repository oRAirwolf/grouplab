using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.Mobile.Dev;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 353 step 2: the real-touch scenario the iOS simulator and the Android emulator run with each nightly that
/// changes the application (scripts/scenarios/phone-touch.json, tapped by scripts/touch-test.py), run here first with this test as the
/// script outside. It reads each hold as the script does, takes the middle of the named control's place on the screen, and presses there
/// as a finger does: down, the layout pass a real screen runs, then up at the same point. Where the tap lands in a field, the keyboard
/// opens as the system's would. On build 157's keyboard handling, Continue and Done with the keyboard up fail here (entry 353's report).
/// </summary>
public class PhoneTouchScenarioTests
{
    private const double Height = 874;
    private const double KeyboardHeight = 336;

    private static void Settle()
    {
        Dispatcher.UIThread.RunJobs();
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>The point the script taps for a hold, by the same rule as scripts/touch-test.py; null with why where it would refuse.</summary>
    internal static (Point? At, string Why) TapPoint(JsonObject hold)
    {
        string tap = hold["tap"]!.GetValue<string>();
        var control = hold["controls"]!.AsArray().OfType<JsonObject>().FirstOrDefault(c => c["id"]?.GetValue<string>() == tap);
        if (control is null)
        {
            return (null, $"no control with the id {tap} is showing");
        }

        if (control["enabled"]?.GetValue<bool>() != true)
        {
            return (null, tap + " is not enabled");
        }

        if (hold["reached"]?.GetValue<bool>() == false)
        {
            return (null, $"{tap} is covered at its middle by {hold["under"]}");
        }

        var screen = control["screen"]!.AsObject();
        double x = screen["x"]!.GetValue<double>() + screen["width"]!.GetValue<double>() / 2;
        double y = screen["y"]!.GetValue<double>() + screen["height"]!.GetValue<double>() / 2;
        if (hold["coveredFrom"]?.GetValue<double>() is { } covered && y >= covered)
        {
            return (null, tap + " is under the keyboard");
        }

        return (new Point(x, y), $"tapped at {x:0},{y:0}");
    }

    [AvaloniaFact]
    public async Task EveryTapDoesItsJobAsAFingerPressesIt()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        var phone = (TestPhone)Phone.Platform!;
        var (was, wasInches) = Phone.Settings.LoadShotSetup();
        Scenario.AnswerFirstRun(Phone.Settings);
        // As the scenario's "caliber": "" has it on a device: no caliber yet, so Take a picture asks for one first.
        Phone.Settings.SaveShotSetup(null, null);
        phone.AllowCamera = true;
        var shell = new Shell();
        var window = new Window { Width = 402, Height = Height, Content = shell };
        window.Show();
        Settle();
        try
        {
            var run = Scenario.Run(File.ReadAllText(Repo.PathTo("scripts", "scenarios", "phone-touch.json")));
            string hold = Path.Combine(Scenario.Results, Scenario.HoldFile);
            var taps = new List<string>();
            var clock = System.Diagnostics.Stopwatch.StartNew();
            while (!run.IsCompleted && clock.Elapsed < TimeSpan.FromMinutes(3))
            {
                if (File.Exists(hold))
                {
                    var said = JsonNode.Parse(File.ReadAllText(hold))!.AsObject();
                    Assert.Equal("points", said["units"]?.GetValue<string>());
                    var (at, why) = TapPoint(said);
                    if (at is { } point)
                    {
                        window.MouseDown(point, MouseButton.Left);
                        Settle();
                        window.MouseUp(point, MouseButton.Left);
                        Settle();
                        // A field took the focus: the system's keyboard comes up, as it does under a finger on the phone.
                        if (TopLevel.GetTopLevel(shell)!.FocusManager!.GetFocusedElement() is Visual focused
                            && (focused is TextBox || focused.GetVisualAncestors().Any(v => v is TextBox)) && shell.Keyboard.KeyboardTop is null)
                        {
                            shell.Keyboard.Opened(Height - KeyboardHeight);
                            Settle();
                        }
                    }

                    taps.Add($"{said["tap"]}: {why}");
                    File.WriteAllText(Path.Combine(Scenario.Results, Scenario.TappedFile), new JsonObject { ["ok"] = at is not null, ["detail"] = why }.ToJsonString());
                    File.Delete(hold);
                }

                await Task.Delay(20);
            }

            Assert.True(run.IsCompleted, "the scenario did not finish; taps: " + string.Join("; ", taps));
            var results = await run;
            var failed = results["steps"]!.AsArray()
                .Where(s => !s!["ok"]!.GetValue<bool>())
                .Select(s => $"{s!["step"]} {s["do"]}: {s["detail"]}").ToList();
            Assert.True(failed.Count == 0, string.Join(Environment.NewLine, failed));
            Assert.Equal(10, taps.Count);
        }
        finally
        {
            window.Close();
            phone.AllowCamera = false;
            Phone.Settings.SaveShotSetup(was, wasInches);
        }
    }

    /// <summary>The scenario names only controls the Capture screen has, so a renamed id fails here and not on the simulator.</summary>
    [Fact]
    public void TheScenarioTapsOnlyIdsTheCaptureScreenHas()
    {
        var scenario = JsonNode.Parse(File.ReadAllText(Repo.PathTo("scripts", "scenarios", "phone-touch.json")))!.AsObject();
        var taps = scenario["steps"]!.AsArray().Where(s => s!["do"]!.GetValue<string>() == "hold").Select(s => s!["tap"]!.GetValue<string>()).ToHashSet();
        // Entry 353's four: Take a picture, Choose a photo, Done on the caliber and distance row, Continue on the caliber's question; and
        // the distance field, whose tap brings the keyboard up for the buttons pressed while it is.
        string[] wanted = ["capture-ask-continue", "capture-change", "capture-choose-photo", "capture-distance", "capture-take-picture"];
        Assert.Equal(wanted, taps.Order(StringComparer.Ordinal).ToArray());
        string capture = File.ReadAllText(Repo.PathTo("mobile", "GroupLab.Mobile", "CapturePage.cs"));
        Assert.All(taps, t => Assert.Contains($".Id(\"{t}\")", capture, StringComparison.Ordinal));
    }
}

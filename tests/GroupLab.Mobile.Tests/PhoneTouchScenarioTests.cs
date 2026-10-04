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
    public Task EveryTapDoesItsJobAsAFingerPressesIt() => TapThrough(noCamera: false);

    /// <summary>
    /// The second simulator run (37089660899): the simulator has no camera, so Take a picture opened the photo picker in its place, the
    /// scenario's "back" did not close it, and every tap after it landed in the picker and chose a photograph. Here the same simulator:
    /// the camera's place opens a picker that stays until closed, and a tap while it is up reaches the picker, not GroupLab.
    /// </summary>
    [AvaloniaFact]
    public Task EveryTapDoesItsJobOnASimulatorWithNoCamera() => TapThrough(noCamera: true);

    /// <summary>
    /// The third simulator run (37091489314): close looked for the picker, did not see it, and said "nothing was open", and every later
    /// step failed. Where the look for a system sheet is blind, close now fails there and says why, from the log's own record of the
    /// picker opening with nothing answering or closing it.
    /// </summary>
    [AvaloniaFact]
    public async Task CloseFailsWhereAPickerItCannotSeeIsStillOpen()
    {
        var failed = await TapThrough(noCamera: true, blind: true);
        Assert.Contains(failed, f => f.StartsWith("23 close: the photo picker was opened", StringComparison.Ordinal) && f.Contains("does not see it", StringComparison.Ordinal));
    }

    private static async Task<List<string>> TapThrough(bool noCamera, bool blind = false)
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
        phone.NoCameraOpensPicker = noCamera;
        if (noCamera && !blind)
        {
            Scenario.SystemSheetUp = () => phone.PickerUp;
            Scenario.CloseSystemSheet = phone.ClosePicker;
        }

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
                    if (phone.PickerUp)
                    {
                        // The picker is over the screen: the finger lands in it, and GroupLab never sees the tap.
                        why += ", in the photo picker";
                    }
                    else if (at is { } point)
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
            if (!blind)
            {
                Assert.True(failed.Count == 0, string.Join(Environment.NewLine, failed));
                Assert.Equal(15, taps.Count);
            }

            return failed;
        }
        finally
        {
            window.Close();
            phone.ClosePicker();
            phone.AllowCamera = false;
            phone.NoCameraOpensPicker = false;
            Scenario.SystemSheetUp = null;
            Scenario.CloseSystemSheet = null;
            Phone.Settings.SaveShotSetup(was, wasInches);
        }
    }

    /// <summary>
    /// The first simulator run (37087711129) refused to tap Continue: its hold was worked out just after the keyboard raised the caliber
    /// question, when the places had been laid out again but the question had not yet been drawn there, and a press is judged against what
    /// was drawn. Here the same: looked at at once, Continue's middle reaches the page under it; the hold looks again until Continue stays
    /// put and a press at its middle reaches it, and only then asks for the tap.
    /// </summary>
    [AvaloniaFact]
    public async Task AHoldWaitsUntilTheRaisedQuestionIsDrawn()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        var (was, wasInches) = Phone.Settings.LoadShotSetup();
        Scenario.AnswerFirstRun(Phone.Settings);
        Phone.Settings.SaveShotSetup(null, null);
        var shell = new Shell();
        var window = new Window { Width = 402, Height = Height, Content = shell };
        window.Show();
        void Drawn()
        {
            Settle();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Settle();
        }

        Drawn();
        try
        {
            var capture = shell.GetVisualDescendants().OfType<CapturePage>().Single();
            capture.AskFirst(() => { });
            Drawn();
            capture.GetVisualDescendants().OfType<TextBox>().Single(t => Avalonia.Automation.AutomationProperties.GetAutomationId(t) == "capture-distance").Focus();
            Drawn();
            shell.Keyboard.Opened(Height - KeyboardHeight);

            // At once, before anything is drawn again: what the run's hold saw. The hold's own first look is given exactly that, since
            // here the dispatcher draws before the step can look, where on the simulator the screenshot before it held the thread.
            var behind = Scenario.HoldFor("capture-ask-continue");
            Assert.False(behind["reached"]!.GetValue<bool>());
            int looks = 0;
            Scenario.Look = tap => looks++ == 0 ? behind.DeepClone().AsObject() : Scenario.HoldFor(tap);

            Directory.CreateDirectory(Scenario.Results);
            string hold = Path.Combine(Scenario.Results, Scenario.HoldFile);
            var step = Scenario.Do(new Scenario.Step("hold", new JsonObject { ["do"] = "hold", ["tap"] = "capture-ask-continue", ["name"] = "settling", ["seconds"] = 30.0 }));
            var clock = System.Diagnostics.Stopwatch.StartNew();
            while (!File.Exists(hold) && clock.Elapsed < TimeSpan.FromSeconds(20))
            {
                Drawn();
                await Task.Delay(50);
            }

            Assert.True(File.Exists(hold), $"no hold written: {step.Status} {step.Exception?.InnerException?.Message} {(step.IsCompletedSuccessfully ? step.Result.Item2 : "")}");
            var said = JsonNode.Parse(File.ReadAllText(hold))!.AsObject();
            Assert.True(said["reached"]!.GetValue<bool>(), $"Continue is still covered by {said["under"]} after {said["settledMs"]} ms");
            Assert.NotNull(TapPoint(said).At);
            File.WriteAllText(Path.Combine(Scenario.Results, Scenario.TappedFile), new JsonObject { ["ok"] = true, ["detail"] = "tapped" }.ToJsonString());
            File.Delete(hold);
            var (ok, detail) = await step;
            Assert.True(ok, detail);
        }
        finally
        {
            Scenario.Look = Scenario.HoldFor;
            window.Close();
            Phone.Settings.SaveShotSetup(was, wasInches);
        }
    }

    /// <summary>
    /// The scenario names only controls the screens it taps have, so a renamed id fails here and not on the simulator: the Capture screen's,
    /// and since entry 363 (issue 19) the Targets screen's distance box and the keyboard bar's Next.
    /// </summary>
    [Fact]
    public void TheScenarioTapsOnlyIdsTheScreensHave()
    {
        var scenario = JsonNode.Parse(File.ReadAllText(Repo.PathTo("scripts", "scenarios", "phone-touch.json")))!.AsObject();
        var taps = scenario["steps"]!.AsArray().Where(s => s!["do"]!.GetValue<string>() == "hold").Select(s => s!["tap"]!.GetValue<string>()).ToHashSet();
        // Entry 353's four: Take a picture, Choose a photo, Done on the caliber and distance row, Continue on the caliber's question; and
        // the distance field, whose tap brings the keyboard up for the buttons pressed while it is. Issue 19's two.
        var source = new Dictionary<string, string>
        {
            ["capture-ask-continue"] = "CapturePage.cs", ["capture-change"] = "CapturePage.cs", ["capture-choose-photo"] = "CapturePage.cs",
            ["capture-distance"] = "CapturePage.cs", ["capture-take-picture"] = "CapturePage.cs", ["keyboard-next"] = "KeyboardRoom.cs",
            ["targets-distance"] = "TargetsPage.cs",
        };
        Assert.Equal(source.Keys.Order(StringComparer.Ordinal), taps.Order(StringComparer.Ordinal));
        Assert.All(taps, t => Assert.Contains($".Id(\"{t}\")", File.ReadAllText(Repo.PathTo("mobile", "GroupLab.Mobile", source[t])), StringComparison.Ordinal));
    }
}

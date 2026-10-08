#if GROUPLAB_DEV
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App.Diagnostics;

namespace GroupLab.Mobile.Dev;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 315 section 2: scripted runs without anybody's hands, in GroupLab Dev only. The public application is built
/// without this file (entry 315's amendment, section 1: compiled out, not hidden), and a test holds that none of its types is in it.
///
/// A scenario is a JSON file in the application's files, <c>scenario/scenario.json</c> (Documents on iOS, which a USB tool or the simulator
/// can write; the files folder on Android, over adb), or named by the launch argument <c>--scenario name.json</c> on iOS and the extra
/// <c>org.grouplab.test.scenario</c> on Android. It runs once: the file is renamed when it is read. Its steps are done in order, and what each
/// did, how long it took and the memory held go to <c>scenario/results/results.json</c> with the screenshots and trees it asked for and the
/// log, for a developer's script to copy off:
/// <code>
/// { "name": "read the sample", "steps": [
///   { "do": "open", "place": "capture" },
///   { "do": "picture", "file": "sample.jpg" },
///   { "do": "wait", "screen": "ResultView", "seconds": 180 },
///   { "do": "screenshot", "name": "result" },
///   { "do": "press", "name": "Fix holes" },
///   { "do": "tree", "name": "fix-holes" },
///   { "do": "open", "place": "sessions" } ] }
/// </code>
/// Steps: <c>open</c> a place along the bottom; <c>back</c>, as Android's back does; <c>picture</c>, a file in the scenario folder (or the
/// files folder itself, Documents on iOS) read as a chosen photograph; <c>read</c>, the same and then a wait for the result;
/// <c>wait</c> until a screen (a control's type) or some text is showing; <c>press</c> a button by its automation id, its automation name
/// or its words; <c>type</c> into a field named by its automation id, its automation name or its placeholder; <c>choose</c> an
/// <c>"item"</c> in a list, or turn a check box, switch or choice named <c>"name"</c> on (or off, with <c>"on": false</c>); <c>scroll</c> a
/// named control into view, or the page <c>"by"</c> so many points or <c>"to"</c> its top or bottom; <c>setting</c>, one value in the
/// settings file by its key; <c>reset</c>, the settings and the sessions taken away and every page made again, as a first run;
/// <c>screenshot</c>; <c>tree</c>, the visible controls with their ids, names, words, places and whether each is enabled; <c>sleep</c>;
/// <c>log</c>, the newest lines of the log; <c>replay</c>, a camera clip or a picture played through the capture screen in place of the
/// camera, and <c>record</c>, the camera's last seconds kept as clips (<see cref="CameraReplay"/>); <c>hold</c>, a wait while a script
/// outside taps the control named <c>"tap"</c> with a real touch, and <c>expect</c>, what that tap should have done (entry 353, see
/// <see cref="Hold"/>); <c>close</c>, the system's own sheet over the screen, such as the photo picker, closed as its Cancel would, or
/// else the camera. A step it does not know fails and says so. Before the
/// application starts, the first run's questions are answered unless the scenario has <c>"firstRun": "ask"</c>, and <c>"caliber"</c> with
/// <c>"distanceInches"</c> are saved as the Capture screen's setup. <c>"stopOnFailure": false</c> carries on past a step that failed.
/// </summary>
internal static class Scenario
{
    /// <summary>The scenario folder in the application's own files.</summary>
    internal static string Folder => Path.Combine(files ?? Phone.Platform.FilesFolder, "scenario");

    /// <summary>The application's files, known before the phone starts; the platform's once it has.</summary>
    private static string? files;

    /// <summary>Told the results folder when a run started by <see cref="StartIfPrepared"/> has written its results (entry 318 section 3).</summary>
    internal static event Action<string>? Finished;

    /// <summary>A scenario read before the application started, waiting for the screens.</summary>
    private static string? pending;

    /// <summary>Whether the scenario wants the first run's questions answered, which is all but a scenario saying "firstRun": "ask".</summary>
    private static bool answerFirst;

    /// <summary>Where a run's results, screenshots and log are written; emptied when a run starts.</summary>
    internal static string Results => Path.Combine(Folder, "results");

    /// <summary>The file a scenario waits in when nothing names another.</summary>
    internal const string Waiting = "scenario.json";

    /// <summary>
    /// The scenario to run: the one named (a file name in the scenario folder, nothing with a folder in it), or the waiting
    /// <c>scenario.json</c>; null where there is neither.
    /// </summary>
    internal static string? Requested(string? named)
    {
        if (named is { Length: > 0 })
        {
            if (named.Contains('/') || named.Contains('\\') || named.Contains("..", StringComparison.Ordinal))
            {
                DiagnosticLog.Info("scenario.refused", ("why", "a folder in the name"));
                return null;
            }

            string path = Path.Combine(Folder, named);
            return File.Exists(path) ? path : null;
        }

        string waiting = Path.Combine(Folder, Waiting);
        return File.Exists(waiting) ? waiting : null;
    }

    /// <summary>
    /// Before the application starts (the heads call this first): the scenario asked for, if any, read once, and the first run's questions
    /// answered as the simulator's self-test answers them (nothing sent, mil or MOA as MOA with inches), so they do not stand in front of
    /// the screens, unless the scenario says <c>"firstRun": "ask"</c>. A question already answered is left as it is. True where one waits.
    /// </summary>
    internal static bool Prepare(string filesFolder, string? named)
    {
        files = filesFolder;
        if (Requested(named) is not { } path)
        {
            return false;
        }

        try
        {
            pending = File.ReadAllText(path);
            // Run once: renamed, so the next start does not run it again.
            File.Move(path, path + ".ran", overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "scenario.read", e);
            return false;
        }

        var store = new GroupLab.App.AppSettingsStore(Path.Combine(filesFolder, "settings.json"));
        JsonObject? root = null;
        try
        {
            root = JsonNode.Parse(pending) as JsonObject;
        }
        catch (JsonException)
        {
            // Said in the results when it runs.
        }

        answerFirst = root?["firstRun"]?.GetValueKind() != JsonValueKind.String || root["firstRun"]!.GetValue<string>() != "ask";
        if (answerFirst)
        {
            AnswerFirstRun(store);
        }

        // The caliber and distance a picture is read with, as the Capture screen's row would have them, so nothing asks for them.
        if (root?["caliber"]?.GetValueKind() == JsonValueKind.String)
        {
            double? distance = root["distanceInches"]?.GetValueKind() == JsonValueKind.Number ? root["distanceInches"]!.GetValue<double>() : null;
            store.SaveShotSetup(root["caliber"]!.GetValue<string>(), distance);
            scriptedCalibre = root["caliber"]!.GetValue<string>();
        }

        return true;
    }

    /// <summary>The caliber the scenario names for its whole run, typed for each picture as a person would (entry 376 item B3).</summary>
    private static string? scriptedCalibre;

    /// <summary>The first run's questions answered where they are still open: nothing sent, and the scope in MOA with inches.</summary>
    internal static void AnswerFirstRun(GroupLab.App.AppSettingsStore store)
    {
        if (store.LoadSending().Choice == GroupLab.Core.Publication.SendingChoice.Unset)
        {
            store.SaveSending(GroupLab.Core.Publication.SendingChoice.Never, null);
        }

        if (store.LoadErrorChoice() == ErrorReportChoice.Unset)
        {
            store.SaveErrorChoice(ErrorReportChoice.Never);
        }

        // Entry 357's two follow-up questions, asked of an answer already given, are due now the phones' senders are on (entry 379); a
        // run that finds an earlier "Always" answers them too, or the first run's screen stands where Capture should be.
        if (store.ErrorWordingDue())
        {
            store.SaveErrorChoice(store.LoadErrorChoice(), fullLogWording: true);
        }

        if (store.EverythingQuestionDue())
        {
            store.SaveEverythingAsked();
        }

        if (store.LoadSurveyChoice() == GroupLab.Core.Survey.SurveyChoice.Unset)
        {
            store.SaveSurveyChoice(GroupLab.Core.Survey.SurveyChoice.No);
        }

        if (store.LoadScopeAnswer() == GroupLab.App.ScopeAnswer.Unset)
        {
            store.SaveScopeAnswer(GroupLab.App.ScopeAnswer.Moa, GroupLab.Core.Marking.LinearUnit.Inch);
        }
    }

    /// <summary>Runs the scenario <see cref="Prepare"/> read, once the screens are up.</summary>
    internal static void StartIfPrepared()
    {
        if (pending is not { } text)
        {
            return;
        }

        pending = null;
        files = null;
        _ = Task.Run(async () =>
        {
            // The first screen is built a moment after the application starts.
            await Task.Delay(TimeSpan.FromSeconds(2));

            // Entry 352: on Android the application can have read its settings before the scenario answered the first run's questions on
            // disk, and the first Android emulator sweep (run 37008821160) found every screen covered by "Before you start". The settings
            // the application holds are answered too, and the screens built again.
            if (answerFirst)
            {
                await OnUi(() =>
                {
                    AnswerFirstRun(Phone.Settings);
                    Shell.Current?.Restart();
                    return true;
                });
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            await Run(text);
            Finished?.Invoke(Results);
        });
    }

    /// <summary>One step as written in the file.</summary>
    internal sealed record Step(string Do, JsonObject Fields)
    {
        public string? Text(string name) => Fields[name]?.GetValueKind() == JsonValueKind.String ? Fields[name]!.GetValue<string>() : null;

        public double Number(string name, double otherwise) =>
            Fields[name]?.GetValueKind() == JsonValueKind.Number ? Fields[name]!.GetValue<double>() : otherwise;
    }

    /// <summary>A scenario's name and steps, from its JSON; a sentence where it cannot be read.</summary>
    internal static (string Name, IReadOnlyList<Step> Steps, bool StopOnFailure)? Parse(string json, out string? why)
    {
        why = null;
        try
        {
            if (JsonNode.Parse(json) is not JsonObject root || root["steps"] is not JsonArray steps)
            {
                why = "a scenario is an object with a list of steps";
                return null;
            }

            var parsed = new List<Step>();
            foreach (var node in steps)
            {
                if (node is not JsonObject step || step["do"]?.GetValueKind() != JsonValueKind.String)
                {
                    why = "every step is an object with \"do\"";
                    return null;
                }

                parsed.Add(new Step(step["do"]!.GetValue<string>().ToLowerInvariant(), step));
            }

            string name = root["name"]?.GetValueKind() == JsonValueKind.String ? root["name"]!.GetValue<string>() : "scenario";
            bool stop = root["stopOnFailure"]?.GetValueKind() != JsonValueKind.False;
            return (name, parsed, stop);
        }
        catch (JsonException e)
        {
            why = "not JSON: " + e.Message;
            return null;
        }
    }

    /// <summary>Runs a scenario's steps and writes what each did. Returns the results' JSON.</summary>
    internal static async Task<JsonObject> Run(string json)
    {
        Directory.CreateDirectory(Folder);
        if (Directory.Exists(Results))
        {
            Directory.Delete(Results, recursive: true);
        }

        Directory.CreateDirectory(Results);
        await File.WriteAllTextAsync(Path.Combine(Results, "status"), "running");
        var results = new JsonObject { ["started"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture) };
        var done = new JsonArray();
        results["steps"] = done;
        bool ok = true;
        try
        {
            // Entry 376 item B3: the run's caliber, typed for each picture; read here too, for a scenario run inside a test.
            if ((JsonNode.Parse(json) as JsonObject)?["caliber"] is JsonValue named && named.TryGetValue(out string? given))
            {
                scriptedCalibre = given;
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // Parse below says what is wrong with it.
        }

        if (Parse(json, out string? why) is not { } scenario)
        {
            results["error"] = why;
            ok = false;
        }
        else
        {
            results["name"] = scenario.Name;
            DiagnosticLog.Info("scenario.start", ("steps", scenario.Steps.Count));
            for (int i = 0; i < scenario.Steps.Count; i++)
            {
                var step = scenario.Steps[i];
                var clock = Stopwatch.StartNew();
                string detail;
                bool passed;
                try
                {
                    (passed, detail) = await Do(step);
                }
                catch (Exception e)
                {
                    (passed, detail) = (false, e.GetType().Name + ": " + e.Message);
                }

                done.Add(new JsonObject
                {
                    ["step"] = i + 1,
                    ["do"] = step.Do,
                    ["ok"] = passed,
                    ["ms"] = clock.ElapsedMilliseconds,
                    ["detail"] = detail,
                    ["memoryMb"] = Math.Round(GC.GetTotalMemory(false) / 1048576.0, 1),
                });
                DiagnosticLog.Info("scenario.step", ("n", i + 1), ("do", step.Do), ("ok", passed), ("ms", clock.ElapsedMilliseconds));
                ok &= passed;
                if (!passed && scenario.StopOnFailure)
                {
                    break;
                }
            }
        }

        results["ok"] = ok;
        results["finished"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        DiagnosticLog.Info("scenario.done", ("ok", ok));
        CopyLog(Path.Combine(Results, "log.txt"), lines: null);
        await File.WriteAllTextAsync(Path.Combine(Results, "results.json"), results.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        await File.WriteAllTextAsync(Path.Combine(Results, "status"), "done");
        return results;
    }

    internal static async Task<(bool, string)> Do(Step step)
    {
        switch (step.Do)
        {
            case "open":
                if (!Enum.TryParse(step.Text("place"), ignoreCase: true, out Shell.Place place) || !Enum.IsDefined(place))
                {
                    return (false, "no place named " + (step.Text("place") ?? "nothing") + "; the places are " + string.Join(", ", Enum.GetNames<Shell.Place>()));
                }

                return await OnUi(() =>
                {
                    Shell.Current?.Show(place);
                    return (Shell.Current is not null, place.ToString());
                });
            case "back":
                return await OnUi(() => (Shell.Current?.Back() == true, Shell.Current?.Showing.ToString() ?? ""));
            case "picture":
                return await Picture(step.Text("file"));
            case "pick":
                return Pick(step.Text("file"));
            case "read":
                // The picture read as a chosen one, and then the result, or the question a smaller copy asks, waited for.
                var (given, which) = await Picture(step.Text("file"));
                if (!given)
                {
                    return (false, which);
                }

                return await Wait("ResultView", "A smaller copy", TimeSpan.FromSeconds(step.Number("seconds", 180)));
            case "wait":
                return await Wait(step.Text("screen"), step.Text("text"), TimeSpan.FromSeconds(step.Number("seconds", 60)));
            case "press":
                return await OnUi(() => Press(step.Text("name")));
            case "type":
                return await OnUi(() => Type(step.Text("name"), step.Text("text") ?? ""));
            case "choose":
                return await OnUi(() => Choose(step.Text("name"), step.Text("item"), step.Fields["on"]?.GetValueKind() != JsonValueKind.False));
            case "scroll":
                return await OnUi(() => Scroll(step.Text("name"), step.Text("to"), step.Number("by", 0)));
            case "setting":
                return await OnUi(() => Setting(step.Text("name"), step.Fields["value"]));
            case "reset":
                return await OnUi(() => Reset(step.Text("firstRun") != "ask"));
            case "screenshot":
                return await OnUi(() => Screenshot(Name(step.Text("name"), "screen")));
            case "tree":
                return await OnUi(() => Tree(Name(step.Text("name"), "tree")));
            case "sleep":
                await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(step.Number("seconds", 1), 0, 600)));
                return (true, "");
            case "replay":
                return await CameraReplay.Step(step);
            case "record":
                return await OnUi(() => CameraReplay.RecordStep(step));
            case "hold":
                return await Hold(step.Text("tap"), Name(step.Text("name"), "hold"), TimeSpan.FromSeconds(Math.Clamp(step.Number("seconds", 120), 1, 1800)));
            case "expect":
                return await Expect(step, TimeSpan.FromSeconds(Math.Clamp(step.Number("seconds", 5), 0, 600)));
            case "close":
                return await Close(TimeSpan.FromSeconds(Math.Clamp(step.Number("seconds", 15), 1, 120)));
            case "log":
                int lines = (int)Math.Clamp(step.Number("lines", 200), 1, 100_000);
                string file = Path.Combine(Results, Name(step.Text("name"), "log") + ".txt");
                return (CopyLog(file, lines), Path.GetFileName(file));
            default:
                return (false, $"\"{step.Do}\" is not a step this build knows");
        }
    }

    /// <summary>A file name made safe: letters, digits, dashes and dots only.</summary>
    internal static string Name(string? asked, string otherwise)
    {
        string name = new((asked ?? "").Where(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.').ToArray());
        return name.Trim('.') is { Length: > 0 } safe ? safe : otherwise;
    }

    internal static Task<T> OnUi<T>(Func<T> function) => Dispatcher.UIThread.InvokeAsync(function).GetTask();

    /// <summary>What is on screen now, laid out first so a page just opened is there to be found.</summary>
    private static IEnumerable<Visual> Showing()
    {
        if (Shell.Current is not { } shell)
        {
            return [];
        }

        shell.UpdateLayout();
        var showing = shell.GetVisualDescendants().Where(v => v.IsEffectivelyVisible).ToList();
        // Entry 363, issue 19: the keyboard's bar lives in the overlay above the shell, and its Next is a tap like any other.
        if (shell.Keyboard.Bar is { } bar && bar.GetVisualParent() is not null && bar.IsEffectivelyVisible)
        {
            showing.Add(bar);
            showing.AddRange(bar.GetVisualDescendants().Where(v => v.IsEffectivelyVisible));
        }

        return showing;
    }

    /// <summary>A picture in the scenario folder, or in the files folder itself, read as a photograph chosen on the Capture screen.</summary>
    /// <summary>
    /// Entry 388 section 1: the photo the next picker on any screen returns, instead of opening the platform's own, so a scenario reaches
    /// screens that start from a chosen photo (Add a store-bought target) on the emulator, where no person is there to choose one.
    /// </summary>
    internal static PhotoHandle? NextPick { get; set; }

    private static (bool, string) Pick(string? file)
    {
        if (file is not { Length: > 0 } || Name(file, "") != file || Path.Combine(Folder, file) is not { } path || !File.Exists(path))
        {
            return (false, "pick needs a \"file\" in the scenario folder");
        }

        NextPick = new PhotoHandle(null, null, null, new FileInfo(path).Length, Path.GetExtension(path).ToLowerInvariant(),
            () => Task.FromResult<Stream?>(File.OpenRead(path)));
        return (true, file);
    }

    private static async Task<(bool, string)> Picture(string? file)
    {
        if (file is not { Length: > 0 } || Name(file, "") != file)
        {
            return (false, "a picture is a file name in the scenario folder or the files folder");
        }

        string path = Path.Combine(Folder, file);
        if (!File.Exists(path))
        {
            path = Path.Combine(Phone.Platform.FilesFolder, file);
        }

        if (!File.Exists(path))
        {
            return (false, file + " is not in the scenario folder or the files folder");
        }

        var handle = new PhotoHandle(null, null, null, new FileInfo(path).Length, Path.GetExtension(path).ToLowerInvariant(),
            () => Task.FromResult<Stream?>(File.OpenRead(path)));
        return await OnUi(() =>
        {
            Shell.Current?.Show(Shell.Place.Capture);
            if (CapturePage.SharedPicture is not { } shared)
            {
                return (false, "the Capture screen is not there to take it");
            }

            // Entry 376 item B3: a person types the caliber for every target; a scenario names one for the whole run, typed here for it.
            if (CapturePage.Latest is { TypedCalibre.Length: 0 } capture && scriptedCalibre is { Length: > 0 } calibre)
            {
                capture.TypedCalibre = calibre;
            }

            shared([handle]);
            return (true, file);
        });
    }

    private static async Task<(bool, string)> Wait(string? screen, string? text, TimeSpan most)
    {
        if (screen is null && text is null)
        {
            return (false, "wait for a \"screen\" or some \"text\"");
        }

        // Several may be given, separated by |: the first that shows ends the wait, and is said. An hour at most.
        most = TimeSpan.FromSeconds(Math.Clamp(most.TotalSeconds, 0, 3600));
        string[] screens = screen?.Split('|') ?? [];
        string[] texts = text?.Split('|') ?? [];
        var clock = Stopwatch.StartNew();
        while (clock.Elapsed < most)
        {
            string? found = await OnUi(() => Showing().Select(v => screens.FirstOrDefault(s => v.GetType().Name == s)
                ?? (v is TextBlock { Text: { } words } ? texts.FirstOrDefault(t => words.Contains(t, StringComparison.Ordinal)) : null))
                .FirstOrDefault(f => f is not null));
            if (found is not null)
            {
                return (true, $"{found} after {clock.Elapsed.TotalSeconds:0.0} s");
            }

            await Task.Delay(250);
        }

        return (false, $"not showing after {most.TotalSeconds:0} s");
    }

    /// <summary>The words a control shows: its own text, or the text of the first words inside it.</summary>
    private static string? Words(Visual visual) => visual switch
    {
        TextBlock block => block.Text,
        ContentControl { Content: string words } => words,
        ContentControl { Content: TextBlock block } => block.Text,
        ContentControl { Content: Visual inside } => inside.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault()?.Text,
        TextBox box => box.Text,
        _ => null,
    };

    /// <summary>Whether a control answers to a name given it for scripts: its automation id or its automation name.</summary>
    private static bool Named(Control control, string? name) =>
        name is not null && (AutomationProperties.GetAutomationId(control) == name || AutomationProperties.GetName(control) == name);

    /// <summary>The first of <paramref name="controls"/> named <paramref name="name"/>, or else the first whose words are it.</summary>
    private static T? Find<T>(IEnumerable<T> controls, string name) where T : Control
    {
        var all = controls.ToList();
        return all.FirstOrDefault(c => Named(c, name)) ?? all.FirstOrDefault(c => Words(c) == name);
    }

    internal static (bool, string) Press(string? name)
    {
        if (name is null)
        {
            return (false, "press needs a \"name\"");
        }

        // Several may be given, separated by |: the first found is pressed, and is said.
        var buttons = Showing().OfType<Button>().ToList();
        foreach (string each in name.Split('|'))
        {
            var button = Find(buttons, each);
            if (button is null)
            {
                continue;
            }

            if (!button.IsEffectivelyEnabled)
            {
                return (false, each + " is not enabled");
            }

            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            return (true, each);
        }

        return (false, "no button named " + name);
    }

    private static (bool, string) Type(string? name, string text)
    {
        // A box that suggests as it is typed in (the caliber) takes the words itself; the field inside it is not what is named.
        var showing = Showing().OfType<Control>().ToList();
        if (showing.OfType<AutoCompleteBox>().FirstOrDefault(b => Named(b, name) || b.PlaceholderText == name) is { } suggesting)
        {
            suggesting.Text = text;
            return (true, name ?? "");
        }

        var box = showing.OfType<TextBox>().FirstOrDefault(b => Named(b, name) || b.PlaceholderText == name);
        if (box is null)
        {
            return (false, "no field named " + name);
        }

        box.Text = text;
        return (true, name ?? "");
    }

    /// <summary>An item chosen in a list named <paramref name="name"/>, or a check box, switch or choice so named turned on or off.</summary>
    private static (bool, string) Choose(string? name, string? item, bool on)
    {
        if (name is null)
        {
            return (false, "choose needs a \"name\"");
        }

        var showing = Showing().OfType<Control>().ToList();
        if (showing.OfType<Avalonia.Controls.Primitives.SelectingItemsControl>().FirstOrDefault(l => Named(l, name)) is { } list)
        {
            if (item is null)
            {
                return (false, "choosing in a list needs an \"item\"");
            }

            int index = 0;
            foreach (object? each in list.Items)
            {
                if ((each is Visual visual ? Words(visual) : each?.ToString()) == item)
                {
                    if (!list.IsEffectivelyEnabled)
                    {
                        return (false, name + " is not enabled");
                    }

                    list.SelectedIndex = index;
                    return (true, item);
                }

                index++;
            }

            return (false, $"{name} has no item {item}");
        }

        if (Find(showing.OfType<Avalonia.Controls.Primitives.ToggleButton>(), name) is not { } toggle)
        {
            return (false, "no list, check box or choice named " + name);
        }

        if (!toggle.IsEffectivelyEnabled)
        {
            return (false, name + " is not enabled");
        }

        // A choice among several is chosen, never unchosen: another is chosen instead.
        toggle.IsChecked = toggle is RadioButton || on;
        return (true, $"{name} {(toggle.IsChecked == true ? "on" : "off")}");
    }

    /// <summary>A named control scrolled into view, or the innermost page that scrolls moved by some points or to its top or bottom.</summary>
    private static (bool, string) Scroll(string? name, string? to, double by)
    {
        var showing = Showing().OfType<Control>().ToList();
        if (name is not null)
        {
            if (Find(showing, name) is not { } control)
            {
                return (false, "no control named " + name);
            }

            // Entry 388 section 1: "to": "top" puts the control at the top of what scrolls, so a picture of one card starts at its heading.
            if (to == "top" && control.FindAncestorOfType<ScrollViewer>() is { Content: Visual content } holder
                && control.TranslatePoint(default, content) is { } at)
            {
                double end = Math.Max(0, holder.Extent.Height - holder.Viewport.Height);
                holder.Offset = new Vector(holder.Offset.X, Math.Clamp(at.Y, 0, end));
                return (true, name);
            }

            control.BringIntoView();
            return (true, name);
        }

        var viewer = showing.OfType<ScrollViewer>().LastOrDefault(v => v.Extent.Height > v.Viewport.Height);
        if (viewer is null)
        {
            return (false, "nothing showing scrolls");
        }

        switch (to)
        {
            case "top":
                viewer.ScrollToHome();
                break;
            case "bottom":
                viewer.ScrollToEnd();
                break;
            case null:
                double most = Math.Max(0, viewer.Extent.Height - viewer.Viewport.Height);
                viewer.Offset = new Vector(viewer.Offset.X, Math.Clamp(viewer.Offset.Y + by, 0, most));
                break;
            default:
                return (false, "scroll \"to\" is top or bottom");
        }

        viewer.UpdateLayout();
        return (true, string.Create(CultureInfo.InvariantCulture, $"at {viewer.Offset.Y:0} of {Math.Max(0, viewer.Extent.Height - viewer.Viewport.Height):0}"));
    }

    /// <summary>
    /// One value in the settings file set by its key, as the settings store would write it, or taken away by a null; the place showing is
    /// made again, so a screen opened afterwards reads it.
    /// </summary>
    private static (bool, string) Setting(string? key, JsonNode? value)
    {
        if (key is not { Length: > 0 })
        {
            return (false, "setting needs a \"name\", the key in the settings file");
        }

        string path = Phone.Settings.Path;
        JsonObject file;
        try
        {
            file = File.Exists(path) && JsonNode.Parse(File.ReadAllText(path)) is JsonObject existing ? existing : [];
        }
        catch (JsonException e)
        {
            return (false, "the settings file is not JSON: " + e.Message);
        }

        if (value is null)
        {
            file.Remove(key);
        }
        else
        {
            file[key] = value.DeepClone();
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, file.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        Shell.Units();
        if (Shell.Current is { } shell && shell.Showing != Shell.Place.Capture)
        {
            shell.Show(shell.Showing);
        }

        DiagnosticLog.Info("scenario.setting", ("key", key));
        return (true, key);
    }

    /// <summary>
    /// What a person's use leaves behind, taken away for a run that starts as a first run: the settings and the sessions with their pictures
    /// and own sheets. The built-in sheets, the scenario folder, the bridge's key and a sitting's kept pictures stay.
    /// </summary>
    internal static readonly string[] ResetTakes = ["settings.json", "sessions.db", "sessions.db-wal", "sessions.db-shm", "sessions.db-journal", "records.json", "sessions", "own-sheets"];

    private static (bool, string) Reset(bool answerFirstRun)
    {
        var kept = new List<string>();
        foreach (string name in ResetTakes)
        {
            string path = Path.Combine(Phone.Platform.FilesFolder, name);
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
                else if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                kept.Add(name);
            }
        }

        if (answerFirstRun)
        {
            AnswerFirstRun(Phone.Settings);
        }

        Shell.Current?.Restart();
        DiagnosticLog.Info("scenario.reset", ("kept", kept.Count));
        return kept.Count == 0 ? (true, answerFirstRun ? "a first run, its questions answered" : "a first run") : (false, "could not take away " + string.Join(", ", kept));
    }

    private static (bool, string) Screenshot(string name)
    {
        if (Shell.Current is not { } shell || TopLevel.GetTopLevel(shell) is not { } top)
        {
            return (false, "there is no screen to photograph");
        }

        double scale = top.RenderScaling;
        var size = new PixelSize(Math.Max(1, (int)(top.Bounds.Width * scale)), Math.Max(1, (int)(top.Bounds.Height * scale)));
        using var bitmap = new RenderTargetBitmap(size, new Vector(96 * scale, 96 * scale));
        bitmap.Render(top);
        string file = Path.Combine(Results, name + ".png");
        using (var stream = File.Create(file))
        {
            bitmap.Save(stream, PngBitmapEncoderOptions.Default);
        }

        // Entry 388 section 2: every screenshot carries its layout's faults beside it, so a sweep at any size and theme is a quality sweep.
        File.WriteAllText(Path.Combine(Results, name + ".quality.json"), Quality(top, name).ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        return (true, Path.GetFileName(file));
    }

    /// <summary>The smallest a control a finger presses may be, either way, in device-independent pixels (entry 388 section 2).</summary>
    internal const double SmallestTouch = 44;

    /// <summary>
    /// Entry 388 section 2, the quality sweep: what is wrong with the layout showing, as the screen's own controls measure it. Four faults:
    /// a control a finger presses under <see cref="SmallestTouch"/> either way ("small"); words cut short, by trimming or by a box too
    /// narrow or too short for them ("cut"); anything reaching past the side of the window, where nothing scrolls sideways ("off"); and two
    /// pieces of text drawn over each other ("overlap"). scripts/phone-quality.py gathers them across sizes and themes.
    /// </summary>
    internal static JsonObject Quality(TopLevel top, string name)
    {
        var findings = new JsonArray();
        double wide = top.Bounds.Width, high = top.Bounds.Height;
        Rect Box(Visual v) => v.TransformToVisual(top) is { } to ? new Rect(v.Bounds.Size).TransformToAABB(to) : default;
        bool SideScrolls(Visual v) => v.GetVisualAncestors().OfType<ScrollViewer>().Any(s => s.Extent.Width > s.Viewport.Width + 1);
        void Add(string kind, Control c, Rect box, string? detail = null) => findings.Add(new JsonObject
        {
            ["kind"] = kind,
            ["type"] = c.GetType().Name,
            ["id"] = AutomationProperties.GetAutomationId(c),
            ["text"] = Words(c) is { } w ? (w.Length > 80 ? w[..80] : w) : null,
            ["x"] = Math.Round(box.X),
            ["y"] = Math.Round(box.Y),
            ["width"] = Math.Round(box.Width, 1),
            ["height"] = Math.Round(box.Height, 1),
            ["detail"] = detail,
        });

        var showing = Showing().OfType<Control>().Where(c => c.Bounds.Width > 0 && c.Bounds.Height > 0).ToList();
        var texts = new List<(TextBlock Text, Rect Box)>();
        foreach (var c in showing)
        {
            var box = Box(c);
            bool inView = box.Bottom > 0 && box.Y < high;
            if (c is Button or Avalonia.Controls.Primitives.ToggleButton or TextBox or ComboBox or Slider
                && c.IsEffectivelyEnabled && inView && (box.Width < SmallestTouch - 0.5 || box.Height < SmallestTouch - 0.5)
                && c.GetVisualAncestors().OfType<Button>().FirstOrDefault() is null)
            {
                Add("small", c, box);
            }

            if (c is TextBlock { Text.Length: > 0 } text)
            {
                var layout = text.TextLayout;
                bool trimmed = layout.TextLines.Any(l => l.HasCollapsed);
                bool narrow = layout.WidthIncludingTrailingWhitespace > text.Bounds.Width + 1 && text.TextWrapping == Avalonia.Media.TextWrapping.NoWrap;
                bool short_ = layout.Height > text.Bounds.Height + 1;
                if (inView && (trimmed || narrow || short_))
                {
                    Add("cut", c, box, trimmed ? "trimmed" : narrow ? "wider than its box" : "taller than its box");
                }

                if (inView)
                {
                    texts.Add((text, box));
                }
            }

            if ((c is TextBlock or Button or TextBox or Image) && (box.X < -1 || box.Right > wide + 1) && !SideScrolls(c))
            {
                Add("off", c, box, box.X < -1 ? "past the left side" : "past the right side");
            }
        }

        for (int i = 0; i < texts.Count; i++)
        {
            for (int j = i + 1; j < texts.Count; j++)
            {
                var (a, boxA) = texts[i];
                var (b, boxB) = texts[j];
                var both = boxA.Intersect(boxB);
                if (both.Width > 2 && both.Height > 2 && !a.IsVisualAncestorOf(b) && !b.IsVisualAncestorOf(a))
                {
                    Add("overlap", a, boxA, "over " + (b.Text is { Length: > 40 } t ? t[..40] : b.Text));
                }
            }
        }

        return new JsonObject
        {
            ["screen"] = name,
            ["width"] = Math.Round(wide),
            ["height"] = Math.Round(high),
            ["dark"] = top.ActualThemeVariant == Avalonia.Styling.ThemeVariant.Dark,
            ["findings"] = findings,
        };
    }

    /// <summary>
    /// Entry 353: where the application's view sits on the screen, in the units a tool that taps the screen works in, and whether those
    /// units are the screen's pixels. The Android head sets it, because <c>adb shell input</c> taps in pixels from the screen's corner;
    /// left unset, the view fills the screen from its corner and Avalonia's units are the screen's own, as on iOS, whose taps are in points.
    /// </summary>
    internal static Func<(double X, double Y, bool Pixels)>? ScreenPlace { get; set; }

    /// <summary>
    /// Entry 353: whether the system's own sheet is up over GroupLab Dev, the photo picker above all, which GroupLab does not draw and a
    /// tap cannot be aimed at. The iOS head sets it; null where the system's pickers are activities of their own, as on Android.
    /// </summary>
    internal static Func<bool>? SystemSheetUp { get; set; }

    /// <summary>Closes the system's own sheet as its Cancel would; true where one was up. The iOS head sets it.</summary>
    internal static Func<bool>? CloseSystemSheet { get; set; }

    /// <summary>
    /// Entry 353: whatever the last tap opened over the Capture screen, closed: the system's sheet where one is up, or else the camera,
    /// and then made sure of. The second run on the simulator (37089660899) left the photo picker that Take a picture opens there, having
    /// no camera, over the screen, and every tap after it chose pictures in the picker instead of pressing GroupLab's buttons. The third
    /// (37091489314) closed once, found nothing, and said so, while the picker was still on its way: the camera's place asks for it a
    /// moment after it shows. So this keeps closing whatever appears until nothing has been up for <see cref="Quiet"/>, and fails, saying
    /// so, where a system sheet is still up when <paramref name="most"/> has passed.
    /// </summary>
    private static async Task<(bool, string)> Close(TimeSpan most)
    {
        var closed = new List<string>();
        var clock = Stopwatch.StartNew();
        var quiet = Stopwatch.StartNew();
        while (clock.Elapsed < most)
        {
            string? what = await OnUi(CloseOne);
            if (what is not null)
            {
                closed.Add(what);
                quiet.Restart();
            }
            else if (quiet.Elapsed >= Quiet && !await OnUi(() => SystemSheetUp?.Invoke() == true))
            {
                if (PickerLeftOpen() is { } open)
                {
                    return (false, open);
                }

                return (true, (closed.Count == 0 ? "nothing was open" : "closed " + string.Join(", then ", closed))
                    + $"; nothing up for {Quiet.TotalSeconds:0.#} s");
            }

            await Task.Delay(200);
        }

        bool sheet = await OnUi(() => SystemSheetUp?.Invoke() == true);
        string said = closed.Count == 0 ? "nothing closed" : "closed " + string.Join(", then ", closed);
        return sheet ? (false, $"a system sheet is still up after {most.TotalSeconds:0} s ({said})") : (true, said);
    }

    /// <summary>
    /// A check that does not trust the look for a system sheet: every photo picker opened since the last hold (the log's <c>ios.pick</c>)
    /// must have answered (<c>phone.pick</c>) or been closed by the scenario (<c>scenario.sheet</c>). Where one has done neither, the look
    /// did not see it, and the taps after it would land in it; said here, where it happened, rather than as every later step failing.
    /// </summary>
    private static string? PickerLeftOpen()
    {
        string since = string.Join('\n', (LogLines(null) ?? "").Split('\n').Skip(logMark));
        int opened = System.Text.RegularExpressions.Regex.Count(since, @"\bios\.pick\s");
        int ended = System.Text.RegularExpressions.Regex.Count(since, @"\b(phone\.pick|scenario\.sheet)\s");
        return opened > ended
            ? $"the photo picker was opened {opened} time(s) since the last hold and answered or closed {ended}: it is still over the screen, and the look for a system sheet does not see it"
            : null;
    }

    /// <summary>How long nothing may be open before <c>close</c> is sure the screen is GroupLab's own again.</summary>
    internal static TimeSpan Quiet { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>One thing over the Capture screen closed, and which; null where nothing was.</summary>
    private static string? CloseOne()
    {
        if (CloseSystemSheet?.Invoke() == true)
        {
            return "the system's sheet";
        }

        // The Shell's own back, which closes the camera on Capture and does nothing else there.
        return Shell.Current is { Showing: Shell.Place.Capture } shell && shell.Back() ? "the camera" : null;
    }

    /// <summary>A rectangle in the window's coordinates as it lies on the screen, in the units a tap is given in.</summary>
    internal static Rect OnScreen(TopLevel top, Rect box)
    {
        var (x, y, pixels) = ScreenPlace?.Invoke() ?? (0, 0, false);
        double scale = pixels ? top.RenderScaling : 1;
        return new Rect(x + box.X * scale, y + box.Y * scale, box.Width * scale, box.Height * scale);
    }

    private static JsonObject Place(Rect box) => new()
    {
        ["x"] = Math.Round(box.X, 1),
        ["y"] = Math.Round(box.Y, 1),
        ["width"] = Math.Round(box.Width, 1),
        ["height"] = Math.Round(box.Height, 1),
    };

    /// <summary>
    /// The controls showing, each with its type, automation name, words, place in the window and on the screen (<see cref="ScreenPlace"/>),
    /// and whether it can be used.
    /// </summary>
    internal static JsonArray Elements()
    {
        var all = new JsonArray();
        if (Shell.Current is not { } shell || TopLevel.GetTopLevel(shell) is not { } top)
        {
            return all;
        }

        foreach (var visual in Showing().OfType<Control>())
        {
            string? name = AutomationProperties.GetName(visual);
            string? words = visual is Panel or Border or Avalonia.Controls.Presenters.ContentPresenter ? null : Words(visual);
            if (name is null && words is null && AutomationProperties.GetAutomationId(visual) is null
                && visual is not (Button or TextBox or Avalonia.Controls.Primitives.ToggleButton or Avalonia.Controls.Primitives.SelectingItemsControl))
            {
                continue;
            }

            var box = visual.TransformToVisual(top) is { } to ? new Rect(visual.Bounds.Size).TransformToAABB(to) : default;
            all.Add(new JsonObject
            {
                ["type"] = visual.GetType().Name,
                ["id"] = AutomationProperties.GetAutomationId(visual),
                ["name"] = name,
                ["text"] = words,
                ["x"] = Math.Round(box.X),
                ["y"] = Math.Round(box.Y),
                ["width"] = Math.Round(box.Width),
                ["height"] = Math.Round(box.Height),
                ["screen"] = Place(OnScreen(top, box)),
                ["enabled"] = visual.IsEffectivelyEnabled,
            });
        }

        return all;
    }

    /// <summary>The file a <c>hold</c> step writes for the script that taps, and takes away once the tap is done.</summary>
    internal const string HoldFile = "hold.json";

    /// <summary>What the script that taps writes before it takes the hold away: <c>{"ok": true, "detail": "..."}</c>.</summary>
    internal const string TappedFile = "tapped.json";

    /// <summary>How many lines the log had when the last hold began; <c>expect</c>'s <c>"log"</c> looks only at the lines after it.</summary>
    private static int logMark;

    /// <summary>
    /// Entry 353, Fenix's report of TestFlight build 157: every button did nothing on an iPhone while a field had the focus, and the sweep
    /// passed, because it presses buttons by raising their click and a finger's press never went through the screen's input at all. A
    /// <c>hold</c> stops the scenario and writes <see cref="HoldFile"/> into the results: the control to tap (<c>"tap"</c>, an automation
    /// id), every control showing with its place on the screen (<see cref="Elements"/>), the view's own place, the top of whatever covers
    /// the bottom of the screen while the keyboard is up, and whether a press at the middle of the control would reach it. A script outside
    /// (scripts/touch-test.py) taps there with the platform's own touch, an XCUITest runner on the simulator and adb on Android, writes
    /// <see cref="TappedFile"/>, and takes the hold away; the scenario then goes on, and an <c>expect</c> step says whether the tap did its
    /// job. The hold is kept as <c>name.json</c> beside the results.
    /// </summary>
    private static async Task<(bool, string)> Hold(string? tap, string name, TimeSpan most)
    {
        if (tap is not { Length: > 0 })
        {
            return (false, "hold needs a \"tap\", the automation id of the control to tap");
        }

        string hold = Path.Combine(Results, HoldFile);
        string tapped = Path.Combine(Results, TappedFile);
        File.Delete(tapped);
        var said = await Settled(tap);
        logMark = LogLines(null)?.Split('\n').Length ?? 0;
        string text = said.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(Path.Combine(Results, name + ".json"), text);
        // Written whole and then renamed, so the script never reads half of it.
        await File.WriteAllTextAsync(hold + ".part", text);
        File.Move(hold + ".part", hold, overwrite: true);
        DiagnosticLog.Info("scenario.hold", ("tap", tap));

        var clock = Stopwatch.StartNew();
        while (File.Exists(hold) && clock.Elapsed < most)
        {
            await Task.Delay(100);
        }

        if (File.Exists(hold))
        {
            File.Delete(hold);
            return (false, $"nothing tapped {tap} in {most.TotalSeconds:0} s");
        }

        if (!File.Exists(tapped))
        {
            return (false, $"the script took the hold for {tap} away without saying what it did");
        }

        try
        {
            var answer = JsonNode.Parse(await File.ReadAllTextAsync(tapped)) as JsonObject;
            bool ok = answer?["ok"]?.GetValueKind() == JsonValueKind.True;
            string detail = answer?["detail"]?.GetValueKind() == JsonValueKind.String ? answer["detail"]!.GetValue<string>() : "";
            return (ok, $"{tap}: {detail}");
        }
        catch (JsonException e)
        {
            return (false, $"{tap}: what the script wrote is not JSON: {e.Message}");
        }
        finally
        {
            File.Delete(tapped);
        }
    }

    /// <summary>How a hold looks at the screen: <see cref="HoldFor"/>, which a test replaces to give a first look that is behind.</summary>
    internal static Func<string, JsonObject> Look { get; set; } = HoldFor;

    /// <summary>The longest a hold waits for the control to be drawn where it lies (<see cref="Settled"/>).</summary>
    internal static TimeSpan MostSettling { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Entry 353, the first run on the simulator (run 37087711129): the hold for Continue, worked out 0.4 s after the keyboard raised the
    /// caliber question and while the screenshot before it held the screen's thread, found the page under Continue's middle and refused
    /// to tap. The places come from a layout run there and then, but what a press reaches is found in what was last drawn, and the
    /// question had not yet been drawn in its new place. A person taps what is drawn, and the press is judged against the same drawing,
    /// so a person never meets this; the test did, by looking before the screen had caught up. So a hold looks again a fifth of a second
    /// later until the control has stayed in one place and a press at its middle reaches it, <see cref="MostSettling"/> at most, and says
    /// how long that took. One that never settles is said as before, and is then the application's fault, not the test's.
    /// </summary>
    private static async Task<JsonObject> Settled(string tap)
    {
        var clock = Stopwatch.StartNew();
        var said = await OnUi(() => Look(tap));
        int looks = 1;
        while (clock.Elapsed < MostSettling)
        {
            await Task.Delay(200);
            var again = await OnUi(() => Look(tap));
            looks++;
            bool still = Where(again, tap) is { } now && now == Where(said, tap);
            said = again;
            if (still && said["reached"]?.GetValueKind() == JsonValueKind.True)
            {
                break;
            }
        }

        said["settledMs"] = clock.ElapsedMilliseconds;
        said["looks"] = looks;
        return said;
    }

    /// <summary>The place on the screen a hold gives the control it names, as text to compare; null where it is not showing.</summary>
    private static string? Where(JsonObject said, string tap) =>
        said["controls"]?.AsArray().OfType<JsonObject>().FirstOrDefault(c => c["id"]?.GetValue<string>() == tap)?["screen"]?.ToJsonString();

    /// <summary>What a hold says to the script that taps: see <see cref="Hold"/>.</summary>
    internal static JsonObject HoldFor(string tap)
    {
        var said = new JsonObject { ["tap"] = tap };
        if (Shell.Current is not { } shell || TopLevel.GetTopLevel(shell) is not { } top)
        {
            said["controls"] = new JsonArray();
            return said;
        }

        var controls = Elements();
        var (_, _, pixels) = ScreenPlace?.Invoke() ?? (0, 0, false);
        said["units"] = pixels ? "pixels" : "points";
        said["view"] = Place(OnScreen(top, new Rect(top.Bounds.Size)));
        // The keyboard is the system's, outside what Avalonia can find under a point, so its top is said; the bar on it is Avalonia's own,
        // and covers the page, but not itself: a tap on the bar's Next (issue 19) is measured against the keyboard alone.
        bool onBar = Showing().OfType<Control>().FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == tap) is { } aimed
            && aimed.GetVisualAncestors().OfType<Avalonia.Controls.Primitives.OverlayLayer>().Any();
        double? covered = shell.Keyboard.KeyboardTop is { } keyboardTop ? keyboardTop - (onBar ? 0 : KeyboardRoom.BarHeight) : null;
        said["coveredFrom"] = covered is { } from ? Math.Round(OnScreen(top, new Rect(0, from, 0, 0)).Y, 1) : null;

        // Whether a press at the middle of the control reaches it, or something lies over it there.
        var target = Showing().OfType<Control>().FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == tap);
        if (target?.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), top) is { } middle)
        {
            // A control in the overlay, as the keyboard's bar is, is hit tested there: the window's own test reaches the page beneath it.
            var overlay = target.GetVisualAncestors().OfType<Avalonia.Controls.Primitives.OverlayLayer>().FirstOrDefault();
            var under = overlay is not null && target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), overlay) is { } inLayer
                ? overlay.InputHitTest(inLayer) as Visual
                : top.InputHitTest(middle) as Visual;
            bool reached = under is not null && (under == target || target.IsVisualAncestorOf(under));
            said["reached"] = reached && (covered is null || middle.Y < covered);
            said["under"] = under?.GetType().Name;
        }

        said["controls"] = controls;
        return said;
    }

    /// <summary>
    /// Entry 353: what a tap should have done, looked at until it is so or <paramref name="most"/> has passed. A control <c>"name"</c>d
    /// (automation id, automation name or words) is <c>"showing"</c> or not, and says <c>"text"</c>; the keyboard is up or not
    /// (<c>"keyboard"</c>); the camera, or the line asking for it, is on the Capture screen or not (<c>"camera"</c>; where the device has no
    /// camera, as the iOS Simulator, the photo picker it opens instead counts: the log says <c>camera.none</c> and the system's sheet is
    /// up); a line in the log since
    /// the last hold holds one of <c>"log"</c>'s words, several separated by |; the control <c>"focused"</c> names has the focus
    /// (entry 363, issue 19: where the keyboard bar's Next goes). All that is given must be so at once.
    /// </summary>
    private static async Task<(bool, string)> Expect(Step step, TimeSpan most)
    {
        string? name = step.Text("name");
        string? text = step.Text("text");
        string? log = step.Text("log");
        bool? showing = Flag(step, "showing");
        bool? keyboard = Flag(step, "keyboard");
        bool? camera = Flag(step, "camera");
        string? focused = step.Text("focused");
        if (name is null && keyboard is null && camera is null && log is null && focused is null)
        {
            return (false, "expect needs a \"name\", \"keyboard\", \"camera\", \"focused\" or \"log\"");
        }

        var clock = Stopwatch.StartNew();
        string why;
        while (true)
        {
            string since = string.Join('\n', (LogLines(null) ?? "").Split('\n').Skip(logMark));
            why = await OnUi(() => Unmet(name, text, showing, keyboard, camera, since) ?? NotFocused(focused)) ?? "";
            if (why.Length == 0 && log is not null)
            {
                why = log.Split('|').Any(l => since.Contains(l, StringComparison.Ordinal)) ? "" : $"no line in the log with {log} since the last hold";
            }

            if (why.Length == 0)
            {
                return (true, $"as expected after {clock.Elapsed.TotalSeconds:0.0} s");
            }

            if (clock.Elapsed >= most)
            {
                return (false, why);
            }

            await Task.Delay(200);
        }
    }

    private static bool? Flag(Step step, string name) => step.Fields[name]?.GetValueKind() switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null,
    };

    /// <summary>Why the control named does not have the focus, in words; null where it does, or where nothing was named.</summary>
    private static string? NotFocused(string? focused)
    {
        if (focused is null)
        {
            return null;
        }

        var control = Find(Showing().OfType<Control>().ToList(), focused);
        var holder = Shell.Current is { } shell ? TopLevel.GetTopLevel(shell)?.FocusManager?.GetFocusedElement() as Control : null;
        if (control is null)
        {
            return focused + " is not showing";
        }

        return holder is not null && (ReferenceEquals(holder, control) || holder.GetVisualAncestors().Contains(control)) ? null
            : $"{focused} does not have the focus; {(holder is null ? "nothing" : AutomationProperties.GetAutomationId(holder) ?? holder.GetType().Name)} has it";
    }

    /// <summary>The first of what was expected that is not so, in words; null where all of it is.</summary>
    private static string? Unmet(string? name, string? text, bool? showing, bool? keyboard, bool? camera, string since)
    {
        var controls = Showing().OfType<Control>().ToList();
        if (name is not null)
        {
            var control = Find(controls, name);
            if (showing == false)
            {
                if (control is not null)
                {
                    return name + " is still showing";
                }
            }
            else if (control is null)
            {
                return name + " is not showing";
            }
            else if (text is not null && Words(control) != text)
            {
                return $"{name} says \"{Words(control)}\", not \"{text}\"";
            }
        }

        if (keyboard is { } up && (Shell.Current?.Keyboard.KeyboardTop is not null) != up)
        {
            return up ? "the keyboard is not up" : "the keyboard is still up";
        }

        if (camera is { } wanted)
        {
            bool open = controls.OfType<CapturePage>().Any(p => Phone.Platform.IsCamera(p.Content))
                || controls.OfType<TextBlock>().Any(t => t.Text == CapturePage.CameraWords)
                || (since.Contains("camera.none", StringComparison.Ordinal) && SystemSheetUp?.Invoke() == true);
            if (open != wanted)
            {
                return wanted ? "neither the camera, the line asking for it, nor the picker that stands in for it is showing" : "the camera is still showing";
            }
        }

        return null;
    }

    private static (bool, string) Tree(string name)
    {
        var tree = Elements();
        string file = Path.Combine(Results, name + ".json");
        File.WriteAllText(file, tree.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        return (tree.Count > 0, $"{Path.GetFileName(file)}, {tree.Count} controls");
    }

    /// <summary>The newest lines of this run's log, or all of it, copied beside the results.</summary>
    private static bool CopyLog(string to, int? lines)
    {
        if (LogLines(lines) is not { } text)
        {
            return false;
        }

        File.WriteAllText(to, text);
        return true;
    }

    /// <summary>The newest lines of this run's log, or all of it; null where there is no log.</summary>
    internal static string? LogLines(int? lines)
    {
        DiagnosticLog.Current.Flush();
        if (DiagnosticLog.Current.FilePath is not { } log || !File.Exists(log))
        {
            return null;
        }

        using var read = new StreamReader(new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete));
        var all = read.ReadToEnd().Split('\n');
        return string.Join('\n', lines is { } n ? all.TakeLast(n) : all);
    }
}
#endif

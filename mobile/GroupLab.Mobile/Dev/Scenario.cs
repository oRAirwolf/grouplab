#if GROUPLAB_DEV
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
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
/// camera, and <c>record</c>, the camera's last seconds kept as clips (<see cref="CameraReplay"/>). A step it does not know fails and says so. Before the
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

        if (root?["firstRun"]?.GetValueKind() != JsonValueKind.String || root["firstRun"]!.GetValue<string>() != "ask")
        {
            AnswerFirstRun(store);
        }

        // The caliber and distance a picture is read with, as the Capture screen's row would have them, so nothing asks for them.
        if (root?["caliber"]?.GetValueKind() == JsonValueKind.String)
        {
            double? distance = root["distanceInches"]?.GetValueKind() == JsonValueKind.Number ? root["distanceInches"]!.GetValue<double>() : null;
            store.SaveShotSetup(root["caliber"]!.GetValue<string>(), distance);
        }

        return true;
    }

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
        return shell.GetVisualDescendants().Where(v => v.IsEffectivelyVisible).ToList();
    }

    /// <summary>A picture in the scenario folder, or in the files folder itself, read as a photograph chosen on the Capture screen.</summary>
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

        return (true, Path.GetFileName(file));
    }

    /// <summary>The controls showing, each with its type, automation name, words, place on screen and whether it can be used.</summary>
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
                ["enabled"] = visual.IsEffectivelyEnabled,
            });
        }

        return all;
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

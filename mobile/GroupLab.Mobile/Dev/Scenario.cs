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
/// Steps: <c>open</c> a place along the bottom; <c>picture</c>, a file in the scenario folder read as a chosen photograph; <c>wait</c> until
/// a screen (a control's type) or some text is showing; <c>press</c> a button by its automation name or its words; <c>type</c> into a field
/// named by its automation name or its placeholder; <c>screenshot</c>; <c>tree</c>, the visible controls with their names, words, places and
/// whether each is enabled; <c>sleep</c>; <c>log</c>, the newest lines of the log. A step it does not know fails and says so. Before the
/// application starts, the first run's questions are answered unless the scenario has <c>"firstRun": "ask"</c>, and <c>"caliber"</c> with
/// <c>"distanceInches"</c> are saved as the Capture screen's setup. <c>"stopOnFailure": false</c> carries on past a step that failed.
/// </summary>
internal static class Scenario
{
    /// <summary>The scenario folder in the application's own files.</summary>
    internal static string Folder => Path.Combine(files ?? Phone.Platform.FilesFolder, "scenario");

    /// <summary>The application's files, known before the phone starts; the platform's once it has.</summary>
    private static string? files;

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

    private static async Task<(bool, string)> Do(Step step)
    {
        switch (step.Do)
        {
            case "open":
                if (!Enum.TryParse(step.Text("place"), ignoreCase: true, out Shell.Place place))
                {
                    return (false, "no place named " + (step.Text("place") ?? "nothing") + "; the places are " + string.Join(", ", Enum.GetNames<Shell.Place>()));
                }

                return await OnUi(() =>
                {
                    Shell.Current?.Show(place);
                    return (Shell.Current is not null, place.ToString());
                });
            case "picture":
                return await Picture(step.Text("file"));
            case "wait":
                return await Wait(step.Text("screen"), step.Text("text"), TimeSpan.FromSeconds(step.Number("seconds", 60)));
            case "press":
                return await OnUi(() => Press(step.Text("name")));
            case "type":
                return await OnUi(() => Type(step.Text("name"), step.Text("text") ?? ""));
            case "screenshot":
                return await OnUi(() => Screenshot(Name(step.Text("name"), "screen")));
            case "tree":
                return await OnUi(() => Tree(Name(step.Text("name"), "tree")));
            case "sleep":
                await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(step.Number("seconds", 1), 0, 600)));
                return (true, "");
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

    private static Task<T> OnUi<T>(Func<T> function) => Dispatcher.UIThread.InvokeAsync(function).GetTask();

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

    /// <summary>A picture in the scenario folder, read as a photograph chosen on the Capture screen.</summary>
    private static async Task<(bool, string)> Picture(string? file)
    {
        if (file is not { Length: > 0 } || Name(file, "") != file)
        {
            return (false, "a picture is a file name in the scenario folder");
        }

        string path = Path.Combine(Folder, file);
        if (!File.Exists(path))
        {
            return (false, file + " is not in the scenario folder");
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

        // Several may be given, separated by |: the first that shows ends the wait, and is said.
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

    private static (bool, string) Press(string? name)
    {
        if (name is null)
        {
            return (false, "press needs a \"name\"");
        }

        // Several may be given, separated by |: the first found is pressed, and is said.
        var buttons = Showing().OfType<Button>().ToList();
        foreach (string each in name.Split('|'))
        {
            var button = buttons.FirstOrDefault(b => AutomationProperties.GetName(b) == each) ?? buttons.FirstOrDefault(b => Words(b) == each);
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
        var box = Showing().OfType<TextBox>().FirstOrDefault(b => AutomationProperties.GetName(b) == name || b.PlaceholderText == name);
        if (box is null)
        {
            return (false, "no field named " + name);
        }

        box.Text = text;
        return (true, name ?? "");
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
            if (name is null && words is null && visual is not (Button or TextBox or CheckBox or RadioButton))
            {
                continue;
            }

            var box = visual.TransformToVisual(top) is { } to ? new Rect(visual.Bounds.Size).TransformToAABB(to) : default;
            all.Add(new JsonObject
            {
                ["type"] = visual.GetType().Name,
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
        DiagnosticLog.Current.Flush();
        if (DiagnosticLog.Current.FilePath is not { } log || !File.Exists(log))
        {
            return false;
        }

        using var read = new StreamReader(new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete));
        var all = read.ReadToEnd().Split('\n');
        File.WriteAllText(to, string.Join('\n', lines is { } n ? all.TakeLast(n) : all));
        return true;
    }
}
#endif

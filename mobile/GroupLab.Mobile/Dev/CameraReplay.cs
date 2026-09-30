#if GROUPLAB_DEV
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Imaging;

namespace GroupLab.Mobile.Dev;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 315 section 3, GroupLab Dev only: the replay camera. When a scenario's <c>replay</c> step (or the bridge's)
/// names a clip or a picture, the next capture screen takes its frames and its level's readings from it instead of the camera and the
/// gravity sensor, and everything else is the live camera's own code (<see cref="CameraJudge"/>): the sheet found, the words, the torch on
/// Auto, the level and the shutter's timing. The shutter then takes the frame showing. And while recording is on (the <c>record</c> step,
/// or the switch in Settings), each capture screen keeps its last few seconds as a clip (<see cref="ClipRecorder"/>) in <c>clips/</c>.
/// </summary>
internal static class CameraReplay
{
    /// <summary>The clips, in the application's files: Documents on iOS, the files folder on Android.</summary>
    internal static string ClipsFolder => Path.Combine(Phone.Platform.FilesFolder, "clips");

    /// <summary>Recording on, with its seconds, every how many frames and width, while this file is there.</summary>
    private static string RecordFile => Path.Combine(ClipsFolder, "record");

    private static readonly Lock Gate = new();
    private static JsonArray seen = [];
    private static string? taken;
    private static ClipPlayer? playing;

    /// <summary>The clip or picture the next capture screen replays, as a name in <see cref="ClipsFolder"/>; null for the live camera.</summary>
    internal static string? Asked { get; set; }

    /// <summary>Round again from the start when the clip ends.</summary>
    internal static bool Loop { get; set; }

    /// <summary>Recording each capture screen's last seconds, kept across starts.</summary>
    internal static bool Recording
    {
        get => File.Exists(RecordFile);
        set => SetRecording(value);
    }

    /// <summary>Recording turned on or off; its seconds, every how many frames it keeps and their width saved with it.</summary>
    internal static void SetRecording(bool on, double seconds = ClipRecorder.DefaultSeconds, int every = 1, int width = ClipRecorder.DefaultWidth)
    {
        Directory.CreateDirectory(ClipsFolder);
        if (on)
        {
            File.WriteAllText(RecordFile, string.Create(CultureInfo.InvariantCulture, $"{seconds} {every} {width}"));
        }
        else
        {
            File.Delete(RecordFile);
        }
    }

    /// <summary>What the switch in Settings says under it.</summary>
    internal static string Words() => Recording
        ? "Each time the camera closes, its last few seconds are kept in the clips folder, on this device only, for a developer to replay."
        : "Off. Nothing the camera sees is kept.";

    /// <summary>A recorder for a capture screen opening now, where recording is on and nothing is being replayed; null otherwise.</summary>
    internal static ClipRecorder? NewRecorder(string device)
    {
        if (Asked is not null || !File.Exists(RecordFile))
        {
            return null;
        }

        string[] saved = (File.ReadAllText(RecordFile) is { } text ? text : "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        double seconds = saved.Length > 0 && double.TryParse(saved[0], CultureInfo.InvariantCulture, out double s) ? s : ClipRecorder.DefaultSeconds;
        int every = saved.Length > 1 && int.TryParse(saved[1], CultureInfo.InvariantCulture, out int e) ? e : 1;
        int width = saved.Length > 2 && int.TryParse(saved[2], CultureInfo.InvariantCulture, out int w) ? w : ClipRecorder.DefaultWidth;
        return new ClipRecorder(ClipsFolder, device, seconds, every, width);
    }

    /// <summary>
    /// The player for a capture screen opening now, where a clip was asked for; null for the live camera, or where the clip cannot be read,
    /// which the log says. <paramref name="each"/> is the head's own frame handler, the one its camera's frames go through.
    /// </summary>
    internal static ClipPlayer? NewPlayer(Action<int, GrayImage, ClipFrame, long> each)
    {
        if (Asked is not { } name)
        {
            return null;
        }

        string? why = "not found";
        if (Resolve(name) is not { } path || CameraClip.Open(path, out why) is not { } clip)
        {
            DiagnosticLog.Info("camera.replay", ("clip", name), ("error", why));
            return null;
        }

        var player = new ClipPlayer(clip, each, Loop);
        lock (Gate)
        {
            (seen, taken, playing) = ([], null, player);
        }

        DiagnosticLog.Info("camera.replay", ("clip", name), ("frames", clip.Frames.Count), ("device", clip.Device), ("loop", Loop));
        return player;
    }

    /// <summary>A clip's folder or a picture by its name, in the clips folder, the scenario folder or the files folder; null where none.</summary>
    internal static string? Resolve(string name)
    {
        if (name.Length == 0 || name.Contains('/') || name.Contains('\\') || name.Contains("..", StringComparison.Ordinal))
        {
            return null;
        }

        foreach (string folder in new[] { ClipsFolder, Scenario.Folder, Phone.Platform.FilesFolder })
        {
            string path = Path.Combine(folder, name);
            if (Directory.Exists(path) || File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    /// <summary>The head's report of one replayed frame, for the step's results.</summary>
    internal static void Saw(int index, CameraStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        lock (Gate)
        {
            seen.Add(new JsonObject
            {
                ["frame"] = index,
                ["ms"] = step.NowMs,
                ["says"] = step.Verdict.Say.ToString(),
                ["decided"] = step.Decided?.ToString(),
                ["progress"] = Math.Round(step.Progress, 2),
                ["torch"] = step.Torch?.Level,
                ["fires"] = step.Fire,
                ["judgedMs"] = step.FrameMs,
            });
        }
    }

    /// <summary>The head's report that the shutter took the replayed frame.</summary>
    internal static void Took(string why)
    {
        lock (Gate)
        {
            taken = why;
        }
    }

    /// <summary>
    /// The scenario's <c>replay</c> step: <c>"clip"</c>, a clip's folder or a picture, opened on the camera in Guided (or Manual, with
    /// <c>"manual": true</c>), waited for until the shutter fires or the clip has played through (<c>"seconds"</c>, 120 at most by
    /// default), and every frame's words, own instruction, ring and torch written to <c>replay-&lt;name&gt;.json</c> in the results. No
    /// clip, and the camera is the live one again.
    /// </summary>
    internal static async Task<(bool, string)> Step(Scenario.Step step)
    {
        ArgumentNullException.ThrowIfNull(step);
        if (step.Text("clip") is not { Length: > 0 } name)
        {
            Asked = null;
            return (true, "the live camera");
        }

        if (Resolve(name) is null)
        {
            return (false, name + " is not in the clips folder, the scenario folder or the files folder");
        }

        (Asked, Loop) = (name, step.Fields["loop"]?.GetValueKind() == JsonValueKind.True);
        lock (Gate)
        {
            playing = null;
        }

        bool manual = step.Fields["manual"]?.GetValueKind() == JsonValueKind.True;
        bool opened = await Scenario.OnUi(() =>
        {
            Phone.Settings.SaveCaptureManual(manual);
            Shell.Current?.Show(Shell.Place.Capture);
            return Scenario.Press("capture-take-picture").Item1 || Scenario.Press("capture-show-camera").Item1;
        });
        if (!opened)
        {
            return (false, "the camera could not be opened from the Capture screen");
        }

        var most = TimeSpan.FromSeconds(Math.Clamp(step.Number("seconds", 120), 1, 1800));
        var clock = System.Diagnostics.Stopwatch.StartNew();
        ClipPlayer? player;
        while (true)
        {
            lock (Gate)
            {
                player = playing;
                if (player is not null && (taken is not null || (player.Finished && !Loop)))
                {
                    break;
                }
            }

            if (clock.Elapsed > most)
            {
                break;
            }

            await Task.Delay(100);
        }

        JsonArray frames;
        string? took;
        lock (Gate)
        {
            (frames, took) = ((JsonArray)seen.DeepClone(), taken);
        }

        Directory.CreateDirectory(Scenario.Results);
        string file = Path.Combine(Scenario.Results, "replay-" + Scenario.Name(Path.GetFileNameWithoutExtension(name), "clip") + ".json");
        var result = new JsonObject { ["clip"] = name, ["manual"] = manual, ["taken"] = took, ["frames"] = frames };
        await File.WriteAllTextAsync(file, result.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        if (player is null)
        {
            return (false, "the camera did not start the clip; see camera.replay in the log");
        }

        int takenAt = frames.Select(f => f?["fires"]?.GetValue<bool>() == true).ToList().IndexOf(true);
        string said = string.Join(", ", frames.Select(f => f?["says"]?.GetValue<string>()).Where((s, i) => i == 0 || s != frames[i - 1]?["says"]?.GetValue<string>()));
        return (true, $"{frames.Count} frames; {(took is not null ? $"taken at frame {takenAt}" : "not taken")}; said {said}; {Path.GetFileName(file)}");
    }

    /// <summary>
    /// The scenario's <c>record</c> step: recording on, for <c>"seconds"</c> (6 by default), every <c>"every"</c>th frame, <c>"width"</c>
    /// pixels on the longer side; or off with <c>"on": false</c>.
    /// </summary>
    internal static (bool, string) RecordStep(Scenario.Step step)
    {
        ArgumentNullException.ThrowIfNull(step);
        if (step.Fields["on"]?.GetValueKind() == JsonValueKind.False)
        {
            Recording = false;
            return (true, "not recording");
        }

        SetRecording(true, Math.Clamp(step.Number("seconds", ClipRecorder.DefaultSeconds), 0.5, 30), (int)Math.Clamp(step.Number("every", 1), 1, 100),
            (int)Math.Clamp(step.Number("width", ClipRecorder.DefaultWidth), 320, 4096));
        return (true, "recording into " + Path.GetFileName(ClipsFolder));
    }
}
#endif

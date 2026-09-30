#if GROUPLAB_DEV
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using GroupLab.App.Diagnostics;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Capture;
using GroupLab.Core.Imaging;
using OpenCvSharp;

namespace GroupLab.Mobile.Dev;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 315 section 3, GroupLab Dev only: a camera clip, the frames a capture screen judged with the time each was
/// judged at and what the level and the torch read then, so real framing can be played back through the same guidance on every later build.
/// One format for Android and iOS, so a clip recorded on either phone tests both. A clip is a folder: <c>clip.json</c> and one greyscale JPEG
/// per frame, the luminance the guidance judges, at the stream's own size unless it was recorded smaller.
/// <code>
/// { "format": "grouplab-camera-clip", "version": 1, "device": "android", "recorded": "2026-09-30T10:15:00Z",
///   "picture": { "width": 4080, "height": 3060 }, "stream": { "width": 1920, "height": 1440 },
///   "frames": [ { "file": "0000.jpg", "ms": 0, "gravity": [0.12, -0.30, 9.80], "torch": 0, "torchOf": 5, "light": 171.4 }, ... ] }
/// </code>
/// <c>ms</c> is the frame's time from the first; <c>gravity</c> the level's reading in the screen's axes, as <see cref="BubbleLevel"/>
/// takes it (Android's gravity sensor, or iOS's turned by <see cref="PhoneCamera.LevelFromGravity"/>); <c>torch</c> the torch's level
/// then, of <c>torchOf</c>; <c>light</c> the frame's mean luminance, 0 to 255. <c>picture</c> is the still's size and <c>stream</c> the
/// analysis frame's as the camera gave it, so the guidance judges resolution at the picture's size as the live camera did.
/// A single picture is a clip too: the same picture every <see cref="PictureEveryMs"/>, the level flat.
/// </summary>
internal sealed class CameraClip
{
    public const string Format = "grouplab-camera-clip";

    public const int Version = 1;

    /// <summary>The clip's description in its folder.</summary>
    public const string FileName = "clip.json";

    /// <summary>A picture replayed as a clip: this many frames, one every <see cref="PictureEveryMs"/>, the Fold 7's median pace (entry 311).</summary>
    public const int PictureFrames = 8;

    public const long PictureEveryMs = 900;

    private string? decodedFile;
    private GrayImage? decoded;

    public CameraClip(string folder, string device, string recorded, (int Width, int Height) picture, (int Width, int Height) stream, IReadOnlyList<ClipFrame> frames)
    {
        Folder = folder;
        Device = device;
        Recorded = recorded;
        Picture = picture;
        Stream = stream;
        Frames = frames;
    }

    /// <summary>The folder the frames are in.</summary>
    public string Folder { get; }

    /// <summary>The system it was recorded on: android, ios, or picture for a picture played as a clip.</summary>
    public string Device { get; }

    public string Recorded { get; }

    public (int Width, int Height) Picture { get; }

    public (int Width, int Height) Stream { get; }

    public IReadOnlyList<ClipFrame> Frames { get; }

    /// <summary>
    /// A clip folder (one with <c>clip.json</c>) or a picture, as a clip; null with the reason where it is neither or cannot be read.
    /// </summary>
    public static CameraClip? Open(string path, out string? why)
    {
        why = null;
        try
        {
            if (Directory.Exists(path))
            {
                string file = Path.Combine(path, FileName);
                if (!File.Exists(file))
                {
                    why = "the folder has no " + FileName;
                    return null;
                }

                return Read(path, JsonNode.Parse(File.ReadAllText(file)) as JsonObject, out why);
            }

            if (File.Exists(path))
            {
                return FromPicture(path);
            }

            why = "there is no clip or picture there";
            return null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or InvalidDataException or OpenCVException)
        {
            why = e.GetType().Name;
            return null;
        }
    }

    private static CameraClip? Read(string folder, JsonObject? root, out string? why)
    {
        why = null;
        if (root?["format"]?.GetValueKind() != JsonValueKind.String || root["format"]!.GetValue<string>() != Format)
        {
            why = "it is not a camera clip";
            return null;
        }

        if (Int(root["version"]) is not { } version || version > Version)
        {
            why = "it is a camera clip of a later version";
            return null;
        }

        var frames = new List<ClipFrame>();
        foreach (var node in root["frames"] as JsonArray ?? [])
        {
            if (node is not JsonObject frame || frame["file"]?.GetValueKind() != JsonValueKind.String || Int(frame["ms"]) is not { } ms)
            {
                continue;
            }

            string name = frame["file"]!.GetValue<string>();
            if (Scenario.Name(name, "") != name)
            {
                // A frame is a file in the clip's own folder, and nothing else.
                continue;
            }

            (double, double, double)? gravity = frame["gravity"] is JsonArray { Count: 3 } g && g.All(v => v?.GetValueKind() == JsonValueKind.Number)
                ? (g[0]!.GetValue<double>(), g[1]!.GetValue<double>(), g[2]!.GetValue<double>())
                : null;
            frames.Add(new ClipFrame(name, ms, gravity, Int(frame["torch"]) ?? 0, Int(frame["torchOf"]) ?? 0, Number(frame["light"])));
        }

        if (frames.Count == 0)
        {
            why = "the clip has no frames";
            return null;
        }

        frames.Sort((a, b) => a.Ms.CompareTo(b.Ms));
        return new CameraClip(folder, Text(root["device"]) ?? "", Text(root["recorded"]) ?? "", Size(root["picture"]), Size(root["stream"]), frames);
    }

    /// <summary>A picture played as a clip: the same frame again and again at the Fold 7's pace, the phone flat, the torch off.</summary>
    public static CameraClip FromPicture(string path)
    {
        var size = ImageMetadataReader.Read(File.ReadAllBytes(path));
        var frames = Enumerable.Range(0, PictureFrames)
            .Select(i => new ClipFrame(Path.GetFileName(path), i * PictureEveryMs, (0.0, 0.0, 9.81), 0, 0, null)).ToList();
        (int, int) picture = (size.Width ?? 0, size.Height ?? 0);
        return new CameraClip(Path.GetDirectoryName(Path.GetFullPath(path))!, "picture", "", picture, (PhoneCamera.AnalysisWidth, PhoneCamera.AnalysisHeight), frames);
    }

    /// <summary>
    /// A frame's luminance. A clip's frames are as they were kept; a picture is made the analysis stream's size, as the camera would see it.
    /// The last decoded is kept, since a picture is every frame.
    /// </summary>
    public GrayImage Frame(int index)
    {
        string file = Path.Combine(Folder, Frames[index].File);
        if (decodedFile == file && decoded is not null)
        {
            return decoded;
        }

        double? most = Device == "picture" ? (double)PhoneCamera.AnalysisWidth * PhoneCamera.AnalysisHeight / 1_000_000 : null;
        decoded = ImageLoader.Load(file, most).Image;
        decodedFile = file;
        return decoded;
    }

    /// <summary>The picture's pixels, as it is measured, per pixel of <paramref name="frame"/>, as the live camera judged it.</summary>
    public double MeasuredScale(GrayImage frame) => PhoneCamera.MeasuredScale(Picture.Width, Picture.Height, frame.Width, frame.Height);

    public JsonObject ToJson() => new()
    {
        ["format"] = Format,
        ["version"] = Version,
        ["device"] = Device,
        ["recorded"] = Recorded,
        ["picture"] = new JsonObject { ["width"] = Picture.Width, ["height"] = Picture.Height },
        ["stream"] = new JsonObject { ["width"] = Stream.Width, ["height"] = Stream.Height },
        ["frames"] = new JsonArray(Frames.Select(f => (JsonNode)f.ToJson()).ToArray()),
    };

    /// <summary>The clip's description written in its folder.</summary>
    public void Save() =>
        File.WriteAllText(Path.Combine(Folder, FileName), ToJson().ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

    private static int? Int(JsonNode? node) => node?.GetValueKind() == JsonValueKind.Number ? (int)Math.Round(node.GetValue<double>()) : null;

    private static double? Number(JsonNode? node) => node?.GetValueKind() == JsonValueKind.Number ? node.GetValue<double>() : null;

    private static string? Text(JsonNode? node) => node?.GetValueKind() == JsonValueKind.String ? node.GetValue<string>() : null;

    private static (int, int) Size(JsonNode? node) => (Int(node?["width"]) ?? 0, Int(node?["height"]) ?? 0);
}

/// <summary>One frame of a clip: its file, its time from the first, the level's reading, the torch's level of how many, and its mean luminance.</summary>
internal sealed record ClipFrame(string File, long Ms, (double X, double Y, double Z)? Gravity, int Torch, int TorchOf, double? Light)
{
    public JsonObject ToJson()
    {
        var json = new JsonObject { ["file"] = File, ["ms"] = Ms };
        if (Gravity is var (x, y, z))
        {
            json["gravity"] = new JsonArray(Math.Round(x, 3), Math.Round(y, 3), Math.Round(z, 3));
        }

        json["torch"] = Torch;
        json["torchOf"] = TorchOf;
        if (Light is { } light)
        {
            json["light"] = Math.Round(light, 1);
        }

        return json;
    }
}

/// <summary>
/// Entry 315 section 3, GroupLab Dev only: the camera's frames kept while it runs, for a clip. The newest <see cref="Seconds"/> of every
/// <see cref="Every"/>th analysis frame are held in memory, made <see cref="Width"/> pixels on their longer side and kept as greyscale JPEGs,
/// and written as a clip when the camera closes, the picture taken or not, into <c>clips/</c> in the application's files (Documents on
/// iOS). So a clip holds the few seconds before the shutter, which is the framing worth replaying. Off unless turned on; the clips stay on
/// the device and are never uploaded or put in the diagnostics file.
/// </summary>
internal sealed class ClipRecorder(string folder, string device, double seconds = ClipRecorder.DefaultSeconds, int every = 1, int width = ClipRecorder.DefaultWidth)
{
    public const double DefaultSeconds = 6;

    /// <summary>
    /// The kept frame's longer side: the stream's own, so a replay reads what the camera read. Smaller loses what the guidance reads from:
    /// entry 281 found a sheet's codes at about a pixel a module at 640 by 480, and the committed sample framed across the stream read 29 of
    /// its 34 markers at 1920 pixels and 16 made 1280 wide (CameraReplayTests). A frame is a few hundred KB, a clip a few MB.
    /// </summary>
    public const int DefaultWidth = 1920;

    private readonly Queue<(byte[] Jpeg, ClipFrame Frame)> kept = new();
    private readonly Lock gate = new();
    private long offered;
    private (int, int) stream;
    private (int, int) picture;

    public double Seconds { get; } = Math.Clamp(seconds, 0.5, 30);

    public int Every { get; } = Math.Max(1, every);

    public int Width { get; } = Math.Clamp(width, 320, 4096);

    /// <summary>Frames held now.</summary>
    public int Count
    {
        get
        {
            lock (gate)
            {
                return kept.Count;
            }
        }
    }

    /// <summary>
    /// A frame the camera judged, at <paramref name="nowMs"/> on its clock, with the level's reading in the screen's axes, the torch's level
    /// of <paramref name="torchOf"/>, and the still's size.
    /// </summary>
    public void Offer(GrayImage grey, long nowMs, (double X, double Y, double Z)? gravity, int torch, int torchOf, (int Width, int Height) still)
    {
        ArgumentNullException.ThrowIfNull(grey);
        if (offered++ % Every != 0)
        {
            return;
        }

        using var full = Mat.FromPixelData(grey.Height, grey.Width, MatType.CV_8UC1, grey.Pixels);
        double scale = Math.Min(1, (double)Width / Math.Max(grey.Width, grey.Height));
        using var small = new Mat();
        if (scale < 1)
        {
            Cv2.Resize(full, small, new Size(0, 0), scale, scale, InterpolationFlags.Area);
        }
        else
        {
            full.CopyTo(small);
        }

        byte[] jpeg = small.ImEncode(".jpg", new ImageEncodingParam(ImwriteFlags.JpegQuality, 90));
        double light = Cv2.Mean(small).Val0;
        lock (gate)
        {
            stream = (grey.Width, grey.Height);
            picture = still;
            kept.Enqueue((jpeg, new ClipFrame("", nowMs, gravity, torch, torchOf, light)));
            while (kept.Count > 1 && nowMs - kept.Peek().Frame.Ms > Seconds * 1000)
            {
                kept.Dequeue();
            }
        }
    }

    /// <summary>
    /// The frames held written as a clip in a new folder named for <paramref name="now"/>, and forgotten; the folder, or null where there
    /// were none or it could not be written.
    /// </summary>
    public string? Write(DateTime now)
    {
        (byte[] Jpeg, ClipFrame Frame)[] frames;
        (int, int) streamSize, pictureSize;
        lock (gate)
        {
            frames = [.. kept];
            kept.Clear();
            (streamSize, pictureSize) = (stream, picture);
        }

        if (frames.Length == 0)
        {
            return null;
        }

        try
        {
            string name = now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            string target = Path.Combine(folder, name);
            for (int n = 2; Directory.Exists(target); n++)
            {
                target = Path.Combine(folder, name + "-" + n.ToString(CultureInfo.InvariantCulture));
            }

            Directory.CreateDirectory(target);
            long first = frames[0].Frame.Ms;
            var written = new List<ClipFrame>();
            for (int i = 0; i < frames.Length; i++)
            {
                string file = i.ToString("0000", CultureInfo.InvariantCulture) + ".jpg";
                File.WriteAllBytes(Path.Combine(target, file), frames[i].Jpeg);
                written.Add(frames[i].Frame with { File = file, Ms = frames[i].Frame.Ms - first });
            }

            var clip = new CameraClip(target, device, now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture), pictureSize, streamSize, written);
            clip.Save();
            DiagnosticLog.Info("camera.clip", ("frames", written.Count), ("ms", written[^1].Ms));
            return target;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            DiagnosticLog.Exception(GroupLab.App.Diagnostics.LogLevel.Warn, "camera.clip", e);
            return null;
        }
    }
}

/// <summary>
/// Entry 315 section 3, GroupLab Dev only: a clip played to a capture screen in place of its camera. Each frame is handed on no sooner than
/// its recorded time from the start, and with that time, so the waits the guidance and the shutter measure are the recording's however
/// long this device takes to judge a frame; a frame is never skipped, so every run judges the same frames. Once through, or round again
/// where it loops.
/// </summary>
/// <param name="clip">The clip.</param>
/// <param name="each">A frame: its index, its luminance, the frame, and its time in milliseconds.</param>
/// <param name="loop">Play it round again from the start once it ends.</param>
internal sealed class ClipPlayer(CameraClip clip, Action<int, GrayImage, ClipFrame, long> each, bool loop = false)
{
    private volatile bool stopped;
    private Thread? thread;

    /// <summary>Played through, or stopped.</summary>
    public bool Finished { get; private set; }

    /// <summary>The index of the frame handed on last, -1 before the first.</summary>
    public int Current { get; private set; } = -1;

    /// <summary>The file of the frame handed on last: what the shutter takes while a clip plays.</summary>
    public string? CurrentFile => Current >= 0 ? Path.Combine(clip.Folder, clip.Frames[Current].File) : null;

    public CameraClip Clip => clip;

    /// <summary>Plays the clip on a thread of its own.</summary>
    public void Start()
    {
        stopped = false;
        thread = new Thread(() => Play(paced: true)) { IsBackground = true, Name = "camera replay" };
        thread.Start();
    }

    public void Stop() => stopped = true;

    /// <summary>Every frame at once, once through, on this thread: what a test runs.</summary>
    public void PlayNow() => Play(paced: false);

    private void Play(bool paced)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        long first = clip.Frames[0].Ms, length = clip.Frames[^1].Ms - first;
        long gap = clip.Frames.Count > 1 ? Math.Max(1, length / (clip.Frames.Count - 1)) : CameraClip.PictureEveryMs;
        long round = 0;
        try
        {
            do
            {
                for (int i = 0; i < clip.Frames.Count && !stopped; i++)
                {
                    var frame = clip.Frames[i];
                    long at = round + frame.Ms - first;
                    while (paced && !stopped && clock.ElapsedMilliseconds < at)
                    {
                        Thread.Sleep((int)Math.Clamp(at - clock.ElapsedMilliseconds, 1, 50));
                    }

                    if (stopped)
                    {
                        break;
                    }

                    var grey = clip.Frame(i);
                    Current = i;
                    each(i, grey, frame, at);
                }

                round += length + gap;
            }
            while (loop && !stopped);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException or OpenCVException)
        {
            DiagnosticLog.Exception(GroupLab.App.Diagnostics.LogLevel.Warn, "camera.replay", e);
        }
        finally
        {
            Finished = true;
        }
    }
}
#endif

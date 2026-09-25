using System.Diagnostics;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Registration;
using GroupLab.Core.Survey;
using GroupLab.Core.Trace;
using OpenCvSharp;
using LogLevel = GroupLab.App.Diagnostics.LogLevel;

namespace GroupLab.Android;

/// <summary>What one photograph came to: the marking, the sheet it was analyzed as, and why it stopped, where it did.</summary>
internal sealed record PhoneResult(MarkingState State, TargetDefinition? Definition, string? Failure, long? SessionId);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 219 item A4: a photograph, from the camera or picked, analyzed the way the desktop analyzes one, at the
/// phone's working size (item A1). The working copy is the session's own image, so every coordinate in the marking is in its pixels and a
/// session opens anywhere without knowing a scale; the full photograph is not kept. The session is saved in the desktop's own format and
/// database, by the desktop's own code.
/// </summary>
internal static class PhoneAnalysis
{
    private static IReadOnlyList<TargetDefinition>? library;

    private static string Files => global::Android.App.Application.Context.FilesDir!.AbsolutePath;

    /// <summary>Where each session's working image lives, one folder a session.</summary>
    internal static string SessionsFolder => Path.Combine(Files, "sessions");

    /// <summary>The session records, the desktop's database.</summary>
    internal static SessionStore Store() => SessionStore.Open(Path.Combine(Files, "sessions.db"));

    /// <summary>The built-in sheets, copied once out of the application's assets, where a sheet's codes are matched.</summary>
    internal static IReadOnlyList<TargetDefinition> Library()
    {
        if (library is { } known)
        {
            return known;
        }

        var context = global::Android.App.Application.Context;
        string folder = Path.Combine(Files, "targets");
        Directory.CreateDirectory(folder);
        foreach (string name in (context.Assets!.List("targets") ?? []).Where(n => n.EndsWith(".gltd.json", StringComparison.Ordinal)))
        {
            string to = Path.Combine(folder, name);
            using var from = context.Assets.Open($"targets/{name}");
            using var file = File.Create(to);
            from.CopyTo(file);
        }

        return library = SheetIdentification.Candidates([folder]);
    }

    public static PhoneResult Run(string photo, Calibre? calibre, UnitSettings units, SurveyQueue? survey, CancellationToken token)
    {
        var clock = Stopwatch.StartNew();
        byte[] bytes = File.ReadAllBytes(photo);
        var original = ImageMetadataReader.Read(bytes);
        string folder = Path.Combine(SessionsFolder, DateTime.Now.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture));
        Directory.CreateDirectory(folder);
        string image = Path.Combine(folder, "target.jpg");
        int width, height;
        double scale;
        using (var colour = Cv2.ImDecode(bytes, ImreadModes.Color | ImreadModes.IgnoreOrientation))
        {
            if (colour.Empty())
            {
                return new PhoneResult(MarkingState.Empty, null, "The picture could not be read as an image.", null);
            }

            scale = WorkingSize.Scale(colour.Width, colour.Height, WorkingSize.PhoneMegapixels);
            using var working = new Mat();
            Cv2.Resize(colour, working, new OpenCvSharp.Size(0, 0), scale, scale, scale < 1 ? InterpolationFlags.Area : InterpolationFlags.Linear);
            Cv2.ImWrite(image, working, new ImageEncodingParam(ImwriteFlags.JpegQuality, 92));
            (width, height) = (working.Width, working.Height);
        }

        var metadata = WorkingSize.Scaled(original, width, height, scale);
        var (grey, _) = ImageLoader.Load(image);
        var (value, _) = ImageLoader.LoadMaxChannel(image);
        var backend = new OpenCvSharpBackend();
        var trace = new TraceRecorder();
        var session = new MarkingSession();
        session.Open(image, original.Orientation);
        if (calibre is not null)
        {
            session.SetCalibre(calibre);
        }

        var identity = SheetIdentification.Identify(grey, Library(), backend, trace, token);
        if (identity.Definition is not { } definition)
        {
            DiagnosticLog.Info("phone.detect", ("named", false), ("ms", clock.ElapsedMilliseconds));
            return new PhoneResult(session.State, null,
                "GroupLab could not read the square codes that name the sheet. Take the picture again with the whole sheet in view, square on, in even light.", null);
        }

        var result = AutomaticMarking.Run(grey, value, metadata, definition, backend, trace, token, calibre);
        survey?.Record(new AnalysisFacts(original.Width ?? grey.Width, original.Height ?? grey.Height, grey.Width, grey.Height, Benchmark.Stages(trace), Benchmark.PeakMegabytes()));
        DiagnosticLog.Info("phone.detect", ("named", true), ("holes", result.Detections.Count), ("failure", result.Failure), ("ms", clock.ElapsedMilliseconds));
        if (result.Failure is not null || result.Scale is null)
        {
            return new PhoneResult(session.State, definition, (result.Failure ?? "The sheet's markers could not be matched").TrimEnd('.') + ".", null);
        }

        session.LoadDetections(result.Scale, result.Bulls, result.Detections, result.Assignment, result.Rejected ?? [], result.Summary, result.Detection, result.Capture);
        long? id = Save(session.State, definition, units, null);
        return new PhoneResult(session.State, definition, null, id);
    }

    /// <summary>Saves the session, or updates it where it was saved before; null where the database would not take it.</summary>
    public static long? Save(MarkingState state, TargetDefinition? definition, UnitSettings units, long? existingId)
    {
        try
        {
            var store = Store();
            var existing = existingId is { } id ? store.Get(id) : null;
            string? sha = state.ImagePath is { } path && File.Exists(path)
                ? Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path)))
                : null;
            var record = SessionRecords.Build(state, definition, units, analyseSighters: false, existing, sha, null, null, DateTime.UtcNow, DateTime.Now);
            long saved = store.Save(record);
            DiagnosticLog.Info("session.save", ("session", saved), ("shots", record.ShotCount));
            return saved;
        }
        catch (Microsoft.Data.Sqlite.SqliteException e)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "session.save", e);
            return null;
        }
    }
}

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

/// <summary>The working copy of a photograph: the session's own image, its metadata at that size, and the photograph's own size.</summary>
internal sealed record WorkingImage(string Path, ImageMetadata Metadata, int OriginalWidth, int OriginalHeight);

/// <summary>What one photograph came to: the marking, the sheet it was analyzed as, and why it stopped, where it did.</summary>
internal sealed record PhoneResult(MarkingState State, TargetDefinition? Definition, string? Failure, long? SessionId, WorkingImage? Image = null, bool AskWhichSheet = false);

/// <summary>What the person said about the shooting: the caliber, which changes what GroupLab finds, and the distance.</summary>
internal sealed record ShotSetup(Calibre? Calibre, double? DistanceInches);

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
    internal static string SessionsFolder => System.IO.Path.Combine(Files, "sessions");

    /// <summary>The session records, the desktop's database.</summary>
    internal static SessionStore Store() => SessionStore.Open(System.IO.Path.Combine(Files, "sessions.db"));

    /// <summary>The built-in sheets, copied once out of the application's assets, where a sheet's codes are matched.</summary>
    internal static IReadOnlyList<TargetDefinition> Library()
    {
        if (library is { } known)
        {
            return known;
        }

        var context = global::Android.App.Application.Context;
        string folder = System.IO.Path.Combine(Files, "targets");
        Directory.CreateDirectory(folder);
        foreach (string name in (context.Assets!.List("targets") ?? []).Where(n => n.EndsWith(".gltd.json", StringComparison.Ordinal)))
        {
            string to = System.IO.Path.Combine(folder, name);
            using var from = context.Assets.Open($"targets/{name}");
            using var file = File.Create(to);
            from.CopyTo(file);
        }

        return library = SheetIdentification.Candidates([folder]);
    }

    /// <summary>The photograph reduced to the working size and written as the session's own image; null where it is not an image.</summary>
    public static WorkingImage? Prepare(string photo)
    {
        byte[] bytes = File.ReadAllBytes(photo);
        var original = ImageMetadataReader.Read(bytes);
        using var colour = Cv2.ImDecode(bytes, ImreadModes.Color | ImreadModes.IgnoreOrientation);
        if (colour.Empty())
        {
            return null;
        }

        string folder = System.IO.Path.Combine(SessionsFolder, DateTime.Now.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture));
        Directory.CreateDirectory(folder);
        string image = System.IO.Path.Combine(folder, "target.jpg");
        double scale = WorkingSize.Scale(colour.Width, colour.Height, WorkingSize.PhoneMegapixels);
        using var working = new Mat();
        Cv2.Resize(colour, working, new OpenCvSharp.Size(0, 0), scale, scale, scale < 1 ? InterpolationFlags.Area : InterpolationFlags.Linear);
        Cv2.ImWrite(image, working, new ImageEncodingParam(ImwriteFlags.JpegQuality, 92));
        return new WorkingImage(image, WorkingSize.Scaled(original, working.Width, working.Height, scale), colour.Width, colour.Height);
    }

    /// <summary>A photograph, prepared and analyzed.</summary>
    public static PhoneResult Run(string photo, ShotSetup setup, UnitSettings units, SurveyQueue? survey, CancellationToken token) =>
        Prepare(photo) is { } working
            ? Detect(working, null, setup, units, survey, token)
            : new PhoneResult(MarkingState.Empty, null, "The picture could not be read as an image.", null);

    /// <summary>
    /// The working image analyzed: as the sheet its codes name, or as <paramref name="chosen"/> where the person named it because the codes
    /// could not be read (entry 115 section 4, as the desktop asks).
    /// </summary>
    public static PhoneResult Detect(WorkingImage working, TargetDefinition? chosen, ShotSetup setup, UnitSettings units, SurveyQueue? survey, CancellationToken token)
    {
        var clock = Stopwatch.StartNew();
        var (grey, _) = ImageLoader.Load(working.Path);
        var (value, _) = ImageLoader.LoadMaxChannel(working.Path);
        var backend = new OpenCvSharpBackend();
        var trace = new TraceRecorder();
        var session = new MarkingSession();
        session.Open(working.Path, working.Metadata.Orientation);
        session.SetCalibre(setup.Calibre);
        session.SetShotDistance(setup.DistanceInches);

        var definition = chosen ?? SheetIdentification.Identify(grey, Library(), backend, trace, token).Definition;
        if (definition is null)
        {
            DiagnosticLog.Info("phone.detect", ("named", false), ("ms", clock.ElapsedMilliseconds));
            return new PhoneResult(session.State, null,
                "GroupLab could not read the square codes that name the sheet. Choose which sheet it is, or take the picture again with the whole sheet in view, square on, in even light.",
                null, working, AskWhichSheet: true);
        }

        var result = AutomaticMarking.Run(grey, value, working.Metadata, definition, backend, trace, token, setup.Calibre);
        survey?.Record(new AnalysisFacts(working.OriginalWidth, working.OriginalHeight, grey.Width, grey.Height, Benchmark.Stages(trace), Benchmark.PeakMegabytes()));
        DiagnosticLog.Info("phone.detect", ("named", chosen is null), ("holes", result.Detections.Count), ("failure", result.Failure), ("ms", clock.ElapsedMilliseconds));
        if (result.Failure is not null || result.Scale is null)
        {
            return new PhoneResult(session.State, definition, (result.Failure ?? "The sheet's markers could not be matched").TrimEnd('.') + ".", null, working);
        }

        session.LoadDetections(result.Scale, result.Bulls, result.Detections, result.Assignment, result.Rejected ?? [], result.Summary, result.Detection, result.Capture);
        long? id = Save(session.State, definition, units, null);
        return new PhoneResult(session.State, definition, null, id, working);
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

    /// <summary>A working image nobody kept: a picture abandoned after it could not be read.</summary>
    public static void Discard(WorkingImage? working)
    {
        if (working is not null && System.IO.Path.GetDirectoryName(working.Path) is { } folder && folder.StartsWith(SessionsFolder, StringComparison.Ordinal))
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}

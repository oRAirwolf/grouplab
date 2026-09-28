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
internal sealed record PhoneResult(MarkingState State, TargetDefinition? Definition, string? Failure, long? SessionId, WorkingImage? Image = null, bool AskWhichSheet = false,
    GroupLab.Core.Capture.PictureVerdict? Check = null);

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

    internal static string Files => global::Android.App.Application.Context.FilesDir!.AbsolutePath;

    /// <summary>Where each session's working image lives, one folder a session.</summary>
    internal static string SessionsFolder => System.IO.Path.Combine(Files, "sessions");

    /// <summary>The session records, the desktop's database.</summary>
    internal static SessionStore Store() => SessionStore.Open(System.IO.Path.Combine(Files, "sessions.db"));

    /// <summary>Entry 227 section 2: the benchmark's work on the phone, the same sheet and detector as the desktop's.</summary>
    internal static (TargetDefinition? Definition, GroupLab.Core.Imaging.IImagingBackend Backend) BenchmarkWork()
    {
        Library();
        string file = System.IO.Path.Combine(Files, "targets", GroupLab.Core.Survey.Benchmark.SheetFile);
        return (File.Exists(file) ? GroupLab.Core.Gltd.Json.GltdJsonReader.ReadFile(file).Definition : null, new GroupLab.Cli.Imaging.OpenCvSharpBackend());
    }

    /// <summary>The built-in sheets, copied once out of the application's assets, where a sheet's codes are matched.</summary>
    internal static IReadOnlyList<TargetDefinition> Library()
    {
        if (library is { } known)
        {
            return known;
        }

        var context = global::Android.App.Application.Context;
        string folder = System.IO.Path.Combine(Files, "targets");

        // The frozen definitions too (entry 226 section 1): a sheet printed before the zeroing grids were redrawn is still identified.
        foreach (string assets in (string[])["targets", "targets/frozen"])
        {
            string into = System.IO.Path.Combine(Files, assets);
            Directory.CreateDirectory(into);
            foreach (string name in (context.Assets!.List(assets) ?? []).Where(n => n.EndsWith(".gltd.json", StringComparison.Ordinal)))
            {
                using var from = context.Assets.Open($"{assets}/{name}");
                using var file = File.Create(System.IO.Path.Combine(into, name));
                from.CopyTo(file);
            }
        }

        return library = SheetIdentification.Candidates([folder]);
    }

    /// <summary>The photograph reduced to the working size and written as the session's own image; null where it is not an image.</summary>
    /// <remarks>
    /// Entry 239: the picture is decoded by Android at a power of two fraction of its size (<see cref="WorkingSize.SampleFor"/>), so a 600 dpi
    /// scan of 32 megapixels is 8 from the start and never exists in memory at full size; only what is left over is then resized, to 8
    /// megapixels at most. Like the OpenCV decode it replaces, it leaves the orientation flag to the marking, which reads it.
    /// </remarks>
    public static WorkingImage? Prepare(string photo)
    {
        var original = ImageMetadataReader.Read(File.ReadAllBytes(photo));
        var bounds = new global::Android.Graphics.BitmapFactory.Options { InJustDecodeBounds = true };
        global::Android.Graphics.BitmapFactory.DecodeFile(photo, bounds);
        if (bounds.OutWidth <= 0 || bounds.OutHeight <= 0)
        {
            return null;
        }

        double most = MemoryBudget.PhoneWorkingMegapixels(Budget());
        int sample = WorkingSize.SampleFor(bounds.OutWidth, bounds.OutHeight, most);
        using var colour = new Mat();
        using (var bitmap = global::Android.Graphics.BitmapFactory.DecodeFile(photo, new global::Android.Graphics.BitmapFactory.Options { InSampleSize = sample, InPreferredConfig = global::Android.Graphics.Bitmap.Config.Argb8888, InScaled = false }))
        {
            if (bitmap is null)
            {
                return null;
            }

            IntPtr pixels = bitmap.LockPixels();
            try
            {
                using var rgba = Mat.FromPixelData(bitmap.Height, bitmap.Width, MatType.CV_8UC4, pixels, bitmap.RowBytes);
                Cv2.CvtColor(rgba, colour, ColorConversionCodes.RGBA2BGR);
            }
            finally
            {
                bitmap.UnlockPixels();
                bitmap.Recycle();
            }
        }

        string folder = System.IO.Path.Combine(SessionsFolder, DateTime.Now.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture));
        Directory.CreateDirectory(folder);
        string image = System.IO.Path.Combine(folder, "target.jpg");
        double rest = WorkingSize.Scale(colour.Width, colour.Height, most);
        using var working = new Mat();
        Cv2.Resize(colour, working, new OpenCvSharp.Size(0, 0), rest, rest, rest < 1 ? InterpolationFlags.Area : InterpolationFlags.Linear);
        Cv2.ImWrite(image, working, new ImageEncodingParam(ImwriteFlags.JpegQuality, 92));
        double scale = (double)working.Width / bounds.OutWidth;
        DiagnosticLog.Info("phone.prepare", ("width", bounds.OutWidth), ("height", bounds.OutHeight), ("sample", sample), ("working", $"{working.Width}x{working.Height}"),
            ("peakMb", Benchmark.PeakMegabytes()));
        return new WorkingImage(image, WorkingSize.Scaled(original, working.Width, working.Height, scale), bounds.OutWidth, bounds.OutHeight);
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 240: the memory this analysis may use, from what the device says now: its total and available memory and
    /// its low memory threshold (<c>ActivityManager.getMemoryInfo</c>), the floor when it says memory is low. Logged with the device's memory
    /// classes, so a report says what the phone allowed.
    /// </summary>
    public static double Budget()
    {
        var activity = (global::Android.App.ActivityManager?)global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.ActivityService);
        if (activity is null)
        {
            return MemoryBudget.FloorMegabytes;
        }

        var info = new global::Android.App.ActivityManager.MemoryInfo();
        activity.GetMemoryInfo(info);
        const double Mb = 1024.0 * 1024;
        double budget = MemoryBudget.Phone(info.TotalMem / Mb, info.AvailMem / Mb, info.Threshold / Mb, info.LowMemory);
        DiagnosticLog.Info("memory.budget", ("totalMb", (long)(info.TotalMem / Mb)), ("availableMb", (long)(info.AvailMem / Mb)), ("low", info.LowMemory),
            ("class", activity.MemoryClass), ("largeClass", activity.LargeMemoryClass), ("budgetMb", (long)budget));
        return budget;
    }

    /// <summary>
    /// A photograph, prepared and analyzed. Entry 243 section 3.2: <paramref name="progress"/> hears what it is doing, a step at a time, and a
    /// cancel leaves nothing behind: the working copy made for it is deleted, and nothing was saved yet.
    /// </summary>
    public static PhoneResult Run(string photo, ShotSetup setup, UnitSettings units, SurveyQueue? survey, CancellationToken token, Action<string>? progress = null, bool torch = false)
    {
        progress?.Invoke(GroupLab.Core.Trace.StageWords.Starting);
        if (Prepare(photo) is not { } working)
        {
            return new PhoneResult(MarkingState.Empty, null, "The picture could not be read as an image.", null);
        }

        try
        {
            token.ThrowIfCancellationRequested();
            return Detect(working, null, setup, units, survey, token, progress, torch);
        }
        catch (OperationCanceledException)
        {
            Forget(working);
            throw;
        }
    }

    /// <summary>A working copy nothing will use, deleted with the folder made for it.</summary>
    public static void Forget(WorkingImage working)
    {
        try
        {
            if (System.IO.Path.GetDirectoryName(working.Path) is { } folder && folder.StartsWith(SessionsFolder, StringComparison.Ordinal))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
        catch (IOException e)
        {
            DiagnosticLog.Info("phone.forget", ("error", e.GetType().Name));
        }
    }

    /// <summary>
    /// The working image analyzed: as the sheet its codes name, or as <paramref name="chosen"/> where the person named it because the codes
    /// could not be read (entry 115 section 4, as the desktop asks).
    /// </summary>
    public static PhoneResult Detect(WorkingImage working, TargetDefinition? chosen, ShotSetup setup, UnitSettings units, SurveyQueue? survey, CancellationToken token, Action<string>? progress = null, bool torch = false)
    {
        var clock = Stopwatch.StartNew();
        var (grey, _) = ImageLoader.Load(working.Path);
        var (value, _) = ImageLoader.LoadMaxChannel(working.Path);
        var backend = new OpenCvSharpBackend();
        var trace = new TraceRecorder();
        trace.Filed += record =>
        {
            if (GroupLab.Core.Trace.StageWords.After(record.Stage) is { } next)
            {
                progress?.Invoke(next);
            }
        };
        var session = new MarkingSession();
        session.Open(working.Path, working.Metadata.Orientation);
        session.SetCalibre(setup.Calibre);
        session.SetShotDistance(setup.DistanceInches);

        var identity = chosen is null ? SheetIdentification.Identify(grey, Library(), backend, trace, token) : null;
        var definition = chosen ?? identity!.Definition;
        int codesRead = identity?.CodesRead ?? 0;
        if (definition is null)
        {
            // Entry 260: every picture is checked, a picture that names no sheet included.
            var unread = GroupLab.Core.Capture.PictureCheck.Of(grey, null, null, codesRead, torch);
            DiagnosticLog.Info("phone.detect", ("named", false), ("ms", clock.ElapsedMilliseconds), ("check", unread.Describe()));
            return new PhoneResult(session.State, null,
                "GroupLab could not read the square codes that name the sheet. Choose which sheet it is, or take the picture again with the whole sheet in view, square on, in even light.",
                null, working, AskWhichSheet: true, Check: unread);
        }

        var result = AutomaticMarking.Run(grey, value, working.Metadata, definition, backend, trace, token, setup.Calibre);
        survey?.Record(new AnalysisFacts(working.OriginalWidth, working.OriginalHeight, grey.Width, grey.Height, Benchmark.Stages(trace), Benchmark.PeakMegabytes()));
        // Entry 246: the most memory held and where the time went, so a phone's run can be read from its log alone.
        DiagnosticLog.Info("phone.detect", ("named", chosen is null), ("holes", result.Detections.Count), ("failure", result.Failure), ("ms", clock.ElapsedMilliseconds),
            ("peakMb", Benchmark.PeakMegabytes()), ("stages", string.Join(" ", Benchmark.Stages(trace).Select(s => $"{s.Stage}={s.Milliseconds}"))));
        var check = GroupLab.Core.Capture.PictureCheck.Of(grey, definition, result, codesRead, torch);
        DiagnosticLog.Info("phone.check", ("check", check.Describe()), ("torch", torch));
        if (result.Failure is not null || result.Scale is null)
        {
            return new PhoneResult(session.State, definition, (result.Failure ?? "The sheet's markers could not be matched").TrimEnd('.') + ".", null, working, Check: check);
        }

        session.LoadDetections(result.Scale, result.Bulls, result.Detections, result.Assignment, result.Rejected ?? [], result.Summary, result.Detection, result.Capture, result.SetSheet);
        long? id = Save(session.State, definition, units, null);
        return new PhoneResult(session.State, definition, null, id, working, Check: check);
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

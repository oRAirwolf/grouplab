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

namespace GroupLab.Mobile;

/// <summary>The working copy of a photograph: the session's own image, its metadata at that size, and the photograph's own size.</summary>
internal sealed record WorkingImage(string Path, ImageMetadata Metadata, int OriginalWidth, int OriginalHeight);

/// <summary>What one photograph came to: the marking, the sheet it was analyzed as, and why it stopped, where it did.</summary>
internal sealed record PhoneResult(MarkingState State, TargetDefinition? Definition, string? Failure, long? SessionId, WorkingImage? Image = null, bool AskWhichSheet = false,
    GroupLab.Core.Capture.PictureVerdict? Check = null, GroupLab.Core.Measurement.ScaleReport? Measured = null, PaperEdge? Paper = null,
    TargetDefinition? LooksLike = null, StoreTargetSeen? Recognized = null, GroupLab.Core.Registration.OpeningOutcome? Opening = null);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 340: a store-bought target recognized in a picture that named no GroupLab sheet, and the working image's size
/// its bulls are placed in.
/// </summary>
internal sealed record StoreTargetSeen(GroupLab.Core.StoreTargets.StoreTargetRecognition Recognition, int Width, int Height);

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

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 313 section 1.4: the longest side the phone reads a whole picture's codes at. The iPad mini's reading
    /// hung on "Reading the sheet's codes". Of 21 real phone pictures measured on the desktop on 2026-09-30 (the Fold 7's sittings), 19 were
    /// named square on in 0.7 to 1.4 seconds; the other two read nothing square on, nothing in the whole picture at its size, doubled, halved
    /// or quartered, and were named by their codes cut out and enlarged. Doubling the whole 3266 pixel picture took 11 to 12.5 seconds of
    /// the 25 to 31 and raised the memory held from 474 to 1495 MB, and named nothing, so the phone does not double it; a picture is read
    /// at its own size, halved and quartered, then cut out and enlarged, as before.
    /// </summary>
    internal const int LongestReadSide = 4096;

    internal static string Files => Phone.Platform.FilesFolder;

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

        string folder = System.IO.Path.Combine(Files, "targets");

        // The frozen definitions too (entry 226 section 1): a sheet printed before the zeroing grids were redrawn is still identified.
        Phone.Platform.CopyBundledTargets(Files);

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
        double most = MemoryBudget.PhoneWorkingMegapixels(Budget());
        if (Phone.Platform.DecodeReduced(photo, most) is not { } decoded)
        {
            return null;
        }

        using var colour = decoded.Colour;
        var bounds = (OutWidth: decoded.Width, OutHeight: decoded.Height);
        int sample = decoded.Sample;

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
    public static double Budget() => Phone.Platform.MemoryBudgetMegabytes();

    /// <summary>
    /// A photograph, prepared and analyzed. Entry 243 section 3.2: <paramref name="progress"/> hears what it is doing, a step at a time, and a
    /// cancel leaves nothing behind: the working copy made for it is deleted, and nothing was saved yet.
    /// </summary>
    public static PhoneResult Run(string photo, ShotSetup setup, UnitSettings units, SurveyQueue? survey, CancellationToken token, Action<string>? progress = null, bool torch = false)
    {
        // Entry 288: an update never installs while a sheet is being read.
        using var running = WorkInProgress.Analysis();
        progress?.Invoke(GroupLab.Core.Trace.StageWords.Starting);
        ReadStage.Began("prepare");
        var clock = Stopwatch.StartNew();
        if (Prepare(photo) is not { } working)
        {
            return new PhoneResult(MarkingState.Empty, null, "The picture could not be read as an image.", null);
        }

        DiagnosticLog.Info("read.stage", [("stage", "prepare"), ("ms", clock.ElapsedMilliseconds), .. DeviceHealth.Fields()]);
        try
        {
            token.ThrowIfCancellationRequested();
            return Detect(working, null, setup, units, survey, token, progress, torch, photo);
        }
        catch (OperationCanceledException)
        {
            Forget(working);
            throw;
        }
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 313 section 1.2: a picture whose reading was stopped, prepared again and offered with the sheets to
    /// choose from and <paramref name="said"/> saying why, without reading its codes again.
    /// </summary>
    public static PhoneResult Unread(string photo, string said)
    {
        if (Prepare(photo) is not { } working)
        {
            return new PhoneResult(MarkingState.Empty, null, "The picture could not be read as an image.", null);
        }

        var session = new MarkingSession();
        session.Open(working.Path, working.Metadata.Orientation);
        return new PhoneResult(session.State, null, said, null, working, AskWhichSheet: true);
    }

    /// <summary>
    /// Entry 313 section 1.4: each stage of a reading in the log as it begins and ends, with its time, and each resolution the codes were
    /// read at as it is tried, so a sitting's log shows where the time went even when the reading never finished. Entry 315 section 4: a
    /// stage's end carries the memory in use and the phone's heat, and the stage begun is what the diagnostics overlay names.
    /// </summary>
    private static void Log(TraceRecorder trace)
    {
        trace.Begun += stage =>
        {
            ReadStage.Began(stage);
            DiagnosticLog.Info("read.stage", ("stage", stage), ("began", true));
        };
        trace.Filed += record => DiagnosticLog.Info("read.stage", [("stage", record.Stage), ("ms", record.DurationMs), ("status", record.Status), .. DeviceHealth.Fields()]);
        trace.Noted += (stage, line) => DiagnosticLog.Info("read.stage", ("stage", stage), ("detail", line));
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
    /// <param name="picture">The picture as the camera saved it, where GroupLab Dev keeps it with its trace (entry 291 section 7.5).</param>
    public static PhoneResult Detect(WorkingImage working, TargetDefinition? chosen, ShotSetup setup, UnitSettings units, SurveyQueue? survey, CancellationToken token, Action<string>? progress = null, bool torch = false, string? picture = null)
    {
        var clock = Stopwatch.StartNew();
        ReadStage.Began("load");
        var (grey, _) = ImageLoader.Load(working.Path);
        var (value, _) = ImageLoader.LoadMaxChannel(working.Path);
        DiagnosticLog.Info("read.stage", [("stage", "load"), ("ms", clock.ElapsedMilliseconds), ("size", $"{grey.Width}x{grey.Height}"), .. DeviceHealth.Fields()]);
        token.ThrowIfCancellationRequested();
        var backend = new OpenCvSharpBackend();
        var trace = new TraceRecorder();
        Log(trace);

        // Entry 291 section 3.1: the line names the step being done as it starts, where it named the next step only as the last finished.
        trace.Begun += stage =>
        {
            if (GroupLab.Core.Trace.StageWords.During(stage) is { } now)
            {
                progress?.Invoke(now);
            }
        };
        var session = new MarkingSession();
        session.Open(working.Path, working.Metadata.Orientation);
        session.SetCalibre(setup.Calibre);
        session.SetShotDistance(setup.DistanceInches);

        var identity = chosen is null ? SheetIdentification.Identify(grey, Library(), backend, trace, token, LongestReadSide) : null;
        var definition = chosen ?? identity!.Definition;
        int codesRead = identity?.CodesRead ?? 0;
        if (definition is null)
        {
            token.ThrowIfCancellationRequested();
            // Entry 260: every picture is checked, a picture that names no sheet included.
            var unread = GroupLab.Core.Capture.PictureCheck.Of(grey, null, null, codesRead, torch);

            // Entry 340 section 1: a target GroupLab did not print may be a store-bought one it knows, which is then marked with its bulls
            // placed and its printed size as the scale, or asked about first where it comes in sizes the picture cannot tell apart.
            var seen = Recognize(working.Path, grey.Width, grey.Height, token);
            // Entry 281: the codes' 0.4 mm modules get about 3 pixels each at the distance the whole sheet fits, so three of six pictures in
            // the camera test were refused here while the markers had named the layout on every frame. The markers never name a sheet by
            // themselves (SheetIdentification), so the sheet the picture looks most like is offered first, for the person to confirm.
            long alikeBegan = clock.ElapsedMilliseconds;
            var looksLike = GroupLab.Core.Capture.LiveSheet.MostAlike(grey, GroupLab.Core.Capture.LiveSheet.SheetsByMarkers(grey, Library(), backend), backend);
            DiagnosticLog.Info("read.stage", ("stage", "looks-like"), ("ms", clock.ElapsedMilliseconds - alikeBegan));
            DiagnosticLog.Info("phone.detect", ("named", false), ("ms", clock.ElapsedMilliseconds), ("looksLike", looksLike?.Name), ("check", unread.Describe()));
            if (picture is not null)
            {
                SittingRecord.Analyzed(picture, trace, "No sheet was named: " + (identity?.Failure ?? "none chosen"));
            }

            // Entry 356 section 5: a picture that looks like a GroupLab sheet and would not read is a problem; anything else is a target
            // GroupLab has not been told about, which is not one.
            var opening = seen is not null ? GroupLab.Core.Registration.OpeningOutcome.StoreTarget
                : GroupLab.Core.Registration.SheetOpening.Decide(identity ?? new SheetIdentity(null, null, 0, 0, null), false,
                    () => GroupLab.Core.Registration.SheetLook.Of(grey, backend, codesRead));
            DiagnosticLog.Info("phone.opening", ("outcome", opening.ToString()));
            if (opening == GroupLab.Core.Registration.OpeningOutcome.NotGroupLab)
            {
                return new PhoneResult(session.State, null, GroupLab.Core.Registration.OpeningWords.WhichSays, null, working, AskWhichSheet: true, Check: unread,
                    LooksLike: looksLike, Recognized: seen, Opening: opening);
            }

            // Entry 354 section 2: codes that were read and named a sheet GroupLab does not have are not codes that could not be read.
            return new PhoneResult(session.State, null,
                identity?.DefinitionId is { } named
                    ? $"GroupLab read the square codes: they name {named}, which is not among its sheets, and the description of the sheet they carry could not be read. Choose which sheet it is."
                    : "GroupLab could not read the square codes that name the sheet. Choose which sheet it is, or take the picture again with the whole sheet in view, square on, in even light.",
                null, working, AskWhichSheet: true, Check: unread, LooksLike: looksLike, Recognized: seen, Opening: opening);
        }

        // Entry 271: a photograph is corrected for the printer chosen, where one has been measured.
        if (PrinterCheck.IsCheckPage(definition))
        {
            // Entry 273: the printer check page is measured by the printer check, under Settings, Printers, not searched for holes.
            return new PhoneResult(session.State, definition, "This is the printer check page. To measure your printer with it, open Settings, then Printers, then Add a printer or Check again.", null, working);
        }

        var result = AutomaticMarking.Run(grey, value, working.Metadata, definition, backend, trace, token, setup.Calibre, printer: Phone.Settings.PrinterForPhotos());
        survey?.Record(new AnalysisFacts(working.OriginalWidth, working.OriginalHeight, grey.Width, grey.Height, Benchmark.Stages(trace), Benchmark.PeakMegabytes()));
        // Entry 246: the most memory held and where the time went, so a phone's run can be read from its log alone.
        DiagnosticLog.Info("phone.detect", ("named", chosen is null), ("holes", result.Detections.Count), ("failure", result.Failure), ("ms", clock.ElapsedMilliseconds),
            ("peakMb", Benchmark.PeakMegabytes()), ("stages", string.Join(" ", Benchmark.Stages(trace).Select(s => $"{s.Stage}={s.Milliseconds}"))));
        // Entry 291 section 3.3: the markers the picture read, at its own size and field of view, against those the last live frame read and
        // foretold (camera.live), so the two can be compared picture by picture.
        DiagnosticLog.Info("phone.markers", ("read", result.Measurement.Fiducials?.Matches.Count), ("of", result.Measurement.Fiducials?.Expected),
            ("codes", codesRead), ("picture", $"{working.OriginalWidth}x{working.OriginalHeight}"), ("measured", $"{grey.Width}x{grey.Height}"));
        var check = GroupLab.Core.Capture.PictureCheck.Of(grey, definition, result, codesRead, torch);
        DiagnosticLog.Info("phone.check", ("check", check.Describe()), ("torch", torch));
        if (picture is not null)
        {
            SittingRecord.Analyzed(picture, trace, $"{definition.Name}: {result.Detections.Count} holes, {result.Measurement.Fiducials?.Matches.Count} of {result.Measurement.Fiducials?.Expected} markers, {codesRead} codes; {check.Describe()}");
        }

        if (result.Failure is not null || result.Scale is null)
        {
            return new PhoneResult(session.State, definition, (result.Failure ?? "The sheet's markers could not be matched").TrimEnd('.') + ".", null, working, Check: check);
        }

        session.LoadDetections(result.Scale, result.Bulls, result.Detections, result.Assignment, result.Rejected ?? [], result.Summary, result.Detection, result.Capture, result.SetSheet);

        // Entry 291 section 2.1: the sheet shown upright as the registration finds it, on the result, the shared picture and the report.
        int upright = ViewRotation.Upright(session.State.Scale, grey.Width, grey.Height, session.State.ViewQuarterTurns);
        if (upright != session.State.ViewQuarterTurns)
        {
            session.Load(session.State with { ViewQuarterTurns = upright });
        }

        // Entry 313 section 1.1: a reading stopped while it ran saves nothing, since nobody will see its result.
        token.ThrowIfCancellationRequested();
        long? id = Save(session.State, definition, units, null);
        return new PhoneResult(session.State, definition, null, id, working, Check: check, Measured: result.Measurement.Scale, Paper: result.Paper);
    }

    /// <summary>
    /// Entry 340: the working image against the store-bought targets GroupLab has fingerprints of; null where none was recognized or the
    /// picture could not be read for it, which leaves the picture to be named or marked by hand as before.
    /// </summary>
    internal static StoreTargetSeen? Recognize(string path, int width, int height, CancellationToken token)
    {
        ReadStage.Began("store-target");
        var clock = Stopwatch.StartNew();
        try
        {
            token.ThrowIfCancellationRequested();
            var seen = GroupLab.Core.StoreTargets.StoreTargetRecognizer.Recognize(path, new OpenCvFingerprintBackend());
            DiagnosticLog.Info("read.stage", ("stage", "store-target"), ("ms", clock.ElapsedMilliseconds), ("decided", seen?.Describe()));
            return seen is { Found: true } ? new StoreTargetSeen(seen, width, height) : null;
        }
        catch (Exception e) when (e is IOException or InvalidDataException or OpenCVException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "storetarget.recognize", e);
            return null;
        }
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

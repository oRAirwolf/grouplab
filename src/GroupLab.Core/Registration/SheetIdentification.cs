using System.Globalization;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Trace;

namespace GroupLab.Core.Registration;

/// <summary>
/// What a sheet's printed codes say it is: the identifier they carry, the candidate definition with that identifier, the tile index in the
/// frame, how many codes were read, and the resolution the frame was read at as a multiple of the image's own; or, with no definition, why not.
/// </summary>
public sealed record SheetIdentity(TargetDefinition? Definition, string? DefinitionId, int TileIndex, int CodesRead, string? Failure, double? Scale = null)
{
    /// <summary>
    /// Entry 354 section 2: the definition was rebuilt from the codes themselves, because no definition on this machine has the identifier
    /// they carry. A sheet GroupLab generated and printed without saving is one: its codes are the only copy of its definition.
    /// </summary>
    public bool FromItsCodes { get; init; }

    /// <summary>Entry 356 section 6: the sheet was named by the harder reading, not the first.</summary>
    public bool ReadHarder { get; init; }

    /// <summary>Entry 356 section 6 item 4: the sheet was named by its printed identifier and title, its codes still unread.</summary>
    public bool ByPrintedName { get; init; }
}

/// <summary>
/// The sheet's definition read off the sheet, NOTES-FROM-PLANNING.md entry 35 section 6 item 3, so that <c>grouplab analyze</c> needs no
/// <c>--target</c> and the marking screen asks for a definition only when the codes cannot give one.
/// <para>
/// Every GroupLab sheet prints its definition as a GLTD-B frame in each of its QR codes, and the identifier is computed from the frame's
/// body (TARGET-SCHEMA.md sections 5 and 6), so a frame that passes its CRC names exactly one definition. The markers cannot do this: the
/// built-in definitions share marker ids, and registering against the wrong definition can look plausible.
/// </para>
/// <para>
/// The image is read at full, double, half and quarter resolution, stopping at the first that yields a valid frame, since a detector that
/// misses a code at one resolution can find it at another. Nothing is guessed. Codes that name two definitions or two tiles, and a definition that
/// is not among the candidates, are refused with the reason, and the caller asks for the definition instead.
/// </para>
/// </summary>
public static partial class SheetIdentification
{
    /// <summary>
    /// The resolutions tried, as multiples of the image's own, in order. Double comes second because a 300 DPI sheet's code modules are under
    /// five pixels, which half and quarter resolution only shrink: the clean 300 DPI render of GL-CF25-LTR read at full resolution on Windows
    /// and gave no code on the Linux runner, whose OpenCV is a different native build.
    /// </summary>
    public static IReadOnlyList<double> Scales { get; } = [1.0, 2.0, 0.5, 0.25];

    /// <summary>
    /// The longest side, in pixels, a resolution may make the image; one that would make it longer is skipped and the skip recorded. It is
    /// a 4000 pixel photograph doubled. Doubling a 600 DPI scan gives the detector nothing the scan did not, and on the Phase 0 sweep it
    /// cost 52.7 s on the one 600 DPI scan that then read at quarter resolution, where the detection before doubling took 8.6 s.
    /// </summary>
    public const int MaximumWorkingSide = 8000;

    /// <param name="cancellation">Checked before each resolution is read and each code cut out is enlarged, so a screen can stop a long identification (NOTES-FROM-PLANNING.md entry 76 section 4, entry 313 section 1.1).</param>
    /// <param name="longestSide">
    /// The longest side a resolution may make the image, <see cref="MaximumWorkingSide"/> unless the caller asks for less. The phone asks for
    /// less (NOTES-FROM-PLANNING.md entry 313 section 1.4): on its pictures doubling the whole picture never named a sheet, took 11 to 12.5
    /// seconds on the desktop and tripled the memory the reading held, and the codes cut out and enlarged read what it could.
    /// </param>
    /// <param name="wholeOnlyWithoutMarkers">Entry 386 section 2: where no GroupLab marker is found, read the whole picture at full size only. False only to measure the old way.</param>
    public static SheetIdentity Identify(GrayImage image, IReadOnlyList<TargetDefinition> candidates, IImagingBackend backend, TraceRecorder trace, CancellationToken cancellation = default, int longestSide = MaximumWorkingSide, bool wholeOnlyWithoutMarkers = true)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(backend);
        ArgumentNullException.ThrowIfNull(trace);
        var inv = CultureInfo.InvariantCulture;
        using var stage = trace.Begin("S0.identify");
        stage.Parameter("candidates", string.Create(inv, $"{candidates.Count} definitions"));
        stage.Parameter("resolutions", string.Join(", ", Scales.Select(s => s.ToString("0.##", inv))));
        int read = 0;

        // Entry 291 section 3.1: the codes turned square on where the markers put them, first. An off-axis picture's codes read nowhere in
        // the whole picture and only enlarged in the cut-outs, after 20 seconds of looking; square on, each is read once in milliseconds.
        // Where the markers fit no layout, or nothing square on reads, the whole picture is read as before.
        cancellation.ThrowIfCancellationRequested();
        long viewsBegan = System.Diagnostics.Stopwatch.GetTimestamp();
        var views = Capture.LiveSheet.CodeViews(image, candidates, backend);
        if (views.Count > 0)
        {
            // One read for each code: a view of a code already read, from a sheet sharing the layout, is not read again.
            var squareOn = new List<byte[]>();
            var readAt = new List<Capture.CodeView>();
            foreach (var view in views)
            {
                cancellation.ThrowIfCancellationRequested();
                if (readAt.Any(r => r.SameCodeAs(view)))
                {
                    continue;
                }

                var found = backend.ReadCutOut(view.Image, 1.0);
                if (found.Count > 0)
                {
                    squareOn.Add(found[0]);
                    readAt.Add(view);
                }
            }

            read += squareOn.Count;
            var viewFrames = squareOn.Select(p => GltdBinary.Decode([p])).Where(d => d.DefinitionId is not null).ToList();
            stage.Detail(string.Create(inv, $"square on where the markers put them: {views.Count} places, {squareOn.Count} codes read, {viewFrames.Count} valid frames, {(long)System.Diagnostics.Stopwatch.GetElapsedTime(viewsBegan).TotalMilliseconds} ms"));
            var viewIds = viewFrames.Select(f => f.DefinitionId!).Distinct(StringComparer.Ordinal).ToList();
            var viewTiles = viewFrames.Select(f => (int)f.TileIndex).Distinct().ToList();
            if (viewIds.Count == 1 && viewTiles.Count == 1 && Named(candidates, viewFrames, viewIds[0]) is { } viewMatch)
            {
                stage.Parameter("definition", viewIds[0]);
                stage.Metric("codes decoded", viewFrames.Count, "count");
                stage.Done(StageStatus.Ok, string.Create(inv, $"{viewIds[0]}{(viewMatch.Definition.Tiling is null ? "" : $", tile {viewTiles[0]}")}, from {viewFrames.Count} codes square on where the markers put them{FromCodesWords(viewMatch.FromItsCodes)}"));
                return new SheetIdentity(viewMatch.Definition, viewIds[0], viewTiles[0], read, null, null) { FromItsCodes = viewMatch.FromItsCodes };
            }
        }

        // Entry 386 section 2, question 83 (b): a picture in which no GroupLab marker is found at all, such as a store-bought target, is read
        // once, whole, at full size, without the other sizes, the corners or the cut-outs. Alan's Rigid crosshair photo took 33.9 s through
        // every one of them. The corpus lost no sheet this way (docs/PHASE1-RESULTS.md, entry 386).
        bool noMarker = wholeOnlyWithoutMarkers && views.Count == 0 && Capture.LiveSheet.PhotographMarkers(image, backend).Count == 0;
        if (noMarker)
        {
            stage.Detail("no GroupLab marker found, so the picture is read whole at full size only");
        }

        foreach (double scale in noMarker ? [1.0] : Scales)
        {
            cancellation.ThrowIfCancellationRequested();
            if (Math.Max(image.Width, image.Height) * scale > longestSide)
            {
                stage.Detail(string.Create(inv, $"at {scale:0.##} times full resolution: skipped, which would make the image longer than {longestSide} px"));
                continue;
            }

            long began = System.Diagnostics.Stopwatch.GetTimestamp();
            var payloads = noMarker ? backend.ReadCutOut(image, scale) : backend.ReadCodes(image, scale);
            long took = (long)System.Diagnostics.Stopwatch.GetElapsedTime(began).TotalMilliseconds;
            read += payloads.Count;
            var frames = payloads.Select(p => GltdBinary.Decode([p])).Where(d => d.DefinitionId is not null).ToList();
            stage.Detail(string.Create(inv, $"at {scale:0.##} times full resolution: {payloads.Count} codes read, {frames.Count} valid frames, {took} ms"));
            if (frames.Count == 0)
            {
                continue;
            }

            var ids = frames.Select(f => f.DefinitionId!).Distinct(StringComparer.Ordinal).ToList();
            if (ids.Count > 1)
            {
                return Failed(stage, null, read, $"the sheet's codes name more than one definition, {string.Join(" and ", ids)}");
            }

            var tiles = frames.Select(f => (int)f.TileIndex).Distinct().Order().ToList();
            if (tiles.Count > 1)
            {
                return Failed(stage, ids[0], read, $"the codes of {ids[0]} name more than one tile, {string.Join(" and ", tiles)}");
            }

            // Entry 354 section 2: a definition on no list here is rebuilt from the codes, which carry it whole (TARGET-SCHEMA.md section 6);
            // only codes that decode to no valid definition are refused.
            if (Named(candidates, frames, ids[0]) is not { } named)
            {
                return Failed(stage, ids[0], read, string.Create(inv, $"the sheet's codes name {ids[0]}, which is not among the {candidates.Count} definitions searched, and the codes do not hold a definition GroupLab can read"));
            }

            var match = named.Definition;

            // Entry 282 section 5: "1 of 2 codes read" on every good picture of the camera test, because reading stops at the first
            // resolution that gives one. Where fewer than the sheet has were read, the rest are read cut out where the markers put them, so
            // the count is true and the second code checks the first.
            if (match.Codes is { Count: > 1 } all && frames.Count < all.Count)
            {
                var more = Capture.LiveSheet.CodeCrops(image, [match], backend)
                    .Select(crop => CropScales.Select(s =>
                    {
                        cancellation.ThrowIfCancellationRequested();
                        return backend.ReadCutOut(crop, s);
                    }).FirstOrDefault(r => r.Count > 0) ?? [])
                    .SelectMany(r => r.Take(1)).Select(p => GltdBinary.Decode([p])).Where(d => d.DefinitionId == ids[0]).ToList();
                if (more.Count > frames.Count)
                {
                    stage.Detail(string.Create(inv, $"cut out where the markers put them: {more.Count} codes of {ids[0]}"));
                    read = Math.Max(read, more.Count);
                    frames = more;
                }
            }

            stage.Parameter("definition", ids[0]);
            stage.Metric("codes decoded", frames.Count, "count");
            stage.Done(StageStatus.Ok, string.Create(inv, $"{ids[0]}{(match.Tiling is null ? "" : $", tile {tiles[0]}")}, from {frames.Count} codes at {scale:0.##} times full resolution{FromCodesWords(named.FromItsCodes)}"));
            return new SheetIdentity(match, ids[0], tiles[0], read, null, scale) { FromItsCodes = named.FromItsCodes };
        }

        if (noMarker)
        {
            return Failed(stage, null, read, read == 0 ? "no GroupLab marker was found and no code could be read" : "no GroupLab marker was found and no code held a valid GroupLab frame");
        }

        // Entry 282 section 5: the codes cut out where the sheet's markers put them, and read enlarged. On the Fold 7's pictures a module
        // got about 3.1 pixels and read only at three times, which the whole picture cannot be.
        cancellation.ThrowIfCancellationRequested();
        var near = new List<byte[]>();
        long cutBegan = System.Diagnostics.Stopwatch.GetTimestamp();
        foreach (var crop in Capture.LiveSheet.CodeCrops(image, candidates, backend))
        {
            foreach (double scale in CropScales)
            {
                // Entry 313 section 1.1: checked before each enlargement too, the slowest reading there is.
                cancellation.ThrowIfCancellationRequested();
                var found = backend.ReadCutOut(crop, scale);
                if (found.Count > 0)
                {
                    near.Add(found[0]); // a cut-out holds one code, however often the reader finds it
                    break;
                }
            }
        }

        read += near.Count;
        var nearFrames = near.Select(p => GltdBinary.Decode([p])).Where(d => d.DefinitionId is not null).ToList();
        stage.Detail(string.Create(inv, $"cut out where the markers put them and enlarged: {near.Count} codes read, {nearFrames.Count} valid frames, {(long)System.Diagnostics.Stopwatch.GetElapsedTime(cutBegan).TotalMilliseconds} ms"));
        var nearIds = nearFrames.Select(f => f.DefinitionId!).Distinct(StringComparer.Ordinal).ToList();
        var nearTiles = nearFrames.Select(f => (int)f.TileIndex).Distinct().ToList();
        if (nearIds.Count == 1 && nearTiles.Count == 1 && Named(candidates, nearFrames, nearIds[0]) is { } nearMatch)
        {
            stage.Parameter("definition", nearIds[0]);
            stage.Metric("codes decoded", nearFrames.Count, "count");
            stage.Done(StageStatus.Ok, string.Create(inv, $"{nearIds[0]}, from {nearFrames.Count} codes cut out where the markers put them{FromCodesWords(nearMatch.FromItsCodes)}"));
            return new SheetIdentity(nearMatch.Definition, nearIds[0], nearTiles[0], read, null, null) { FromItsCodes = nearMatch.FromItsCodes };
        }

        return Failed(stage, null, read, read == 0 ? "no code on the sheet could be read" : "no code on the sheet held a valid GroupLab frame");
    }

    /// <summary>
    /// The definition the codes name: the candidate with that identifier, or, where no candidate has it, the definition the codes carry
    /// (entry 354 section 2). Null only where neither exists.
    /// </summary>
    private static (TargetDefinition Definition, bool FromItsCodes)? Named(IReadOnlyList<TargetDefinition> candidates, IReadOnlyList<DecodeResult> frames, string id)
    {
        if (candidates.FirstOrDefault(c => GltdBinary.Encode(c).Encoding?.DefinitionId == id) is { } listed)
        {
            return (listed, false);
        }

        return frames.FirstOrDefault(f => f.DefinitionId == id && f.Definition is not null)?.Definition is { } carried ? (carried, true) : null;
    }

    private static string FromCodesWords(bool fromCodes) => fromCodes ? "; no definition here has it, so it was read from the codes themselves" : "";

    /// <summary>The enlargements a code cut out of the picture is read at, in order: a phone picture's module of about 3 pixels read at three times.</summary>
    public static IReadOnlyList<double> CropScales { get; } = [2.0, 3.0, 4.0];

    /// <summary>
    /// Every readable definition under the directories, subdirectories included so frozen definitions are found, in path order so the choice
    /// between two copies of one definition is stable. A directory that does not exist is skipped.
    /// </summary>
    public static IReadOnlyList<TargetDefinition> Candidates(IEnumerable<string> directories)
    {
        ArgumentNullException.ThrowIfNull(directories);
        return [.. directories.Where(Directory.Exists)
            .SelectMany(d => Directory.EnumerateFiles(d, "*.gltd.json", SearchOption.AllDirectories))
            .Order(StringComparer.Ordinal)
            .Select(path => GltdJsonReader.ReadFile(path).Definition)
            .OfType<TargetDefinition>()];
    }

    private static SheetIdentity Failed(StageScope stage, string? id, int read, string reason)
    {
        stage.Done(StageStatus.Failed, reason);
        return new SheetIdentity(null, id, 0, read, reason);
    }
}

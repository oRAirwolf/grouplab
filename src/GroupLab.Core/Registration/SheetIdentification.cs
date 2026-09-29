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
public sealed record SheetIdentity(TargetDefinition? Definition, string? DefinitionId, int TileIndex, int CodesRead, string? Failure, double? Scale = null);

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
public static class SheetIdentification
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

    /// <param name="cancellation">Checked before each resolution is read, so a screen can stop a long identification (NOTES-FROM-PLANNING.md entry 76 section 4).</param>
    public static SheetIdentity Identify(GrayImage image, IReadOnlyList<TargetDefinition> candidates, IImagingBackend backend, TraceRecorder trace, CancellationToken cancellation = default)
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
        foreach (double scale in Scales)
        {
            cancellation.ThrowIfCancellationRequested();
            if (Math.Max(image.Width, image.Height) * scale > MaximumWorkingSide)
            {
                stage.Detail(string.Create(inv, $"at {scale:0.##} times full resolution: skipped, which would make the image longer than {MaximumWorkingSide} px"));
                continue;
            }

            var payloads = backend.ReadCodes(image, scale);
            read += payloads.Count;
            var frames = payloads.Select(p => GltdBinary.Decode([p])).Where(d => d.DefinitionId is not null).ToList();
            stage.Detail(string.Create(inv, $"at {scale:0.##} times full resolution: {payloads.Count} codes read, {frames.Count} valid frames"));
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

            var match = candidates.FirstOrDefault(c => GltdBinary.Encode(c).Encoding?.DefinitionId == ids[0]);
            if (match is null)
            {
                return Failed(stage, ids[0], read, string.Create(inv, $"the sheet's codes name {ids[0]}, which is not among the {candidates.Count} definitions searched"));
            }

            // Entry 282 section 5: "1 of 2 codes read" on every good picture of the camera test, because reading stops at the first
            // resolution that gives one. Where fewer than the sheet has were read, the rest are read cut out where the markers put them, so
            // the count is true and the second code checks the first.
            if (match.Codes is { Count: > 1 } all && frames.Count < all.Count)
            {
                var more = Capture.LiveSheet.CodeCrops(image, [match], backend)
                    .Select(crop => CropScales.Select(s => backend.ReadCodes(crop, s)).FirstOrDefault(r => r.Count > 0) ?? [])
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
            stage.Done(StageStatus.Ok, string.Create(inv, $"{ids[0]}{(match.Tiling is null ? "" : $", tile {tiles[0]}")}, from {frames.Count} codes at {scale:0.##} times full resolution"));
            return new SheetIdentity(match, ids[0], tiles[0], read, null, scale);
        }

        // Entry 282 section 5: the codes cut out where the sheet's markers put them, and read enlarged. On the Fold 7's pictures a module
        // got about 3.1 pixels and read only at three times, which the whole picture cannot be.
        cancellation.ThrowIfCancellationRequested();
        var near = new List<byte[]>();
        foreach (var crop in Capture.LiveSheet.CodeCrops(image, candidates, backend))
        {
            foreach (double scale in CropScales)
            {
                var found = backend.ReadCodes(crop, scale);
                if (found.Count > 0)
                {
                    near.Add(found[0]); // a cut-out holds one code, however often the reader finds it
                    break;
                }
            }
        }

        read += near.Count;
        var nearFrames = near.Select(p => GltdBinary.Decode([p])).Where(d => d.DefinitionId is not null).ToList();
        stage.Detail(string.Create(inv, $"cut out where the markers put them and enlarged: {near.Count} codes read, {nearFrames.Count} valid frames"));
        var nearIds = nearFrames.Select(f => f.DefinitionId!).Distinct(StringComparer.Ordinal).ToList();
        var nearTiles = nearFrames.Select(f => (int)f.TileIndex).Distinct().ToList();
        if (nearIds.Count == 1 && nearTiles.Count == 1 && candidates.FirstOrDefault(c => GltdBinary.Encode(c).Encoding?.DefinitionId == nearIds[0]) is { } nearMatch)
        {
            stage.Parameter("definition", nearIds[0]);
            stage.Metric("codes decoded", nearFrames.Count, "count");
            stage.Done(StageStatus.Ok, string.Create(inv, $"{nearIds[0]}, from {nearFrames.Count} codes cut out where the markers put them"));
            return new SheetIdentity(nearMatch, nearIds[0], nearTiles[0], read, null, null);
        }

        return Failed(stage, null, read, read == 0 ? "no code on the sheet could be read" : "no code on the sheet held a valid GroupLab frame");
    }

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

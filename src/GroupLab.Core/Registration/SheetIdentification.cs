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

    public static SheetIdentity Identify(GrayImage image, IReadOnlyList<TargetDefinition> candidates, IImagingBackend backend, TraceRecorder trace)
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

            stage.Parameter("definition", ids[0]);
            stage.Metric("codes decoded", frames.Count, "count");
            stage.Done(StageStatus.Ok, string.Create(inv, $"{ids[0]}{(match.Tiling is null ? "" : $", tile {tiles[0]}")}, from {frames.Count} codes at {scale:0.##} times full resolution"));
            return new SheetIdentity(match, ids[0], tiles[0], read, null, scale);
        }

        return Failed(stage, null, read, read == 0 ? "no code on the sheet could be read" : "no code on the sheet held a valid GroupLab frame");
    }

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

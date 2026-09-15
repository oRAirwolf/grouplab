using System.Diagnostics;
using System.Globalization;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Registration;
using GroupLab.Core.Trace;

namespace GroupLab.Cli.Spike;

/// <summary>
/// <c>grouplab identify sweep</c>: every Phase 0 image through <see cref="SheetIdentification"/> against the whole of <c>targets</c>, frozen
/// definitions included, with the definition and tile each image was printed from as the truth (NOTES-FROM-PLANNING.md entry 35 section 6
/// item 3 and entry 47 section 3). The QR detectors are native code, a different build on each platform, so how many sheets name themselves
/// is measured on each platform rather than assumed from one. It prints a table and a count and writes no record. It fails only when an
/// image names a definition or a tile it was not printed from, because a refusal is the safe outcome and a wrong name is a defect.
/// </summary>
public static class IdentifySweep
{
    public static int Run(string scans, string targets, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var inv = CultureInfo.InvariantCulture;
        var candidates = SheetIdentification.Candidates([targets]);
        var backend = new OpenCvSharpBackend();
        output.WriteLine("| Image | Kind | Named | Tile | Read at | Time (ms) | Result |");
        output.WriteLine("|---|---|---|---|---|---|---|");
        int named = 0, wrong = 0;
        foreach (var sample in SampleSet.All)
        {
            string truth = GltdBinary.Encode(GltdJsonReader.ReadFile(Path.Combine(SampleSet.FrozenDirectory, sample.Definition)).Definition!).Encoding!.DefinitionId;
            var (grey, _) = ImageLoader.Load(Path.Combine(scans, sample.File));
            var clock = Stopwatch.StartNew();
            var identity = SheetIdentification.Identify(grey, candidates, backend, new TraceRecorder());
            clock.Stop();
            string result;
            if (identity.Definition is null)
            {
                result = "not named: " + identity.Failure;
            }
            else if (identity.DefinitionId == truth && identity.TileIndex == sample.Tile)
            {
                named++;
                result = "right";
            }
            else
            {
                wrong++;
                result = string.Create(inv, $"**wrong**: printed from {truth}, tile {sample.Tile}");
            }

            string tile = identity.Definition is null ? "" : identity.TileIndex.ToString(inv);
            string scale = identity.Scale is { } s ? string.Create(inv, $"{s:0.##}x") : "";
            output.WriteLine(string.Create(inv, $"| `{sample.File}` | {sample.Kind.ToString().ToLowerInvariant()} | {identity.DefinitionId ?? ""} | {tile} | {scale} | {clock.ElapsedMilliseconds} | {result} |"));
        }

        int total = SampleSet.All.Count;
        output.WriteLine();
        output.WriteLine(string.Create(inv, $"{named} of {total} images named the definition and tile they were printed from, {wrong} named a wrong one, and {total - named - wrong} were not named."));
        return wrong == 0 ? 0 : 1;
    }
}

using System.Globalization;
using System.Text.Json;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Derivation;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Gltd.Validation;
using GroupLab.Core.Rendering;
using GroupLab.Core.Registration;

namespace GroupLab.Cli.Library;

/// <summary>One sheet of the marker module sweep, with the marker centres <c>tools/layout/module_sweep.py</c> derived for it.</summary>
public sealed record SweepSheet(int ModuleDmm, string FileName, TargetDefinition Definition, IReadOnlyList<PointDmm> LayoutMarkers);

/// <summary>
/// The marker module sweep of FIDUCIAL-DECISION.md section 10, measurement 2 (PHASE1-BRIEF.md M0): GL-CF25-LTR at 0.3,
/// 0.4, 0.5, 0.6 and 0.8 mm modules. Everything but the fiducials block is GL-CF25-LTR as the Phase 0 sample set was
/// printed, the frozen <c>GL-YCSK-DZZ1-R0VJ-4T5Y</c>, so the 0.5 mm sheet is the Phase 0 sheet by identifier and the
/// sweep carries its own control (PHASE1-RESULTS.md M0). It is not the live library sheet, whose sighter row moved in
/// the geometry change of NOTES-FROM-PLANNING.md entry 13. tag36h11 prints 8 modules across and TARGET-SCHEMA.md section 3.7 defines <c>markerSize</c> as that square's edge,
/// so a module of m dmm is a marker of 8m; the quiet zone stays at two modules, FIDUCIAL-DECISION.md's 1.0 mm at 0.5 mm.
/// The lattice is derived here by the C# port and checked against <c>layout.py</c>'s, marker for marker, because
/// CONTRIBUTING.md makes the tool the authority. These are measurement sheets, not built-in library sheets.
/// </summary>
public static class ModuleSweep
{
    public const string Base = "GL-CF25-LTR";

    /// <summary>tag36h11 prints 8 modules across: a 6 by 6 data field in a one-module black border, as the renderer draws it.</summary>
    public const int TagModules = 8;

    public const int QuietModules = 2;

    public static IReadOnlyList<int> ModulesDmm { get; } = [3, 4, 5, 6, 8];

    public static IReadOnlyList<SweepSheet> Build(string baseDefinitionPath, string sweepJsonPath)
    {
        var reference = GltdJsonReader.ReadFile(baseDefinitionPath).Definition
            ?? throw new InvalidDataException($"{baseDefinitionPath} is not a valid definition.");
        using var document = JsonDocument.Parse(File.ReadAllBytes(sweepJsonPath));
        var rows = document.RootElement.GetProperty("sheets").EnumerateArray().ToDictionary(r => r.GetProperty("module_dmm").GetInt32());
        var sheets = new List<SweepSheet>();
        foreach (int module in ModulesDmm)
        {
            var row = rows[module];
            int size = TagModules * module, quiet = QuietModules * module;
            if (row.GetProperty("marker_size").GetInt32() != size || row.GetProperty("quiet_zone").GetInt32() != quiet)
            {
                throw new InvalidDataException($"The {module} dmm row of {sweepJsonPath} does not declare a {size} dmm marker with a {quiet} dmm quiet zone.");
            }

            if (!Ints(row, "xs").SequenceEqual(reference.Bulls.Where(b => b.Scoring).Select(b => b.X).Distinct())
                || !Ints(row, "sighter_y").SequenceEqual(reference.Bulls.Where(b => !b.Scoring).Select(b => b.Y).Distinct()))
            {
                throw new InvalidDataException($"The {module} dmm row of {sweepJsonPath} does not place the bulls of {Base}.");
            }

            string mm = (module / 10.0).ToString("0.0", CultureInfo.InvariantCulture);
            var definition = reference with
            {
                Id = null,
                Name = $"GroupLab Marker Module Sweep, {mm} mm, Letter",
                Description = $"{Base} with its fiducial markers printed at a {mm} mm module: a {size / 10.0:0.0} mm marker and a {quiet / 10.0:0.0} mm quiet zone. " +
                    "A measurement sheet for FIDUCIAL-DECISION.md section 10, measurement 2, not a built-in target.",
                Author = "GroupLab measurement sweep",
                Fiducials = reference.Fiducials! with { MarkerSize = size, QuietZone = quiet, Markers = null },
            };
            definition = FiducialDerivation.WithDerivedMarkers(definition);
            var encoded = GltdBinary.Encode(definition);
            if (encoded.Encoding is null)
            {
                throw new InvalidOperationException($"The {mm} mm sheet does not encode: {string.Join("; ", encoded.Diagnostics)}");
            }

            var marks = row.GetProperty("marks").EnumerateArray().Select(p => new PointDmm(p[0].GetInt32(), p[1].GetInt32())).ToList();
            sheets.Add(new SweepSheet(module, $"module-{mm}mm.gltd.json", definition with { Id = encoded.Encoding.DefinitionId }, marks));
        }

        return sheets;
    }

    /// <summary>
    /// Builds the sweep, checks each lattice against <c>layout.py</c>, validates, renders and runs conformance test 43 at
    /// 300 and 600 DPI, writes each definition and its PDF to <paramref name="directory"/>, and prints the table.
    /// The as-printed geometry puts the sighters outside the lattice at a 60 dmm footprint or more, which test 26f makes an
    /// error, so the sheets render with <see cref="RenderOptions.AllowInvalid"/> and the finding stays in the validator
    /// column rather than refusing the measurement sheet (NOTES-FROM-PLANNING.md entries 11 and 13).
    /// </summary>
    public static int Run(string baseDefinitionPath, string sweepJsonPath, string directory, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        Directory.CreateDirectory(directory);
        var backend = new OpenCvSharpBackend();
        int failures = 0, pages = 0;
        output.WriteLine("| Module | Marker / quiet zone / footprint (dmm) | Markers | Matches layout.py | Identifier | Validator | PDF pages | Test 43, 300 DPI | Test 43, 600 DPI |");
        output.WriteLine("|---|---|---|---|---|---|---|---|---|");
        var options = new RenderOptions(AllowInvalid: true);
        foreach (var sheet in Build(baseDefinitionPath, sweepJsonPath))
        {
            var d = sheet.Definition;
            var derived = d.Fiducials!.Markers!.Select(m => new PointDmm(m.X, m.Y)).ToList();
            bool matches = derived.SequenceEqual(sheet.LayoutMarkers);

            var diagnostics = GltdValidator.Validate(d);
            string validator = diagnostics.Count == 0
                ? "clean"
                : string.Join("; ", diagnostics.GroupBy(x => (x.Severity, x.Test, x.Code)).Select(g => $"{g.Count()} {g.Key.Severity.ToString().ToLowerInvariant()} {g.Key.Code} (test {g.Key.Test ?? "none"})"));

            var rendered = TargetRenderer.Render(d, options);
            if (rendered.Pdf is null)
            {
                output.WriteLine($"| {sheet.ModuleDmm / 10.0:0.0} mm | | | | {d.Id} | {validator} | render refused: {string.Join("; ", rendered.Diagnostics)} | | |");
                failures++;
                continue;
            }

            pages += rendered.Pages.Count;
            File.WriteAllBytes(Path.Combine(directory, sheet.FileName), CanonicalJsonWriter.Write(d));
            File.WriteAllBytes(Path.Combine(directory, sheet.FileName.Replace(".gltd.json", ".pdf", StringComparison.Ordinal)), rendered.Pdf);

            var page = SceneBuilder.Build(d, options).Pages[0];
            string[] gate = [.. new[] { 300, 600 }.Select(dpi =>
            {
                var report = SyntheticScanCheck.Run(SceneRasterizer.Rasterize(page, dpi), d, page.TileIndex, dpi, Perturbation.Phase0, backend);
                failures += report.Passed ? 0 : 1;
                return $"{report.Summary()} {(report.Passed ? "pass" : "FAIL")}";
            })];

            failures += matches ? 0 : 1;
            output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"| {sheet.ModuleDmm / 10.0:0.0} mm | {d.Fiducials.MarkerSize} / {d.Fiducials.QuietZone} / {FiducialDerivation.Footprint(d.Fiducials)} | {derived.Count} | {(matches ? "yes" : "NO")} | `{d.Id}` | {validator} | {rendered.Pages.Count} | {gate[0]} | {gate[1]} |"));
        }

        output.WriteLine();
        output.WriteLine($"{pages} PDF page(s) in total; {failures} failure(s)");
        return failures == 0 ? 0 : 1;
    }

    private static List<int> Ints(JsonElement e, string name) => [.. e.GetProperty(name).EnumerateArray().Select(v => v.GetInt32())];
}

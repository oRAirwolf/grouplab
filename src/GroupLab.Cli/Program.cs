using System.Globalization;
using GroupLab.Cli;
using GroupLab.Cli.Imaging;
using GroupLab.Cli.Library;
using GroupLab.Cli.Spike;
using GroupLab.Core.Gltd;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Validation;
using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Trace;

return args switch
{
    ["validate", .. var files] when files.Length > 0 => Validate(files),
    ["canonical", var input] => Canonical(input, null),
    ["canonical", var input, "-o", var output] => Canonical(input, output),
    ["encode", var input] => Encode(input),
    ["decode", .. var frames] when frames.Length > 0 => Decode(frames),
    ["library", "build", var layouts, var directory] => LibraryBuild(layouts, directory),
    ["library", "verify", var layouts, var directory] => LibraryVerify(layouts, directory),
    ["render", var input, .. var rest] => Render(input, rest),
    ["selftest"] => SelfTest("targets"),
    ["selftest", var directory] => SelfTest(directory),
    ["measure", var image, var definition, .. var rest] => Measure(image, definition, rest),
    ["spike", "stability"] => StabilitySpike.Run("scans/phase0", SampleSet.FrozenDirectory, "scans/phase1", StabilitySpike.DefaultOrders, Console.Out),
    ["spike", "corners", "--export", var file] => CornerJournal.Export("scans/phase0", SampleSet.FrozenDirectory, file, Console.Out),
    ["spike", "corners", "--replay", var file] => CornerJournal.Replay("scans/phase0", SampleSet.FrozenDirectory, file, Console.Out),
    ["spike", var measurement] => Spike(measurement, "scans/phase0", SampleSet.FrozenDirectory),
    ["spike", var measurement, var scans, var targets] => Spike(measurement, scans, targets),
    ["sweep", "module", var baseDefinition, var sweep, var directory] => ModuleSweep.Run(baseDefinition, sweep, directory, Console.Out),
    ["surface", "synthetic"] => SurfaceSweep.Synthetic(SampleSet.FrozenDirectory, "scans/phase0/measurements", "scans/phase1", Console.Out),
    ["surface", "rendered"] => SurfaceSweep.Rendered(SampleSet.FrozenDirectory, "scans/phase1", Console.Out),
    ["surface", "frames"] => SurfaceFrames.Run("scans/phase0", SampleSet.FrozenDirectory, Console.Out),
    ["surface", "frames", "--joint"] => SurfaceFrames.Run("scans/phase0", SampleSet.FrozenDirectory, Console.Out, joint: true),
    ["surface", "lens-sweep"] => SurfaceLens.Sweep(SampleSet.FrozenDirectory, "scans/phase0/measurements", "scans/phase1", Console.Out),
    ["surface", "lens"] => SurfaceLens.Frames("scans/phase0", SampleSet.FrozenDirectory, Console.Out),
    ["surface", "noise"] => SurfaceNoise.Run("scans/phase0", SampleSet.FrozenDirectory, Console.Out),
    ["surface", "correlation"] => SurfaceCorrelation.Run("scans/phase0", SampleSet.FrozenDirectory, Console.Out),
    ["holes", "baseline"] => HolesBaseline.Run("scans", "scans/phase1", Console.Out),
    ["holes", "ink-proximity"] => InkProximity.Run("scans/phase0", SampleSet.FrozenDirectory, "scans/phase1", null, false, Console.Out),
    ["holes", "ink-proximity", "-v"] => InkProximity.Run("scans/phase0", SampleSet.FrozenDirectory, "scans/phase1", null, true, Console.Out),
    ["holes", "ink-proximity", "--local", var manifest] => InkProximity.Run("scans/phase0", SampleSet.FrozenDirectory, "scans/phase1", manifest, false, Console.Out),
    ["holes", "ink-proximity", "--local", var manifest, "-v"] => InkProximity.Run("scans/phase0", SampleSet.FrozenDirectory, "scans/phase1", manifest, true, Console.Out),
    ["holes", "split-calibration"] => SplitCalibration.Run("scans/phase0", SampleSet.FrozenDirectory, null, Console.Out),
    ["holes", "split-calibration", "--local", var manifest] => SplitCalibration.Run("scans/phase0", SampleSet.FrozenDirectory, manifest, Console.Out),
    ["holes", "synthetic"] => HolesSynthetic.Run("targets", "scans/phase1", Console.Out),
    ["holes", "synthetic", "--realism"] => HolesSynthetic.Run("targets", "scans/phase1", Console.Out, realismOnly: true),
    ["holes", "synthetic", "--held-out"] => HolesSynthetic.Run("targets", "scans/phase1", Console.Out, heldOut: true),
    ["mounted", "pair"] => MountedPair.Run("scans", "scans/phase1", Console.Out),
    ["mounted", "pair", "--debug", var directory] => ((Func<int>)(() => { MountedPair.DebugDirectory = directory; return MountedPair.Run("scans", "scans/phase1", Console.Out); }))(),
    ["intake", var submission, var publicRoot, .. var rest] => GroupLab.Cli.IntakeVerb.Run(submission, publicRoot, rest, Console.Out),
    ["scrub", var input, var output] => GroupLab.Cli.ScrubVerb.Run(input, output, Console.Out),
    ["publish-owner", var source, var target, .. var rest] => GroupLab.Cli.OwnerVerb.Run(source, target, rest, Console.Out),
    ["analyze", var image, .. var rest] => GroupLab.Cli.AnalyzeVerb.Run(image, rest, Console.Out, Console.Error),
    ["corpus", "counts", .. var rest] when rest.All(a => a == "--write") || rest is ["--local", _] or ["--local", _, "--write"] or ["--write", "--local", _] =>
        CorpusCounts.Run("scans/phase0", SampleSet.FrozenDirectory, "targets", rest.SkipWhile(a => a != "--local").Skip(1).FirstOrDefault(), rest.Contains("--write"), Console.Out),
    ["identify", "sweep"] => IdentifySweep.Run("scans/phase0", "targets", Console.Out),
    ["stats", "coverage"] => StatsCoverage.Run(Console.Out),
    ["stats", "range-table"] => StatsRangeTable.Run(StatsRangeTable.DefaultTable, 2, 100, 10_000_000, Console.Out),
    ["stats", "range-table", var from, var to] => StatsRangeTable.Run(StatsRangeTable.DefaultTable, int.Parse(from, CultureInfo.InvariantCulture), int.Parse(to, CultureInfo.InvariantCulture), 10_000_000, Console.Out),
    ["stats", "range-table", var from, var to, var replications] => StatsRangeTable.Run(StatsRangeTable.DefaultTable, int.Parse(from, CultureInfo.InvariantCulture), int.Parse(to, CultureInfo.InvariantCulture), long.Parse(replications, CultureInfo.InvariantCulture), Console.Out),
    ["surface", "general-sweep"] => SurfaceGeneral.Sweep(SampleSet.FrozenDirectory, "scans/phase0/measurements", "scans/phase1", Console.Out),
    ["surface", "general"] => SurfaceGeneral.Frames("scans/phase0", SampleSet.FrozenDirectory, Console.Out),
    _ => Usage(),
};

static int Render(string input, string[] rest)
{
    string? output = null;
    var options = new RenderOptions();
    for (int i = 0; i < rest.Length; i++)
    {
        switch (rest[i])
        {
            case "-o" when i + 1 < rest.Length:
                output = rest[++i];
                break;
            case "--filled":
                options = options with { Mode = DataBlockMode.Filled };
                break;
            case "--tile" when i + 1 < rest.Length:
                options = options with { TileIndex = int.Parse(rest[++i], CultureInfo.InvariantCulture) };
                break;
            case "--scale" when i + 1 < rest.Length:
                options = options with { Scale = double.Parse(rest[++i], CultureInfo.InvariantCulture) };
                break;
            case "--allow-invalid":
                options = options with { AllowInvalid = true };
                break;
            default:
                Console.Error.WriteLine($"render: unknown option {rest[i]}");
                return 2;
        }
    }

    var read = GltdJsonReader.ReadFile(input);
    Report(input, read.Diagnostics, Console.Error);
    if (read.Definition is null)
    {
        return 1;
    }

    var result = TargetRenderer.Render(read.Definition, options);
    Report(input, result.Diagnostics, Console.Error);
    if (result.Pdf is null)
    {
        return 1;
    }

    string path = output ?? Path.ChangeExtension(input, null).Replace(".gltd", "", StringComparison.Ordinal) + ".pdf";
    File.WriteAllBytes(path, result.Pdf);
    Console.WriteLine($"{result.DefinitionId}  {result.Pages.Count} page(s)  {path}");
    return 0;
}

// Conformance test 43 of TARGET-SCHEMA.md section 10 on every built-in sheet, rendered by SceneRasterizer rather than a
// PDF engine (test 39 ties the two together): every page at 300 DPI, then the reference sheet at 600 DPI and at the 96.2
// percent print scale of DESIGN.md, whose reported scale must match.
static int SelfTest(string directory)
{
    var backend = new OpenCvSharpBackend();
    string reference = Path.Combine(directory, "GL-CF25-LTR.gltd.json");
    var runs = Directory.EnumerateFiles(directory, "*.gltd.json").Order(StringComparer.Ordinal).Select(f => (Path: f, Dpi: 300, Scale: 1.0))
        .Append((reference, 600, 1.0))
        .Append((reference, 300, 0.962));

    Console.WriteLine($"Test 43 against {Perturbation.Phase0}");
    int failed = 0;
    foreach (var (path, dpi, scale) in runs)
    {
        var read = GltdJsonReader.ReadFile(path);
        var scenes = read.Definition is null ? null : SceneBuilder.Build(read.Definition);
        if (read.Definition is null || scenes is not { Pages.Count: > 0 })
        {
            Report(path, [.. read.Diagnostics, .. scenes?.Diagnostics ?? []], Console.Error);
            failed++;
            continue;
        }

        foreach (var page in scenes.Pages)
        {
            var report = SyntheticScanCheck.Run(SceneRasterizer.Rasterize(page, dpi, scale), read.Definition, page.TileIndex, dpi, Perturbation.Phase0, backend);
            bool passed = report.Passed && Math.Abs(report.Registration.Scale - scale) <= 0.0005;
            failed += passed ? 0 : 1;
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"{Path.GetFileName(path),-27} print {scale:0.000}  {report.Summary()}  {(passed ? "pass" : "FAIL")}"));
        }
    }

    Console.WriteLine(failed == 0 ? "all pages pass" : $"{failed} page(s) failing");
    return failed == 0 ? 0 : 1;
}

// PHASE0-SPIKE-BRIEF.md section 4: register an image against a definition and locate every bull. Prints the stage
// records in the console form of DETECTION-PIPELINE.md section 6.3 and the per-bull error table; --json writes the same
// as structured output.
static int Measure(string imagePath, string definitionPath, string[] rest)
{
    var options = new MeasureOptions();
    string? json = null;
    int verbosity = 1;
    try
    {
        for (int i = 0; i < rest.Length; i++)
        {
            string option = rest[i];
            string Next() => i + 1 < rest.Length ? rest[++i] : throw new FormatException($"{option} needs a value");
            switch (option)
            {
                case "--tile":
                    options = options with { TileIndex = int.Parse(Next(), CultureInfo.InvariantCulture) };
                    break;
                case "--dpi":
                    options = options with { Dpi = double.Parse(Next(), CultureInfo.InvariantCulture) };
                    break;
                case "--locator":
                    options = options with
                    {
                        Locator = Next() switch
                        {
                            "centroid" => BullLocatorKind.Centroid,
                            "edge" => BullLocatorKind.EdgeFit,
                            var other => throw new FormatException($"unknown locator {other}"),
                        },
                    };
                    break;
                case "--model":
                    options = options with
                    {
                        Model = Next() switch
                        {
                            "auto" => RegistrationModel.Auto,
                            "homography" => RegistrationModel.Homography,
                            "radial" => RegistrationModel.Radial,
                            "surface" => RegistrationModel.Surface,
                            var other => throw new FormatException($"unknown model {other}"),
                        },
                    };
                    break;
                case "--mask":
                    options = options with { MaskRadius = double.Parse(Next(), CultureInfo.InvariantCulture) };
                    break;
                case "--refine":
                    options = options with
                    {
                        Refinement = Next() switch
                        {
                            "none" => CornerRefinement.None,
                            "subpix" => CornerRefinement.Subpixel,
                            "contour" => CornerRefinement.Contour,
                            var other => throw new FormatException($"unknown refinement {other}"),
                        },
                    };
                    break;
                case "--refine-window":
                    options = options with { RefinementWindowModules = double.Parse(Next(), CultureInfo.InvariantCulture) };
                    break;
                case "--threshold-window":
                    options = options with { ThresholdWindowMaxPixels = int.Parse(Next(), CultureInfo.InvariantCulture) };
                    break;
                case "--downsample":
                    options = options with { DownsampleFactor = int.Parse(Next(), CultureInfo.InvariantCulture) };
                    break;
                case "--json":
                    json = Next();
                    break;
                case "-v":
                    verbosity = int.Parse(Next(), CultureInfo.InvariantCulture);
                    break;
                default:
                    throw new FormatException($"unknown option {option}");
            }
        }
    }
    catch (FormatException ex)
    {
        Console.Error.WriteLine($"measure: {ex.Message}");
        return 2;
    }

    var read = GltdJsonReader.ReadFile(definitionPath);
    Report(definitionPath, read.Diagnostics, Console.Error);
    if (read.Definition is null)
    {
        return 1;
    }

    var trace = new TraceRecorder();
    GrayImage image;
    ImageMetadata metadata;
    using (var s0 = trace.Begin("S0.decode"))
    {
        try
        {
            (image, metadata) = ImageLoader.Load(imagePath);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            s0.Done(StageStatus.Failed, ex.Message);
            Console.Error.WriteLine($"measure: {ex.Message}");
            return 1;
        }

        string camera = metadata.IsCamera ? $", {metadata.CameraMake} {metadata.CameraModel} at {metadata.FocalLengthMm:0.0} mm" : "";
        string orientation = metadata.Orientation is { } turned and not 1 ? $", EXIF orientation {turned} not applied" : "";
        s0.Done(StageStatus.Ok, $"{metadata.Format} {image.Width}x{image.Height}{camera}{orientation}");
    }

    var result = SheetMeasurer.Measure(image, metadata, read.Definition, options, new OpenCvSharpBackend(), trace);
    foreach (var record in result.Trace)
    {
        Console.Write(TraceConsole.Format(record, verbosity));
    }

    if (json is not null)
    {
        File.WriteAllText(json, MeasurementJson.Serialize(imagePath, definitionPath, metadata, result));
    }

    if (result.Failure is not null)
    {
        Console.Error.WriteLine($"measure: {result.Failure}");
        return 1;
    }

    Console.WriteLine();
    Console.WriteLine("bull    declared x, y (in)    dx (in)     dy (in)   error (in)");
    foreach (var b in result.Bulls)
    {
        Console.WriteLine(b.Recovered is null
            ? $"{b.Name,-6}  {b.Declared.X / 254,7:0.000}, {b.Declared.Y / 254,7:0.000}   not located: {b.Failure}"
            : $"{b.Name,-6}  {b.Declared.X / 254,7:0.000}, {b.Declared.Y / 254,7:0.000}   {b.Dx / 254,9:+0.00000;-0.00000}   {b.Dy / 254,9:+0.00000;-0.00000}   {b.Error / 254,8:0.00000}");
    }

    Console.WriteLine($"worst {result.WorstError / 254:0.00000} in at bull {result.WorstBull?.Name}, mean {result.MeanError / 254:0.00000} in");
    return 0;
}

// PHASE0-SPIKE-BRIEF.md sections 2, 6 and 7 over the committed sample set, each as a Markdown table.
static int Spike(string measurement, string scans, string targets) => measurement switch
{
    "sheets" => Phase0Spike.Sheets(scans, targets, Console.Out),
    "photos" => Phase0Measurements.Photos(scans, targets, Console.Out),
    "markers" => Phase0Measurements.MarkerCount(scans, targets, Console.Out),
    "refinement" => Phase0Measurements.Refinement(scans, targets, Console.Out),
    "threshold" => Phase0Measurements.Threshold(scans, targets, Console.Out),
    "scale" => Phase0Measurements.Scale(scans, targets, Console.Out),
    "field" => Phase0Measurements.Field(scans, targets, Console.Out),
    "detectors" => DetectorComparison.Run(scans, targets, Console.Out),
    _ => Usage(),
};

static int Validate(string[] files)
{
    int failed = 0;
    foreach (string file in files)
    {
        var result = GltdJsonReader.ReadFile(file);
        var diagnostics = result.Definition is null
            ? result.Diagnostics
            : [.. result.Diagnostics, .. GltdValidator.Validate(result.Definition)];
        Report(file, diagnostics);
        if (diagnostics.Any(d => d.Severity == Severity.Error))
        {
            failed++;
        }
        else
        {
            Console.WriteLine($"{file}: ok");
        }
    }

    return failed == 0 ? 0 : 1;
}

static int Canonical(string input, string? output)
{
    var result = GltdJsonReader.ReadFile(input);
    Report(input, result.Diagnostics, Console.Error);
    if (result.Definition is null)
    {
        return 1;
    }

    Emit(CanonicalJsonWriter.Write(result.Definition), output);
    return 0;
}

static int Encode(string input)
{
    var read = GltdJsonReader.ReadFile(input);
    Report(input, read.Diagnostics);
    if (read.Definition is null)
    {
        return 1;
    }

    var encoded = GltdBinary.Encode(read.Definition);
    Report(input, encoded.Diagnostics);
    if (encoded.Encoding is not { } e)
    {
        return 1;
    }

    byte[] frame = GltdBinary.ReplicatedFrame(e);
    Console.WriteLine($"id     {e.DefinitionId}");
    Console.WriteLine($"body   {e.Body.Length} bytes  {Convert.ToHexStringLower(e.Body)}");
    Console.WriteLine($"frame  {frame.Length} bytes  {Convert.ToHexStringLower(frame)}");
    return 0;
}

static int Decode(string[] hexFrames)
{
    byte[][] frames;
    try
    {
        frames = [.. hexFrames.Select(Convert.FromHexString)];
    }
    catch (FormatException ex)
    {
        Console.Error.WriteLine($"decode: {ex.Message}");
        return 2;
    }

    var result = GltdBinary.Decode(frames);
    Report("decode", result.Diagnostics, Console.Error);
    if (result.Definition is null)
    {
        return 1;
    }

    Console.Error.WriteLine($"id {result.DefinitionId}, tile {result.TileIndex}");
    Emit(CanonicalJsonWriter.Write(result.Definition), null);
    return 0;
}

static int LibraryBuild(string layouts, string directory)
{
    Directory.CreateDirectory(directory);
    foreach (var target in LibraryBuilder.Build(layouts))
    {
        File.WriteAllBytes(Path.Combine(directory, target.FileName), CanonicalJsonWriter.Write(target.Definition));
        Console.WriteLine($"{target.Definition.Id}  {target.FileName}");
    }

    return 0;
}

static int LibraryVerify(string layouts, string directory)
{
    int errors = 0, warnings = 0, stale = 0;
    var targets = LibraryBuilder.Build(layouts);
    foreach (var target in targets)
    {
        string path = Path.Combine(directory, target.FileName);
        byte[] expected = CanonicalJsonWriter.Write(target.Definition);
        if (!File.Exists(path) || !File.ReadAllBytes(path).AsSpan().SequenceEqual(expected))
        {
            Console.WriteLine($"{target.FileName}: stale or missing; run grouplab library build");
            stale++;
            continue;
        }

        var read = GltdJsonReader.ReadFile(path);
        var diagnostics = read.Definition is null ? read.Diagnostics : [.. read.Diagnostics, .. GltdValidator.Validate(read.Definition)];
        var encoded = read.Definition is null ? null : GltdBinary.Encode(read.Definition).Encoding;
        var decoded = encoded is null ? null : GltdBinary.Decode([GltdBinary.ReplicatedFrame(encoded)]);
        bool roundTrips = decoded?.Definition is { } projection
            && GltdBinary.Encode(projection).Encoding?.Body.AsSpan().SequenceEqual(encoded!.Body) == true
            && encoded.DefinitionId == target.Definition.Id;

        Report(target.FileName, diagnostics);
        errors += diagnostics.Count(d => d.Severity == Severity.Error) + (roundTrips ? 0 : 1);
        warnings += diagnostics.Count(d => d.Severity == Severity.Warning);
        Console.WriteLine($"{target.Definition.Id}  {target.FileName}  {(roundTrips ? "round trip ok" : "ROUND TRIP FAILED")}");
    }

    Console.WriteLine($"{targets.Count} definitions: {errors} errors, {warnings} warnings, {stale} stale");
    return errors == 0 && stale == 0 ? 0 : 1;
}

static void Report(string source, IEnumerable<Diagnostic> diagnostics, TextWriter? writer = null)
{
    foreach (var diagnostic in diagnostics)
    {
        (writer ?? Console.Out).WriteLine($"{source}: {diagnostic}");
    }
}

static void Emit(byte[] bytes, string? output)
{
    if (output is null)
    {
        using var stdout = Console.OpenStandardOutput();
        stdout.Write(bytes);
    }
    else
    {
        File.WriteAllBytes(output, bytes);
    }
}


static int Usage()
{
    Console.Error.WriteLine("""
        grouplab validate <file.gltd.json>...
        grouplab canonical <file.gltd.json> [-o <output>]
        grouplab encode <file.gltd.json>
        grouplab decode <frame-hex>...
        grouplab library build <layouts.json> <targets-directory>
        grouplab library verify <layouts.json> <targets-directory>
        grouplab render <file.gltd.json> [-o <out.pdf>] [--filled] [--tile <n>] [--scale <s>] [--allow-invalid]
        grouplab selftest [<targets-directory>]
        grouplab intake <submission-directory> <public-directory> [--accept <file>]... [--submissions <directory>]
        grouplab scrub <input-image> <output-image>
        grouplab publish-owner <source-directory> <public-directory> --taken-by <name> --statement <text> [--hold <file> <reason>]...
        grouplab analyze <image> [--target <file.gltd.json>] [--library <directory>]... [--calibre <calibre>] [-v 1|2|3] [--json <marking.json>]
        grouplab corpus counts [--local <manifest.json>] [--write]
        grouplab holes ink-proximity [--local <manifest.json>] [-v]
        grouplab holes split-calibration [--local <manifest.json>]
        grouplab identify sweep
        grouplab spike stability
        grouplab spike corners --export <file.json> | --replay <file.json>
        grouplab measure <image> <file.gltd.json> [--tile <n>] [--dpi <d>] [--locator centroid|edge] [--model auto|homography|radial|surface]
                         [--mask <dmm>] [--refine none|subpix|contour] [--refine-window <modules>] [--threshold-window <px>]
                         [--downsample <f>] [--json <out.json>] [-v 1|2|3]
        grouplab spike sheets|photos|markers|refinement|threshold|scale|field|detectors [<scans-directory> <definitions-directory>]
        grouplab sweep module <base.gltd.json> <module-sweep-layouts.json> <output-directory>
        grouplab surface synthetic|rendered|frames [--joint]|lens-sweep|lens|noise|correlation|general-sweep|general
        grouplab holes baseline|synthetic [--realism|--held-out]
        grouplab stats range-table [from to [replications]]|coverage
        grouplab mounted pair
        """);
    return 2;
}

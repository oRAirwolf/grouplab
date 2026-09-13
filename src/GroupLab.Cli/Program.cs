using System.Globalization;
using GroupLab.Cli.Imaging;
using GroupLab.Cli.Library;
using GroupLab.Core.Gltd;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Validation;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;

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
        """);
    return 2;
}

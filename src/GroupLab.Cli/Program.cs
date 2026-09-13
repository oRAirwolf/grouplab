using GroupLab.Cli.Library;
using GroupLab.Core.Gltd;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Validation;

return args switch
{
    ["validate", .. var files] when files.Length > 0 => Validate(files),
    ["canonical", var input] => Canonical(input, null),
    ["canonical", var input, "-o", var output] => Canonical(input, output),
    ["encode", var input] => Encode(input),
    ["decode", .. var frames] when frames.Length > 0 => Decode(frames),
    ["library", "build", var layouts, var directory] => LibraryBuild(layouts, directory),
    ["library", "verify", var layouts, var directory] => LibraryVerify(layouts, directory),
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
        """);
    return 2;
}

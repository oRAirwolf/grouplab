using GroupLab.Core.Gltd;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Json;

return args switch
{
    ["validate", .. var files] when files.Length > 0 => Validate(files),
    ["canonical", var input] => Canonical(input, null),
    ["canonical", var input, "-o", var output] => Canonical(input, output),
    ["encode", var input] => Encode(input),
    ["decode", .. var frames] when frames.Length > 0 => Decode(frames),
    _ => Usage(),
};

static int Validate(string[] files)
{
    int failed = 0;
    foreach (string file in files)
    {
        var result = GltdJsonReader.ReadFile(file);
        Report(file, result.Diagnostics);
        if (result.Definition is null)
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

        Further commands arrive milestone by milestone during Phase 0a.
        """);
    return 2;
}

using GroupLab.Core.Gltd;
using GroupLab.Core.Gltd.Json;

return args switch
{
    ["validate", .. var files] when files.Length > 0 => Validate(files),
    ["canonical", var input] => Canonical(input, null),
    ["canonical", var input, "-o", var output] => Canonical(input, output),
    _ => Usage(),
};

static int Validate(string[] files)
{
    int failed = 0;
    foreach (string file in files)
    {
        var result = GltdJsonReader.ReadFile(file);
        foreach (var diagnostic in result.Diagnostics)
        {
            Console.WriteLine($"{file}: {diagnostic}");
        }

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
    foreach (var diagnostic in result.Diagnostics)
    {
        Console.Error.WriteLine($"{input}: {diagnostic}");
    }

    if (result.Definition is null)
    {
        return 1;
    }

    byte[] bytes = CanonicalJsonWriter.Write(result.Definition);
    if (output is null)
    {
        using var stdout = Console.OpenStandardOutput();
        stdout.Write(bytes);
    }
    else
    {
        File.WriteAllBytes(output, bytes);
    }

    return 0;
}

static int Usage()
{
    Console.Error.WriteLine("""
        grouplab validate <file.gltd.json>...
        grouplab canonical <file.gltd.json> [-o <output>]

        Further commands arrive milestone by milestone during Phase 0a.
        """);
    return 2;
}

using System.Text.Json.Nodes;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Rendering;
using GroupLab.Core.Reporting;

namespace GroupLab.Cli;

/// <summary>
/// <c>grouplab donor-pack</c>, NOTES-FROM-PLANNING.md entry 264: the donor pack's PDFs made from the target library, one per sheet that
/// <c>website/donor/sheets.json</c> names, each the sheet and its page of instructions (<see cref="VolunteerPack"/>). Run it after a sheet
/// is redrawn and commit what it writes; DonorPackTests fails until it has been.
/// </summary>
public static class DonorPackVerb
{
    public const string Usage = "grouplab donor-pack [<repository>]";

    /// <summary>Every library file the pack names, Letter and A4.</summary>
    public static IReadOnlyList<string> Sheets(string repository)
    {
        var list = JsonNode.Parse(File.ReadAllText(Path.Combine(repository, "website", "donor", "sheets.json")))!["sheets"]!.AsArray();
        return [.. list.SelectMany(s => new[] { (string?)s!["letter"], (string?)s["a4"] }).OfType<string>()];
    }

    /// <summary>One sheet's pack PDF, exactly as the library makes it.</summary>
    public static byte[] Pack(string repository, string file)
    {
        var definition = GltdJsonReader.ReadFile(Path.Combine(repository, "targets", file + ".gltd.json")).Definition
            ?? throw new InvalidOperationException($"targets/{file}.gltd.json does not read");
        return VolunteerPack.Write(definition, SceneBuilder.Build(definition).Pages);
    }

    public static int Run(string repository, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        foreach (string file in Sheets(repository))
        {
            byte[] pdf = Pack(repository, file);
            string target = Path.Combine(repository, "website", "donor", file + ".pdf");
            File.WriteAllBytes(target, pdf);
            output.WriteLine($"Wrote {target}, {pdf.Length / 1024} KB.");
        }

        return 0;
    }
}

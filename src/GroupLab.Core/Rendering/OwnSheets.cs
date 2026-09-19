using System.Text;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;

namespace GroupLab.Core.Rendering;

/// <summary>
/// The person's own sheets, NOTES-FROM-PLANNING.md entry 112 section 3: definitions from the parametric editor, kept as canonical GLTD-J
/// files in a folder of the application's data, beside the read-only built-in library. A sheet can be saved, renamed, duplicated as the
/// start of a new one, and deleted. The name is not part of the printed codes, so renaming a sheet leaves every copy already printed and
/// every session analysed against it reading as before.
/// </summary>
public sealed class OwnSheets(string folder)
{
    /// <summary>The family the print screen and the library list a person's sheets under.</summary>
    public const string Family = "Your own sheets";

    public string Folder { get; } = folder;

    /// <summary>Every readable sheet in the folder, by name.</summary>
    public IReadOnlyList<LibrarySheet> List()
    {
        if (!Directory.Exists(Folder))
        {
            return [];
        }

        return [.. Directory.EnumerateFiles(Folder, "*.gltd.json")
            .Select(path => GltdJsonReader.ReadFile(path).Definition is { } definition ? new LibrarySheet(Path.GetFileName(path), Family, null, definition) : null)
            .OfType<LibrarySheet>()
            .OrderBy(s => s.Definition.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(s => s.File, StringComparer.Ordinal)];
    }

    /// <summary>Saves a definition as a new sheet under its own name, made unique among the sheets already here.</summary>
    public LibrarySheet Save(TargetDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        Directory.CreateDirectory(Folder);
        var named = definition with { Name = UniqueName(definition.Name) };
        string file = UniqueFile(named.Name);
        Write(file, named);
        return new LibrarySheet(file, Family, null, named);
    }

    /// <summary>Gives a sheet a new name, kept in the same file; the name must be new among the person's sheets.</summary>
    public LibrarySheet Rename(LibrarySheet sheet, string name)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        string trimmed = (name ?? "").Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("A sheet needs a name.", nameof(name));
        }

        if (List().Any(s => s.File != sheet.File && string.Equals(s.Definition.Name, trimmed, StringComparison.CurrentCultureIgnoreCase)))
        {
            throw new ArgumentException($"There is already a sheet called {trimmed}.", nameof(name));
        }

        var renamed = sheet.Definition with { Name = trimmed };
        Write(sheet.File, renamed);
        return sheet with { Definition = renamed };
    }

    /// <summary>A copy of any sheet, built-in or the person's own, as a new sheet of their own named "... copy".</summary>
    public LibrarySheet Duplicate(LibrarySheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        return Save(sheet.Definition with { Name = sheet.Definition.Name + " copy" });
    }

    /// <summary>Deletes one of the person's sheets. Sessions keep their own copy of the definition, so none becomes unreadable.</summary>
    public void Delete(LibrarySheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        File.Delete(Path.Combine(Folder, sheet.File));
    }

    private void Write(string file, TargetDefinition definition) =>
        File.WriteAllBytes(Path.Combine(Folder, file), CanonicalJsonWriter.Write(definition));

    private string UniqueName(string name)
    {
        var taken = List().Select(s => s.Definition.Name).ToHashSet(StringComparer.CurrentCultureIgnoreCase);
        string candidate = name.Trim().Length == 0 ? "My sheet" : name.Trim();
        string stem = candidate;
        for (int n = 2; taken.Contains(candidate); n++)
        {
            candidate = $"{stem} {n}";
        }

        return candidate;
    }

    private string UniqueFile(string name)
    {
        var slug = new StringBuilder();
        foreach (char c in name.ToLowerInvariant())
        {
            slug.Append(char.IsAsciiLetterOrDigit(c) ? c : '-');
        }

        string stem = string.Join('-', slug.ToString().Split('-', StringSplitOptions.RemoveEmptyEntries));
        stem = stem.Length == 0 ? "sheet" : stem[..Math.Min(stem.Length, 60)];
        string file = stem + ".gltd.json";
        for (int n = 2; File.Exists(Path.Combine(Folder, file)); n++)
        {
            file = $"{stem}-{n}.gltd.json";
        }

        return file;
    }
}

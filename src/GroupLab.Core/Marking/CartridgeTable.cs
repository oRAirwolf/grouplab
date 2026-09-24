using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GroupLab.Core.Marking;

/// <summary>One cartridge in the table, and which of the table's sources confirm its bullet diameter.</summary>
public sealed record Cartridge(string Name, IReadOnlyList<string> Shorthand, IReadOnlyList<string> Sources);

/// <summary>A cartridge the table does not offer yet: only one source confirms it, or the two disagree.</summary>
public sealed record HeldCartridge(string Name, double Diameter, IReadOnlyList<string> Sources, string? Note);

/// <summary>Every cartridge that shares one bullet diameter, with the other families a person could mistake it for.</summary>
public sealed record CartridgeFamily(double Diameter, string Name, IReadOnlyList<string> Shorthand, IReadOnlyList<double> Traps, IReadOnlyList<Cartridge> Cartridges);

/// <summary>
/// Cartridge names, matched before numbers, NOTES-FROM-PLANNING.md entry 163 section 3.
/// <para>
/// A real user who shoots a 6.5 Creedmoor typed 6.5 and GroupLab offered .257, because 6.53 mm contains "6.5" and is the diameter nearest to
/// it. A person who knows they shoot a 6.5 Creedmoor does not necessarily know that it is a 0.264 in bullet, and matching a typed number
/// against diameters in millimetres picks the wrong bullet exactly when the name and the bullet disagree.
/// </para>
/// <para>
/// <b>This reverses entry 108, deliberately.</b> Entry 108 refused calibre designations and asked for a diameter, because reading ".38" as
/// 0.380 in would be wrong. A named table removes the ambiguity instead of refusing the input: ".38" is a name, and the name is 0.357 in.
/// </para>
/// <para>
/// Every cartridge offered is confirmed at its diameter by two independent sources, recorded in <c>cartridges.json</c> beside this file.
/// One the table has only one source for waits there, offered nowhere, until a second is found.
/// </para>
/// </summary>
public static partial class CartridgeTable
{
    private static readonly Lazy<(IReadOnlyList<CartridgeFamily> Families, IReadOnlyList<HeldCartridge> Held, IReadOnlyDictionary<string, string> Sources)> Loaded = new(Load);

    /// <summary>Every family, the common ones first, which is the order a suggestion list shows them in.</summary>
    public static IReadOnlyList<CartridgeFamily> Families => Loaded.Value.Families;

    /// <summary>Every cartridge held back, with the source it has, offered nowhere until a second agrees.</summary>
    public static IReadOnlyList<HeldCartridge> Held => Loaded.Value.Held;

    /// <summary>The sources, by the letter each cartridge names them with.</summary>
    public static IReadOnlyDictionary<string, string> Sources => Loaded.Value.Sources;

    private static (IReadOnlyList<CartridgeFamily>, IReadOnlyList<HeldCartridge>, IReadOnlyDictionary<string, string>) Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GroupLab.Core.Marking.cartridges.json")
            ?? throw new InvalidOperationException("the cartridge table is not embedded in GroupLab.Core");
        using var doc = JsonDocument.Parse(stream);
        static List<string> Strings(JsonElement e) => [.. e.EnumerateArray().Select(x => x.GetString()!)];
        var held = doc.RootElement.GetProperty("pending").EnumerateArray().Select(h => new HeldCartridge(
            h.GetProperty("name").GetString()!, h.GetProperty("diameter").GetDouble(), Strings(h.GetProperty("sources")),
            h.TryGetProperty("note", out var note) ? note.GetString() : null)).ToList();
        var sources = doc.RootElement.GetProperty("sources").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!);
        return (
        [
            .. doc.RootElement.GetProperty("families").EnumerateArray().Select(f => new CartridgeFamily(
                f.GetProperty("diameter").GetDouble(),
                f.GetProperty("name").GetString()!,
                Strings(f.GetProperty("shorthand")),
                [.. f.GetProperty("traps").EnumerateArray().Select(t => t.GetDouble())],
                [.. f.GetProperty("cartridges").EnumerateArray().Select(c => new Cartridge(
                    c.GetProperty("name").GetString()!, Strings(c.GetProperty("shorthand")), Strings(c.GetProperty("sources"))))])),
        ], held, sources);
    }

    /// <summary>Case, surrounding space, runs of space and a trailing "calibre" or "cal" do not change which cartridge was meant.</summary>
    private static string Key(string text) =>
        Spaces().Replace(text.Trim().TrimEnd('.').ToLowerInvariant(), " ").Replace(" caliber", "").Replace(" calibre", "").Replace(" cal", "");

    private static IEnumerable<string> Words(CartridgeFamily family) =>
        family.Shorthand.Concat(family.Cartridges.SelectMany(c => c.Shorthand.Prepend(c.Name))).Append(family.Name);

    /// <summary>The family a typed name means, exactly, or null. A name is matched before anything is read as a number.</summary>
    public static CartridgeFamily? Named(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        string key = Key(text);
        return Families.FirstOrDefault(f => Words(f).Any(w => Key(w) == key)) ?? FromSuggestion(text);
    }

    /// <summary>
    /// What to offer while somebody types, NOTES-FROM-PLANNING.md entry 163 section 3.1: families whose names start with what was typed, the
    /// common first, each with its cartridges and its diameter together, and under the first one the families it is easily mistaken for.
    /// Never a bare diameter.
    /// </summary>
    public static IReadOnlyList<string> Suggest(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [.. Families.Select(Describe)];
        }

        string key = Key(text);
        var exact = Families.Where(f => Words(f).Any(w => Key(w) == key));
        var starts = Families.Where(f => Words(f).Any(w => Key(w).StartsWith(key, StringComparison.Ordinal)));
        var within = key.Length >= 3 ? Families.Where(f => Words(f).Any(w => Key(w).Contains(key, StringComparison.Ordinal))) : [];
        var found = exact.Concat(starts).Concat(within).Distinct().ToList();
        if (found.Count == 0)
        {
            return [];
        }

        var lines = new List<string> { Describe(found[0]) };
        lines.AddRange(found[0].Traps.Select(Trap).Where(t => t is not null).Select(t => t!));
        lines.AddRange(found.Skip(1).Select(Describe));
        return lines;
    }

    /// <summary>"6.5 Creedmoor, 6.5x55 Swedish, .260 Remington and others: 0.264 in (6.71 mm)". The names and the diameter, together.</summary>
    public static string Describe(CartridgeFamily family)
    {
        ArgumentNullException.ThrowIfNull(family);
        var names = family.Cartridges.Select(c => c.Name).ToList();
        string listed = names.Count <= 3 ? string.Join(", ", names) : string.Join(", ", names.Take(3)) + " and others";
        return string.Create(CultureInfo.InvariantCulture, $"{listed}: {family.Diameter:0.000} in ({family.Diameter * 25.4:0.00} mm)");
    }

    /// <summary>"not the same as .25 calibre, 0.257 in (6.53 mm)": a family a person could pick by mistake, named as the mistake it is.</summary>
    public static string? Trap(double diameter)
    {
        var other = Families.FirstOrDefault(f => Math.Abs(f.Diameter - diameter) < 1e-9);
        return other is null
            ? null
            : string.Create(CultureInfo.InvariantCulture, $"not the same as {other.Name}, {other.Diameter:0.000} in ({other.Diameter * 25.4:0.00} mm)");
    }

    /// <summary>The family a suggestion line names, when the line itself is what was chosen. A trap line is not a choice and means nothing.</summary>
    public static CartridgeFamily? FromSuggestion(string? text)
    {
        if (text is null || text.StartsWith("not the same as", StringComparison.Ordinal) || Described().Match(text) is not { Success: true } m)
        {
            return null;
        }

        double inches = double.Parse(m.Groups["inches"].Value, CultureInfo.InvariantCulture);
        return Families.FirstOrDefault(f => Math.Abs(f.Diameter - inches) < 1e-9);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();

    [GeneratedRegex(@": (?<inches>0\.\d{3}) in \(\d+\.\d{2} mm\)$")]
    private static partial Regex Described();
}

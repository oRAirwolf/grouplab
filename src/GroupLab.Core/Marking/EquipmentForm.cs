using System.Collections.Immutable;
using System.Globalization;

namespace GroupLab.Core.Marking;

/// <summary>Which of the three lists on the Equipment screen a field belongs to.</summary>
public enum EquipmentKind
{
    Rifle,
    Barrel,
    Load,
}

/// <summary>What a field on the Equipment screen holds, so the form knows how to show it and the autocomplete knows whether to offer it.</summary>
public enum FieldKind
{
    /// <summary>Free text, which is what autocomplete is for.</summary>
    Words,

    /// <summary>A number, offered back with its unit so a person can see what they typed last time.</summary>
    Number,

    /// <summary>A choice from a fixed list, which needs no autocomplete because the list is already in front of them.</summary>
    Choice,

    /// <summary>A date.</summary>
    Date,

    /// <summary>Several lines of free text.</summary>
    Lines,
}

/// <summary>
/// One field of one record, NOTES-FROM-PLANNING.md entry 131 section 7: its name in the file, what the form calls it, what it holds, and how
/// to read it off a record. The reading function is what makes the autocomplete of section 7.5 possible without a second list of field names
/// to keep in step.
/// </summary>
/// <param name="Key">The field's name, used by the form, the autocomplete and the tests.</param>
/// <param name="Label">What the form calls it.</param>
/// <param name="Kind">What it holds.</param>
/// <param name="Unit">The unit shown after the box, for a number, or null.</param>
/// <param name="Required">True only for a name: entry 131 section 7.1 makes every other field optional.</param>
public sealed record EquipmentField(string Key, string Label, FieldKind Kind, string? Unit = null, bool Required = false);

/// <summary>
/// The Equipment screen, NOTES-FROM-PLANNING.md entry 131 section 7: rifles, barrels and loads, each with a form, every field optional except
/// a name, and autocomplete from earlier values everywhere.
/// <para>
/// <b>The field list is the form.</b> Three records with thirty-odd fields between them is where a screen and a file quietly stop agreeing:
/// a field gets added to the record and not the form, or to the form and not the save. Listing them once, with a function that reads each one
/// off a record, means the form is generated, the autocomplete reads the same list, and a test can require that every field of every record
/// appears on its form. It is the same arrangement the glossary uses, for the same reason.
/// </para>
/// </summary>
public static class EquipmentForm
{
    /// <summary>The rifle's form, entry 131 section 7.2, in the order it is shown.</summary>
    public static ImmutableList<EquipmentField> Rifle { get; } =
    [
        new("name", "Name", FieldKind.Words, Required: true),
        new("firearm", "Rifle or pistol", FieldKind.Choice),
        new("manufacturer", "Manufacturer", FieldKind.Words),
        new("cartridge", "Cartridge", FieldKind.Words),
        new("barrelLengthInches", "Barrel length", FieldKind.Number, "in"),
        new("twistInches", "Twist", FieldKind.Number, "in a turn"),
        new("twistDirection", "Twist direction", FieldKind.Choice),
        new("scope", "Scope", FieldKind.Words),
        new("clickUnit", "Scope units", FieldKind.Choice),
        new("clickValue", "Click value", FieldKind.Number),
        new("sightHeightInches", "Sight height", FieldKind.Number, "in"),
        new("zeroDistanceYards", "Zeroed at", FieldKind.Number, "yd"),
        new("stock", "Chassis or stock", FieldKind.Words),
        new("notes", "Notes", FieldKind.Lines),
    ];

    /// <summary>The barrel's form, entry 131 section 7.3.</summary>
    public static ImmutableList<EquipmentField> Barrel { get; } =
    [
        new("name", "Name", FieldKind.Words, Required: true),
        new("rifle", "Rifle", FieldKind.Choice),
        new("lengthInches", "Length", FieldKind.Number, "in"),
        new("twistInches", "Twist", FieldKind.Number, "in a turn"),
        new("twistDirection", "Twist direction", FieldKind.Choice),
        new("rounds", "Rounds fired", FieldKind.Number),
        new("installed", "Installed", FieldKind.Date),
        new("notes", "Notes", FieldKind.Lines),
    ];

    /// <summary>The load's form, entry 131 section 7.4.</summary>
    public static ImmutableList<EquipmentField> Load { get; } =
    [
        new("name", "Name", FieldKind.Words, Required: true),
        new("bulletDiameterInches", "Bullet caliber", FieldKind.Number, "in"),
        new("bulletWeightGrains", "Bullet weight", FieldKind.Number, "gr"),
        new("bulletName", "Bullet", FieldKind.Words),
        new("bulletLengthInches", "Bullet length", FieldKind.Number, "in"),
        new("ballisticCoefficient", "Ballistic coefficient", FieldKind.Number),
        new("dragModel", "Drag model", FieldKind.Choice),
        new("brassManufacturer", "Brass", FieldKind.Words),
        new("brassCartridge", "Brass cartridge", FieldKind.Words),
        new("powder", "Powder", FieldKind.Words),
        new("powderChargeGrains", "Charge", FieldKind.Number, "gr"),
        new("primer", "Primer", FieldKind.Words),
        new("overallLengthInches", "Cartridge length", FieldKind.Number, "in"),
        new("baseToOgiveInches", "Base to ogive", FieldKind.Number, "in"),
        new("muzzleVelocityFps", "Velocity", FieldKind.Number, "fps"),
        new("muzzleVelocitySdFps", "Velocity SD", FieldKind.Number, "fps"),
        new("components", "Components", FieldKind.Lines),
        new("notes", "Notes", FieldKind.Lines),
    ];

    /// <summary>The form for one kind of record.</summary>
    public static ImmutableList<EquipmentField> For(EquipmentKind kind) => kind switch
    {
        EquipmentKind.Rifle => Rifle,
        EquipmentKind.Barrel => Barrel,
        _ => Load,
    };

    /// <summary>
    /// What earlier records put in this field, offered back as a person types, most used first
    /// (NOTES-FROM-PLANNING.md entry 131 section 7.5).
    /// <para>
    /// A match is anywhere in the value and not just at the start, because somebody who has typed "Lapua 6.5 Creedmoor" wants it back after
    /// typing "creed". Ties are broken alphabetically so the list does not reorder itself between two values used the same number of times.
    /// </para>
    /// </summary>
    /// <param name="book">The records to read earlier values from.</param>
    /// <param name="kind">Which list is being edited.</param>
    /// <param name="fieldKey">The field's <see cref="EquipmentField.Key"/>.</param>
    /// <param name="typed">What has been typed so far; null or blank offers the most used values.</param>
    /// <param name="most">How many to offer.</param>
    public static ImmutableList<string> Suggestions(RecordBook book, EquipmentKind kind, string fieldKey, string? typed, int most = 8)
    {
        ArgumentNullException.ThrowIfNull(book);
        var field = For(kind).FirstOrDefault(f => string.Equals(f.Key, fieldKey, StringComparison.Ordinal));

        // A name is what tells two records apart, and a fixed list is already in front of the person: offering either back would be noise.
        if (field is null || field.Key == "name" || field.Kind == FieldKind.Choice)
        {
            return [];
        }

        string what = typed?.Trim() ?? "";
        return
        [
            .. Values(book, kind, fieldKey)
                .Where(v => what.Length == 0 || v.Contains(what, StringComparison.OrdinalIgnoreCase))
                .GroupBy(v => v, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .Take(most),
        ];
    }

    /// <summary>Every value earlier records hold in one field, in the order they were recorded, blanks left out.</summary>
    private static IEnumerable<string> Values(RecordBook book, EquipmentKind kind, string key) => kind switch
    {
        EquipmentKind.Rifle => book.Rifles.Select(r => OfRifle(r, key)).OfType<string>(),
        EquipmentKind.Barrel => book.Barrels.Select(b => OfBarrel(b, key)).OfType<string>(),
        _ => book.Loads.Select(l => OfLoad(l, key)).OfType<string>(),
    };

    private static string? Text(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Text(double? value) => value is { } v ? v.ToString("0.####", CultureInfo.InvariantCulture) : null;

    /// <summary>One field of a rifle as text, or null where it is not set.</summary>
    public static string? OfRifle(Rifle rifle, string key)
    {
        ArgumentNullException.ThrowIfNull(rifle);
        return key switch
        {
            "name" => Text(rifle.Name),
            "manufacturer" => Text(rifle.Manufacturer),
            "cartridge" => Text(rifle.Cartridge),
            "barrelLengthInches" => Text(rifle.BarrelLengthInches),
            "twistInches" => Text(rifle.TwistInches),
            "firearm" => rifle.Firearm.ToString(),
            "twistDirection" => rifle.TwistDirection is { } d ? (d < 0 ? "left" : "right") : null,
            "scope" => Text(rifle.Scope),
            "clickUnit" => rifle.ClickUnit.ToString(),
            "clickValue" => Text(rifle.ClickValue),
            "sightHeightInches" => Text(rifle.SightHeightInches),
            "zeroDistanceYards" => Text(rifle.ZeroDistanceYards),
            "stock" => Text(rifle.Stock),
            "notes" => Text(rifle.Notes),
            _ => null,
        };
    }

    /// <summary>One field of a barrel as text, or null where it is not set.</summary>
    public static string? OfBarrel(Barrel barrel, string key)
    {
        ArgumentNullException.ThrowIfNull(barrel);
        return key switch
        {
            "name" => Text(barrel.Name),
            "rifle" => Text(barrel.Rifle),
            "lengthInches" => Text(barrel.LengthInches),
            "twistInches" => Text(barrel.TwistInches),
            "twistDirection" => barrel.TwistDirection is { } d ? (d < 0 ? "left" : "right") : null,
            "rounds" => barrel.Rounds.ToString(CultureInfo.InvariantCulture),
            "installed" => barrel.Installed?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            "notes" => Text(barrel.Notes),
            _ => null,
        };
    }

    /// <summary>One field of a load as text, or null where it is not set.</summary>
    public static string? OfLoad(Load load, string key)
    {
        ArgumentNullException.ThrowIfNull(load);
        return key switch
        {
            "name" => Text(load.Name),
            "bulletDiameterInches" => Text(load.BulletDiameterInches),
            "bulletWeightGrains" => Text(load.BulletWeightGrains),
            "bulletName" => Text(load.BulletName),
            "bulletLengthInches" => Text(load.BulletLengthInches),
            "ballisticCoefficient" => Text(load.BallisticCoefficient),
            "dragModel" => load.DragModel?.ToString(),
            "brassManufacturer" => Text(load.BrassManufacturer),
            "brassCartridge" => Text(load.BrassCartridge),
            "powder" => Text(load.Powder),
            "powderChargeGrains" => Text(load.PowderChargeGrains),
            "primer" => Text(load.Primer),
            "overallLengthInches" => Text(load.OverallLengthInches),
            "baseToOgiveInches" => Text(load.BaseToOgiveInches),
            "muzzleVelocityFps" => Text(load.MuzzleVelocityFps),
            "muzzleVelocitySdFps" => Text(load.MuzzleVelocitySdFps),
            "components" => Text(load.Components),
            "notes" => Text(load.Notes),
            _ => null,
        };
    }

    /// <summary>
    /// What is wrong with a name, or null where nothing is. Entry 131 section 7.1 makes the name the one required field, and a name already
    /// in the list is refused rather than silently replacing the record behind it, which is what <see cref="RecordBook.With(Rifle)"/> would
    /// otherwise do to somebody who typed a name they had forgotten using.
    /// </summary>
    public static string? WhyNotSaveable(RecordBook book, EquipmentKind kind, string? name, string? replacing = null)
    {
        ArgumentNullException.ThrowIfNull(book);
        if (string.IsNullOrWhiteSpace(name))
        {
            return "A name is the one thing this needs, because it is what everything else refers to it by.";
        }

        string trimmed = name.Trim();
        bool taken = kind switch
        {
            EquipmentKind.Rifle => book.Rifles.Any(r => Same(r.Name, trimmed)),
            EquipmentKind.Barrel => book.Barrels.Any(b => Same(b.Name, trimmed)),
            _ => book.Loads.Any(l => Same(l.Name, trimmed)),
        };

        return taken && !Same(replacing, trimmed)
            ? $"There is already a {kind.ToString().ToLowerInvariant()} called \"{trimmed}\". Give this one a different name, or open that one and change it."
            : null;
    }

    private static bool Same(string? a, string? b) => string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
}

using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Marking;

/// <summary>
/// A rifle, NOTES-FROM-PLANNING.md entry 97 section 2: a name and what one click of its scope moves the point of impact, which is the whole
/// reason the record exists. Entry 91's zero correction stopped at MOA and mil because a click value is a property of the scope, and a
/// shooter dials clicks. <see cref="ClickUnit"/> is the scope's own unit, MOA or milliradians, so "a quarter MOA a click" is
/// <c>new Rifle("Tikka", 0.25, AngularUnit.Moa)</c>.
/// </summary>
public sealed record Rifle(string Name, double ClickValue, AngularUnit ClickUnit)
{
    // What the ballistic solver needs from the rifle, NOTES-FROM-PLANNING.md entry 112 section 4, all optional: a rifle without them simply
    // cannot use the solver, and the screen says which is missing.

    /// <summary>
    /// Whether this is a rifle or a pistol, which decides the list a calibre is guessed from. It defaults to a rifle, because every record
    /// made before this field existed is one, and because most of them will go on being one.
    /// </summary>
    public FirearmType Firearm { get; init; } = FirearmType.Rifle;

    /// <summary>The height of the sight's axis above the bore, in inches.</summary>
    public double? SightHeightInches { get; init; }

    /// <summary>The distance the rifle is zeroed at, in yards.</summary>
    public double? ZeroDistanceYards { get; init; }

    /// <summary>The barrel's twist, inches per turn, for spin drift.</summary>
    public double? TwistInches { get; init; }

    /// <summary>1 for a right-hand twist, −1 for a left-hand one.</summary>
    public int? TwistDirection { get; init; }

    // NOTES-FROM-PLANNING.md entry 131 section 7.2: what a person would write on a card beside the rifle. Every one is optional, because a
    // rifle with only a name and a click value is still enough to read a zero correction from, and a form that demands more before it will
    // save anything is a form people work around.

    public string? Manufacturer { get; init; }

    public string? Cartridge { get; init; }

    public double? BarrelLengthInches { get; init; }

    public string? Scope { get; init; }

    public string? Stock { get; init; }

    public string? Notes { get; init; }

    /// <summary>
    /// The barrel on this rifle now, by name, or null where none is marked (NOTES-FROM-PLANNING.md entry 407 section 5). Its length, twist and
    /// twist direction are copied onto <see cref="BarrelLengthInches"/>, <see cref="TwistInches"/> and <see cref="TwistDirection"/>, which is
    /// what the ballistic solver reads, so the solver needs no change and always has the barrel that is on.
    /// </summary>
    public string? BarrelOn { get; init; }

    /// <summary>
    /// The zeros this rifle had before, newest first, NOTES-FROM-PLANNING.md entry 455: a zero kept from the point blank view moves
    /// <see cref="ZeroDistanceYards"/>, and the one it replaced is kept here so a tap puts it back.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public IReadOnlyList<PreviousZero> PreviousZeros => PreviousZero.FromJson(PreviousZerosJson);

    /// <summary>
    /// <see cref="PreviousZeros"/> as the database and the export keep it. Held as text so two rifles with the same history are equal, as
    /// a record's other fields are; a list would compare by reference and make every rifle with a history look changed on import.
    /// </summary>
    public string? PreviousZerosJson { get; init; }

    // NOTES-FROM-PLANNING.md entry 456: the scope and mount, all optional. Travel and cant are in the scope's own unit, ClickUnit.

    /// <summary>The scope's elevation travel, in the scope unit: the whole of it, or the most up from the zero (<see cref="ScopeTravelKind"/>).</summary>
    public double? ScopeTravel { get; init; }

    /// <summary>What <see cref="ScopeTravel"/> is; null reads as the whole travel.</summary>
    public GroupLab.Core.Ballistics.ScopeTravelKind? ScopeTravelKind { get; init; }

    /// <summary>The rail's cant, in the scope unit: the elevation built into the mount, such as a 20 MOA rail.</summary>
    public double? RailCant { get; init; }

    /// <summary>The sight scale factor from a tracking test, 1 where the scope tracks true; every click GroupLab gives is divided by it.</summary>
    public double? SightScaleFactor { get; init; }

    /// <summary>The rifle's weight as fired, with its optic, loaded magazine and bipod, in pounds, for Top Gun.</summary>
    public double? WeightAsFiredLb { get; init; }

    /// <summary>This rifle zeroed at another distance, the zero it had kept as the newest previous one.</summary>
    public Rifle WithZero(double yards, string why, DateTimeOffset when) => this with
    {
        ZeroDistanceYards = yards,
        PreviousZerosJson = PreviousZero.ToJson([new PreviousZero(ZeroDistanceYards, when.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture), why), .. PreviousZeros]),
    };

    /// <summary>This rifle with its newest previous zero put back, or as it is where it has none.</summary>
    public Rifle UndoZero()
    {
        var zeros = PreviousZeros;
        return zeros.Count == 0 ? this : this with { ZeroDistanceYards = zeros[0].ZeroDistanceYards, PreviousZerosJson = PreviousZero.ToJson([.. zeros.Skip(1)]) };
    }

    /// <summary>
    /// How the click reads to a person: "0.25 MOA a click", "0.1 mil a click", in the unit's own symbol. A NATO mil scope was called MOA here
    /// while the residual beside it said NATO mil (found 2026-10-08 on Alan's 6 ARC session, docs/PROOF-CHECKLIST.md row 16).
    /// </summary>
    public string DescribeClick() => string.Create(CultureInfo.InvariantCulture, $"{ClickValue:0.###} {UnitSettings.Symbol(ClickUnit)} a click");
}

/// <summary>A zero a rifle had before: its distance (none where it had none), when it was replaced, and why, in words.</summary>
public sealed record PreviousZero(double? ZeroDistanceYards, string ReplacedUtc, string Why)
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>A rifle's previous zeros as the database keeps them, or null where it has none.</summary>
    public static string? ToJson(IReadOnlyList<PreviousZero> zeros) => zeros.Count == 0 ? null : JsonSerializer.Serialize(zeros, Options);

    public static IReadOnlyList<PreviousZero> FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<PreviousZero>>(json, Options) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}

/// <summary>
/// The click values scopes are usually made with, entry 294 section 1, in the order a rifle's form and Shots Needed to Zero offer them on
/// both platforms; any other value is typed, in mil or MOA.
/// </summary>
public static class ScopeClicks
{
    public static IReadOnlyList<(string Words, double Value, AngularUnit Unit)> Common { get; } =
    [
        ("0.1 mil", 0.1, AngularUnit.Mrad), ("0.05 mil", 0.05, AngularUnit.Mrad), ("1/4 MOA", 0.25, AngularUnit.Moa), ("1/8 MOA", 0.125, AngularUnit.Moa),
    ];

    /// <summary>The click a new rifle starts with in a scope unit: a tenth of a mil, or a quarter of a minute.</summary>
    public static (double Value, AngularUnit Unit) Usual(AngularUnit scope) =>
        scope == AngularUnit.Mrad ? (0.1, AngularUnit.Mrad) : (0.25, scope == AngularUnit.Smoa ? AngularUnit.Smoa : AngularUnit.Moa);

    /// <summary>Where a click value sits in <see cref="Common"/>, or -1 for any other.</summary>
    public static int IndexOf(double value, AngularUnit unit)
    {
        for (int i = 0; i < Common.Count; i++)
        {
            if (Common[i].Unit == unit && Math.Abs(Common[i].Value - value) < 1e-9)
            {
                return i;
            }
        }

        return -1;
    }
}

/// <summary>A barrel: a name, the rifle it is on, and how many rounds it has fired, which is what a barrel's life is counted in.</summary>
public sealed record Barrel(string Name, string? Rifle, int Rounds)
{
    // NOTES-FROM-PLANNING.md entry 131 section 7.3, all optional. A rifle has several barrels over its life, which is why the round count
    // lives here and not on the rifle.

    public double? LengthInches { get; init; }

    public double? TwistInches { get; init; }

    /// <summary>1 for a right-hand twist, −1 for a left-hand one.</summary>
    public int? TwistDirection { get; init; }

    /// <summary>When the barrel went on, as a date with no time, because nobody records the hour they fitted a barrel.</summary>
    public DateOnly? Installed { get; init; }

    public string? Notes { get; init; }
}

/// <summary>
/// A load: a name and its components as free text, the data block's own fields. Deliberately not a reloading database (entry 97 section 2):
/// the record exists so a sheet can say which load it carried, and so subgroups can be named by the load rather than by a string typed twice.
/// </summary>
public sealed record Load(string Name, string? Components)
{
    // What the ballistic solver needs from the load, entry 112 section 4, all optional.

    public double? MuzzleVelocityFps { get; init; }

    /// <summary>The muzzle velocity's standard deviation, which hit probability at distance propagates (entry 113 section 3).</summary>
    public double? MuzzleVelocitySdFps { get; init; }

    /// <summary>
    /// Where <see cref="MuzzleVelocitySdFps"/> came from, in words, when GroupLab worked it out: "24 readings, 20 September 2026". Null where
    /// a person typed it (NOTES-FROM-PLANNING.md entry 115 section 3).
    /// </summary>
    public string? MuzzleVelocitySdFrom { get; init; }

    public double? BallisticCoefficient { get; init; }

    public GroupLab.Core.Ballistics.DragModel? DragModel { get; init; }

    /// <summary>The atmosphere the coefficient is stated against, ICAO unless it says otherwise.</summary>
    public GroupLab.Core.Ballistics.ReferenceAtmosphere? BcReference { get; init; }

    public double? BulletWeightGrains { get; init; }

    public double? BulletLengthInches { get; init; }

    public double? BulletDiameterInches { get; init; }

    /// <summary>
    /// The least velocity at which the bullet still opens, ft/s, as its maker publishes it, NOTES-FROM-PLANNING.md entry 455. The point
    /// blank view ends its distances where the bullet falls below it; blank, it ends nothing and says the maker publishes one.
    /// </summary>
    public double? MinExpansionFps { get; init; }

    // NOTES-FROM-PLANNING.md entry 131 section 7.4: the rest of what a reloader writes on the box. A load is kept apart from a rifle because
    // one load is shot in several rifles, and the fields below are the load's own, not any rifle's.

    public string? BulletName { get; init; }

    public string? BrassManufacturer { get; init; }

    public string? BrassCartridge { get; init; }

    public string? Powder { get; init; }

    public double? PowderChargeGrains { get; init; }

    /// <summary>Cartridge overall length, in inches.</summary>
    public double? OverallLengthInches { get; init; }

    /// <summary>Cartridge base to ogive, in inches, which is the measurement that actually repeats between lots of brass.</summary>
    public double? BaseToOgiveInches { get; init; }

    public string? Primer { get; init; }

    public string? Notes { get; init; }
}

/// <summary>
/// The person's rifles, barrels and loads, DESIGN.md section 3's records, kept in one small file beside the settings. Names are the keys, as
/// a person refers to them; a record added with a name already there replaces it.
/// </summary>
public sealed record RecordBook(ImmutableList<Rifle> Rifles, ImmutableList<Barrel> Barrels, ImmutableList<Load> Loads)
{
    public static RecordBook Empty { get; } = new([], [], []);

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public RecordBook With(Rifle rifle) => this with { Rifles = [.. Rifles.Where(r => !Same(r.Name, rifle.Name)), rifle] };

    public RecordBook With(Barrel barrel)
    {
        var next = this with { Barrels = [.. Barrels.Where(b => !Same(b.Name, barrel.Name)), barrel] };
        return barrel.Rifle is { } rifle && next.FindRifle(rifle) is { } on && Same(on.BarrelOn, barrel.Name) ? next.PutOn(rifle, barrel.Name) : next;
    }

    /// <summary>
    /// The name of the rifle that holds barrels whose rifle is not known, NOTES-FROM-PLANNING.md entry 407 section 5.5: made by the move to
    /// barrels under their rifle for any barrel it could not place, and the person moves each to its rifle.
    /// </summary>
    public const string NotYetOnARifle = "Barrels not yet on a rifle";

    /// <summary>The barrels on a rifle, the one on now first, then by name.</summary>
    public IReadOnlyList<Barrel> BarrelsOf(string? rifle)
    {
        var on = FindRifle(rifle)?.BarrelOn;
        return [.. Barrels.Where(b => Same(b.Rifle, rifle)).OrderBy(b => Same(b.Name, on) ? 0 : 1).ThenBy(b => b.Name, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>The barrel on a rifle now, or null.</summary>
    public Barrel? OnNow(string? rifle) => FindRifle(rifle)?.BarrelOn is { } on && FindBarrel(on) is { } barrel && Same(barrel.Rifle, rifle) ? barrel : null;

    /// <summary>
    /// Marks a barrel as the one on its rifle now, and copies its length and twist onto the rifle, where the ballistic solver reads them. A
    /// barrel with none of them leaves the rifle's own as they were.
    /// </summary>
    public RecordBook PutOn(string rifle, string barrel)
    {
        if (FindRifle(rifle) is not { } r || FindBarrel(barrel) is not { } b)
        {
            return this;
        }

        var on = r with
        {
            BarrelOn = b.Name,
            BarrelLengthInches = b.LengthInches ?? r.BarrelLengthInches,
            TwistInches = b.TwistInches ?? r.TwistInches,
            TwistDirection = b.TwistDirection ?? r.TwistDirection,
        };
        return this with { Rifles = [.. Rifles.Select(x => Same(x.Name, rifle) ? on : x)] };
    }

    /// <summary>
    /// What the setup's one list says for a rifle and a barrel together: "Test rifle with its 26 in Bartlein (on now)", or the rifle alone
    /// where it has no barrels. The length is in the record's inches, as the barrel's form takes it.
    /// </summary>
    public string Together(Rifle rifle, Barrel? barrel)
    {
        ArgumentNullException.ThrowIfNull(rifle);
        if (barrel is null)
        {
            return rifle.Name;
        }

        string length = barrel.LengthInches is { } inches ? inches.ToString("0.##", CultureInfo.InvariantCulture) + " in " : "";
        return $"{rifle.Name} with its {length}{barrel.Name}" + (Same(rifle.BarrelOn, barrel.Name) ? " (on now)" : "");
    }

    public RecordBook With(Load load) => this with { Loads = [.. Loads.Where(l => !Same(l.Name, load.Name)), load] };

    /// <summary>
    /// Error report 38, 2026-10-25: a rifle taken away, with the barrels on it. Sessions keep the rifle's name as they were saved with it.
    /// </summary>
    public RecordBook WithoutRifle(string name) =>
        this with { Rifles = [.. Rifles.Where(r => !Same(r.Name, name))], Barrels = [.. Barrels.Where(b => !Same(b.Rifle, name))] };

    /// <summary>A barrel taken away; a rifle it was on now has none on, and keeps the length and twist it had.</summary>
    public RecordBook WithoutBarrel(string name) =>
        this with { Barrels = [.. Barrels.Where(b => !Same(b.Name, name))], Rifles = [.. Rifles.Select(r => Same(r.BarrelOn, name) ? r with { BarrelOn = null } : r)] };

    /// <summary>A load taken away. Sessions keep the load's name as they were saved with it.</summary>
    public RecordBook WithoutLoad(string name) => this with { Loads = [.. Loads.Where(l => !Same(l.Name, name))] };

    public Rifle? FindRifle(string? name) => Rifles.FirstOrDefault(r => Same(r.Name, name));

    public Barrel? FindBarrel(string? name) => Barrels.FirstOrDefault(b => Same(b.Name, name));

    public Load? FindLoad(string? name) => Loads.FirstOrDefault(l => Same(l.Name, name));

    /// <summary>Adds a sheet's shots to a barrel's count. It is a person's step, not an automatic one, so reopening a marking never counts it twice.</summary>
    public RecordBook Fired(string barrel, int rounds) =>
        FindBarrel(barrel) is { } b ? With(b with { Rounds = b.Rounds + Math.Max(0, rounds) }) : this;

    /// <summary>
    /// Writes the book, every field of it.
    /// <para>
    /// <b>It did not, before NOTES-FROM-PLANNING.md entry 131 section 7.</b> This wrote a rifle's name, click value and click unit and
    /// nothing else, so the sight height, zero distance and twist a person typed into the ballistics page, and every one of a load's
    /// muzzle velocity, ballistic coefficient and bullet figures, were dropped the moment the book was saved and gone at the next start.
    /// Nothing failed and nothing said so: the page simply asked for them again. Every field is written now, and a test reads a book back
    /// and requires it to equal what was written, which is what would have caught it.
    /// </para>
    /// </summary>
    public string Write() => JsonSerializer.Serialize(new
    {
        rifles = Rifles.Select(r => new
        {
            r.Name,
            r.ClickValue,
            clickUnit = r.ClickUnit.ToString(),
            r.SightHeightInches,
            r.ZeroDistanceYards,
            r.TwistInches,
            r.TwistDirection,
            r.Manufacturer,
            r.Cartridge,
            r.BarrelLengthInches,
            r.Scope,
            r.Stock,
            r.Notes,
            r.BarrelOn,
            r.PreviousZerosJson,
            r.ScopeTravel,
            scopeTravelKind = r.ScopeTravelKind?.ToString(),
            r.RailCant,
            r.SightScaleFactor,
            r.WeightAsFiredLb,
        }),
        barrels = Barrels.Select(b => new
        {
            b.Name,
            b.Rifle,
            b.Rounds,
            b.LengthInches,
            b.TwistInches,
            b.TwistDirection,
            installed = b.Installed?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            b.Notes,
        }),
        loads = Loads.Select(l => new
        {
            l.Name,
            l.Components,
            l.MuzzleVelocityFps,
            l.MuzzleVelocitySdFps,
            l.MuzzleVelocitySdFrom,
            l.BallisticCoefficient,
            dragModel = l.DragModel?.ToString(),
            bcReference = l.BcReference?.ToString(),
            l.BulletWeightGrains,
            l.BulletLengthInches,
            l.BulletDiameterInches,
            l.MinExpansionFps,
            l.BulletName,
            l.BrassManufacturer,
            l.BrassCartridge,
            l.Powder,
            l.PowderChargeGrains,
            l.OverallLengthInches,
            l.BaseToOgiveInches,
            l.Primer,
            l.Notes,
        }),
    }, Options);

    /// <summary>Reads a record book, or returns an empty one for a file that is not one, so a damaged file loses the records and not the application.</summary>
    public static RecordBook Read(string? json)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json) || JsonNode.Parse(json) is not JsonObject root)
            {
                return Empty;
            }

            return new RecordBook(
                [.. (root["rifles"] as JsonArray ?? []).Select(r => new Rifle((string)r!["name"]!, (double)r["clickValue"]!, Enum.Parse<AngularUnit>((string)r["clickUnit"]!))
                {
                    SightHeightInches = (double?)r["sightHeightInches"],
                    ZeroDistanceYards = (double?)r["zeroDistanceYards"],
                    TwistInches = (double?)r["twistInches"],
                    TwistDirection = (int?)r["twistDirection"],
                    Manufacturer = (string?)r["manufacturer"],
                    Cartridge = (string?)r["cartridge"],
                    BarrelLengthInches = (double?)r["barrelLengthInches"],
                    Scope = (string?)r["scope"],
                    Stock = (string?)r["stock"],
                    Notes = (string?)r["notes"],
                    BarrelOn = (string?)r["barrelOn"],
                    PreviousZerosJson = (string?)r["previousZerosJson"],
                    ScopeTravel = (double?)r["scopeTravel"],
                    ScopeTravelKind = (string?)r["scopeTravelKind"] is { } kind && Enum.TryParse<GroupLab.Core.Ballistics.ScopeTravelKind>(kind, out var k) ? k : null,
                    RailCant = (double?)r["railCant"],
                    SightScaleFactor = (double?)r["sightScaleFactor"],
                    WeightAsFiredLb = (double?)r["weightAsFiredLb"],
                })],
                [.. (root["barrels"] as JsonArray ?? []).Select(b => new Barrel((string)b!["name"]!, (string?)b["rifle"], (int?)b["rounds"] ?? 0)
                {
                    LengthInches = (double?)b["lengthInches"],
                    TwistInches = (double?)b["twistInches"],
                    TwistDirection = (int?)b["twistDirection"],
                    Installed = (string?)b["installed"] is { } day && DateOnly.TryParse(day, CultureInfo.InvariantCulture, out var date) ? date : null,
                    Notes = (string?)b["notes"],
                })],
                [.. (root["loads"] as JsonArray ?? []).Select(l => new Load((string)l!["name"]!, (string?)l["components"])
                {
                    MuzzleVelocityFps = (double?)l["muzzleVelocityFps"],
                    MuzzleVelocitySdFps = (double?)l["muzzleVelocitySdFps"],
                    MuzzleVelocitySdFrom = (string?)l["muzzleVelocitySdFrom"],
                    BallisticCoefficient = (double?)l["ballisticCoefficient"],
                    DragModel = (string?)l["dragModel"] is { } drag ? Enum.Parse<GroupLab.Core.Ballistics.DragModel>(drag) : null,
                    BcReference = (string?)l["bcReference"] is { } air ? Enum.Parse<GroupLab.Core.Ballistics.ReferenceAtmosphere>(air) : null,
                    BulletWeightGrains = (double?)l["bulletWeightGrains"],
                    BulletLengthInches = (double?)l["bulletLengthInches"],
                    BulletDiameterInches = (double?)l["bulletDiameterInches"],
                    MinExpansionFps = (double?)l["minExpansionFps"],
                    BulletName = (string?)l["bulletName"],
                    BrassManufacturer = (string?)l["brassManufacturer"],
                    BrassCartridge = (string?)l["brassCartridge"],
                    Powder = (string?)l["powder"],
                    PowderChargeGrains = (double?)l["powderChargeGrains"],
                    OverallLengthInches = (double?)l["overallLengthInches"],
                    BaseToOgiveInches = (double?)l["baseToOgiveInches"],
                    Primer = (string?)l["primer"],
                    Notes = (string?)l["notes"],
                })]);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException or ArgumentException or NullReferenceException)
        {
            return Empty;
        }
    }

    private static bool Same(string? a, string? b) => string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// An offset in the scope's clicks, entry 97 section 2 and entry 53 section 2: the whole clicks to dial, which way, and what is left after
/// rounding, because a turret has no half positions and silently rounding is the kind of confident answer this project refuses.
/// </summary>
public sealed record Clicks(int Count, string Direction, double ResidualAngle, AngularUnit Unit, double ScaleFactor = 1)
{
    /// <summary>
    /// The clicks for an offset of <paramref name="offsetInches"/> at <paramref name="distanceInches"/>. The angle of an offset is the full
    /// angle to a point, atan(offset / distance), not the half-angle form docs/STATISTICS.md section 12.5 uses for a size across a group.
    /// </summary>
    public static Clicks For(double offsetInches, double distanceInches, Rifle rifle, string direction)
    {
        ArgumentNullException.ThrowIfNull(rifle);
        // Entry 456 section 2: the turret's reading is the true angle over the sight scale factor, so a scope that under-tracks is dialed further.
        double factor = rifle.SightScaleFactor is > 0 and var f ? f : 1;
        double angle = Angular.Constant(rifle.ClickUnit) / 2 * Math.Atan(Math.Abs(offsetInches) / distanceInches) / factor;
        int count = (int)Math.Round(angle / rifle.ClickValue, MidpointRounding.AwayFromZero);
        return new Clicks(count, direction, angle - (count * rifle.ClickValue), rifle.ClickUnit, factor);
    }

    /// <summary>"12 clicks right", in the turret's own words.</summary>
    public string Describe() => string.Create(CultureInfo.InvariantCulture, $"{Count} click{(Count == 1 ? "" : "s")} {Direction}");
}

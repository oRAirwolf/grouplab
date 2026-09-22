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

    /// <summary>How the click reads to a person: "0.25 MOA a click", "0.1 mil a click".</summary>
    public string DescribeClick() => string.Create(CultureInfo.InvariantCulture, $"{ClickValue:0.###} {(ClickUnit == AngularUnit.Mrad ? "mil" : ClickUnit == AngularUnit.Smoa ? "SMOA" : "MOA")} a click");
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

    public RecordBook With(Barrel barrel) => this with { Barrels = [.. Barrels.Where(b => !Same(b.Name, barrel.Name)), barrel] };

    public RecordBook With(Load load) => this with { Loads = [.. Loads.Where(l => !Same(l.Name, load.Name)), load] };

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
public sealed record Clicks(int Count, string Direction, double ResidualAngle, AngularUnit Unit)
{
    /// <summary>
    /// The clicks for an offset of <paramref name="offsetInches"/> at <paramref name="distanceInches"/>. The angle of an offset is the full
    /// angle to a point, atan(offset / distance), not the half-angle form docs/STATISTICS.md section 12.5 uses for a size across a group.
    /// </summary>
    public static Clicks For(double offsetInches, double distanceInches, Rifle rifle, string direction)
    {
        ArgumentNullException.ThrowIfNull(rifle);
        double angle = Angular.Constant(rifle.ClickUnit) / 2 * Math.Atan(Math.Abs(offsetInches) / distanceInches);
        int count = (int)Math.Round(angle / rifle.ClickValue, MidpointRounding.AwayFromZero);
        return new Clicks(count, direction, angle - (count * rifle.ClickValue), rifle.ClickUnit);
    }

    /// <summary>"12 clicks right", in the turret's own words.</summary>
    public string Describe() => string.Create(CultureInfo.InvariantCulture, $"{Count} click{(Count == 1 ? "" : "s")} {Direction}");
}

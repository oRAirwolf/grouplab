using GroupLab.Core.Ballistics;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 131 section 7: rifles, barrels and loads on their own screen, every field optional except a name, with
/// autocomplete from earlier values everywhere.
/// <para>
/// <b><see cref="EveryFieldOfEveryRecordSurvivesSavingAndReopening"/> is the one that earned its place.</b> Writing it found that the record
/// book had never saved a rifle's sight height, zero distance or twist, or any of a load's velocity, ballistic coefficient and bullet
/// figures: they were typed into the ballistics page, used, and dropped at the next start with nothing said.
/// </para>
/// </summary>
public class EquipmentFormTests
{
    private static Rifle FullRifle() => new("Tikka T3x", 0.1, AngularUnit.Mrad)
    {
        Manufacturer = "Tikka",
        Cartridge = "6.5 Creedmoor",
        BarrelLengthInches = 24,
        TwistInches = 8,
        TwistDirection = 1,
        Scope = "Vortex Razor",
        Stock = "MDT ACC",
        SightHeightInches = 1.85,
        ZeroDistanceYards = 100,
        Notes = "Bedded 2025.",
    };

    private static Barrel FullBarrel() => new("Bartlein #3", "Tikka T3x", 480)
    {
        LengthInches = 24,
        TwistInches = 8,
        TwistDirection = 1,
        Installed = new DateOnly(2025, 3, 14),
        Notes = "Threaded 5/8x24.",
    };

    private static Load FullLoad() => new("41.2 H4350", "41.2 gr H4350, 140 ELD-M")
    {
        MuzzleVelocityFps = 2712,
        MuzzleVelocitySdFps = 8.4,
        MuzzleVelocitySdFrom = "24 readings, 20 September 2026",
        BallisticCoefficient = 0.326,
        DragModel = DragModel.G7,
        BcReference = ReferenceAtmosphere.Icao,
        BulletWeightGrains = 140,
        BulletLengthInches = 1.35,
        BulletDiameterInches = 0.264,
        BulletName = "Hornady ELD-M",
        BrassManufacturer = "Lapua",
        BrassCartridge = "6.5 Creedmoor",
        Powder = "H4350",
        PowderChargeGrains = 41.2,
        OverallLengthInches = 2.825,
        BaseToOgiveInches = 2.211,
        Primer = "CCI BR-2",
        Notes = "Node held across 15 C.",
    };

    /// <summary>
    /// Everything a person typed comes back, because until tonight most of it did not. A rifle set up for the ballistics solver lost its
    /// sight height and zero distance on the way to disc, and the page asked for them again at the next start as though they had never
    /// been given.
    /// </summary>
    [Fact]
    public void EveryFieldOfEveryRecordSurvivesSavingAndReopening()
    {
        var book = RecordBook.Empty.With(FullRifle()).With(FullBarrel()).With(FullLoad());

        var read = RecordBook.Read(book.Write());

        Assert.Equal(FullRifle(), read.Rifles.Single());
        Assert.Equal(FullBarrel(), read.Barrels.Single());
        Assert.Equal(FullLoad(), read.Loads.Single());
    }

    /// <summary>A book written by the version before any of this reopens with the old fields intact and the new ones empty, entry 131 section 7.7.</summary>
    [Fact]
    public void ABookWrittenBeforeTheEquipmentScreenStillReads()
    {
        const string old = """
        {
          "rifles": [ { "name": "Tikka", "clickValue": 0.25, "clickUnit": "Moa" } ],
          "barrels": [ { "name": "Factory", "rifle": "Tikka", "rounds": 1200 } ],
          "loads": [ { "name": "Factory 140", "components": "Hornady Match" } ]
        }
        """;

        var book = RecordBook.Read(old);

        Assert.Equal("Tikka", book.Rifles.Single().Name);
        Assert.Equal(0.25, book.Rifles.Single().ClickValue);
        Assert.Equal(AngularUnit.Moa, book.Rifles.Single().ClickUnit);
        Assert.Null(book.Rifles.Single().Cartridge);
        Assert.Equal(1200, book.Barrels.Single().Rounds);
        Assert.Null(book.Barrels.Single().Installed);
        Assert.Equal("Hornady Match", book.Loads.Single().Components);
        Assert.Null(book.Loads.Single().Powder);
    }

    /// <summary>Every field the form shows can be read off its record, so no box on the screen is one nothing fills.</summary>
    [Fact]
    public void EveryFieldOnEveryFormCanBeReadOffItsRecord()
    {
        Assert.All(EquipmentForm.Rifle, f => Assert.NotNull(EquipmentForm.OfRifle(FullRifle(), f.Key)));
        Assert.All(EquipmentForm.Barrel, f => Assert.NotNull(EquipmentForm.OfBarrel(FullBarrel(), f.Key)));
        Assert.All(EquipmentForm.Load, f => Assert.NotNull(EquipmentForm.OfLoad(FullLoad(), f.Key)));
    }

    /// <summary>Every field has a label, and no key appears twice on one form.</summary>
    [Fact]
    public void EveryFormIsWellFormed()
    {
        foreach (var kind in new[] { EquipmentKind.Rifle, EquipmentKind.Barrel, EquipmentKind.Load })
        {
            var form = EquipmentForm.For(kind);
            Assert.All(form, f => Assert.False(string.IsNullOrWhiteSpace(f.Label), $"{kind}.{f.Key} has no label"));
            Assert.Equal(form.Count, form.Select(f => f.Key).Distinct(StringComparer.Ordinal).Count());

            // Section 7.1: every field optional except a name.
            Assert.Equal(["name"], form.Where(f => f.Required).Select(f => f.Key));
        }
    }

    /// <summary>Every number field says its unit, or a person cannot know whether a barrel is 24 of something or 610 of something else.</summary>
    [Fact]
    public void EveryMeasurementSaysItsUnit()
    {
        var unitless = new[] { "clickValue", "rounds", "ballisticCoefficient" };
        foreach (var kind in new[] { EquipmentKind.Rifle, EquipmentKind.Barrel, EquipmentKind.Load })
        {
            Assert.All(
                EquipmentForm.For(kind).Where(f => f.Kind == FieldKind.Number && !unitless.Contains(f.Key, StringComparer.Ordinal)),
                f => Assert.False(string.IsNullOrWhiteSpace(f.Unit), $"{kind}.{f.Key} is a measurement with no unit beside it"));
        }
    }

    /// <summary>Entry 131 section 7.5: earlier values come back, most used first.</summary>
    [Fact]
    public void EarlierValuesAreOfferedMostUsedFirst()
    {
        var book = RecordBook.Empty
            .With(new Load("A", null) { Powder = "H4350" })
            .With(new Load("B", null) { Powder = "H4350" })
            .With(new Load("C", null) { Powder = "Varget" })
            .With(new Load("D", null) { Powder = "RL16" });

        Assert.Equal(["H4350", "RL16", "Varget"], EquipmentForm.Suggestions(book, EquipmentKind.Load, "powder", null));
    }

    /// <summary>A match is anywhere in the value, so somebody who typed "Lapua 6.5 Creedmoor" gets it back after typing "creed".</summary>
    [Fact]
    public void AMatchIsAnywhereInTheValueAndIgnoresCase()
    {
        var book = RecordBook.Empty
            .With(new Load("A", null) { BrassCartridge = "Lapua 6.5 Creedmoor" })
            .With(new Load("B", null) { BrassCartridge = "Peterson 308 Winchester" });

        Assert.Equal(["Lapua 6.5 Creedmoor"], EquipmentForm.Suggestions(book, EquipmentKind.Load, "brassCartridge", "creed"));
    }

    /// <summary>
    /// A name is never offered back, and neither is a fixed list. Offering a name would invite two records called the same thing, and a
    /// choice is already in front of the person.
    /// </summary>
    [Fact]
    public void NamesAndFixedListsAreNotOfferedBack()
    {
        var book = RecordBook.Empty.With(FullRifle()).With(new Rifle("Tikka T1x", 0.25, AngularUnit.Moa));

        Assert.Empty(EquipmentForm.Suggestions(book, EquipmentKind.Rifle, "name", "Tik"));
        Assert.Empty(EquipmentForm.Suggestions(book, EquipmentKind.Rifle, "clickUnit", null));
        Assert.Empty(EquipmentForm.Suggestions(book, EquipmentKind.Rifle, "nothingCalledThis", null));
    }

    [Fact]
    public void BlanksAreNotOffered()
    {
        var book = RecordBook.Empty
            .With(new Load("A", null) { Powder = "   " })
            .With(new Load("B", null));

        Assert.Empty(EquipmentForm.Suggestions(book, EquipmentKind.Load, "powder", null));
    }

    /// <summary>A name is the one required field.</summary>
    [Fact]
    public void ARecordWithNoNameCannotBeSaved()
    {
        Assert.NotNull(EquipmentForm.WhyNotSaveable(RecordBook.Empty, EquipmentKind.Rifle, "  "));
        Assert.Null(EquipmentForm.WhyNotSaveable(RecordBook.Empty, EquipmentKind.Rifle, "Tikka"));
    }

    /// <summary>
    /// A name already used is refused with a sentence rather than silently replacing the record behind it, which is what the record book
    /// would otherwise do to somebody who had forgotten using that name.
    /// </summary>
    [Fact]
    public void ANameAlreadyUsedIsRefusedRatherThanOverwriting()
    {
        var book = RecordBook.Empty.With(FullRifle());

        string? why = EquipmentForm.WhyNotSaveable(book, EquipmentKind.Rifle, "tikka t3x");
        Assert.NotNull(why);
        Assert.Contains("already a rifle", why, StringComparison.Ordinal);

        // Opening that record and saving it under its own name is not a clash.
        Assert.Null(EquipmentForm.WhyNotSaveable(book, EquipmentKind.Rifle, "Tikka T3x", replacing: "Tikka T3x"));
    }

    /// <summary>A barrel's round count still adds up the way the rest of the application counts it.</summary>
    [Fact]
    public void FiringStillCountsRoundsOntoTheBarrel()
    {
        var book = RecordBook.Empty.With(FullBarrel()).Fired("Bartlein #3", 25);

        Assert.Equal(505, book.Barrels.Single().Rounds);
        Assert.Equal(new DateOnly(2025, 3, 14), book.Barrels.Single().Installed);
    }
}

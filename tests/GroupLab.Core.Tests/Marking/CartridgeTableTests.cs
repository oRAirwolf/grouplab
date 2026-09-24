using System.Globalization;
using System.Text.Json;
using GroupLab.Core.Marking;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 163 section 3: cartridge names, matched before numbers, with the traps named. A real user typed 6.5 for his
/// 6.5 Creedmoor and was offered .257, because "6.53 mm" contains "6.5".
/// </summary>
public class CartridgeTableTests
{
    /// <summary>
    /// Section 7.1, one case per row of section 3.2 that the table can offer: what is typed, the diameter that must come first, and the trap
    /// that must be named under it where the row has one.
    /// </summary>
    [Theory]
    [InlineData("6.5", 0.264, 0.257)]
    [InlineData("6.5 Creedmoor", 0.264, 0.257)]
    [InlineData("6.5x55", 0.264, 0.257)]
    [InlineData("260", 0.264, 0.257)]
    [InlineData("25-06", 0.257, 0.264)]
    [InlineData("257 Roberts", 0.257, 0.264)]
    [InlineData("243", 0.243, null)]
    [InlineData("223", 0.224, null)]
    [InlineData("22-250", 0.224, null)]
    [InlineData("270", 0.277, 0.284)]
    [InlineData("6.8 SPC", 0.277, 0.284)]
    [InlineData("7mm", 0.284, 0.277)]
    [InlineData("280", 0.284, 0.277)]
    [InlineData("7mm-08", 0.284, 0.277)]
    [InlineData("308", 0.308, null)]
    [InlineData("30-06", 0.308, null)]
    [InlineData("300 Win Mag", 0.308, null)]
    [InlineData("8x57", 0.323, null)]
    [InlineData("338 Lapua", 0.338, null)]
    [InlineData("9mm", 0.355, 0.357)]
    [InlineData("380", 0.355, 0.357)]
    [InlineData("38", 0.357, 0.355)]
    [InlineData(".38", 0.357, 0.355)]
    [InlineData("357 Mag", 0.357, 0.355)]
    [InlineData("40", 0.400, null)]
    [InlineData("10mm", 0.400, null)]
    public void TheRightBulletComesFirstAndTheTrapIsNamed(string typed, double first, double? trap)
    {
        var lines = CartridgeTable.Suggest(typed);
        Assert.NotEmpty(lines);
        string mine = string.Create(CultureInfo.InvariantCulture, $": {first:0.000} in (");
        Assert.Contains(mine, lines[0], StringComparison.Ordinal);

        // Never a bare diameter: every offer names cartridges, and a name comes before the number.
        Assert.All(lines, l => Assert.Matches(@"^[^:]*[A-Za-z]", l));

        if (trap is { } t)
        {
            Assert.Contains(lines, l => l.StartsWith("not the same as", StringComparison.Ordinal)
                && l.Contains(string.Create(CultureInfo.InvariantCulture, $"{t:0.000} in"), StringComparison.Ordinal));
        }

        // And choosing what was typed, or the line offered, gives that bullet.
        Assert.Equal(first, Calibre.Parse(typed, out _)!.DiameterInches, 6);
        Assert.Equal(first, Calibre.Parse(lines[0], out _)!.DiameterInches, 6);
    }

    /// <summary>The case that started it, in the words the list shows: 0.264 first, and .257 named as the mistake it is.</summary>
    [Fact]
    public void TypingSixPointFiveOffersTheSixPointFiveFamilyAndNamesTheTwentyFive()
    {
        var lines = CartridgeTable.Suggest("6.5");
        Assert.StartsWith("6.5 Creedmoor", lines[0], StringComparison.Ordinal);
        Assert.EndsWith(": 0.264 in (6.71 mm)", lines[0], StringComparison.Ordinal);
        Assert.Equal("not the same as .25 calibre, 0.257 in (6.53 mm)", lines[1]);

        // A trap line is not a choice.
        Assert.Null(Calibre.Parse(lines[1], out _));
    }

    /// <summary>
    /// The rows of section 3.2 the table cannot offer yet, because only one source confirms them or the two disagree. They must not be
    /// offered at a wrong diameter; they fall back to asking for the diameter, which is what happened before.
    /// </summary>
    [Theory]
    [InlineData("7.62x39")]
    [InlineData("7.62x54R")]
    [InlineData(".303 British")]
    [InlineData(".44 Magnum")]
    [InlineData(".45 ACP")]
    [InlineData(".50 BMG")]
    [InlineData("22 LR")]
    public void AHeldCartridgeIsNotOfferedAtAnyDiameter(string typed)
    {
        Assert.Empty(CartridgeTable.Suggest(typed));
        Assert.Null(Calibre.Parse(typed, out string? why));
        Assert.False(string.IsNullOrEmpty(why));
    }

    /// <summary>Section 3.3: every entry has a name, a shorthand, a diameter and its sources, and everything offered has two.</summary>
    [Fact]
    public void EveryOfferedCartridgeHasTwoSourcesAndEveryHeldOneSaysWhatItHas()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Repo.PathTo("src", "GroupLab.Core", "Marking"), "cartridges.json")));
        var sources = doc.RootElement.GetProperty("sources").EnumerateObject().Select(p => p.Name).ToHashSet();
        Assert.True(sources.Count >= 2, "the table names fewer than two sources");

        foreach (var family in CartridgeTable.Families)
        {
            Assert.InRange(family.Diameter, 0.1, 1.0);
            Assert.NotEmpty(family.Shorthand);
            foreach (var c in family.Cartridges)
            {
                Assert.False(string.IsNullOrWhiteSpace(c.Name));
                Assert.True(c.Shorthand.Count > 0, $"{c.Name} has no shorthand");
                Assert.True(c.Sources.Distinct().Count(sources.Contains) >= 2,
                    $"{c.Name} is offered at {family.Diameter:0.000} in with {c.Sources.Count} source(s); entry 163 section 3.2 asks for two.");
            }
        }

        foreach (var held in doc.RootElement.GetProperty("pending").EnumerateArray())
        {
            Assert.False(string.IsNullOrWhiteSpace(held.GetProperty("name").GetString()));
            int agree = held.GetProperty("sources").GetArrayLength();
            Assert.True(agree < 2 || held.TryGetProperty("note", out _),
                $"{held.GetProperty("name").GetString()} is held with two sources and no note saying why; offer it or say why not.");
        }
    }

    /// <summary>Entry 163 section 3.1 rule 3: a leading point or leading zero below one is a diameter in inches, when it names nothing.</summary>
    [Fact]
    public void ADiameterThatNamesNothingIsStillADiameter()
    {
        Assert.Equal(0.2215, Calibre.Parse("0.2215", out _)!.DiameterInches, 6);
        Assert.Equal(0.264, Calibre.Parse(".264", out _)!.DiameterInches, 6);
        Assert.Equal(0.264, Calibre.Parse("6.71 mm", out _)!.DiameterInches, 2);
        Assert.Null(Calibre.Parse("7.62 mm", out _));
    }
}

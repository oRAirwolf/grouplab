using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 314 section 2: the lookup of every cartridge, planning's 661 rows with the rows that named one cartridge twice
/// merged, and the search that autocompletes it; and section 1, what the caliber box offers from it.
/// </summary>
public class CartridgeLookupTests
{
    [Fact]
    public void EveryRowHasADiameterAndATier()
    {
        // 661 rows in planning's draft, 56 of them a cartridge already listed under the same or another name.
        Assert.Equal(605, CartridgeLookup.All.Count);
        Assert.All(CartridgeLookup.All, r =>
        {
            Assert.False(string.IsNullOrWhiteSpace(r.Name));
            Assert.True(r.DiameterInches > 0, r.Name);
            Assert.InRange(r.Tier, 1, 4);
            Assert.True(Math.Abs(r.DiameterInches * 25.4 - r.DiameterMillimetres) < 0.05, $"{r.Name}: {r.DiameterInches} in is not {r.DiameterMillimetres} mm");
            Assert.Contains(r.Kind, new[] { "rifle", "handgun", "rifle handgun" });
        });
    }

    [Fact]
    public void EveryNameAndAliasMeansOneCartridge()
    {
        var owners = CartridgeLookup.All
            .SelectMany(r => r.Names.Select(CartridgeLookup.Key).Distinct().Select(k => (Key: k, r.Name)))
            .GroupBy(p => p.Key)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key}: {string.Join(", ", g.Select(p => p.Name))}")
            .ToList();
        Assert.True(owners.Count == 0, string.Join(" | ", owners));
    }

    /// <summary>The bullet diameters SAAMI and CIP give for the everyday and common cartridges, a representative set.</summary>
    [Theory]
    [InlineData(".223 Remington", 0.224)]
    [InlineData("5.56x45mm NATO", 0.224)]
    [InlineData(".22-250 Remington", 0.224)]
    [InlineData(".243 Winchester", 0.243)]
    [InlineData("6mm Creedmoor", 0.243)]
    [InlineData(".25-06 Remington", 0.257)]
    [InlineData("6.5 Creedmoor", 0.264)]
    [InlineData("6.5x55mm Swedish", 0.264)]
    [InlineData(".270 Winchester", 0.277)]
    [InlineData("7mm Remington Magnum", 0.284)]
    [InlineData(".308 Winchester", 0.308)]
    [InlineData(".30-06 Springfield", 0.308)]
    [InlineData(".300 Winchester Magnum", 0.308)]
    [InlineData("7.62x39mm", 0.311)]
    [InlineData(".338 Lapua Magnum", 0.338)]
    [InlineData("9mm Luger", 0.355)]
    [InlineData(".380 ACP", 0.355)]
    [InlineData(".38 Special", 0.357)]
    [InlineData(".357 Magnum", 0.357)]
    [InlineData(".40 S&W", 0.400)]
    [InlineData("10mm Auto", 0.400)]
    [InlineData(".44 Magnum", 0.429)]
    [InlineData(".45 ACP", 0.452)]
    [InlineData(".45 Colt", 0.452)]
    [InlineData(".45-70", 0.458)]
    [InlineData(".17 Hornady Magnum Rimfire", 0.172)]
    public void TheCommonCartridgesCarryTheirStandardDiameter(string name, double inches)
    {
        var row = CartridgeLookup.Exact(name);
        Assert.NotNull(row);
        Assert.InRange(row.Tier, 1, 2);
        Assert.Equal(inches, row.DiameterInches, 3);
    }

    [Fact]
    public void EveryEverydayAndCommonDiameterIsOneABulletCanHave()
    {
        // A slip of a digit in the table (.0308, 3.08) would put a common cartridge's calibre outside what a hole can measure.
        Assert.All(CartridgeLookup.All.Where(r => r.Tier <= 2), r => Assert.InRange(r.DiameterInches, 0.17, 0.51));
    }

    [Theory]
    [InlineData("65 creed", "6.5 Creedmoor")]
    [InlineData("6.5cm", "6.5 Creedmoor")]
    [InlineData("308", ".308 Winchester")]
    [InlineData("9mm", "9mm Luger")]
    [InlineData("300 blk", ".300 AAC Blackout")]
    [InlineData("22lr", ".22 Long Rifle")]
    public void TheSearchForgivesPunctuationAndSpacing(string typed, string first)
    {
        Assert.Equal(first, CartridgeLookup.Search(typed)[0].Name);
    }

    [Fact]
    public void AnExactNameComesFirstThenTheTierThenPrecision()
    {
        // "6mm" is exactly nothing, so the tier decides; "6 BR" is exactly the 6mm BR's own short name, though it is tier 3.
        var six = CartridgeLookup.Search("6mm").ToList();
        Assert.True(six.Count > 5);
        for (int i = 1; i < six.Count; i++)
        {
            Assert.True(six[i - 1].Tier <= six[i].Tier, $"{six[i - 1].Name} before {six[i].Name}");
            if (six[i - 1].Tier == six[i].Tier)
            {
                Assert.True(six[i - 1].Precision || !six[i].Precision, $"{six[i - 1].Name} before {six[i].Name}");
            }
        }

        Assert.Equal("6mm BR Remington", CartridgeLookup.Search("6 BR")[0].Name);
    }

    [Fact]
    public void BeforeTypingOnlyTheCommonAndThePrecisionCartridgesAreShown()
    {
        var common = CartridgeLookup.Search("");
        Assert.Same(CartridgeLookup.Common, common);
        Assert.All(common, r => Assert.True(r.Tier <= 2 || r.Precision, r.Name));
        Assert.Equal(CartridgeLookup.Offered.Count(r => r.Tier <= 2 || r.Precision), common.Count);
        Assert.Equal(1, common[0].Tier);

        // A rare one is found when it is typed, and not before.
        var nagant = CartridgeLookup.Search("7.62 nagant");
        Assert.Equal("7.62mm Nagant", nagant[0].Name);
        Assert.DoesNotContain(common, r => r.Name == "7.62mm Nagant");
    }

    [Fact]
    public void TheCalibersAreTheCommonDiametersWithTheirUsualNames()
    {
        var expected = CartridgeLookup.Offered.Where(r => r.Tier <= 2).Select(r => r.DiameterInches).Distinct().Order().ToList();
        Assert.Equal(expected, CaliberChoices.Calibers.Select(c => c.Inches));
        var labels = CaliberChoices.Calibers.Select(c => c.Label).ToList();
        foreach (string label in new[] { ".224 (5.56 mm)", ".243 (6 mm)", ".264 (6.5 mm)", ".277 (6.8 mm, .270)", ".284 (7 mm)", ".308 (7.62 mm, .30)", ".311", ".338", ".355 (9 mm)", ".357", ".400 (10 mm, .40)", ".429 (.44)", ".452 (.45)" })
        {
            Assert.Contains(label, labels);
        }

        // Entry 314 section 2.4: the grouping the confirmed table makes under one diameter is kept.
        Assert.Equal(".264 (6.5 mm): 6.5 Creedmoor, 6.5x55 Swedish, .260 Remington and others", CaliberChoices.Calibers.Single(c => c.Inches == 0.264).Line);
    }

    [Fact]
    public void TheSettingChoosesCalibersCartridgesOrBoth()
    {
        var calibers = CaliberChoices.Suggest("65 creed", CaliberList.Calibers);
        Assert.Equal(".264 (6.5 mm): 6.5 Creedmoor, 6.5x55 Swedish, .260 Remington and others", calibers[0]);
        Assert.All(calibers, l => Assert.True(l.StartsWith('.') || l.StartsWith("not the same as", StringComparison.Ordinal), l));

        var cartridges = CaliberChoices.Suggest("65 creed", CaliberList.Cartridges);
        Assert.Equal("6.5 Creedmoor, 0.264 in (6.71 mm)", cartridges[0]);
        Assert.DoesNotContain(cartridges, l => l.StartsWith(".264 (", StringComparison.Ordinal));

        // Both: the calibers first, then the cartridges.
        var both = CaliberChoices.Suggest("65 creed", CaliberList.Both);
        Assert.Equal(calibers.Concat(cartridges), both);

        // Before typing: every caliber, then the common cartridges.
        var shown = CaliberChoices.Suggest("", CaliberList.Both);
        Assert.Equal(CaliberChoices.Calibers.Count + CartridgeLookup.Common.Count, shown.Count);
        Assert.Equal(CaliberChoices.Calibers[0].Line, shown[0]);
    }

    [Fact]
    public void EveryChoiceKeepsAShortNameThatReadsAsItsDiameter()
    {
        foreach (var caliber in CaliberChoices.Calibers)
        {
            string kept = CaliberChoices.Short(caliber.Line);
            Assert.Equal(caliber.Label, kept);
            Assert.Equal(caliber.Line, CaliberChoices.Explain(kept));
            Assert.Equal(caliber.Inches, Calibre.Parse(kept, out _)!.DiameterInches, 4);
            Assert.Equal(caliber.Inches, Calibre.Parse(caliber.Line, out _)!.DiameterInches, 4);
        }

        foreach (var row in CartridgeLookup.Offered)
        {
            string line = CaliberChoices.Line(row);
            string kept = CaliberChoices.Short(line);
            Assert.Equal(CaliberChoices.ShortName(row.Name, row.DiameterInches), kept);
            Assert.NotNull(CaliberChoices.Explain(kept));
            Assert.Equal(row.DiameterInches, Calibre.Parse(kept, out string? why)!.DiameterInches, 4);
            Assert.Null(why);
        }

        // Any other diameter is still typed, in inches or in millimetres.
        Assert.Equal(0.3105, Calibre.Parse("0.3105", out _)!.DiameterInches, 4);
        Assert.Equal(7.9 / 25.4, Calibre.Parse("7.9 mm", out _)!.DiameterInches, 4);
    }
}

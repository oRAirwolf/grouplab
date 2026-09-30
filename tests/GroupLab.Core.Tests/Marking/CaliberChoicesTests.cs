using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 312 section 5: a choice from the caliber list leaves a short name in the box, "6.5 Creedmoor, 0.264 in",
/// with the whole line under it, and the short name reads back as the same diameter.
/// </summary>
public class CaliberChoicesTests
{
    [Fact]
    public void AChosenLineLeavesAShortNameThatReadsAsItsDiameter()
    {
        string line = CartridgeTable.Suggest("6.5")[0];
        Assert.StartsWith("6.5 Creedmoor, 6.5x55 Swedish", line, StringComparison.Ordinal);
        string shortName = CaliberChoices.Short(line);
        Assert.Equal("6.5 Creedmoor, 0.264 in", shortName);
        // Entry 314: the name is the lookup's 6.5 Creedmoor, so what is said beneath is that cartridge, its diameter and its other names.
        Assert.StartsWith("6.5 Creedmoor: 0.264 in (6.71 mm), a rifle cartridge", CaliberChoices.Explain(shortName), StringComparison.Ordinal);
        Assert.Equal(0.264, Calibre.Parse(shortName, out string? why)!.DiameterInches, 4);
        Assert.Null(why);

        // A warning line is not a choice, and typed text stays as it was typed.
        string trap = CartridgeTable.Suggest("6.5").First(s => s.StartsWith("not the same as", StringComparison.Ordinal));
        Assert.Equal(trap, CaliberChoices.Short(trap));
        Assert.Equal("6.5", CaliberChoices.Short(" 6.5 "));
        Assert.Null(CaliberChoices.Explain("6.5"));
    }
}

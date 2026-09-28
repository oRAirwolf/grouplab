using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Rendering;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 250 section 2. Alan: "Can we push the A4 pages to the bottom of the lists? I want letter to be above A4." One
/// rule orders every family of the Targets list, desktop and phone: Letter, then the other US sizes, then A4 and A3, and the other way round
/// where the system's region uses A4.
/// </summary>
public class LibraryOrderTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EachFamilyListsItsPaperInTheRegionsOrder(bool letterFirst)
    {
        var sheets = TargetLibrary.Load(Repo.PathTo("targets"), letterFirst);
        foreach (var family in sheets.GroupBy(s => s.Family))
        {
            var ranks = family.Select(s => TargetLibrary.PaperRank(s.Definition.Page.Size, letterFirst)).ToList();
            Assert.True(ranks.SequenceEqual(ranks.Order()), $"{family.Key}: " + string.Join(", ", family.Select(s => s.File)));
        }

        // Each family stays in one run, in the catalogue's order, whichever paper leads.
        var seen = new List<string>();
        foreach (var sheet in sheets)
        {
            if (seen.Count == 0 || seen[^1] != sheet.Family)
            {
                Assert.DoesNotContain(sheet.Family, seen);
                seen.Add(sheet.Family);
            }
        }

        Assert.Equal(TargetLibrary.Load(Repo.PathTo("targets"), !letterFirst).Select(s => s.Family).Distinct(), seen);
    }

    [Fact]
    public void LetterComesBeforeA4ForAlan()
    {
        var centerfire = TargetLibrary.Load(Repo.PathTo("targets")).Where(s => s.Family == "Centerfire load development").ToList();
        int lastLetter = centerfire.FindLastIndex(s => s.Definition.Page.Size == PageSize.Letter);
        int firstA4 = centerfire.FindIndex(s => s.Definition.Page.Size == PageSize.A4);
        Assert.True(lastLetter >= 0 && firstA4 > lastLetter, string.Join(", ", centerfire.Select(s => s.File)));
    }
}

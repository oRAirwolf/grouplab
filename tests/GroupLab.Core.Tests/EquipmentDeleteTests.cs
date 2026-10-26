using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Tests;

/// <summary>Error report 38, 2026-10-25: rifles, barrels and loads can be deleted, a rifle with the barrels on it.</summary>
public class EquipmentDeleteTests
{
    private static RecordBook Book() => RecordBook.Empty
        .With(new Rifle("Rifle A", 0.1, AngularUnit.Mrad))
        .With(new Rifle("Rifle B", 0.25, AngularUnit.Moa))
        .With(new Barrel("Barrel one", "Rifle A", 0))
        .With(new Barrel("Barrel two", "Rifle B", 0))
        .PutOn("Rifle B", "Barrel two")
        .With(new Load("Load A", null))
        .With(new Load("Load B", null));

    [Fact]
    public void ARifleGoesWithItsBarrelsAndNothingElse()
    {
        var after = Book().WithoutRifle("rifle a");
        Assert.Equal(["Rifle B"], after.Rifles.Select(r => r.Name));
        Assert.Equal(["Barrel two"], after.Barrels.Select(b => b.Name));
        Assert.Equal(2, after.Loads.Count);
    }

    [Fact]
    public void ABarrelOnARifleLeavesTheRifleWithNoneOn()
    {
        var after = Book().WithoutBarrel("Barrel two");
        Assert.Null(after.FindBarrel("Barrel two"));
        Assert.Null(after.FindRifle("Rifle B")!.BarrelOn);
        Assert.Equal(2, after.Rifles.Count);
    }

    [Fact]
    public void ALoadGoesAlone()
    {
        var after = Book().WithoutLoad("Load A");
        Assert.Equal(["Load B"], after.Loads.Select(l => l.Name));
        Assert.Equal(2, after.Rifles.Count);
        Assert.Equal(2, after.Barrels.Count);
    }
}

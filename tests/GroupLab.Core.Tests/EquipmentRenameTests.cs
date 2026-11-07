using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Tests;

/// <summary>
/// Error report 42, 2026-10-28 (iPhone): changing a rifle's or a load's name kept a second one under the new name beside the first. Kept from the
/// form it was opened in, a new name now renames it, a rifle's barrels go with it, and a new record is still added as before.
/// </summary>
public class EquipmentRenameTests
{
    private static RecordBook Book() => RecordBook.Empty
        .With(new Rifle("Rifle A", 0.1, AngularUnit.Mrad))
        .With(new Rifle("Rifle B", 0.25, AngularUnit.Moa))
        .With(new Barrel("Barrel one", "Rifle A", 0))
        .With(new Barrel("Barrel two", "Rifle B", 0))
        .PutOn("Rifle A", "Barrel one")
        .With(new Load("Load A", null))
        .With(new Load("Load B", null));

    [Fact]
    public void ARenamedRifleIsOneRifleAndKeepsItsBarrels()
    {
        var book = Book();
        var after = book.With(book.FindRifle("Rifle A")! with { Name = "Rifle C" }, "Rifle A");
        Assert.Equal(["Rifle B", "Rifle C"], after.Rifles.Select(r => r.Name).Order());
        Assert.Equal("Rifle C", after.FindBarrel("Barrel one")!.Rifle);
        Assert.Equal("Barrel one", after.FindRifle("Rifle C")!.BarrelOn);
        Assert.Equal(["Barrel one"], after.BarrelsOf("Rifle C").Select(b => b.Name));
    }

    [Fact]
    public void ARenamedLoadIsOneLoad()
    {
        var after = Book().With(new Load("Load C", null), "Load A");
        Assert.Equal(["Load B", "Load C"], after.Loads.Select(l => l.Name).Order());
    }

    [Fact]
    public void ARenamedBarrelStaysOnItsRifle()
    {
        var after = Book().With(new Barrel("Barrel nine", "Rifle A", 0), "Barrel one");
        Assert.Null(after.FindBarrel("Barrel one"));
        Assert.Equal("Barrel nine", after.FindRifle("Rifle A")!.BarrelOn);
    }

    [Fact]
    public void ANewRecordOrTheSameNameIsKeptAsBefore()
    {
        var book = Book();
        Assert.Equal(3, book.With(new Rifle("Rifle D", 0.1, AngularUnit.Mrad), null).Rifles.Count);
        Assert.Equal(2, book.With(new Load("load a", "changed"), "Load A").Loads.Count);
        Assert.Equal(3, book.With(new Load("Load D", null), "No such load").Loads.Count);
    }

    [Fact]
    public void ANameAnotherRecordHasIsRefusedBeforeItIsKept()
    {
        Assert.NotNull(EquipmentForm.WhyNotSaveable(Book(), EquipmentKind.Rifle, "Rifle B", "Rifle A"));
        Assert.Null(EquipmentForm.WhyNotSaveable(Book(), EquipmentKind.Rifle, "Rifle C", "Rifle A"));
    }
}

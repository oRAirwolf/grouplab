using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 131 section 7: rifles, barrels and loads on a screen of their own.
/// <para>
/// <b>What it replaces is the point.</b> Records were added from a box on the marking screen with one field shared between a barrel's round
/// count and a load's components, so the field meant two different things depending on which button you pressed afterwards, and nothing said
/// so. Alan called it confusing.
/// </para>
/// </summary>
public class EquipmentScreenTests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    [AvaloniaFact]
    public void EachListHasItsOwnFormAndEveryFieldOfTheRecordIsOnIt()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            foreach (var kind in new[] { EquipmentKind.Rifle, EquipmentKind.Barrel, EquipmentKind.Load })
            {
                window.ShowEquipment(kind);
                Settle();

                // The form is generated from the one field list, so it is the record's own fields and nothing else.
                Assert.Equal(EquipmentForm.For(kind).Select(f => f.Key), window.EquipmentFieldKeys);
            }
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>A rifle typed into the form is a rifle in the book, with the fields that used to be lost on the way to disc.</summary>
    [AvaloniaFact]
    public void ARifleSavedFromTheFormKeepsEverythingTypedIntoIt()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.ShowEquipment(EquipmentKind.Rifle);
            Settle();

            window.TypeEquipment("name", "Tikka T3x");
            window.TypeEquipment("manufacturer", "Tikka");
            window.TypeEquipment("cartridge", "6.5 Creedmoor");
            window.TypeEquipment("clickValue", "0.1");
            window.TypeEquipment("clickUnit", "mil");
            window.TypeEquipment("sightHeightInches", "1.85");
            window.TypeEquipment("zeroDistanceYards", "100");
            window.SaveEquipment();
            Settle();

            var rifle = window.Book.FindRifle("Tikka T3x");
            Assert.NotNull(rifle);
            Assert.Equal("Tikka", rifle!.Manufacturer);
            Assert.Equal("6.5 Creedmoor", rifle.Cartridge);
            Assert.Equal(0.1, rifle.ClickValue);
            Assert.Equal(AngularUnit.Mrad, rifle.ClickUnit);
            Assert.Equal(1.85, rifle.SightHeightInches);
            Assert.Equal(100, rifle.ZeroDistanceYards);

            Assert.Contains("Tikka T3x", window.EquipmentNames);
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 407 section 5 (concept 5B): a barrel is added from its rifle's page, comes back to it when saved, is the one on
    /// now when it is the rifle's first, and another chosen by its radio takes its place; the rifle then carries the barrel's twist, which is
    /// what the ballistics page reads, and the setup's one list names them together with the barrel on now first.
    /// </summary>
    [AvaloniaFact]
    public void ABarrelIsAddedOnItsRiflesPageAndTheOneOnNowLeadsTheSetupList()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.Book = RecordBook.Empty.With(new Rifle("Test rifle", 0.25, AngularUnit.Moa));
            window.ShowEquipment(EquipmentKind.Rifle);
            window.EditBarrel("Test rifle", null);
            Settle();
            window.TypeEquipment("name", "Bartlein");
            window.TypeEquipment("lengthInches", "26");
            window.TypeEquipment("twistInches", "8");
            window.SaveEquipment();
            Settle();
            Assert.Equal("Bartlein", window.Book.FindRifle("Test rifle")!.BarrelOn);
            Assert.Equal(8, window.Book.FindRifle("Test rifle")!.TwistInches);

            window.EditBarrel("Test rifle", null);
            window.TypeEquipment("name", "Krieger");
            window.TypeEquipment("twistInches", "7.5");
            window.SaveEquipment();
            Settle();
            Assert.Equal(["Bartlein", "Krieger"], window.BarrelRows("Test rifle"));

            window.PutBarrelOn("Test rifle", "Krieger");
            Settle();
            Assert.Equal(["Krieger", "Bartlein"], window.BarrelRows("Test rifle"));
            Assert.Equal(7.5, window.Book.FindRifle("Test rifle")!.TwistInches);
            Assert.Equal("Test rifle with its Krieger (on now)", window.Book.Together(window.Book.FindRifle("Test rifle")!, window.Book.FindBarrel("Krieger")));
            Assert.Equal("Test rifle with its 26 in Bartlein", window.Book.Together(window.Book.FindRifle("Test rifle")!, window.Book.FindBarrel("Bartlein")));
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>A name is the one thing it needs, and the screen says so rather than saving something nothing can refer to.</summary>
    [AvaloniaFact]
    public void ARecordWithNoNameIsRefusedWithAReason()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.ShowEquipment(EquipmentKind.Load);
            Settle();

            window.TypeEquipment("powder", "H4350");
            window.SaveEquipment();
            Settle();

            Assert.Contains("A name is the one thing", window.EquipmentProblemText, StringComparison.Ordinal);
            Assert.Empty(window.EquipmentNames);
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>
    /// A name already in the list is refused rather than quietly replacing the record behind it. The record book replaces by name, so saving
    /// under a name somebody had forgotten using would have overwritten it with no warning at all.
    /// </summary>
    [AvaloniaFact]
    public void ANameAlreadyUsedIsRefused()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.Book = RecordBook.Empty.With(new Rifle("Tikka", 0.25, AngularUnit.Moa));
            window.ShowEquipment(EquipmentKind.Rifle);
            Settle();

            window.TypeEquipment("name", "tikka");
            window.SaveEquipment();
            Settle();

            Assert.Contains("already a rifle", window.EquipmentProblemText, StringComparison.Ordinal);
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>
    /// Entry 131 section 7.7: records made before this screen existed still open on it, with nothing lost. The old box could set a rifle's
    /// name and click, a barrel's round count and a load's components, and all four are still there.
    /// </summary>
    [AvaloniaFact]
    public void RecordsMadeBeforeThisScreenStillOpenOnIt()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.Book = RecordBook.Empty
                .With(new Rifle("Old rifle", 0.25, AngularUnit.Moa))
                .With(new Barrel("Old barrel", "Old rifle", 1200))
                .With(new Load("Old load", "Hornady Match"));

            window.ShowEquipment(EquipmentKind.Rifle);
            Settle();
            Assert.Contains("Old rifle", window.EquipmentNames);

            // Entry 407 section 5: a barrel is listed under its rifle now, on the rifle's own page.
            Assert.Contains("Old barrel", window.BarrelRows("Old rifle"));
            Assert.Equal(1200, window.Book.FindBarrel("Old barrel")!.Rounds);

            window.ShowEquipment(EquipmentKind.Load);
            Settle();
            Assert.Contains("Old load", window.EquipmentNames);
            Assert.Equal("Hornady Match", window.Book.FindLoad("Old load")!.Components);
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}

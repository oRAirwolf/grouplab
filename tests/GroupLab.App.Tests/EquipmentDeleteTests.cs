using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.Headless.XUnit;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.App.Tests;

/// <summary>Error report 38, 2026-10-25: on the computer, Equipment's Delete asks first, and Keep it leaves the record alone.</summary>
public class EquipmentDeleteTests
{
    private static void Press(MainWindow window, string name)
    {
        var button = window.GetLogicalDescendants().OfType<Button>().First(b => b.Name == name && b.IsEffectivelyVisible);
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void ARifleIsDeletedWithItsBarrelsOnlyOnceAsked()
    {
        var (window, _, _) = Entry109Tests.Sheet();
        try
        {
            window.Book = RecordBook.Empty.With(new Rifle("Test rifle", 0.1, AngularUnit.Mrad)).With(new Barrel("Its barrel", "Test rifle", 0))
                .With(new Load("Test load", null));
            window.EditRifle("Test rifle");
            Dispatcher.UIThread.RunJobs();
            Press(window, "equipment-delete");
            Assert.NotNull(window.Book.FindRifle("Test rifle"));
            Assert.Contains(window.GetLogicalDescendants().OfType<TextBlock>(),
                t => t.Text == "Delete the rifle Test rifle and its 1 barrel? Sessions saved with it keep its name.");

            Press(window, "equipment-delete-yes");
            Assert.Null(window.Book.FindRifle("Test rifle"));
            Assert.Null(window.Book.FindBarrel("Its barrel"));
            Assert.NotNull(window.Book.FindLoad("Test load"));
            Assert.Null(window.Sessions!.LoadBook().FindRifle("Test rifle"));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ARecordNotYetSavedHasNoDelete()
    {
        var (window, _, _) = Entry109Tests.Sheet();
        try
        {
            window.ShowEquipment(EquipmentKind.Load);
            Dispatcher.UIThread.RunJobs();
            Assert.DoesNotContain(window.GetLogicalDescendants().OfType<Button>(), b => b.Name == "equipment-delete" && b.IsEffectivelyVisible);
        }
        finally
        {
            window.Close();
        }
    }
}

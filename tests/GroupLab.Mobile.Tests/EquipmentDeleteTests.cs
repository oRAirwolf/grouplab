using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;
using GroupLab.Mobile.Dev;

namespace GroupLab.Mobile.Tests;

/// <summary>Error report 38, 2026-10-25: on the phone, the rifle and load forms on Ballistics have Delete, asked first, as the computer has.</summary>
public class EquipmentDeleteTests
{
    private static readonly TestPhone ThePhone = new();

    private static void Press(Control within, string id)
    {
        var button = within.GetLogicalDescendants().OfType<Button>().First(b => Avalonia.Automation.AutomationProperties.GetAutomationId(b) == id);
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void ALoadIsDeletedOnlyOnceAskedAndARifleWithItsBarrels()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(ThePhone, Avalonia.Application.Current!, () => "US", null);
        }

        var store = PhoneAnalysis.Store();
        var before = store.LoadBook();
        var window = new Window { Width = 402, Height = 874 };
        window.Show();
        try
        {
            store.SaveBook(RecordBook.Empty.With(new Rifle("Phone rifle", 0.1, AngularUnit.Mrad)).With(new Barrel("Its barrel", "Phone rifle", 0))
                .With(new Load("Phone load", null)));
            var page = new BallisticsPage(openForm: "load");
            window.Content = page;
            Dispatcher.UIThread.RunJobs();
            Press(page, "ballistics-load-delete");
            Assert.NotNull(store.LoadBook().FindLoad("Phone load"));
            Assert.Contains(page.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text == "Delete the load Phone load? Sessions saved with it keep its name.");
            Press(page, "ballistics-load-delete-yes");
            Assert.Null(store.LoadBook().FindLoad("Phone load"));

            page = new BallisticsPage(openForm: "rifle");
            window.Content = page;
            Dispatcher.UIThread.RunJobs();
            Press(page, "ballistics-rifle-delete");
            Press(page, "ballistics-rifle-delete-yes");
            Assert.Empty(store.LoadBook().Rifles);
            Assert.Empty(store.LoadBook().Barrels);
        }
        finally
        {
            store.SaveBook(before);
            window.Close();
        }
    }
}

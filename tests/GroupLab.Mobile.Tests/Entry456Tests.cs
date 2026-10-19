using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.Core.Ballistics;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Statistics;
using GroupLab.Mobile;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 456 on the phone: the rifle's form in Ballistics carries the scope and mount, its figures and Top Gun, and a
/// tracking test that puts its factor into the field, and keeping the rifle keeps them.
/// </summary>
public class Entry456Tests
{
    private static readonly TestPhone ThePhone = new();

    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static void Press(Control within, Func<Button, bool> which)
    {
        within.GetLogicalDescendants().OfType<Button>().First(which).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Settle();
    }

    private static string? Id(Control control) => Avalonia.Automation.AutomationProperties.GetAutomationId(control);

    [AvaloniaFact]
    public void TheRifleFormCarriesTheScopeAndMountAndKeepsIt()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(ThePhone, Avalonia.Application.Current!, () => "US", null);
        }

        var store = PhoneAnalysis.Store();
        var before = store.LoadBook();
        var window = new Window { Width = 360, Height = 900, FontSize = 16 };
        try
        {
            store.SaveBook(RecordBook.Empty
                .With(new Rifle("Test rifle", 0.1, AngularUnit.Mrad) { SightHeightInches = 1.75, ZeroDistanceYards = 100, ScopeTravel = 20, RailCant = 6, WeightAsFiredLb = 11 })
                .With(new Load("Test load", null)
                {
                    MuzzleVelocityFps = 2710, MuzzleVelocitySdFps = 10, BallisticCoefficient = 0.326, DragModel = DragModel.G7,
                    BcReference = ReferenceAtmosphere.Icao, BulletWeightGrains = 140,
                }));
            var page = new BallisticsPage();
            window.Content = page;
            window.Show();
            Settle();
            Press(page, b => b.Content as string is { } words && words.StartsWith("Rifle:", StringComparison.Ordinal));
            var said = page.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToList();
            Assert.Contains("Scope and mount", said);
            Assert.Contains(said, t => t.StartsWith("The zero uses 1.2 mil of elevation with Test load.", StringComparison.Ordinal));
            Assert.Contains("Top Gun expects about 1.04 MOA average group for Test load (Bryan Litz's trend, not a limit).", said);

            // The tracking test: 20 mil expected, the turret read 20.5, so 0.976 goes into the factor's field.
            var reading = page.GetLogicalDescendants().OfType<TextBox>().First(t => Id(t) == "ballistics-rifle-reading");
            reading.Text = "20.5";
            Press(page, b => Id(b) == "ballistics-rifle-tracking");
            Assert.Contains(page.GetLogicalDescendants().OfType<TextBox>(), t => t.Text == "0.976");

            Press(page, b => (b.Content as string ?? b.GetLogicalDescendants().OfType<TextBlock>().FirstOrDefault()?.Text) == "Keep this rifle");
            var kept = store.LoadBook().FindRifle("Test rifle")!;
            Assert.Equal(0.976, kept.SightScaleFactor);
            Assert.Equal(20, kept.ScopeTravel);
            Assert.Equal(ScopeTravelKind.Total, kept.ScopeTravelKind);
            Assert.Equal(6, kept.RailCant);
            Assert.Equal(11, kept.WeightAsFiredLb);
        }
        finally
        {
            window.Close();
            store.SaveBook(before);
        }
    }
}

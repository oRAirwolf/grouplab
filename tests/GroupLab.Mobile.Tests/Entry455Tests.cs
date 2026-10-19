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
/// NOTES-FROM-PLANNING.md entry 455 on the phone: Ballistics' Point blank tab gives the computer's answer in the same words, from the Test
/// load, and keeps the zero on the rifle and puts it back.
/// </summary>
public class Entry455Tests
{
    private static readonly TestPhone ThePhone = new();

    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static void Press(Control within, Func<Button, bool> which)
    {
        within.GetLogicalDescendants().OfType<Button>().First(which).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Settle();
    }

    private static string? Id(Control control) => Avalonia.Automation.AutomationProperties.GetAutomationId(control);

    private static IEnumerable<string> Texts(Control within) => within.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "");

    [AvaloniaFact]
    public void ThePointBlankTabGivesTheComputersAnswerAndKeepsTheZero()
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
                .With(new Rifle("Test rifle", 0.1, AngularUnit.Mrad) { SightHeightInches = 1.75, ZeroDistanceYards = 100 })
                .With(new Load("Test load", null)
                {
                    MuzzleVelocityFps = 2710, MuzzleVelocitySdFps = 10, BallisticCoefficient = 0.326, DragModel = DragModel.G7,
                    BcReference = ReferenceAtmosphere.Icao, BulletWeightGrains = 140,
                }));
            var page = new BallisticsPage();
            window.Content = page;
            window.Show();
            Settle();
            Press(page, b => b.Content as string == "Point blank");
            var radius = page.GetLogicalDescendants().OfType<TextBox>().First(t => t.Text == "0.75");
            radius.Text = "0.63";
            Press(page, b => Id(b) == "ballistics-point-blank");

            var said = Texts(page).ToList();
            Assert.Contains("Dead on 0 to 263 yd", said);
            Assert.Contains("Zero 2.6 in high at 100 yd: 0.72 mil, 7 clicks up from your 100 yd zero.", said);
            Assert.Contains(PointBlankTargets.CheckTheLaw, said);

            Press(page, b => Id(b) == "ballistics-point-keep");
            var rifle = store.LoadBook().FindRifle("Test rifle")!;
            Assert.NotEqual(100, rifle.ZeroDistanceYards);
            Assert.Equal(100, Assert.Single(rifle.PreviousZeros).ZeroDistanceYards);

            Press(page, b => b.Content as string == "Point blank");
            Press(page, b => Id(b) == "ballistics-point-blank");
            Press(page, b => Id(b) == "ballistics-point-undo");
            Assert.Equal(100, store.LoadBook().FindRifle("Test rifle")!.ZeroDistanceYards);
        }
        finally
        {
            window.Close();
            store.SaveBook(before);
        }
    }
}

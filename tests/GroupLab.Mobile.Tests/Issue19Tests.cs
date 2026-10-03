using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// A tester's TestFlight report of build 153 (issue 19): "Next button doesn't jump between input fields. It jumps down to the next section
/// instead." On Targets, the optic card's four boxes sit two by two; Next goes across a row, then down, and stays in the card.
/// </summary>
public class Issue19Tests
{
    private static void Settle()
    {
        Dispatcher.UIThread.RunJobs();
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void NextGoesAcrossTheOpticCardsRowsInOrder()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        GroupLab.Mobile.Dev.Scenario.AnswerFirstRun(Phone.Settings);
        var shell = new Shell();
        var window = new Window { Width = 402, Height = 874, Content = shell };
        window.Show();
        try
        {
            shell.Show(Shell.Place.Targets);
            Settle();
            TextBox Labelled(string label) => shell.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == label)
                .GetVisualParent()!.GetVisualDescendants().OfType<TextBox>().First();
            var order = new[] { "Distance, yards", "Lowest magnification", "Red dot size in MOA, at 1x only", "Shots" }.Select(Labelled).ToList();
            order[0].Focus();
            shell.Keyboard.Opened(874 - 336);
            Settle();
            var reached = new List<string>();
            for (int i = 1; i < order.Count; i++)
            {
                shell.Keyboard.Advance();
                Settle();
                var now = TopLevel.GetTopLevel(shell)!.FocusManager!.GetFocusedElement() as Control;
                reached.Add(order.IndexOf(now as TextBox ?? now!.FindAncestorOfType<TextBox>()!).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            Assert.Equal(["1", "2", "3"], reached);
        }
        finally
        {
            window.Close();
        }
    }
}

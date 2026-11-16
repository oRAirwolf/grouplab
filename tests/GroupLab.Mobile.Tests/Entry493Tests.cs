using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 493 on the phone, where the tester asked for it: a printer checked by a scan on a computer is given to the
/// phone, which has no scanner, by typing the two numbers the computer shows. Refused in plain words outside any printer's scale, kept as a
/// check typed in from another device, and shown in Printers and paper with both numbers plainly.
/// </summary>
public class Entry493Tests
{
    private const string Printer = "Entry 493 laser";

    private static void Press(Control page, string words)
    {
        page.GetVisualDescendants().OfType<Button>().First(b => b.Content is TextBlock t && t.Text == words)
            .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void ThePhoneTakesAComputersPrinterCheckAsTypedNumbers()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        bool finished = false;
        var page = new PrinterCheckPage(() => finished = true, Printer);
        var window = new Window { Width = 400, Height = 800, Content = page };
        window.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();
            var choice = page.GetVisualDescendants().OfType<RadioButton>()
                .Single(r => r.Content is StackPanel p && p.Children.OfType<TextBlock>().Any(t => t.Text == PrinterProfile.TypedTitle));
            choice.IsChecked = true;
            Press(page, "Next");
            Assert.Contains(page.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == PrinterProfile.TypedHelp);

            var boxes = page.GetVisualDescendants().OfType<TextBox>().ToList();
            Assert.Equal(2, boxes.Count);
            boxes[0].Text = "99.23";
            boxes[1].Text = "116";
            Press(page, "Use these");
            Assert.Contains(page.GetVisualDescendants().OfType<TextBlock>(), t => (t.Text ?? "").Contains("116% is outside it", StringComparison.Ordinal));

            boxes[1].Text = "99.41";
            Press(page, "Use these");
            Assert.Contains(page.GetVisualDescendants().OfType<TextBlock>(), t => (t.Text ?? "").Contains("typed in from another device's check on", StringComparison.Ordinal));
            Press(page, "Save and finish");
            Assert.True(finished);

            var kept = Phone.Settings.LoadPrinters().Single(p => p.Name == Printer);
            Assert.Equal(PrinterMethod.Typed, kept.Method);
            Assert.Equal("99.23% across, 99.41% down", kept.CopyWords);
        }
        finally
        {
            window.Close();
            Phone.Settings.DeletePrinter(Printer);
        }
    }
}

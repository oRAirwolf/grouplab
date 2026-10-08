using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Printing.Labels;
using GroupLab.Core.Rendering;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 389 section 1: with the Phomemo M834 chosen under "Print on", the Targets screen prints straight to it
/// through the one way out, and the bytes a fake port receives are the phone's bytes for the same page. No test opens a real port.
/// </summary>
public class Entry389M834Tests
{
    [AvaloniaFact]
    public async Task TheComputerSendsThePhonesBytesToTheM834()
    {
        var link = new AnsweringLink();
        TestDefaults.Outside.Forget();
        TestDefaults.Outside.SerialPrinter = _ => (link, null);
        var panel = TargetsScreen.Open();
        try
        {
            panel.Select("GL-SCALE-LTR-1.gltd.json");
            Dispatcher.UIThread.RunJobs();
            Assert.False(panel.M834Chosen);
            panel.ChoosePrintOn(1 + PrintPanel.ThermalChoices.ToList().FindIndex(c => c.Words.StartsWith("Phomemo M834", StringComparison.Ordinal)));
            Dispatcher.UIThread.RunJobs();
            Assert.True(panel.M834Chosen);
            Assert.Equal(M834Print.Profile.HeadDots, panel.Head!.Dots);
            var button = panel.GetLogicalDescendants().OfType<Button>().Single(b => Equals(b.Content, PrintPanel.M834PrintWords));
            Assert.True(button.IsEffectivelyVisible);

            await panel.PrintOnM834();
            Dispatcher.UIThread.RunJobs();

            Assert.Contains(TestDefaults.Outside.Asked, a => a == ("serial printer", M834Print.NameHint));
            var phone = SceneBuilder.Build(panel.Sheets.Single(s => s.File.EndsWith("GL-SCALE-LTR-1.gltd.json", StringComparison.Ordinal)).Definition,
                new RenderOptions(PrintNote: SceneBuilder.ActualSizeNote)).Pages.Select(p => M834Print.Encode(p, PaperForm.Roll)).ToList();
            Assert.Equal(phone.SelectMany(p => p), link.Written);
            Assert.Equal(StatusKind.Success, panel.StatusState);
            Assert.StartsWith("The M834 printed the page.", panel.StatusText, StringComparison.Ordinal);
        }
        finally
        {
            TestDefaults.Outside.Forget();
            panel.Close();
        }
    }

    [AvaloniaFact]
    public async Task AnM834ThatIsNotPairedIsSaidInPlainWords()
    {
        TestDefaults.Outside.Forget();
        var panel = TargetsScreen.Open();
        try
        {
            panel.Select("GL-SCALE-LTR-1.gltd.json");
            panel.ChoosePrintOn(1 + PrintPanel.ThermalChoices.ToList().FindIndex(c => c.Words.StartsWith("Phomemo M834", StringComparison.Ordinal)));
            Dispatcher.UIThread.RunJobs();
            await panel.PrintOnM834();
            Assert.Equal(StatusKind.Alert, panel.StatusState);
            Assert.Equal(SerialPrinterWords.NotPaired(M834Print.NameHint), panel.StatusText);

            // Off Windows the choice stays, and says it is not available on this computer yet.
            var words = panel.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
            Assert.Contains(OperatingSystem.IsWindows() ? PrintPanel.M834PairWords : SerialPrinterWords.NotHere(M834Print.NameHint), words);
        }
        finally
        {
            panel.Close();
        }
    }

    private sealed class AnsweringLink : IPrinterLink
    {
        private bool owed;

        public List<byte> Written { get; } = [];

        public Task WriteAsync(ReadOnlyMemory<byte> chunk, CancellationToken token)
        {
            Written.AddRange(chunk.ToArray());
            owed = true;
            return Task.CompletedTask;
        }

        public IReadOnlyList<byte[]> TakeAnswers()
        {
            if (!owed)
            {
                return [];
            }

            owed = false;
            return [PrinterFinish.M834Printed];
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

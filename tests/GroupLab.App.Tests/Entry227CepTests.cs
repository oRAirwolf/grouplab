using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 227 section 3: CEP 99 as a toggle beside the others, drawn and listed, and a circle for any percent under
/// Advanced, drawn and listed like the others and remembered; a percent out of range is refused with what it wants.
/// </summary>
public class Entry227CepTests
{
    [AvaloniaFact]
    public void Cep99AndAPercentOfOnesOwnAreDrawnListedAndRemembered()
    {
        var window = Entry204Tests.Sample();
        string store = window.SettingsStore.Path;
        try
        {
            Assert.False(window.Cep99Toggle.IsChecked == true);
            Assert.DoesNotContain("CEP 99", window.FigureNames);

            window.Cep99Toggle.IsChecked = true;
            Dispatcher.UIThread.RunJobs();
            Assert.Contains("CEP 99", window.FigureNames);
            Assert.NotNull(window.Plot.Cep99Inches);
            Assert.True(window.Plot.Shown.Cep99);

            window.SetCepPercent("150");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("Type a percent from 1 to 99.9.", window.CepPercentSaid);
            Assert.Null(window.Plot.Shown.CustomPercent);

            window.SetCepPercent("97.5");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(97.5, window.Plot.Shown.CustomPercent);
            Assert.NotNull(window.Plot.CustomCepInches);
            Assert.True(window.Plot.CustomCepInches > window.Plot.Cep95Inches && window.Plot.CustomCepInches < window.Plot.Cep99Inches);
            Assert.Contains(window.FigureNames, n => n.StartsWith("CEP 97", StringComparison.Ordinal));

            var remembered = window.SettingsStore.LoadPlotMarks();
            Assert.True(remembered.Cep99);
            Assert.Equal(97.5, remembered.CustomPercent);

            window.SetCepPercent("");
            Dispatcher.UIThread.RunJobs();
            Assert.Null(window.SettingsStore.LoadPlotMarks().CustomPercent);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.DeleteFile(store);
        }
    }
}

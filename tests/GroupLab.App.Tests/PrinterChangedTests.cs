using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 291 section 5.2: a printer check is tied to the prints it measured. A person who calibrates or services the
/// printer says so under Printers; the check is kept for the sheets printed before, and every result it corrects says so and offers another.
/// </summary>
public class PrinterChangedTests
{
    private static readonly DateOnly Checked = new(2026, 9, 29);

    [Fact]
    public void APrinterMarkedChangedKeepsItsCheckAndSaysItMayBeStale()
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        var home = new PrinterProfile("My printer", 1.0048, 1.0038, PrinterMethod.Card, Checked, PrinterProfile.CardUncertainty);
        var garage = new PrinterProfile("Garage laser", 0.992, 0.994, PrinterMethod.Scan, Checked, PrinterProfile.ScanUncertainty);
        store.SavePrinter(garage);
        store.SavePrinter(home);
        Assert.Null(store.PrinterForPhotos()!.Stale(Checked));

        store.MarkPrinterChanged("My printer", Checked);

        var chosen = store.PrinterForPhotos();
        Assert.NotNull(chosen);
        Assert.Equal(home.Scale, chosen.Scale);
        Assert.Equal(Checked, chosen.ChangedOn);
        Assert.NotNull(chosen.Stale(Checked.AddDays(1)));
        Assert.Null(store.LoadPrinters().Single(p => p.Name == "Garage laser").ChangedOn);

        // A new check under the same name replaces the old one, and is fresh.
        store.SavePrinter(home with { MeasuredOn = Checked.AddDays(2) });
        Assert.Null(store.PrinterForPhotos()!.Stale(Checked.AddDays(2)));
    }
}

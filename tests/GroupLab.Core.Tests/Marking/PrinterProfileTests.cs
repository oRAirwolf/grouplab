using GroupLab.Cli.Imaging;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 271: a printer's scale, measured once from a scan or a ruler, gives a photograph of its sheets real inches.
/// </summary>
public class PrinterProfileTests
{
    private const double Dpi = 300;
    private const double Printed = 0.962;
    private static readonly DateOnly Today = new(2026, 9, 28);

    /// <summary>GL-CF25-LTR printed at 96.2 percent: one page inch is 288.6 pixels at 300 dpi.</summary>
    private static GrayImage Small() => SceneRasterizer.Rasterize(SceneBuilder.Build(BuiltIns.Load("GL-CF25-LTR.gltd.json")).Pages[0], Dpi * Printed);

    private static ImageMetadata Photo(GrayImage image) => new("JPEG", image.Width, image.Height, null, null, "Test", "Phone", 1, 6.25, 24);

    private static ImageMetadata Scan(GrayImage image) => new("PNG", image.Width, image.Height, Dpi, Dpi, null, null, null, null, null);

    /// <summary>The distance between two bulls as the result reports it, in inches.</summary>
    private static double Between(AutomaticResult result, int a, int b)
    {
        var bulls = result.Definition!.Bulls;
        var sheet = Assert.IsType<SheetReference>(result.Scale);
        var p = sheet.ToTarget(sheet.Mapping.ToImage(new PointD(bulls[a].X, bulls[a].Y)));
        var q = sheet.ToTarget(sheet.Mapping.ToImage(new PointD(bulls[b].X, bulls[b].Y)));
        return Math.Sqrt(((p.X - q.X) * (p.X - q.X)) + ((p.Y - q.Y) * (p.Y - q.Y)));
    }

    private static double Drawn(AutomaticResult result, int a, int b)
    {
        var bulls = result.Definition!.Bulls;
        return Math.Sqrt(Math.Pow(bulls[a].X - bulls[b].X, 2) + Math.Pow(bulls[a].Y - bulls[b].Y, 2)) / 254;
    }

    /// <summary>Entry 271 section 5: a sheet printed at 96.2 percent and photographed reads its true size once the profile is applied.</summary>
    [Fact]
    public void APhotographReadsTrueSizeWithTheProfile()
    {
        var image = Small();
        var printer = new PrinterProfile("My printer", Printed, Printed, PrinterMethod.Scan, Today, 0.001);
        var result = AutomaticMarking.Run(image, image, Photo(image), BuiltIns.Load("GL-CF25-LTR.gltd.json"), new OpenCvSharpBackend(), printer: printer);
        Assert.Null(result.Failure);
        var sheet = Assert.IsType<SheetReference>(result.Scale);
        Assert.Equal(Printed, sheet.PrintScale);
        Assert.True(sheet.RealInches);
        Assert.Equal("Corrected for My printer, 96.2%, checked 28 September", sheet.ScaleFrom);
        Assert.Equal(sheet.ScaleFrom, DetectionAdvice.PrintScale(result.Measurement, sheet.ScaleFrom));
        Assert.InRange(Between(result, 0, 4), (Drawn(result, 0, 4) * Printed) - 0.002, (Drawn(result, 0, 4) * Printed) + 0.002);
    }

    /// <summary>With no profile, the same photograph stays in the sheet's own inches and says so, as before entry 271.</summary>
    [Fact]
    public void APhotographWithNoProfileKeepsTheSheetsInches()
    {
        var image = Small();
        var result = AutomaticMarking.Run(image, image, Photo(image), BuiltIns.Load("GL-CF25-LTR.gltd.json"), new OpenCvSharpBackend());
        var sheet = Assert.IsType<SheetReference>(result.Scale);
        Assert.Null(sheet.PrintScale);
        Assert.Null(sheet.ScaleFrom);
        Assert.Equal(DetectionAdvice.SheetInches, DetectionAdvice.PrintScale(result.Measurement, sheet.ScaleFrom));
        Assert.InRange(Between(result, 0, 4), Drawn(result, 0, 4) - 0.002, Drawn(result, 0, 4) + 0.002);
    }

    /// <summary>
    /// A scan measures its own scale and the profile is not used; the scan's scale makes a profile; and a ruler read to a sixteenth at each
    /// end over bull 1 to bull 5 makes one that agrees with it.
    /// </summary>
    [Fact]
    public void AProfileFromAScanAndFromARulerAgree()
    {
        var image = Small();
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var wrong = new PrinterProfile("Office", 1.03, 1.03, PrinterMethod.Ruler, Today, 0.002);
        var result = AutomaticMarking.Run(image, image, Scan(image), definition, new OpenCvSharpBackend(), printer: wrong);
        var sheet = Assert.IsType<SheetReference>(result.Scale);
        Assert.InRange(sheet.PrintScale!.Value, Printed - 0.0005, Printed + 0.0005);
        Assert.Null(sheet.ScaleFrom);

        var scan = PrinterProfile.FromScan(null, result.Measurement.Scale, Today);
        Assert.NotNull(scan);
        Assert.Equal(PrinterProfile.DefaultName, scan.Name);

        var span = RulerSpan.Of(definition);
        Assert.NotNull(span);
        Assert.Equal(("1", "5"), (span.From, span.To));
        double read = Math.Round(span.DrawnInches * Printed * 16) / 16;
        var ruler = PrinterProfile.FromRuler("My printer", read, span.DrawnInches, Today);
        Assert.NotNull(ruler);
        Assert.True(scan.AgreesWith(ruler), $"scan {scan.Scale:0.0000} ± {scan.Uncertainty:0.0000}, ruler {ruler.Scale:0.0000} ± {ruler.Uncertainty:0.0000}");
        Assert.Equal(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"Corrected for My printer, {ruler.Scale * 100:0.0}%, checked 28 September"), ruler.Line);
    }

    /// <summary>
    /// Entry 273: a printer that prints differently across and down corrects each axis by its own figure, and says both in its line.
    /// </summary>
    [Fact]
    public void EachAxisIsCorrectedByItsOwnFigure()
    {
        var mapping = new HomographyMapping(new Homography([1, 0, 0, 0, 1, 0, 0, 0, 1]));
        var printer = new PrinterProfile("My printer", 0.992, 0.994, PrinterMethod.Card, Today, PrinterProfile.CardUncertainty);
        var sheet = new SheetReference(mapping, "38 of 38 markers found").CorrectedBy(printer);
        var at = sheet.ToTarget(new PointD(2540, 2540));
        Assert.Equal(9.92, at.X, 9);
        Assert.Equal(9.94, at.Y, 9);
        Assert.Equal("Corrected for My printer, 99.2 by 99.4%, checked 28 September", sheet.ScaleFrom);
        Assert.Equal("My printer prints at 99.2% across and 99.4% down (plus or minus 0.3%)", printer.Result);
        Assert.Equal("My printer prints a little small", printer.Headline);
    }

    /// <summary>Entry 273: the check page's caliper and ruler readings, typed, each over its drawn length.</summary>
    [Fact]
    public void TypedLengthsMakeAProfile()
    {
        double mm = 1 / 25.4;
        var caliper = PrinterProfile.FromLengths(null, PrinterMethod.Caliper, 148.8 * mm, 150 * mm, 149.1 * mm, 150 * mm, Today);
        Assert.NotNull(caliper);
        Assert.Equal(0.992, caliper.Across, 9);
        Assert.Equal(0.994, caliper.Down, 9);
        Assert.True(caliper.Uncertainty < 0.0015, $"{caliper.Uncertainty}");
        var ruler = PrinterProfile.FromLengths(null, PrinterMethod.Ruler, 188.5 * mm, 190 * mm, 248.5 * mm, 250 * mm, Today);
        Assert.NotNull(ruler);
        Assert.True(caliper.AgreesWith(ruler));
        Assert.Null(PrinterProfile.FromLengths(null, PrinterMethod.Scan, 1, 1, 1, 1, Today));
        Assert.Null(PrinterProfile.FromLengths(null, PrinterMethod.Caliper, 75 * mm, 150 * mm, 150 * mm, 150 * mm, Today));
    }

    /// <summary>A typing slip is refused rather than saved: no printer prints a sheet at half or twice its size.</summary>
    [Fact]
    public void ARulerSlipIsRefused()
    {
        Assert.Null(PrinterProfile.FromRuler(null, 3, 6, Today));
        Assert.Null(PrinterProfile.FromRuler(null, 0, 6, Today));
        Assert.Null(PrinterProfile.FromRuler(null, double.NaN, 6, Today));
        Assert.NotNull(PrinterProfile.FromRuler(null, 5.75, 6, Today));
    }

    /// <summary>A ruler reading is read the ways a person types it, and anything else is asked again.</summary>
    [Theory]
    [InlineData("5.75", 5.75)]
    [InlineData("5 3/4", 5.75)]
    [InlineData("5.75 in", 5.75)]
    [InlineData("5.75\"", 5.75)]
    [InlineData("146.05 mm", 5.75)]
    [InlineData("14.605cm", 5.75)]
    [InlineData("23/4", 5.75)]
    [InlineData("five", null)]
    [InlineData("", null)]
    [InlineData("-5", null)]
    [InlineData("5 3/0", null)]
    public void ARulerReadingIsReadAsTyped(string typed, double? inches)
    {
        var read = RulerSpan.ReadInches(typed);
        if (inches is null)
        {
            Assert.Null(read);
        }
        else
        {
            Assert.NotNull(read);
            Assert.Equal(inches.Value, read.Value, 6);
        }
    }

    /// <summary>A profile is saved and read back whole, and a damaged one is dropped rather than applied.</summary>
    [Fact]
    public void AProfileIsKeptWhole()
    {
        var profile = new PrinterProfile("Garage laser", 0.9921, 0.9943, PrinterMethod.Ruler, Today, 0.0052);
        Assert.Equal(profile, PrinterProfile.FromJson(profile.ToJson()));
        var damaged = profile.ToJson();
        damaged["across"] = 2.5;
        Assert.Null(PrinterProfile.FromJson(damaged));
        Assert.Null(PrinterProfile.FromJson(null));
        var changed = profile.Changed(Today.AddDays(3));
        Assert.Equal(changed, PrinterProfile.FromJson(changed.ToJson()));
        Assert.Null(profile.ToJson()["changedOn"]);
    }

    /// <summary>
    /// Entry 291 section 5.2: a check measures the sheets printed before it. Once the person says the printer was calibrated or serviced, or
    /// the check is more than about six months old, the result says so and offers another check; a fresh check is trusted.
    /// </summary>
    [Fact]
    public void ACheckIsTiedToThePrintsItMeasured()
    {
        var profile = new PrinterProfile("My printer", 1.0048, 1.0038, PrinterMethod.Card, new DateOnly(2026, 9, 29), PrinterProfile.CardUncertainty);
        Assert.Null(profile.Stale(new DateOnly(2026, 9, 29)));
        Assert.Null(profile.Stale(new DateOnly(2027, 3, 1)));
        Assert.Equal("Corrected for My printer, 100.5 by 100.4%, checked 29 September", profile.Line);

        var calibrated = profile.Changed(new DateOnly(2026, 9, 29));
        Assert.Equal(
            "My printer was calibrated or serviced on 29 September, after its check on 29 September. Sheets printed before then are still corrected rightly; for sheets printed since, check it again on a sheet printed now.",
            calibrated.Stale(new DateOnly(2026, 9, 30)));

        // The correction itself does not change: sheets printed before the calibration still print at the size it measured.
        Assert.Equal(profile.Scale, calibrated.Scale);

        Assert.Equal(
            "My printer was last checked on 29 September, more than six months ago. A printer can drift; check it again on a sheet printed now.",
            profile.Stale(new DateOnly(2027, 4, 1)));

        // A check made after the change is fresh again.
        var again = calibrated with { MeasuredOn = new DateOnly(2026, 10, 2), ChangedOn = null };
        Assert.Null(again.Stale(new DateOnly(2026, 10, 2)));
    }

    /// <summary>Entry 291 section 5.2: both Settings screens say a check may be stale, offer to mark the printer changed, and so do both results.</summary>
    [Fact]
    public void BothSettingsScreensAndBothResultsOfferANewCheck()
    {
        foreach (var file in new[] { Path.Combine("src", "GroupLab.App", "MainWindow.Printer.cs"), Path.Combine("mobile", "GroupLab.Mobile", "SettingsView.cs") })
        {
            string text = File.ReadAllText(Path.Combine(Repo.Root, file));
            Assert.Contains("PrinterProfile.ChangedWords", text, StringComparison.Ordinal);
            Assert.Contains("MarkPrinterChanged", text, StringComparison.Ordinal);
            Assert.Contains(".Stale(", text, StringComparison.Ordinal);
        }

        foreach (var file in new[] { Path.Combine("src", "GroupLab.App", "MainWindow.Printer.cs"), Path.Combine("mobile", "GroupLab.Mobile", "PrinterCard.cs") })
        {
            string text = File.ReadAllText(Path.Combine(Repo.Root, file));
            Assert.Contains("from == printer.Line && printer.Stale(Today())", text, StringComparison.Ordinal);
            Assert.Contains("\"Check your printer\"", text, StringComparison.Ordinal);
        }
    }

    /// <summary>A saved session says which printer's scale corrected it.</summary>
    [Fact]
    public void ASavedSessionSaysWhichPrinterCorrectedIt()
    {
        var sheet = new SheetReference(new HomographyMapping(new Homography([1, 0, 0, 0, 1, 0, 0, 0, 1])), "38 of 38 markers found")
        {
            PrintScale = 0.993,
            PrintScaleAcross = 0.992,
            PrintScaleDown = 0.994,
            ScaleFrom = "Corrected for My printer, 99.2 by 99.4%",
        };
        var session = new MarkingSession();
        session.Open("sheet.jpg", 1);
        session.SetScale(sheet);
        session.AddShot(new PointD(1200, 900));
        var (state, _) = MarkingFile.Read(MarkingFile.Write(session.State));
        var read = Assert.IsType<SheetReference>(state.Scale);
        Assert.Equal(0.993, read.PrintScale);
        Assert.Equal((0.992, 0.994), (read.PrintScaleAcross, read.PrintScaleDown));
        Assert.Equal(sheet.ScaleFrom, read.ScaleFrom);
    }
}

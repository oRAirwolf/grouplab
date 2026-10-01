using System.IO.Compression;
using System.Security;
using System.Text;
using System.Text.Json;
using GroupLab.Core.Records;

namespace GroupLab.Core.Tests.Records;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 334: chronograph workbooks, in the two layouts Alan's own exports have, written here as .xlsx so nothing of
/// his is committed. Format B is the Garmin Xero's (one sheet per string, shots down the rows, a footer of its own figures); format A is a
/// 2023 radar chronograph's (shots across the columns, a location block above them that must never be read). His folder, where it is on
/// this computer, is read whole by the last test.
/// </summary>
public class ChronographWorkbookTests
{
    /// <summary>A minimal .xlsx: each sheet a name and rows of cells; a cell that parses as a number is written as one where asked.</summary>
    private static MemoryStream Workbook(params (string Name, string[][] Rows, bool Numbers)[] sheets)
    {
        var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            void Put(string path, string text)
            {
                using var w = new StreamWriter(zip.CreateEntry(path).Open(), new UTF8Encoding(false));
                w.Write(text);
            }

            string overrides = string.Concat(sheets.Select((_, i) => $"<Override PartName=\"/xl/worksheets/sheet{i + 1}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>"));
            Put("[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">"
                + "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/>"
                + "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" + overrides + "</Types>");
            Put("_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"
                + "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            Put("xl/workbook.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>"
                + string.Concat(sheets.Select((s, i) => $"<sheet name=\"{SecurityElement.Escape(s.Name)}\" sheetId=\"{i + 1}\" r:id=\"rId{i + 1}\"/>")) + "</sheets></workbook>");
            Put("xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"
                + string.Concat(sheets.Select((_, i) => $"<Relationship Id=\"rId{i + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{i + 1}.xml\"/>")) + "</Relationships>");
            for (int i = 0; i < sheets.Length; i++)
            {
                var rows = new StringBuilder();
                for (int r = 0; r < sheets[i].Rows.Length; r++)
                {
                    rows.Append($"<row r=\"{r + 1}\">");
                    for (int c = 0; c < sheets[i].Rows[r].Length; c++)
                    {
                        string cell = sheets[i].Rows[r][c], at = $"{(char)('A' + c)}{r + 1}";
                        rows.Append(sheets[i].Numbers && double.TryParse(cell, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _)
                            ? $"<c r=\"{at}\"><v>{cell}</v></c>"
                            : $"<c r=\"{at}\" t=\"inlineStr\"><is><t>{SecurityElement.Escape(cell)}</t></is></c>");
                    }

                    rows.Append("</row>");
                }

                Put($"xl/worksheets/sheet{i + 1}.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>"
                    + rows + "</sheetData></worksheet>");
            }
        }

        stream.Position = 0;
        return stream;
    }

    private static readonly string[] XeroHeader = ["#", "Speed (FPS)", "Δ Avg (FPS)", "KE (FT-LB)", "Power Factor (kgr⋅ft/s)", "Time", "Clean Bore", "Cold Bore", "Shot Notes"];

    /// <summary>A Xero string as the September 2026 exports are: text numbers with thousands separators, a deleted shot, a shot left out.</summary>
    private static string[][] XeroString(string name) =>
    [
        [name],
        XeroHeader,
        ["1", "2,853.4", "-2.0", "3,001.2", "855.1", "13:16:02", "", "TRUE", ""],
        ["2", "2,861.0", "5.6", "3,017.3", "857.4", "13:16:31", "", "", "flyer called"],
        ["4", "2,851.8", "-3.6", "2,997.9", "854.6", "13:17:40", "", "", ""],
        ["5", "2,790.2", "--", "2,869.5", "836.2", "13:18:05", "", "", ""],
        ["-"],
        ["AVERAGE SPEED", "2,855.4"],
        ["AVERAGE POWER FACTOR", "855.7"],
        ["STD DEV", "4.9"],
        ["SPREAD", "9.2"],
        ["Projectile Weight (GRAINS)", "135.0"],
        ["AVG KINETIC ENERGY", "3,005.5"],
        ["Session Note", ""],
        ["-"],
        ["Date", "SEPTEMBER 19, 2026 13:16"],
    ];

    [Fact]
    public void AXeroWorkbookGivesEachSheetAsAStringWithItsNumberingAndItsOwnFigures()
    {
        using var file = Workbook(("6.5 CM 135 sheet name cut to 31", XeroString("6.5 CM 135 ELD-M, 41.5 gr H4350"), false), ("second", XeroString("second string"), false));
        var strings = ChronographFiles.ReadFile(file, "Sessions_SEP_2026-SEP_2026.xls".Replace(".xls", ".xlsx", StringComparison.Ordinal), out int passedOver);
        Assert.Equal(0, passedOver);
        Assert.Equal(2, strings.Count);
        var first = strings[0];
        Assert.Equal(ChronographFormat.GarminXero, first.Format);
        Assert.False(first.Experimental);
        Assert.Equal("6.5 CM 135 ELD-M, 41.5 gr H4350", first.Name);
        Assert.Equal([1, 2, 4, 5], first.Shots.Select(s => s.Number));
        Assert.Equal([2853.4, 2861.0, 2851.8, 2790.2], first.VelocitiesFps);
        Assert.True(first.Shots[0].ColdBore);
        Assert.Equal("flyer called", first.Shots[1].Note);
        Assert.True(first.Shots[3].LeftOutByChronograph);
        Assert.Contains("1 deleted on the chronograph", first.Said, StringComparison.Ordinal);
        Assert.Contains("1 of them left out of the chronograph's own figures", first.Said, StringComparison.Ordinal);
        Assert.Null(first.Disagrees);
        Assert.Equal(135.0, first.Conditions!.ProjectileGrains);
        Assert.Equal("second string", strings[1].Name);
    }

    [Fact]
    public void AFooterThatDisagreesWithTheShotsIsSaid()
    {
        var rows = XeroString("zero SD");
        rows[9] = ["STD DEV", "0.0"];
        using var file = Workbook(("s", rows, false));
        var read = Assert.Single(ChronographFiles.ReadFile(file, "x.xlsx", out _));
        Assert.Equal("The file's own figures differ from its shots: SD 0.0 against 4.9.", read.Disagrees);
    }

    [Fact]
    public void AXeroCsvWithItsNameFirstAndAByteOrderMarkOnTheHeaderReads()
    {
        string csv = "Range day\n\uFEFF" + string.Join(",", XeroHeader) + "\n1,\"2,701.4\",-1.1,2833,472.7,14:03:11,,,\n2,2695.9,-6.6,2822,471.8,14:04:02,,,\n-,,,\nAVERAGE SPEED,2698.7\n";
        using var file = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var read = Assert.Single(ChronographFiles.ReadFile(file, "range.csv", out _));
        Assert.Equal(ChronographFormat.GarminXero, read.Format);
        Assert.Equal("Range day", read.Name);
        Assert.Equal([2701.4, 2695.9], read.VelocitiesFps);
        Assert.Null(read.Disagrees);
    }

    /// <summary>Entry 339: the Xero's metric export writes its unit as MPS, with KE in joules and the power factor in N⋅s.</summary>
    [Fact]
    public void TheXerosMetricExportIsReadInMetresASecond()
    {
        var rows = XeroString("metric export");
        rows[1] = ["#", "Speed (MPS)", "Δ Avg (MPS)", "KE (J)", "Power Factor (N⋅s)", "Time", "Clean Bore", "Cold Bore", "Shot Notes"];
        rows[2][1] = "870.0";
        using var file = Workbook(("m", rows, false));
        var read = Assert.Single(ChronographFiles.ReadFile(file, "m.xlsx", out _));
        Assert.Equal(870.0 / 0.3048, read.VelocitiesFps[0], 6);
        Assert.Contains("in m/s", read.Said, StringComparison.Ordinal);
    }

    [Fact]
    public void MetresASecondInTheHeaderAreConverted()
    {
        var rows = XeroString("metric");
        rows[1] = [.. XeroHeader.Select(h => h.Replace("(FPS)", "(M/S)", StringComparison.Ordinal))];
        rows[2][1] = "870.0";
        using var file = Workbook(("m", rows, false));
        var read = Assert.Single(ChronographFiles.ReadFile(file, "m.xlsx", out _));
        Assert.Equal(870.0 / 0.3048, read.VelocitiesFps[0], 6);
    }

    /// <summary>
    /// Format A, with the location block the older files carry. The reader takes the shots, the statistics and the weather, and nothing of
    /// the place: the whole result written out holds no place name and no coordinate.
    /// </summary>
    [Fact]
    public void TheBulletSeekerExportReadsItsShotsAndNeverItsLocation()
    {
        string[][] rows =
        [
            ["Name", "18.6 ARC 105 BTHP"],
            ["Created", "1/1/2024 11:13:15 AM"],
            [""],
            ["Location"],
            ["Name", "Testplace Range"],
            ["Latitude", "39.9999"],
            ["Longitude", "-104.9999"],
            [""],
            ["Statistics", "Speed [fps]", "Energy [ft lb]"],
            ["Min", "2483.2", "1460.6"],
            ["Max", "2518.4", "1524.7"],
            ["Avg", "2500.4", "1497.3"],
            ["Deviation", "12.7", "15.2"],
            ["Weather"],
            ["Temperature", "31.8", "°F"],
            ["Pressure", "30.1", "inHg"],
            ["Humidity", "0.0174", "%"],
            ["Notes", ""],
            [""],
            ["Shot Number", "Shot 1", "Shot 2", "Shot 3", "Shot 4", "Shot 5", "Shot 6", "Shot 7"],
            ["Time", "1/1/2024 11:10:02 PM", "", "", "", "", "", ""],
            ["Mean Speed [fps]", "2507", "2500", "2504", "2483", "2507", "2518", "2484"],
            ["Measurements [fps]"],
            ["", "2506.1", "2499.8"],
        ];
        using var file = Workbook(("Data", rows, true), ("Chart", [], false));
        var strings = ChronographFiles.ReadFile(file, "radar.xlsx", out int passedOver);
        var read = Assert.Single(strings);
        Assert.Equal(1, passedOver);
        Assert.Equal(ChronographFormat.BulletSeeker, read.Format);
        Assert.True(read.Experimental);
        Assert.Equal([2507, 2500, 2504, 2483, 2507, 2518, 2484], read.VelocitiesFps);
        Assert.Equal("18.6 ARC 105 BTHP", read.Name);
        Assert.Null(read.Disagrees);
        Assert.Equal(1.74, read.Conditions!.HumidityPct!.Value, 6);
        Assert.Equal(31.8, read.Conditions.TemperatureF);
        Assert.DoesNotContain("Xero", read.Said, StringComparison.OrdinalIgnoreCase);

        string everything = JsonSerializer.Serialize(read);
        Assert.DoesNotContain("Testplace", everything, StringComparison.Ordinal);
        Assert.DoesNotContain("39.9999", everything, StringComparison.Ordinal);
        Assert.DoesNotContain("104.9999", everything, StringComparison.Ordinal);
        Assert.DoesNotContain("Latitude", everything, StringComparison.Ordinal);
    }

    /// <summary>
    /// Alan's own exports, where they are on this computer (entry 334): every file reads, and every Garmin Xero string has shots. Not on CI,
    /// which never has them; nothing of them is committed.
    /// </summary>
    [Fact]
    public void EveryOneOfAlansExportsReadsWhereTheyAreHere()
    {
        string folder = @"C:\Dev\grouplab-local\chronograph-samples\garmin-xero";
        if (!Directory.Exists(folder))
        {
            return;
        }

        int xero = 0;
        foreach (string path in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).Where(f => ChronographFiles.Extensions.Contains(Path.GetExtension(f).ToLowerInvariant())))
        {
            using var stream = File.OpenRead(path);
            var strings = ChronographFiles.ReadFile(stream, path, out _);
            Assert.True(strings.Count > 0, Path.GetRelativePath(folder, path) + " gave no string");
            foreach (var s in strings.Where(s => s.Format == ChronographFormat.GarminXero))
            {
                xero++;
                Assert.True(s.VelocitiesFps.Count > 0, Path.GetRelativePath(folder, path) + " has a Xero string with no shots");
            }
        }

        Assert.True(xero > 0);
    }
}

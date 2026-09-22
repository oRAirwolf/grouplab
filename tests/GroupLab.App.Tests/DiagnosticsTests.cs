using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Marking;
using OpenCvSharp;
using LogLevel = GroupLab.App.Diagnostics.LogLevel;

namespace GroupLab.App.Tests;

/// <summary>A small JPEG carrying the metadata a log must never repeat: a GPS block, a maker note, an artist and a description.</summary>
internal static class PersonalJpeg
{
    public const string Artist = "Jane Q Contributor";
    public const string Description = "Home range behind 12 Oak Lane";
    public const string MakerNote = "MAKERNOTE-SECRET-4471";
    public const string LatitudeSeconds = "5123";
    public const string LongitudeSeconds = "4488";

    public static byte[] Bytes()
    {
        using var image = new Mat(48, 64, MatType.CV_8UC3, new Scalar(200, 200, 200));
        Cv2.ImEncode(".jpg", image, out byte[] jpeg);
        static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s + "\0");
        static byte[] Rationals(params (uint N, uint D)[] values) => [.. values.SelectMany(v => BitConverter.GetBytes(v.N).Concat(BitConverter.GetBytes(v.D)))];
        byte[] tiff = Tiff(
            [(0x010E, 2, Ascii(Description)), (0x010F, 2, Ascii("TestPhone")), (0x0110, 2, Ascii("Model 7")), (0x013B, 2, Ascii(Artist))],
            [(0x920A, 5, Rationals((43, 10))), (0x927C, 7, Encoding.ASCII.GetBytes(MakerNote))],
            [(0x0001, 2, Ascii("N")), (0x0002, 5, Rationals((47, 1), (36, 1), (5123, 100))), (0x0003, 2, Ascii("W")), (0x0004, 5, Rationals((122, 1), (19, 1), (4488, 100)))]);
        byte[] body = [.. "Exif\0\0"u8.ToArray(), .. tiff];
        return [jpeg[0], jpeg[1], 0xFF, 0xE1, (byte)((body.Length + 2) >> 8), (byte)(body.Length + 2), .. body, .. jpeg.AsSpan(2)];
    }

    /// <summary>A little-endian TIFF: a primary IFD pointing at an EXIF IFD and a GPS IFD, with values laid out after the three.</summary>
    private static byte[] Tiff(List<(int Tag, int Type, byte[] Value)> primary, List<(int Tag, int Type, byte[] Value)> exif, List<(int Tag, int Type, byte[] Value)> gps)
    {
        var ifds = new[] { new List<(int Tag, int Type, byte[] Value)>(primary) { (0x8769, 4, new byte[4]), (0x8825, 4, new byte[4]) }, exif, gps };
        int[] starts = new int[3];
        int at = 8;
        for (int i = 0; i < 3; i++)
        {
            starts[i] = at;
            at += 2 + (12 * ifds[i].Count) + 4;
        }

        var data = new List<byte>();
        var output = new List<byte>();
        output.AddRange("II"u8.ToArray());
        output.AddRange(BitConverter.GetBytes((ushort)42));
        output.AddRange(BitConverter.GetBytes(8u));
        for (int i = 0; i < 3; i++)
        {
            output.AddRange(BitConverter.GetBytes((ushort)ifds[i].Count));
            foreach (var (tag, type, value) in ifds[i].OrderBy(e => e.Tag))
            {
                int size = type switch { 3 => 2, 4 => 4, 5 => 8, _ => 1 };
                output.AddRange(BitConverter.GetBytes((ushort)tag));
                output.AddRange(BitConverter.GetBytes((ushort)type));
                output.AddRange(BitConverter.GetBytes((uint)(value.Length / size)));
                if (tag is 0x8769 or 0x8825)
                {
                    output.AddRange(BitConverter.GetBytes((uint)starts[tag == 0x8769 ? 1 : 2]));
                }
                else if (value.Length <= 4)
                {
                    output.AddRange([.. value, .. new byte[4 - value.Length]]);
                }
                else
                {
                    output.AddRange(BitConverter.GetBytes((uint)(at + data.Count)));
                    data.AddRange(value);
                }
            }

            output.AddRange(BitConverter.GetBytes(0u));
        }

        return [.. output, .. data];
    }
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 41 sections 2, 3 and 8: the log never carries a location, a photograph's metadata block or a path; it
/// rotates to the newest twenty files; and a log that cannot be written never stops the application.
/// </summary>
public partial class DiagnosticsTests
{
    /// <summary>A directory several levels deep whose names identify a person, as a real user's photographs folder does.</summary>
    private static string DeepDirectory() =>
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"grouplab-privacy-{Guid.NewGuid():N}", "Users", PersonalJpeg.Artist, "Range photos", "2026")).FullName;

    private static MainWindow Window(string settings)
    {
        var store = new AppSettingsStore(settings);
        store.SaveUnits(UnitSettings.Imperial);
        var window = new MainWindow(store) { Width = 1400, Height = 900 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    /// <summary>Opens a file through the window with a log of the test's own, and returns everything the log wrote.</summary>
    private static string LogOf(Action<MainWindow> act)
    {
        string root = Path.Combine(Path.GetTempPath(), $"grouplab-log-{Guid.NewGuid():N}");
        string settings = Path.Combine(root, "settings.json");
        var previous = DiagnosticLog.Current;
        var log = new DiagnosticLog(Path.Combine(root, "logs"), verbose: true);
        DiagnosticLog.Current = log;
        try
        {
            var window = Window(settings);
            act(window);
            window.Close();
            log.Dispose();
            return File.ReadAllText(log.FilePath!);
        }
        finally
        {
            DiagnosticLog.Current = previous;
            log.Dispose();
            GroupLab.Tests.Support.Temp.Delete(root);
        }
    }

    [AvaloniaFact]
    public void OpeningAPhotographLogsNoLocationNoMetadataBlockAndNoPath()
    {
        string directory = DeepDirectory();
        string photograph = Path.Combine(directory, "target.jpg");
        File.WriteAllBytes(photograph, PersonalJpeg.Bytes());
        try
        {
            string text = LogOf(window => window.OpenImage(photograph));

            Assert.Contains("image.open", text, StringComparison.Ordinal);
            Assert.Contains("file=target.jpg", text, StringComparison.Ordinal);
            Assert.Contains("pathid=", text, StringComparison.Ordinal);
            Assert.Contains("make=TestPhone", text, StringComparison.Ordinal);
            foreach (string forbidden in new[] { "GPS", PersonalJpeg.LatitudeSeconds, PersonalJpeg.LongitudeSeconds, "Jane", "Oak Lane", "MAKERNOTE", "Range photos", Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar) })
            {
                Assert.DoesNotContain(forbidden, text, StringComparison.Ordinal);
            }
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetFullPath(Path.Combine(directory, "..", "..", "..", "..")));
        }
    }

    [AvaloniaFact]
    public void ALogFromFilesInADeepDirectoryHoldsNoAbsolutePathOrDriveLetter()
    {
        string directory = DeepDirectory();
        string photograph = Path.Combine(directory, "target.jpg");
        string marking = Path.Combine(directory, "broken.grouplab.json");
        File.WriteAllBytes(photograph, PersonalJpeg.Bytes());
        File.WriteAllText(marking, "{ \"format\": \"not-a-marking\" }");
        try
        {
            string text = LogOf(window =>
            {
                window.OpenImage(photograph);
                window.OpenMarking(marking);
                window.OpenMarking(Path.Combine(directory, "missing.grouplab.json"));
            });

            Assert.Contains("file=broken.grouplab.json", text, StringComparison.Ordinal);
            Assert.DoesNotMatch(DriveLetterPath(), text);
            Assert.DoesNotContain(directory, text, StringComparison.Ordinal);
            Assert.DoesNotContain(PersonalJpeg.Artist, text, StringComparison.Ordinal);
            Assert.DoesNotContain("/tmp/", text, StringComparison.Ordinal);
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetFullPath(Path.Combine(directory, "..", "..", "..", "..")));
        }
    }

    [Fact]
    public void TwentyFiveRunsLeaveTheNewestTwentyLogs()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"grouplab-rotation-{Guid.NewGuid():N}");
        try
        {
            var start = new DateTime(2026, 9, 15, 18, 0, 0, DateTimeKind.Utc);
            for (int run = 0; run < 25; run++)
            {
                using var log = new DiagnosticLog(directory, verbose: false, start.AddSeconds(run), 1000 + run);
                log.Write(LogLevel.Info, "app.start", [("run", run)]);
            }

            var names = Directory.GetFiles(directory, "grouplab-*.log").Select(Path.GetFileName).Order(StringComparer.Ordinal).ToList();
            Assert.Equal(DiagnosticLog.KeepFiles, names.Count);
            Assert.Equal("grouplab-20260915-180005-1005.log", names[0]);
            Assert.Equal("grouplab-20260915-180024-1024.log", names[^1]);
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(directory);
        }
    }

    [Fact]
    public void ALogThatCannotBeWrittenIsOffAndNeverThrows()
    {
        string blocker = Path.Combine(Path.GetTempPath(), $"grouplab-not-a-directory-{Guid.NewGuid():N}");
        File.WriteAllText(blocker, "a file where the log directory should be");
        try
        {
            using var log = new DiagnosticLog(blocker, verbose: true);
            Assert.False(log.IsEnabled);
            Assert.NotNull(log.DisabledReason);
            log.Write(LogLevel.Error, "app.crash", [("ex", "System.Exception")]);
            log.Flush();
        }
        finally
        {
            File.Delete(blocker);
        }
    }

    [Fact]
    public void AValueOrMessageShapedLikeAPathIsReplacedAndAValueWithASpaceIsQuoted()
    {
        Assert.Equal("Could not find file '<path>", DiagnosticLog.Scrub(@"Could not find file 'C:\Users\Jane Q\photo.jpg"));
        Assert.Equal("at X in <path>:line 4", DiagnosticLog.Scrub("at X in /home/jane/src/Thing.cs:line 4"));
        string line = DiagnosticLog.Format(new DateTime(2026, 9, 15, 18, 42, 7, 104, DateTimeKind.Utc), LogLevel.Info, "image.open", [("file", "a b.png"), ("w", 5100), ("dpi", 600.0), ("none", null)]);
        Assert.Equal("2026-09-15T18:42:07.104Z  INFO   image.open     file=\"a b.png\" w=5100 dpi=600", line);
    }

    [GeneratedRegex(@"(?<![A-Za-z])[A-Za-z]:[\\/]")]
    private static partial Regex DriveLetterPath();
}

using Avalonia.Headless.XUnit;
using GroupLab.App;
using GroupLab.Core.Trace;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 291 section 7.5: GroupLab Dev keeps every picture of a sitting, without its metadata, with its live record
/// and trace; a release build keeps nothing, and the switch deletes what was kept.
/// </summary>
public class SittingRecordTests
{
    /// <summary>A JPEG's skeleton: start, a JFIF segment, an EXIF segment standing for location and time, a comment, the scan, the end.</summary>
    private static byte[] Jpeg()
    {
        var bytes = new List<byte> { 0xFF, 0xD8 };
        void Segment(byte marker, byte[] body)
        {
            bytes.AddRange([0xFF, marker, (byte)((body.Length + 2) >> 8), (byte)((body.Length + 2) & 0xFF)]);
            bytes.AddRange(body);
        }

        Segment(0xE0, "JFIF\0\u0001\u0001\0\0\u0001\0\u0001\0\0"u8.ToArray());
        Segment(0xE1, "Exif\0\0GPS and a time"u8.ToArray());
        Segment(0xFE, "a comment"u8.ToArray());
        Segment(0xDB, new byte[65]);
        Segment(0xDA, new byte[10]);
        bytes.AddRange([1, 2, 3, 4, 0xFF, 0xD9]);
        return [.. bytes];
    }

    [Fact]
    public void AKeptPictureHasNoMetadataAndItsPictureDataUnchanged()
    {
        byte[] jpeg = Jpeg();
        byte[] kept = SittingRecord.JpegWithoutMetadata(jpeg);
        Assert.Equal(0xD8, kept[1]);
        Assert.False(System.Text.Encoding.Latin1.GetString(kept).Contains("Exif", StringComparison.Ordinal));
        Assert.False(System.Text.Encoding.Latin1.GetString(kept).Contains("comment", StringComparison.Ordinal));
        Assert.Contains("JFIF", System.Text.Encoding.Latin1.GetString(kept), StringComparison.Ordinal);
        Assert.Equal(jpeg[^16..], kept[^16..]);
        Assert.Empty(SittingRecord.JpegWithoutMetadata([0x89, 0x50, 0x4E, 0x47]));
    }

    [AvaloniaFact]
    public void OnlyGroupLabDevKeepsASittingAndTheSwitchDeletesIt()
    {
        var phone = new TestPhone();
        if (Phone.Platform is null)
        {
            Phone.Start(phone, Avalonia.Application.Current!, () => "US", null);
        }

        var platform = (TestPhone)Phone.Platform!;
        string picture = Path.Combine(platform.CacheFolder, "still-sitting.jpg");
        File.WriteAllBytes(picture, Jpeg());
        try
        {
            platform.IsDevBuild = false;
            Assert.Null(SittingRecord.Keep(picture, "live"));

            platform.IsDevBuild = true;
            Phone.Settings.SaveKeepSitting(true);
            string folder = SittingRecord.Keep(picture, "say=Ready markers=30 of 34")!;
            Assert.NotNull(folder);
            Assert.Equal("say=Ready markers=30 of 34", File.ReadAllText(Path.Combine(folder, "live.txt")));
            var trace = new TraceRecorder();
            using (var stage = trace.Begin("S0.identify"))
            {
                stage.Done(StageStatus.Ok, "named");
            }

            SittingRecord.Analyzed(picture, trace, "25 holes");
            Assert.Contains("S0.identify", File.ReadAllText(Path.Combine(folder, "analysis.txt")), StringComparison.Ordinal);
            Assert.True(SittingRecord.Count() >= 1);

            Phone.Settings.SaveKeepSitting(false);
            SittingRecord.Clear();
            Assert.False(SittingRecord.On);
            Assert.Equal(0, SittingRecord.Count());
        }
        finally
        {
            platform.IsDevBuild = false;
            Phone.Settings.SaveKeepSitting(true);
        }
    }
}

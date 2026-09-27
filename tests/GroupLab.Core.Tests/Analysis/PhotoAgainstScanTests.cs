using System.Text.Json.Nodes;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Marking;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Analysis;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 233: Alan photographed the three load sheets he had scanned, on a kitchen counter with the Fold 7's own camera,
/// the way anybody would: a hand's shadow across the bottom third, paper mid gray under the kitchen light, the corners taped and lifted, two
/// of them turned a quarter in the frame. Each photograph is held to what its own 600 dpi scan read. Every shot must be found, each within a
/// few hundredths of an inch of where the scan put it; and no more marks than the tape tears the photographs really show.
/// <para>
/// Before entry 233 these read 28, 27 and 24 marks against 25, 25 and 23 shots: the counter showing inside the sheet's edge and the shadow's
/// edge both made the paper beside them read darker than it was, and shot 21, under the shadow, was missed on all three.
/// </para>
/// </summary>
public class PhotoAgainstScanTests
{
    /// <summary>How far a photographed shot may be from where the scan put it, inches: the median over a sheet, and the worst.</summary>
    private const double MedianInches = 0.04, WorstInches = 0.08;

    public static TheoryData<string> Photos => ["photo-6arc-dominus-k-2026-09-26.jpg", "photo-6arc-magnus-m-2026-09-26.jpg", "photo-6.5-magnus-c-2026-09-26.jpg"];

    [Theory]
    [MemberData(nameof(Photos))]
    public void APhotoReadsTheShotsItsScanRead(string file)
    {
        string path = TestData.Path(file, @"C:\Dev\grouplab-originals\range-2026-09-26\photos-clean");
        if (!System.IO.File.Exists(path))
        {
            Assert.True(true, $"skipped: {file} is not on this machine");
            return;
        }

        var sheet = JsonNode.Parse(System.IO.File.ReadAllText(Repo.PathTo("tests", "GroupLab.Core.Tests", "Fixtures", "photos-against-scans-2026-09-26.json")))!["sheets"]!
            .AsArray().Single(s => (string?)s!["photo"] == file)!;
        var expected = sheet["shots"]!.AsArray().Select(p => (X: (double)p![0]!, Y: (double)p[1]!)).ToList();
        var definition = GltdJsonReader.ReadFile(Repo.PathTo("targets", "GL-CF25-LTR-D.gltd.json")).Definition!;
        var (grey, metadata) = ImageLoader.Load(path);
        var (value, _) = ImageLoader.LoadMaxChannel(path);
        var result = AutomaticMarking.Run(grey, value, metadata, definition, new OpenCvSharpBackend(), calibre: Calibre.Of((double)sheet["calibre"]!));
        Assert.Null(result.Failure);

        var found = result.Detections.Select(d => result.Scale!.ToTarget(d.Image)).ToList();
        var distances = new List<double>();
        var missed = new List<int>();
        var taken = new HashSet<int>();
        for (int i = 0; i < expected.Count; i++)
        {
            var nearest = found.Select((f, k) => (k, Distance: Math.Sqrt(Math.Pow(f.X - expected[i].X, 2) + Math.Pow(f.Y - expected[i].Y, 2))))
                .Where(c => !taken.Contains(c.k)).OrderBy(c => c.Distance).FirstOrDefault();
            if (found.Count == 0 || nearest.Distance > 0.15)
            {
                missed.Add(i + 1);
                continue;
            }

            taken.Add(nearest.k);
            distances.Add(nearest.Distance);
        }

        // Shot 14 on the 6.5 sheet touches a marker; the photograph puts its center just inside the marker's zone, which is blind, where the
        // scan put it just outside. That one is allowed and nothing else.
        int allowedMissed = file.Contains("6.5", StringComparison.Ordinal) ? 1 : 0;
        Assert.True(missed.Count <= allowedMissed, $"{file}: shots the scan found and the photograph did not: {string.Join(", ", missed)}");
        distances.Sort();
        Assert.True(distances[distances.Count / 2] <= MedianInches, $"{file}: median {distances[distances.Count / 2]:0.000} in from the scan");
        Assert.True(distances[^1] <= WorstInches, $"{file}: worst {distances[^1]:0.000} in from the scan");
        Assert.True(found.Count - taken.Count <= 1, $"{file}: {found.Count - taken.Count} marks the scan does not have");
    }
}

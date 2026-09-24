using System.Text.Json;
using System.Text.Json.Nodes;
using GroupLab.Core.Capture;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Capture;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 187 section 2 item 2: the quality score's levels are judgment, so every part of every score travels with
/// the session file and with a target sent to the project, where real submissions can tune them later. One record serves both.
/// </summary>
public class CaptureDocumentTests
{
    [Fact]
    public void EveryPartOfTheScoreIsKept()
    {
        var quality = new CaptureQuality(62, "usable", 0.012, 0.8, 0.001, 231, 0.9, 18, 0.7, 142, 0.95, 20, 24, 0.83);
        var record = new CaptureRecord("main", 5.1, 24, 18, "metadata", "homography", -0.01, 0.002, quality);
        var document = JsonSerializer.SerializeToNode(MarkingFile.CaptureDocument(record))!.AsObject();
        var parts = document["quality"]!.AsObject();
        foreach (var property in typeof(CaptureQuality).GetProperties().Where(p => p.DeclaringType == typeof(CaptureQuality) && p.GetMethod?.GetParameters().Length == 0 && p.Name is not ("EqualityContract" or "Weakest" or "OffAxisDegrees")))
        {
            string key = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
            Assert.True(parts.ContainsKey(key), $"the score's {property.Name} is not kept");
        }

        // The angle is kept once, beside the lens, rather than inside the score as well.
        Assert.Equal(18, (double)document["offAxisDegrees"]!);
        Assert.Null(MarkingFile.CaptureDocument(null));
    }
}

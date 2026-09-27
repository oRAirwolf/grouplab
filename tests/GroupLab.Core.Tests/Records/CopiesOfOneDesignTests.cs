using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Records;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 229 section 1: Alan printed one PDF three times and wrote K, M and C in the serial boxes, so all three sheets
/// carry the printed code GL-R0T0-384Z-HRBE-M0EW, which names the design. Three scans of them are three sessions: nothing merges them or
/// drops one, each keeps its own image hash and shots, and a label of the person's own tells them apart and survives the file.
/// </summary>
public class CopiesOfOneDesignTests
{
    [Fact]
    public void ThreeCopiesOfOneDesignStayThreeSessionsToldApartByTheirLabels()
    {
        var definition = GltdJsonReader.ReadFile(Repo.PathTo("targets", "GL-CF25-LTR-D.gltd.json")).Definition!;
        string root = Path.Combine(Path.GetTempPath(), $"grouplab-copies-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var store = SessionStore.Open(Path.Combine(root, "sessions.db"));
            foreach (var (label, hash, x) in new[] { ("K", "aa", 100.0), ("M", "bb", 200.0), ("C", "cc", 300.0) })
            {
                var session = new MarkingSession();
                session.SetScale(new LengthReference(new PointD(0, 0), new PointD(600, 0), 1));
                session.AddShot(new PointD(x, x));
                session.SetSheetLabel(label);
                var (read, _) = MarkingFile.Read(MarkingFile.Write(session.State));
                Assert.Equal(label, read.SheetLabel);
                store.Save(SessionRecords.Build(read, definition, UnitSettings.Imperial, false, null, hash, null, null, DateTime.UtcNow, DateTime.Now));
            }

            Assert.Equal(3, store.CountUsing(definition.Id!));
            var listed = store.List();
            Assert.Equal(3, listed.Count);
            Assert.Equal(3, listed.Select(s => s.SheetName).Distinct().Count());
            Assert.All(listed, s => Assert.Matches(", [KMC]$", s.SheetName));
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(root);
        }
    }
}

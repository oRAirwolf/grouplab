using System.Text.Json.Nodes;
using GroupLab.Core.Publication;

namespace GroupLab.Core.Tests.Publication;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 37 section 5: the sheet size a contributor wrote in the notes is carried as structured fields when it can be
/// read without guessing, and found again beside a published image.
/// </summary>
public class StatedSheetSizeTests
{
    [Theory]
    [InlineData("Action Target PR-BE6 17.5x23”", 17.5, 23, "in", "17.5x23”")]
    [InlineData("Action Target TCT-MK3-MOD2 23x35”", 23, 35, "in", "23x35”")]
    [InlineData("a 23 x 35 inch sheet", 23, 35, "in", "23 x 35 inch")]
    [InlineData("12.5X18in, stapled", 12.5, 18, "in", "12.5X18in")]
    [InlineData("24 × 36\"", 24, 36, "in", "24 × 36\"")]
    [InlineData("an A3 target, 297 x 420 mm", 297, 420, "mm", "297 x 420 mm")]
    [InlineData("50x70cm", 50, 70, "cm", "50x70cm")]
    public void ASizeWithItsUnitIsReadInTheOrderWritten(string notes, double width, double height, string unit, string text)
    {
        var size = StatedSheetSize.Parse(notes);
        Assert.NotNull(size);
        Assert.Equal((width, height, unit, text), (size.Width, size.Height, size.Unit, size.Text));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("shot at 100 yards, 5 rounds")]
    [InlineData("Action Target PR-BE6 17.5x23")]
    [InlineData("a 3x9 scope")]
    [InlineData("17.5x23” target on a 24x36” backer")]
    public void NoSizeANoUnitOrTwoSizesGiveNothing(string? notes) => Assert.Null(StatedSheetSize.Parse(notes));

    [Fact]
    public void MillimetresAndCentimetresComeToInches()
    {
        Assert.Equal(11.6929, StatedSheetSize.Parse("297 x 420 mm")!.WidthInches, 4);
        Assert.Equal(27.5591, StatedSheetSize.Parse("50x70cm")!.HeightInches, 4);
    }

    [Fact]
    public void ThePublishedRecordBesideAnImageGivesTheSizeForTheImagesItLists()
    {
        string directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"grouplab-stated-{Guid.NewGuid():N}")).FullName;
        try
        {
            var provenance = new JsonObject
            {
                ["files"] = new JsonArray(new JsonObject { ["storedName"] = "001_IMG_1580.jpg" }),
                ["statedSheetSize"] = StatedSheetSize.Parse("Action Target PR-BE6 17.5x23”")!.ToJson(),
            };
            File.WriteAllText(Path.Combine(directory, PublicationCheck.ProvenanceFile), provenance.ToJsonString());

            var size = StatedSheetSize.Beside(Path.Combine(directory, "001_IMG_1580.jpg"));
            Assert.Equal(new StatedSheetSize(17.5, 23, "in", "17.5x23”"), size);
            Assert.Null(StatedSheetSize.Beside(Path.Combine(directory, "002_not_listed.jpg")));
            Assert.Null(StatedSheetSize.Beside(Path.Combine(Path.GetTempPath(), "no-record-here", "photo.jpg")));

            File.WriteAllText(Path.Combine(directory, PublicationCheck.ProvenanceFile), "{ not json");
            Assert.Null(StatedSheetSize.Beside(Path.Combine(directory, "001_IMG_1580.jpg")));
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(directory);
        }
    }
}

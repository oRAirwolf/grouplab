using System.Text.Json.Nodes;
using GroupLab.Core.Gltd;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Binary;

/// <summary>What the body cannot carry is refused, naming the field, rather than silently dropped.</summary>
public class EncoderRefusalTests
{
    [Fact]
    public void DrawnCellsNeedTheCellBlock()
    {
        var doc = Spec.Section4Node();
        doc["cells"]!["drawn"] = true;

        AssertRefused(doc, "encode.cellBlock", "/cells/drawn");
    }

    [Fact]
    public void NonDefaultLabelNeedsTheLabelBlock()
    {
        var doc = Spec.Section4Node();
        doc["bulls"]![8]!["label"] = "9A";

        AssertRefused(doc, "encode.labelBlock", "/bulls/8/label");
    }

    [Fact]
    public void LabelOffsetNeedsTheLabelBlock()
    {
        var doc = Spec.Section4Node();
        doc["bulls"]![0]!["labelOffset"] = new JsonObject { ["x"] = -20, ["y"] = 0 };

        AssertRefused(doc, "encode.labelBlock", "/bulls/0/labelOffset");
    }

    [Fact]
    public void ExplicitCodePlacementIsRefusedPerQuestion13()
    {
        var doc = Spec.Section4Node();
        doc["codes"]!["placement"] = "explicit";

        AssertRefused(doc, "encode.explicitCodes", "/codes/placement");
    }

    [Fact]
    public void ExplicitFiducialSchemeIsRefused()
    {
        var doc = Spec.Section4Node();
        doc["fiducials"]!["scheme"] = "explicit";

        AssertRefused(doc, "encode.explicitMarkers", "/fiducials/scheme");
    }

    [Fact]
    public void CodeVersionOtherThanTenIsRefused()
    {
        var doc = Spec.Section4Node();
        doc["codes"]!["version"] = 8;

        AssertRefused(doc, "encode.notCarried", "/codes/version");
    }

    [Fact]
    public void ExplicitDataBlockIsRefused()
    {
        var doc = Spec.Section4Node();
        var (_, block) = Spec.Fragment("### 3.10 ");
        block["layout"] = "explicit";
        block["fields"] = new JsonArray(new JsonObject { ["key"] = "date", ["label"] = "Date" });
        doc["dataBlock"] = block;

        AssertRefused(doc, "encode.explicitDataBlock", "/dataBlock");
    }

    [Fact]
    public void GridStrokesOtherThanStyleOneAreRefused()
    {
        var doc = WithSection313Grid();
        doc["grids"]![0]!["majorStroke"] = 5;

        AssertRefused(doc, "encode.notCarried", "/grids/0");
    }

    [Fact]
    public void Section313GridEncodesOnceItsGreyInkExists()
    {
        var result = GltdBinary.Encode(Read(WithSection313Grid()));

        Assert.Empty(result.Diagnostics);
        Assert.NotNull(result.Encoding);
    }

    [Fact]
    public void MissingFiducialsIsRefused()
    {
        var doc = Spec.Section4Node();
        doc.Remove("fiducials");

        AssertRefused(doc, "encode.missingBlock", "/fiducials");
    }

    [Fact]
    public void CellGridThatDisagreesWithTheBullsIsRefused()
    {
        var doc = Spec.Section4Node();
        doc["cells"]!["grid"]!["originX"] = 131;

        AssertRefused(doc, "encode.cellsInconsistent", "/cells/grid");
    }

    [Fact]
    public void SixteenDistinctColoursAreRefused()
    {
        var doc = Spec.Section4Node();
        var inks = new JsonArray();
        for (int i = 0; i < 16; i++)
        {
            inks.Add(new JsonObject { ["key"] = $"c{i}", ["srgb"] = $"#0000{i:X2}", ["role"] = i == 1 ? "fiducial" : "artwork" });
        }

        doc["inks"] = inks;
        doc["fiducials"]!["ink"] = "c1";
        foreach (var disc in doc["ringSets"]![0]!["discs"]!.AsArray())
        {
            disc!["ink"] = "c0";
        }

        AssertRefused(doc, "encode.tooManyColours", "/inks");
    }

    [Fact]
    public void AbsentLabelsEncodeAsTheDefaultLabels()
    {
        var doc = Spec.Section4Node();
        foreach (var bull in doc["bulls"]!.AsArray())
        {
            bull!.AsObject().Remove("label");
        }

        Assert.Equal(Encode(Spec.Section4Node()).Body, Encode(doc).Body);
    }

    [Fact]
    public void TrimmedNamedPageEncodesAsCustom()
    {
        var doc = Spec.Section4Node();
        doc["page"]!["width"] = 2100;
        var encoding = Encode(doc);

        var decoded = GltdBinary.DecodeBody(encoding.Body, encoding.BlockFlags);

        Assert.Equal(new Page(PageSize.Custom, 2100, 2794, Orientation.Portrait), decoded.Definition!.Page);
    }

    [Fact]
    public void GridCellsOnAnIrregularLayoutAreRefused()
    {
        var doc = Spec.Section4Node();
        doc["bulls"]![4]!["x"] = 1841;

        AssertRefused(doc, "encode.cellBlock", "/cells/mode");
    }

    [Fact]
    public void IrregularBullsUseExplicitModeAndRoundTrip()
    {
        var doc = Spec.Section4Node();
        doc["bulls"]![4]!["x"] = 1841;
        doc.Remove("cells");
        var encoding = Encode(doc);

        var decoded = GltdBinary.DecodeBody(encoding.Body, encoding.BlockFlags);

        Assert.Equal(1, encoding.BlockFlags & 1);
        Assert.Empty(decoded.Diagnostics);
        Assert.Equal(1841, decoded.Definition!.Bulls[4].X);
        Assert.Null(decoded.Definition.Cells);
    }

    private static JsonObject WithSection313Grid()
    {
        var doc = Spec.Section4Node();
        doc["inks"]!.AsArray().Add(new JsonObject { ["key"] = "grey", ["srgb"] = "#808080", ["role"] = "artwork" });
        var (name, grids) = Spec.Fragment("### 3.13 ");
        doc[name] = grids;
        return doc;
    }

    private static TargetDefinition Read(JsonObject doc)
    {
        var result = GltdJsonReader.Read(Spec.Utf8(doc));
        Assert.Empty(result.Diagnostics);
        return result.Definition!;
    }

    private static BinaryEncoding Encode(JsonObject doc)
    {
        var result = GltdBinary.Encode(Read(doc));
        Assert.Empty(result.Diagnostics);
        return result.Encoding!;
    }

    private static void AssertRefused(JsonObject doc, string code, string path)
    {
        var result = GltdBinary.Encode(Read(doc));

        Assert.Null(result.Encoding);
        Assert.Contains(result.Diagnostics, d => d.Severity == Severity.Error && d.Code == code && d.Path == path);
    }
}

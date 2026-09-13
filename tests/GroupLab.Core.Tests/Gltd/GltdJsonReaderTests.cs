using System.Text;
using System.Text.Json.Nodes;
using GroupLab.Core.Gltd;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Gltd;

public class GltdJsonReaderTests
{
    [Fact]
    public void Section4ExampleReadsCleanly()
    {
        var result = GltdJsonReader.Read(Encoding.UTF8.GetBytes(Spec.Section4Example));

        Assert.Empty(result.Diagnostics);
        var d = result.Definition;
        Assert.NotNull(d);
        Assert.Equal("GL-YCSK-DZZ1-R0VJ-4T5Y", d.Id);
        Assert.Equal(PageSize.Letter, d.Page.Size);
        Assert.Equal(28, d.Bulls.Count);
        Assert.Equal(3, d.Bulls.Count(b => !b.Scoring));
        Assert.Equal(CodePlacement.Corners1, d.Codes!.Placement);
        Assert.Equal(new PointDmm(1909, 2544), d.Codes.Positions[3]);
    }

    [Theory]
    [InlineData("### 3.2 ")]
    [InlineData("### 3.3 ")]
    [InlineData("### 3.4 ")]
    [InlineData("### 3.5 ")]
    [InlineData("### 3.6 ")]
    [InlineData("### 3.7 ")]
    [InlineData("### 3.8 ")]
    [InlineData("### 3.9 ")]
    [InlineData("### 3.10 ")]
    [InlineData("### 3.11 ")]
    [InlineData("### 3.12 ")]
    [InlineData("### 3.13 ")]
    public void EverySection3ExampleReadsCleanly(string heading)
    {
        var doc = Spec.Section4Node();
        var (name, value) = Spec.Fragment(heading);
        doc[name] = value;

        var result = GltdJsonReader.Read(Spec.Utf8(doc));

        Assert.Empty(result.Diagnostics);
        Assert.NotNull(result.Definition);
    }

    [Fact]
    public void UnknownMajorIsRefusedWithoutAPartialParse()
    {
        var doc = Spec.Section4Node();
        doc["gltd"] = 2;
        doc["name"] = 42;

        var result = GltdJsonReader.Read(Spec.Utf8(doc));

        Assert.Null(result.Definition);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("gltd.unsupportedMajor", diagnostic.Code);
        Assert.Contains("version 2", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("supports version 1", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownRevisionProceeds()
    {
        var doc = Spec.Section4Node();
        doc["revision"] = 7;

        var result = GltdJsonReader.Read(Spec.Utf8(doc));

        Assert.Empty(result.Diagnostics);
        Assert.Equal(7, result.Definition!.Revision);
    }

    [Theory]
    [InlineData("320.0")]
    [InlineData("3.2e2")]
    [InlineData("\"320\"")]
    public void NonIntegerLengthIsRejected(string raw)
    {
        string text = Spec.Section4Example.Replace("\"x\": 320,  \"y\": 539", $"\"x\": {raw},  \"y\": 539", StringComparison.Ordinal);

        var result = GltdJsonReader.Read(Encoding.UTF8.GetBytes(text));

        AssertError(result, "schema.type", "/bulls/0/x");
    }

    [Theory]
    [InlineData(65536)]
    [InlineData(-1)]
    [InlineData(499)]
    public void PageLengthOutsideItsRangeIsRejected(int width)
    {
        var doc = Spec.Section4Node();
        doc["page"]!["width"] = width;

        AssertError(GltdJsonReader.Read(Spec.Utf8(doc)), "schema.range", "/page/width");
    }

    [Fact]
    public void UnknownPropertyInsideAKnownBlockIsAnError()
    {
        var doc = Spec.Section4Node();
        doc["page"]!["colour"] = "white";

        AssertError(GltdJsonReader.Read(Spec.Utf8(doc)), "schema.additionalProperty", "/page/colour");
    }

    [Fact]
    public void DuplicateKeyIsAnError()
    {
        string text = Spec.Section4Example.Replace("\"units\": \"dmm\",", "\"units\": \"dmm\", \"units\": \"dmm\",", StringComparison.Ordinal);

        AssertError(GltdJsonReader.Read(Encoding.UTF8.GetBytes(text)), "json.duplicateKey", "/units");
    }

    [Fact]
    public void MissingPositionsIsNamed()
    {
        var doc = Spec.Section4Node();
        doc["codes"]!.AsObject().Remove("positions");

        var result = GltdJsonReader.Read(Spec.Utf8(doc));

        var diagnostic = AssertError(result, "schema.required", "/codes");
        Assert.Contains("positions", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownEnumerationValueIsRejected()
    {
        var doc = Spec.Section4Node();
        doc["page"]!["size"] = "b5";

        AssertError(GltdJsonReader.Read(Spec.Utf8(doc)), "schema.enum", "/page/size");
    }

    [Fact]
    public void UnitsOtherThanDmmIsRejected()
    {
        var doc = Spec.Section4Node();
        doc["units"] = "mm";

        AssertError(GltdJsonReader.Read(Spec.Utf8(doc)), "schema.const", "/units");
    }

    [Fact]
    public void PatternIsAnchoredAtTheEndOfInput()
    {
        var doc = Spec.Section4Node();
        doc["inks"]![0]!["key"] = "black\n";

        AssertError(GltdJsonReader.Read(Spec.Utf8(doc)), "schema.pattern", "/inks/0/key");
    }

    [Fact]
    public void Utf8ByteOrderMarkIsAccepted()
    {
        byte[] bytes = [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes(Spec.Section4Example)];

        Assert.Empty(GltdJsonReader.Read(bytes).Diagnostics);
    }

    [Fact]
    public void MalformedJsonIsReportedRatherThanThrown()
    {
        AssertError(GltdJsonReader.Read("{\"gltd\": 1,"u8), "json.syntax", "");
    }

    private static Diagnostic AssertError(GltdReadResult result, string code, string path)
    {
        Assert.Null(result.Definition);
        return Assert.Single(result.Diagnostics, d => d.Severity == Severity.Error && d.Code == code && d.Path == path);
    }
}

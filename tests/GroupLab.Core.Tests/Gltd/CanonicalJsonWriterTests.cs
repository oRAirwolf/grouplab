using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Gltd.Schema;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Gltd;

public class CanonicalJsonWriterTests
{
    private static readonly string[] Section3Blocks =
        ["### 3.6 ", "### 3.7 ", "### 3.9 ", "### 3.10 ", "### 3.11 ", "### 3.12 ", "### 3.13 "];

    private static string GoldenPath =>
        Repo.PathTo("tests", "GroupLab.Core.Tests", "Fixtures", "section4-example.canonical.gltd.json");

    [Fact]
    public void Section4ExampleMatchesGolden()
    {
        byte[] written = CanonicalJsonWriter.Write(ReadClean(Encoding.UTF8.GetBytes(Spec.Section4Example)));

        Assert.Equal(File.ReadAllText(GoldenPath, Encoding.UTF8), Encoding.UTF8.GetString(written));
        Assert.Equal(File.ReadAllBytes(GoldenPath), written);
    }

    [Fact]
    public void WritingIsIdempotent()
    {
        byte[] once = CanonicalJsonWriter.Write(ReadClean(Spec.Utf8(AllBlocksDocument())));
        byte[] twice = CanonicalJsonWriter.Write(ReadClean(once));

        Assert.Equal(once, twice);
    }

    [Fact]
    public void FormatIsLfTwoSpacesWithoutBomOrTrailingWhitespace()
    {
        byte[] written = CanonicalJsonWriter.Write(ReadClean(Spec.Utf8(AllBlocksDocument())));
        string text = Encoding.UTF8.GetString(written);

        Assert.NotEqual(0xEF, written[0]);
        Assert.DoesNotContain('\r', text);
        Assert.DoesNotContain('\t', text);
        Assert.EndsWith("}\n", text, StringComparison.Ordinal);
        Assert.False(text.EndsWith("\n\n", StringComparison.Ordinal));
        foreach (string line in text[..^1].Split('\n'))
        {
            Assert.Equal(line.TrimEnd(), line);
            Assert.Equal(0, (line.Length - line.TrimStart(' ').Length) % 2);
        }
    }

    [Fact]
    public void InnerKeyOrderFollowsTheSection9Schema()
    {
        byte[] written = CanonicalJsonWriter.Write(ReadClean(Spec.Utf8(AllBlocksDocument())));
        using var output = JsonDocument.Parse(written);
        using var schema = JsonDocument.Parse(GltdSchema.ReadText());

        int objectsChecked = CheckOrder(output.RootElement, schema.RootElement, "", isRoot: true);

        Assert.True(objectsChecked > 40, $"Only {objectsChecked} objects were checked.");
    }

    [Fact]
    public void TopLevelOrderIsFixedAndUnknownMembersFollowInReadOrder()
    {
        var doc = AllBlocksDocument();
        doc.Insert(3, "futureBlock", new JsonObject { ["enabled"] = true });
        doc["zeta"] = 3;

        byte[] written = CanonicalJsonWriter.Write(ReadClean(Spec.Utf8(doc)));
        using var output = JsonDocument.Parse(written);

        string[] expected =
        [
            "gltd", "revision", "id", "name", "description", "author", "licence", "created", "units",
            "page", "inks", "ringSets", "bulls", "cells", "fiducials", "codes", "print",
            "dataBlock", "instance", "tiling", "grids", "futureBlock", "zeta",
        ];
        Assert.Equal(expected, output.RootElement.EnumerateObject().Select(p => p.Name).ToArray());
    }

    [Fact]
    public void UnknownTopLevelBlockSurvivesAReadWriteCycle()
    {
        // The JSON half of conformance test 4: a later revision's block is kept unchanged.
        var doc = Spec.Section4Node();
        doc["futureBlock"] = JsonNode.Parse("""{"nested":{"list":[1,2.5,{"deep":"x"}]},"flag":false,"text":"é"}""");

        byte[] written = CanonicalJsonWriter.Write(ReadClean(Spec.Utf8(doc)));

        Assert.True(JsonNode.DeepEquals(doc["futureBlock"], JsonNode.Parse(written)!["futureBlock"]));
    }

    [Fact]
    public void SrgbIsWrittenInUpperCase()
    {
        var doc = Spec.Section4Node();
        doc["inks"]![0]!["srgb"] = "#c8102e";

        string text = Encoding.UTF8.GetString(CanonicalJsonWriter.Write(ReadClean(Spec.Utf8(doc))));

        Assert.Contains("\"srgb\": \"#C8102E\"", text, StringComparison.Ordinal);
    }

    private static TargetDefinition ReadClean(byte[] bytes)
    {
        var result = GltdJsonReader.Read(bytes);
        Assert.Empty(result.Diagnostics);
        return result.Definition!;
    }

    private static JsonObject AllBlocksDocument()
    {
        var doc = Spec.Section4Node();
        foreach (string heading in Section3Blocks)
        {
            var (name, value) = Spec.Fragment(heading);
            doc[name] = value;
        }

        return doc;
    }

    private static int CheckOrder(JsonElement value, JsonElement schema, string path, bool isRoot)
    {
        int count = 0;
        if (value.ValueKind == JsonValueKind.Object && schema.TryGetProperty("properties", out var properties))
        {
            var schemaOrder = properties.EnumerateObject().Select(p => p.Name).ToList();
            var written = value.EnumerateObject().Select(p => p.Name).Where(schemaOrder.Contains).ToList();
            if (!isRoot)
            {
                var expected = schemaOrder.Where(written.Contains).ToList();
                Assert.True(expected.SequenceEqual(written),
                    $"{path}: expected {string.Join(", ", expected)} but wrote {string.Join(", ", written)}.");
                count++;
            }

            foreach (var member in value.EnumerateObject())
            {
                if (properties.TryGetProperty(member.Name, out var child))
                {
                    count += CheckOrder(member.Value, child, $"{path}/{member.Name}", isRoot: false);
                }
            }
        }
        else if (value.ValueKind == JsonValueKind.Array && schema.TryGetProperty("items", out var items))
        {
            int i = 0;
            foreach (var item in value.EnumerateArray())
            {
                count += CheckOrder(item, items, $"{path}/{i++}", isRoot: false);
            }
        }

        return count;
    }
}

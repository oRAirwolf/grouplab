using System.Text;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Binary;

/// <summary>Conformance tests 28 to 31 and the GLTD-I frame of section 3.11.</summary>
public class InstanceCodecTests
{
    private const string ReferenceId = "GL-YCSK-DZZ1-R0VJ-4T5Y";

    [Fact]
    public void Test28InstanceDataDoesNotChangeTheIdentifier()
    {
        var doc = Spec.Section4Node();
        var (name, instance) = Spec.Fragment("### 3.11 ");
        doc[name] = instance;
        var definition = GltdJsonReader.Read(Spec.Utf8(doc)).Definition!;

        var encoded = GltdBinary.Encode(definition);

        Assert.Equal(ReferenceId, encoded.Encoding!.DefinitionId);
    }

    [Fact]
    public void Section311ExampleMeasures128BytesOfFieldsAnd153InTotal()
    {
        var frame = EncodeSection311Example();

        Assert.Equal(153, frame.Length);
        Assert.Equal(128, frame.Length - InstanceCodec.HeaderLength);
    }

    [Fact]
    public void Section311ExampleRoundTrips()
    {
        var source = Section311Instance();

        var decoded = InstanceCodec.Decode(EncodeSection311Example(), FieldSet.Standard9, ReferenceId);

        Assert.Empty(decoded.Diagnostics);
        Assert.Equal(source.Serial, decoded.Instance!.Serial);
        Assert.Equal(source.Printed, decoded.Instance.Printed);
        Assert.Equal(source.Values, decoded.Instance.Values);
    }

    [Fact]
    public void Test29MismatchedDefinitionIsReportedNotMerged()
    {
        var decoded = InstanceCodec.Decode(EncodeSection311Example(), FieldSet.Standard9, "GL-0000-0000-0000-0000");

        Assert.Null(decoded.Instance);
        Assert.Equal(ReferenceId, decoded.DefinitionId);
        Assert.Equal("instance.definitionMismatch", Assert.Single(decoded.Diagnostics).Code);
    }

    [Fact]
    public void Test30OverflowingTheFieldBudgetIsRefusedNamingTheField()
    {
        var source = Section311Instance();
        var values = source.Values!.Select(v => v.Key == "notes" ? new KeyValuePair<string, string>("notes", new string('x', 40)) : v).ToList();

        var result = InstanceCodec.Encode(source with { Values = values }, FieldSet.Standard9, ReferenceId);

        Assert.Null(result.Frame);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("instance.overBudget", diagnostic.Code);
        Assert.Equal("/instance/values/notes", diagnostic.Path);
    }

    [Fact]
    public void FieldOutsideTheFieldSetIsRefused()
    {
        var result = InstanceCodec.Encode(Section311Instance(), FieldSet.Standard6, ReferenceId);

        Assert.Null(result.Frame);
        Assert.Contains(result.Diagnostics, d => d.Code == "instance.unknownField" && d.Path == "/instance/values/bullet");
    }

    [Fact]
    public void Test31InstanceAndDefinitionFramesAreNeverConfused()
    {
        var definitionFrame = GltdBinary.ReplicatedFrame(
            GltdBinary.Encode(GltdJsonReader.Read(Encoding.UTF8.GetBytes(Spec.Section4Example)).Definition!).Encoding!);

        var asInstance = InstanceCodec.Decode(definitionFrame, FieldSet.Standard9, ReferenceId);
        var asDefinition = GltdBinary.Decode([EncodeSection311Example()]);

        Assert.Contains("definition frame", Assert.Single(asInstance.Diagnostics).Message, StringComparison.Ordinal);
        Assert.Null(asDefinition.Definition);
        Assert.Contains("GLTD-I instance frame", Assert.Single(asDefinition.Diagnostics).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CorruptedFieldsAreRejected()
    {
        byte[] frame = EncodeSection311Example();
        frame[^2] ^= 0x20;

        var decoded = InstanceCodec.Decode(frame, FieldSet.Standard9, ReferenceId);

        Assert.Null(decoded.Instance);
        Assert.Contains("CRC-32", Assert.Single(decoded.Diagnostics).Message, StringComparison.Ordinal);
    }

    private static Instance Section311Instance()
    {
        var doc = Spec.Section4Node();
        var (name, instance) = Spec.Fragment("### 3.11 ");
        doc[name] = instance;
        return GltdJsonReader.Read(Spec.Utf8(doc)).Definition!.Instance!;
    }

    private static byte[] EncodeSection311Example()
    {
        var result = InstanceCodec.Encode(Section311Instance(), FieldSet.Standard9, ReferenceId);
        Assert.Empty(result.Diagnostics);
        return result.Frame!;
    }
}

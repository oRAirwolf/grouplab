using System.Text;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Derivation;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Binary;

/// <summary>The C# encoder against tools/gltd/check.py, byte for byte, on check.py's own inputs.</summary>
public class ReferenceEncoderParityTests
{
    public static TheoryData<string> Names()
    {
        var data = new TheoryData<string>();
        foreach (var d in ReferenceFixtures.Definitions)
        {
            data.Add(d.GetProperty("name").GetString()!);
        }

        return data;
    }

    [Fact]
    public void FixtureHoldsAllTwentyTwoDefinitions()
    {
        Assert.Equal(22, ReferenceFixtures.Definitions.Count());
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void BodyFrameAndIdentifierMatchCheckPy(string name)
    {
        var fixture = ReferenceFixtures.Named(name);

        var (body, flags) = BodyCodec.Write(ReferenceFixtures.ToBody(fixture.GetProperty("input")));
        byte[] frame = FrameCodec.Replicated(body, flags);

        Assert.Equal(fixture.GetProperty("bodyHex").GetString(), Convert.ToHexStringLower(body));
        Assert.Equal(fixture.GetProperty("frameHex").GetString(), Convert.ToHexStringLower(frame));
        Assert.Equal(fixture.GetProperty("id").GetString(), DefinitionId.Compute(body));
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void CheckPyFrameDecodesCanonicallyAndKeepsItsIdentifier(string name)
    {
        var fixture = ReferenceFixtures.Named(name);
        byte[] frame = Convert.FromHexString(fixture.GetProperty("frameHex").GetString()!);

        var decoded = GltdBinary.Decode([frame]);

        Assert.Empty(decoded.Diagnostics);
        Assert.Equal(fixture.GetProperty("id").GetString(), decoded.DefinitionId);
        var input = fixture.GetProperty("input");
        var expected = input.GetProperty("codes").GetProperty("positions").EnumerateArray()
            .Select(p => new PointDmm(p.GetProperty("x").GetInt32(), p.GetProperty("y").GetInt32()));
        Assert.Equal(expected, decoded.Definition!.Codes!.Positions);
        Assert.Equal(input.GetProperty("page").GetProperty("width").GetInt32(), decoded.Definition.Page.Width);
        Assert.Equal(input.GetProperty("page").GetProperty("height").GetInt32(), decoded.Definition.Page.Height);
    }

    [Fact]
    public void Section4DocumentEncodesToTheReferenceSheetBytes()
    {
        var definition = GltdJsonReader.Read(Encoding.UTF8.GetBytes(Spec.Section4Example)).Definition!;
        var fixture = ReferenceFixtures.Named("GL-CF25-LTR");

        var result = GltdBinary.Encode(definition);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(fixture.GetProperty("bodyHex").GetString(), Convert.ToHexStringLower(result.Encoding!.Body));
        Assert.Equal("GL-YCSK-DZZ1-R0VJ-4T5Y", result.Encoding.DefinitionId);
        Assert.Equal(definition.Id, result.Encoding.DefinitionId);
    }

    [Fact]
    public void Crc32MatchesItsCheckValue()
    {
        Assert.Equal(0xCBF43926u, Crc32.Compute("123456789"u8));
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void IdentifierParsesBackToItsBytes(string name)
    {
        string id = ReferenceFixtures.Named(name).GetProperty("id").GetString()!;

        Assert.True(DefinitionId.TryParse(id, out byte[] bytes));
        Assert.Equal(id, DefinitionId.Format(bytes));
    }

    [Theory]
    [InlineData("GL-YCSK-DZZ1-R0VJ-4T5I")]
    [InlineData("GL-YCSK-DZZ1-R0VJ-4T5")]
    [InlineData("GX-YCSK-DZZ1-R0VJ-4T5Y")]
    public void MalformedIdentifierIsRejected(string text)
    {
        Assert.False(DefinitionId.TryParse(text, out _));
    }

    [Fact]
    public void Corners1ReproducesSection38AndTheZeroingSheet()
    {
        Assert.Equal([new(250, 250), new(1909, 250), new(250, 2544), new(1909, 2544)], Corners1.Positions(2159, 2794, 0, 4, 4));
        Assert.Equal([new(250, 250), new(1909, 250)], Corners1.Positions(2159, 2794, 310, 2, 4));
        Assert.Equal([new(250, 250), new(1909, 250), new(250, 2304), new(1909, 2304)], Corners1.Positions(2159, 2794, 210, 4, 4));
    }
}

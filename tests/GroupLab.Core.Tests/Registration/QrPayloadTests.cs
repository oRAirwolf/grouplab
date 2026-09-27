using System.Text;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Registration;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 243 section 4: OpenCV hands a code's bytes to .NET as text, kept as UTF-8 when they happen to be valid UTF-8
/// and converted from Latin-1 when not. The bytes that were printed come back either way, one payload a code, including the frame that
/// exposed it, whose only high bytes were CE 95 in its CRC.
/// </summary>
public class QrPayloadTests
{
    public static TheoryData<string> Sheets() => new() { "GL-CF25-LTR-D-C.gltd.json", "GL-CF25-LTR-D-E.gltd.json", "GL-CF25-LTR.gltd.json", "GL-ZERO-MOA-100Y.gltd.json" };

    [Theory]
    [MemberData(nameof(Sheets))]
    public void ThePrintedFrameComesBackWhicheverWayOpenCvReadIt(string file)
    {
        byte[] printed = GltdBinary.ReplicatedFrame(GltdBinary.Encode(BuiltIns.Load(file)).Encoding!);
        Assert.Equal(printed, OpenCvSharpBackend.Payload(AsOpenCvGivesIt(printed)));
    }

    [Fact]
    public void TheFrameThatExposedItIsValidUtf8()
    {
        byte[] printed = GltdBinary.ReplicatedFrame(GltdBinary.Encode(BuiltIns.Load("GL-CF25-LTR-D-C.gltd.json")).Encoding!);
        Assert.NotEqual(printed, Encoding.Latin1.GetBytes(AsOpenCvGivesIt(printed)));
    }

    [Fact]
    public void PlainTextIsItsOwnBytes() => Assert.Equal("GL-TEST"u8.ToArray(), OpenCvSharpBackend.Payload("GL-TEST"));

    /// <summary>The text OpenCV's decoder makes of a byte-mode payload: as UTF-8 when it is valid UTF-8, from Latin-1 when it is not.</summary>
    private static string AsOpenCvGivesIt(byte[] printed)
    {
        try
        {
            return new UTF8Encoding(false, true).GetString(printed);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Latin1.GetString(printed);
        }
    }
}

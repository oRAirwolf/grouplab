using GroupLab.Core.Imaging;

namespace GroupLab.Core.Tests.Imaging;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 239: a picked picture is decoded at a power of two fraction of its size, never at full size, and never below
/// the phone's working size.
/// </summary>
public class WorkingSizeSampleTests
{
    [Theory]
    [InlineData(4958, 6458, 2)]
    [InlineData(4000, 3000, 1)]
    [InlineData(8160, 6120, 2)]
    [InlineData(16000, 12000, 4)]
    [InlineData(2000, 1500, 1)]
    public void APictureIsDecodedAtTheLargestFractionThatStillHoldsTheWorkingSize(int width, int height, int expected)
    {
        int sample = WorkingSize.SampleFor(width, height, WorkingSize.PhoneMegapixels);
        Assert.Equal(expected, sample);
        if (sample > 1)
        {
            Assert.True((double)width / sample * height / sample >= WorkingSize.PhoneMegapixels * 1e6);
            Assert.True((double)width / (sample * 2) * height / (sample * 2) < WorkingSize.PhoneMegapixels * 1e6);
        }
    }
}

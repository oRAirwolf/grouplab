using System.Numerics;
using GroupLab.Core.Rendering.Markers;

namespace GroupLab.Core.Tests.Rendering;

/// <summary>
/// The tag36h11 table against the properties FIDUCIAL-DECISION.md section 3.3 states for the family. A transcription
/// error or a wrong bit layout would break the Hamming distance, which is why it is computed rather than trusted.
/// </summary>
public class Tag36h11Tests
{
    [Fact]
    public void TableHasTheReferenceImplementationsFirstAndLastCodes()
    {
        Assert.Equal(0xd7e00984bUL, Tag36h11.Code(0));
        Assert.Equal(0xe8b772fe0UL, Tag36h11.Code(Tag36h11.CodeCount - 1));
    }

    [Fact]
    public void BorderIsInkedAndEverySetDataBitIsBlank()
    {
        for (int id = 0; id < Tag36h11.CodeCount; id++)
        {
            var modules = Tag36h11.InkedModules(id);
            int blank = 0;
            for (int row = 0; row < Tag36h11.Modules; row++)
            {
                for (int col = 0; col < Tag36h11.Modules; col++)
                {
                    bool border = row is 0 or 7 || col is 0 or 7;
                    Assert.True(!border || modules[row, col], $"Marker {id} has a blank border module at ({row}, {col}).");
                    blank += modules[row, col] ? 0 : 1;
                }
            }

            Assert.Equal(BitOperations.PopCount(Tag36h11.Code(id)), blank);
        }
    }

    [Fact]
    public void MinimumHammingDistanceAcrossCodesAndRotationsIsEleven()
    {
        var rotations = Enumerable.Range(0, Tag36h11.CodeCount).Select(Rotations).ToArray();
        int minimum = int.MaxValue;
        for (int i = 0; i < Tag36h11.CodeCount; i++)
        {
            for (int rotation = 1; rotation < 4; rotation++)
            {
                minimum = Math.Min(minimum, BitOperations.PopCount(rotations[i][0] ^ rotations[i][rotation]));
            }

            for (int j = i + 1; j < Tag36h11.CodeCount; j++)
            {
                for (int rotation = 0; rotation < 4; rotation++)
                {
                    minimum = Math.Min(minimum, BitOperations.PopCount(rotations[i][0] ^ rotations[j][rotation]));
                }
            }
        }

        Assert.Equal(Tag36h11.MinimumHammingDistance, minimum);
    }

    /// <summary>The 6 by 6 data area as printed, in its four orientations, read back from the rendered modules.</summary>
    private static ulong[] Rotations(int id)
    {
        var modules = Tag36h11.InkedModules(id);
        var result = new ulong[4];
        for (int rotation = 0; rotation < 4; rotation++)
        {
            ulong bits = 0;
            for (int row = 0; row < 6; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    var (r, c) = rotation switch
                    {
                        1 => (col, 5 - row),
                        2 => (5 - row, 5 - col),
                        3 => (5 - col, row),
                        _ => (row, col),
                    };
                    bits = (bits << 1) | (modules[r + 1, c + 1] ? 0UL : 1UL);
                }
            }

            result[rotation] = bits;
        }

        return result;
    }
}

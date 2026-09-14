namespace GroupLab.Core.Statistics;

/// <summary>
/// The engine's seedable generator, xoshiro256** seeded through SplitMix64, with unbiased bounded integers and normal deviates
/// by Marsaglia's polar method. docs/STATISTICS.md section 6 requires the seed of every resampling to be recorded so a reported
/// interval can be reproduced exactly, and that is only true of a generator whose algorithm the engine owns: the runtime's
/// <see cref="Random"/> makes no promise that a seed means the same stream from one version to the next.
/// </summary>
internal sealed class StatisticsRandom
{
    private ulong s0, s1, s2, s3;
    private double spare = double.NaN;

    public StatisticsRandom(ulong seed)
    {
        ulong state = seed;
        s0 = SplitMix(ref state);
        s1 = SplitMix(ref state);
        s2 = SplitMix(ref state);
        s3 = SplitMix(ref state);
    }

    private static ulong SplitMix(ref ulong state)
    {
        ulong z = state += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    public ulong NextUInt64()
    {
        ulong result = ulong.RotateLeft(s1 * 5, 7) * 9, t = s1 << 17;
        s2 ^= s0;
        s3 ^= s1;
        s1 ^= s2;
        s0 ^= s3;
        s2 ^= t;
        s3 = ulong.RotateLeft(s3, 45);
        return result;
    }

    /// <summary>A uniform double in [0, 1), from the top 53 bits.</summary>
    public double NextDouble() => (NextUInt64() >> 11) * (1.0 / (1UL << 53));

    /// <summary>A uniform integer in [0, <paramref name="bound"/>), by Lemire's multiply-and-reject, so no index is favoured.</summary>
    public int NextInt(int bound)
    {
        ulong range = (ulong)bound;
        while (true)
        {
            UInt128 product = (UInt128)NextUInt64() * range;
            ulong low = (ulong)product;
            if (low >= (0UL - range) % range)
            {
                return (int)(product >> 64);
            }
        }
    }

    /// <summary>A standard normal deviate.</summary>
    public double NextNormal()
    {
        if (!double.IsNaN(spare))
        {
            double value = spare;
            spare = double.NaN;
            return value;
        }

        while (true)
        {
            double u = (2 * NextDouble()) - 1, v = (2 * NextDouble()) - 1, s = (u * u) + (v * v);
            if (s > 0 && s < 1)
            {
                double f = Math.Sqrt(-2 * Math.Log(s) / s);
                spare = v * f;
                return u * f;
            }
        }
    }
}

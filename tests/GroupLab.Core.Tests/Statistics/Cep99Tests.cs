using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Tests.Statistics;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 227 section 3: CEP 99, and a CEP for any percent, from the same sigma and model as CEP 50, 90 and 95, checked
/// against a circular normal group whose answer is known; its range scales with sigma's; and a CEP further into the tail than the shots
/// reach says so.
/// </summary>
public class Cep99Tests
{
    private static List<PointD> Normal(int n, double sigma, int seed)
    {
        var random = new Random(seed);
        var shots = new List<PointD>(n);
        for (int i = 0; i < n; i++)
        {
            double u1 = 1 - random.NextDouble(), u2 = random.NextDouble();
            double r = sigma * Math.Sqrt(-2 * Math.Log(u1));
            shots.Add(new PointD(r * Math.Cos(2 * Math.PI * u2), r * Math.Sin(2 * Math.PI * u2)));
        }

        return shots;
    }

    [Theory]
    [InlineData(99, 3.0349)]
    [InlineData(97.5, 2.7162)]
    [InlineData(50, 1.1774)]
    [InlineData(99.9, 3.7169)]
    public void ACepMatchesTheRayleighQuantileOfAKnownGroup(double percent, double sigmas)
    {
        var shots = Normal(40000, 1.0, 227);
        var cep = GroupAnalysis.Cep(shots, percent)!.Value;
        Assert.Equal(sigmas, cep.Value, 1);
        Assert.InRange(cep.Value, sigmas * 0.98, sigmas * 1.02);

        var centre = GroupStatistics.Centre(shots);
        double inside = shots.Count(s => Math.Sqrt(((s.X - centre.X) * (s.X - centre.X)) + ((s.Y - centre.Y) * (s.Y - centre.Y))) <= cep.Value) / (double)shots.Count;
        Assert.InRange(inside, (percent / 100) - 0.005, (percent / 100) + 0.005);
    }

    [Fact]
    public void Cep99IsAmongTheFiguresWithTheSameRangeAsSigma()
    {
        var shots = Normal(10, 0.5, 99);
        var rayleigh = GroupStatistics.Rayleigh(shots);
        var cep = GroupAnalysis.Cep(shots, 99)!.Value;

        Assert.Equal(rayleigh.Sigma.Value * Math.Sqrt(-2 * Math.Log(0.01)), cep.Value, 9);
        Assert.Equal(rayleigh.Sigma.Lower / rayleigh.Sigma.Value, cep.Lower / cep.Value, 9);
        Assert.Equal(rayleigh.Sigma.Upper / rayleigh.Sigma.Value, cep.Upper / cep.Value, 9);
    }

    [Theory]
    [InlineData(10, 99, true)]
    [InlineData(25, 99, true)]
    [InlineData(100, 99, false)]
    [InlineData(25, 95, false)]
    [InlineData(500, 99.9, true)]
    public void ACepBeyondTheShotsSaysItIsTheModelsTail(int shots, double percent, bool said)
    {
        string? note = GroupAnalysis.CepTailNote(shots, percent);
        Assert.Equal(said, note is not null);
        if (note is not null)
        {
            Assert.Contains($"With {shots} shots", note, StringComparison.Ordinal);
            Assert.Contains(percent == 99 ? "About 100 shots" : "About 1000 shots", note, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void NoCepFromTooFewShotsOrAPercentOutOfRange()
    {
        Assert.Null(GroupAnalysis.Cep(Normal(4, 1, 1), 99));
        Assert.Null(GroupAnalysis.Cep(Normal(10, 1, 1), 100));
        Assert.Null(GroupAnalysis.Cep(Normal(10, 1, 1), 0));
    }
}

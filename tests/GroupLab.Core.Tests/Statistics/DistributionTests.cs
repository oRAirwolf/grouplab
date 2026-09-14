using GroupLab.Core.Statistics;

namespace GroupLab.Core.Tests.Statistics;

/// <summary>
/// The special functions and distributions against closed forms, docs/STATISTICS.md section 15.3: the closed-form
/// quantities built on them are compared with shotGroups at 1e-12 relative, so every check here is tighter than that.
/// </summary>
public class DistributionTests
{
    private static void Close(double expected, double actual, double relative = 1e-13) =>
        Assert.True(Math.Abs(expected - actual) <= relative * Math.Max(Math.Abs(expected), 1e-300), $"expected {expected:R}, got {actual:R}");

    [Fact]
    public void LogGammaMatchesItsExactValues()
    {
        Close(0.5 * Math.Log(Math.PI), SpecialFunctions.LogGamma(0.5));
        Close(Math.Log(362880), SpecialFunctions.LogGamma(10));
        Close(Math.Log(120), SpecialFunctions.LogGamma(6));
        Close(Math.Log(3 * Math.Sqrt(Math.PI) / 4), SpecialFunctions.LogGamma(2.5));
        double sum = 0;
        for (int k = 1; k < 400; k++)
        {
            sum += Math.Log(k);
        }

        Close(sum, SpecialFunctions.LogGamma(400), 1e-15);
    }

    [Fact]
    public void C4NeverOverflowsAndMatchesItsSmallValue()
    {
        // c4(2) = sqrt(2 / pi).
        Close(Math.Sqrt(2 / Math.PI), SpecialFunctions.C4(2));
        Assert.True(SpecialFunctions.C4(2000) is > 0.9998 and <= 1);
    }

    [Theory]
    [InlineData(0.975, 1.959963984540054)]
    [InlineData(0.5, 0)]
    [InlineData(0.8413447460685429, 1)]
    public void NormalQuantileMatchesKnownValues(double p, double z) => Assert.True(Math.Abs(Distributions.NormalQuantile(p) - z) < 1e-14);

    [Theory]
    [InlineData(1e-10)]
    [InlineData(0.025)]
    [InlineData(0.5)]
    [InlineData(0.975)]
    [InlineData(1 - 1e-10)]
    public void ChiSquareOnTwoDegreesIsExponential(double p) => Close(-2 * SpecialFunctions.Log1P(-p), Distributions.ChiSquareQuantile(p, 2));

    [Theory]
    [InlineData(0.025, 38)]
    [InlineData(0.975, 38)]
    [InlineData(0.5, 1)]
    [InlineData(0.99, 1058)]
    [InlineData(0.01, 4)]
    public void ChiSquareQuantileInvertsItsCdf(double p, double df)
    {
        double x = Distributions.ChiSquareQuantile(p, df);
        var (lower, upper) = Distributions.ChiSquare(x, df);
        Close(p, p < 0.5 ? lower : 1 - upper, 1e-14);
    }

    [Theory]
    [InlineData(0.975)]
    [InlineData(0.6)]
    [InlineData(0.01)]
    public void StudentTOnOneAndTwoDegreesMatchesItsClosedForms(double p)
    {
        Close(Math.Tan(Math.PI * (p - 0.5)), Distributions.StudentTQuantile(p, 1));
        Close((2 * p - 1) / Math.Sqrt(2 * p * (1 - p)), Distributions.StudentTQuantile(p, 2));
    }

    [Theory]
    [InlineData(0.5, 18)]
    [InlineData(0.95, 19)]
    [InlineData(0.025, 7)]
    public void FOnTwoDegreesMatchesItsClosedForm(double p, double m)
    {
        // P(F <= x) = 1 - (1 + 2x/m)^(-m/2), so x = m/2 ((1 - p)^(-2/m) - 1).
        Close(m / 2 * (Math.Pow(1 - p, -2 / m) - 1), Distributions.FQuantile(p, 2, m));
    }

    [Fact]
    public void IncompleteBetaIsSymmetricAtAHalf() => Close(0.5, SpecialFunctions.IncompleteBeta(0.5, 7.5, 7.5).Lower, 1e-14);
}

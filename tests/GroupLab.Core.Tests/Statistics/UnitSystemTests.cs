using System.Globalization;
using System.Text.Json;
using GroupLab.Core.Imaging;
using GroupLab.Core.Statistics;
using GroupLab.Core.Tests.Support;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Statistics;

/// <summary>
/// docs/STATISTICS.md section 15.5 point 2: <c>DFcm</c> and <c>DFinch</c> are the same shots in centimetres at 25 m and in inches
/// at 27.34 yd, and must produce identical results after conversion.
/// <para>
/// <b>They are not quite the same data.</b> Every centimetre shot is an inch shot times 2.54 to 1e-15, but shot 242 is in series 5 of
/// <c>DFcm</c> and series 4 of <c>DFinch</c>, so series 4 and 5 hold different shots in the two frames (docs/QUESTIONS-FOR-PLANNING.md
/// question 11). The test asserts that this one shot is the only difference, then compares every series with both frames grouped the
/// inch file's way. Linear results agree to machine precision. Angular results cannot agree exactly, because 25 m is 27.340332 yd and
/// the data says 27.34, and the test asserts that the angular difference is that rounding and nothing else.
/// </para>
/// </summary>
public class UnitSystemTests(ITestOutputHelper output)
{
    private sealed record Shot(int Index, PointD Point, string Series, double Distance);

    private static List<Shot> Load(string dataset)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(Repo.PathTo("test", "fixtures", "shotgroups", $"shotGroups_{dataset}.json")));
        var root = document.RootElement;
        var values = root.GetProperty("values");
        var labels = root.GetProperty("seriesLabels").EnumerateArray().Select(e => e.GetString()!).ToList();
        int n = root.GetProperty("nShots").GetInt32();
        return [.. Enumerable.Range(1, n).Select(i =>
        {
            string at = i.ToString(CultureInfo.InvariantCulture);
            double D(string key) => values.GetProperty(key + at).GetDouble();
            return new Shot(i, new PointD(D("shots.x."), D("shots.y.")), labels[(int)D("shots.seriesIndex.") - 1], D("shots.distance."));
        })];
    }

    [Fact]
    public void MetricAndImperialCopiesAgreeAfterConversion()
    {
        var metric = Load("DFcm");
        var imperial = Load("DFinch");
        var byPosition = imperial.ToDictionary(s => (Math.Round(s.Point.X, 9), Math.Round(s.Point.Y, 9)));

        // Every centimetre shot has its inch twin; record the series each frame puts it in.
        var pairs = metric.Select(m => (Metric: m, Imperial: byPosition[(Math.Round(m.Point.X / 2.54, 9), Math.Round(m.Point.Y / 2.54, 9))])).ToList();
        var moved = pairs.Where(p => p.Metric.Series != p.Imperial.Series).Select(p => (p.Metric.Index, From: p.Metric.Series, To: p.Imperial.Series)).ToList();
        output.WriteLine("shots in different series: " + string.Join(", ", moved.Select(m => $"shot {m.Index}, series {m.From} in DFcm and {m.To} in DFinch")));
        Assert.Equal([(242, "5", "4")], moved);

        double worstLinear = 0, worstAngular = 0;
        foreach (var group in pairs.GroupBy(p => p.Imperial.Series))
        {
            var cm = group.Select(p => p.Metric.Point).ToList();
            var inch = group.Select(p => p.Imperial.Point).ToList();

            double[] Linear(List<PointD> shots)
            {
                var ray = GroupStatistics.Rayleigh(shots);
                var (xx, xy, yy) = GroupStatistics.Covariance(shots);
                double es = GroupGeometry.MaximumPairDistance(shots).Distance;
                var fromRange = RangeStatistics.Sigma(RangeStatistic.ExtremeSpread, es, shots.Count);
                return [ray.Sigma.Value, ray.Sigma.Lower, ray.Sigma.Upper, ray.MeanRadius.Value, GroupStatistics.CepCorrNormal(xx, xy, yy, 0.5),
                    GroupStatistics.CepGrubbsPatnaik(xx, xy, yy, 0.9), es, GroupGeometry.MinimumEnclosingCircle(shots).Radius, GroupGeometry.MinimumAreaBox(shots).Diagonal,
                    GroupGeometry.MinimumVolumeEllipse(shots).SemiMajor, fromRange.Value, fromRange.Upper];
            }

            // The centimetre results are converted after they are computed, which is what "after conversion" means to a user.
            var fromMetric = Linear(cm).Select(v => v / 2.54).ToArray();
            var fromImperial = Linear(inch);
            for (int i = 0; i < fromMetric.Length; i++)
            {
                worstLinear = Math.Max(worstLinear, Math.Abs(fromMetric[i] - fromImperial[i]) / Math.Abs(fromImperial[i]));
            }

            double moaFromMetric = Angular.ToAngle(GroupStatistics.Rayleigh(cm).MeanRadius.Value, group.First().Metric.Distance, 100, AngularUnit.Moa);
            double moaFromImperial = Angular.ToAngle(GroupStatistics.Rayleigh(inch).MeanRadius.Value, group.First().Imperial.Distance, 36, AngularUnit.Moa);
            worstAngular = Math.Max(worstAngular, Math.Abs(moaFromMetric - moaFromImperial) / moaFromImperial);
        }

        double rounding = (25 / 0.9144 / 27.34) - 1;
        output.WriteLine($"worst linear relative difference {worstLinear:0.0e+00}; worst angular {worstAngular:0.00000e+00}, against the distance rounding {rounding:0.00000e+00}");
        Assert.True(worstLinear < 1e-12, $"linear results differ by {worstLinear:R}");
        Assert.True(Math.Abs(worstAngular - rounding) < 1e-9, $"angular results differ by {worstAngular:R}, not by the distance rounding {rounding:R}");
    }
}

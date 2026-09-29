using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>NOTES-FROM-PLANNING.md entry 278 section 5b: several aim points on one photograph, each with its own figures.</summary>
public class ByAimPointTests
{
    [Fact]
    public void EachAimPointIsMeasuredFromItselfWithoutShotsLeftOut()
    {
        var session = new MarkingSession();
        session.Load(MarkingState.Empty);
        session.SetScale(new LengthReference(new PointD(0, 0), new PointD(1000, 0), 1)); // 1000 pixels to the inch
        int left = session.AddBull(new PointD(20000, 20000)), right = session.AddBull(new PointD(30000, 20000));
        // Around the left aim point, a plus of five shots 0.2 in out with one in the middle; around the right, a plus 0.5 in right of it and
        // one far out, left out. Five shots is the fewest GroupLab quotes a spread for.
        foreach (var (x, y, bull) in new[] { (20200.0, 20000.0, left), (19800.0, 20000.0, left), (20000.0, 20200.0, left), (20000.0, 19800.0, left), (20000.0, 20000.0, left),
                     (30500.0, 20000.0, right), (30500.0, 20100.0, right), (30500.0, 19900.0, right), (30600.0, 20000.0, right), (30400.0, 20000.0, right), (33000.0, 20000.0, right) })
        {
            session.AddShot(new PointD(x, y), bull);
        }

        session.SetExclusion(session.State.Shots.MaxBy(s => s.Id)!.Id, ExclusionReason.CalledFlyer);
        var byAim = GroupAnalysis.ByAimPoint(session.State);
        Assert.Equal(2, byAim.Count);
        var (_, l) = byAim[0];
        var (_, r) = byAim[1];
        Assert.Equal(5, l!.Shots);
        Assert.Equal(0, l.CentreFromAim!.Value.X, 9);
        Assert.Equal(0.4, l.ExtremeSpread!.Value, 9);
        Assert.Equal(5, r!.Shots);
        Assert.Equal(0.5, r.CentreFromAim!.Value.X, 9); // measured from its own aim point, not the left one's
    }
}

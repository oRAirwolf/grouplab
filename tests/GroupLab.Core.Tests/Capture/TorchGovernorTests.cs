using GroupLab.Core.Capture;

namespace GroupLab.Core.Tests.Capture;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 302: the torch on Auto starts low, steps up only while the paper is dim, steps down or goes off on glare, a
/// hotspot or paper already bright, and never flickers.
/// </summary>
public class TorchGovernorTests
{
    // A registered frame with the paper at a median level and a share of it blown out.
    private static CaptureQuality Frame(double paperLevel, double clipped = 0)
    {
        double exposure = Math.Min(Math.Clamp((CaptureQualities.UselessClipped - clipped) / (CaptureQualities.UselessClipped - CaptureQualities.FineClipped), 0, 1),
            Math.Clamp((paperLevel - CaptureQualities.UselessPaperLevel) / (CaptureQualities.FinePaperLevel - CaptureQualities.UselessPaperLevel), 0, 1));
        return new CaptureQuality(80, "good", 0.001, 1, clipped, paperLevel, exposure, 5, 1, 200, 1, 34, 34, 1);
    }

    private static List<TorchChange> Run(TorchGovernor torch, IEnumerable<(CaptureQuality Quality, double? Evenness)> frames, ref long now, long stepMs = 100)
    {
        var changes = new List<TorchChange>();
        foreach (var (quality, evenness) in frames)
        {
            if (torch.Next(quality, evenness, now += stepMs) is { } change)
            {
                changes.Add(change);
            }
        }

        return changes;
    }

    private static IEnumerable<(CaptureQuality, double?)> Repeat(CaptureQuality quality, int count, double? evenness = 0.95) => Enumerable.Repeat<(CaptureQuality, double?)>((quality, evenness), count);

    [Fact]
    public void ItStartsAtTheLowestLevelAndStepsUpOnlyWhileTheSheetIsDim()
    {
        var torch = new TorchGovernor(5);
        long now = 0;
        var changes = Run(torch, Repeat(Frame(80), 10), ref now);
        Assert.Equal([new TorchChange(1, "dim")], changes);

        // Still dim: one more step after the exposure has settled, then enough light and it stays.
        changes = Run(torch, Repeat(Frame(90), 20).Concat(Repeat(Frame(180), 60)), ref now);
        Assert.Equal([new TorchChange(2, "dim")], changes);
        Assert.Equal(2, torch.Level);
    }

    [Fact]
    public void GlareStepsItDownAndThatLevelIsNeverTriedAgain()
    {
        var torch = new TorchGovernor(5, level: 3);
        long now = 0;
        var changes = Run(torch, Repeat(Frame(250, clipped: 0.1), 30), ref now);
        Assert.Equal(new TorchChange(2, "glare"), changes[0]);

        // Dim again at a lower level: it may step up, but never back to the level that glared.
        changes = Run(torch, Repeat(Frame(250, clipped: 0.1), 30).Concat(Repeat(Frame(60), 200)), ref now);
        Assert.True(torch.Level < 3);
        Assert.DoesNotContain(changes, c => c.Level >= 3);
    }

    [Fact]
    public void GlareAtTheLowestLevelTurnsItOff()
    {
        var torch = new TorchGovernor(1, level: 1);
        long now = 0;
        var changes = Run(torch, Repeat(Frame(252, clipped: 0.3), 10).Concat(Repeat(Frame(100), 100)), ref now);
        Assert.Equal([new TorchChange(0, "glare")], changes);
    }

    [Fact]
    public void PaperAlreadyBrightTurnsItDown()
    {
        var torch = new TorchGovernor(1, level: 1);
        long now = 0;
        var changes = Run(torch, Repeat(Frame(240), 10), ref now);
        Assert.Equal([new TorchChange(0, "bright")], changes);
    }

    [Fact]
    public void AHotspotTheTorchMadeStepsItDown()
    {
        var torch = new TorchGovernor(5);
        long now = 0;
        // Even without it but dim; with it, the bulls' light far less even.
        var changes = Run(torch, Repeat(Frame(90), 10, 0.9).Concat(Repeat(Frame(170), 30, 0.6)), ref now);
        Assert.Equal([new TorchChange(1, "dim"), new TorchChange(0, "hotspot")], changes);
    }

    [Fact]
    public void ItNeverFlickers()
    {
        // Light that swings every few frames between too dim and too bright: at least the settling time between changes, and it stops
        // changing after two changes of direction.
        var torch = new TorchGovernor(5);
        long now = 0;
        var swinging = Enumerable.Range(0, 600).Select(i => ((i / 6) % 2 == 0 ? Frame(80) : Frame(242), (double?)0.95));
        var times = new List<long>();
        foreach (var (quality, evenness) in swinging)
        {
            if (torch.Next(quality, evenness, now += 100) is not null)
            {
                times.Add(now);
            }
        }

        Assert.All(times.Zip(times.Skip(1)), pair => Assert.True(pair.Second - pair.First >= TorchGovernor.SettleMs));
        Assert.True(times.Count <= 3, $"{times.Count} changes");
    }

    [Fact]
    public void APhoneWithNoTorchIsNeverAskedToChange()
    {
        var torch = new TorchGovernor(0);
        long now = 0;
        Assert.Empty(Run(torch, Repeat(Frame(60), 50), ref now));
    }

    [Fact]
    public void AFrameWithNoSheetChangesNothing()
    {
        var torch = new TorchGovernor(5);
        for (long t = 0; t < 5000; t += 100)
        {
            Assert.Null(torch.Next(null, null, t));
        }
    }
}

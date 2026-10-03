using GroupLab.Cli.Imaging;
using GroupLab.Core.Evaluation;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;
using GroupLab.Core.Trace;

namespace GroupLab.Core.Tests.Registration;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 356 section 5: opening a picture decides, in order, a GroupLab sheet that reads, a store-bought target
/// recognized, a picture that looks like a GroupLab sheet but would not read (an error), and anything else (not one). The "looks like" test
/// must never fire on a target with no GroupLab marks: here on the any-target scoreboard's drawn targets, which run everywhere; the
/// store-bought scans and photographs it was measured on are in the commit that brought it.
/// </summary>
public class SheetOpeningTests
{
    [Fact]
    public void TheDrawnTargetsOfTheAnyTargetScoreboardDoNotLookLikeAGroupLabSheet()
    {
        var backend = new OpenCvSharpBackend();
        foreach (var kind in Scoreboard.AnyTargetKinds)
        {
            var (value, _) = Scoreboard.AnyTargetPicture(kind, Scoreboard.AnyTargetSeeds[0]);
            var look = SheetLook.Of(value, backend);
            Assert.False(look.LooksLikeGroupLab, $"{kind}: {look.Markers} markers, {look.Codes.Count} codes");
        }
    }

    [Fact]
    public void AGroupLabSheetWithItsCodesCutOffStillLooksLikeOne()
    {
        var page = SceneBuilder.Build(BuiltIns.Load("GL-RF25-LTR.gltd.json")).Pages[0];
        var codeless = page with { Items = [.. page.Items.Where(i => i.Layer != SceneLayer.Codes)] };
        var render = SceneRasterizer.Rasterize(codeless, 300);
        var backend = new OpenCvSharpBackend();
        var identity = SheetIdentification.Identify(render, SheetIdentification.Candidates([Repo.PathTo("targets")]), backend, new TraceRecorder());
        Assert.Null(identity.Definition);

        var look = SheetLook.Of(render, backend, identity.CodesRead);
        Assert.True(look.Markers >= SheetLook.LeastMarkers, $"{look.Markers} markers");
        Assert.Equal(OpeningOutcome.LooksLikeGroupLab, SheetOpening.Decide(identity, false, () => look));
    }

    [Fact]
    public void TheRuleIsAppliedInItsOrder()
    {
        var named = new SheetIdentity(BuiltIns.Load("GL-CF25-LTR.gltd.json"), "GL-20J3-Y141-0BN3-EYME", 0, 1, null);
        var none = new SheetIdentity(null, null, 0, 0, "no code on the sheet could be read");
        var unknown = new SheetIdentity(null, "GL-MBTW-2V2M-JTPE-4518", 0, 1, "the codes do not hold a definition");
        SheetLook Never() => throw new InvalidOperationException("the look is not needed");
        var nothing = new SheetLook(0, [], 0);
        var markers = new SheetLook(3, [], 0);
        IReadOnlyList<PointD> box = [new(0, 0), new(10, 0), new(10, 10), new(0, 10)];

        Assert.Equal(OpeningOutcome.Sheet, SheetOpening.Decide(named, true, Never));
        Assert.Equal(OpeningOutcome.StoreTarget, SheetOpening.Decide(none, true, Never));
        Assert.Equal(OpeningOutcome.LooksLikeGroupLab, SheetOpening.Decide(unknown, false, Never));
        Assert.Equal(OpeningOutcome.LooksLikeGroupLab, SheetOpening.Decide(none, false, () => markers));
        Assert.Equal(OpeningOutcome.NotGroupLab, SheetOpening.Decide(none, false, () => nothing));

        // A maker's QR code read as anything, a web address, is not a GroupLab sheet; two that read as nothing, or one beside a marker, are.
        Assert.False(new SheetLook(0, [box], 1).LooksLikeGroupLab);
        Assert.False(new SheetLook(0, [box], 0).LooksLikeGroupLab);
        Assert.True(new SheetLook(0, [box, box], 0).LooksLikeGroupLab);
        Assert.True(new SheetLook(1, [box], 1).LooksLikeGroupLab);
    }
}

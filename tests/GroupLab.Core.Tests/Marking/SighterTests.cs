using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 105 section 8: sighters are ignored unless analysed. Detection and matching still run over them, so what is
/// ignored is everything after: their size flags and the contests only between sighters. A contest between a sighter bull and a scoring bull
/// is never ignored, because the scoring bull's shot depends on it. Analysed, they are a group of their own, never pooled.
/// </summary>
public class SighterTests
{
    private static readonly LengthReference Scale = new(new PointD(0, 0), new PointD(100, 0), 1);

    // Three scoring bulls in a row, and two sighters beneath the outer two.
    private static readonly BullAim[] Bulls =
    [
        new(0, "1", new PointD(100, 100)), new(1, "2", new PointD(250, 100)), new(2, "3", new PointD(400, 100)),
        new(3, "S1", new PointD(100, 300), Scoring: false), new(4, "S2", new PointD(250, 300), Scoring: false),
    ];

    private static MarkingSession Loaded(params (PointD Image, AssignedShot Assignment, DetectedOversize? Oversize)[] shots)
    {
        var session = new MarkingSession();
        session.Open("sheet.png");
        var result = new ShotAssignmentResult(AssignmentMethod.OneToOne, "test", [.. shots.Select(s => s.Assignment)]);
        session.LoadDetections(Scale, Bulls, [.. shots.Select(s => new DetectedShot(s.Image, s.Assignment, 0.3, s.Oversize))], result, [], "test");
        return session;
    }

    /// <summary>A size flag on a sighter's mark and a contest between the two sighters are ignored, and come back when sighters are analysed.</summary>
    [Fact]
    public void WhatConcernsOnlySightersIsIgnoredUntilTheyAreAnalysed()
    {
        var session = Loaded(
            (new PointD(105, 100), new AssignedShot(0, 0, 13, 0, 13, 3000, false), null),
            (new PointD(110, 305), new AssignedShot(1, 3, 28, 3, 28, 3000, false), new DetectedOversize(1.6, false)),
            (new PointD(180, 300), new AssignedShot(2, 3, 203, 4, 178, 25, true), null));

        var ignored = ReviewQueue.For(session.State);
        Assert.DoesNotContain(ignored, i => i.Kind == ReviewKind.Oversized);
        Assert.DoesNotContain(ignored, i => i.Kind == ReviewKind.Contested);

        var analysed = ReviewQueue.For(session.State, analyseSighters: true);
        Assert.Contains(analysed, i => i.Kind == ReviewKind.Oversized);
        Assert.Contains(analysed, i => i.Kind == ReviewKind.Contested);
    }

    /// <summary>
    /// The case to test hardest: a sighter's hole that sits between a sighter bull and a scoring bull is an item whether sighters are analysed or
    /// not, and so is a scoring shot whose nearest bull is a sighter. Either way a scoring bull's shot depends on the answer.
    /// </summary>
    [Fact]
    public void AContestBetweenASighterAndAScoringBullIsNeverIgnored()
    {
        var session = Loaded(
            (new PointD(400, 190), new AssignedShot(0, 4, 245, 2, 229, 16, true), null),
            (new PointD(250, 205), new AssignedShot(1, 1, 267, 4, 250, 17, true), null));

        foreach (bool analyse in new[] { false, true })
        {
            var contested = ReviewQueue.For(session.State, analyse).Where(i => i.Kind == ReviewKind.Contested).ToList();
            Assert.Equal(2, contested.Count);
            Assert.Contains(contested, i => i.Bull == 4);
            Assert.Contains(contested, i => i.Bull == 1);
        }
    }

    /// <summary>Analysed, the sighters are a group of their own: the scoring group never counts them, and theirs holds only them.</summary>
    [Fact]
    public void AnalysedSightersAreTheirOwnGroupAndNeverPooled()
    {
        var session = new MarkingSession();
        session.SetScale(Scale);
        session.LoadDetections(Scale, Bulls,
            [
                (new PointD(105, 98), (int?)0), (new PointD(96, 104), 0), (new PointD(252, 97), 1), (new PointD(247, 104), 1), (new PointD(404, 100), 2),
                (new PointD(103, 306), 3), (new PointD(98, 296), 3), (new PointD(254, 302), 4),
            ], "test");
        var state = session.State;

        Assert.True(GroupAnalysis.HasSighters(state));
        Assert.Equal(5, GroupAnalysis.Analyse(state).AllShots!.Shots);
        Assert.Equal(3, GroupAnalysis.Analyse(state).SighterShots);

        var sighters = GroupAnalysis.Sighters(state);
        Assert.Equal(3, GroupAnalysis.Analyse(sighters).AllShots!.Shots);
        Assert.Equal(0, GroupAnalysis.Analyse(sighters).SighterShots);
        Assert.All(sighters.Shots, s => Assert.Contains(s.Bull!.Value, new[] { 3, 4 }));

        // Three sighters are too few to quote a dispersion, and the zero correction says what it can at that count.
        Assert.NotNull(GroupAnalysis.Analyse(sighters).AllShots!.DispersionWithheld);
        Assert.Contains(state.Shots, s => GroupAnalysis.OnSighter(state, s));
    }
}

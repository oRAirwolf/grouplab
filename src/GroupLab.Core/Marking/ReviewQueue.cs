using System.Globalization;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Marking;

/// <summary>What a review item is about, in the order the queue puts them.</summary>
public enum ReviewKind
{
    /// <summary>A shot the matching gave a bull other than its nearest, or whose margin is too small to trust, or that an edit moved.</summary>
    Contested,

    /// <summary>A shot the detector flagged as covering about two holes' area.</summary>
    Oversized,

    /// <summary>A scoring bull holding more than one shot.</summary>
    Doubled,

    /// <summary>A shot with no bull.</summary>
    Unassigned,

    /// <summary>A scoring bull with no shot, where the detector refused a candidate.</summary>
    Refused,

    /// <summary>The marks disagree with the number of rounds the person said they fired (NOTES-FROM-PLANNING.md entry 95 section 2).</summary>
    Count,
}

/// <summary>
/// One thing a person should look at, DESIGN.md section 13 and the assignment editor of NOTES-FROM-PLANNING.md entries 69 and 83. It names the
/// shot or bull, says what was found in a sentence that names no single cause, and offers the choices that settle it. <see cref="Key"/>
/// identifies it across edits, so a person's "leave it as it is" is remembered.
/// </summary>
public sealed record ReviewItem(string Key, ReviewKind Kind, int? ShotId, int? Bull, PointD Image, string Sentence, IReadOnlyList<ReviewChoice> Choices, bool Resolved);

/// <summary>One way to settle a review item.</summary>
public sealed record ReviewChoice(string Label, ReviewAction Action, int? Bull = null);

/// <summary>What a choice does to the marking.</summary>
public enum ReviewAction
{
    /// <summary>Pin the shot to the bull named.</summary>
    AssignBull,

    /// <summary>Mark the shot as not a shot.</summary>
    NotAShot,

    /// <summary>Leave the marking as it is and stop asking.</summary>
    Keep,

    /// <summary>Place a shot where the refused candidate was, on the bull named.</summary>
    AddShot,

    /// <summary>Take one oversized mark as two shots, one at each half of it (NOTES-FROM-PLANNING.md entry 94 section 4).</summary>
    SplitIntoTwo,
}

/// <summary>
/// The review queue: every item the marking wants a person to look at, contested assignments first, then oversized marks, doubled bulls,
/// shots with no bull, and refused candidates in empty scoring bulls, each in the sheet's order. An item is resolved once a person has
/// decided it: chosen the shot's bull, marked it not a shot, or said to keep it. <see cref="Apply"/> carries out a choice.
/// </summary>
public static class ReviewQueue
{
    /// <summary>Below this margin between a shot's two nearest bulls a 0.05 in registration error would flip it, DESIGN.md section 13 [r3].</summary>
    public const double ContestedMarginInches = 0.15;

    /// <summary>
    /// A refused candidate is offered only when it was refused as too small and is at least this size: a hole just under the size gate, as
    /// the concept's example has. Candidates refused for their shape are residue far more often than holes, on the corpus's photographs.
    /// </summary>
    public const double RefusedCandidateInches = 0.10;

    /// <summary>How many candidates a count item names, most likely first.</summary>
    public const int CountCandidates = 3;

    /// <param name="analyseSighters">
    /// NOTES-FROM-PLANNING.md entry 105 section 8: sighters are ignored unless the person asks for them. Ignored, nothing that concerns only
    /// sighter bulls is an item: no size flag on a sighter's mark and no contest between two sighters. A contest that involves a scoring bull
    /// is still an item, because the scoring bull's shot depends on it. Detection and one-to-one matching ran over the sighters either way,
    /// which is what keeps a sighter's hole off the scoring bulls above it.
    /// </param>
    public static IReadOnlyList<ReviewItem> For(MarkingState state, bool analyseSighters = false)
    {
        ArgumentNullException.ThrowIfNull(state);
        var sighterBulls = state.Bulls.Where(b => !b.Scoring).Select(b => b.Index).ToHashSet();
        bool OnlySighters(params int?[] involved) =>
            !analyseSighters && involved.Any(b => b is not null) && involved.All(b => b is null || sighterBulls.Contains(b.Value));
        var inv = CultureInfo.InvariantCulture;
        var labels = ShotLabels.For(state).ToDictionary(l => l.ShotId, l => l.Text ?? "");
        var bulls = state.Bulls.ToDictionary(b => b.Index);
        string BullName(int? index) => index is { } i && bulls.TryGetValue(i, out var b) ? b.Label : "none";
        var shots = state.Shots.Where(s => s.IsShot).ToList();
        var items = new List<ReviewItem>();
        bool Dismissed(string key) => state.Dismissed?.Contains(key) == true;

        // Contested: the matching's own figures, where the detection ran.
        foreach (var shot in shots)
        {
            if (state.Assignment?.For(shot.Id) is not { } detail)
            {
                continue;
            }

            bool overridden = detail.Bull is { } held && held != detail.NearestBull;
            bool moved = detail.Bull != detail.DetectedBull;
            if ((!detail.Ambiguous && !overridden && !moved) || OnlySighters(shot.Bull, detail.Bull, detail.NearestBull, detail.DetectedBull))
            {
                continue;
            }

            string key = $"contested:{shot.Id}";
            string holder = shots.FirstOrDefault(o => o.Id != shot.Id && o.Bull == detail.NearestBull) is { } other && state.Assignment.For(other.Id) is { } od
                ? string.Create(inv, $"bull {BullName(detail.NearestBull)} already holds shot {labels[other.Id]} at {od.DistanceInches:0.000} in")
                : string.Create(inv, $"bull {BullName(detail.NearestBull)} holds no other shot");
            string sentence = moved
                ? string.Create(inv, $"Shot {labels[shot.Id]} was on bull {BullName(detail.DetectedBull)} and an edit moved it to bull {BullName(detail.Bull)}, {detail.DistanceInches:0.000} in away. Its nearest bull is {BullName(detail.NearestBull)}, at {detail.NearestInches:0.000} in.")
                : overridden
                    ? string.Create(inv, $"This hole is {detail.NearestInches:0.000} in from bull {BullName(detail.NearestBull)} and {detail.DistanceInches:0.000} in from bull {BullName(detail.Bull)}. Nearest bull says {BullName(detail.NearestBull)}, but {holder}. One-to-one matching gives it to bull {BullName(detail.Bull)}.")
                    : detail.MarginInches < ContestedMarginInches
                        ? string.Create(inv, $"Shot {labels[shot.Id]} is {detail.NearestInches:0.000} in from bull {BullName(detail.NearestBull)}, and its next bull is only {detail.MarginInches:0.000} in further. A small registration error would change which bull it reads as.")
                        : string.Create(inv, $"Shot {labels[shot.Id]} is on bull {BullName(detail.Bull)}, its nearest, at {detail.NearestInches:0.000} in. Its bulls hold more shots than bulls, so no one-to-one matching was forced and each shot there was left on its nearest bull: check it is the one it was fired at.");
            var choices = new List<ReviewChoice>();
            if (shot.Bull is { } assigned)
            {
                choices.Add(new ReviewChoice($"Bull {BullName(assigned)}, as matched", ReviewAction.AssignBull, assigned));
            }

            if (detail.NearestBull != shot.Bull)
            {
                choices.Add(new ReviewChoice($"Bull {BullName(detail.NearestBull)}", ReviewAction.AssignBull, detail.NearestBull));
            }

            if (moved && detail.DetectedBull is { } before && before != shot.Bull && before != detail.NearestBull)
            {
                choices.Add(new ReviewChoice($"Bull {BullName(before)}", ReviewAction.AssignBull, before));
            }

            choices.Add(new ReviewChoice("Not a shot", ReviewAction.NotAShot));
            items.Add(new ReviewItem(key, ReviewKind.Contested, shot.Id, shot.Bull, shot.Image, sentence, choices, shot.BullChosen || Dismissed(key)));
        }

        foreach (var shot in shots.Where(s => s.Oversize is not null && !OnlySighters(s.Bull)))
        {
            string key = $"oversized:{shot.Id}";
            var choices = new List<ReviewChoice> { new("One shot", ReviewAction.Keep) };

            // The item the flag exists to raise is a mark that really is two shots, and until entry 94 section 4 the only way to take it as
            // two was a tap on the image, so the keyboard loop broke on exactly that item. The detector says where the two halves sit.
            if (shot.Oversize is { SplitA: not null, SplitB: not null })
            {
                choices.Add(new ReviewChoice("Two shots", ReviewAction.SplitIntoTwo, shot.Bull));
            }

            choices.Add(new ReviewChoice("Not a shot", ReviewAction.NotAShot));
            items.Add(new ReviewItem(key, ReviewKind.Oversized, shot.Id, shot.Bull, shot.Image, shot.Oversize!.Describe(labels[shot.Id]), choices, Dismissed(key)));
        }

        // Entry 113 section 4: a bull the marking says holds more than one, or a sheet read by nearest bull, is not doubled by holding them.
        foreach (var group in shots.Where(s => s.Bull is { } b && bulls.TryGetValue(b, out var bull) && bull.Scoring).GroupBy(s => s.Bull!.Value)
            .Where(g => g.Count() > (state.Rule is { NearestOnly: true } ? int.MaxValue : state.Rule?.For(g.Key) ?? 1)))
        {
            string key = $"doubled:{group.Key}:{string.Join(',', group.Select(s => s.Id).Order())}";
            items.Add(new ReviewItem(key, ReviewKind.Doubled, group.First().Id, group.Key, group.First().Image,
                $"Bull {BullName(group.Key)} holds {group.Count()} shots, {string.Join(" and ", group.Select(s => labels[s.Id]))}. The sheet expects one a bull: reassign one, or keep them if more rounds were fired than bulls.",
                [new ReviewChoice("Keep them", ReviewAction.Keep)], group.All(s => s.BullChosen) || Dismissed(key)));
        }

        foreach (var shot in shots.Where(s => s.Bull is null && state.Bulls.Count > 0))
        {
            string key = $"unassigned:{shot.Id}";
            var choices = new List<ReviewChoice>();
            if (Nearest(state, shot.Image) is { } nearest)
            {
                choices.Add(new ReviewChoice($"Bull {BullName(nearest)}, the nearest", ReviewAction.AssignBull, nearest));
            }

            choices.Add(new ReviewChoice("Not a shot", ReviewAction.NotAShot));
            choices.Add(new ReviewChoice("Leave it without a bull", ReviewAction.Keep));
            items.Add(new ReviewItem(key, ReviewKind.Unassigned, shot.Id, null, shot.Image, $"Shot {labels[shot.Id]} has no bull, so it is left out of the group.", choices, Dismissed(key)));
        }

        // Refused candidates in scoring bulls that hold no shot: a hole the detector may have missed.
        if (state.Assignment is { } review && state.Scale is { } scale)
        {
            double pitch = Pitch(state, scale);
            var occupied = shots.Where(s => s.Bull is not null).Select(s => s.Bull!.Value).ToHashSet();
            foreach (var candidate in review.Rejected.Where(r => r.DiameterInches >= RefusedCandidateInches && r.Reason.StartsWith("too small", StringComparison.Ordinal)))
            {
                var at = scale.ToTarget(candidate.Image);
                var bull = state.Bulls.Where(b => b.Scoring).MinBy(b => Distance(scale.ToTarget(b.Image), at));
                if (bull is null || occupied.Contains(bull.Index) || Distance(scale.ToTarget(bull.Image), at) > pitch / 2)
                {
                    continue;
                }

                string key = string.Create(inv, $"refused:{candidate.Image.X:0}:{candidate.Image.Y:0}");
                items.Add(new ReviewItem(key, ReviewKind.Refused, null, bull.Index, candidate.Image,
                    string.Create(inv, $"Bull {bull.Label} has no shot. A {candidate.DiameterInches:0.00} in candidate there was refused: {candidate.Reason}."),
                    [new ReviewChoice($"Add a shot on bull {bull.Label}", ReviewAction.AddShot, bull.Index), new ReviewChoice("Leave it out", ReviewAction.Keep)],
                    Dismissed(key) || state.Shots.Any(s => s.Provenance == ShotProvenance.Manual && Distance(scale.ToTarget(s.Image), at) < 0.15)));
            }
        }

        if (Count(state, labels) is { } count)
        {
            items.Insert(0, count);
        }

        return items;
    }

    /// <summary>
    /// The count item, NOTES-FROM-PLANNING.md entry 95 section 2: the person said how many rounds they fired and the marks disagree. Too few,
    /// and the marks most likely to be two are ranked by how close each sits to the size of two holes, largest first, because a pair
    /// overlapping by two thirds reads about 1.37 holes and a torn single hole reads the same, so the image cannot settle it and the count can.
    /// Too many, and the marks least like a hole are ranked smallest first. Its choice acts on the first candidate: take it as two shots, or
    /// mark it not a shot. It goes when the count agrees, and "Leave the count" stops it asking.
    /// </summary>
    private static ReviewItem? Count(MarkingState state, IReadOnlyDictionary<int, string> labels)
    {
        if (state.ExpectedShots is not { } expected)
        {
            return null;
        }

        var sighters = state.Bulls.Where(b => !b.Scoring).Select(b => b.Index).ToHashSet();
        var shots = state.Shots.Where(s => s.IsShot && !(s.Bull is { } b && sighters.Contains(b))).ToList();
        int found = shots.Count;
        if (found == expected)
        {
            return null;
        }

        var inv = CultureInfo.InvariantCulture;
        bool tooFew = found < expected;
        var ranked = (tooFew
                ? shots.Where(s => s.Size is not null).OrderByDescending(s => s.Size!.Holes)
                : shots.Where(s => s.Size is not null).OrderBy(s => s.Size!.Holes))
            .Take(CountCandidates)
            .ToList();
        string list = ranked.Count == 0
            ? " No mark carries a measured size, so there is nothing to rank: look at the sheet."
            : (tooFew ? " Most likely to be two, closest to two holes' size first: " : " Least like a hole, smallest first: ")
              + string.Join(", ", ranked.Select(s => string.Create(inv, $"shot {labels[s.Id]} at {s.Size!.Holes:0.00} holes"))) + ".";
        string sentence = string.Create(inv, $"You fired {expected} and {found} {(found == 1 ? "is" : "are")} marked.") + list;

        var first = ranked.FirstOrDefault();
        var choices = new List<ReviewChoice>();
        if (first is not null && tooFew && first.Size is { SplitA: not null, SplitB: not null })
        {
            choices.Add(new ReviewChoice($"Shot {labels[first.Id]} is two shots", ReviewAction.SplitIntoTwo, first.Bull));
        }
        else if (first is not null && !tooFew)
        {
            choices.Add(new ReviewChoice($"Shot {labels[first.Id]} is not a shot", ReviewAction.NotAShot));
        }

        choices.Add(new ReviewChoice("Leave the count", ReviewAction.Keep));
        string key = string.Create(inv, $"count:{expected}:{found}");
        return new ReviewItem(key, ReviewKind.Count, first?.Id, first?.Bull, first?.Image ?? default, sentence, choices, state.Dismissed?.Contains(key) == true);
    }

    /// <summary>How many items still want a decision.</summary>
    public static int Open(IReadOnlyList<ReviewItem> items) => items?.Count(i => !i.Resolved) ?? 0;

    /// <summary>Carries out a choice on the session, as one undoable step, and returns the shot it concerned.</summary>
    public static int? Apply(MarkingSession session, ReviewItem item, ReviewChoice choice)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(choice);
        switch (choice.Action)
        {
            case ReviewAction.AssignBull when item.ShotId is { } id:
                session.AssignBull(id, choice.Bull);
                return id;
            case ReviewAction.NotAShot when item.ShotId is { } id:
                session.SetNotAShot(id, true);
                return id;
            case ReviewAction.AddShot:
                return session.AddShot(item.Image, choice.Bull);
            case ReviewAction.SplitIntoTwo when item.ShotId is { } id && session.State.Find(id)?.Oversize is { SplitA: { } a, SplitB: { } b }:
                session.SplitShot(id, a, b);
                return id;
            case ReviewAction.SplitIntoTwo when item.ShotId is { } id && session.State.Find(id)?.Size is { SplitA: { } a, SplitB: { } b }:
                session.SplitShot(id, a, b);
                return id;
            default:
                session.Dismiss(item.Key);
                return item.ShotId;
        }
    }

    private static int? Nearest(MarkingState state, PointD image)
    {
        PointD Plane(PointD p) => state.Scale is { } scale ? scale.ToTarget(p) : p;
        return state.Bulls.MinBy(b => Distance(Plane(b.Image), Plane(image)))?.Index;
    }

    private static double Pitch(MarkingState state, ScaleReference scale)
    {
        var centres = state.Bulls.Where(b => b.Scoring).Select(b => scale.ToTarget(b.Image)).ToList();
        var gaps = centres.SelectMany((a, i) => centres.Skip(i + 1).Select(b => Distance(a, b))).Where(d => d > 0.1).ToList();
        return gaps.Count == 0 ? 1.5 : gaps.Min();
    }

    private static double Distance(PointD a, PointD b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));
}

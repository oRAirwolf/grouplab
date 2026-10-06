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

    /// <summary>
    /// A shot placed on the hole-sized part of a mark twice a hole across or more, a hole read together with what lies beside it
    /// (NOTES-FROM-PLANNING.md entry 291 section 7 item 4).
    /// </summary>
    Joined,

    /// <summary>
    /// A hole Find holes proposed on a target GroupLab did not print and was not sure of (NOTES-FROM-PLANNING.md entry 318 section 2).
    /// </summary>
    Proposed,

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

/// <summary>
/// A shot whose mark was flagged for its size and not yet settled, as the result shows it (NOTES-FROM-PLANNING.md entry 318 section 1): where it
/// is, the sentence, whether it was judged from too few marks, and whether it is a hole placed inside a larger mark.
/// </summary>
public sealed record SizeFlag(int ShotId, PointD Image, string Sentence, bool Tentative, bool Joined);

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
/// The review queue: every item the marking wants a person to look at, contested assignments first, then oversized marks, shots placed
/// on the hole-sized part of a larger mark, doubled bulls, shots with no bull, and refused candidates in empty scoring bulls, each in the sheet's order. An item is resolved once a person has
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

    /// <summary>The fewest flagged marks before "most of them" can mean anything: three is a pattern, one is a mark.</summary>
    public const int MostOfThem = 3;

    /// <summary>The share of a sheet's marks that being flagged makes the calibre the likelier explanation than the holes.</summary>
    public const double MostOfThemShare = 0.6;

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
        var labels = ShotLabels.For(state).ToDictionary(l => l.ShotId, l => l.Name ?? l.Text ?? "");
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
                ? string.Create(inv, $"bull {BullName(detail.NearestBull)} already holds another shot, at {od.DistanceInches:0.000} in")
                : string.Create(inv, $"bull {BullName(detail.NearestBull)} holds no other shot");
            string sentence = moved
                ? string.Create(inv, $"{ShotLabels.Start(labels[shot.Id])} was on bull {BullName(detail.DetectedBull)} and an edit moved it to bull {BullName(detail.Bull)}, {detail.DistanceInches:0.000} in away. Its nearest bull is {BullName(detail.NearestBull)}, at {detail.NearestInches:0.000} in.")
                : overridden
                    ? string.Create(inv, $"This hole is {detail.NearestInches:0.000} in from bull {BullName(detail.NearestBull)} and {detail.DistanceInches:0.000} in from bull {BullName(detail.Bull)}. Nearest bull says {BullName(detail.NearestBull)}, but {holder}. One-to-one matching gives it to bull {BullName(detail.Bull)}.")
                    : detail.MarginInches < ContestedMarginInches
                        ? string.Create(inv, $"{ShotLabels.Start(labels[shot.Id])} is {detail.NearestInches:0.000} in from bull {BullName(detail.NearestBull)}, and its next bull is only {detail.MarginInches:0.000} in further. A small registration error would change which bull it reads as.")
                        : string.Create(inv, $"{ShotLabels.Start(labels[shot.Id])} is on bull {BullName(detail.Bull)}, its nearest, at {detail.NearestInches:0.000} in. Its bulls hold more shots than bulls, so no one-to-one matching was forced and each shot there was left on its nearest bull: check it is the one it was fired at.");
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

        // NOTES-FROM-PLANNING.md entry 140 section 3.2: if most of the holes on a sheet would be flagged as possibly two, the assumption is
        // wrong, not the holes. Alan opened a 6.5 mm sheet after a smaller one, the calibre followed him across, and all fifteen holes were
        // flagged at 2.0 to 2.36 holes' area: sixteen items on a sheet with nothing wrong with it. A queue that cries wolf teaches people to
        // ignore it, so this raises one question about the calibre instead of one item a shot.
        // A joined mark is placed on one hole and says nothing about the caliber, so it is neither counted here nor raised as possibly two.
        var flagged = shots.Where(s => s.Oversize is { Joined: false } && !OnlySighters(s.Bull)).ToList();
        var judged = shots.Where(s => !OnlySighters(s.Bull)).ToList();
        if (flagged.Count >= MostOfThem && judged.Count > 0 && flagged.Count >= judged.Count * MostOfThemShare)
        {
            const string key = "oversized:all";
            items.Add(new ReviewItem(
                key,
                ReviewKind.Oversized,
                flagged[0].Id,
                flagged[0].Bull,
                flagged[0].Image,
                string.Create(CultureInfo.InvariantCulture,
                    $"{flagged.Count} of the {judged.Count} marks on this sheet read as more than one hole, which usually means the caliber is wrong rather than that you fired twice at every bull. Check what you were shooting."),
                [new ReviewChoice("One shot each", ReviewAction.Keep)],
                Dismissed(key)));
            flagged = [];
        }

        foreach (var shot in flagged)
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
            items.Add(new ReviewItem(key, ReviewKind.Oversized, shot.Id, shot.Bull, shot.Image, shot.Oversize!.Describe(ShotLabels.Start(labels[shot.Id])), choices, Dismissed(key)));
        }

        // Entry 291 section 7 item 4: a shot placed on the hole-sized part of a larger mark is counted where it was placed, and shown so a
        // person can check it sits on the hole.
        foreach (var shot in shots.Where(s => s.Oversize is { Joined: true } && !OnlySighters(s.Bull)))
        {
            string key = JoinedKey(shot.Id);
            items.Add(new ReviewItem(key, ReviewKind.Joined, shot.Id, shot.Bull, shot.Image, shot.Oversize!.Describe(ShotLabels.Start(labels[shot.Id]), state.Calibre?.DiameterInches),
                [new ReviewChoice("It is on the hole", ReviewAction.Keep), new ReviewChoice("Not a shot", ReviewAction.NotAShot)], Dismissed(key)));
        }

        // Entry 318 section 2: a hole Find holes proposed and was not sure of, until the person says it is a hole, moves it or takes it away.
        foreach (var shot in shots.Where(s => s.Proposal is { Doubt: not null } && s.Provenance == ShotProvenance.Automatic && !OnlySighters(s.Bull)))
        {
            string key = ProposedKey(shot.Id);
            items.Add(new ReviewItem(key, ReviewKind.Proposed, shot.Id, shot.Bull, shot.Image, Doubt(labels[shot.Id], shot.Proposal!),
                [new ReviewChoice("It is a hole", ReviewAction.Keep), new ReviewChoice("Not a shot", ReviewAction.NotAShot)], Dismissed(key)));
        }

        // Entry 113 section 4: a bull the marking says holds more than one, or a sheet read by nearest bull, is not doubled by holding them.
        foreach (var group in shots.Where(s => s.Bull is { } b && bulls.TryGetValue(b, out var bull) && bull.Scoring).GroupBy(s => s.Bull!.Value)
            .Where(g => g.Count() > (state.Rule is { NearestOnly: true } || (state.Rule is null && OneBull(state)) ? int.MaxValue : state.Rule?.For(g.Key) ?? 1)))
        {
            string key = $"doubled:{group.Key}:{string.Join(',', group.Select(s => s.Id).Order())}";
            items.Add(new ReviewItem(key, ReviewKind.Doubled, group.First().Id, group.Key, group.First().Image,
                $"Bull {BullName(group.Key)} holds {group.Count()} shots, {string.Join(" and ", group.Select(s => ShotLabels.InSentence(labels[s.Id])))}. The sheet expects one a bull: reassign one, or keep them if more rounds were fired than bulls.",
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
            items.Add(new ReviewItem(key, ReviewKind.Unassigned, shot.Id, null, shot.Image, $"{ShotLabels.Start(labels[shot.Id])} has no bull, so it is left out of the group.", choices, Dismissed(key)));
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
        if (Expected(state) is not { } expected)
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
        // NOTES-FROM-PLANNING.md entry 354 section 1.6: with more marks than rounds, the marks to look at first are the ones off every bull and
        // the ones on or beside the sheet's own printing, a marker, a code or its words, and only then the smallest. the submitted sheet led with the
        // smallest mark and left a marker's edge and a bull's number further down.
        var ranked = (tooFew
                ? shots.Where(s => s.Size is not null).OrderByDescending(s => s.Size!.Holes)
                : shots.Where(s => s.Size is not null).OrderBy(s => s.Bull is null ? 0 : s.Size!.Beside is not null ? 1 : 2).ThenBy(s => s.Size!.Holes))
            .Take(CountCandidates)
            .ToList();
        bool printFirst = !tooFew && ranked.Any(s => s.Bull is null || s.Size!.Beside is not null);
        string Why(MarkedShot s) => tooFew ? "" : s.Bull is null ? ", off every bull" : s.Size!.Beside is { } beside ? $", on or beside {beside}" : "";
        string list = ranked.Count == 0
            ? " No mark carries a measured size, so there is nothing to rank: look at the sheet."
            : (tooFew ? " Most likely to be two, closest to two holes' size first: "
                : printFirst ? " Least like a hole: those off the bulls or on the sheet's own printing first, then the smallest: "
                : " Least like a hole, smallest first: ")
              + string.Join(", ", ranked.Select(s => string.Create(inv, $"{ShotLabels.InSentence(labels[s.Id])} at {s.Size!.Holes:0.00} holes{Why(s)}"))) + ".";
        // NOTES-FROM-PLANNING.md entry 140: "You fired 25" was said to Alan on a sheet where he had typed nothing, because the number came
        // from the sheet's own twenty five bulls. He read it as the last sheet's count following him across, and it is worth seeing why that
        // reading was reasonable: the sentence claimed he had said something he had not. A number the sheet worked out says so, and says what
        // would settle it.
        string marked = string.Create(inv, $"{found} {(found == 1 ? "is" : "are")} marked");
        string sentence = (state.ExpectedShots is not null
                ? string.Create(inv, $"You fired {expected} and {marked}.")
                : string.Create(inv, $"This sheet takes {expected} shots and {marked}. Nobody has said how many rounds were fired: type it into Rounds fired at the group and this settles itself."))
            + (tooFew ? Empty(state, labels) : "") + list;

        var first = ranked.FirstOrDefault();
        var choices = new List<ReviewChoice>();
        if (first is not null && tooFew && first.Size is { SplitA: not null, SplitB: not null })
        {
            choices.Add(new ReviewChoice($"{ShotLabels.Start(labels[first.Id])} is two shots", ReviewAction.SplitIntoTwo, first.Bull));
        }
        else if (first is not null && !tooFew)
        {
            choices.Add(new ReviewChoice($"{ShotLabels.Start(labels[first.Id])} is not a shot", ReviewAction.NotAShot));
        }

        choices.Add(new ReviewChoice("Leave the count", ReviewAction.Keep));
        string key = string.Create(inv, $"count:{expected}:{found}");
        return new ReviewItem(key, ReviewKind.Count, first?.Id, first?.Bull, first?.Image ?? default, sentence, choices, state.Dismissed?.Contains(key) == true);
    }

    /// <summary>
    /// How many shots the sheet says were fired: the number the person typed, or, failing that, what the shots-per-bull rule and the
    /// scoring bulls already say between them.
    /// <para>
    /// NOTES-FROM-PLANNING.md entry 130 section 2b.2, and it is the fix for a real harm. On scan 1 of the second range day a shooter fired
    /// fifteen, GroupLab found fourteen, and <b>said nothing at all</b>: the review queue was empty because nobody had typed a count, even
    /// though the sheet was being analysed one shot to a bull across fifteen scoring bulls, which is a count. A shortfall that passes as a
    /// clean result is the worst way to be wrong, because the shooter has no reason to look.
    /// </para>
    /// </summary>
    internal static int? Expected(MarkingState state)
    {
        if (state.ExpectedShots is { } typed)
        {
            return typed;
        }

        // Nearest-bull means the person has said they are not counting, so there is nothing to hold the marks against.
        if (state.Rule is { NearestOnly: true })
        {
            return null;
        }

        var scoring = state.Bulls.Where(b => b.Scoring).ToList();
        if (scoring.Count == 0)
        {
            return null;
        }

        // Entry 196: one scoring bull takes a group, so the sheet itself says nothing about how many; only the person's count does.
        if (state.Rule is null && scoring.Count == 1)
        {
            return null;
        }

        return state.Rule is { } rule ? scoring.Sum(b => rule.For(b.Index)) : scoring.Count;
    }

    /// <summary>A sheet with one scoring bull, which is shot as a group at it (entry 196).</summary>
    private static bool OneBull(MarkingState state) => state.Bulls.Count(b => b.Scoring) == 1;

    /// <summary>The scoring bulls with nothing on them, named, because that is where a missing shot is.</summary>
    private static string Empty(MarkingState state, IReadOnlyDictionary<int, string> labels)
    {
        var taken = state.Shots.Where(s => s.IsShot && s.Bull is not null).Select(s => s.Bull!.Value).ToHashSet();
        var empty = state.Bulls.Where(b => b.Scoring && !taken.Contains(b.Index)).Select(b => b.Label).ToList();
        if (empty.Count == 0)
        {
            return " Every bull has a shot on it, so a mark may be two.";
        }

        string which = empty.Count <= 8
            ? string.Join(", ", empty)
            : string.Join(", ", empty.Take(8)) + " and " + (empty.Count - 8).ToString(CultureInfo.InvariantCulture) + " more";
        return empty.Count == 1
            ? $" Nothing is marked on bull {which}."
            : $" Nothing is marked on bulls {which}.";
    }

    /// <summary>How many items still want a decision.</summary>
    public static int Open(IReadOnlyList<ReviewItem> items) => items?.Count(i => !i.Resolved) ?? 0;

    /// <summary>
    /// The header's count, "2 of 26 need review": how many of the shots have something open about them, never more than there are shots.
    /// NOTES-FROM-PLANNING.md entry 354 section 1.5: it counted open items over shots, and a shot can carry several items, so the submitted sheet
    /// read "49 of 29 need review". An open question about the sheet as a whole, the count of rounds or a bull with no shot, is not a shot,
    /// so it is counted after the shots: "2 of 29 and 1 more need review".
    /// </summary>
    public static string CountWords(IReadOnlyList<ReviewItem> items, int shots)
    {
        var open = (items ?? []).Where(i => !i.Resolved).ToList();
        int needing = open.Where(i => i.Kind != ReviewKind.Count && i.ShotId is not null).Select(i => i.ShotId!.Value).Distinct().Count();
        int sheet = open.Count(i => i.Kind == ReviewKind.Count || i.ShotId is null);
        string counted = string.Create(CultureInfo.InvariantCulture, $"{Math.Min(needing, Math.Max(shots, 0))} of {Math.Max(shots, 0)}");
        return sheet == 0 ? counted + " need review" : counted + string.Create(CultureInfo.InvariantCulture, $" and {sheet} more need review");
    }

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

    /// <summary>The key of the review item a shot placed inside a larger mark raises, entry 291 section 7 item 4.</summary>
    public static string JoinedKey(int shotId) => string.Create(CultureInfo.InvariantCulture, $"joined:{shotId}");

    /// <summary>The key of the review item a hole Find holes proposed and was not sure of raises, entry 318 section 2.</summary>
    public static string ProposedKey(int shotId) => string.Create(CultureInfo.InvariantCulture, $"proposed:{shotId}");

    /// <summary>The sentence for a proposed hole the finder was not sure of: that it was proposed, that the finder is experimental, and why.</summary>
    public static string Doubt(string shot, HoleProposal proposal)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        return $"Find holes, which is experimental, proposed {ShotLabels.InSentence(shot)} and is not sure of it: {proposal.Doubt}. Check that it is a hole.";
    }

    /// <summary>
    /// Whether a proposed hole still wants checking, entry 318 section 2: it stays until the person says it is a hole, takes it away, or moves
    /// it, which makes it theirs.
    /// </summary>
    public static bool StillDoubted(MarkingState state, MarkedShot shot)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(shot);
        return shot.IsShot && shot.Provenance == ShotProvenance.Automatic && shot.Proposal is { Doubt: not null }
            && state.Dismissed?.Contains(ProposedKey(shot.Id)) != true;
    }

    /// <summary>
    /// Every mark the result asks the person to check, in the sheet's order: the size flags of <see cref="SizeFlags"/>, then the holes Find
    /// holes proposed and was not sure of (entry 318 section 2), each with its sentence. The phone rings each in amber and lists the sentences.
    /// </summary>
    public static IReadOnlyList<SizeFlag> MarksToCheck(MarkingState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var named = ShotLabels.For(state);
        var labels = named.ToDictionary(l => l.ShotId, l => l.Name ?? l.Text ?? "");
        // Entry 376 item B4: in bull order, lowest bull first, the order the shots are named in.
        var place = named.Select((l, k) => (l.ShotId, k)).ToDictionary(x => x.ShotId, x => x.k);
        return [.. SizeFlags(state).Concat(state.Shots.Where(s => StillDoubted(state, s)).Select(s => new SizeFlag(s.Id, s.Image,
            Doubt(labels.TryGetValue(s.Id, out var label) ? label : s.Id.ToString(CultureInfo.InvariantCulture), s.Proposal!), true, false)))
            .OrderBy(f => place.GetValueOrDefault(f.ShotId, int.MaxValue))];
    }

    /// <summary>
    /// Whether a shot's size flag still stands, NOTES-FROM-PLANNING.md entry 318 section 1: a mark much bigger than the bullet stays flagged on
    /// the result until the person settles it, by saying the shot is on the hole (or one shot), or by moving it, which clears the flag with
    /// the measurement it described. A shot marked as not a shot carries no flag.
    /// </summary>
    public static bool StillFlagged(MarkingState state, MarkedShot shot)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(shot);
        bool Settled(string key) => state.Dismissed?.Contains(key) == true;
        return shot.IsShot && shot.Oversize is { } flag
            && !(flag.Joined ? Settled(JoinedKey(shot.Id)) : Settled($"oversized:{shot.Id}") || Settled("oversized:all"));
    }

    /// <summary>
    /// What the result shows of the size flags, entry 318 section 1: every shot whose flag still stands, with the sentence the review queue
    /// says about it, in the sheet's order. The phone marks each on the picture and lists the sentences; the desktop draws the same shots'
    /// rings and panel from it.
    /// </summary>
    public static IReadOnlyList<SizeFlag> SizeFlags(MarkingState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var labels = ShotLabels.For(state).ToDictionary(l => l.ShotId, l => l.Name ?? l.Text ?? "");
        return [.. state.Shots.Where(s => StillFlagged(state, s)).Select(s => new SizeFlag(s.Id, s.Image,
            s.Oversize!.Describe(labels.TryGetValue(s.Id, out var label) ? ShotLabels.Start(label) : s.Id.ToString(CultureInfo.InvariantCulture), state.Calibre?.DiameterInches),
            s.Oversize.Tentative, s.Oversize.Joined))];
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

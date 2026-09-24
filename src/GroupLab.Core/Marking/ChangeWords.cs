namespace GroupLab.Core.Marking;

/// <summary>
/// What one step of the marking did, in a few words, for the undo and redo buttons' tooltips: NOTES-FROM-PLANNING.md entry 166 section 2,
/// where the first macOS tester was not sure what undo would undo. It is read from the two markings either side of the step, so no
/// operation has to remember to describe itself and none can describe itself wrongly.
/// </summary>
public static class ChangeWords
{
    public static string Describe(MarkingState before, MarkingState after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        var was = before.Shots.ToDictionary(s => s.Id);
        var now = after.Shots.ToDictionary(s => s.Id);
        var added = now.Keys.Where(id => !was.ContainsKey(id)).ToList();
        var removed = was.Keys.Where(id => !now.ContainsKey(id)).ToList();

        // Adding or deleting a shot can re-match its neighbours to their bulls, so a count of changed shots would overstate the step.
        if (added.Count > 0 || removed.Count > 0)
        {
            return (added.Count, removed.Count) switch
            {
                (1, 0) => "add " + Shot(after, added[0]),
                (0, 1) => "delete " + Shot(before, removed[0]),
                (> 1, 0) => $"add {added.Count} shots",
                (0, > 1) => $"delete {removed.Count} shots",
                _ => "change the shots",
            };
        }

        var changed = now.Values.Where(s => was[s.Id] != s).Select(s => (Was: was[s.Id], Now: s)).ToList();
        var moved = changed.Where(c => c.Was.Image != c.Now.Image).ToList();
        if (moved.Count == 1)
        {
            return "move " + Shot(before, moved[0].Was.Id);
        }

        if (moved.Count > 1)
        {
            return $"move {moved.Count} shots";
        }

        if (changed.Count == 1)
        {
            var (a, b) = changed[0];
            string shot = Shot(before, a.Id);
            return a switch
            {
                _ when a.NotAShot != b.NotAShot => b.NotAShot ? $"mark {shot} as not a shot" : $"count {shot} as a shot",
                _ when a.Exclusion != b.Exclusion => b.Exclusion is null ? $"put {shot} back in the group" : $"leave {shot} out of the group",
                _ when a.Flyer != b.Flyer => b.Flyer ? $"call {shot} a flyer" : $"stop calling {shot} a flyer",
                _ when a.Bull != b.Bull => after.Bulls.FirstOrDefault(x => x.Index == b.Bull) is { } bull ? $"put {shot} on bull {bull.Label}" : $"take {shot} off its bull",
                _ => "change " + shot,
            };
        }

        if (changed.Count > 1)
        {
            return $"change {changed.Count} shots";
        }

        return true switch
        {
            _ when before.Scale != after.Scale => after.Scale is null ? "remove the scale" : "set the scale",
            _ when before.PointOfAim != after.PointOfAim => "move the point of aim",
            _ when !before.Bulls.SequenceEqual(after.Bulls) => "change the bulls",
            _ when before.ViewQuarterTurns != after.ViewQuarterTurns => "turn the view",
            _ when before.Calibre != after.Calibre => "set the caliber",
            _ when before.ShotDistanceInches != after.ShotDistanceInches => "set the distance",
            _ when before.ExpectedShots != after.ExpectedShots => "set the number of shots",
            _ when before.Subgroups != after.Subgroups => "change the strings",
            _ when before.Paper != after.Paper || before.Backing != after.Backing => "set the paper",
            _ when before.Rifle != after.Rifle || before.Barrel != after.Barrel || before.Load != after.Load => "set the rifle and load",
            _ when before.Dismissed != after.Dismissed => "set aside an item to check",
            _ when before.Assignment != after.Assignment || before.Rule != after.Rule => "change which bull each shot is on",
            _ => "the last change",
        };
    }

    /// <summary>A shot by the label it wears on the sheet, "shot 6" or "shot B2a", or "a shot" when it wears none.</summary>
    private static string Shot(MarkingState state, int id) => ShotLabels.For(state).FirstOrDefault(l => l.ShotId == id)?.Text switch
    {
        ShotLabels.NotAShot => "a mark that is not a shot",
        ShotLabels.Unassigned => "an unassigned shot",
        { Length: > 0 } text => "shot " + text,
        _ => "a shot",
    };
}

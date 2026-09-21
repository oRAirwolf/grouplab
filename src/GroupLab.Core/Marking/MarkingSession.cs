using System.Collections.Immutable;
using System.Globalization;
using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;

namespace GroupLab.Core.Marking;

/// <summary>Where a shot's position came from, DESIGN.md section 13: provenance keeps the statistics honest about their source.</summary>
public enum ShotProvenance
{
    /// <summary>Placed by detection and not touched.</summary>
    Automatic,

    /// <summary>Placed by detection and then moved or reassigned by the user.</summary>
    Corrected,

    /// <summary>Placed by hand.</summary>
    Manual,
}

/// <summary>
/// The short list of reasons a shot may be excluded, docs/STATISTICS.md section 10: exclusion is permitted for the legitimate
/// cases, requires a reason, and the reason is recorded.
/// </summary>
public enum ExclusionReason
{
    /// <summary>The shooter called the shot as a flyer when it broke.</summary>
    CalledFlyer,

    /// <summary>A round the shooter knows was bad: a misfeed, a squib, a mismeasured charge.</summary>
    BadRound,

    /// <summary>A shot the shooter knows they pulled.</summary>
    PulledShot,
}

/// <summary>
/// An exclusion reason as a person reads it, NOTES-FROM-PLANNING.md entry 111 section 3: "Called flyer", never the enum's name "CalledFlyer".
/// The saved marking keeps the enum's name, which is a file format, not something a person reads.
/// </summary>
public static class ExclusionReasons
{
    /// <summary>The reason as it starts a line or fills a list: "Called flyer".</summary>
    public static string Words(this ExclusionReason reason) => reason switch
    {
        ExclusionReason.CalledFlyer => "Called flyer",
        ExclusionReason.BadRound => "Bad round",
        _ => "Pulled shot",
    };

    /// <summary>The reason inside a sentence: "excluded as called flyer".</summary>
    public static string InSentence(this ExclusionReason reason) => reason.Words().ToLowerInvariant();
}

/// <summary>
/// One impact on the image: its position in image pixels, where that came from, whether it is excluded and why, whether it has
/// been marked as not a shot at all (a detection that was handwriting, a staple, a tear), and the bull it is assigned to, if any.
/// <para>
/// <see cref="BullChosen"/> says whether a person chose that bull, NOTES-FROM-PLANNING.md entry 74 section 1. It is deliberately not
/// <see cref="Provenance"/>: placing a hole says "there is a hole here", choosing a bull says "it belongs there", and only the second
/// constrains the matching. A shot placed by hand whose bull the software picked has a person's position and the software's bull.
/// </para>
/// <para>
/// <see cref="MeasuredDiameterInches"/> is the diameter the detector measured for a detected shot, the number that shows a merged pair
/// or ink under a mark (entry 76 section 4). A shot placed by hand has none, and moving a shot clears it, because the measurement
/// described the point the detector chose and not the one the person chose instead. <see cref="Oversize"/> is the detector's flag on
/// the same measurement, entry 82 section 6, and is cleared with it.
/// </para>
/// </summary>
public sealed record MarkedShot(int Id, PointD Image, ShotProvenance Provenance, ExclusionReason? Exclusion = null, bool NotAShot = false, int? Bull = null, bool BullChosen = false, double? MeasuredDiameterInches = null, DetectedOversize? Oversize = null, MarkSize? Size = null)
{
    /// <summary>Counted in the group: not marked as not a shot. Excluded shots are counted in the full figures and left out of the reduced ones.</summary>
    public bool IsShot => !NotAShot;

    /// <summary>
    /// A shot the person called out as a flyer but kept, NOTES-FROM-PLANNING.md entry 131 section 2. It is a mark on the sheet and in the
    /// list and it changes no figure, which is the whole difference between it and <see cref="Exclusion"/>: pointing at a shot and dropping
    /// it from the group are two different acts, and a screen that ran them together would quietly shrink a group because somebody wanted a
    /// shot looked at.
    /// </summary>
    public bool Flyer { get; init; }

    /// <summary>
    /// A sighting shot on a marking whose sheet does not say so, entry 131 section 2. On a GroupLab sheet a sighter is a property of the
    /// bull, and this stays false there; on a plain group, where there are no bulls to carry it, this is the only place the fact can live.
    /// </summary>
    public bool Sighter { get; init; }

    /// <summary>
    /// The hole's diameter in inches as a person set it, entry 131 section 2's "adjust the diameter ring if the detected size is wrong". It
    /// is kept apart from <see cref="MeasuredDiameterInches"/> rather than overwriting it, so what the detector measured is still on record
    /// after somebody disagrees with it.
    /// </summary>
    public double? ChosenDiameterInches { get; init; }

    /// <summary>A note the person typed against this shot, entry 131 section 2, or null. Nothing reads it: it is theirs.</summary>
    public string? Note { get; init; }

    /// <summary>What the hole is taken to measure: what a person set, else what the detector measured, else nothing.</summary>
    public double? DiameterInches => ChosenDiameterInches ?? MeasuredDiameterInches;
}

/// <summary>
/// A bull a shot can be assigned to: its index and label in the target definition, its centre in image pixels, and, for a detected sheet,
/// its declared centre in page dmm. The image centre is where the bull was located, and offsets are measured from it; the declared centre
/// is the definition's exact geometry, and assignment classifies against it (NOTES-FROM-PLANNING.md entry 70 section 5).
/// </summary>
public sealed record BullAim(int Index, string Label, PointD Image, bool Scoring = true, PointD? Declared = null);

/// <summary>
/// Which bulls hold which load, NOTES-FROM-PLANNING.md entry 94 section 2: a name for each bull that carries one, kept in the session and
/// never in the target definition. It is what lets one sheet carry six charge weights and be compared load against load, which is the
/// analysis Jeff's thirty bulls are for (entry 89 section 3).
/// <para>
/// <b>Why here rather than in the format.</b> The comparison half was already built and had nothing to feed it: every test in
/// <see cref="GroupLab.Core.Statistics.GroupComparison"/> takes a group label per shot, and no layer of GLTD could supply one. A bull carries
/// only <c>scoring</c>, a two-way split; the data block holds one field set for the whole sheet; and <c>instance</c> is a flat map excluded
/// from the definition and its identifier. A session mapping needs no format change, so it works on today's thirty-bull sheets and cannot be
/// wrong in a way that outlives a definition identifier. When a specification arrives the format question reopens with this as the fallback
/// that already works.
/// </para>
/// </summary>
public sealed record SubgroupMap(ImmutableDictionary<int, string> ByBull)
{
    public static SubgroupMap Empty { get; } = new(ImmutableDictionary<int, string>.Empty);

    /// <summary>The subgroup a bull belongs to, or null where it belongs to none.</summary>
    public string? For(int bull) => ByBull.TryGetValue(bull, out string? name) ? name : null;

    /// <summary>The subgroup names in the order they first appear by bull index, so a report lists them the way the sheet is shot.</summary>
    public IReadOnlyList<string> Names => [.. ByBull.OrderBy(p => p.Key).Select(p => p.Value).Distinct(StringComparer.Ordinal)];
}

/// <summary>
/// How a sheet's shots are to be read against its bulls when it breaks one shot a bull on purpose, NOTES-FROM-PLANNING.md entry 113 section 4:
/// every shot to its nearest bull, never matched; or matched with the named bulls each taking the number of shots given. The doubles sheet,
/// two shots into each of bulls 1 to 10 and none into 11 to 25, is read either way; without either, one-to-one matching pushes each second
/// shot onto an empty neighbour, and the review queue raises every one of them.
/// </summary>
public sealed record AssignmentRule(bool NearestOnly, ImmutableDictionary<int, int> PerBull)
{
    public static AssignmentRule Nearest { get; } = new(true, ImmutableDictionary<int, int>.Empty);

    /// <summary>The shots a bull is expected to hold under this rule: its named number, or one.</summary>
    public int For(int bull) => PerBull.TryGetValue(bull, out int shots) ? shots : 1;
}

/// <summary>
/// What a marking's detection was run with, NOTES-FROM-PLANNING.md entry 80 section 5: the calibre named, or none, and the size its holes were
/// taken to measure. The same image detects differently with a calibre and without, so two markings are comparable only when these agree.
/// </summary>
public sealed record DetectionRecord(Calibre? Calibre, double? HoleSizeInches)
{
    public string Describe() => Calibre is { } calibre
        ? string.Create(System.Globalization.CultureInfo.InvariantCulture, $"detected with the calibre {calibre.Name}, whose holes were taken to measure {HoleSizeInches:0.000} in")
        : "detected without a calibre, so whether a mark was one hole or two was judged by its shape alone";
}

/// <summary>
/// Everything a marking holds at one moment. It is immutable, so undo is keeping the previous one and every screen reads a
/// state that cannot change under it.
/// <para>
/// Every position is in the stored pixel frame, <see cref="ViewRotation.StoredPixelFrame"/>. <see cref="ViewQuarterTurns"/> is how the
/// screen turns the image for display, clockwise, and is part of the state only so that undo covers it and a saved marking reopens
/// the way it was left (NOTES-FROM-PLANNING.md entry 26); it never changes a position. <see cref="ExifOrientation"/> is the image's
/// tag as read, or null when it has none, as a flatbed scan does not.
/// </para>
/// </summary>
public sealed record MarkingState(
    string? ImagePath,
    ScaleReference? Scale,
    PointD? PointOfAim,
    ImmutableList<BullAim> Bulls,
    ImmutableList<MarkedShot> Shots,
    int NextId,
    string? RegistrationSummary = null,
    int ViewQuarterTurns = 0,
    int? ExifOrientation = null,
    Calibre? Calibre = null,
    double? ShotDistanceInches = null,
    AssignmentReview? Assignment = null,
    DetectionRecord? Detection = null,
    ImmutableHashSet<string>? Dismissed = null,
    SubgroupMap? Subgroups = null,
    int? ExpectedShots = null,
    Rifle? Rifle = null,
    string? Barrel = null,
    string? Load = null,
    AssignmentRule? Rule = null)
{
    public static MarkingState Empty { get; } = new(null, null, null, [], [], 1);

    public MarkedShot? Find(int id) => Shots.FirstOrDefault(s => s.Id == id);
}

/// <summary>
/// The marking screen's model, NOTES-FROM-PLANNING.md entry 21 section 3: one screen that is both the manual path, where a user
/// sets a scale, marks the point of aim and taps each impact on any photograph, and the correction interface DESIGN.md section 13
/// requires for the automatic path, where detection pre-fills the shots and the user accepts, moves, reassigns or deletes them.
/// <para>
/// Every change is a new <see cref="MarkingState"/>, pushed onto the undo stack, so undo and redo work throughout, as section 13
/// requires. Nothing here draws or reads the mouse: the screen translates taps into these operations, which is also what lets the
/// operations be tested without a screen and used from a touch interface later without change.
/// </para>
/// </summary>
public sealed class MarkingSession
{
    private readonly Stack<MarkingState> undo = new();
    private readonly Stack<MarkingState> redo = new();

    public MarkingSession(MarkingState? initial = null)
    {
        State = initial ?? MarkingState.Empty;
    }

    public MarkingState State { get; private set; }

    public bool CanUndo => undo.Count > 0;

    public bool CanRedo => redo.Count > 0;

    /// <summary>Raised after every change, undo and redo included.</summary>
    public event EventHandler? Changed;

    private void Apply(MarkingState next)
    {
        if (next == State)
        {
            return;
        }

        undo.Push(State);
        redo.Clear();
        State = next;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Undo()
    {
        if (undo.Count == 0)
        {
            return;
        }

        redo.Push(State);
        State = undo.Pop();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Redo()
    {
        if (redo.Count == 0)
        {
            return;
        }

        undo.Push(State);
        State = redo.Pop();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Starts a new marking on an image, forgetting the previous one and its history. The view starts turned as the image's EXIF
    /// Orientation tag asks, NOTES-FROM-PLANNING.md entry 24 section 4, so the common phone photograph needs no interaction; that is
    /// where the marking starts, not an undo step.
    /// </summary>
    public void Open(string imagePath, int? exifOrientation = null) =>
        Load(MarkingState.Empty with { ImagePath = imagePath, ExifOrientation = exifOrientation, ViewQuarterTurns = ViewRotation.FromExifOrientation(exifOrientation) });

    /// <summary>Replaces the marking with a whole state, a saved marking reopened, forgetting the history.</summary>
    public void Load(MarkingState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        undo.Clear();
        redo.Clear();
        State = state;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Turns the view by quarter turns, positive clockwise, on any image at any time, whether or not it had a tag and whether or not
    /// the tag was obeyed (entry 26). It is an undo step like every other action, and nothing but the view changes: no mark moves.
    /// </summary>
    public void Rotate(int quarterTurns) => Apply(State with { ViewQuarterTurns = ViewRotation.Normalise(State.ViewQuarterTurns + quarterTurns) });

    public void SetScale(ScaleReference? scale) => Apply(State with { Scale = scale });

    /// <summary>Sets the group's calibre, or clears it with null (NOTES-FROM-PLANNING.md entry 24 section 5).</summary>
    public void SetCalibre(Calibre? calibre) => Apply(State with { Calibre = calibre });

    /// <summary>
    /// Sets the distance the group was shot at, in inches whatever unit it was typed in, or clears it with null. Angular figures need it
    /// and are absent without it (docs/STATISTICS.md section 13, NOTES-FROM-PLANNING.md entry 25 section 1).
    /// </summary>
    public void SetShotDistance(double? inches) => Apply(State with { ShotDistanceInches = inches });

    public void SetPointOfAim(PointD? image) => Apply(State with { PointOfAim = image });

    /// <summary>
    /// Places a shot by hand, and returns its id. Where the marking has bulls, a shot given no bull is assigned to its nearest,
    /// NOTES-FROM-PLANNING.md entry 39 section 1: a person marking a GroupLab sheet by hand should not have to know that assignment is a
    /// separate step, and an unassigned shot on such a sheet is measured from the wrong place. That nearest bull is the software's choice,
    /// so the shot takes part in the matching like a detection (entry 74 section 1); a bull passed in is the caller's choice, and is kept.
    /// </summary>
    public int AddShot(PointD image, int? bull = null)
    {
        int id = State.NextId;
        Apply(Rematch(State with { Shots = State.Shots.Add(new MarkedShot(id, image, ShotProvenance.Manual, Bull: bull ?? NearestBull(State, image), BullChosen: bull is not null)), NextId = id + 1 }));
        return id;
    }

    /// <summary>
    /// Moves a shot; a detected shot the user moves becomes corrected. A shot on its nearest bull stays on its nearest bull wherever it is
    /// moved, and a shot the user assigned elsewhere, or unassigned, keeps that choice (entry 39 section 1).
    /// </summary>
    public void MoveShot(int id, PointD image) => Update(id, s => s with
    {
        Image = image,
        MeasuredDiameterInches = image == s.Image ? s.MeasuredDiameterInches : null,
        Oversize = image == s.Image ? s.Oversize : null,
        Size = image == s.Image ? s.Size : null,
        Bull = s.Bull == NearestBull(State, s.Image) ? NearestBull(State, image) : s.Bull,
        Provenance = Touched(s.Provenance),
    });

    /// <summary>The bull a shot at this image point is nearest to, <see cref="NearestBull(MarkingState, PointD)"/>.</summary>
    public int? NearestBull(PointD image) => NearestBull(State, image);

    /// <summary>
    /// The nearest-bull rule the detector's assignment falls back to, <see cref="ShotAssignment"/>, for a single shot: nearest on the
    /// target plane when a scale is set, which is where a sheet's bulls are evenly spaced, and in image pixels otherwise. Null when the
    /// marking has no bulls.
    /// </summary>
    private static int? NearestBull(MarkingState state, PointD image)
    {
        if (state.Bulls.Count == 0)
        {
            return null;
        }

        PointD Plane(PointD p) => state.Scale is { } scale ? scale.ToTarget(p) : p;
        var at = Plane(image);
        return state.Bulls.MinBy(b =>
        {
            var c = Plane(b.Image);
            return ((c.X - at.X) * (c.X - at.X)) + ((c.Y - at.Y) * (c.Y - at.Y));
        })!.Index;
    }

    public void DeleteShot(int id)
    {
        if (State.Find(id) is { } shot)
        {
            Apply(Rematch(State with { Shots = State.Shots.Remove(shot) }));
        }
    }

    /// <summary>Excludes a shot with its reason, or restores it with null.</summary>
    public void SetExclusion(int id, ExclusionReason? reason) => Update(id, s => s with { Exclusion = reason });

    /// <summary>Puts back a whole earlier state as one undoable step, as the editor's Discard edits does.</summary>
    public void Restore(MarkingState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Apply(state);
    }

    /// <summary>Records that a person looked at a review item and chose to leave the marking as it is, <see cref="ReviewQueue"/>.</summary>
    public void Dismiss(string reviewKey) => Apply(State with { Dismissed = (State.Dismissed ?? []).Add(reviewKey) });

    /// <summary>Marks a detection as not a shot, or restores it.</summary>
    public void SetNotAShot(int id, bool notAShot) => Update(id, s => s with { NotAShot = notAShot, Provenance = Touched(s.Provenance) });

    /// <summary>
    /// Calls a shot out as a flyer while keeping it in the group, NOTES-FROM-PLANNING.md entry 131 section 2. No figure changes: to leave a
    /// shot out of the figures is <see cref="SetExclusion"/>, and it needs a reason, which this deliberately does not.
    /// </summary>
    public void SetFlyer(int id, bool flyer) => Update(id, s => s with { Flyer = flyer });

    /// <summary>Marks a shot as a sighting shot, or unmarks it, on a marking whose sheet has no sighter bull to say it.</summary>
    public void SetSighter(int id, bool sighter) => Update(id, s => s with { Sighter = sighter });

    /// <summary>
    /// Sets the hole's diameter in inches by hand, or clears it with null and goes back to what the detector measured. It is not a
    /// correction of the position, so the shot's provenance does not change: the detector still placed it where it is.
    /// </summary>
    public void SetHoleDiameter(int id, double? inches) => Update(id, s => s with { ChosenDiameterInches = inches is > 0 ? inches : null });

    /// <summary>Puts a note on a shot, or clears it. Blank is null, so a note emptied leaves nothing behind in the file.</summary>
    public void SetNote(int id, string? note) => Update(id, s => s with { Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim() });

    /// <summary>
    /// Moves a shot by one press of an arrow key, <see cref="ShotEditor.Nudged"/>: dx and dy are −1, 0 or 1 on the image's axes, and
    /// <paramref name="coarse"/> is Shift held. It is <see cref="MoveShot"/> underneath, so a nudged detection becomes corrected and its
    /// measured diameter is cleared with the move like any other.
    /// </summary>
    public void NudgeShot(int id, int dx, int dy, bool coarse)
    {
        if (State.Find(id) is { } shot && (dx != 0 || dy != 0))
        {
            MoveShot(id, ShotEditor.Nudged(State, shot.Image, dx, dy, coarse));
        }
    }

    /// <summary>
    /// Assigns a shot to a bull, or to none, DESIGN.md section 13's click a hole then click a bull. It is the one place a person chooses a
    /// bull, so the choice is pinned against the matching (entry 74 section 1), and a detected shot reassigned becomes corrected.
    /// </summary>
    public void AssignBull(int id, int? bull) => Update(id, s => s with { Bull = bull, BullChosen = true, Provenance = Touched(s.Provenance) });

    /// <summary>
    /// Puts a bull in a subgroup, or takes it out of one when the name is null or blank (NOTES-FROM-PLANNING.md entry 94 section 2). Nothing
    /// about the sheet changes: this is the session saying which bulls hold which load.
    /// </summary>
    public void SetSubgroup(int bull, string? name)
    {
        var map = State.Subgroups ?? SubgroupMap.Empty;
        string? trimmed = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        var next = trimmed is null ? map.ByBull.Remove(bull) : map.ByBull.SetItem(bull, trimmed);
        if (next != map.ByBull)
        {
            Apply(State with { Subgroups = next.IsEmpty ? null : new SubgroupMap(next) });
        }
    }

    /// <summary>
    /// How the sheet's shots are read against its bulls, NOTES-FROM-PLANNING.md entry 113 section 4: by nearest bull, or matched with named
    /// bulls taking more than one, or null for the default one-to-one matching. Every shot whose bull nobody chose is matched again under it.
    /// </summary>
    public void SetAssignmentRule(AssignmentRule? rule)
    {
        if (rule != State.Rule)
        {
            // The rule is how the sheet is read, not an edit: what it assigns becomes the reading edits are measured from, so no shot it
            // places is flagged as moved by an edit.
            var next = Rematch(State with { Rule = rule });
            Apply(next.Assignment is { } review ? next with { Assignment = review with { Shots = [.. review.Shots.Select(d => d with { DetectedBull = d.Bull })] } } : next);
        }
    }

    /// <summary>
    /// How many rounds the person says they fired at the group, sighters not counted, or null to stop checking (NOTES-FROM-PLANNING.md
    /// entry 95 section 2). The detector can never know this and the shooter always does, so it turns "is this mark two holes", which the
    /// image cannot answer, into "you fired ten and nine are marked", which arithmetic can.
    /// </summary>
    public void SetExpectedShots(int? shots)
    {
        int? value = shots is > 0 ? shots : null;
        if (value != State.ExpectedShots)
        {
            Apply(State with { ExpectedShots = value });
        }
    }

    /// <summary>
    /// Which rifle, barrel and load the sheet was shot with (NOTES-FROM-PLANNING.md entry 97 section 2). The rifle is kept whole, click value
    /// included, so a correction read from a saved marking is the one that was right when it was shot, whatever the record book says since.
    /// </summary>
    public void SetEquipment(Rifle? rifle, string? barrel, string? load)
    {
        string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        var next = State with { Rifle = rifle, Barrel = Clean(barrel), Load = Clean(load) };
        if (next != State)
        {
            Apply(next);
        }
    }

    /// <summary>Takes every bull out of its subgroup.</summary>
    public void ClearSubgroups()
    {
        if (State.Subgroups is not null)
        {
            Apply(State with { Subgroups = null });
        }
    }

    /// <summary>
    /// Take one mark as two shots, NOTES-FROM-PLANNING.md entry 94 section 4: the shot moves to the first half and a second appears at the
    /// other, both on the bull the mark held. The size flag goes from both, because it was a statement about one mark that is now two, and
    /// the pair shows up in the queue as a bull holding two shots, which is the item that asks whether that is what happened.
    /// </summary>
    /// <returns>The id of the second shot.</returns>
    public int SplitShot(int id, PointD first, PointD second)
    {
        var shot = State.Find(id) ?? throw new ArgumentOutOfRangeException(nameof(id), id, "no such shot");
        Update(id, s => s with { Image = first, Oversize = null, Size = null, MeasuredDiameterInches = null, Provenance = Touched(s.Provenance) });
        return AddShot(second, shot.Bull);
    }

    /// <summary>
    /// Replaces the bulls and the detected shots with what the automatic path found, keeping shots placed by hand, as one step that
    /// can be undone. A kept shot that has no bull is assigned to its nearest under the new registration: marking by hand and then
    /// detecting left every hand-placed shot unassigned, measured from one aim across the whole sheet (entry 39 section 1).
    /// </summary>
    public void LoadDetections(ScaleReference scale, IEnumerable<BullAim> bulls, IEnumerable<(PointD Image, int? Bull)> detections, string summary)
    {
        ArgumentNullException.ThrowIfNull(detections);
        Load(scale, bulls, [.. detections.Select(d => (d.Image, d.Bull, (double?)null, (DetectedOversize?)null, (MarkSize?)null))], summary, null, _ => null);
    }

    /// <summary>
    /// The same, with what the matching decided and what the detector refused, NOTES-FROM-PLANNING.md entry 70 section 4: each detected
    /// shot's figures are kept under the id it is given here, with the method, its reason and the refused candidates, so the editor can
    /// show a contested case and count what needs review. A shot kept from before carries no figures, because the matching did not place it.
    /// </summary>
    public void LoadDetections(ScaleReference scale, IEnumerable<BullAim> bulls, IReadOnlyList<DetectedShot> detections, ShotAssignmentResult? assignment, IEnumerable<RejectedCandidate> rejected, string summary, DetectionRecord? detection = null)
    {
        ArgumentNullException.ThrowIfNull(detections);
        ArgumentNullException.ThrowIfNull(rejected);
        Load(scale, bulls, [.. detections.Select(d => (d.Image, d.Assignment.Bull, d.DiameterInches, d.Oversize, d.Size))], summary, detection, firstId => assignment is null
            ? null
            : new AssignmentReview(assignment.Method, assignment.Reason, [.. detections.Select((d, i) => AssignmentReview.Detail(firstId + i, d.Assignment, d.Assignment.Bull))], [.. rejected], assignment.Method));
    }

    private void Load(ScaleReference scale, IEnumerable<BullAim> bulls, IReadOnlyList<(PointD Image, int? Bull, double? Diameter, DetectedOversize? Oversize, MarkSize? Size)> detections, string summary, DetectionRecord? detection, Func<int, AssignmentReview?> review)
    {
        int id = State.NextId;
        var registered = State with { Scale = scale, Bulls = [.. bulls], RegistrationSummary = summary, Detection = detection };
        var kept = State.Shots.Where(s => s.Provenance == ShotProvenance.Manual).Select(s => s.Bull is null ? s with { Bull = NearestBull(registered, s.Image) } : s).ToList();
        int firstId = id;
        var detected = detections.Select(d => new MarkedShot(id++, d.Image, ShotProvenance.Automatic, Bull: d.Bull, MeasuredDiameterInches: d.Diameter, Oversize: d.Oversize, Size: d.Size)).ToList();
        Apply(Rematch(registered with
        {
            Shots = [.. kept, .. detected],
            NextId = id,
            Assignment = review(firstId),
        }));
    }

    /// <summary>
    /// The matching rule of NOTES-FROM-PLANNING.md entry 70 section 3, applied after every change to the shots.
    /// <list type="number">
    /// <item><b>A person's choice of bull is a constraint, not an input.</b> A shot whose bull a person chose keeps it; every other shot is
    /// matched, including one placed by hand whose bull the software picked, because placing a hole is not choosing a bull (entry 74
    /// section 1, correcting entry 70). Re-solving the chosen ones too would let one reassignment silently reverse another, which
    /// DESIGN.md section 2 rules out. Positions are never re-solved: a mark a person placed or moved stays where they put it.</item>
    /// <item><b>The rest re-solve on every edit</b>, against the bulls no such decision holds, so a shot added, moved or deleted never leaves
    /// an answer computed for a different set of shots.</item>
    /// <item><b>A shot the re-solve moves stays visible</b> as moved, <see cref="AssignmentReview.Moved"/>, for as long as it stays moved.</item>
    /// <item><b>Section 13's counts rule holds live.</b> More unplaced shots than free bulls, and no matching is forced: each goes to its nearest
    /// free bull, every one is flagged, and <see cref="AssignmentReview.MethodChanged"/> says so. Sighter and scoring bulls are separate
    /// pools with the rule applied in each, so a sighter's hole is never matched to a scoring bull (entry 73 section 1).</item>
    /// <item><b>Undo restores the pins</b> as well as the positions, because both are the state undo keeps.</item>
    /// </list>
    /// It runs while a detected sheet is loaded: it needs the page mapping and each bull's declared position, and classifies against the
    /// declared geometry (entry 70 section 5). A marking with no detection, or one reopened from a file, which keeps no mapping, is left
    /// to the nearest-bull rule it has always used.
    /// </summary>
    private static MarkingState Rematch(MarkingState state)
    {
        if (state.Assignment is not { } previous || state.Scale is not SheetReference sheet || state.Bulls.Any(b => b.Declared is null))
        {
            return state;
        }

        // A bull a person's choice holds is full once it holds what the rule expects of it, one unless the rule names more (entry 113 section 4).
        var held = state.Shots.Where(s => s.IsShot && s.BullChosen && s.Bull is not null).GroupBy(s => s.Bull!.Value).ToDictionary(g => g.Key, g => g.Count());
        var rule = state.Rule;
        int Room(int bull) => (rule?.For(bull) ?? 1) - held.GetValueOrDefault(bull);
        var taken = held.Keys.ToHashSet();
        var free = state.Shots.Where(s => s.IsShot && !s.BullChosen).ToList();
        var open = state.Bulls.Where(b => rule is { NearestOnly: true } || Room(b.Index) > 0).ToList();
        var result = ShotAssignment.Assign([.. free.Select(s => sheet.Mapping.ToPage(s.Image))], [.. open.Select(b => b.Declared!.Value)], scoring: [.. open.Select(b => b.Scoring)],
            capacity: rule is null ? null : [.. open.Select(b => Math.Max(1, Room(b.Index)))], nearestOnly: rule?.NearestOnly == true);
        int? Index(int? position) => position is { } p && p >= 0 ? open[p].Index : null;

        var bulls = new Dictionary<int, int?>();
        var details = new List<ShotAssignmentDetail>(free.Count);
        for (int i = 0; i < free.Count; i++)
        {
            var matched = result.Shots[i];
            int? bull = Index(matched.Bull);
            bulls[free[i].Id] = bull;
            // A shot the matching has not placed before, one just placed by hand, is measured against the bull it was given when placed.
            details.Add(AssignmentReview.Detail(free[i].Id, matched with { Bull = bull, NearestBull = Index(matched.NearestBull) ?? -1 }, previous.For(free[i].Id) is { } before ? before.DetectedBull : free[i].Bull));
        }

        string reason = taken.Count == 0
            ? result.Reason
            : string.Create(CultureInfo.InvariantCulture, $"{result.Reason}, over the {free.Count} shots whose bull nobody chose and the {open.Count} bulls no choice of yours holds");
        return state with
        {
            Shots = [.. state.Shots.Select(s => bulls.TryGetValue(s.Id, out int? bull) ? s with { Bull = bull } : s)],
            Assignment = previous with { Method = result.Method, Reason = reason, Shots = [.. details] },
        };
    }

    private void Update(int id, Func<MarkedShot, MarkedShot> change)
    {
        if (State.Find(id) is not { } shot)
        {
            return;
        }

        Apply(Rematch(State with { Shots = State.Shots.Replace(shot, change(shot)) }));
    }

    private static ShotProvenance Touched(ShotProvenance provenance) => provenance == ShotProvenance.Automatic ? ShotProvenance.Corrected : provenance;
}

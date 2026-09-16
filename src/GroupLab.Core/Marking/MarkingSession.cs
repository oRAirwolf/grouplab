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
/// One impact on the image: its position in image pixels, where that came from, whether it is excluded and why, whether it has
/// been marked as not a shot at all (a detection that was handwriting, a staple, a tear), and the bull it is assigned to, if any.
/// </summary>
public sealed record MarkedShot(int Id, PointD Image, ShotProvenance Provenance, ExclusionReason? Exclusion = null, bool NotAShot = false, int? Bull = null)
{
    /// <summary>Counted in the group: not marked as not a shot. Excluded shots are counted in the full figures and left out of the reduced ones.</summary>
    public bool IsShot => !NotAShot;
}

/// <summary>
/// A bull a shot can be assigned to: its index and label in the target definition, its centre in image pixels, and, for a detected sheet,
/// its declared centre in page dmm. The image centre is where the bull was located, and offsets are measured from it; the declared centre
/// is the definition's exact geometry, and assignment classifies against it (NOTES-FROM-PLANNING.md entry 70 section 5).
/// </summary>
public sealed record BullAim(int Index, string Label, PointD Image, bool Scoring = true, PointD? Declared = null);

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
    AssignmentReview? Assignment = null)
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
    /// separate step, and an unassigned shot on such a sheet is measured from the wrong place.
    /// </summary>
    public int AddShot(PointD image, int? bull = null)
    {
        int id = State.NextId;
        Apply(Rematch(State with { Shots = State.Shots.Add(new MarkedShot(id, image, ShotProvenance.Manual, Bull: bull ?? NearestBull(State, image))), NextId = id + 1 }));
        return id;
    }

    /// <summary>
    /// Moves a shot; a detected shot the user moves becomes corrected. A shot on its nearest bull stays on its nearest bull wherever it is
    /// moved, and a shot the user assigned elsewhere, or unassigned, keeps that choice (entry 39 section 1).
    /// </summary>
    public void MoveShot(int id, PointD image) => Update(id, s => s with
    {
        Image = image,
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

    /// <summary>Marks a detection as not a shot, or restores it.</summary>
    public void SetNotAShot(int id, bool notAShot) => Update(id, s => s with { NotAShot = notAShot, Provenance = Touched(s.Provenance) });

    /// <summary>Assigns a shot to a bull, or to none, DESIGN.md section 13's click a hole then click a bull; a detected shot reassigned becomes corrected.</summary>
    public void AssignBull(int id, int? bull) => Update(id, s => s with { Bull = bull, Provenance = Touched(s.Provenance) });

    /// <summary>
    /// Replaces the bulls and the detected shots with what the automatic path found, keeping shots placed by hand, as one step that
    /// can be undone. A kept shot that has no bull is assigned to its nearest under the new registration: marking by hand and then
    /// detecting left every hand-placed shot unassigned, measured from one aim across the whole sheet (entry 39 section 1).
    /// </summary>
    public void LoadDetections(ScaleReference scale, IEnumerable<BullAim> bulls, IEnumerable<(PointD Image, int? Bull)> detections, string summary)
    {
        ArgumentNullException.ThrowIfNull(detections);
        Load(scale, bulls, [.. detections], summary, _ => null);
    }

    /// <summary>
    /// The same, with what the matching decided and what the detector refused, NOTES-FROM-PLANNING.md entry 70 section 4: each detected
    /// shot's figures are kept under the id it is given here, with the method, its reason and the refused candidates, so the editor can
    /// show a contested case and count what needs review. A shot kept from before carries no figures, because the matching did not place it.
    /// </summary>
    public void LoadDetections(ScaleReference scale, IEnumerable<BullAim> bulls, IReadOnlyList<DetectedShot> detections, ShotAssignmentResult? assignment, IEnumerable<RejectedCandidate> rejected, string summary)
    {
        ArgumentNullException.ThrowIfNull(detections);
        ArgumentNullException.ThrowIfNull(rejected);
        Load(scale, bulls, [.. detections.Select(d => (d.Image, d.Assignment.Bull))], summary, firstId => assignment is null
            ? null
            : new AssignmentReview(assignment.Method, assignment.Reason, [.. detections.Select((d, i) => AssignmentReview.Detail(firstId + i, d.Assignment, d.Assignment.Bull))], [.. rejected], assignment.Method));
    }

    private void Load(ScaleReference scale, IEnumerable<BullAim> bulls, IReadOnlyList<(PointD Image, int? Bull)> detections, string summary, Func<int, AssignmentReview?> review)
    {
        int id = State.NextId;
        var registered = State with { Scale = scale, Bulls = [.. bulls], RegistrationSummary = summary };
        var kept = State.Shots.Where(s => s.Provenance == ShotProvenance.Manual).Select(s => s.Bull is null ? s with { Bull = NearestBull(registered, s.Image) } : s).ToList();
        int firstId = id;
        var detected = detections.Select(d => new MarkedShot(id++, d.Image, ShotProvenance.Automatic, Bull: d.Bull)).ToList();
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
    /// <item><b>A person's decision is a constraint, not an input.</b> A shot placed by hand or corrected keeps its bull; only shots detection
    /// placed and nobody has touched are matched. Re-solving everything would let one person's reassignment silently reverse another
    /// shot's, which is the behaviour DESIGN.md section 2 rules out.</item>
    /// <item><b>The rest re-solve on every edit</b>, against the bulls no such decision holds, so a shot added, moved or deleted never leaves
    /// an answer computed for a different set of shots.</item>
    /// <item><b>A shot the re-solve moves stays visible</b> as moved, <see cref="AssignmentReview.Moved"/>, for as long as it stays moved.</item>
    /// <item><b>Section 13's counts rule holds live.</b> More unplaced shots than free bulls, and no matching is forced: each goes to its nearest
    /// free bull, every one is flagged, and <see cref="AssignmentReview.MethodChanged"/> says so.</item>
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

        var taken = state.Shots.Where(s => s.IsShot && s.Provenance != ShotProvenance.Automatic && s.Bull is not null).Select(s => s.Bull!.Value).ToHashSet();
        var free = state.Shots.Where(s => s.IsShot && s.Provenance == ShotProvenance.Automatic).ToList();
        var open = state.Bulls.Where(b => !taken.Contains(b.Index)).ToList();
        var result = ShotAssignment.Assign([.. free.Select(s => sheet.Mapping.ToPage(s.Image))], [.. open.Select(b => b.Declared!.Value)]);
        int? Index(int? position) => position is { } p && p >= 0 ? open[p].Index : null;

        var bulls = new Dictionary<int, int?>();
        var details = new List<ShotAssignmentDetail>(free.Count);
        for (int i = 0; i < free.Count; i++)
        {
            var matched = result.Shots[i];
            int? bull = Index(matched.Bull);
            bulls[free[i].Id] = bull;
            details.Add(AssignmentReview.Detail(free[i].Id, matched with { Bull = bull, NearestBull = Index(matched.NearestBull) ?? -1 }, previous.For(free[i].Id)?.DetectedBull));
        }

        string reason = taken.Count == 0
            ? result.Reason
            : string.Create(CultureInfo.InvariantCulture, $"{result.Reason}, over the {free.Count} untouched detections and the {open.Count} bulls no decision of yours holds");
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

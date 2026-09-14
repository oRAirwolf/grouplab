using System.Collections.Immutable;
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

/// <summary>A bull a shot can be assigned to: its index and label in the target definition, and its centre in image pixels.</summary>
public sealed record BullAim(int Index, string Label, PointD Image);

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
    Calibre? Calibre = null)
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

    public void SetPointOfAim(PointD? image) => Apply(State with { PointOfAim = image });

    /// <summary>Places a shot by hand, and returns its id.</summary>
    public int AddShot(PointD image, int? bull = null)
    {
        int id = State.NextId;
        Apply(State with { Shots = State.Shots.Add(new MarkedShot(id, image, ShotProvenance.Manual, Bull: bull)), NextId = id + 1 });
        return id;
    }

    /// <summary>Moves a shot; a detected shot the user moves becomes corrected.</summary>
    public void MoveShot(int id, PointD image) => Update(id, s => s with { Image = image, Provenance = Touched(s.Provenance) });

    public void DeleteShot(int id)
    {
        if (State.Find(id) is { } shot)
        {
            Apply(State with { Shots = State.Shots.Remove(shot) });
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
    /// can be undone.
    /// </summary>
    public void LoadDetections(ScaleReference scale, IEnumerable<BullAim> bulls, IEnumerable<(PointD Image, int? Bull)> detections, string summary)
    {
        ArgumentNullException.ThrowIfNull(detections);
        int id = State.NextId;
        var kept = State.Shots.Where(s => s.Provenance == ShotProvenance.Manual).ToList();
        var detected = detections.Select(d => new MarkedShot(id++, d.Image, ShotProvenance.Automatic, Bull: d.Bull)).ToList();
        Apply(State with
        {
            Scale = scale,
            Bulls = [.. bulls],
            Shots = [.. kept, .. detected],
            NextId = id,
            RegistrationSummary = summary,
        });
    }

    private void Update(int id, Func<MarkedShot, MarkedShot> change)
    {
        if (State.Find(id) is not { } shot)
        {
            return;
        }

        Apply(State with { Shots = State.Shots.Replace(shot, change(shot)) });
    }

    private static ShotProvenance Touched(ShotProvenance provenance) => provenance == ShotProvenance.Automatic ? ShotProvenance.Corrected : provenance;
}

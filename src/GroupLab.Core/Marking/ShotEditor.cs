using System.Collections.Immutable;
using System.Globalization;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Marking;

/// <summary>
/// What one action in the shot editor is called, what key runs it, and whether it is available on this shot. The list is data rather than a
/// row of buttons on a screen, so the popover, the shots list's row menu and the keyboard handler all read one definition and cannot come to
/// disagree about what Delete does or which key does it.
/// </summary>
/// <param name="Id">A stable name for the action, used by the screen and by tests, never shown.</param>
/// <param name="Text">What the button says.</param>
/// <param name="Key">The key printed beside it, or null where the action has no key.</param>
/// <param name="Available">False where the action cannot apply to this shot, so the screen shows it greyed rather than hiding it and moving everything else.</param>
/// <param name="On">True where the action is a mark this shot already carries, so the button reads as pressed.</param>
public sealed record EditorAction(string Id, string Text, string? Key, bool Available = true, bool On = false);

/// <summary>How far one press of an arrow key moves a shot, and in what.</summary>
/// <param name="Fine">One press.</param>
/// <param name="Coarse">One press with Shift held.</param>
/// <param name="Unit">"in" where a scale is set, "px" where none is, which is what the step is actually in.</param>
public sealed record NudgeSteps(double Fine, double Coarse, string Unit)
{
    /// <summary>How it reads beside the arrow keys: "0.01 in a press, 0.1 in with Shift".</summary>
    public string Describe() => string.Create(CultureInfo.InvariantCulture, $"{Fine:0.###} {Unit} a press, {Coarse:0.###} {Unit} with Shift");
}

/// <summary>
/// Everything the editing popover shows for one shot, NOTES-FROM-PLANNING.md entry 131 section 2. It is built from the marking state alone,
/// so a test can assert what a person would see without a screen, and the screen has nothing to decide.
/// </summary>
/// <param name="ShotId">The shot being edited.</param>
/// <param name="Title">The shot's name as the rest of the application names it: "7", "7a", "unassigned".</param>
/// <param name="Marks">Sighter, flyer, excluded and not a shot, each with whether it is on.</param>
/// <param name="Bulls">Every bull the shot could be assigned to, the current one marked <see cref="EditorAction.On"/>, plus "no bull".</param>
/// <param name="Steps">What an arrow key moves.</param>
/// <param name="HoleSize">What the hole is taken to measure and where that came from, or null on a marking with no scale.</param>
/// <param name="Note">The note on the shot, or null.</param>
/// <param name="Shortcuts">Every key the editor answers to, for the line along the bottom of the popover.</param>
public sealed record ShotEditorPanel(
    int ShotId,
    string Title,
    ImmutableList<EditorAction> Marks,
    ImmutableList<EditorAction> Bulls,
    NudgeSteps Steps,
    string? HoleSize,
    string? Note,
    ImmutableList<EditorAction> Shortcuts);

/// <summary>
/// The shot editor's model, NOTES-FROM-PLANNING.md entry 131 section 2: click a hole and a small editor opens beside it.
/// <para>
/// <b>Nothing here draws anything.</b> The popover is a screen's job; what goes in it, what each button does, which keys run which action and
/// what a step of an arrow key means are decisions, and decisions belong where they can be tested. That is the same split
/// <see cref="MarkingSession"/> already makes, and it is why the editing operations this panel names are session operations a keyboard, a
/// touch screen or a test can call directly.
/// </para>
/// </summary>
public static class ShotEditor
{
    /// <summary>One press of an arrow key, on the target plane, in inches: fine enough to settle a hole's centre, coarse enough to be worth pressing.</summary>
    public const double FineStepInches = 0.01;

    /// <summary>One press with Shift, in inches.</summary>
    public const double CoarseStepInches = 0.10;

    /// <summary>One press with no scale set, in image pixels, where an inch is not yet a thing this marking knows about.</summary>
    public const double FineStepPixels = 1;

    /// <summary>One press with Shift and no scale set, in image pixels.</summary>
    public const double CoarseStepPixels = 10;

    /// <summary>What an arrow key moves on this marking: inches where a scale is set, pixels where none is.</summary>
    public static NudgeSteps Steps(MarkingState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.Scale is null
            ? new NudgeSteps(FineStepPixels, CoarseStepPixels, "px")
            : new NudgeSteps(FineStepInches, CoarseStepInches, "in");
    }

    /// <summary>
    /// The popover for a shot, or null where the marking holds no such shot, which is what a screen gets if the shot was deleted under it by
    /// an undo.
    /// </summary>
    public static ShotEditorPanel? For(MarkingState state, int shotId)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Find(shotId) is not { } shot)
        {
            return null;
        }

        string title = ShotLabels.For(state).FirstOrDefault(l => l.ShotId == shotId)?.Text ?? "this shot";
        return new ShotEditorPanel(shotId, title, Marks(state, shot), BullChoices(state, shot), Steps(state), HoleSize(state, shot), shot.Note, Shortcuts);
    }

    /// <summary>
    /// Sighter, flyer, excluded and not a shot, in the order the popover lists them, each saying whether the shot carries it.
    /// <para>
    /// <b>Flyer and excluded are two marks, not one.</b> Entry 131 section 2 asks for a flyer that is "kept but called out" beside an
    /// exclusion that leaves the shot out of the figures, and the difference is the point: calling a shot out says "look at this one", and
    /// dropping it from the figures says "this one does not count". A screen that made the first quietly do the second would remove a shot
    /// from a group because somebody wanted it pointed at, which is the exact thing docs/STATISTICS.md section 10 exists to stop.
    /// </para>
    /// </summary>
    private static ImmutableList<EditorAction> Marks(MarkingState state, MarkedShot shot)
    {
        bool onSighterBull = shot.Bull is { } b && state.Bulls.Any(x => x.Index == b && !x.Scoring);
        return
        [
            new EditorAction(
                "sighter",
                onSighterBull ? "Sighter (this bull's)" : "Sighter",
                "S",
                // A shot on a sighter bull is already a sighting shot because of where it sits; saying so again per shot would let the two
                // disagree, so the button reads as on and does nothing.
                Available: !onSighterBull,
                On: onSighterBull || shot.Sighter),
            new EditorAction("flyer", "Flyer, kept", "F", On: shot.Flyer),
            new EditorAction("exclude", "Leave out of the figures", "X", On: shot.Exclusion is not null),
            new EditorAction("notashot", "Not a shot", "N", On: shot.NotAShot),
        ];
    }

    /// <summary>Every bull, plus no bull, with the shot's own marked on. Empty on a marking with no bulls, where there is nothing to assign to.</summary>
    private static ImmutableList<EditorAction> BullChoices(MarkingState state, MarkedShot shot)
    {
        if (state.Bulls.Count == 0)
        {
            return [];
        }

        var bulls = state.Bulls.Where(b => b.Scoring).Concat(state.Bulls.Where(b => !b.Scoring));
        return
        [
            .. bulls.Select(b => new EditorAction(
                string.Create(CultureInfo.InvariantCulture, $"bull{b.Index}"),
                b.Scoring ? b.Label : b.Label + " (sighter)",
                null,
                On: shot.Bull == b.Index)),
            new EditorAction("bullnone", "No bull", null, On: shot.Bull is null),
        ];
    }

    /// <summary>
    /// What the hole is taken to measure and where that number came from: the detector, a person, or nowhere. The provenance is in the line
    /// rather than in a tooltip because a person about to type over a number should be able to see whether they are overriding a measurement
    /// or filling a blank.
    /// </summary>
    private static string? HoleSize(MarkingState state, MarkedShot shot)
    {
        if (state.Scale is null)
        {
            return null;
        }

        if (shot.ChosenDiameterInches is { } chosen)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{chosen:0.000} in, set by hand");
        }

        return shot.MeasuredDiameterInches is { } measured
            ? string.Create(CultureInfo.InvariantCulture, $"{measured:0.000} in, measured")
            : "not measured";
    }

    /// <summary>
    /// Every key the editor answers to, shown along the bottom of the popover. Entry 131 section 2 asks for the shortcuts to be shown in the
    /// popover; listing them here is what makes shown and handled the same list.
    /// </summary>
    public static ImmutableList<EditorAction> Shortcuts { get; } =
    [
        new EditorAction("move", "Move", "arrows"),
        new EditorAction("movecoarse", "Move further", "Shift+arrows"),
        new EditorAction("delete", "Delete", "Delete"),
        new EditorAction("sighter", "Sighter", "S"),
        new EditorAction("flyer", "Flyer, kept", "F"),
        new EditorAction("exclude", "Leave out", "X"),
        new EditorAction("notashot", "Not a shot", "N"),
        new EditorAction("add", "Add a shot", "A"),
        new EditorAction("undo", "Undo", "Ctrl+Z"),
        new EditorAction("redo", "Redo", "Ctrl+Y"),
        new EditorAction("close", "Close", "Escape"),
    ];

    /// <summary>
    /// Where a nudge puts a shot, in image pixels, for one press of an arrow key. With a scale set the step is an inch on the paper, so a
    /// press moves the same distance on the sheet wherever the shot sits and whichever way the photograph was taken; with no scale a pixel
    /// is a pixel, which is all such a marking knows.
    /// <para>
    /// <b>The conversion is a local linear fit rather than an inverse.</b> A <see cref="ScaleReference"/> maps pixels to the paper and not
    /// back, and the three kinds invert quite differently: a uniform scale trivially, a homography by an inverse matrix, and a registered
    /// sheet through a lens model that has no closed form at all. Sampling the map at the shot and one pixel each way gives the Jacobian
    /// there, which is exact for the first two and right to first order for the third, over a step of a hundredth of an inch. A degenerate
    /// fit, which a scale that collapses an axis would give, falls back to moving by pixels rather than moving by a guess.
    /// </para>
    /// </summary>
    public static PointD Nudged(MarkingState state, PointD from, int dx, int dy, bool coarse)
    {
        ArgumentNullException.ThrowIfNull(state);
        var steps = Steps(state);
        double step = coarse ? steps.Coarse : steps.Fine;
        if (state.Scale is not { } scale)
        {
            return new PointD(from.X + (dx * step), from.Y + (dy * step));
        }

        var at = scale.ToTarget(from);
        var alongX = scale.ToTarget(new PointD(from.X + 1, from.Y));
        var alongY = scale.ToTarget(new PointD(from.X, from.Y + 1));
        double a = alongX.X - at.X, b = alongY.X - at.X, c = alongX.Y - at.Y, d = alongY.Y - at.Y;
        double determinant = (a * d) - (b * c);
        if (Math.Abs(determinant) < 1e-12 || !double.IsFinite(determinant))
        {
            return new PointD(from.X + (dx * step), from.Y + (dy * step));
        }

        double wantX = dx * step, wantY = dy * step;
        return new PointD(
            from.X + (((d * wantX) - (b * wantY)) / determinant),
            from.Y + (((a * wantY) - (c * wantX)) / determinant));
    }
}

/// <summary>
/// Adding mode, NOTES-FROM-PLANNING.md entry 131 section 2: "Add several" stays in adding mode so each click adds a shot to that bull until
/// Escape or Done. It is a record rather than a flag on the screen so that what the mode is for, and what it has done so far, can be reported
/// to the person and undone as one thing.
/// </summary>
/// <param name="Bull">The bull every click adds to, or null for a marking with no bulls, where a shot goes where it is clicked.</param>
/// <param name="Added">The shots added since the mode started, oldest first.</param>
public sealed record AddingShots(int? Bull, ImmutableList<int> Added)
{
    public static AddingShots For(int? bull) => new(bull, []);

    public AddingShots With(int shotId) => this with { Added = Added.Add(shotId) };

    /// <summary>What the banner says while the mode is on, so the person knows what clicking will do and how to stop.</summary>
    public string Describe(MarkingState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        string where = Bull is { } b && state.Bulls.FirstOrDefault(x => x.Index == b) is { } bull
            ? "to bull " + bull.Label
            : "where you click";
        string so = Added.Count == 0
            ? "none yet"
            : string.Create(CultureInfo.InvariantCulture, $"{Added.Count} added");
        return string.Create(CultureInfo.InvariantCulture, $"Adding shots {where}: click to add, {so}. Escape or Done to stop.");
    }
}

using System.Globalization;

namespace GroupLab.Core.StoreTargets;

/// <summary>The five steps of adding a store-bought target, NOTES-FROM-PLANNING.md entry 348, in the order they are taken.</summary>
public enum FingerprintStep
{
    Photo,
    Scale,
    Straighten,
    Bulls,
    Send,
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 348, concept A on both: the words of the guided steps that make a store-bought target's fingerprint from a
/// photograph, one question at a time, shared by the computer and the phone so the two say the same. Entry 348 section 2 corrects the drawing:
/// no scale source is called the most accurate by fixed text, because entry 344's poster trial found a GroupLab sheet in the photo was not
/// the most accurate at poster size; each choice says what that trial measured for it instead (<see cref="Measured"/>).
/// </summary>
public static class FingerprintWords
{
    /// <summary>The screen's name, and the button on Targets that opens it.</summary>
    public const string Title = "Add a store-bought target";

    /// <summary>What the button on Targets leads to, beside it.</summary>
    public const string Offer = "A target GroupLab does not know yet: photograph it blank, and save a small file of its fingerprint to send, so a later GroupLab recognizes it.";

    /// <summary>The steps' names, as the step list and the progress bar have them.</summary>
    public static IReadOnlyList<string> Steps { get; } = ["Photo", "Size and scale", "Straighten", "The bulls", "Name and send"];

    public static string Name(FingerprintStep step) => Steps[(int)step];

    /// <summary>"Step 2 of 5", over the phone's progress bar.</summary>
    public static string Of(FingerprintStep step) => string.Create(CultureInfo.InvariantCulture, $"Step {(int)step + 1} of {Steps.Count}");

    /// <summary>Each step's one question.</summary>
    public static string Question(FingerprintStep step) => step switch
    {
        FingerprintStep.Photo => "A photo of the blank target",
        FingerprintStep.Scale => "How big is this target?",
        FingerprintStep.Straighten => "Are the corners right?",
        FingerprintStep.Bulls => "Are these the bulls?",
        _ => "Name it and save the file",
    };

    /// <summary>The line under the question; <paramref name="touch"/> says tap where the computer says click.</summary>
    public static string Explain(FingerprintStep step, bool touch) => step switch
    {
        FingerprintStep.Photo => "Lay the target flat and unshot, and take the photo square on, all four corners in it and the target filling most of the picture.",
        FingerprintStep.Scale => "GroupLab needs one true length to know the scale. Pick whichever you have.",
        FingerprintStep.Straighten => touch ? "Corners found. Drag a corner if it is off; pinch to zoom in." : "Corners found. Drag a corner if it is off; scroll to zoom in.",
        FingerprintStep.Bulls => touch ? "Tap a ring to remove it; tap the picture to add one." : "Click a ring to remove it; click the picture to add one.",
        _ => "Give it the name on its package.",
    };

    /// <summary>Said in place of "Corners found" where the photo's outline could not be found by itself.</summary>
    public const string CornersNotFound = "GroupLab could not find all four corners. Drag each amber corner onto a corner of the target.";

    /// <summary>Entry 362 section 2: the same, saying why, where the corner finder said; its best guess is what the handles start on.</summary>
    public static string CornersMissed(string? why) => string.IsNullOrWhiteSpace(why)
        ? CornersNotFound
        : $"GroupLab could not find all four corners: {why}. The amber corners start at its best guess; drag each onto a corner of the target.";

    /// <summary>Entry 362 section 5: the two Rotate buttons, what a screen reader says for them, and the keys on the computer.</summary>
    public const string RotateRight = "Rotate right";

    public const string RotateLeft = "Rotate left";

    public const string RotateRightName = "Rotate the picture a quarter turn clockwise";

    public const string RotateLeftName = "Rotate the picture a quarter turn counterclockwise";

    public const string RotateKeys = "R turns the picture clockwise, Shift and R the other way.";

    /// <summary>Entry 362 section 3: said when a corner let go moves onto a clear corner of the picture near it, and the button that undoes it.</summary>
    public const string Snapped = "The corner moved onto the clear corner nearest where you let it go.";

    public const string UndoSnap = "Undo";

    /// <summary>The scale worse than this, as a share, is warned of before the file is saved (entry 364).</summary>
    public const double PoorScale = 0.02;

    /// <summary>
    /// Entry 364: said on the last step where the scale is worse than <see cref="PoorScale"/>: two of Alan's first files took it from two
    /// points an inch apart and came out about 7 percent uncertain, which would make every group on the target that much wrong.
    /// </summary>
    public static string ScaleWarning(double uncertainty) => FormattableString.Invariant(
        $"This scale is only good to about {100 * uncertainty:0.#} percent, so every group measured on this target could be that much wrong. Go back to How big is this target? and use its printed size, or two points far apart.");

    public const string TakePhoto = "Take a photo";

    public const string ChoosePhoto = "Choose a photo";

    public const string Back = "Back";

    /// <summary>The button that moves on, named for where it goes; on the last step it saves.</summary>
    public static string Next(FingerprintStep step, bool phone) => step switch
    {
        FingerprintStep.Photo => "Next: size and scale",
        FingerprintStep.Scale => "Next: straighten",
        FingerprintStep.Straighten => "Next: the bulls",
        FingerprintStep.Bulls => "These are right",
        _ => phone ? "Save and share the file" : "Save reference file",
    };

    public const string AddBull = "Add a bull";

    /// <summary>Said after "Add a bull": where the next tap goes.</summary>
    public static string WhereTheBull(bool touch) => touch ? "Tap the center of the bull." : "Click the center of the bull.";

    /// <summary>The scale sources' names, as entry 348 section 2 draws them.</summary>
    public static string Choice(ScaleSource source) => source switch
    {
        ScaleSource.PrintedSize => "Its printed size",
        ScaleSource.GroupLabSheet => "A GroupLab sheet in the photo",
        _ => "Two points and a distance",
    };

    /// <summary>What to do for each source, shorter on the phone.</summary>
    public static string How(ScaleSource source, bool phone) => source switch
    {
        ScaleSource.PrintedSize => phone ? "From the package. Fastest." : "The sheet's outside size, from the package. Fastest.",
        ScaleSource.GroupLabSheet => phone ? "Lay one on the target, then take the photo." : "Lay any GroupLab sheet or card on the target and take the photo again.",
        _ => phone ? "Tap both ends of something you measured, as far apart as the target allows, and type the length; ends close together give a poor scale."
            : "Click both ends of something you measured, as far apart as the target allows, then type its length; ends close together give a poor scale.",
    };

    /// <summary>
    /// The honest accuracy line under the source picked: what entry 344's poster trial measured for it, on computer-made photos of a 12 by 18
    /// in and a 23 by 35 in poster, twelve of each, tilted 1 to 29 degrees (docs/PHASE1-RESULTS.md, entry 344). Typical is the median.
    /// </summary>
    public static string Measured(ScaleSource source) => source switch
    {
        ScaleSource.PrintedSize => "Measured on computer-made photos of two test posters: typically within 0.01 percent at 12 by 18 in and 0.28 percent at 23 by 35 in, 0.63 percent at worst.",
        ScaleSource.GroupLabSheet => "Measured on the same photos: a Letter sheet was read in 8 of 12 at 12 by 18 in, typically within 0.28 percent, 0.55 at worst, and in none at 23 by 35 in, where it was too small to read.",
        _ => "Measured on the same photos: typically within 0.16 percent at 12 by 18 in and 0.40 percent at 23 by 35 in, 0.80 percent at worst.",
    };

    public const string Width = "Width";

    public const string Height = "Height";

    public const string Inches = "in";

    public const string By = "by";

    /// <summary>The two points' length, typed.</summary>
    public const string Distance = "Distance between them, inches";

    public static string PointsSaid(int placed, bool touch) => placed switch
    {
        0 => touch ? "Tap one end of the length you measured on the picture." : "Click one end of the length you measured on the picture.",
        1 => touch ? "Now tap the other end." : "Now click the other end.",
        _ => touch ? "Both ends placed. Tap again to start over." : "Both ends placed. Click again to start over.",
    };

    public const string ReadingSheet = "Reading the GroupLab sheet in the photo.";

    public const string SheetRead = "GroupLab sheet read.";

    public const string SheetNotRead = "No GroupLab sheet could be read in this photo. Lay one flat on the target, with its corner markers showing, and take the photo again.";

    public const string NameBox = "Name";

    public const string NameHint = "Cabela's 23 by 35 in sight-in";

    /// <summary>Entry 348 section 3, under the save button: what the file holds.</summary>
    public const string Never = "The fingerprint, name, size and bulls. Never the photo.";

    /// <summary>Entry 344: a file is checked by hand before anything joins the library everyone gets.</summary>
    public const string Checked = "Send the file to GroupLab's maker. Each one is checked by hand before it joins the library a later GroupLab recognizes.";

    public const string Working = "Working on the photo.";

    /// <summary>What a step needs before Next, said when Next is pressed without it.</summary>
    public const string NeedPhoto = "Take or choose a photo of the target first.";

    public const string NeedSize = "Type the target's width and height in inches, as the package gives them.";

    public const string NeedSheet = "The GroupLab sheet has not been read; choose another way, or take the photo again with the sheet in it.";

    public const string NeedPoints = "Place both ends of the length on the picture, and type how far apart they are.";

    public const string NeedBull = "Mark at least one bull: the point you aim at.";

    public const string NeedName = "Type the target's name.";

    public const string NotATarget = "These corners do not make a target's outline. Drag each one onto a corner of the target.";

    /// <summary>The printed size, as a reference names it.</summary>
    public static string Size(double width, double height) => string.Create(CultureInfo.InvariantCulture, $"{width:0.#} by {height:0.#} in");

    /// <summary>A short file name from the target's name: its letters and digits, joined by hyphens.</summary>
    public static string FileName(string name)
    {
        var words = new List<string>();
        var word = new System.Text.StringBuilder();
        foreach (char c in (name ?? "").ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(c))
            {
                word.Append(c);
            }
            else if (word.Length > 0)
            {
                words.Add(word.ToString());
                word.Clear();
            }
        }

        if (word.Length > 0)
        {
            words.Add(word.ToString());
        }

        string id = string.Join('-', words);
        return id.Length == 0 ? "store-target" : id.Length > 60 ? id[..60].TrimEnd('-') : id;
    }
}

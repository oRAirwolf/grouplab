using System.Globalization;

namespace GroupLab.Core.Registration;

/// <summary>One way on from "Which target is this?": its label and the line under it.</summary>
public sealed record OpeningChoice(string Label, string Says);

/// <summary>
/// The words of the two things opening a picture can end in when it names no sheet, NOTES-FROM-PLANNING.md entry 356 sections 5 to 7, on the
/// canvas's boards "Not an error", "B, final candidate: reading harder, and not a GroupLab sheet" and "B, final candidate, on a phone". The
/// desktop and the phone say the same, so the words are here once.
/// </summary>
public static class OpeningWords
{
    // "Which target is this?": not an error, no warning and no amber.

    public const string WhichTitle = "Which target is this?";

    public const string WhichSays = "It is not a GroupLab sheet and not one of the store-bought targets GroupLab knows, so it needs one thing from you: its scale.";

    public static OpeningChoice ByHand { get; } = new("Mark it by hand", "Give one true length: the sheet's size, a ring, or a ruler in the picture.");

    public static OpeningChoice StoreBought { get; } = new("It is a store-bought target", "Pick it from the library, or add it so it is known next time.");

    public static OpeningChoice GroupLabSheet { get; } = new("It is a GroupLab sheet", "Choose which one, or take the picture again with its corner codes in view.");

    public static IReadOnlyList<OpeningChoice> WhichChoices { get; } = [ByHand, StoreBought, GroupLabSheet];

    public const string WhichFooter = "Find holes (Experimental) works on any of these once the scale is set.";

    /// <summary>What the status line says once the person has chosen to mark a target by hand.</summary>
    public const string ByHandStatus = "Mark it by hand: set a scale with one true length, the sheet's size, a ring or a ruler, then place the shots.";

    // Entry 517 section 1: marking by hand, the scale asked for at the top of the right column, where the person is looking, until it is set.

    /// <summary>
    /// Entry 518: on the phone, "Which target is this?" come back to from adding a store-bought target or from marking by hand, where the
    /// picture still has no scale: what gives it one. Adding a target saves a reference file for the project and gives this picture nothing.
    /// </summary>
    public const string StillNoScale = "This picture still has no scale. Mark it by hand gives it one: a length you know, such as the target's printed size, typed in.";

    /// <summary>The heading of the card that asks for the scale.</summary>
    public const string ScaleAskTitle = "Set the scale";

    /// <summary>What the card says the scale is and where it can come from.</summary>
    public const string ScaleAskSays = "GroupLab needs one true length on this picture before it can measure anything: a ruler or a ring whose size you know, or the sheet's own printed size.";

    /// <summary>The choice of a known length: the length tool.</summary>
    public const string ScaleAskLength = "A length I know: tap its two ends";

    /// <summary>The choice of the sheet's size: the rectangle tool.</summary>
    public const string ScaleAskCorners = "The sheet's printed size: tap its four corners";

    /// <summary>The choice of finding the paper's edges, then typing its size.</summary>
    public const string ScaleAskPaper = "Find the paper's edges for me";

    // The problem dialog: it looks like a GroupLab sheet and its codes would not read.

    public const string CodesTitle = "GroupLab could not read this sheet's codes";

    public const string UnknownTitle = "This sheet's codes name a sheet GroupLab does not have";

    /// <summary>The corner codes a GroupLab sheet prints, where the sheet itself is not known.</summary>
    public const int CornerCodes = 4;

    /// <summary>Why, as far as GroupLab knows: none or some of the corner codes read, and nothing measured.</summary>
    public static string CodesReason(int read, int of) => read == 0
        ? string.Create(CultureInfo.InvariantCulture, $"This looks like a GroupLab sheet, but none of its {Count(of)} corner codes (outlined) could be read, so GroupLab does not know which sheet it is or its scale. Nothing has been measured yet.")
        : string.Create(CultureInfo.InvariantCulture, $"This looks like a GroupLab sheet, but only {read} of its {Count(of)} corner codes (outlined) could be read, and they did not say which sheet it is, so GroupLab does not know it or its scale. Nothing has been measured yet.");

    /// <summary>Codes that read and named a sheet whose description could not be read.</summary>
    public static string UnknownReason(string id) =>
        $"Its codes read and name {id}, a sheet made on another copy of GroupLab or by the target generator, but the description of the sheet they carry could not be read. Choose the sheet if you saved it, or make it again with the same settings and save it to your own sheets.";

    /// <summary>The sheet's printed name, where a sheet GroupLab knows matches it.</summary>
    public static string PrintedName(string name) => $"Its printed name looks like {name}.";

    /// <summary>The phone's shorter line with the printed name.</summary>
    public static string LooksLikeName(string name) => $"It looks like {name}.";

    public const string ChooseSheet = "Choose the sheet";

    public const string ReadHarder = "Try again, reading harder";

    public const string MarkByHand = "Mark it by hand";

    public const string TakeAgain = "Take it again";

    public const string NotGroupLab = "Not a GroupLab sheet?";

    public const string StoreOrDrawn = "It is a store-bought or hand-drawn target";

    public const string StoreOrDrawnShort = "Store-bought or hand-drawn";

    public const string ShowWhatWentWrong = "Show what went wrong";

    public const string SendToProject = "Send it to the project";

    public const string MoreChoices = "More choices";

    public const string AllChoices = "All choices";

    public const string Dismiss = "Dismiss";

    /// <summary>The amber bar a dismissed problem leaves, and what Show work names.</summary>
    public const string BarSays = "GroupLab could not read this sheet's codes. Nothing has been measured.";

    public const string ShowWorkSays = "Show work: codes not read";

    // The stage list beside the picture.

    public const string PictureOpened = "Picture opened";

    public const string LooksLikeGroupLab = "Looks like a GroupLab sheet";

    public static string CornerCodesStage(int read, int of) => string.Create(CultureInfo.InvariantCulture, $"Corner codes: {read} of {of}");

    public const string WhichSheet = "Which sheet";

    public const string FindingHoles = "Finding holes";

    public const string WhichSheetThenHoles = "Which sheet, then the holes";

    private static string Count(int n) => n switch { 2 => "two", 3 => "three", 4 => "four", 6 => "six", 8 => "eight", _ => n.ToString(CultureInfo.InvariantCulture) };
}

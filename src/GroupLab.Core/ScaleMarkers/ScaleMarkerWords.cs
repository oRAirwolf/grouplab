using System.Globalization;

namespace GroupLab.Core.ScaleMarkers;

/// <summary>
/// Entry 365: the words for scale markers, shared by the computer and the phone so the two say the same. The accuracy lines are what
/// <c>grouplab marker-trial</c> measured on computer-made photos (docs/PHASE1-RESULTS.md, entry 365), never the concepts' planning estimates.
/// </summary>
public static class ScaleMarkerWords
{
    /// <summary>The heading on Targets.</summary>
    public const string Heading = "Scale markers";

    public const string Intro = "Put one of these beside a target and GroupLab works out the scale from the photo, with nothing typed or tapped.";

    /// <summary>The four kinds as the concepts name them, and what each gives.</summary>
    public const string Brackets = "Corner brackets";

    /// <summary>Entry 375: the printed codes give the scale, so neither the cut nor a gap moves it; the corners come from the paper's own edges.</summary>
    public const string BracketsGive = "Four L shaped pieces, cut roughly from one page, one placed flat near each corner of the target, anywhere close. The printed codes give the scale and the camera's angle, so the cut does not need to be neat; GroupLab finds the target's corners from its own edges where they show against the surface.";

    public const string Bars = "Scale bars";

    public const string BarsGive = "Two strips with a code at each end, 10 inches apart (250 mm on A4), laid flat along an edge. One gives the scale; two in an L give the angle too; two end to end give 20 inches for a poster. Touching the target or a little apart, the scale is the same.";

    public const string Stickers = "Board stickers";

    public const string StickersGive = "Four code stickers near the corners of your target board, measured once with a GroupLab sheet on the board. After that, every photo of a target on that board has its scale, with nothing placed.";

    public const string Card = "A bank card";

    public const string CardGives = "Any bank card, driver's license or gift card, back side up, laid flat on or beside the target. Nothing to print, and the least accurate of the four.";

    /// <summary>Entry 375: where the target's own corners were not found sure beside the brackets.</summary>
    public const string BracketCornersNotSure = "GroupLab is not sure of the target's corners beside the brackets. Drag each onto the target's own corner; the brackets have given the scale.";

    /// <summary>Entry 372: the scale labels, under Scale markers.</summary>
    public const string Labels = "Scale labels (M220 and other label printers)";

    public const string LabelsGive = "A label with rows of codes across its width, stuck flat on the target where you will not shoot: one gives the scale, two at opposite corners the camera's angle too. Every label has its own codes, so two on one target are told apart.";

    public const string LabelSize = "Label size loaded";

    public const string SaveLabels = "Save four scale labels for the printer's app…";

    /// <summary>Entry 386 section 3: the label printer's own check, a label of the size loaded, scanned at 600 dpi.</summary>
    public const string SaveCheckLabel = "Save the printer check label…", MeasureCheckLabel = "Measure a scanned check label…";

    public const string CheckLabelSteps = "To check the label printer: print the check label at 100 percent, scan it at 600 dpi, and measure the scan here. Across the label should be exact; along the paper's feed it may not be, and GroupLab reads scale labels across only.";

    /// <summary>The print buttons.</summary>
    public const string PrintBrackets = "Print corner brackets", PrintBars = "Print scale bars", PrintStickers = "Print board stickers", MeasureBoard = "Measure a board…";

    /// <summary>Under the print buttons: printed markers are as good as the printer.</summary>
    public const string PrintNote = "Print at Actual size (100%), never Fit to page. Printed markers are corrected by your printer check, as GroupLab sheets are; without one they can be off by up to 1.5 percent.";

    public const string NoPrinterCheck = "It assumes the markers printed at their own size; a printer check (Settings, Printers) removes that doubt.";

    /// <summary>Entry 365 section D, said wherever a card is used.</summary>
    public const string CardLeast = "A card is the least accurate marker: it is small beside a target, its corners are rounded and its thickness lifts it off the paper.";

    /// <summary>Entry 365 section D, privacy without exception: said on screen whenever a card was found.</summary>
    public const string CardBlanked = "The card was found and blanked out. GroupLab never keeps, shows in a saved picture, logs or sends that part of the photo, and never reads anything on a card.";

    public const string CardBackUp = "Lay the card back side up.";

    public const string NoStickers = "No board stickers were found. Put the four stickers of one set on the board, with a GroupLab sheet on it, and take the photo again.";

    public const string BoardNotMeasured = "Board stickers were found, but no board has been measured. On Targets, Scale markers, choose Measure a board.";

    public const string NoBoardSheet = "No GroupLab sheet could be read in this photo. Lay one flat on the board, among the stickers, and take the photo again.";

    /// <summary>The scale choice's name on the Size step, and what it does.</summary>
    public const string Choice = "Markers in the photo";

    public const string CardChoice = "A bank card in the photo";

    public static string How(bool card, bool phone) => card
        ? phone ? "Lay a bank card back side up beside the target, then take the photo." : "Lay any bank card, license or gift card back side up beside the target and take the photo again."
        : phone ? "Brackets, a scale bar or board stickers in the photo." : "Corner brackets, a scale bar or board stickers in the photo, printed from Targets, Scale markers.";

    public const string NeedMarkers = "No scale markers were found in this photo. Choose another way, or take the photo again with corner brackets, a scale bar or board stickers beside the target.";

    public const string NeedCard = "No card was found in this photo. Lay a bank card back side up, flat on or beside the target, with all four corners showing, and take the photo again.";

    /// <summary>Said where a board's stickers no longer agree with where they were measured.</summary>
    public static string BoardMoved(string name, double rms) => string.Create(CultureInfo.InvariantCulture,
        $"{name}'s stickers no longer agree with where they were measured (by about {rms:0.0} mm): the board may have bent or swollen. Measure it again (Targets, Scale markers, Measure a board).");

    public const string BoardNoPrinterCheck = "The GroupLab sheet's printer has no printer check, so the board is only as good as that print, up to 1.5 percent; a printer check (Settings, Printers) and measuring the board again remove that doubt.";

    /// <summary>A board in the list on Targets.</summary>
    public static string BoardLine(ScaleBoard board) => string.Create(CultureInfo.InvariantCulture,
        $"{board.Name}: stickers {board.Set}1 to {board.Set}4, measured {board.MeasuredOn:yyyy-MM-dd}, good to about {100 * board.Uncertainty:0.##} percent.");

    public const string NoBoards = "No board measured yet.";

    /// <summary>The name a new board is given: Board 1, Board 2 and so on, past any already saved.</summary>
    public static string NextBoard(IReadOnlyList<ScaleBoard> boards)
    {
        int n = 1;
        while (boards.Any(b => b.Name == "Board " + n.ToString(CultureInfo.InvariantCulture)))
        {
            n++;
        }

        return "Board " + n.ToString(CultureInfo.InvariantCulture);
    }

    public const string LabelStickers = "Board stickers for a label printer";

    public const string Forget = "Forget";

    public static string BoardSaved(ScaleBoard board) => string.Create(CultureInfo.InvariantCulture,
        $"Saved {board.Name}: stickers {board.Set}1 to {board.Set}4, measured to about {100 * board.Uncertainty:0.##} percent. Every photo with them in it now has its scale.");

    /// <summary>What was used, "4 corner brackets and scale bar 1".</summary>
    public static string What(IReadOnlyList<(MarkerKind Kind, int Piece)> used, string? board)
    {
        ArgumentNullException.ThrowIfNull(used);
        var parts = new List<string>();
        int brackets = used.Count(u => u.Kind == MarkerKind.Bracket);
        if (brackets > 0)
        {
            parts.Add(brackets == 1 ? "1 corner bracket" : string.Create(CultureInfo.InvariantCulture, $"{brackets} corner brackets"));
        }

        int bars = used.Count(u => u.Kind is MarkerKind.InchBar or MarkerKind.MetricBar);
        if (bars > 0)
        {
            parts.Add(bars == 1 ? "a scale bar" : string.Create(CultureInfo.InvariantCulture, $"{bars} scale bars"));
        }

        int labels = used.Count(u => u.Kind == MarkerKind.Label);
        if (labels > 0)
        {
            parts.Add(labels == 1 ? "a scale label" : string.Create(CultureInfo.InvariantCulture, $"{labels} rows of scale labels"));
        }

        if (used.Any(u => u.Kind == MarkerKind.Sticker))
        {
            parts.Add(board is null ? "board stickers" : board + "'s stickers");
        }

        if (used.Any(u => u.Kind == MarkerKind.Card))
        {
            parts.Add("a bank card");
        }

        return parts.Count switch
        {
            0 => "markers",
            1 => parts[0],
            _ => string.Join(", ", parts.Take(parts.Count - 1)) + " and " + parts[^1],
        };
    }

    /// <summary>
    /// The honest accuracy line under each kind, from <c>grouplab marker-trial</c> (entry 365): computer-made photos of a 12 by 12 in and a
    /// 23 by 35 in target, tilted 1 to 29 degrees, each marker printed at its exact size. Typical is the median.
    /// </summary>
    public static string Measured(MarkerKind kind) => kind switch
    {
        MarkerKind.Bracket => TrialBrackets,
        MarkerKind.InchBar or MarkerKind.MetricBar => TrialBars,
        MarkerKind.Sticker => TrialBoard,
        _ => TrialCard,
    };

    /// <summary>Under "Markers in the photo": every kind's measured figure in one line.</summary>
    public const string TrialAll = "Measured on computer-made photos: a measured board typically within 0.015 percent, a scale bar 0.06, corner brackets 0.07 on a 12 in target and 0.2 on a 23 by 35 in poster.";

    public const string TrialBrackets = "Measured on computer-made photos: typically within 0.07 percent on a 12 in target and 0.2 percent on a 23 by 35 in poster, 0.75 at worst.";

    public const string TrialBars = "Measured on computer-made photos: typically within 0.06 percent, one bar or two, 0.84 at worst.";

    public const string TrialBoard = "Measured on computer-made photos: typically within 0.015 percent, 0.03 at worst, on top of how well the board itself was measured, which GroupLab says when it saves it.";

    public const string TrialCard = "Measured on computer-made photos: typically within 0.15 percent, 0.31 at worst, the card found in 30 of 40; a real card's thickness adds about 0.1 to 0.2 percent.";
}

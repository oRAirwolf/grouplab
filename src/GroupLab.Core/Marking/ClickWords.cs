using System.Globalization;

namespace GroupLab.Core.Marking;

/// <summary>
/// Entry 422 (Jylee): what a click on the picture did, said in one short sentence a new user understands, on the status line under it.
/// </summary>
public static class ClickWords
{
    /// <summary>A plain click on a bull: it is chosen to give holes to.</summary>
    public static string BullChosen(string bull) => $"Bull {bull} chosen: a hole you click now goes to {Named(bull)}.";

    /// <summary>Shift or the command key on a bull: it joins or leaves the bulls a load is set on.</summary>
    public static string BullForLoad(string bull, bool added, int chosen) => string.Create(CultureInfo.InvariantCulture,
        $"Bull {bull} {(added ? "added to" : "taken out of")} the bulls for a load; {chosen} chosen, under Loads on this sheet.");

    /// <summary>A hole, or the line from its bull to it: the shot is selected and its row opened.</summary>
    public static string ShotSelected(string shot) => $"{Capital(shot)} selected; its row is open under Shots.";

    /// <summary>The Impact tool: a hole placed where the click was.</summary>
    public static string HoleAdded(string shot) => $"Hole added as {shot}; its row is open under Shots.";

    private static string Named(string bull) => bull.Length > 0 && bull.All(char.IsDigit) ? "bull " + bull : bull;

    private static string Capital(string words) => words.Length == 0 ? words : char.ToUpperInvariant(words[0]) + words[1..];
}

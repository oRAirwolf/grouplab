using System.Globalization;

namespace GroupLab.Core.StoreTargets;

/// <summary>
/// One answer to <see cref="FamilyQuestion"/>: a family member's name, its printed size, and the outline GroupLab draws of it, at the same
/// inches to the point as every other answer, so the larger target is drawn larger.
/// </summary>
/// <param name="Match">The member as it fits this picture: its own scale and bulls.</param>
/// <param name="Name">The member's name, its size in it, as the maker names it.</param>
/// <param name="Printed">The printed area, as GroupLab measured it, and the maker's item number.</param>
public sealed record FamilyAnswer(StoreTargetMatch Match, string Name, string Printed)
{
    /// <summary>The printed area's width and height, inches, for the outline.</summary>
    public (double Width, double Height) Inches => Match.Target.PrintedInches;

    /// <summary>The aim points, inches from the printed area's top left corner, for the outline.</summary>
    public IReadOnlyList<Imaging.PointD> Bulls
    {
        get
        {
            var layout = Match.Target.Fingerprint.Layout;
            return [.. Match.Target.Fingerprint.Bulls.Select(b => new Imaging.PointD(b.X - layout.X0, b.Y - layout.Y0))];
        }
    }
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 340 section 2, from Alan: "for the two targets that could be mistaken because they are the same design but
/// different size, just have a pop up that asks the user to confirm which size target it is". It is asked only when the picture cannot
/// tell the family's sizes apart by the trial's own measure (<see cref="StoreTargetRecognizer.Margin"/>); the answer chosen sets the scale
/// and the bulls; "Not sure" sets neither and leaves the scale to be measured by hand. The person's last answer for the family is offered
/// first next time. Entry 341 section 3: it settles which size, not how exact the print is, so the warning still follows the scale.
/// </summary>
public static class FamilyQuestion
{
    public const string Title = "Which target is this?";

    public const string NotSure = "Not sure";

    /// <summary>Why it is asked, under the title.</summary>
    public const string Why = "This target is sold at more than one size with the same printing, and the picture does not show which. The size you choose sets the scale and places the bulls.";

    /// <summary>What "Not sure" leaves the person to do.</summary>
    public const string NotSureSaid = "No size chosen, so no scale is set. Measure a known length or the target's printed size to set it.";

    /// <summary>The answers, the person's last answer for this family first, then smallest first.</summary>
    public static IReadOnlyList<FamilyAnswer> Answers(StoreTargetRecognition recognition, string? lastAnswer)
    {
        ArgumentNullException.ThrowIfNull(recognition);
        return [.. recognition.Family
            .OrderBy(m => m.Target.Id == lastAnswer ? 0 : 1)
            .ThenBy(m => m.Target.PrintedInches.Width * m.Target.PrintedInches.Height)
            .Select(m => new FamilyAnswer(m, m.Target.ShortName, string.Create(CultureInfo.CurrentCulture,
                $"Printed area about {m.Target.PrintedInches.Width:0.#} by {m.Target.PrintedInches.Height:0.#} in, {m.Target.Maker} {m.Target.Catalogue}")))];
    }

    /// <summary>The family the question is about, for remembering the answer.</summary>
    public static string? Family(StoreTargetRecognition recognition) => recognition?.Family.FirstOrDefault()?.Target.Family;
}

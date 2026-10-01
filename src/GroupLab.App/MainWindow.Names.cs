using Avalonia.Automation;
using Avalonia.Controls;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 342, worker A item 3: every field on the window has a name a screen reader announces, in the words of the
/// label a sighted person reads beside it. A text box's placeholder and a combo box's choice are not names, so without these a screen reader
/// said "edit" or "combo box" and nothing else. DesktopSweepTests holds every screen to it.
/// </summary>
public sealed partial class MainWindow
{
    /// <summary>A control's screen reader name, where it has none of its own.</summary>
    internal static T Named<T>(T control, string name) where T : Control
    {
        if (!string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(AutomationProperties.GetName(control)))
        {
            AutomationProperties.SetName(control, name);
        }

        return control;
    }

    /// <summary>The window's own fields, named once as it is made.</summary>
    private void NameTheFields()
    {
        foreach (var (control, name) in new (Control, string)[]
        {
            (calibreBox, "Caliber"),
            (shotDistance, "Shot distance"),
            (shotDistanceUnit, "Unit of the shot distance"),
            (roundsFired, "Rounds fired at the group, sighters not counted"),
            (rifleChoice, "Rifle"),
            (barrelChoice, "Barrel"),
            (loadChoice, "Load"),
            (paperChoice, "Paper it was printed on"),
            (backingChoice, "What was behind it"),
            (exclusionReason, "Why the shot is left out"),
            (sheetChoice, "Which sheet it is"),
            (sheetLabelBox, "This sheet's own label"),
            (cepPercentBox, "CEP at this percent"),
            (sessionRifle, "Rifle"),
            (sessionLoad, "Load"),
            (linearUnit, "Lengths"),
            (angularUnit, "Angles"),
            (distanceUnit, "Distances"),
            (themeChoice, "Theme"),
            (ballisticRifle, "Rifle"),
            (ballisticLoad, "Load"),
            (carryTo, "Carry the group to"),
            (atRange, "At one range"),
            (chronoSource, "From"),
            (chronoDate, "Date"),
            (chronoReadings, "Velocities, one a line or separated by commas"),
            (hitFrom, "Chance of a hit from"),
            (hitShape, "Target shape"),
            (hitSizeUnit, "Unit of the target's size"),
            (hitWidth, "Target width"),
            (hitHeight, "Target height"),
            (hitPreset, "Conditions"),
            (shotsPerBull, "Shots per bull"),
            (bullLoad, "Load on the chosen bulls"),
        })
        {
            Named(control, name);
        }
    }
}

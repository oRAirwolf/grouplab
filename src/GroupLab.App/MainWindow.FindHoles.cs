using Avalonia.Controls;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Marking;

namespace GroupLab.App;

/// <summary>
/// Find holes on the desktop, NOTES-FROM-PLANNING.md entry 318 section 2: on a target GroupLab did not print, once the scale is set by hand
/// (and the bulls, if they are placed), a button under the scale proposes the holes. It is always offered here, and always labelled
/// Experimental. Each proposal is a normal mark to confirm, move or remove; one the finder is unsure of is in the review with its reason.
/// </summary>
public sealed partial class MainWindow
{
    /// <summary>The button and what it does, under the scale.</summary>
    private Control FindHolesRow()
    {
        var button = Button(FindHoles.Label, FindHolesAsync);
        Avalonia.Automation.AutomationProperties.SetName(button, FindHoles.Label);
        ToolTip.SetTip(button, FindHoles.Explanation);
        return new StackPanel { Spacing = Tokens.Space4, Name = "FindHoles", Children = { Row(button), Line(FindHoles.Explanation) } };
    }

    /// <summary>Whether the button is offered: a picture open whose scale was set by hand.</summary>
    internal bool FindHolesOffered => valueImage is not null && FindHoles.Offered(session.State);

    /// <summary>Finds the holes away from the screen's thread, then places them as one step that Undo takes back.</summary>
    internal async Task FindHolesAsync()
    {
        if (valueImage is not { } value || !FindHoles.Offered(session.State))
        {
            status.Text = "Set the scale first: Find holes needs to know how big an inch is on this picture.";
            return;
        }

        var state = session.State;
        status.Text = "Finding holes, experimental…";
        AnyTargetFinding finding;
        try
        {
            finding = await Task.Run(() => FindHoles.Run(value, state, new OpenCvSharpBackend()));
        }
        catch (Exception ex) when (ex is InvalidOperationException or OpenCvSharp.OpenCVException)
        {
            status.Text = "Find holes could not read this picture. Mark the holes by hand.";
            DiagnosticLog.Exception(LogLevel.Warn, "marking.findholes", ex);
            return;
        }

        ShowProposals(finding);
    }

    /// <summary>Finds and places the holes on the calling thread, for the headless tests.</summary>
    internal void FindHolesForTests() => ShowProposals(FindHoles.Run(valueImage!, session.State, new OpenCvSharpBackend()));

    private void ShowProposals(AnyTargetFinding finding)
    {
        int placed = session.ProposeHoles(finding.Holes);
        int doubted = session.State.Shots.Count(s => ReviewQueue.StillDoubted(session.State, s));
        status.Text = FindHoles.Said(placed, doubted);
        DiagnosticLog.Info("marking.findholes", ("proposed", finding.Holes.Count), ("placed", placed), ("doubted", doubted));
    }
}

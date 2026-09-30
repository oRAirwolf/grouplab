using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 291 section 2.2: correcting the holes GroupLab found, on a page of its own, built on Marking A (entries 278
/// and 279). Alan: "There is no option to zoom into the photo and accidentally touching one of the holes moves it and screws up everything."
/// So the result's picture is for looking only, and here the picture pans and pinches under a crosshair fixed at the middle, and nothing
/// changes until a button is pressed: Add a hole where the crosshair is, or with a hole under it, Move it (pick it up, bring the crosshair
/// to where it belongs, put it down) or Remove it. Every change can be undone. Done takes the changes back to the result, which measures
/// again; leaving any other way, by Back or Android's back, asks whether to keep them.
/// </summary>
internal sealed class FixHolesPage : UserControl
{
    /// <summary>How near the crosshair, in screen units, a hole must be to be the one the buttons act on: Marking A's reach.</summary>
    internal const double Reach = 18;

    private readonly MarkingSession session;
    private readonly MarkingAPage.Viewer viewer;
    private readonly Action<MarkingState?> finished;
    private readonly TextBlock words = Screens.Line("");
    private readonly Button main = Screens.Primary("", () => { }).Id("fix-main");
    private readonly Button move = Screens.Choice("Move this hole", () => { }).Id("fix-move");
    private readonly Button remove = Screens.Choice("Remove this hole", () => { }).Id("fix-remove");
    private readonly Button undo = Screens.Choice("Undo", () => { }).Id("fix-undo");
    private readonly Button done = Screens.Primary("Done", () => { }).Id("fix-done");
    private readonly Border ask;
    private readonly TextBlock trueSize = Screens.Dim("");
    private readonly UnitSettings units;
    private int? under;

    /// <param name="state">The result's marking; it is copied, so nothing reaches the result until Done or a yes to keeping the changes.</param>
    /// <param name="turns">The quarter turns the result shows the picture upright by.</param>
    /// <param name="finished">Called once with the corrected marking, or null where the changes were thrown away or none were made.</param>
    /// <param name="units">The person's units, for how far a hole being moved has gone.</param>
    public FixHolesPage(MarkingState state, int turns, Action<MarkingState?> finished, UnitSettings? units = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        WorkInProgress.HoldWhileShown(this);
        this.finished = finished;
        this.units = units ?? UnitSettings.Imperial;
        session = new MarkingSession(state);
        viewer = new MarkingAPage.Viewer(new Bitmap(state.ImagePath!), session, [], turns)
        {
            Height = 420,
            RingInches = state.Calibre?.DiameterInches,
        };
        viewer.Moved += Show;
        main.Click += (_, _) => Press();
        move.Click += (_, _) => Move();
        remove.Click += (_, _) => Remove();
        undo.Click += (_, _) => Undo();
        done.Click += (_, _) => Finish(session.State);

        ask = Screens.Card(
            Screens.Heading("Keep the changes you made?"),
            Screens.Choice("Keep them", () => Finish(session.State)).Id("fix-keep"),
            Screens.Choice("Throw them away", () => Finish(null)).Id("fix-throw-away"),
            Screens.Choice("Go on fixing", GoOn).Id("fix-go-on"));
        ask.IsVisible = false;

        var pair = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = 8, Children = { move, remove } };
        Grid.SetColumn(remove, 1);
        var closing = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = 8, Children = { undo, done } };
        Grid.SetColumn(done, 1);
        var column = new StackPanel { Spacing = 10, Margin = new Thickness(16, 12, 16, 12) };
        column.Children.Add(Screens.Title("Fix holes"));
        column.Children.Add(words);
        column.Children.Add(viewer);
        column.Children.Add(trueSize);
        column.Children.Add(main);
        column.Children.Add(pair);
        column.Children.Add(closing);
        column.Children.Add(ask);
        column.Children.Add(Screens.Choice("Back to the result", Leave).Id("fix-back"));
        Content = new ScrollViewer { Content = column };

        // The picture takes most of the screen, the buttons under it within a thumb's reach.
        SizeChanged += (_, e) => viewer.Height = Math.Max(300, e.NewSize.Height * 0.55);
        AttachedToVisualTree += (_, _) => Shell.BackOverride = BackPressed;
        DetachedFromVisualTree += (_, _) =>
        {
            if (Shell.BackOverride == BackPressed)
            {
                Shell.BackOverride = null;
            }
        };
        Show();
    }

    /// <summary>Whether anything has been changed since the page opened.</summary>
    internal bool Changed => session.CanUndo;

    /// <summary>The marking as it stands on this page.</summary>
    internal MarkingState State => session.State;

    /// <summary>The picture, for a test to pan and pinch.</summary>
    internal MarkingAPage.Viewer Picture => viewer;

    /// <summary>Whether the question about keeping the changes is showing.</summary>
    internal bool Asking => ask.IsVisible;

    private int Shots => session.State.Shots.Count(s => s.IsShot);

    private void Show()
    {
        under = viewer.Held is null ? viewer.MarkUnderCrosshair(Reach) : null;
        // Entry 309 section 3.1: the circles are the bullet's size where the caliber is known; where it is not, a line says what would make them so.
        trueSize.Text = viewer.RingInches is null ? "Set the caliber on the result and each circle is drawn at the bullet's true size." : "";
        trueSize.IsVisible = trueSize.Text.Length > 0;
        if (viewer.Held is { } held)
        {
            // Entry 309 section 3.3: the circle is dragged itself, the crosshair on its center, with how far it has gone.
            string gone = viewer.HeldMovedInches is { } inches ? $" Moved {units.Length(inches)}." : "";
            words.Text = $"Drag hole {Number(held)}'s circle with your finger until it sits on the hole's edge, then put it there.{gone}";
            main.Content = Label("Put the hole here");
            move.Content = Label("Cancel");
            move.IsEnabled = true;
            remove.IsEnabled = false;
        }
        else
        {
            words.Text = under is { } id
                ? $"The crosshair is on hole {Number(id)}. Move it or remove it; to add a hole beside it, bring the crosshair a little away."
                : "Pinch to zoom and drag the picture to put the crosshair on a hole. Add a hole GroupLab missed, or put the crosshair on a ring to move or remove it.";
            main.Content = Label(under is null ? $"Add a hole here ({Shots + 1})" : "Add a hole here");
            move.Content = Label("Move this hole");
            move.IsEnabled = under is not null;
            remove.IsEnabled = under is not null;
        }

        main.IsEnabled = viewer.Held is not null || under is null;
        undo.IsEnabled = session.CanUndo;
        viewer.InvalidateVisual();
    }

    private static TextBlock Label(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center };

    private int Number(int id) => session.State.Shots.Where(s => s.IsShot).Select((s, i) => (s.Id, Number: i + 1)).FirstOrDefault(s => s.Id == id).Number;

    /// <summary>Adds a hole under the crosshair, or puts down the hole being moved there.</summary>
    internal void Press()
    {
        if (viewer.Held is { } held)
        {
            var to = viewer.Centre;
            viewer.Held = null;
            session.MoveShot(held, to);
        }
        else if (under is null)
        {
            session.AddShot(viewer.Centre);
        }

        Show();
    }

    /// <summary>Picks up the hole under the crosshair, or puts back the one picked up where it was.</summary>
    internal void Move()
    {
        viewer.Held = viewer.Held is null ? under : null;
        Show();
    }

    /// <summary>Removes the hole under the crosshair.</summary>
    internal void Remove()
    {
        if (viewer.Held is null && under is { } id)
        {
            session.DeleteShot(id);
        }

        Show();
    }

    internal void Undo()
    {
        viewer.Held = null;
        session.Undo();
        Show();
    }

    /// <summary>Back, from the page's own button or Android's: straight back where nothing changed, and otherwise the question.</summary>
    internal void Leave()
    {
        if (!Changed)
        {
            Finish(null);
            return;
        }

        viewer.Held = null;
        ask.IsVisible = true;
        ask.BringIntoView();
    }

    private void GoOn()
    {
        ask.IsVisible = false;
        Show();
    }

    private bool BackPressed()
    {
        Leave();
        return true;
    }

    private void Finish(MarkingState? kept)
    {
        if (Shell.BackOverride == BackPressed)
        {
            Shell.BackOverride = null;
        }

        DiagnosticLog.Info("result.fixholes", ("kept", kept is not null && Changed), ("shots", Shots));
        finished(kept is not null && Changed ? kept : null);
    }
}

using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using Button = Avalonia.Controls.Button;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 219 item A4: what a photograph came to, on the phone. The group's figures in the person's own units, the
/// desktop's composite plot filled the same way from the same marking, and the photograph with every hole GroupLab found, corrected by
/// touch (entry 199: 48 dp targets and a magnifier under the finger): move a hole, add one it missed, remove one that is not a shot, and
/// undo. Every change is saved at once. Where the sheet could not be read, the reason and what to do next, never a blank screen.
/// </summary>
public sealed class ResultView : UserControl
{
    private readonly MarkingSession session;
    private readonly TargetDefinition? definition;
    private UnitSettings units;
    private readonly CompositePlot plot = new() { Height = 360, HorizontalAlignment = HorizontalAlignment.Stretch };

    /// <summary>Entry 259 screen 1: the tiles, the plot with its chips, and the sections that open, from the shared figures.</summary>
    private readonly FiguresView full;
    private readonly TextBlock saved = Screens.Line("");
    private readonly Button undo = new() { Content = "Undo", MinHeight = Screens.Touch, Margin = new Thickness(4), IsEnabled = false };
    private SheetEditor? editor;
    private long? sessionId;
    private readonly Action again;

    internal ResultView(PhoneResult result, ShotSetup setup, UnitSettings units, Action again)
    {
        // Entry 273: a tap on any number switches units everywhere; this result shows them again.
        void Follow() => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            this.units = Phone.Settings.LoadUnits();
            Refresh();
        });
        AttachedToVisualTree += (_, _) => Shell.UnitsChanged += Follow;
        DetachedFromVisualTree += (_, _) => Shell.UnitsChanged -= Follow;

        this.units = units;
        this.again = again;
        definition = result.Definition;
        sessionId = result.SessionId;
        session = new MarkingSession(result.State);
        full = new FiguresView(result.State, units, plot, ShowShotsToZero) { Definition = result.Definition };
        var column = new StackPanel { Spacing = 12 };
        column.Children.Add(Screens.Title(result.Definition?.Name ?? "The sheet"));
        if (result.Failure is { } failure)
        {
            column.Children.Add(Screens.Line(failure));
            if (result.AskWhichSheet && result.Image is { } working)
            {
                if (result.LooksLike is { } likely)
                {
                    // Entry 281: the sheet the picture looks most like, by its markers and its drawing, to confirm with one press.
                    column.Children.Add(Screens.Line($"It looks like {likely.Name}."));
                    column.Children.Add(Screens.Primary("Yes, measure it as that sheet", () => _ = AsSheet(working, likely, setup, again)));
                }

                // Entry 279 section 2: a target GroupLab did not print is marked by hand, Marking A.
                var failed = Screens.Page(column);
                column.Children.Add(Screens.Choice("Not a GroupLab sheet: mark it by hand", () => Content = new MarkingAPage(working.Path, working.Metadata.Orientation, setup, units,
                    marked => Content = new ResultView(marked, setup, units, again), () => Content = failed)));
                column.Children.Add(Screens.Line(result.LooksLike is null ? "Which sheet is it?" : "Or another sheet:"));
                foreach (var sheet in PhoneAnalysis.Library().OrderBy(d => d.Name, StringComparer.CurrentCultureIgnoreCase))
                {
                    column.Children.Add(Screens.Choice(sheet.Name, () => _ = AsSheet(working, sheet, setup, again)));
                }
            }

            column.Children.Add(Screens.Choice("Try another picture", () =>
            {
                PhoneAnalysis.Discard(result.Image);
                again();
            }));
            Content = Screens.Page(column);
            return;
        }

        // Entry 243 section 3.3: three parts, the numbers, the sheet and what to do next, which a phone shows one under the other in the
        // order it always had, and a big screen in landscape, at least ExpandedWidth wide, side by side: the sheet on the left, the numbers
        // and what to do next beside it. Entry 246 found the tablet in portrait, 924 wide, going side by side too, which the entry did not
        // ask for, and the phone's order changed; both are as they were now.
        var numbers = new StackPanel { Spacing = 12 };
        var picture = new StackPanel { Spacing = 12 };
        var actions = new StackPanel { Spacing = 12 };
        numbers.Children.Add(full);
        // Entry 280 section 2, board MultiAim: on a target GroupLab did not print, a chip per aim point in its own color, its own figures,
        // and "+ Aim point"; the figures above stay the pooled ones.
        if (AimPoints(result, setup, again) is { } aims)
        {
            numbers.Children.Add(aims);
        }

        // Entry 273: the one-time hint card, until a number has been tapped once.
        if (!Phone.Settings.LoadUnitTapped())
        {
            numbers.Children.Add(Screens.Card(Screens.Line(GroupLab.Core.Marking.UnitSwitch.Hint + "."), Screens.Dim(GroupLab.Core.Marking.UnitSwitch.HintMore)));
        }
        // Entry 271: what the figures are measured in, and on a photograph the ruler that makes them real inches.
        if (PrinterCard.For(result, session, Changed) is { } printer)
        {
            numbers.Children.Add(printer);
        }

        if (result.State.ImagePath is { } path && File.Exists(path))
        {
            editor = new SheetEditor(new Bitmap(path), result.State.ViewQuarterTurns, () => session.State.Shots.Where(s => s.IsShot).ToList(), Edited(session),
                definition is null && AimedByHand(result.State) ? shot => AimColour(session.State, shot.Bull) : null);
            var tools = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center };
            var toolWords = Screens.Line(SheetEditor.Say(SheetEditor.Tool.Move));
            foreach (var (tool, words) in new[] { (SheetEditor.Tool.Move, "Move"), (SheetEditor.Tool.Add, "Add a hole"), (SheetEditor.Tool.Remove, "Remove") })
            {
                var b = new Button { Content = words, MinHeight = Screens.Touch, Margin = new Thickness(4) };
                b.Click += (_, _) =>
                {
                    editor.Mode = tool;
                    toolWords.Text = SheetEditor.Say(tool);
                };
                tools.Children.Add(b);
            }

            undo.Click += (_, _) =>
            {
                session.Undo();
                Changed();
            };
            tools.Children.Add(undo);
            picture.Children.Add(tools);
            picture.Children.Add(toolWords);
            picture.Children.Add(new LayoutTransformControl { LayoutTransform = new RotateTransform(90 * result.State.ViewQuarterTurns), Child = editor });
        }

        // Entry 259 screen 6: a sheet of a set from Made for your optic leads to the set, pooled so far, and the sheets still to read.
        if (session.State.SetSheet is not null && definition?.Tiling is { } set && set.Cols * set.Rows > 1)
        {
            actions.Children.Insert(0, Screens.Row("Your set", "The sheets read so far pooled into one group, and those still to read", ShowSet));
        }

        // Entry 259 screen 5: the hit chance with this group carried in.
        actions.Children.Add(Screens.Row("Ballistics", "The dope, and the chance of a hit with this group", () => Shell.Current?.ShowBallistics(session.State)));

        // Entry 259 screen 2: which bulls were fired at, so each shot is measured from its own.
        if (session.State.Bulls.Count(b => b.Scoring) > 1)
        {
            actions.Children.Add(Screens.Row("Bulls you fired at", AimedBulls.Says(session.State.Rule, session.State.Bulls), ShowBulls));
        }

        // Entry 280 section 2, Shots A: every shot's offset and clicks, and which count.
        actions.Children.Add(Screens.Row("Shots", "Each shot's offset and clicks, and which count", () =>
        {
            var result = Content;
            Content = new ShotsPage(session, definition, units, Changed, () =>
            {
                Content = result;
                Refresh();
            });
        }));

        // Entry 280 section 2, board ZeroFrom: the zero from this group, and on to Shots Needed to Zero.
        actions.Children.Add(Screens.Row("Zero from this group", "Where the group sits, the clicks, and how sure", () =>
        {
            var result = Content;
            Content = new ZeroFromPage(session.State, units, ShowShotsToZero, () => Content = result);
        }));

        // Entry 279 section 3 and entry 281 section 2: Unholy's "Fudd buster mode", from twenty shots.
        if (FuddBusterPage.Shots(session.State).Count >= GroupLab.Core.Statistics.FuddBuster.LeastShots)
        {
            actions.Children.Add(Screens.Row(GroupLab.Core.Statistics.FuddBusterWords.Title, "Why a few shots mislead, shown with your own", () =>
            {
                var result = Content;
                Content = new FuddBusterPage(session.State, units, () => Content = result);
            }));
        }

        // Entry 281 section 2: the phone follows A, saved by itself, and says so from the start, not only after a change.
        saved.Text = SavedWords(sessionId);
        actions.Children.Add(saved);
        var shareSaid = Screens.Line("");
        actions.Children.Add(Screens.Choice("Share this session", () => shareSaid.Text = SessionFiles.Share(session.State, definition, units) ?? ""));
        actions.Children.Add(Screens.Choice("Share the shots as CSV", () => shareSaid.Text = SessionFiles.ShareCsv(session.State, definition) ?? ""));
        actions.Children.Add(shareSaid);
        actions.Children.Add(Screens.Choice("Another target", again));
        Refresh();

        var host = new Grid { Margin = new Thickness(16) };
        var left = new StackPanel { Spacing = 12 };
        var right = new StackPanel { Spacing = 12 };
        Grid.SetColumn(right, 2);
        bool? wide = null;
        void Arrange(Size size)
        {
            bool now = size.Width >= ExpandedWidth && size.Width > size.Height && picture.Children.Count > 0;
            if (wide == now)
            {
                return;
            }

            wide = now;
            host.Children.Clear();
            host.ColumnDefinitions.Clear();
            left.Children.Clear();
            right.Children.Clear();
            foreach (var part in new Control[] { numbers, picture, actions })
            {
                column.Children.Remove(part);
            }

            if (now)
            {
                host.MaxWidth = double.PositiveInfinity;
                host.ColumnDefinitions = new ColumnDefinitions("3*,24,2*");
                left.Children.Add(column);
                left.Children.Add(picture);
                right.Children.Add(numbers);
                right.Children.Add(actions);
                host.Children.Add(left);
                host.Children.Add(right);
            }
            else
            {
                host.MaxWidth = 640;
                column.Children.Add(numbers);
                column.Children.Add(picture);
                column.Children.Add(actions);
                host.Children.Add(column);
            }
        }

        SizeChanged += (_, e) => Arrange(e.NewSize);
        Arrange(Bounds.Size);
        // The explanation sheet lies over the page at its bottom edge, and closes with its own button.
        Content = new Grid
        {
            Children =
            {
                new ScrollViewer { Content = host, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled },
                full.Sheet,
            },
        };
    }

    /// <summary>Entry 259 screen 6: the set this sheet belongs to, as a checklist; photographing the next sheet goes to the camera.</summary>
    private void ShowSet()
    {
        var result = Content;
        string? date = sessionId is { } id ? PhoneAnalysis.Store().Get(id)?.ShotDate : null;
        Content = new SetPage(definition!, date, units, again, () => Content = result);
    }

    /// <summary>Entry 259 screen 2: the bulls fired at, chosen on the sheet's own layout, and back to the result.</summary>
    private void ShowBulls()
    {
        var result = Content;
        Content = new BullsPage(session, Changed, () => Content = result);
    }

    /// <summary>Entry 259 screen 3: Shots Needed to Zero as its own page, from this result, and back to it.</summary>
    private void ShowShotsToZero()
    {
        var result = Content;
        Content = new ShotsToZeroPage(session.State, units, () => Content = result);
    }

    /// <summary>The width from which the sheet and the numbers sit side by side in landscape, Material's expanded window class, entry 243 section 3.3.</summary>
    internal const double ExpandedWidth = 840;

    private Action<Action<MarkingSession>> Edited(MarkingSession s) => change =>
    {
        change(s);
        Changed();
    };

    /// <summary>Whether the aim points were placed by hand on a target GroupLab did not print, where each has a color.</summary>
    internal static bool AimedByHand(MarkingState state) =>
        state.Bulls.Count > 0 && state.Scale is not GroupLab.Core.Marking.SheetReference && state.Bulls.All(b => b.Declared is null);

    /// <summary>An aim point's color, the desktop's own (Tokens.BullMarks), by its place among the aim points.</summary>
    internal static IBrush AimColour(MarkingState state, int? bull)
    {
        int at = bull is { } b ? state.Bulls.FindIndex(x => x.Index == b) : -1;
        var marks = GroupLab.App.Theme.Tokens.BullMarks;
        return at < 0 ? Brushes.OrangeRed : new SolidColorBrush(marks[at % marks.Count]);
    }

    private Control? AimPoints(PhoneResult result, ShotSetup setup, Action again)
    {
        var state = session.State;
        if (definition is not null || !AimedByHand(state) || result.State.ImagePath is not { } path)
        {
            return null;
        }

        var figures = GroupAnalysis.ByAimPoint(state);
        var said = Screens.Line("Tap an aim point for its own figures.");
        var chips = new WrapPanel();
        foreach (var (aim, own) in figures)
        {
            var dot = new Avalonia.Controls.Shapes.Ellipse { Width = 12, Height = 12, Fill = AimColour(state, aim.Index), Margin = new Thickness(0, 0, 6, 0) };
            var chip = new Button { Content = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Children = { dot, new TextBlock { Text = "Aim " + aim.Label } } }, MinHeight = Screens.Touch, Margin = new Thickness(0, 0, 6, 6) };
            chip.Click += (_, _) => said.Text = own is null
                ? $"Aim {aim.Label}: no shots yet."
                : $"Aim {aim.Label}: {own.Shots} shots"
                  + (own.MeanRadius is { } mr ? ", mean radius " + units.Length(mr.Value) : "")
                  + (own.ExtremeSpread is { } es ? ", extreme spread " + units.Length(es.Value) : "")
                  + (own.CentreFromAim is { } c ? ", center " + units.Length(Math.Sqrt((c.X * c.X) + (c.Y * c.Y))) + " from its aim" : "") + ".";
            chips.Children.Add(chip);
        }

        var add = Screens.Choice("+ Aim point", () =>
        {
            var here = Content;
            Content = new MarkingAPage(path, session.State.ExifOrientation, setup, units,
                marked => Content = new ResultView(marked, setup, units, again), () => Content = here, session.State, sessionId);
        });
        return Screens.Card(Screens.Heading("Aim points"), chips, said, add,
            Screens.Dim("The figures above pool every aim point's shots, each measured from its own aim point."));
    }

    /// <summary>Where the result is kept and that it is safe to close, entry 279 section 3 (Unholy) and entry 281 section 2 (A).</summary>
    private static string SavedWords(long? id) => id is null
        ? "This session could not be saved on the phone."
        : "Saved in Sessions on this phone, and every change as you make it: safe to close.";

    /// <summary>After a change: the figures and the plot again, and the session saved over itself.</summary>
    private void Changed()
    {
        sessionId = PhoneAnalysis.Save(session.State, definition, units, sessionId) ?? sessionId;
        Refresh();
    }

    private void Refresh()
    {
        var state = session.State;
        var labels = ShotLabels.For(state);
        string Label(int id) => labels.FirstOrDefault(l => l.ShotId == id) is { Text: { } text } ? text : id.ToString(CultureInfo.InvariantCulture);
        string Bull(int index) => state.Bulls.FirstOrDefault(b => b.Index == index)?.Label ?? index.ToString(CultureInfo.InvariantCulture);
        plot.Show(state, definition, units, Label, Bull);
        full.Show(state, units);
        undo.IsEnabled = session.UndoWords is not null;
        saved.Text = SavedWords(sessionId);
        editor?.InvalidateVisual();
    }

    private async Task AsSheet(WorkingImage working, TargetDefinition sheet, ShotSetup setup, Action again)
    {
        using var cancel = new CancellationTokenSource();
        var (page, line, stop) = Screens.Progress($"Reading the sheet as {sheet.Name}");
        stop.Click += (_, _) =>
        {
            cancel.Cancel();
            line.Text = "Canceling…";
        };
        Content = page;
        try
        {
            var result = await Task.Run(() => PhoneAnalysis.Detect(working, sheet, setup, units, Phone.Survey, cancel.Token, words => Dispatcher.UIThread.Post(() => line.Text = words)));
            Dispatcher.UIThread.Post(() => Content = new ResultView(result, setup, units, again));
        }
        catch (OperationCanceledException)
        {
            // Entry 243 section 3.2: canceled, so the working copy is forgotten and the person is back where they started.
            GroupLab.App.Diagnostics.DiagnosticLog.Info("phone.detect.cancel");
            PhoneAnalysis.Forget(working);
            Dispatcher.UIThread.Post(again);
        }
    }

    /// <summary>
    /// Entry 246, look B: the figures as tiles, in the person's own units with the angle beneath where the distance is known: mean radius,
    /// the one that answers the question, extreme spread, the shots and the group's center from the aim. None where there is no group.
    /// </summary>
    internal static IReadOnlyList<(string Label, string Value, string Under, bool Headline)> Tiles(GroupFigures? figures, UnitSettings units, double? distanceInches)
    {
        if (figures is null || figures.Shots == 0 || figures.MeanRadius is null)
        {
            return [];
        }

        string Angle(double inches) => units.Angle(inches, distanceInches) is { } angle ? $"{angle.ToString("0.00", CultureInfo.CurrentCulture)} {UnitSettings.Symbol(units.Angular)}" : "";
        var shown = new List<(string, string, string, bool)> { ("Mean radius", units.Length(figures.MeanRadius.Value), Angle(figures.MeanRadius.Value), true) };
        if (figures.ExtremeSpread is { } spread)
        {
            shown.Add(("Extreme spread", units.Length(spread.Value), Angle(spread.Value) is { Length: > 0 } a ? a : "center to center", false));
        }

        shown.Add(("Shots", figures.Shots.ToString(CultureInfo.CurrentCulture), "on the sheet", false));
        if (figures.CentreFromAim is { } centre)
        {
            double off = Math.Sqrt((centre.X * centre.X) + (centre.Y * centre.Y));
            shown.Add(("Center from aim", units.Length(off), Angle(off) is { Length: > 0 } a ? a : "from the bulls' centers", false));
        }

        return shown;
    }

    /// <summary>The group in one or two sentences: how many shots, the extreme spread and the mean radius, with the angle where the distance is known.</summary>
    internal static string Figures(GroupFigures? figures, UnitSettings units, double? distanceInches)
    {
        if (figures is null || figures.Shots == 0)
        {
            return "No holes were found on this sheet. Add them by touch below, or take the picture again closer.";
        }

        string Size(double inches) => units.Angle(inches, distanceInches) is { } angle
            ? $"{units.Length(inches)} ({angle.ToString("0.00", CultureInfo.CurrentCulture)} {UnitSettings.Symbol(units.Angular)})"
            : units.Length(inches);
        string said = figures.Shots == 1 ? "1 shot." : $"{figures.Shots} shots.";
        if (figures.ExtremeSpread is { } spread)
        {
            said += $" Extreme spread {Size(spread.Value)}, center to center.";
        }

        if (figures.MeanRadius is { } radius)
        {
            said += $" Mean radius {Size(radius.Value)}.";
        }

        return said + (distanceInches is null ? " Enter the distance on Capture to see the angles." : "");
    }

    /// <summary>
    /// The working image fitted to the width, a ring on every hole, and the holes corrected by touch. A touch within 24 dp of a hole is on
    /// it. While a hole is dragged, a magnifier in the corner away from the finger shows three times the area under it, with a cross at the
    /// point the hole will go, because a finger hides exactly the part that matters.
    /// <para>
    /// Entry 281 section 1.7: the picture was stretched to whatever box the page gave it, and shown as the camera's sensor stores it, on its
    /// side for a picture taken upright. It is now scaled alike across and down, never filled, and turned as the marking's view turns
    /// (<see cref="ViewRotation"/>), with every ring and the magnifier turned with it; the positions themselves stay in the stored pixels.
    /// </para>
    /// </summary>
    private sealed class SheetEditor(Bitmap image, int turns, Func<IReadOnlyList<MarkedShot>> shots, Action<Action<MarkingSession>> edit, Func<MarkedShot, IBrush?>? colour = null) : Control
    {
        internal enum Tool
        {
            Move,
            Add,
            Remove,
        }

        private const double Reach = 24;
        private const double Loupe = 140;
        private const double Magnify = 3;
        private int? dragging;
        private Point? finger;

        public Tool Mode { get; set; } = Tool.Move;

        internal static string Say(Tool tool) => tool switch
        {
            Tool.Add => "Tap where a hole is that GroupLab missed.",
            Tool.Remove => "Tap a ring that is not a shot to remove it.",
            _ => "Drag a ring to the center of its hole.",
        };

        private double PixelWidth => image.PixelSize.Width;

        private double PixelHeight => image.PixelSize.Height;

        /// <summary>The picture's size as it is shown, turned.</summary>
        private (double Width, double Height) Shown => ViewRotation.DisplaySize(turns, PixelWidth, PixelHeight);

        /// <summary>Screen units per image pixel, the same across and down so the picture keeps its shape.</summary>
        private double Scale => Math.Min(Bounds.Width / Shown.Width, Bounds.Height / Shown.Height);

        protected override Size MeasureOverride(Size available)
        {
            double width = double.IsFinite(available.Width) ? available.Width : Shown.Width;
            return new Size(width, width * Shown.Height / Shown.Width);
        }

        private PointD ToImage(Point p) => ViewRotation.ToImage(new PointD(p.X / Scale, p.Y / Scale), turns, PixelWidth, PixelHeight);

        private Point ToScreen(PointD image)
        {
            var shown = ViewRotation.ToDisplay(image, turns, PixelWidth, PixelHeight);
            return new Point(shown.X * Scale, shown.Y * Scale);
        }

        /// <summary>Stored pixels to the screen: the turn, then <paramref name="k"/> screen units a pixel, then a shift.</summary>
        private Matrix Turned(double k, double shiftX, double shiftY)
        {
            var (a, b, c, d, e, f) = ViewRotation.Affine(turns, PixelWidth, PixelHeight);
            return new Matrix(a * k, d * k, b * k, e * k, (c * k) + shiftX, (f * k) + shiftY);
        }

        private int? Nearest(Point p)
        {
            var near = shots().Select(s => (s.Id, Distance: Point.Distance(ToScreen(s.Image), p)))
                .Where(s => s.Distance <= Reach).OrderBy(s => s.Distance).ToList();
            return near.Count > 0 ? near[0].Id : null;
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            var p = e.GetPosition(this);
            switch (Mode)
            {
                case Tool.Add:
                    edit(s => s.AddShot(ToImage(p)));
                    break;
                case Tool.Remove when Nearest(p) is { } gone:
                    edit(s => s.DeleteShot(gone));
                    break;
                case Tool.Move when Nearest(p) is { } held:
                    dragging = held;
                    finger = p;
                    e.Pointer.Capture(this);
                    InvalidateVisual();
                    break;
            }

            e.Handled = true;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (dragging is not null)
            {
                finger = e.GetPosition(this);
                InvalidateVisual();
                e.Handled = true;
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (dragging is { } id && finger is { } at)
            {
                dragging = null;
                finger = null;
                e.Pointer.Capture(null);
                edit(s => s.MoveShot(id, ToImage(at)));
                e.Handled = true;
            }
        }

        public override void Render(DrawingContext context)
        {
            var whole = new Rect(0, 0, PixelWidth, PixelHeight);
            using (context.PushTransform(Turned(Scale, 0, 0)))
            {
                context.DrawImage(image, new Rect(image.Size), whole);
            }

            var pen = new Pen(Brushes.OrangeRed, 2);
            // Entry 280 section 2, Shots A: a shot left out is dashed on the picture, still there.
            var leftOut = new Pen(Brushes.OrangeRed, 2, new DashStyle([2, 2], 0));
            foreach (var shot in shots())
            {
                var at = shot.Id == dragging && finger is { } f ? f : ToScreen(shot.Image);
                var ring = colour?.Invoke(shot) is { } brush ? new Pen(brush, 2, shot.Exclusion is null ? null : new DashStyle([2, 2], 0)) : shot.Exclusion is null ? pen : leftOut;
                context.DrawEllipse(null, ring, at, 9, 9);
            }

            if (finger is not { } held)
            {
                return;
            }

            // The loupe: the corner away from the finger, three times the area under it, and a cross where the hole will go.
            bool left = held.X > Bounds.Width / 2;
            var box = new Rect(left ? 8 : Bounds.Width - Loupe - 8, 8, Loupe, Loupe);
            // The magnifier turned as the picture is, the point under the finger at its center.
            double k = Scale * Magnify;
            var under = ViewRotation.ToDisplay(ToImage(held), turns, PixelWidth, PixelHeight);
            using (context.PushClip(box))
            using (context.PushTransform(Turned(k, box.Center.X - (under.X * k), box.Center.Y - (under.Y * k))))
            {
                context.DrawImage(image, new Rect(image.Size), whole);
            }

            context.DrawRectangle(null, new Pen(Brushes.White, 2), box);
            var mid = box.Center;
            var cross = new Pen(Brushes.OrangeRed, 1.5);
            context.DrawLine(cross, new Point(mid.X - 12, mid.Y), new Point(mid.X + 12, mid.Y));
            context.DrawLine(cross, new Point(mid.X, mid.Y - 12), new Point(mid.X, mid.Y + 12));
        }
    }
}

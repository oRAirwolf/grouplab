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

namespace GroupLab.Android;

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
    private readonly UnitSettings units;
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
                column.Children.Add(Screens.Line("Which sheet is it?"));
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
        // Entry 271: what the figures are measured in, and on a photograph the ruler that makes them real inches.
        if (PrinterCard.For(result, session, Changed) is { } printer)
        {
            numbers.Children.Add(printer);
        }

        if (result.State.ImagePath is { } path && File.Exists(path))
        {
            editor = new SheetEditor(new Bitmap(path), () => session.State.Shots.Where(s => s.IsShot).ToList(), Edited(session));
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

        actions.Children.Add(saved);
        var shareSaid = Screens.Line("");
        actions.Children.Add(Screens.Choice("Share this session", () => shareSaid.Text = SessionFiles.Share(session.State, definition, units) ?? ""));
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
        saved.Text = sessionId is null ? "This session could not be saved on the phone." : "Saved in Sessions. Every change is saved as you make it.";
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
            var result = await Task.Run(() => PhoneAnalysis.Detect(working, sheet, setup, units, App.Survey, cancel.Token, words => Dispatcher.UIThread.Post(() => line.Text = words)));
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
    /// </summary>
    private sealed class SheetEditor(Bitmap image, Func<IReadOnlyList<MarkedShot>> shots, Action<Action<MarkingSession>> edit) : Control
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

        /// <summary>Screen units per image pixel, and image pixels to the bitmap's own units, which differ where the file carries a dpi.</summary>
        private double Scale => Bounds.Width / image.PixelSize.Width;

        private double Units => image.Size.Width / image.PixelSize.Width;

        protected override Size MeasureOverride(Size available)
        {
            double width = double.IsFinite(available.Width) ? available.Width : image.Size.Width;
            return new Size(width, width * image.PixelSize.Height / image.PixelSize.Width);
        }

        private PointD ToImage(Point p) => new(p.X / Scale, p.Y / Scale);

        private int? Nearest(Point p)
        {
            var near = shots().Select(s => (s.Id, Distance: Math.Sqrt(Math.Pow((s.Image.X * Scale) - p.X, 2) + Math.Pow((s.Image.Y * Scale) - p.Y, 2))))
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
            double scale = Scale;
            context.DrawImage(image, new Rect(0, 0, Bounds.Width, Bounds.Height));
            var pen = new Pen(Brushes.OrangeRed, 2);
            foreach (var shot in shots())
            {
                var at = shot.Id == dragging && finger is { } f ? f : new Point(shot.Image.X * scale, shot.Image.Y * scale);
                context.DrawEllipse(null, pen, at, 9, 9);
            }

            if (finger is not { } held)
            {
                return;
            }

            // The loupe: the corner away from the finger, three times the area under it, and a cross where the hole will go.
            bool left = held.X > Bounds.Width / 2;
            var box = new Rect(left ? 8 : Bounds.Width - Loupe - 8, 8, Loupe, Loupe);
            double half = Loupe / Magnify / 2 / scale;
            var centre = ToImage(held);
            var source = new Rect((centre.X - half) * Units, (centre.Y - half) * Units, 2 * half * Units, 2 * half * Units);
            using (context.PushClip(box))
            {
                context.DrawImage(image, source, box);
            }

            context.DrawRectangle(null, new Pen(Brushes.White, 2), box);
            var mid = box.Center;
            var cross = new Pen(Brushes.OrangeRed, 1.5);
            context.DrawLine(cross, new Point(mid.X - 12, mid.Y), new Point(mid.X + 12, mid.Y));
            context.DrawLine(cross, new Point(mid.X, mid.Y - 12), new Point(mid.X, mid.Y + 12));
        }
    }
}

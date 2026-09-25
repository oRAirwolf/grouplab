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
    private readonly TextBlock figures = Screens.Line("");
    private readonly CompositePlot plot = new() { Height = 360, HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly TextBlock saved = Screens.Line("");
    private readonly Button undo = new() { Content = "Undo", MinHeight = Screens.Touch, Margin = new Thickness(4), IsEnabled = false };
    private SheetEditor? editor;
    private long? sessionId;

    internal ResultView(PhoneResult result, ShotSetup setup, UnitSettings units, Action again)
    {
        this.units = units;
        definition = result.Definition;
        sessionId = result.SessionId;
        session = new MarkingSession(result.State);
        var column = new StackPanel { Spacing = 12 };
        column.Children.Add(Screens.Heading(result.Definition?.Name ?? "The sheet"));
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

        column.Children.Add(figures);
        column.Children.Add(plot);
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
            column.Children.Add(tools);
            column.Children.Add(toolWords);
            column.Children.Add(new LayoutTransformControl { LayoutTransform = new RotateTransform(90 * result.State.ViewQuarterTurns), Child = editor });
        }

        column.Children.Add(saved);
        column.Children.Add(Screens.Choice("Another target", again));
        Refresh();
        Content = Screens.Page(column);
    }

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
        figures.Text = Figures(GroupAnalysis.Analyse(state).AllShots, units, state.ShotDistanceInches);
        plot.Show(state, definition, units, Label, Bull);
        undo.IsEnabled = session.UndoWords is not null;
        saved.Text = sessionId is null ? "This session could not be saved on the phone." : "Saved in Sessions. Every change is saved as you make it.";
        editor?.InvalidateVisual();
    }

    private async Task AsSheet(WorkingImage working, TargetDefinition sheet, ShotSetup setup, Action again)
    {
        Content = Screens.Words("Reading the sheet", $"Registering and detecting as {sheet.Name}.");
        var result = await Task.Run(() => PhoneAnalysis.Detect(working, sheet, setup, units, App.Survey, CancellationToken.None));
        Dispatcher.UIThread.Post(() => Content = new ResultView(result, setup, units, again));
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

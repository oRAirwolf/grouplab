using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Android;

/// <summary>
/// Marking a target GroupLab did not print, on the phone, NOTES-FROM-PLANNING.md entry 279 section 2: Marking A as drawn on the phone parity
/// canvas, boards MarkA1 and MarkA2, and Alan's choice of entry 278 section 1. Three steps: the scale, the aim point, the holes. The picture
/// pans and pinches under a crosshair fixed at the middle of the screen, and one button sets the point under it and counts ("Set end 2 of
/// 2", "Add hole here (6)"), with Undo. Once the scale is known the crosshair's ring is the bullet's real size. Holes GroupLab found are
/// rings with their number; holes added here are rings with a dot; a wrong mark is removed by putting the crosshair on it, when the button
/// says so. "Done: measure N shots" makes the result; "Keep as a template" keeps the aim points for the next sheet of the same target.
/// </summary>
internal sealed class MarkingAPage : UserControl
{
    private enum Step
    {
        Scale,
        Aim,
        Holes,
    }

    /// <summary>How near the crosshair, in screen units, a mark must be for the button to offer to remove it.</summary>
    private const double Reach = 18;

    private readonly MarkingSession session = new();
    private readonly Viewer viewer;
    private readonly UnitSettings units;
    private readonly Action<PhoneResult> done;
    private readonly Action cancel;
    private readonly TextBlock title = Screens.Title("");
    private readonly TextBlock words = Screens.Line("");
    private readonly TextBox length = new() { MinHeight = Screens.Touch };
    private readonly Button main = Screens.Primary("", () => { });
    private readonly Button undo = Screens.Choice("Undo", () => { });
    private readonly Button next = Screens.Choice("", () => { });
    private readonly Button template = Screens.Choice("Keep as a template", () => { });
    private readonly List<PointD> ends = [];
    private Step step = Step.Scale;
    private readonly long? sessionId;

    /// <param name="existing">A marking to go on with, entry 280 section 2's "+ Aim point": its scale kept, starting at the aim points.</param>
    public MarkingAPage(string imagePath, int? exifOrientation, ShotSetup setup, UnitSettings units, Action<PhoneResult> done, Action cancel, MarkingState? existing = null, long? sessionId = null)
    {
        WorkInProgress.HoldWhileShown(this);
        this.sessionId = sessionId;
        this.units = units;
        this.done = done;
        this.cancel = cancel;
        if (existing is { Scale: not null })
        {
            session.Load(existing);
            step = Step.Aim;
        }
        else
        {
            session.Open(imagePath, exifOrientation);
            session.SetCalibre(setup.Calibre);
            session.SetShotDistance(setup.DistanceInches);
        }

        viewer = new Viewer(new Bitmap(imagePath), session, ends) { Height = 420 };
        viewer.Moved += Show;
        length.PlaceholderText = $"The length between the two ends, in {UnitSettings.Symbol(units.Linear)}";
        main.Click += (_, _) => Press();
        undo.Click += (_, _) => Undo();
        next.Click += (_, _) => Next();
        template.Click += (_, _) => KeepTemplate();

        var buttons = new StackPanel { Spacing = 8, Children = { main, new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = 8, Children = { undo, next } }, template } };
        Grid.SetColumn(next, 1);
        var column = new StackPanel { Spacing = 10, Margin = new Thickness(16, 12, 16, 12) };
        column.Children.Add(title);
        column.Children.Add(words);
        column.Children.Add(viewer);
        column.Children.Add(length);
        column.Children.Add(buttons);
        column.Children.Add(Screens.Choice("Cancel", cancel));
        Content = new ScrollViewer { Content = column };
        Show();
    }

    private int Shots => session.State.Shots.Count(s => s.IsShot);

    private void Show()
    {
        viewer.RingInches = step == Step.Holes ? session.State.Calibre?.DiameterInches : null;
        length.IsVisible = step == Step.Scale && ends.Count == 2;
        template.IsVisible = step == Step.Holes && session.State.Bulls.Count >= 2;
        undo.IsEnabled = step == Step.Scale ? ends.Count > 0 : session.CanUndo;
        switch (step)
        {
            case Step.Scale:
                title.Text = "1. The scale";
                words.Text = ends.Count < 2
                    ? "Put the crosshair on one end of a length you know, a ruler's mark or the target's printed size, and set it; then the other end."
                    : "Type how long it is, then go on to the aim point.";
                SetMain(ends.Count < 2 ? $"Set end {ends.Count + 1} of 2" : "Set end 2 again", ends.Count < 2);
                next.Content = Label("Next: the aim point");
                next.IsEnabled = ends.Count == 2;
                break;
            case Step.Aim:
                title.Text = "2. The aim point";
                words.Text = "Put the crosshair on the center of the bull you aimed at, and set it. A target with several bulls can have one for each.";
                SetMain(session.State.Bulls.Count == 0 ? "Set the aim point" : $"Add another aim point ({session.State.Bulls.Count + 1})", true);
                next.Content = Label("Next: the holes");
                next.IsEnabled = session.State.Bulls.Count > 0;
                break;
            default:
                title.Text = "3. The holes";
                words.Text = "Put the crosshair on the center of each hole and add it. To take one away, put the crosshair on it.";
                if (viewer.MarkUnderCrosshair(Reach) is { } near)
                {
                    SetMain("Remove this hole", true);
                    main.Tag = near;
                }
                else
                {
                    SetMain($"Add hole here ({Shots + 1})", true);
                    main.Tag = null;
                }

                next.Content = Label($"Done: measure {Shots} shots");
                next.IsEnabled = Shots >= 2;
                break;
        }

        viewer.InvalidateVisual();
    }

    private static TextBlock Label(string words) => new() { Text = words, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center };

    private void SetMain(string words, bool enabled)
    {
        main.Content = Label(words);
        main.IsEnabled = enabled;
    }

    private void Press()
    {
        var at = viewer.Centre;
        switch (step)
        {
            case Step.Scale:
                if (ends.Count == 2)
                {
                    ends.RemoveAt(1);
                }

                ends.Add(at);
                break;
            case Step.Aim:
                session.AddBull(at);
                break;
            default:
                if (main.Tag is int id)
                {
                    session.DeleteShot(id);
                }
                else
                {
                    session.AddShot(at);
                }

                break;
        }

        Show();
    }

    private void Undo()
    {
        if (step == Step.Scale)
        {
            if (ends.Count > 0)
            {
                ends.RemoveAt(ends.Count - 1);
            }
        }
        else if (session.CanUndo)
        {
            session.Undo();
        }

        Show();
    }

    private void Next()
    {
        switch (step)
        {
            case Step.Scale:
                if (!double.TryParse(length.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out double value) || value <= 0)
                {
                    words.Text = "Type the length between the two ends first, as a number.";
                    return;
                }

                double inches = UnitSettings.ToInches(value, units.Linear);
                session.SetScale(new LengthReference(ends[0], ends[1], inches));
                step = Step.Aim;
                break;
            case Step.Aim:
                step = Step.Holes;
                break;
            default:
                var state = session.State;
                long? id = PhoneAnalysis.Save(state, null, units, sessionId);
                DiagnosticLog.Info("marking.byhand", ("shots", Shots), ("aims", state.Bulls.Count));
                done(new PhoneResult(state, null, null, id));
                return;
        }

        Show();
    }

    private void KeepTemplate()
    {
        if (BullTemplate.From(string.Create(CultureInfo.CurrentCulture, $"{session.State.Bulls.Count} aim points, {DateTime.Now:d MMM yyyy}"), session.State) is { } kept)
        {
            App.Settings.SaveBullTemplate(kept);
            words.Text = "Kept: the next sheet of this target needs only its first two aim points set.";
        }
    }

    /// <summary>
    /// The picture under a fixed crosshair: one finger pans it, two pinch it about the crosshair. It draws the scale's ends, the aim
    /// points, and the holes, found ones as numbered rings and added ones as rings with a dot, and the crosshair with the bullet's ring.
    /// </summary>
    private sealed class Viewer : Control
    {
        private readonly Bitmap image;
        private readonly MarkingSession session;
        private readonly IReadOnlyList<PointD> ends;
        private double zoom;
        private Point offset;
        private Point? last;
        private double pinch = 1;

        public Viewer(Bitmap image, MarkingSession session, IReadOnlyList<PointD> ends)
        {
            this.image = image;
            this.session = session;
            this.ends = ends;
            ClipToBounds = true;
            GestureRecognizers.Add(new PinchGestureRecognizer());
            AddHandler(InputElement.PinchEvent, (_, e) =>
            {
                if (pinch > 0 && e.Scale > 0)
                {
                    ZoomAboutCentre(e.Scale / pinch);
                }

                pinch = e.Scale;
                e.Handled = true;
            });
            AddHandler(InputElement.PinchEndedEvent, (_, _) => pinch = 1);
        }

        public event Action? Moved;

        /// <summary>The bullet's diameter in inches, for the crosshair's ring, once the scale is known.</summary>
        public double? RingInches { get; set; }

        /// <summary>The image point under the crosshair.</summary>
        public PointD Centre => ToImage(new Point(Bounds.Width / 2, Bounds.Height / 2));

        private PointD ToImage(Point p) => new((p.X - offset.X) / zoom, (p.Y - offset.Y) / zoom);

        private Point ToScreen(PointD p) => new((p.X * zoom) + offset.X, (p.Y * zoom) + offset.Y);

        /// <summary>The hole under the crosshair, within <paramref name="reach"/> screen units, if any.</summary>
        public int? MarkUnderCrosshair(double reach)
        {
            var middle = new Point(Bounds.Width / 2, Bounds.Height / 2);
            return session.State.Shots.Where(s => s.IsShot).Select(s => (s.Id, Distance: Point.Distance(ToScreen(s.Image), middle)))
                .Where(s => s.Distance <= reach).OrderBy(s => s.Distance).Select(s => (int?)s.Id).FirstOrDefault();
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);
            if (zoom == 0 && Bounds.Width > 0)
            {
                // The whole picture in view to start with, centered.
                zoom = Math.Min(Bounds.Width / image.PixelSize.Width, Bounds.Height / image.PixelSize.Height);
                offset = new Point((Bounds.Width - (image.PixelSize.Width * zoom)) / 2, (Bounds.Height - (image.PixelSize.Height * zoom)) / 2);
                Moved?.Invoke();
            }
        }

        private void ZoomAboutCentre(double factor)
        {
            double fit = Math.Min(Bounds.Width / image.PixelSize.Width, Bounds.Height / image.PixelSize.Height);
            double to = Math.Clamp(zoom * factor, fit / 2, 8);
            var middle = new Point(Bounds.Width / 2, Bounds.Height / 2);
            var under = ToImage(middle);
            zoom = to;
            offset = new Point(middle.X - (under.X * zoom), middle.Y - (under.Y * zoom));
            InvalidateVisual();
            Moved?.Invoke();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            last = e.GetPosition(this);
            e.Handled = true;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (last is { } was && pinch == 1)
            {
                var now = e.GetPosition(this);
                offset += now - was;
                last = now;
                InvalidateVisual();
                Moved?.Invoke();
                e.Handled = true;
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            last = null;
        }

        public override void Render(DrawingContext context)
        {
            context.FillRectangle(Brushes.Black, new Rect(Bounds.Size));
            context.DrawImage(image, new Rect(image.Size), new Rect(offset.X, offset.Y, image.PixelSize.Width * zoom, image.PixelSize.Height * zoom));
            var amber = new SolidColorBrush(Color.FromRgb(232, 150, 46));
            foreach (var end in ends)
            {
                context.DrawEllipse(amber, null, ToScreen(end), 5, 5);
            }

            if (ends.Count == 2)
            {
                context.DrawLine(new Pen(amber, 2), ToScreen(ends[0]), ToScreen(ends[1]));
            }

            var blue = new Pen(new SolidColorBrush(Color.FromRgb(90, 160, 240)), 2);
            foreach (var bull in session.State.Bulls)
            {
                var at = ToScreen(bull.Image);
                context.DrawLine(blue, new Point(at.X - 12, at.Y), new Point(at.X + 12, at.Y));
                context.DrawLine(blue, new Point(at.X, at.Y - 12), new Point(at.X, at.Y + 12));
            }

            var found = new Pen(Brushes.OrangeRed, 2);
            var added = new Pen(Brushes.LimeGreen, 2);
            int number = 0;
            foreach (var shot in session.State.Shots.Where(s => s.IsShot))
            {
                number++;
                var at = ToScreen(shot.Image);
                bool byHand = shot.Provenance == ShotProvenance.Manual;
                context.DrawEllipse(null, byHand ? added : found, at, 9, 9);
                if (byHand)
                {
                    context.DrawEllipse(Brushes.LimeGreen, null, at, 2.5, 2.5);
                }
                else
                {
                    var text = new FormattedText(number.ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 12, Brushes.OrangeRed);
                    context.DrawText(text, new Point(at.X + 10, at.Y - 18));
                }
            }

            // The crosshair, fixed at the middle; its ring the bullet's real size where the scale and caliber are known.
            var middle = new Point(Bounds.Width / 2, Bounds.Height / 2);
            var cross = new Pen(Brushes.White, 1.5);
            context.DrawLine(cross, new Point(middle.X - 30, middle.Y), new Point(middle.X - 6, middle.Y));
            context.DrawLine(cross, new Point(middle.X + 6, middle.Y), new Point(middle.X + 30, middle.Y));
            context.DrawLine(cross, new Point(middle.X, middle.Y - 30), new Point(middle.X, middle.Y - 6));
            context.DrawLine(cross, new Point(middle.X, middle.Y + 6), new Point(middle.X, middle.Y + 30));
            if (RingInches is { } bullet && session.State.Scale is { } scale)
            {
                var a = scale.ToTarget(Centre);
                double pixelsPerInch = 1 / Math.Max(1e-9, Math.Abs(scale.ToTarget(new PointD(Centre.X + 1, Centre.Y)).X - a.X));
                context.DrawEllipse(null, cross, middle, bullet / 2 * pixelsPerInch * zoom, bullet / 2 * pixelsPerInch * zoom);
            }
        }
    }
}

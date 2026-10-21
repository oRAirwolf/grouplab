using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.StoreTargets;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 340 section 2 on the phone: "Which target is this?", asked only when the picture is of a store-bought target
/// sold at more than one size with the same printing and cannot tell which. Entry 465 3B: the picture fills the screen above a bottom sheet
/// that never covers it, so the target asked about is whole in view; the sheet holds the question and one line of why, the sizes as
/// buttons side by side, the last answer for the family first, and "Not sure", which places the bull and leaves the scale to be measured.
/// The answer chosen is kept for next time. The desktop asks the same question in a bar along the bottom of the picture.
/// </summary>
internal sealed class WhichTargetPage : UserControl
{
    /// <param name="seen">What recognition decided, with the picture's size in its stored pixels.</param>
    /// <param name="picture">The picture the question is about.</param>
    /// <param name="orientation">The picture's EXIF orientation, so it is shown upright.</param>
    /// <param name="answered">The member chosen, or null for "Not sure".</param>
    public WhichTargetPage(StoreTargetSeen seen, string picture, int? orientation, Action<StoreTargetMatch?> answered)
    {
        ArgumentNullException.ThrowIfNull(seen);
        ArgumentNullException.ThrowIfNull(answered);
        var recognition = seen.Recognition;
        string? family = FamilyQuestion.Family(recognition);
        Answers = FamilyQuestion.Answers(recognition, family is null ? null : Phone.Settings.LoadFamilyAnswer(family));
        AskedAbout = FamilyQuestion.AskedAbout(recognition, seen.Width, seen.Height);
        // Side by side, two to a row, so each name has half the sheet to wrap in.
        int across = Math.Min(2, Answers.Count);
        var sizes = new Grid { ColumnSpacing = 8, RowSpacing = 8 };
        for (int i = 0; i < across; i++)
        {
            sizes.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        for (int i = 0; i < (Answers.Count + across - 1) / Math.Max(1, across); i++)
        {
            sizes.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        }

        foreach (var answer in Answers)
        {
            var button = ProblemSheet.Choice(answer.Name, () =>
            {
                if (family is not null)
                {
                    Phone.Settings.SaveFamilyAnswer(family, answer.Match.Target.Id);
                }

                DiagnosticLog.Info("storetarget.answer", ("answer", answer.Match.Target.Id));
                answered(answer.Match);
            }, primary: sizes.Children.Count == 0).Id("which-target-" + answer.Match.Target.Id);
            button.MinHeight = Screens.Touch;
            Grid.SetColumn(button, sizes.Children.Count % across);
            Grid.SetRow(button, sizes.Children.Count / across);
            Avalonia.Automation.AutomationProperties.SetHelpText(button, answer.Printed);
            sizes.Children.Add(button);
        }

        var notSure = ProblemSheet.Choice(FamilyQuestion.NotSure, () =>
        {
            DiagnosticLog.Info("storetarget.answer", ("answer", "not sure"));
            answered(null);
        }).Id("which-target-not-sure");
        notSure.MinHeight = Screens.Touch;
        var column = new StackPanel { Spacing = 10, Children = { Screens.Heading(FamilyQuestion.Title), Screens.Quiet(FamilyQuestion.Why), sizes, notSure } };
        Sheet = new Border
        {
            Child = new ScrollViewer { Content = column, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled },
            Padding = new Thickness(16, 14, 16, 16),
            CornerRadius = new CornerRadius(14, 14, 0, 0),
            Classes = { PhoneStyles.Card },
        };
        Avalonia.Automation.AutomationProperties.SetName(Sheet, FamilyQuestion.Title);
        Picture = new QuestionPicture(File.Exists(picture) ? new Bitmap(picture) : null, ViewRotation.FromExifOrientation(orientation), seen.Width, seen.Height);
        var layout = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };
        Grid.SetRow(Sheet, 1);
        layout.Children.Add(Picture);
        layout.Children.Add(Sheet);
        Content = layout;
    }

    /// <summary>The answers in the order shown, for a test.</summary>
    internal IReadOnlyList<FamilyAnswer> Answers { get; }

    /// <summary>The part of the picture the question is about, in its stored pixels, for a test.</summary>
    internal (PointD TopLeft, PointD BottomRight)? AskedAbout { get; }

    /// <summary>The bottom sheet with the question, for a test.</summary>
    internal Border Sheet { get; }

    /// <summary>The picture above it, for a test.</summary>
    internal QuestionPicture Picture { get; }

    /// <summary>The picture, upright and whole in the room above the sheet, so the bull asked about is in view.</summary>
    internal sealed class QuestionPicture(Bitmap? image, int turns, int width, int height) : Control
    {
        private (double Width, double Height) Shown => ViewRotation.DisplaySize(turns, width, height);

        private double Scale => Math.Min(Bounds.Width / Math.Max(1, Shown.Width), Bounds.Height / Math.Max(1, Shown.Height));

        private Vector Offset => new((Bounds.Width - (Shown.Width * Scale)) / 2, (Bounds.Height - (Shown.Height * Scale)) / 2);

        /// <summary>A stored image point where it is drawn on this control.</summary>
        internal Point ToScreen(PointD stored)
        {
            var shown = ViewRotation.ToDisplay(stored, turns, width, height);
            return new Point((shown.X * Scale) + Offset.X, (shown.Y * Scale) + Offset.Y);
        }

        public override void Render(DrawingContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            if (image is null || width <= 0 || height <= 0)
            {
                return;
            }

            var (a, b, c, d, e, f) = ViewRotation.Affine(turns, width, height);
            double k = Scale;
            using (context.PushTransform(new Matrix(a * k, d * k, b * k, e * k, (c * k) + Offset.X, (f * k) + Offset.Y)))
            {
                context.DrawImage(image, new Rect(image.Size), new Rect(0, 0, width, height));
            }
        }
    }
}

using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Cli.Library;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.StoreTargets;
using Orientation = Avalonia.Layout.Orientation;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 348, concept A on the computer ("Fingerprint A: guided steps"): adding a store-bought target one question at a
/// time, reached from Targets, "Add a store-bought target". The five steps listed on the left, the done ones ticked in teal and the current
/// one amber; the photograph, or the straightened target, in the middle; the step's question on the right with Back and Next. It drives a
/// <see cref="FingerprintSession"/>, as the phone's steps do, so the two do the same work in the same words.
/// </summary>
public sealed class FingerprintStepsView : UserControl
{
    private readonly FingerprintSession session;
    private readonly Action leave;
    private readonly StackPanel stepList = new() { Spacing = Tokens.Space4 };
    private readonly FingerprintPicture picture = new();
    private readonly TextBlock pictureLine = new() { TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } };
    private readonly Border pictureNote;
    private readonly TextBlock empty = new() { Text = "No photo yet", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.Dim } };
    private readonly TextBlock question = new() { TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Section } };
    private readonly TextBlock explain = new() { TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } };
    private readonly StackPanel body = new() { Spacing = Tokens.Space12 };
    private readonly TextBlock said = new() { TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Warn } };
    private readonly Button back = new() { Content = FingerprintWords.Back };
    private readonly Button next = new() { Classes = { AppStyles.Primary } };
    private readonly TextBox width = Field("Width in inches");
    private readonly TextBox height = Field("Height in inches");
    private readonly TextBox distance = Field("Distance between the two ends, inches");
    private readonly TextBox name = new() { MinWidth = 240, PlaceholderText = FingerprintWords.NameHint };
    private readonly Dictionary<ScaleSource, RadioButton> sources = [];
    private bool adding;
    private Bitmap? shown;
    private Bitmap? straightShown;

    /// <param name="sheets">The GroupLab sheets a sheet in the photo is identified among.</param>
    /// <param name="leave">Back to Targets.</param>
    public FingerprintStepsView(Func<IReadOnlyList<TargetDefinition>> sheets, Action leave)
    {
        session = new FingerprintSession(sheets);
        this.leave = leave ?? throw new ArgumentNullException(nameof(leave));
        AutomationProperties.SetName(picture, "The photo of the target");
        AutomationProperties.SetName(name, FingerprintWords.NameBox);
        pictureNote = new Border { Child = pictureLine, Padding = new Thickness(Tokens.Space12, Tokens.Space8), Margin = new Thickness(Tokens.Space16), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom, MaxWidth = 420, Classes = { AppStyles.Chip } };
        picture.CornerMoved += (i, to) =>
        {
            session.MoveCorner(i, to);
            Draw();
        };
        picture.PointTapped += Touched;
        back.Click += (_, _) => GoBack();
        next.Click += (_, _) => _ = GoOn();
        name.TextChanged += (_, _) => session.Name = name.Text ?? "";
        width.TextChanged += (_, _) => session.WidthInches = Number(width.Text);
        height.TextChanged += (_, _) => session.HeightInches = Number(height.Text);
        distance.TextChanged += (_, _) => session.Distance = Number(distance.Text);
        foreach (var source in new[] { ScaleSource.PrintedSize, ScaleSource.GroupLabSheet, ScaleSource.TwoPoints })
        {
            var radio = new RadioButton { GroupName = "fingerprint-scale", IsChecked = source == session.Source, VerticalContentAlignment = VerticalAlignment.Top };
            AutomationProperties.SetName(radio, FingerprintWords.Choice(source));
            radio.IsCheckedChanged += (_, _) =>
            {
                if (radio.IsChecked == true && session.Source != source)
                {
                    Choose(source);
                }
            };
            sources[source] = radio;
        }

        var leaveButton = new Button { Content = "‹ Targets" };
        leaveButton.Click += (_, _) => Leave();
        var title = new TextBlock { Text = FingerprintWords.Title, VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.Title } };
        var head = new StackPanel { Orientation = Orientation.Horizontal, Spacing = Tokens.Space12, Children = { leaveButton, title } };

        var middle = new Grid { Children = { picture, empty, pictureNote }, Margin = new Thickness(Tokens.Space16, 0) };
        var right = new DockPanel { LastChildFill = true };
        var buttons = new DockPanel { Margin = new Thickness(0, Tokens.Space12, 0, 0), Children = { back, next } };
        DockPanel.SetDock(back, Dock.Left);
        next.HorizontalAlignment = HorizontalAlignment.Right;
        DockPanel.SetDock(buttons, Dock.Bottom);
        right.Children.Add(buttons);
        right.Children.Add(new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = new StackPanel { Spacing = Tokens.Space12, Children = { question, explain, body, said } },
        });

        var columns = new Grid { ColumnDefinitions = new ColumnDefinitions("200,*,360"), Margin = new Thickness(0, Tokens.Space16, 0, 0) };
        var steps = new Border { Child = stepList, Padding = new Thickness(0, 0, Tokens.Space16, 0), BorderThickness = new Thickness(0, 0, 1, 0) };
        steps.BorderBrush = new SolidColorBrush(Tokens.For(ActualThemeVariant).Line);
        ActualThemeVariantChanged += (_, _) =>
        {
            steps.BorderBrush = new SolidColorBrush(Tokens.For(ActualThemeVariant).Line);
            FillSteps(session.Step);
        };
        AutomationProperties.SetName(stepList, "Steps");
        Grid.SetColumn(middle, 1);
        Grid.SetColumn(right, 2);
        columns.Children.Add(steps);
        columns.Children.Add(middle);
        columns.Children.Add(right);
        var whole = new Grid { RowDefinitions = new RowDefinitions("Auto,*"), Margin = new Thickness(Tokens.Space24, Tokens.Space20) };
        Grid.SetRow(columns, 1);
        whole.Children.Add(head);
        whole.Children.Add(columns);
        Content = whole;
        Show();
    }

    private static TextBox Field(string named)
    {
        var box = new TextBox { Width = 84, FontFamily = Tokens.Mono };
        AutomationProperties.SetName(box, named);
        return box;
    }

    private static double? Number(string? text) =>
        double.TryParse((text ?? "").Trim(), NumberStyles.Float, CultureInfo.CurrentCulture, out double v)
        || double.TryParse((text ?? "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : null;

    /// <summary>The session the steps drive, for the headless tests.</summary>
    internal FingerprintSession Session => session;

    /// <summary>The last piece of slow work started, for the headless tests to wait on.</summary>
    internal Task Work { get; private set; } = Task.CompletedTask;

    /// <summary>The words of the right-hand column and the step list, for the headless tests.</summary>
    internal IEnumerable<string> Words =>
        new[] { question.Text, explain.Text, said.Text, pictureLine.Text }.Concat(body.GetLogicalDescendantsText()).Concat(stepList.GetLogicalDescendantsText()).Select(t => t ?? "");

    internal Button NextButton => next;

    internal Button BackButton => back;

    internal FingerprintPicture Picture => picture;

    /// <summary>A tap or click on the picture at a point in its own units, as the picture hands it on, for the headless tests.</summary>
    internal void Touch(PointD at) => Touched(at);

    /// <summary>A field of the right-hand column by its name, for the headless tests.</summary>
    internal TextBox FieldNamed(string named) => new[] { width, height, distance, name }.First(t => AutomationProperties.GetName(t) == named);

    /// <summary>Chooses a scale source as its card does.</summary>
    internal void ChooseSource(ScaleSource source) => sources[source].IsChecked = true;

    /// <summary>Reads a photograph into the first step, as choosing one does.</summary>
    internal void UsePhoto(string path)
    {
        string? why = session.Load(path);
        DiagnosticLog.Info("fingerprint.photo", ("read", why is null), ("corners", session.CornersFound));
        shown = why is null && session.Shown is { } jpeg ? new Bitmap(new MemoryStream(jpeg)) : shown;
        straightShown = null;
        said.Text = why ?? "";
        Show();
    }

    private async Task ChoosePhoto()
    {
        if (TopLevel.GetTopLevel(this)?.StorageProvider is not { } storage)
        {
            return;
        }

        DiagnosticLog.Info("dialog.open", ("dialog", "fingerprint-photo"));
        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = FingerprintWords.ChoosePhoto,
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Pictures") { Patterns = ["*.jpg", "*.jpeg", "*.png", "*.heic", "*.tif", "*.tiff"] }],
        });
        if (files.FirstOrDefault()?.TryGetLocalPath() is { } path)
        {
            UsePhoto(path);
        }
    }

    private void Choose(ScaleSource source)
    {
        session.Source = source;
        said.Text = "";
        DiagnosticLog.Info("fingerprint.scale", ("source", source.ToString()));
        if (source == ScaleSource.GroupLabSheet && !session.SheetTried)
        {
            said.Text = FingerprintWords.ReadingSheet;
            Busy(true);
            Work = Task.Run(session.ReadSheet).ContinueWith(t => Dispatcher.UIThread.Post(() =>
            {
                Busy(false);
                said.Text = t.IsCompletedSuccessfully ? t.Result : FingerprintWords.SheetNotRead;
                Show();
            }), TaskScheduler.Default);
        }

        Show();
    }

    private void Touched(PointD at)
    {
        switch (session.Step)
        {
            case FingerprintStep.Scale when session.Source == ScaleSource.TwoPoints:
                session.Place(at);
                break;
            case FingerprintStep.Bulls:
                if (!adding && session.BullAt(at) is { } bull)
                {
                    session.RemoveBull(bull);
                }
                else
                {
                    session.AddBull(at);
                }

                adding = false;
                break;
            default:
                return;
        }

        said.Text = "";
        Show();
    }

    /// <summary>Back a step, or to Targets from the first.</summary>
    internal void GoBack()
    {
        if (session.Step == FingerprintStep.Photo)
        {
            Leave();
            return;
        }

        session.Back();
        said.Text = "";
        adding = false;
        Show();
    }

    /// <summary>Next, or on the last step Save reference file; the slow work off the screen's thread.</summary>
    internal Task GoOn()
    {
        if (session.Step == FingerprintStep.Send)
        {
            Work = Save();
            return Work;
        }

        Take();
        if (session.Missing() is { } missing)
        {
            said.Text = missing;
            return Task.CompletedTask;
        }

        var from = session.Step;
        said.Text = FingerprintWords.Working;
        Busy(true);
        Work = Task.Run(session.Next).ContinueWith(t => Dispatcher.UIThread.Post(() =>
        {
            Busy(false);
            string? refused = t.IsCompletedSuccessfully ? t.Result : FingerprintWords.NotATarget;
            said.Text = refused ?? "";
            if (from == FingerprintStep.Straighten && session.StraightShown is { } jpeg)
            {
                straightShown = new Bitmap(new MemoryStream(jpeg));
            }

            DiagnosticLog.Info("fingerprint.step", ("step", session.Step.ToString()), ("refused", refused is not null));
            adding = false;
            Show();
        }), TaskScheduler.Default);
        return Work;
    }

    /// <summary>What is typed in the fields, into the session, whether or not the fields said it changed.</summary>
    private void Take()
    {
        session.WidthInches = Number(width.Text);
        session.HeightInches = Number(height.Text);
        session.Distance = Number(distance.Text);
        session.Name = name.Text ?? "";
    }

    private async Task Save()
    {
        Take();
        if (session.Missing() is { } missing)
        {
            said.Text = missing;
            return;
        }

        if (TopLevel.GetTopLevel(this)?.StorageProvider is not { } storage)
        {
            return;
        }

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = FingerprintWords.Next(FingerprintStep.Send, phone: false),
            SuggestedFileName = session.FileName,
            DefaultExtension = "glref",
        });
        if (file?.TryGetLocalPath() is { } path)
        {
            SaveTo(path);
        }
    }

    /// <summary>Writes the reference file, never the photograph, and says so.</summary>
    internal bool SaveTo(string path)
    {
        Take();
        try
        {
            var reference = session.Write(path);
            DiagnosticLog.Info("fingerprint.save", ("bulls", reference.Fingerprint.Bulls.Count), ("bytes", new FileInfo(path).Length), ("family", reference.Target.Family is not null));
            said.Text = string.Create(CultureInfo.InvariantCulture, $"Saved {Path.GetFileName(path)}, {new FileInfo(path).Length / 1024.0:0} KB. {FingerprintWords.Checked}");
            said.Classes.Remove(AppStyles.Warn);
            said.Classes.Add(AppStyles.Good);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "fingerprint.save", e);
            said.Text = "The file could not be written: " + e.Message;
            return false;
        }
    }

    private void Busy(bool on)
    {
        next.IsEnabled = !on;
        back.IsEnabled = !on;
    }

    private void Leave()
    {
        session.Dispose();
        leave();
    }

    /// <summary>The whole screen for the step the session is on.</summary>
    private void Show()
    {
        var step = session.Step;
        FillSteps(step);
        question.Text = FingerprintWords.Question(step);
        explain.Text = FingerprintWords.Explain(step, touch: false);
        next.Content = FingerprintWords.Next(step, phone: false);
        AutomationProperties.SetName(next, FingerprintWords.Next(step, phone: false));
        back.Content = step == FingerprintStep.Photo ? "Back to Targets" : FingerprintWords.Back;
        body.Children.Clear();
        switch (step)
        {
            case FingerprintStep.Photo:
                var choose = new Button { Content = FingerprintWords.ChoosePhoto };
                choose.Click += (_, _) => _ = ChoosePhoto();
                body.Children.Add(choose);
                if (session.Shown is not null)
                {
                    body.Children.Add(Line(session.CornersFound ? "Corners found." : FingerprintWords.CornersNotFound));
                }

                break;
            case FingerprintStep.Scale:
                foreach (var (source, radio) in sources)
                {
                    radio.IsChecked = source == session.Source;
                    var inside = new StackPanel { Spacing = Tokens.Space4 };
                    inside.Children.Add(new TextBlock { Text = FingerprintWords.Choice(source), FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap });
                    inside.Children.Add(Line(FingerprintWords.How(source, phone: false)));
                    if (source == ScaleSource.PrintedSize)
                    {
                        inside.Children.Add(new WrapPanel { Children = { Detach(width), Small(FingerprintWords.By), Detach(height), Small(FingerprintWords.Inches) } });
                    }
                    else if (source == ScaleSource.TwoPoints && session.Source == ScaleSource.TwoPoints)
                    {
                        inside.Children.Add(Line(FingerprintWords.PointsSaid(session.Placed, touch: false)));
                        inside.Children.Add(new WrapPanel { Children = { Detach(distance), Small(FingerprintWords.Inches) } });
                    }

                    radio.Content = inside;
                    var card = new Border { Child = Detach(radio), Padding = new Thickness(Tokens.Space8), Classes = { AppStyles.TableRow } };
                    if (source == session.Source)
                    {
                        card.Classes.Add(AppStyles.Warn);
                    }

                    body.Children.Add(card);
                }

                body.Children.Add(new TextBlock { Text = FingerprintWords.Measured(session.Source), TextWrapping = TextWrapping.Wrap, FontFamily = Tokens.Mono, FontSize = Tokens.SecondarySize, Classes = { AppStyles.Dim } });
                break;
            case FingerprintStep.Straighten:
                if (!session.CornersFound)
                {
                    explain.Text = FingerprintWords.CornersNotFound;
                }

                break;
            case FingerprintStep.Bulls:
                for (int i = 0; i < session.Bulls.Count; i++)
                {
                    int at = i;
                    var b = session.Bulls[i];
                    var remove = new Button { Content = "Remove" };
                    AutomationProperties.SetName(remove, string.Create(CultureInfo.InvariantCulture, $"Remove bull {i + 1}"));
                    remove.Click += (_, _) =>
                    {
                        session.RemoveBull(at);
                        Show();
                    };
                    var row = new DockPanel { Children = { remove, Line(string.Create(CultureInfo.InvariantCulture, $"{i + 1}   Bull at {b.X:0.0}, {b.Y:0.0} in")) } };
                    DockPanel.SetDock(remove, Dock.Right);
                    row.Children[1].VerticalAlignment = VerticalAlignment.Center;
                    body.Children.Add(row);
                }

                var add = new Button { Content = FingerprintWords.AddBull };
                add.Click += (_, _) =>
                {
                    adding = true;
                    said.Text = FingerprintWords.WhereTheBull(touch: false);
                };
                body.Children.Add(add);
                break;
            case FingerprintStep.Send:
                body.Children.Add(new TextBlock { Text = FingerprintWords.NameBox, Classes = { AppStyles.Label } });
                body.Children.Add(Detach(name));
                body.Children.Add(Line("Size: " + session.Size));
                if (session.Target is { } target)
                {
                    body.Children.Add(Line(target.Says));
                }

                if (session.FamilySaid is { } family)
                {
                    body.Children.Add(new TextBlock { Text = family, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Warn } });
                }

                body.Children.Add(new TextBlock { Text = FingerprintWords.Never, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Dim } });
                break;
        }

        Draw();
    }

    private void Draw()
    {
        var step = session.Step;
        bool straightened = step is FingerprintStep.Bulls or FingerprintStep.Send;
        picture.Image = straightened ? straightShown : shown;
        picture.PerUnit = straightened ? session.StraightShownScale : session.ShownScale;
        picture.Corners = straightened ? [] : session.Corners;
        picture.CornersMove = step is FingerprintStep.Scale or FingerprintStep.Straighten;
        picture.Ends = step == FingerprintStep.Scale && session.Source == ScaleSource.TwoPoints
            ? new[] { session.PointA, session.PointB }.Where(p => p is not null).Select(p => p!.Value).ToList()
            : [];
        picture.Rings = straightened ? [.. session.Bulls] : [];
        picture.RingRadius = session.RingInches;
        picture.InvalidateVisual();
        empty.IsVisible = picture.Image is null;
        pictureLine.Text = step switch
        {
            FingerprintStep.Scale or FingerprintStep.Straighten when session.Shown is not null => session.CornersFound ? FingerprintWords.Explain(FingerprintStep.Straighten, false) : FingerprintWords.CornersNotFound,
            FingerprintStep.Bulls => FingerprintWords.Explain(FingerprintStep.Bulls, false),
            _ => "",
        };
        pictureNote.IsVisible = pictureLine.Text.Length > 0;
    }

    private void FillSteps(FingerprintStep current)
    {
        stepList.Children.Clear();
        var palette = Tokens.For(ActualThemeVariant);
        for (int i = 0; i < FingerprintWords.Steps.Count; i++)
        {
            var step = (FingerprintStep)i;
            bool done = step < current, now = step == current;
            var mark = new Border
            {
                Width = 24,
                Height = 24,
                CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(1),
                Child = new TextBlock
                {
                    Text = done ? "✓" : (i + 1).ToString(CultureInfo.InvariantCulture),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = Tokens.SecondarySize,
                    Foreground = new SolidColorBrush(done ? palette.Teal : now ? palette.OnAmber : palette.Faint),
                },
                Background = new SolidColorBrush(done ? palette.TealTint : now ? palette.Amber : palette.Bg),
                BorderBrush = new SolidColorBrush(done ? palette.TealTintBorder : now ? palette.Amber : palette.Line2),
            };
            var words = new TextBlock { Text = FingerprintWords.Steps[i], VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, FontWeight = now ? FontWeight.SemiBold : FontWeight.Normal, Classes = { now ? AppStyles.Secondary : AppStyles.Dim } };
            if (now)
            {
                words.Foreground = new SolidColorBrush(palette.Text);
            }

            var row = new Border
            {
                Padding = new Thickness(Tokens.Space8),
                CornerRadius = Tokens.ButtonRadius,
                Background = now ? new SolidColorBrush(palette.AmberTint) : null,
                Child = new StackPanel { Orientation = Orientation.Horizontal, Spacing = Tokens.Space8, Children = { mark, words } },
            };
            AutomationProperties.SetName(row, $"{FingerprintWords.Steps[i]}, {(done ? "done" : now ? "now" : "to come")}");
            stepList.Children.Add(row);
        }
    }

    private static TextBlock Line(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } };

    private static TextBlock Small(string text) => new() { Text = text, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(Tokens.Space8, 0), Classes = { AppStyles.Dim } };

    /// <summary>A control kept between redraws, taken from wherever it was, since a control has one parent.</summary>
    private static T Detach<T>(T control)
        where T : Control
    {
        switch (control.Parent)
        {
            case Panel panel:
                panel.Children.Remove(control);
                break;
            case ContentControl holder:
                holder.Content = null;
                break;
            case Decorator decorator:
                decorator.Child = null;
                break;
        }

        return control;
    }
}

/// <summary>The words under a control, for the headless tests.</summary>
internal static class LogicalText
{
    public static IEnumerable<string?> GetLogicalDescendantsText(this Control control) =>
        Avalonia.LogicalTree.LogicalExtensions.GetLogicalDescendants(control).OfType<TextBlock>().Select(t => t.Text);
}

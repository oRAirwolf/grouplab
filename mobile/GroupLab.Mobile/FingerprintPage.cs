using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Cli.Library;
using GroupLab.Core.Imaging;
using GroupLab.Core.StoreTargets;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 348, concept A on a phone ("Fingerprint A on a phone"): adding a store-bought target in five full-screen steps,
/// reached from Targets, "Add a store-bought target". A five-part progress bar with Back at the top, the step's question and its picture, and
/// one big button at the bottom; on the bulls, "Add a bull" beside "These are right". The last step saves the reference file and hands it to
/// the share sheet. It drives the same <see cref="FingerprintSession"/> as the computer's steps, so both say and do the same.
/// </summary>
internal sealed class FingerprintPage : UserControl
{
    private readonly FingerprintSession session = new(PhoneAnalysis.Library, GroupLab.Core.Imaging.WorkingSize.PhoneMegapixels * 2);
    private readonly Action leave;
    private readonly FingerprintPicture picture = new() { Reach = 32 };
    private readonly TextBlock said = Screens.Line("");
    private readonly TextBox width = Box("fingerprint-width", "Width in inches");
    private readonly TextBox height = Box("fingerprint-height", "Height in inches");
    private readonly TextBox distance = Box("fingerprint-distance", "Distance between the two ends, inches");
    private readonly TextBox name = new TextBox { MinHeight = Screens.Touch, PlaceholderText = FingerprintWords.NameHint }.Id("fingerprint-name");
    private bool adding;
    private bool busy;
    private Bitmap? shown;
    private Bitmap? straightShown;

    public FingerprintPage(Action leave)
    {
        this.leave = leave ?? throw new ArgumentNullException(nameof(leave));
        AutomationProperties.SetName(picture, "The photo of the target");
        AutomationProperties.SetName(name, FingerprintWords.NameBox);
        picture.CornerMoved += (i, to) => session.MoveCorner(i, to);
        picture.PointTapped += Touched;
        ActualThemeVariantChanged += (_, _) => Show();
        Show();
    }

    private static TextBox Box(string id, string named)
    {
        var box = Screens.Numeric(new TextBox { MinHeight = Screens.Touch, Width = 88 }).Id(id);
        AutomationProperties.SetName(box, named);
        return box;
    }

    /// <summary>The session the steps drive, for the tests.</summary>
    internal FingerprintSession Session => session;

    /// <summary>The last piece of slow work, for the tests to wait on.</summary>
    internal Task Work { get; private set; } = Task.CompletedTask;

    internal string Said => said.Text ?? "";

    /// <summary>Reads a photograph into the first step, off the screen's thread.</summary>
    internal Task UsePhoto(string path)
    {
        said.Text = FingerprintWords.Working;
        busy = true;
        Show();
        Work = Task.Run(() => session.Load(path)).ContinueWith(t => Dispatcher.UIThread.Post(() =>
        {
            busy = false;
            string? why = t.IsCompletedSuccessfully ? t.Result : "That file is not a picture GroupLab can read.";
            DiagnosticLog.Info("fingerprint.photo", ("read", why is null), ("corners", session.CornersFound));
            if (why is null && session.Shown is { } jpeg)
            {
                shown = new Bitmap(new MemoryStream(jpeg));
                straightShown = null;
            }

            said.Text = why ?? "";
            Show();
        }), TaskScheduler.Default);
        return Work;
    }

    private async Task Choose()
    {
        var picked = await PhotoPages.Pick(this, PhotoSource.Photos, "fingerprint", words => said.Text = words);
        if (picked.FirstOrDefault() is { } photo)
        {
            await UsePhoto(photo.Path);
        }
    }

    private void Camera()
    {
        if (!Phone.Platform.CameraAllowed())
        {
            said.Text = "GroupLab needs the camera to take the photo. Allow it, then press Take a photo again.";
            return;
        }

        var back = Content;
        Shell.Current?.Immersive(true);
        Content = Phone.Platform.Camera((path, torch) =>
        {
            Shell.Current?.Immersive(false);
            Content = back;
            _ = UsePhoto(path);
        }, () =>
        {
            Shell.Current?.Immersive(false);
            Content = back;
        }, () =>
        {
            Shell.Current?.Immersive(false);
            Content = back;
            _ = Choose();
        });
    }

    /// <summary>A tap on the picture, as the picture hands it on.</summary>
    internal void Touched(PointD at)
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

    private void Take()
    {
        session.WidthInches = Screens.Read(width.Text);
        session.HeightInches = Screens.Read(height.Text);
        session.Distance = Screens.Read(distance.Text);
        session.Name = name.Text ?? "";
    }

    /// <summary>Back a step, or to Targets from the first.</summary>
    internal void GoBack()
    {
        Take();
        if (session.Step == FingerprintStep.Photo)
        {
            session.Dispose();
            leave();
            return;
        }

        session.Back();
        said.Text = "";
        adding = false;
        Show();
    }

    /// <summary>The big button: the next step, its slow work off the screen's thread, or on the last step the file saved and shared.</summary>
    internal Task GoOn()
    {
        Take();
        if (session.Missing() is { } missing)
        {
            said.Text = missing;
            return Task.CompletedTask;
        }

        if (session.Step == FingerprintStep.Send)
        {
            SaveAndShare();
            return Task.CompletedTask;
        }

        var from = session.Step;
        said.Text = FingerprintWords.Working;
        busy = true;
        Show();
        Work = Task.Run(session.Next).ContinueWith(t => Dispatcher.UIThread.Post(() =>
        {
            busy = false;
            string? refused = t.IsCompletedSuccessfully ? t.Result : FingerprintWords.NotATarget;
            if (from == FingerprintStep.Straighten && session.StraightShown is { } jpeg)
            {
                straightShown = new Bitmap(new MemoryStream(jpeg));
            }

            DiagnosticLog.Info("fingerprint.step", ("step", session.Step.ToString()), ("refused", refused is not null));
            said.Text = refused ?? "";
            adding = false;
            Show();
        }), TaskScheduler.Default);
        return Work;
    }

    /// <summary>Entry 348 section 3: the file saved in the phone's own folder and handed to the share sheet; never the photo.</summary>
    internal void SaveAndShare()
    {
        string path = Path.Combine(Phone.Platform.CacheFolder, session.FileName);
        try
        {
            var reference = session.Write(path);
            DiagnosticLog.Info("fingerprint.save", ("bulls", reference.Fingerprint.Bulls.Count), ("bytes", new FileInfo(path).Length), ("family", reference.Target.Family is not null));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "fingerprint.save", e);
            said.Text = "The file could not be written: " + e.Message;
            return;
        }

        string? refused = Phone.Platform.ShareFile(path, "application/json", FingerprintWords.Title);
        said.Text = refused ?? string.Create(CultureInfo.InvariantCulture, $"Saved {session.FileName}, {new FileInfo(path).Length / 1024.0:0} KB. {FingerprintWords.Checked}");
    }

    /// <summary>The page for the step the session is on.</summary>
    private void Show()
    {
        var step = session.Step;
        var palette = Tokens.For(ActualThemeVariant);

        // The top: Back, the step's place, and five bars, done in teal, now in amber.
        var backButton = new Button
        {
            Content = new TextBlock { Text = "‹", FontSize = 24, VerticalAlignment = VerticalAlignment.Center },
            MinWidth = Screens.Touch,
            MinHeight = Screens.Touch,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            IsEnabled = !busy,
        }.Id("fingerprint-back");
        AutomationProperties.SetName(backButton, step == FingerprintStep.Photo ? "Back to Targets" : FingerprintWords.Back);
        backButton.Click += (_, _) => GoBack();
        var where = new TextBlock { Text = FingerprintWords.Of(step), TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Classes = { PhoneStyles.Dim } };
        var top = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), Children = { backButton, where } };
        Grid.SetColumn(where, 1);
        var bars = new Avalonia.Controls.Primitives.UniformGrid { Columns = FingerprintWords.Steps.Count, Margin = new Thickness(0, 4, 0, 0) };
        for (int i = 0; i < FingerprintWords.Steps.Count; i++)
        {
            var bar = new Border
            {
                Height = 4,
                Margin = new Thickness(2, 0),
                CornerRadius = new CornerRadius(2),
                Background = new SolidColorBrush(i < (int)step ? palette.Teal : i == (int)step ? palette.Amber : palette.Line2),
            };
            AutomationProperties.SetName(bar, $"{FingerprintWords.Steps[i]}, {(i < (int)step ? "done" : i == (int)step ? "now" : "to come")}");
            bars.Children.Add(bar);
        }

        var head = new StackPanel { Margin = new Thickness(8, 8, 8, 4), Children = { top, bars } };

        // The middle: the picture where the step has one, the question, and what the step asks for.
        var column = new StackPanel { Spacing = 12 };
        bool straightened = step is FingerprintStep.Bulls or FingerprintStep.Send;
        picture.Image = straightened ? straightShown : shown;
        picture.PerUnit = straightened ? session.StraightShownScale : session.ShownScale;
        picture.Corners = straightened ? [] : session.Corners;
        picture.CornersMove = step is FingerprintStep.Scale or FingerprintStep.Straighten;
        picture.Ends = step == FingerprintStep.Scale && session.Source == ScaleSource.TwoPoints
            ? new[] { session.PointA, session.PointB }.Where(p => p is not null).Select(p => p!.Value).ToList()
            : [];
        picture.Rings = straightened && step == FingerprintStep.Bulls ? [.. session.Bulls] : [];
        picture.RingRadius = session.RingInches;
        picture.Height = step switch { FingerprintStep.Straighten => 420, FingerprintStep.Bulls => 380, _ => 250 };
        picture.InvalidateVisual();
        bool pictured = picture.Image is not null && step != FingerprintStep.Send;
        if (pictured && step != FingerprintStep.Bulls)
        {
            column.Children.Add(Screens.Detach(picture));
        }

        column.Children.Add(Screens.Title(FingerprintWords.Question(step)));
        if (step != FingerprintStep.Scale)
        {
            column.Children.Add(Screens.Dim(step == FingerprintStep.Straighten && !session.CornersFound ? FingerprintWords.CornersNotFound : FingerprintWords.Explain(step, touch: true)));
        }

        Control bottom;
        switch (step)
        {
            case FingerprintStep.Photo:
                if (session.Shown is not null)
                {
                    column.Children.Add(Screens.Line(session.CornersFound ? "Corners found." : FingerprintWords.CornersNotFound));
                }

                column.Children.Add(Screens.Choice(FingerprintWords.TakePhoto, Camera).Id("fingerprint-camera"));
                column.Children.Add(Screens.Choice(FingerprintWords.ChoosePhoto, () => _ = Choose()).Id("fingerprint-choose"));
                bottom = Next(step);
                break;
            case FingerprintStep.Scale:
                foreach (var source in new[] { ScaleSource.PrintedSize, ScaleSource.GroupLabSheet, ScaleSource.TwoPoints })
                {
                    column.Children.Add(Source(source));
                }

                column.Children.Add(Screens.Quiet(FingerprintWords.Measured(session.Source)));
                bottom = Next(step);
                break;
            case FingerprintStep.Bulls:
                if (pictured)
                {
                    column.Children.Add(Screens.Detach(picture));
                }

                var rows = new StackPanel();
                for (int i = 0; i < session.Bulls.Count; i++)
                {
                    int at = i;
                    var number = new TextBlock { Text = (i + 1).ToString(CultureInfo.InvariantCulture), FontFamily = Tokens.Mono, VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(palette.Teal), MinWidth = 24 }; // one line on purpose: a number
                    var b = session.Bulls[i];
                    var words = new TextBlock { Text = string.Create(CultureInfo.InvariantCulture, $"Bull at {b.X:0.0}, {b.Y:0.0} in"), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
                    var remove = new Button { Content = "Remove", MinHeight = 44, MinWidth = 44, VerticalAlignment = VerticalAlignment.Center }.Id("fingerprint-remove-" + (i + 1).ToString(CultureInfo.InvariantCulture));
                    AutomationProperties.SetName(remove, string.Create(CultureInfo.InvariantCulture, $"Remove bull {i + 1}"));
                    remove.Click += (_, _) =>
                    {
                        session.RemoveBull(at);
                        Show();
                    };
                    var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), ColumnSpacing = 12, MinHeight = 48, Children = { number, words, remove } };
                    Grid.SetColumn(words, 1);
                    Grid.SetColumn(remove, 2);
                    rows.Children.Add(row);
                }

                if (session.Bulls.Count > 0)
                {
                    column.Children.Add(new Border { Child = rows, Classes = { PhoneStyles.Card } });
                }

                var add = Screens.Choice(FingerprintWords.AddBull, () =>
                {
                    adding = true;
                    said.Text = FingerprintWords.WhereTheBull(touch: true);
                }).Id("fingerprint-add-bull");
                var right = Next(step);
                var pair = new Grid { ColumnDefinitions = new ColumnDefinitions("*,12,1.4*"), Children = { add, right } };
                Grid.SetColumn(right, 2);
                bottom = pair;
                break;
            case FingerprintStep.Send:
                column.Children.Add(Screens.Dim(FingerprintWords.NameBox));
                column.Children.Add(Screens.Detach(name));
                column.Children.Add(Screens.Line("Size: " + session.Size));
                if (session.Target is { } target)
                {
                    column.Children.Add(Screens.Quiet(target.Says));
                }

                if (session.FamilySaid is { } family)
                {
                    column.Children.Add(Screens.Card(Screens.Line(family)));
                }

                column.Children.Add(Screens.Quiet(FingerprintWords.Never));
                bottom = Next(step);
                break;
            default:
                bottom = Next(step);
                break;
        }

        column.Children.Add(Screens.Detach(said));
        var foot = new Border { Child = bottom, Padding = new Thickness(16, 12, 16, 16), BorderThickness = new Thickness(0, 1, 0, 0), BorderBrush = new SolidColorBrush(palette.Line) };
        var dock = new DockPanel();
        DockPanel.SetDock(head, Dock.Top);
        DockPanel.SetDock(foot, Dock.Bottom);
        dock.Children.Add(head);
        dock.Children.Add(foot);
        dock.Children.Add(Screens.Page(column));
        Content = dock;
    }

    private Button Next(FingerprintStep step)
    {
        var next = Screens.Primary(FingerprintWords.Next(step, phone: true), () => _ = GoOn()).Id("fingerprint-next");
        next.MinHeight = 52;
        next.IsEnabled = !busy;
        return next;
    }

    /// <summary>One scale source as a card that turns amber when chosen, its own fields inside it.</summary>
    private Control Source(ScaleSource source)
    {
        var inside = new StackPanel { Spacing = 8 };
        inside.Children.Add(new TextBlock { Text = FingerprintWords.Choice(source), FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap });
        inside.Children.Add(Screens.Quiet(FingerprintWords.How(source, phone: true)));
        bool chosen = session.Source == source;
        if (source == ScaleSource.PrintedSize)
        {
            inside.Children.Add(new WrapPanel { Children = { Screens.Detach(width), Words(FingerprintWords.By), Screens.Detach(height), Words(FingerprintWords.Inches) } });
        }
        else if (source == ScaleSource.TwoPoints && chosen)
        {
            inside.Children.Add(Screens.Line(FingerprintWords.PointsSaid(session.Placed, touch: true)));
            inside.Children.Add(new WrapPanel { Children = { Screens.Detach(distance), Words(FingerprintWords.Inches) } });
        }

        var radio = new RadioButton { GroupName = "fingerprint-scale", Content = inside, IsChecked = chosen, Classes = { PhoneStyles.Choice } }
            .Id("fingerprint-source-" + source.ToString().ToLowerInvariant());
        AutomationProperties.SetName(radio, FingerprintWords.Choice(source));
        radio.IsCheckedChanged += (_, _) =>
        {
            if (radio.IsChecked == true && session.Source != source)
            {
                ChooseSource(source);
            }
        };
        return radio;
    }

    private static TextBlock Words(string text) => new() { Text = text, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0), Classes = { PhoneStyles.Dim } };

    /// <summary>Chooses a scale source as its card does; a GroupLab sheet is looked for at once.</summary>
    internal void ChooseSource(ScaleSource source)
    {
        Take();
        session.Source = source;
        said.Text = "";
        DiagnosticLog.Info("fingerprint.scale", ("source", source.ToString()));
        if (source == ScaleSource.GroupLabSheet && !session.SheetTried)
        {
            said.Text = FingerprintWords.ReadingSheet;
            busy = true;
            Work = Task.Run(session.ReadSheet).ContinueWith(t => Dispatcher.UIThread.Post(() =>
            {
                busy = false;
                said.Text = t.IsCompletedSuccessfully ? t.Result : FingerprintWords.SheetNotRead;
                Show();
            }), TaskScheduler.Default);
        }

        Dispatcher.UIThread.Post(Show);
    }

    /// <summary>A field by its automation id, for the tests.</summary>
    internal TextBox Field(string id) => new[] { width, height, distance, name }.First(t => AutomationProperties.GetAutomationId(t) == id);
}

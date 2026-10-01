using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Reporting;

namespace GroupLab.App;

/// <summary>
/// Row 10 of the phone parity canvas on the desktop, NOTES-FROM-PLANNING.md entry 280 section 2: "Desktop equivalents follow the same content
/// in the desktop's layout." Shots A, Zero from this group and Share A are windows of their own, as Fudd buster mode is; the several aim
/// points are a section under the shot table; the one-page report is a second report beside the full one. Their words and numbers are the
/// phone's, from the same shared code (<see cref="ResultWords"/>, <see cref="ShotOffsets"/>, <see cref="ShareCard"/>,
/// <see cref="OnePageReports"/>), so the two cannot disagree.
/// </summary>
public sealed partial class MainWindow
{
    /// <summary>The aim points' section under the shot table, on a target GroupLab did not print.</summary>
    private readonly StackPanel aimPoints = new() { Spacing = Tokens.Space4 };

    /// <summary>The group's zero offset carried into Ballistics' dope, from "Use as the zero offset in Ballistics"; none until chosen.</summary>
    private ZeroOffset? ballisticsZeroOffset;

    /// <summary>The window open now, for the headless tests.</summary>
    internal Window? GroupToolWindow { get; private set; }

    /// <summary>The words of a window, in order, for the headless tests.</summary>
    internal static IEnumerable<string> WindowText(Window window) =>
        window.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").Where(t => t.Length > 0);

    /// <summary>The aim points' section's words, for the headless tests.</summary>
    internal IEnumerable<string> AimPointsText => aimPoints.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "");

    /// <summary>The ways into row 10 from the analysis: under the shot table and under the zero correction.</summary>
    private void BuildGroupTools(StackPanel shotsColumn, StackPanel figures)
    {
        var shots = Button("Shots and clicks…", () => _ = ShowGroupTool(ShotsWindow()));
        ToolTip.SetTip(shots, "Every shot's offset and clicks, and which count");
        var share = Button("Share a picture…", () => _ = ShowGroupTool(ShareWindow()));
        ToolTip.SetTip(share, "The target with its results box and the mean radius circle, saved as a picture");
        shotsColumn.Children.Insert(shotsColumn.Children.IndexOf(offsetTable) + 1, Row(shots, share));
        shotsColumn.Children.Insert(shotsColumn.Children.IndexOf(offsetTable) + 2, aimPoints);
        var zero = Button("Zero from this group…", () => _ = ShowGroupTool(ZeroFromWindow()));
        ToolTip.SetTip(zero, "Where the group sits, the clicks, and how sure");
        figures.Children.Insert(figures.Children.IndexOf(zeroPanel) + 1, Row(zero));
    }

    private async Task ShowGroupTool(Window window)
    {
        GroupToolWindow = window;
        DiagnosticLog.Info("dialog.open", ("dialog", window.Title ?? ""));
        await window.ShowDialog(this);
        GroupToolWindow = null;
    }

    private static Window ToolWindow(string title, Control body, double width = 640, double height = 720) => new()
    {
        Title = title,
        Width = width,
        Height = height,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        Content = new ScrollViewer { Content = body },
    };

    /// <summary>
    /// Board MultiAim on the desktop: on a target GroupLab did not print, each aim point in its own color with its own figures, the note that
    /// the figures above are pooled, and "+ Aim point", which goes back to the sheet with the aim point tool chosen.
    /// </summary>
    private void ShowAimPoints(MarkingState state)
    {
        aimPoints.Children.Clear();
        aimPoints.IsVisible = plotDefinition is null && ResultWords.AimedByHand(state);
        if (!aimPoints.IsVisible)
        {
            return;
        }

        aimPoints.Children.Add(Ruled("Aim points"));
        var marks = Tokens.BullMarks;
        int at = 0;
        foreach (var (aim, own) in GroupAnalysis.ByAimPoint(state))
        {
            int index = state.Bulls.FindIndex(b => b.Index == aim.Index);
            var dot = new Avalonia.Controls.Shapes.Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(marks[(index < 0 ? at : index) % marks.Count]),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, Tokens.Space8, 0),
            };
            aimPoints.Children.Add(new DockPanel { Children = { dot, Line(ResultWords.AimPoint(aim, own, units)) } });
            at++;
        }

        aimPoints.Children.Add(Line(ResultWords.AimPointsPooled));
        aimPoints.Children.Add(Row(Button("+ Aim point", () =>
        {
            BackToEditor();
            SetTool(MarkingTool.Bulls);
        })));
    }

    /// <summary>
    /// Shots A, board ShotsA: every shot's offset across and up from its aim point, its clicks from the rifle's click value, and "Counted"; a
    /// shot left out is struck through here, hollow on the plot and in no figure, and stays on the record.
    /// </summary>
    internal Window ShotsWindow()
    {
        var body = new StackPanel { Margin = Tokens.SectionPadding, Spacing = Tokens.Space8 };
        var window = ToolWindow("Shots", body);
        void Fill()
        {
            body.Children.Clear();
            var state = session.State;
            body.Children.Add(new TextBlock { Text = "Shots", Classes = { AppStyles.Section } });
            if (ResultWords.ShotsSummary(GroupAnalysis.Analyse(state), units) is { } summary)
            {
                body.Children.Add(new TextBlock { Text = summary, TextWrapping = TextWrapping.Wrap });
            }

            var rows = ShotOffsets.Table(state);
            if (rows.Count == 0)
            {
                body.Children.Add(Line(ResultWords.NoOffsets));
            }
            else
            {
                body.Children.Add(Note(ResultWords.ShotsIntro(state)));
                var table = new Grid { ColumnDefinitions = new ColumnDefinitions("60,*,*,Auto"), ColumnSpacing = Tokens.Space8, RowSpacing = Tokens.Space4 };
                void Cell(Control cell, int row, int column)
                {
                    Grid.SetRow(cell, row);
                    Grid.SetColumn(cell, column);
                    table.Children.Add(cell);
                }

                table.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                string[] heads = ["shot", "offset", "clicks", "counted"];
                for (int c = 0; c < heads.Length; c++)
                {
                    Cell(new TextBlock { Text = heads[c], Classes = { AppStyles.Dim } }, 0, c);
                }

                for (int i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    table.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    var struck = row.LeftOut ? TextDecorations.Strikethrough : null;
                    Cell(new TextBlock { Text = "Shot " + row.Label, TextDecorations = struck, VerticalAlignment = VerticalAlignment.Center }, i + 1, 0);
                    Cell(new TextBlock { Text = ResultWords.Offset(row, units), TextDecorations = struck, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.Secondary } }, i + 1, 1);
                    Cell(new TextBlock { Text = ResultWords.Clicks(row), TextDecorations = struck, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.Secondary } }, i + 1, 2);
                    var counted = new CheckBox { IsChecked = !row.LeftOut, Content = "Counted" };
                    int id = row.ShotId;
                    counted.IsCheckedChanged += (_, _) =>
                    {
                        session.SetExclusion(id, counted.IsChecked == true ? null : ExclusionReason.ByShooter);
                        Fill();
                    };
                    Cell(counted, i + 1, 3);
                }

                body.Children.Add(table);
            }

            body.Children.Add(Row(Button("Save the table (CSV)…", async () => await CsvFromShots(window)), Button("Close", window.Close)));
        }

        Fill();
        return window;
    }

    /// <summary>"Share the table (CSV)", on the desktop a file saved where the person chooses, the file the export writes.</summary>
    private async Task CsvFromShots(Window owner)
    {
        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save the shots as CSV",
            SuggestedFileName = Path.GetFileNameWithoutExtension(session.State.ImagePath ?? "shots") + ".csv",
            DefaultExtension = "csv",
        });
        if (file?.TryGetLocalPath() is { } path)
        {
            await WriteCsv(path);
        }
    }

    /// <summary>
    /// Zero from this group, board ZeroFrom: where the group's centre sits from the aim point, the clicks with the scope named, how well the
    /// centre is known, and on to Shots Needed to Zero or to Ballistics with the offset as its zero offset.
    /// </summary>
    internal Window ZeroFromWindow()
    {
        var state = session.State;
        var body = new StackPanel { Margin = Tokens.SectionPadding, Spacing = Tokens.Space8 };
        var window = ToolWindow("Zero from this group", body, height: 460);
        body.Children.Add(new TextBlock { Text = "Zero from this group", Classes = { AppStyles.Section } });
        var words = ResultWords.ZeroFrom(state, units, AskWhichRifle);
        if (words.Refusal is { } refusal)
        {
            body.Children.Add(Line(refusal));
        }
        else
        {
            foreach (string axis in words.Axes)
            {
                body.Children.Add(UnitTap.Attach(new TextBlock { Text = axis, TextWrapping = TextWrapping.Wrap }));
            }

            body.Children.Add(Note(words.Scope));
            body.Children.Add(Note(words.HowWell));
            body.Children.Add(new TextBlock { Text = words.Verdict, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap });
        }

        body.Children.Add(Row(
            Button("Open in Shots Needed to Zero", () =>
            {
                window.Close();
                fullFiguresPanel.IsExpanded = true;
                BringShotsToZeroIntoView();
            }),
            Button("Use as the zero offset in Ballistics", () =>
            {
                window.Close();
                UseZeroOffset(state);
            }),
            Button("Close", window.Close)));
        return window;
    }

    /// <summary>Ballistics with this group's offset in its dope at every range, the phone's "Use as the zero offset in Ballistics".</summary>
    internal void UseZeroOffset(MarkingState state)
    {
        ballisticsZeroOffset = ResultWords.ZeroOffsetFor(state, units);
        status.Text = ballisticsZeroOffset is null
            ? "This group has no zero offset to carry: it needs the shot distance and enough shots about an aim point."
            : "Ballistics' dope now includes this group's zero offset.";
        Go(Destination.Ballistics);
        FillDope();
    }

    /// <summary>
    /// Share A, board ShareA: the picture with its results box, the lines chosen on the right, the mean radius circle about the group's center,
    /// a label, the box's style and the crop, and "Save picture", the desktop's gallery.
    /// </summary>
    internal Window ShareWindow()
    {
        var state = session.State;
        var body = new StackPanel { Margin = Tokens.SectionPadding, Spacing = Tokens.Space8 };
        var window = ToolWindow("Share a picture", body, width: 1040, height: 760);
        body.Children.Add(new TextBlock { Text = "Share a picture", Classes = { AppStyles.Section } });
        var (bitmap, frameWidth, frameHeight) = canvas.Frame;
        if (bitmap is null)
        {
            body.Children.Add(Line("This target has no picture open to share."));
            body.Children.Add(Row(Button("Close", window.Close)));
            return window;
        }

        string title = (plotDefinition?.Name ?? "Marked by hand") + (state.SheetLabel is { Length: > 0 } label ? ", " + label : "");
        string date = (currentSession is { } id ? sessions?.Get(id)?.ShotDate : null) ?? DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var lines = ShareCard.Lines(state, title, date, units);
        var chosen = lines.Where(l => l.Shown).Select(l => l.Key).ToHashSet();
        var picture = new SharePicture(bitmap, frameWidth, frameHeight, state.ViewQuarterTurns) { Circles = ShareCard.Circles(state), MaxHeight = 600 };
        SharePictureShown = picture;
        void ShowLines()
        {
            picture.Lines = [.. lines.Where(l => chosen.Contains(l.Key)).Select(l => l.Text)];
            picture.InvalidateVisual();
        }

        var side = new StackPanel { Spacing = Tokens.Space4, Width = 300 };
        side.Children.Add(FieldLabel("The box's lines"));
        foreach (var line in lines)
        {
            var box = new CheckBox { Content = new TextBlock { Text = line.Text, TextWrapping = TextWrapping.Wrap }, IsChecked = chosen.Contains(line.Key) };
            string key = line.Key;
            box.IsCheckedChanged += (_, _) =>
            {
                _ = box.IsChecked == true ? chosen.Add(key) : chosen.Remove(key);
                ShowLines();
            };
            side.Children.Add(box);
        }

        var circle = new CheckBox { Content = "Mean radius circle", IsChecked = true };
        circle.IsCheckedChanged += (_, _) =>
        {
            picture.ShowCircle = circle.IsChecked == true;
            picture.InvalidateVisual();
        };
        var labelBox = new TextBox { PlaceholderText = "A label across the top", [Avalonia.Automation.AutomationProperties.NameProperty] = "Label across the top", Text = state.SheetLabel ?? "" };
        var labelOn = new CheckBox { Content = "Label", IsChecked = false };
        void ShowLabel()
        {
            picture.Label = labelOn.IsChecked == true ? labelBox.Text : null;
            picture.InvalidateVisual();
        }

        labelOn.IsCheckedChanged += (_, _) => ShowLabel();
        labelBox.TextChanged += (_, _) => ShowLabel();
        var style = new ComboBox { [Avalonia.Automation.AutomationProperties.NameProperty] = "Results box style", ItemsSource = new[] { "Box: dark", "Box: light", "Box: words only" }, SelectedIndex = 0, HorizontalAlignment = HorizontalAlignment.Stretch };
        style.SelectionChanged += (_, _) =>
        {
            picture.Style = (ShareBoxStyle)Math.Max(0, style.SelectedIndex);
            picture.InvalidateVisual();
        };
        var crop = new ComboBox { [Avalonia.Automation.AutomationProperties.NameProperty] = "Crop", ItemsSource = new[] { "Crop: whole picture", "Crop: around the group" }, SelectedIndex = 0, HorizontalAlignment = HorizontalAlignment.Stretch };
        crop.SelectionChanged += (_, _) =>
        {
            picture.Crop = crop.SelectedIndex == 1 ? ShareCard.GroupArea(state, frameWidth, frameHeight) : null;
            picture.InvalidateMeasure();
            picture.InvalidateVisual();
        };
        side.Children.Add(FieldLabel("On the picture"));
        side.Children.Add(circle);
        side.Children.Add(labelOn);
        side.Children.Add(labelBox);
        side.Children.Add(style);
        side.Children.Add(crop);
        var said = Line("");
        side.Children.Add(Row(Button("Save picture…", async () => said.Text = await SavePicture(window, picture, title + " " + date)), Button("Close", window.Close)));
        side.Children.Add(said);
        ShowLines();

        var pictureColumn = new StackPanel { Spacing = Tokens.Space4 };
        pictureColumn.Children.Add(picture);
        pictureColumn.Children.Add(Note("Drag the box anywhere; drag its corner to resize it; the lines it carries are chosen on the right."));
        var columns = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = Tokens.Space12 };
        Grid.SetColumn(side, 1);
        columns.Children.Add(pictureColumn);
        columns.Children.Add(side);
        body.Children.Add(columns);
        return window;
    }

    /// <summary>The picture in the share window, for the headless tests.</summary>
    internal SharePicture? SharePictureShown { get; private set; }

    /// <summary>"Save picture": the PNG saved where the person chooses; the sentence to show.</summary>
    private static async Task<string> SavePicture(Window owner, SharePicture picture, string name)
    {
        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save the picture",
            SuggestedFileName = string.Concat(name.Split(Path.GetInvalidFileNameChars())).Trim() + ".png",
            DefaultExtension = "png",
        });
        if (file?.TryGetLocalPath() is not { } path)
        {
            return "";
        }

        byte[] png = picture.Png();
        await File.WriteAllBytesAsync(path, png);
        DiagnosticLog.Info("file.save", [.. DiagnosticLog.File(path), ("kind", "share-picture"), ("bytes", png.Length)]);
        return "Saved to " + path;
    }

    /// <summary>The one-page report of the marking as it stands: the phone's page, with this computer's paper.</summary>
    internal OnePageReport BuildOnePageReport()
    {
        var state = session.State;
        var saved = currentSession is { } id ? sessions?.Get(id) : null;
        string title = (plotDefinition?.Name ?? (state.ImagePath is { } image ? Path.GetFileName(image) : "Marked by hand"))
            + (state.SheetLabel is { Length: > 0 } label ? ", " + label : "");
        string date = saved?.ShotDate ?? DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var picture = state.ImagePath is { } path ? ReportPicture.From(path, state.ViewQuarterTurns) : null;
        var paper = OnePageReports.PaperFor(AppSettingsStore.LetterRegion(AppSettingsStore.Region()));
        return OnePageReports.For(state, title, date, units, paper, picture, "GroupLab " + AppInfo.Version);
    }

    /// <summary>The one-page report, saved where the person chooses.</summary>
    private async Task OnePageReportDialog()
    {
        DiagnosticLog.Info("dialog.open", ("dialog", "one-page-report"));
        var report = BuildOnePageReport();
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save the one-page report",
            SuggestedFileName = OnePageReports.FileName(report) + ".pdf",
            DefaultExtension = "pdf",
        });
        DiagnosticLog.Info("dialog.result", ("dialog", "one-page-report"), ("chosen", file is not null));
        if (file?.TryGetLocalPath() is { } path)
        {
            WriteOnePageReport(path, report);
        }
    }

    /// <summary>Writes the one-page report, and says where; also the headless tests' way in.</summary>
    internal void WriteOnePageReport(string path, OnePageReport? report = null)
    {
        byte[] pdf = OnePageReports.Write(report ?? BuildOnePageReport());
        File.WriteAllBytes(path, pdf);
        status.Text = "One-page report saved to " + path;
        DiagnosticLog.Info("file.save", [.. DiagnosticLog.File(path), ("kind", "one-page-report"), ("bytes", pdf.Length)]);
    }

    /// <summary>The report button: the full report, or the one page (entry 280 section 2, board Report), beside it.</summary>
    private Button ReportButton()
    {
        var menu = new MenuFlyout();
        foreach (var (words, action) in new (string, Func<Task>)[]
        {
            ("Full report…", ReportDialog),
            ("One-page report…", OnePageReportDialog),
        })
        {
            var item = new MenuItem { Header = words };
            item.Click += async (_, _) => await action();
            menu.Items.Add(item);
        }

        var button = new Button { Content = "Report", Flyout = menu, Margin = new Thickness(2) };
        ToolTip.SetTip(button, "The full report, every figure and every why, or one page with the picture, the plot and the figures");
        return button;
    }
}

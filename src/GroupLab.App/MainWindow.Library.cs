using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Rendering;
using GroupLab.Core.Trace;

namespace GroupLab.App;

/// <summary>
/// The target library, NOTES-FROM-PLANNING.md entry 112 section 3, the rail's destination the concept's library-and-print screen draws: the
/// built-in sheets, read only, grouped by family, and the person's own sheets from the designer after them. An own sheet can be renamed,
/// duplicated and deleted after asking; a built-in one can be duplicated as the start of an own one. Printing is the print screen's, opened
/// on the chosen sheet, so an own sheet prints through exactly the refusals a built-in one does.
/// A sheet a session was analysed against stays readable because every session keeps its own copy of the definition, which Accept and
/// analyse stores and reopening reads; so deleting a sheet is allowed, and the question before it says how many sessions used it and that
/// each keeps its copy. Refusing instead would leave a sheet undeletable for as long as any session of it is kept.
/// </summary>
public sealed partial class MainWindow
{
    private readonly StackPanel libraryList = new() { Spacing = 0 };
    private readonly StackPanel libraryDetail = new() { Spacing = Tokens.Space8 };

    // Entry 120 section 10.2: the preview fills everything under the detail, keeping its aspect, and grows with the window. Stretch rather
    // than a fixed box, and no MaxHeight, because a fixed box left two thirds of the window empty.
    private readonly Image libraryPreview = new()
    {
        Stretch = Stretch.Uniform,
        StretchDirection = StretchDirection.Both,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Stretch,
    };

    private readonly ScrollViewer libraryPreviewHost = new()
    {
        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
    };

    private readonly Panel libraryPreviewFrame = new();
    private readonly Grid libraryPreviewArea = new();
    private readonly Grid librarySplit = new();
    private double libraryZoom;
    private LibrarySheet? librarySelected;
    private IReadOnlyList<LibrarySheet>? builtInSheets;

    /// <summary>
    /// The narrowest the list column may be, entry 120 section 10.1: wide enough for the longest built-in name beside its paper and bulls at
    /// the current font, so nothing is cut at the smallest window the application supports. A name longer than this wraps to a second line.
    /// </summary>
    internal const double LibraryListWidth = 520;

    /// <summary>What the status line says on the library screen. It used to say the marking screen's words about zooming with buttons that were not there.</summary>
    internal const string LibraryStatus = "The built-in sheets and your own. Choose one on the left to see it whole; the buttons under it zoom the preview.";

    /// <summary>
    /// The library screen, entry 120 section 10: it fills the window. A heading and one line at the top, then the list and the sheet side by
    /// side, with a splitter between them whose width is remembered, and the preview taking everything left over.
    /// </summary>
    private Control BuildLibrary()
    {
        var head = new StackPanel { Spacing = Tokens.Space12 };
        head.Children.Add(new TextBlock { Text = "Target library", Classes = { AppStyles.Title } });
        head.Children.Add(Line("The built-in sheets, read only, and your own sheets from the designer. Print opens the print screen on the sheet chosen, where every sheet prints the same way."));
        head.Children.Add(Row(Button("Design your own sheet", () => OpenPrint(null, design: true))));

        double width = Math.Max(LibraryListWidth, settingsStore.LoadColumnWidth("library") ?? LibraryListWidth);
        librarySplit.ColumnDefinitions = new ColumnDefinitions(string.Create(CultureInfo.InvariantCulture, $"{width},Auto,*"));
        librarySplit.ColumnDefinitions[0].MinWidth = LibraryListWidth;
        librarySplit.ColumnDefinitions[2].MinWidth = 320;

        var listScroll = new ScrollViewer { Content = libraryList, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        var splitter = new GridSplitter { Width = Tokens.Space8, ResizeDirection = GridResizeDirection.Columns, Cursor = new Cursor(StandardCursorType.SizeWestEast) };
        Grid.SetColumn(splitter, 1);
        splitter.DragCompleted += (_, _) => settingsStore.SaveColumnWidth("library", librarySplit.ColumnDefinitions[0].ActualWidth);

        // The sheet: what it is and what can be done with it at the top, the preview filling everything below.
        var detail = new Grid { RowDefinitions = new RowDefinitions("Auto,*"), Margin = new Thickness(Tokens.Space16, 0, 0, 0) };
        detail.Children.Add(libraryDetail);


        var preview = libraryPreviewArea;
        preview.RowDefinitions = new RowDefinitions("*,Auto");
        preview.Children.Add(libraryPreview);
        var zoom = Row(
            Button("Zoom in", () => SetLibraryZoom(libraryZoom <= 0 ? 1.25 : libraryZoom * 1.25)),
            Button("Zoom out", () => SetLibraryZoom(libraryZoom <= 0 ? 0.8 : libraryZoom * 0.8)),
            Button("Fit", () => SetLibraryZoom(0)));
        zoom.Margin = new Thickness(0, Tokens.Space8, 0, 0);
        Grid.SetRow(zoom, 1);
        preview.Children.Add(zoom);
        Grid.SetRow(preview, 1);
        detail.Children.Add(preview);

        Grid.SetColumn(detail, 2);
        librarySplit.Children.Add(listScroll);
        librarySplit.Children.Add(splitter);
        librarySplit.Children.Add(detail);

        var whole = new Grid { RowDefinitions = new RowDefinitions("Auto,*"), Margin = new Thickness(Tokens.Space24, Tokens.Space20) };
        whole.Children.Add(head);
        Grid.SetRow(librarySplit, 1);
        librarySplit.Margin = new Thickness(0, Tokens.Space12, 0, 0);
        whole.Children.Add(librarySplit);
        whole.IsVisible = false;
        return whole;
    }

    /// <summary>
    /// Zooms the preview, entry 120 section 10.3, which is what the status line promises. Zero means fit the page whole, which is where it
    /// starts and where Fit puts it back.
    /// </summary>
    private void SetLibraryZoom(double scale)
    {
        libraryZoom = scale <= 0 ? 0 : Math.Clamp(scale, 0.2, 8);
        // Fitting: the image sits straight in the grid row, whose height and width come down the chain of stars from the window, so it
        // fills whatever room there is. Inside a scroll viewer it measured its own natural size instead and left the window two thirds
        // empty, which is what entry 120 section 10.2 is about.
        if (libraryZoom <= 0)
        {
            Reparent(libraryPreview, libraryPreviewArea);
            libraryPreviewHost.IsVisible = false;
            libraryPreview.Width = double.NaN;
            libraryPreview.Height = double.NaN;
            libraryPreview.Stretch = Stretch.Uniform;
            return;
        }

        // Zoomed: the image goes into the scroll viewer at its chosen size, so it can be panned as well as magnified.
        if (libraryPreview.Source is { } source)
        {
            Reparent(libraryPreview, libraryPreviewFrame);
            libraryPreviewHost.Content = libraryPreviewFrame;
            libraryPreviewHost.IsVisible = true;
            if (!libraryPreviewArea.Children.Contains(libraryPreviewHost))
            {
                libraryPreviewArea.Children.Add(libraryPreviewHost);
            }

            libraryPreview.Stretch = Stretch.Uniform;
            libraryPreview.Width = source.Size.Width * libraryZoom;
            libraryPreview.Height = source.Size.Height * libraryZoom;
            libraryPreviewFrame.Width = libraryPreview.Width;
            libraryPreviewFrame.Height = libraryPreview.Height;
        }
    }

    /// <summary>Moves the preview between the grid row that fits it and the scroll viewer that pans it.</summary>
    private static void Reparent(Control child, Panel into)
    {
        if (child.Parent == into)
        {
            return;
        }

        (child.Parent as Panel)?.Children.Remove(child);
        into.Children.Add(child);
    }

    /// <summary>The list column's width, for the layout test.</summary>
    internal double LibraryListActualWidth => librarySplit.ColumnDefinitions.Count > 0 ? librarySplit.ColumnDefinitions[0].ActualWidth : 0;

    /// <summary>The preview as it is drawn, for the layout test.</summary>
    internal Rect LibraryPreviewBounds => libraryPreview.Bounds;

    /// <summary>The room the preview has, for the layout test.</summary>
    internal Rect LibraryPreviewArea => libraryPreviewArea.Bounds;

    /// <summary>The split between the list and the sheet, for the layout test.</summary>
    internal Rect LibrarySplitBounds => librarySplit.Bounds;

    /// <summary>Every sheet the library lists: the built-in ones in the catalogue's order, then the person's own by name.</summary>
    private IReadOnlyList<LibrarySheet> LibrarySheets()
    {
        builtInSheets ??= PrintWindow.BuiltIn();
        return [.. builtInSheets, .. ownSheets.List()];
    }

    /// <summary>Fills the list, each family under its heading, keeping the chosen sheet chosen where it still exists.</summary>
    private void FillLibrary()
    {
        libraryList.Children.Clear();
        var all = LibrarySheets();
        librarySelected = librarySelected is { } chosen ? all.FirstOrDefault(s => s.File == chosen.File && s.Family == chosen.Family) : null;
        librarySelected ??= all.FirstOrDefault();
        foreach (var family in all.GroupBy(s => s.Family))
        {
            libraryList.Children.Add(Heading(family.Key));
            int index = 0;
            foreach (var sheet in family)
            {
                // Entry 120 section 10.1: a grid rather than a dock, so the name has its own column and wraps inside it. A dock let the
                // name's own width run under the paper and bulls, which read as "100 m, A4A4 . 25 + 3" on one row.
                var cells = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
                var detail = new TextBlock
                {
                    Text = ShortPaper(sheet),
                    FontFamily = Mono,
                    FontSize = Tokens.DetailSize,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(Tokens.Space12, 0, 0, 0),
                    Classes = { AppStyles.Dim },
                };
                Grid.SetColumn(detail, 1);
                cells.Children.Add(new TextBlock { Text = sheet.Definition.Name, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center });
                cells.Children.Add(detail);
                var row = new Button
                {
                    Content = cells,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    FontWeight = sheet == librarySelected ? FontWeight.SemiBold : FontWeight.Normal,
                    Classes = { AppStyles.TableRow },
                };
                if (index++ % 2 == 1)
                {
                    row.Classes.Add(AppStyles.Shaded);
                }

                if (sheet == librarySelected)
                {
                    row.Classes.Add(AppStyles.Warn);
                }

                ToolTip.SetTip(row, sheet.Summary);
                var picked = sheet;
                row.Click += (_, _) =>
                {
                    librarySelected = picked;
                    FillLibrary();
                };
                libraryList.Children.Add(row);
            }
        }

        if (all.All(s => s.Family != OwnSheets.Family))
        {
            libraryList.Children.Add(Heading(OwnSheets.Family));
            libraryList.Children.Add(Line("None yet. Design your own sheet makes one, and so does Duplicate on any sheet."));
        }

        ShowLibrarySheet();
    }

    private static string ShortPaper(LibrarySheet sheet) =>
        string.Create(CultureInfo.InvariantCulture, $"{sheet.Paper.Split(',')[0]} \u00b7 {sheet.Definition.Bulls.Count(b => b.Scoring)}{(sheet.Definition.Bulls.Count(b => !b.Scoring) is > 0 and var sighters ? $" + {sighters}" : "")}");

    /// <summary>The chosen sheet: its name, what it is, its identifier, what can be done with it, and its artwork.</summary>
    private void ShowLibrarySheet()
    {
        libraryDetail.Children.Clear();
        (libraryPreview.Source as IDisposable)?.Dispose();
        libraryPreview.Source = null;
        if (librarySelected is not { } sheet)
        {
            libraryDetail.Children.Add(Line("The built-in library was not found beside the application."));
            return;
        }

        bool mine = sheet.Family == OwnSheets.Family;
        string? id = GltdBinary.Encode(sheet.Definition).Encoding?.DefinitionId;
        libraryDetail.Children.Add(new TextBlock { Text = sheet.Definition.Name, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Title } });
        libraryDetail.Children.Add(Line(sheet.Summary));
        libraryDetail.Children.Add(Detail(id is null ? sheet.File : $"{id}  \u00b7  {sheet.File}"));
        libraryDetail.Children.Add(Line(mine
            ? "One of your own sheets, kept in GroupLab's data folder."
            : "Built in and read only. Duplicate makes a copy of your own, to rename and keep beside it."));

        var actions = Row(Button("Print\u2026", () => OpenPrint(sheet, design: false)), Button("Duplicate", () => DuplicateSheet(sheet)));
        libraryDetail.Children.Add(actions);
        if (mine)
        {
            var name = new TextBox { Text = sheet.Definition.Name, Width = 280 };
            Avalonia.Automation.AutomationProperties.SetName(name, "New name");
            libraryDetail.Children.Add(Row(FieldLabel("Name"), name, Button("Rename", () => RenameSheet(sheet, name.Text ?? ""))));
            var delete = Button("Delete\u2026", () => { });
            var deleteRow = Row(delete);
            delete.Click += (_, _) =>
            {
                deleteRow.Children.Clear();
                deleteRow.Children.Add(new TextBlock { Text = DeleteQuestion(sheet, id), TextWrapping = TextWrapping.Wrap, MaxWidth = 520, VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.Warn } });
                deleteRow.Children.Add(Button("Delete", () => DeleteSheet(sheet)));
                deleteRow.Children.Add(Button("Keep it", FillLibrary));
            };
            libraryDetail.Children.Add(deleteRow);
        }

        libraryPreview.Source = PrintWindow.Preview(sheet.Definition);
        SetLibraryZoom(libraryZoom);
    }

    /// <summary>The question before a delete, which says how many sessions used the sheet and that each keeps its own copy.</summary>
    private string DeleteQuestion(LibrarySheet sheet, string? id)
    {
        int used = id is null || sessions is null ? 0 : sessions.CountUsing(id);
        return $"Delete {sheet.Definition.Name}? " + used switch
        {
            0 => "No session was analysed against it.",
            1 => "One session was analysed against it. It keeps its own copy of the sheet, so it stays readable.",
            _ => $"{used} sessions were analysed against it. Each keeps its own copy of the sheet, so they stay readable.",
        };
    }

    private void DuplicateSheet(LibrarySheet sheet)
    {
        librarySelected = ownSheets.Duplicate(sheet);
        DiagnosticLog.Info("sheet.duplicate", ("sheet", sheet.File), ("copy", librarySelected.File));
        status.Text = $"Duplicated as {librarySelected.Definition.Name}, in your own sheets.";
        FillLibrary();
    }

    private void RenameSheet(LibrarySheet sheet, string name)
    {
        try
        {
            librarySelected = ownSheets.Rename(sheet, name);
            DiagnosticLog.Info("sheet.rename", ("sheet", sheet.File));
            status.Text = $"Renamed to {librarySelected.Definition.Name}.";
            FillLibrary();
        }
        catch (ArgumentException ex)
        {
            status.Text = ex.Message.Split(" (Parameter", StringSplitOptions.None)[0];
        }
    }

    private void DeleteSheet(LibrarySheet sheet)
    {
        ownSheets.Delete(sheet);
        DiagnosticLog.Info("sheet.delete", ("sheet", sheet.File));
        status.Text = $"Deleted {sheet.Definition.Name}.";
        librarySelected = null;
        FillLibrary();
    }

    /// <summary>The print screen, on a chosen sheet or in its designer, with the person's own sheets listed and a design saved into them.</summary>
    private PrintWindow OpenPrint(LibrarySheet? sheet, bool design)
    {
        var print = new PrintWindow(ownSheets);
        print.SheetsChanged += () =>
        {
            if (destination == Destination.Library)
            {
                FillLibrary();
            }
        };
        print.Show();
        if (design)
        {
            print.Design();
        }
        else if (sheet is not null)
        {
            print.Select(sheet.File);
        }

        return print;
    }

    /// <summary>Shows or leaves the target library.</summary>
    internal void ShowLibrary(bool on = true) => Go(on ? Destination.Library : Destination.Analyse);

    internal bool ShowingLibrary => destination == Destination.Library;

    /// <summary>The library's rows as their sheet names under their families, for the headless tests.</summary>
    internal IReadOnlyList<string> LibraryRows =>
        [.. libraryList.Children.Select(c => c switch
        {
            TextBlock heading => "# " + heading.Text,
            Button row => ((Panel)row.Content!).Children.OfType<TextBlock>().First().Text ?? "",
            _ => "",
        })];

    /// <summary>The chosen sheet's panel, for the headless tests.</summary>
    internal StackPanel LibraryDetail => libraryDetail;

    internal LibrarySheet? LibrarySelected => librarySelected;

    internal OwnSheets OwnSheets => ownSheets;

    /// <summary>Chooses a sheet in the library by its name, for the headless tests.</summary>
    internal void ChooseLibrarySheet(string name)
    {
        librarySelected = LibrarySheets().First(s => s.Definition.Name == name);
        FillLibrary();
    }

    /// <summary>Opens the print screen from the library as its Print does, for the headless tests.</summary>
    internal PrintWindow PrintFromLibrary() => OpenPrint(librarySelected, design: false);

    /// <summary>The texts of the chosen sheet's panel, for the headless tests.</summary>
    internal IEnumerable<string> LibraryDetailText => libraryDetail.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "");
}

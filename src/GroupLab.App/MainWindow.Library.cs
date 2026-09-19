using System.Globalization;
using Avalonia;
using Avalonia.Controls;
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
    private readonly Image libraryPreview = new() { Stretch = Stretch.Uniform, MaxHeight = 540, HorizontalAlignment = HorizontalAlignment.Left };
    private LibrarySheet? librarySelected;
    private IReadOnlyList<LibrarySheet>? builtInSheets;

    private Control BuildLibrary()
    {
        var column = new StackPanel { Margin = new Thickness(Tokens.Space24, Tokens.Space20), Spacing = Tokens.Space12 };
        column.Children.Add(new TextBlock { Text = "Target library", Classes = { AppStyles.Title } });
        column.Children.Add(Line("The built-in sheets, read only, and your own sheets from the designer. Print opens the print screen on the sheet chosen, where every sheet prints the same way."));
        column.Children.Add(Row(Button("Design your own sheet", () => OpenPrint(null, design: true))));
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("340,*") };
        libraryList.Margin = new Thickness(0, 0, Tokens.Space24, 0);
        Grid.SetColumn(libraryDetail, 1);
        grid.Children.Add(libraryList);
        grid.Children.Add(libraryDetail);
        column.Children.Add(grid);
        return new ScrollViewer { Content = column, IsVisible = false };
    }

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
                var cells = new DockPanel();
                var detail = new TextBlock { Text = ShortPaper(sheet), FontFamily = Mono, FontSize = Tokens.DetailSize, VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.Dim } };
                DockPanel.SetDock(detail, Dock.Right);
                cells.Children.Add(detail);
                cells.Children.Add(new TextBlock { Text = sheet.Definition.Name, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center });
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
        libraryDetail.Children.Add(libraryPreview);
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
            Button row => ((DockPanel)row.Content!).Children.OfType<TextBlock>().Last().Text ?? "",
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

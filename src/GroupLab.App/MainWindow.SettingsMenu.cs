using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using System.Globalization;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Marking;
using GroupLab.Core.Updates;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 468 section 1, concept A, Alan: Settings "is very cluttered". A side menu of eight categories with a search
/// box at its top, one page at a time on the right, each page cards of rows with the name on the left and its control on the right, and
/// "Put this page back as it came" at the end of each. Every setting keeps its stored value and meaning; only where it is shown moved.
/// </summary>
public partial class MainWindow
{
    private readonly SettingsPages settingsPages = new();
    private readonly StackPanel settingsMenu = new() { Spacing = Tokens.Space4 };
    private readonly List<Button> settingsMenuItems = [];
    private readonly TextBox settingsSearch = new() { PlaceholderText = SettingsMenu.FindHint };
    private readonly TextBlock settingsNothing = new() { Text = SettingsMenu.NothingFound, TextWrapping = TextWrapping.Wrap, IsVisible = false, Classes = { AppStyles.Secondary } };
    private readonly ScrollViewer settingsScroll = new();

    /// <summary>The category whose page is shown, for the headless tests.</summary>
    internal SettingsCategory SettingsPage => settingsPages.Current;

    /// <summary>Shows Settings at <paramref name="category"/>'s page, as choosing it in the side menu does.</summary>
    internal void ShowSettings(SettingsCategory category)
    {
        ShowSettings();
        ShowSettingsPage(category);
    }

    /// <summary>Searches Settings as typing in "Find a setting" does; the number of rows found.</summary>
    internal int FindSetting(string words)
    {
        settingsSearch.Text = words;
        SearchSettings();
        return settingsPages.Query is null ? 0 : CountFound();
    }

    /// <summary>Shows what "Find a setting" holds: the matching rows of every page, or the page that was showing when it is empty.</summary>
    private void SearchSettings()
    {
        int found = settingsPages.Filter(settingsSearch.Text);
        settingsNothing.IsVisible = settingsPages.Query is not null && found == 0;
        MarkSettingsMenu(settingsPages.Query is null ? settingsPages.Current : null);
        if (settingsPages.Query is null)
        {
            settingsPages.Show(settingsPages.Current);
        }

        settingsScroll.Offset = default;
    }

    private int CountFound() => settingsPages.Pages.Sum(p => p.Cards.Sum(c => c.Root.IsVisible ? Math.Max(1, c.Rows.Count(r => r.Row.IsVisible)) : 0));

    private void ShowSettingsPage(SettingsCategory category)
    {
        settingsSearch.Text = "";
        settingsPages.Show(category);
        settingsNothing.IsVisible = false;
        MarkSettingsMenu(category);

        settingsStore.SaveSettingsPage(category);
        settingsScroll.Offset = default;
    }

    private Control BuildSettings(AppSettingsStore settings)
    {
        var pagesColumn = new StackPanel { Margin = new Thickness(Tokens.Space24, Tokens.Space20), Spacing = Tokens.Space20, MaxWidth = 820, HorizontalAlignment = HorizontalAlignment.Left };
        pagesColumn.Children.Add(settingsNothing);
        BuildGeneralPage(pagesColumn);
        BuildUnitsPage(pagesColumn);
        BuildShootingPage(pagesColumn);
        BuildPrintersPage(pagesColumn);
        BuildDataPage(pagesColumn, settings);
        BuildSharingPage(pagesColumn);
        BuildUpdatesPage(pagesColumn);
        BuildHelpPage(pagesColumn, settings);
        settingsScroll.Content = pagesColumn;

        // The side menu: a list reachable by Tab and moved through with Up and Down, each category named to a screen reader.
        foreach (var category in SettingsMenu.All)
        {
            var item = new Button
            {
                Content = new TextBlock { Text = SettingsMenu.Name(category), TextWrapping = TextWrapping.Wrap },
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(Tokens.Space8, Tokens.Space8),
                Classes = { AppStyles.TableRow },
            };
            Avalonia.Automation.AutomationProperties.SetName(item, SettingsMenu.Name(category));
            item.Click += (_, _) => ShowSettingsPage(category);
            // Handled ones too: the window's own arrow-key focus moving must not take the key before the menu does.
            item.AddHandler(KeyDownEvent, (_, e) =>
            {
                int step = e.Key switch { Key.Down => 1, Key.Up => -1, _ => 0 };
                int to = (int)category + step;
                if (step != 0 && to >= 0 && to < settingsMenuItems.Count)
                {
                    ShowSettingsPage((SettingsCategory)to);
                    settingsMenuItems[to].Focus(NavigationMethod.Directional);
                    e.Handled = true;
                }
            }, Avalonia.Interactivity.RoutingStrategies.Bubble, handledEventsToo: true);
            settingsMenuItems.Add(item);
            settingsMenu.Children.Add(item);
        }

        Avalonia.Automation.AutomationProperties.SetName(settingsMenu, "Settings categories");
        Avalonia.Automation.AutomationProperties.SetName(settingsSearch, SettingsMenu.Find);
        settingsSearch.TextChanged += (_, _) => SearchSettings();
        settingsSearch.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && !string.IsNullOrEmpty(settingsSearch.Text))
            {
                settingsSearch.Text = "";
                e.Handled = true;
            }
        };

        var side = new DockPanel { Margin = new Thickness(Tokens.Space12, Tokens.Space20, Tokens.Space12, Tokens.Space12) };
        var find = new StackPanel { Spacing = Tokens.Space4, Margin = new Thickness(0, 0, 0, Tokens.Space12), Children = { new TextBlock { Text = SettingsMenu.Find, Classes = { AppStyles.Secondary } }, settingsSearch } };
        DockPanel.SetDock(find, Dock.Top);
        side.Children.Add(find);
        side.Children.Add(settingsMenu);
        var sideBorder = new Border { Child = side, BorderThickness = new Thickness(0, 0, 1, 0), Classes = { AppStyles.Ruled } };

        var remembered = new TextBlock { Text = SettingsMenu.Remembered, Margin = new Thickness(Tokens.Space20, Tokens.Space8), TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } };
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("240,*"), RowDefinitions = new RowDefinitions("*,Auto"), IsVisible = false };
        Grid.SetColumn(settingsScroll, 1);
        var footer = new Border { BorderThickness = new Thickness(0, 1, 0, 0), Child = remembered, Classes = { AppStyles.Ruled } };
        Grid.SetRow(footer, 1);
        Grid.SetColumnSpan(footer, 2);
        grid.Children.Add(sideBorder);
        grid.Children.Add(settingsScroll);
        grid.Children.Add(footer);

        // Entry 468 section 1: the last category shown is remembered.
        var last = settings.LoadSettingsPage();
        settingsPages.Show(last);
        MarkSettingsMenu(last);
        return grid;
    }

    /// <summary>The side menu's row for <paramref name="category"/> marked as the page shown, and no other.</summary>
    private void MarkSettingsMenu(SettingsCategory? category)
    {
        for (int i = 0; i < settingsMenuItems.Count; i++)
        {
            settingsMenuItems[i].Classes.Set(AppStyles.SelectedRow, category is { } c && (int)c == i);
        }
    }

    /// <summary>A page: its title, its one line, and "Put this page back as it came" at its end, added after its cards.</summary>
    private (StackPanel Body, SettingsPages.Page Page) SettingsPageOf(StackPanel column, SettingsCategory category)
    {
        var body = new StackPanel { Spacing = Tokens.Space16 };
        var intro = Line(SettingsMenu.Intro(category));
        var head = new StackPanel { Spacing = Tokens.Space4, Children = { new TextBlock { Text = SettingsMenu.Name(category), Classes = { AppStyles.Title } }, intro } };
        body.Children.Add(head);
        column.Children.Add(body);
        return (body, settingsPages.Add(category, body, intro));
    }

    /// <summary>Ends a page with "Put this page back as it came", which asks first and touches no data.</summary>
    private void EndSettingsPage(StackPanel body, SettingsPages.Page page)
    {
        var link = Link(SettingsMenu.PutBack, () => AskToPutSettingsPageBack(page.Category));
        link.HorizontalAlignment = HorizontalAlignment.Left;
        body.Children.Add(link);
        page.OnItsOwn(link);
    }

    private void AskToPutSettingsPageBack(SettingsCategory category) =>
        ShowProblem(SettingsMenu.PutBack + "?", SettingsMenu.PutBackAsks(category),
            (SettingsMenu.PutBack, () => PutSettingsPageBack(category)),
            ("Leave it as it is", () => { }));

    /// <summary>Puts <paramref name="category"/>'s page back as it came, without asking, for the headless tests and after the question.</summary>
    internal void PutSettingsPageBack(SettingsCategory category)
    {
        settingsPages.PutBack(category);
        DiagnosticLog.Info("settings.put-back", ("page", category.ToString()));
        toaster.Say(SettingsMenu.PutBackSaid(category));
    }

    /// <summary>A card on <paramref name="page"/>, with its heading where it has one.</summary>
    private (StackPanel Inside, SettingsPages.Card Card) SettingsCard(StackPanel body, SettingsPages.Page page, string heading, string words = "")
    {
        var inside = new StackPanel { Spacing = Tokens.Space8 };
        if (heading.Length > 0)
        {
            inside.Children.Add(new TextBlock { Text = heading, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Section } });
        }

        var border = new Border { Child = inside, Classes = { AppStyles.ResultCard } };
        body.Children.Add(border);
        return (inside, page.Card(border, heading, words));
    }

    /// <summary>
    /// A row: the name on the left, at most one short line of help under it, and the control on the right, found by its name, its help and
    /// <paramref name="words"/>.
    /// </summary>
    private Control SettingRow(StackPanel inside, SettingsPages.Card card, string name, string? help, Control control, string words = "")
    {
        var label = FieldLabel(name);
        label.TextWrapping = TextWrapping.Wrap;
        label.VerticalAlignment = VerticalAlignment.Center;
        control.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(control, 1);
        Grid row;
        if (help is null)
        {
            row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = Tokens.Space16, Children = { label, control } };
        }
        else
        {
            var left = new StackPanel { Spacing = Tokens.Space4, VerticalAlignment = VerticalAlignment.Center, Children = { label, Line(help) } };
            row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = Tokens.Space16, Children = { left, control } };
        }

        // A box or a switch is named by its row; a button already says what it does.
        if (control is not Avalonia.Controls.Button { Content: string } && Avalonia.Automation.AutomationProperties.GetName(control) is null)
        {
            Avalonia.Automation.AutomationProperties.SetName(control, name);
        }
        var holder = new Border { Child = row, Padding = new Thickness(0, Tokens.Space4) };
        inside.Children.Add(holder);
        return card.Row(holder, name + " " + help + " " + words);
    }

    /// <summary>A row whose choices are too long to sit beside its name: the name, its line, and the choices beneath.</summary>
    private Control SettingBlock(StackPanel inside, SettingsPages.Card card, string name, string? help, Control content, string words = "")
    {
        var label = FieldLabel(name);
        label.TextWrapping = TextWrapping.Wrap;
        var block = new StackPanel { Spacing = Tokens.Space4, Children = { label } };
        if (help is not null)
        {
            block.Children.Add(Line(help));
        }

        block.Children.Add(content);
        var holder = new Border { Child = block, Padding = new Thickness(0, Tokens.Space4) };
        inside.Children.Add(holder);
        return card.Row(holder, name + " " + help + " " + words);
    }

    private void BuildGeneralPage(StackPanel column)
    {
        var (body, page) = SettingsPageOf(column, SettingsCategory.General);
        var (look, lookCard) = SettingsCard(body, page, "Look");
        Avalonia.Automation.AutomationProperties.SetName(themeChoice, "Theme");
        SettingRow(look, lookCard, "Theme", null, themeChoice, "dark light colour color high contrast appearance");
        themeChoice.SelectionChanged += (_, _) =>
        {
            if (!showingTheme && themeChoice.SelectedIndex >= 0)
            {
                SetTheme((ThemeChoice)themeChoice.SelectedIndex);
            }
        };
        page.OnPutBack(() => SetTheme(ThemeChoice.System));

        // Entry 468 section 2.6: the text larger, through the type scale's tokens everywhere, and Reduce motion.
        var size = new ComboBox { ItemsSource = GeneralWords.TextSizeChoices, SelectedIndex = (int)settingsStore.LoadTextSize(), MinWidth = 180 };
        size.SelectionChanged += (_, _) =>
        {
            var chosen = (TextSizeChoice)Math.Max(0, size.SelectedIndex);
            settingsStore.SaveTextSize(chosen);
            TextSize.Choose(chosen);
            DiagnosticLog.Info("settings.text-size", ("size", chosen.ToString()));
        };
        SettingRow(look, lookCard, GeneralWords.TextSize, GeneralWords.TextSizeSays, size, "font bigger larger zoom read accessibility");
        page.OnPutBack(() =>
        {
            size.SelectedIndex = (int)TextSizeChoice.Normal;
            settingsStore.SaveTextSize(TextSizeChoice.Normal);
            TextSize.Choose(TextSizeChoice.Normal);
        });
        var motion = new ComboBox { ItemsSource = GeneralWords.MotionChoices, SelectedIndex = (int)settingsStore.LoadReduceMotion(), MinWidth = 180 };
        motion.SelectionChanged += (_, _) => settingsStore.SaveReduceMotion((MotionChoice)Math.Max(0, motion.SelectedIndex));
        SettingRow(look, lookCard, GeneralWords.Motion, GeneralWords.MotionSays, motion, "animation flash still accessibility");
        page.OnPutBack(() =>
        {
            motion.SelectedIndex = (int)MotionChoice.System;
            settingsStore.SaveReduceMotion(MotionChoice.System);
        });

        // Entry 439 section 1.2: the guided demo, from the start or from where it was left.
        var (demo, demoCard) = SettingsCard(body, page, "Guided demo");
        SettingBlock(demo, demoCard, DemoWords.SettingsRow, null, DemoSettingsRow(), "tour demo walk through guided sample");

        // Entry 298 section 3: every column dragged on any screen goes back to its default width at once.
        var (layout, layoutCard) = SettingsCard(body, page, "Layout");
        SettingRow(layout, layoutCard, "Reset layout", "Every split between panes can be dragged, and GroupLab remembers where you left it.", Button("Reset layout", () =>
        {
            ResetLayout();
            toaster.Say("Every pane is back to its default size.");
        }), "panes columns widths split");
        EndSettingsPage(body, page);
    }

    private void BuildUnitsPage(StackPanel column)
    {
        var (body, page) = SettingsPageOf(column, SettingsCategory.Units);

        // Entry 25 section 1 and entry 294 section 1: one application-wide unit setting on three axes, the scope's unit first, said plainly.
        var (unitsCard, units) = SettingsCard(body, page, "Units");
        Avalonia.Automation.AutomationProperties.SetName(angularUnit, UnitSettings.ScopeUnitLabel);
        SettingRow(unitsCard, units, UnitSettings.ScopeUnitLabel, UnitSettings.ScopeUnitSays, angularUnit, "MOA mil mrad SMOA IPHY angle clicks metric imperial");
        Avalonia.Automation.AutomationProperties.SetName(linearUnit, "Lengths");
        SettingRow(unitsCard, units, "Lengths", null, linearUnit, "inches millimetres millimeters centimetres metric imperial mm cm in size");
        Avalonia.Automation.AutomationProperties.SetName(distanceUnit, "Distances");
        SettingRow(unitsCard, units, "Distances", null, distanceUnit, "yards metres meters range metric imperial yd m");
        foreach (var combo in new[] { angularUnit, linearUnit, distanceUnit })
        {
            combo.SelectionChanged += (_, _) => UnitsChosen();
        }

        // Entry 189 section 3.2: SMOA explained where it is chosen, in the glossary's words.
        if (Glossary.Find("SMOA") is { } smoa)
        {
            ToolTip.SetTip(angularUnit, "SMOA: " + smoa.Plain);
        }

        page.OnPutBack(() =>
        {
            UseUnits(UnitSettings.ForRegion(AppSettingsStore.Region()));
            ShowUnits();
        });

        var (figures, figuresCard) = SettingsCard(body, page, "How figures are shown");
        // Entry 189 section 3.4: the size on the paper first, for a shooter who only shoots one distance.
        var paperFirst = new CheckBox { IsChecked = sizeOnPaperFirst };
        paperFirst.IsCheckedChanged += (_, _) =>
        {
            sizeOnPaperFirst = paperFirst.IsChecked == true;
            settingsStore.SaveSizeOnPaperFirst(sizeOnPaperFirst);
            Refresh();
        };
        SettingRow(figures, figuresCard, "Size on the paper first", "For shooting at one distance: a group's size on the paper first, its angle beneath.", paperFirst, "inches mm group size angle one distance");
        page.OnPutBack(() => paperFirst.IsChecked = false);

        // Entry 314 section 1: what the caliber box offers, the same setting as on the phone.
        var caliberChoices = new StackPanel { Spacing = Tokens.Space4 };
        var caliberRadios = new List<(CaliberList List, RadioButton Radio)>();
        foreach (var list in Enum.GetValues<CaliberList>())
        {
            var choice = new RadioButton { GroupName = "caliberList", Content = Wrapped(list.ToString()), IsChecked = caliberList == list };
            choice.IsCheckedChanged += (_, _) =>
            {
                if (choice.IsChecked == true)
                {
                    SetCaliberList(list);
                }
            };
            caliberChoices.Children.Add(choice);
            caliberRadios.Add((list, choice));
        }

        SettingBlock(figures, figuresCard, AppSettingsStore.CaliberListLabel, AppSettingsStore.CaliberListSays, caliberChoices, "calibre caliber cartridge diameter");
        page.OnPutBack(() => caliberRadios.First(r => r.List == CaliberList.Both).Radio.IsChecked = true);

        // Entry 468 section 2.4: the main figures, as before, or every figure with Advanced and the other folded ones open.
        var opens = new ComboBox { ItemsSource = DefaultsWords.AnalysisChoices, SelectedIndex = settingsStore.LoadEveryFigure() ? 1 : 0, MinWidth = 180 };
        opens.SelectionChanged += (_, _) => settingsStore.SaveEveryFigure(opens.SelectedIndex == 1);
        SettingRow(figures, figuresCard, DefaultsWords.AnalysisOpens, DefaultsWords.AnalysisOpensSays, opens, "advanced folded expanded figures open");
        page.OnPutBack(() =>
        {
            opens.SelectedIndex = 0;
            settingsStore.SaveEveryFigure(false);
        });

        // Entry 424 section 2, Alan's choice: what the zero correction says of an axis inside the scatter, the same setting as on the phone.
        var (zero, zeroCard) = SettingsCard(body, page, "Zero correction");
        var scatterRule = settingsStore.LoadInsideScatter();
        var scatterChoices = new StackPanel { Spacing = Tokens.Space4 };
        RadioButton? leaveIt = null;
        foreach (var (rule, words) in new[] { (InsideScatter.LeaveIt, ZeroWords.LeaveItChoice), (InsideScatter.ClicksAnyway, ZeroWords.AnywayChoice) })
        {
            var choice = new RadioButton { GroupName = "insideScatter", Content = Wrapped(words), IsChecked = scatterRule == rule };
            choice.IsCheckedChanged += (_, _) =>
            {
                if (choice.IsChecked == true && settingsStore.LoadInsideScatter() != rule)
                {
                    settingsStore.SaveInsideScatter(rule);
                    DiagnosticLog.Info("settings.insideScatter", ("rule", rule.ToString()));
                    Refresh();
                }
            };
            scatterChoices.Children.Add(choice);
            leaveIt ??= choice;
        }

        SettingBlock(zero, zeroCard, ZeroWords.SettingLabel, ZeroWords.SettingSays, scatterChoices, "zero clicks scatter correction dial");
        page.OnPutBack(() => leaveIt!.IsChecked = true);
        EndSettingsPage(body, page);
    }

    private void BuildShootingPage(StackPanel column)
    {
        var (body, page) = SettingsPageOf(column, SettingsCategory.Shooting);
        var (start, card) = SettingsCard(body, page, DefaultsWords.StartsWith);

        // Entry 468 section 2.2: the distance, the last one used (as before) or always the one typed here, in the distance unit chosen.
        var fixedInches = settingsStore.LoadNewTargetDistance();
        var distance = new ComboBox { ItemsSource = DefaultsWords.DistanceChoices, SelectedIndex = fixedInches is null ? 0 : 1, MinWidth = 180 };
        var fixedBox = new TextBox { Width = 90, Text = fixedInches is { } shown ? UnitSettings.DistanceFromInches(shown, units.Distance).ToString("0.###", CultureInfo.InvariantCulture) : "" };
        var fixedUnit = new TextBlock { Text = UnitSettings.Symbol(units.Distance), VerticalAlignment = VerticalAlignment.Center };
        var fixedRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = Tokens.Space8, IsVisible = fixedInches is not null, Children = { fixedBox, fixedUnit } };
        Avalonia.Automation.AutomationProperties.SetName(fixedBox, "Always this distance");
        void SaveFixed()
        {
            fixedUnit.Text = UnitSettings.Symbol(units.Distance);
            if (distance.SelectedIndex == 0)
            {
                settingsStore.SaveNewTargetDistance(null);
            }
            else if (double.TryParse(fixedBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double typed) && typed > 0)
            {
                settingsStore.SaveNewTargetDistance(UnitSettings.DistanceToInches(typed, units.Distance));
            }
        }

        distance.SelectionChanged += (_, _) =>
        {
            fixedRow.IsVisible = distance.SelectedIndex == 1;
            SaveFixed();
        };
        fixedBox.LostFocus += (_, _) => SaveFixed();
        fixedBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                SaveFixed();
            }
        };
        SettingRow(start, card, DefaultsWords.Distance, DefaultsWords.DistanceSays, new StackPanel { Spacing = Tokens.Space4, Children = { distance, fixedRow } }, "range yards metres default new target");
        Avalonia.Automation.AutomationProperties.SetName(distance, DefaultsWords.Distance);
        page.OnPutBack(() =>
        {
            distance.SelectedIndex = 0;
            fixedBox.Text = "";
            settingsStore.SaveNewTargetDistance(null);
        });

        var rounds = new ComboBox { ItemsSource = DefaultsWords.RoundsChoices, SelectedIndex = (int)settingsStore.LoadNewTargetRounds(), MinWidth = 180 };
        rounds.SelectionChanged += (_, _) => settingsStore.SaveNewTargetRounds((RoundsStart)Math.Max(0, rounds.SelectedIndex));
        SettingRow(start, card, DefaultsWords.Rounds, DefaultsWords.RoundsSays, rounds, "shots count bulls missing default new target");
        page.OnPutBack(() =>
        {
            rounds.SelectedIndex = (int)RoundsStart.Ask;
            settingsStore.SaveNewTargetRounds(RoundsStart.Ask);
        });

        var equipment = new ComboBox { ItemsSource = DefaultsWords.EquipmentChoices, SelectedIndex = settingsStore.LoadNewTargetEquipment() ? 1 : 0, MinWidth = 180 };
        equipment.SelectionChanged += (_, _) => settingsStore.SaveNewTargetEquipment(equipment.SelectedIndex == 1);
        SettingRow(start, card, DefaultsWords.Equipment, DefaultsWords.EquipmentSays, equipment, "rifle barrel load equipment default new target");
        page.OnPutBack(() =>
        {
            equipment.SelectedIndex = 0;
            settingsStore.SaveNewTargetEquipment(false);
        });

        // Shown here too; it lives in Printers and paper.
        SettingRow(start, card, DefaultsWords.Paper, DefaultsWords.PaperSays, PaperChoice(), "letter a4 sheet size print");
        page.OnPutBack(PutPaperBack);
        EndSettingsPage(body, page);
    }

    private readonly List<ComboBox> paperChoices = [];
    private bool showingPaper;

    /// <summary>Entry 468 section 2.2: Letter or A4, one setting shown on two pages, each kept showing the other's choice.</summary>
    private ComboBox PaperChoice()
    {
        var paper = new ComboBox { ItemsSource = DefaultsWords.PaperChoices, SelectedIndex = AppSettingsStore.LetterPaper() ? 0 : 1, MinWidth = 180 };
        paper.SelectionChanged += (_, _) =>
        {
            if (showingPaper || paper.SelectedIndex < 0)
            {
                return;
            }

            settingsStore.SavePaperLetter(paper.SelectedIndex == 0);
            ShowPaper();
        };
        paperChoices.Add(paper);
        return paper;
    }

    private void ShowPaper()
    {
        showingPaper = true;
        foreach (var paper in paperChoices)
        {
            paper.SelectedIndex = AppSettingsStore.LetterPaper() ? 0 : 1;
        }

        showingPaper = false;
    }

    /// <summary>The paper back to the region's own.</summary>
    private void PutPaperBack()
    {
        settingsStore.ForgetPaper();
        ShowPaper();
    }

    private void BuildPrintersPage(StackPanel column)
    {
        var (body, page) = SettingsPageOf(column, SettingsCategory.Printers);
        // Entry 271: which printer's measured scale photographs are corrected for.
        var (printers, _) = SettingsCard(body, page, "Printers", "printer scale check correction measured M834 M220");
        BuildPrinterSettings(printers, page);
        var (paper, paperCard) = SettingsCard(body, page, DefaultsWords.Paper);
        SettingRow(paper, paperCard, DefaultsWords.Paper, DefaultsWords.PaperSays, PaperChoice(), "letter a4 sheet size print");
        page.OnPutBack(PutPaperBack);
        EndSettingsPage(body, page);
    }

    private void BuildDataPage(StackPanel column, AppSettingsStore settings)
    {
        var (body, page) = SettingsPageOf(column, SettingsCategory.Data);

        // Entry 468 section 2.1, Alan's priority: a daily copy of the records, first on the page.
        BuildCopiesCard(body, page);

        // Entry 463 section 1 (1A): synchronization between devices, its own card.
        var (sync, _) = SettingsCard(body, page, Core.Sync.SyncScreens.Title, "sync synchronize devices cloud folder phone computer");
        BuildSyncSettings(sync);

        // Entry 281 section 2: how a target is saved, A by itself (the default) or B with a Save button.
        var (saving, savingCard) = SettingsCard(body, page, "Saving");
        var byItself = new RadioButton { GroupName = "saving", Content = Wrapped("By itself, as soon as a target is changed or accepted"), IsChecked = !settings.LoadSaveByHand() };
        var byHand = new RadioButton { GroupName = "saving", Content = Wrapped("With a Save button, and a question before an unsaved target is left or closed"), IsChecked = settings.LoadSaveByHand() };
        byItself.IsCheckedChanged += (_, _) =>
        {
            if (byItself.IsChecked == true)
            {
                settings.SaveSaveByHand(false);
                ShowSaved();
            }
        };
        byHand.IsCheckedChanged += (_, _) =>
        {
            if (byHand.IsChecked == true)
            {
                settings.SaveSaveByHand(true);
                ShowSaved();
            }
        };
        SettingBlock(saving, savingCard, "A target is saved", null, new StackPanel { Spacing = Tokens.Space4, Children = { byItself, byHand } }, "save button automatically");
        page.OnPutBack(() => byItself.IsChecked = true);

        // Entry 307: everything exported to one file, and imported from one.
        var (data, _) = SettingsCard(body, page, "Your data", "export import file records sessions move computer");
        BuildDataSettings(data);
        EndSettingsPage(body, page);
    }

    private void BuildSharingPage(StackPanel column)
    {
        var (body, page) = SettingsPageOf(column, SettingsCategory.Sharing);
        // Entry 299: the page that says exactly what GroupLab sends, always in view above the three sections.
        var whatIsSent = Row(Link(SharingWords.WhatIsSentLabel, () => OpenInTheBrowser(SharingWords.WhatIsSentAddress)));
        body.Children.Add(whatIsSent);
        // Entry 208 section 4: the first run screen's three questions, in the same order.
        var (sending, _) = SettingsCard(body, page, "", "sending targets share pictures consent publish privacy");
        BuildSendingSettings(sending);
        var (errors, _) = SettingsCard(body, page, "", "error reports crash send automatically privacy");
        BuildErrorSettings(errors);
        var (survey, _) = SettingsCard(body, page, "", "hardware survey benchmark speed computer privacy");
        BuildSurveySettings(survey);
        EndSettingsPage(body, page);
    }

    private void BuildUpdatesPage(StackPanel column)
    {
        var (body, page) = SettingsPageOf(column, SettingsCategory.Updates);

        // Entry 119 section 6.2: the train, how often to look, what happened last time, and what a check sends. Nothing here reaches the
        // network by itself; Check now is the only button that would, and it says what it found in the line below it.
        var (updatesCard, _) = SettingsCard(body, page, "Updates", "update train nightly beta stable check new version");
        // Entry 224 section 3.1: the Store's copy says who updates it, and shows none of GroupLab's own update controls.
        if (AppInfo.FromStore)
        {
            updatesCard.Children.Add(Line(StoreUpdateWords));
        }

        var trainHelp = Line(UpdateTrainHelp());
        trainHelp.IsVisible = !AppInfo.FromStore;
        updatesCard.Children.Add(trainHelp);
        var train = new ComboBox { ItemsSource = UpdateTrains.Choosable.Select(TrainLabel).ToList(), SelectedIndex = UpdateTrains.Choosable.ToList().IndexOf(updates.Train) };
        train.SelectionChanged += (_, _) =>
        {
            var chosen = UpdateTrains.Choosable[Math.Max(0, train.SelectedIndex)];
            if (!chosen.IsAvailable())
            {
                // Entry 119 section 4.5: the other trains are shown so a person knows they are coming, and cannot be chosen yet.
                train.SelectedIndex = UpdateTrains.Choosable.ToList().IndexOf(updates.Train);
                Says(chosen.Words() + ": " + UpdateTrains.NotAvailableYet + ". Nightly is the only train with builds on it.");
                return;
            }

            updates = updates with { Train = chosen };
            settingsStore.SaveUpdatePreferences(updates);
            Says("Following the " + chosen.Words().ToLowerInvariant() + " train.");
        };

        var often = new ComboBox { ItemsSource = UpdateCheckIntervals.All.Select(i => i.Words()).ToList(), SelectedIndex = UpdateCheckIntervals.All.ToList().IndexOf(updates.Interval) };
        often.SelectionChanged += (_, _) =>
        {
            updates = updates with { Interval = UpdateCheckIntervals.All[Math.Max(0, often.SelectedIndex)] };
            settingsStore.SaveUpdatePreferences(updates);
            Says("GroupLab will look " + updates.Interval.Words().ToLowerInvariant() + ".");
        };

        // Entry 125 section 3: laid out as the Units rows are, in a grid with the label centred against its control.
        var updateGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("160,Auto"), RowDefinitions = new RowDefinitions("Auto,Auto"), RowSpacing = Tokens.Space8 };
        Named(train, "Update train");
        Named(often, "Check for updates");
        foreach (var (name, control, line) in new (string Name, Control Control, int Line)[] { ("Train", train, 0), ("Check", Row(often, Button("Check now", CheckForUpdates)), 1) })
        {
            var label = FieldLabel(name);
            label.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetRow(label, line);
            Grid.SetRow(control, line);
            Grid.SetColumn(control, 1);
            updateGrid.Children.Add(label);
            updateGrid.Children.Add(control);
        }

        updateGrid.IsVisible = !AppInfo.FromStore;
        updatesCard.Children.Add(updateGrid);

        // Entry 125 section 3: what happened last time, hidden when there is nothing to say.
        Says(LastCheckLine());
        updatesCard.Children.Add(updateState);
        var checkWords = Line("A check is one request for one public file. It sends nothing about you, your rifles or your targets. docs/UPDATES.md says exactly what it does.");
        checkWords.IsVisible = !AppInfo.FromStore;
        updatesCard.Children.Add(checkWords);
        page.OnPutBack(() =>
        {
            var fresh = UpdatePreferences.Default(ThisBuild.Train == UpdateTrain.Development ? UpdateTrain.Nightly : ThisBuild.Train);
            train.SelectedIndex = UpdateTrains.Choosable.ToList().IndexOf(fresh.Train);
            often.SelectedIndex = UpdateCheckIntervals.All.ToList().IndexOf(fresh.Interval);
        });

        // Entry 119 section 4: the build says what it is, where a tester can read it and select it, so a report from a rolling test build
        // names the exact commit rather than "the latest one". The same string is the first line of every log and of every crash record.
        var (build, _) = SettingsCard(body, page, "This build", "version about commit github build number");
        build.Children.Add(new SelectableTextBlock
        {
            Text = BuildLine(),
            TextWrapping = TextWrapping.Wrap,
            Classes = { AppStyles.Secondary },
        });
        build.Children.Add(Line("Copy that line into a bug report: it names the commit this build was made from."));
        build.Children.Add(Row(Button("The project on GitHub", () => OpenInTheBrowser("https://github.com/oRAirwolf/grouplab"))));

        // Entry 126 section 1: the support page, from SupportLink and nowhere else.
        var (support, _) = SettingsCard(body, page, SupportLink.Label, "support donate help the project");
        support.Children.Add(Row(Button(SupportLink.Label, () =>
        {
            if (SupportLink.Address is { } address)
            {
                OpenInTheBrowser(address);
            }
            else
            {
                Failed("There is no support page yet", SupportLink.NoAddressYet);
            }
        })));
        support.Children.Add(Line(SupportLink.Exists ? SupportLink.OpensTheSupportPage : SupportLink.NoAddressYet));
        EndSettingsPage(body, page);
    }

    private void BuildHelpPage(StackPanel column, AppSettingsStore settings)
    {
        var (body, page) = SettingsPageOf(column, SettingsCategory.Help);

        // Alan's relaunch fault: where the last update installed but GroupLab did not come back on its own, this says so.
        body.Children.Add(settingsRelaunch);

        var (problems, problemsCard) = SettingsCard(body, page, "Reporting a problem");
        SettingRow(problems, problemsCard, "Report a problem", "What happened, with the log, to GroupLab's maker or to a file.", Button("Report a problem…", () => OpenReport(null)), "bug crash report issue");

        // Entry 41 section 3: the log's DEBUG switch, remembered, and where the log is, or why there is none.
        var detailedLogging = new CheckBox { IsChecked = DiagnosticLog.Current.Verbose || settings.LoadVerbose() };
        detailedLogging.IsCheckedChanged += (_, _) =>
        {
            DiagnosticLog.Current.Verbose = detailedLogging.IsChecked == true;
            settingsStore.SaveVerbose(detailedLogging.IsChecked == true);
        };
        SettingRow(problems, problemsCard, "Detailed logging", DiagnosticLog.Current.IsEnabled
            ? "The log is in " + DiagnosticLog.Current.DescribedDirectory + "."
            : "Logging is off: " + DiagnosticLog.Current.DisabledReason + ".", detailedLogging, "log debug verbose diagnostics");
        page.OnPutBack(() => detailedLogging.IsChecked = false);

        // Entry 41 sections 5 and 6: the crash records not yet dealt with, which the marking panel's banner also offers until they are.
        var (crashes, _) = SettingsCard(body, page, "Crash records", "crash record report");
        crashes.Children.Add(settingsCrashes);
        EndSettingsPage(body, page);
    }
}

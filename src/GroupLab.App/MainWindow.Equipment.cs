using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Ballistics;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.App;

/// <summary>
/// The Equipment screen, NOTES-FROM-PLANNING.md entry 131 section 7: rifles, barrels and loads on a screen of their own.
/// <para>
/// <b>What it replaces.</b> Records were added from a cramped box on the marking screen with one field shared between a barrel's round count
/// and a load's components, under a heading that named three different things. Alan called it confusing, and it was: the field's meaning
/// changed depending on which of two buttons you pressed afterwards, and nothing on the screen said so.
/// </para>
/// <para>
/// <b>The forms are generated.</b> Every field comes from <see cref="EquipmentForm"/>, which is also what the autocomplete reads, so a field
/// added to a record cannot be forgotten on the screen and the two cannot drift. That is the same arrangement the glossary uses.
/// </para>
/// <para>
/// Entry 407 section 5 (concept 5B, Alan's pick): two tabs, Rifles and Loads, and the Barrels tab gone. A rifle's page is "The rifle" and,
/// under it, "Barrels on this rifle": a table with the barrel on now chosen by a radio, each barrel's length, twist, direction and rounds, an
/// Edit for each and Add a barrel. A barrel's form opens in the same place and comes back to its rifle when saved.
/// </para>
/// </summary>
public sealed partial class MainWindow
{
    private readonly StackPanel equipmentLists = new() { Spacing = Tokens.Space12 };

    private readonly StackPanel equipmentForm = new() { Spacing = Tokens.Space8 };

    /// <summary>How wide a numeric field is: enough for a click value, a twist or a barrel length, and no wider.</summary>
    private const double NumberFieldWidth = 140;

    private readonly TextBlock equipmentProblem = new() { TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Alert } };

    /// <summary>Which list is being shown and edited.</summary>
    private EquipmentKind equipmentKind = EquipmentKind.Rifle;

    /// <summary>The record being edited by name, or null where the form is adding a new one.</summary>
    private string? equipmentEditing;

    /// <summary>Every field's box, by key, so the form can be read back without hunting through the tree.</summary>
    private readonly Dictionary<string, Control> equipmentFields = [];

    /// <summary>Entry 407 section 5: the rifle a barrel being added or edited is on.</summary>
    private string? barrelRifle;

    private Control BuildEquipment()
    {
        var column = new StackPanel { Margin = new Thickness(Tokens.Space24, Tokens.Space20), Spacing = Tokens.Space12 };
        column.Children.Add(new TextBlock { Text = "Equipment", Classes = { AppStyles.Title } });
        column.Children.Add(Line("Your rifles, with the barrels on each, and your loads. A sheet names a rifle with its barrel, and a load, and the analysis and the ballistics page take what they need from here. Every field is optional except a name."));

        var tabs = new StackPanel { Orientation = Orientation.Horizontal, Spacing = Tokens.Space4 };
        foreach (var kind in new[] { EquipmentKind.Rifle, EquipmentKind.Load })
        {
            var button = new Button { Content = kind == EquipmentKind.Rifle ? "Rifles" : "Loads", Name = "EquipmentTab" + kind };
            button.Click += (_, _) =>
            {
                equipmentKind = kind;
                equipmentEditing = null;
                ShowEquipmentScreen(top: true);
            };
            tabs.Children.Add(button);
        }

        column.Children.Add(tabs);
        column.Children.Add(equipmentProblem);

        // Entry 298: the list and the form are peers, so the split between them keeps its share of the window.
        var left = new Border { Child = new ScrollViewer { Content = equipmentLists }, Padding = new Thickness(0, Tokens.Space8, Tokens.Space16, 0) };
        equipmentFormScroll = new ScrollViewer { Content = equipmentForm };
        var right = new Border { Child = equipmentFormScroll, Padding = new Thickness(Tokens.Space16, Tokens.Space8, 0, 0), Classes = { AppStyles.Side } };
        var two = PeerPanes("equipment", left, right, 0.5, 260, 320);
        two.MinHeight = 360;
        column.Children.Add(two);
        equipmentScreenScroll = new ScrollViewer { Content = column };
        return equipmentScreenScroll;
    }

    private ScrollViewer? equipmentScreenScroll;

    private ScrollViewer? equipmentFormScroll;

    private (EquipmentKind, string?, string?) equipmentShown;

    /// <summary>
    /// The list on the left and the form on the right, rebuilt from the record book. A page opened, or a different record, starts at its
    /// top; a rebuild of the same record after a save keeps its place (2026-10-19: a rifle opened after scrolling another started at the
    /// other's place, and the website's Equipment picture with it).
    /// </summary>
    private void ShowEquipmentScreen(bool top = false)
    {
        equipmentLists.Children.Clear();
        equipmentProblem.Text = "";

        // Entry 407 section 5: a barrel is edited on its rifle's page, so the list beside a barrel's form is the rifles.
        bool rifles = equipmentKind != EquipmentKind.Load;
        string kindWord = rifles ? "rifle" : "load";
        var names = rifles ? book.Rifles.Select(r => r.Name).ToList() : book.Loads.Select(l => l.Name).ToList();

        equipmentLists.Children.Add(new TextBlock
        {
            Text = names.Count == 0
                ? $"No {kindWord}s yet. Fill the form and press Save."
                : string.Create(CultureInfo.InvariantCulture, $"{names.Count} {kindWord}{(names.Count == 1 ? "" : "s")}."),
            Classes = { AppStyles.Secondary },
            TextWrapping = TextWrapping.Wrap,
        });

        foreach (string name in names)
        {
            var row = new Button { Content = name, Classes = { AppStyles.TableRow }, HorizontalAlignment = HorizontalAlignment.Stretch, Name = "EquipmentRow" };
            row.Click += (_, _) =>
            {
                equipmentKind = rifles ? EquipmentKind.Rifle : EquipmentKind.Load;
                equipmentEditing = name;
                ShowEquipmentScreen(top: true);
            };
            equipmentLists.Children.Add(row);
        }

        var add = Button("Add another", () =>
        {
            equipmentKind = rifles ? EquipmentKind.Rifle : EquipmentKind.Load;
            equipmentEditing = null;
            ShowEquipmentScreen(top: true);
        });
        equipmentLists.Children.Add(add);

        BuildEquipmentForm();
        var shown = (equipmentKind, equipmentEditing, barrelRifle);
        if (top || shown != equipmentShown)
        {
            foreach (var scroll in new[] { equipmentFormScroll, equipmentScreenScroll })
            {
                scroll?.Offset = default;
            }
        }

        equipmentShown = shown;
    }

    /// <summary>The form for the chosen record, generated from the field list so the screen and the record cannot drift apart.</summary>
    private void BuildEquipmentForm()
    {
        equipmentForm.Children.Clear();
        equipmentFields.Clear();

        equipmentForm.Children.Add(new TextBlock
        {
            Text = equipmentKind switch
            {
                EquipmentKind.Rifle => equipmentEditing is null ? "The rifle: a new one" : "The rifle: " + equipmentEditing,
                EquipmentKind.Barrel => (equipmentEditing ?? "A new barrel") + (barrelRifle is { } on ? ", on " + on : ""),
                _ => equipmentEditing ?? "New load",
            },
            Classes = { AppStyles.Section },
        });

        // Entry 131 section 1's checklist, applied by looking at the render: fourteen fields in one tall column ran past the fold at
        // 1280 by 720 and gave a click value of 0.25 a box 1200 pixels wide at 2560 by 1440. Two columns, and a width that follows what
        // the field holds, so the form reads as a form rather than as a list of sentences waiting to be written.
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), Margin = new Thickness(0, Tokens.Space8, 0, 0) };
        int column = 0, row = 0;
        equipmentForm.Children.Add(grid);

        foreach (var field in EquipmentForm.For(equipmentKind))
        {
            // Entry 456: a field that starts a section puts its heading across both columns first.
            if (field.Section is { } section)
            {
                if (column != 0)
                {
                    row++;
                    column = 0;
                }

                var heading = new TextBlock { Text = section, Classes = { AppStyles.Section }, Margin = new Thickness(0, Tokens.Space8, 0, Tokens.Space8) };
                Grid.SetRow(heading, row);
                Grid.SetColumnSpan(heading, 2);
                while (grid.RowDefinitions.Count <= row)
                {
                    grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                }

                grid.Children.Add(heading);
                row++;
            }

            string? value = CurrentEquipmentValue(field.Key);
            var cell = new StackPanel { Spacing = Tokens.Space4, Margin = new Thickness(0, 0, Tokens.Space16, Tokens.Space12) };
            cell.Children.Add(new TextBlock
            {
                Text = field.Unit is null ? field.Label : $"{field.Label} ({field.Unit})",
                // Entry 462 section 3: a long unit, such as the click value's, was cut off at 1280 by 720.
                TextWrapping = TextWrapping.Wrap,
                Classes = { AppStyles.Label },
            });

            Control box;
            if (field.Kind == FieldKind.Choice && ChoicesFor(field.Key) is { Count: > 0 } choices)
            {
                var combo = new ComboBox { ItemsSource = choices, HorizontalAlignment = HorizontalAlignment.Stretch, Name = "EquipmentField_" + field.Key };
                combo.SelectedIndex = value is null ? -1 : choices.FindIndex(c => string.Equals(c, value, StringComparison.OrdinalIgnoreCase));
                box = Named(combo, field.Label);
            }
            else
            {
                var text = new TextBox
                {
                    Text = value ?? "",
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    AcceptsReturn = field.Kind == FieldKind.Lines,
                    MinHeight = field.Kind == FieldKind.Lines ? 60 : 0,
                    Name = "EquipmentField_" + field.Key,
                };
                Named(text, field.Unit is null ? field.Label : $"{field.Label} ({field.Unit})");

                // Entry 131 section 7.5: as a person types, what they put in this field before, most used first.
                if (field.Kind is FieldKind.Words or FieldKind.Number)
                {
                    text.TextChanged += (_, _) => ShowEquipmentSuggestions(field.Key, text);
                }

                box = text;
            }

            // A number is a few characters, and a box the width of the panel says a sentence is expected. Notes take the whole width,
            // because they are the one field where somebody really does write one.
            if (field.Kind == FieldKind.Number)
            {
                box.HorizontalAlignment = HorizontalAlignment.Left;
                box.Width = NumberFieldWidth;
            }

            equipmentFields[field.Key] = box;
            cell.Children.Add(box);
            cell.Children.Add(new StackPanel { Spacing = Tokens.Space4, Name = "EquipmentHints_" + field.Key });

            bool wide = field.Kind == FieldKind.Lines;
            if (wide && column != 0)
            {
                row++;
                column = 0;
            }

            Grid.SetRow(cell, row);
            Grid.SetColumn(cell, column);
            Grid.SetColumnSpan(cell, wide ? 2 : 1);
            while (grid.RowDefinitions.Count <= row)
            {
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            }

            grid.Children.Add(cell);
            if (wide || column == 1)
            {
                row++;
                column = 0;
            }
            else
            {
                column = 1;
            }
        }

        var buttons = Row(
            Primary("Save", SaveEquipment),
            Button("Cancel", () =>
            {
                if (equipmentKind == EquipmentKind.Barrel && barrelRifle is { } back)
                {
                    equipmentKind = EquipmentKind.Rifle;
                    equipmentEditing = back;
                }
                else
                {
                    equipmentEditing = null;
                }

                ShowEquipmentScreen();
            }));
        // Error report 38: a record already saved can be deleted, asked first.
        if (equipmentEditing is { } saved && EquipmentSaved(saved))
        {
            var delete = Button("Delete", () => AskDeleteEquipment(buttons, saved));
            delete.Name = "equipment-delete";
            buttons.Children.Add(delete);
        }

        equipmentForm.Children.Add(buttons);

        if (equipmentKind == EquipmentKind.Rifle && equipmentEditing is { } rifle && book.FindRifle(rifle) is not null)
        {
            equipmentForm.Children.Add(BarrelsOnThisRifle(rifle));
            equipmentForm.Children.Add(ScopeAndMount(book.FindRifle(rifle)!));
        }
    }

    /// <summary>
    /// Entry 407 section 5.2: the rifle's barrels, the one on now chosen by a radio, each with its length, twist, direction and rounds and an
    /// Edit, and Add a barrel.
    /// </summary>
    private Control BarrelsOnThisRifle(string rifle)
    {
        var part = new StackPanel { Spacing = Tokens.Space4, Margin = new Thickness(0, Tokens.Space16, 0, 0) };
        part.Children.Add(new TextBlock { Text = "Barrels on this rifle", Classes = { AppStyles.Section } });
        var barrels = book.BarrelsOf(rifle);
        if (barrels.Count == 0)
        {
            part.Children.Add(Line("No barrel yet. Add one, and its length and twist are what the ballistics page uses for this rifle."));
        }
        else
        {
            var table = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto,Auto,Auto,Auto") };
            string[] heads = ["On now", "Name", "Length", "Twist", "Direction", "Rounds", ""];
            table.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            for (int c = 0; c < heads.Length; c++)
            {
                var head = new TextBlock { Text = heads[c], Classes = { AppStyles.Label }, Margin = new Thickness(0, 0, Tokens.Space12, Tokens.Space4) };
                Grid.SetColumn(head, c);
                table.Children.Add(head);
            }

            var on = book.OnNow(rifle);
            for (int i = 0; i < barrels.Count; i++)
            {
                var barrel = barrels[i];
                table.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                var radio = new RadioButton { GroupName = "barrel-on-" + rifle, IsChecked = on?.Name == barrel.Name, VerticalAlignment = VerticalAlignment.Center };
                Avalonia.Automation.AutomationProperties.SetName(radio, $"{barrel.Name} is on {rifle} now");
                string name = barrel.Name;
                radio.IsCheckedChanged += (_, _) =>
                {
                    if (radio.IsChecked == true && book.OnNow(rifle)?.Name != name)
                    {
                        PutBarrelOn(rifle, name);
                    }
                };
                Control[] cells =
                [
                    radio,
                    new TextBlock { Text = barrel.Name, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap },
                    new TextBlock { Text = barrel.LengthInches is { } l ? units.Length(l) : "", VerticalAlignment = VerticalAlignment.Center },
                    new TextBlock { Text = barrel.TwistInches is { } t ? string.Create(CultureInfo.InvariantCulture, $"1 in {t:0.##} in") : "", VerticalAlignment = VerticalAlignment.Center },
                    new TextBlock { Text = barrel.TwistDirection is { } d ? (d < 0 ? "left" : "right") : "", VerticalAlignment = VerticalAlignment.Center },
                    new TextBlock { Text = barrel.Rounds.ToString(CultureInfo.InvariantCulture), VerticalAlignment = VerticalAlignment.Center },
                    Button("Edit", () => EditBarrel(rifle, name)),
                ];
                for (int c = 0; c < cells.Length; c++)
                {
                    cells[c].Margin = new Thickness(0, 0, Tokens.Space12, Tokens.Space4);
                    Grid.SetRow(cells[c], i + 1);
                    Grid.SetColumn(cells[c], c);
                    table.Children.Add(cells[c]);
                }
            }

            part.Children.Add(table);
        }

        part.Children.Add(Row(Button("Add a barrel", () => EditBarrel(rifle, null))));
        return part;
    }

    /// <summary>Opens a barrel's form on its rifle, or a new barrel's with the rifle already chosen.</summary>
    internal void EditBarrel(string rifle, string? barrel)
    {
        equipmentKind = EquipmentKind.Barrel;
        equipmentEditing = barrel;
        barrelRifle = rifle;
        ShowEquipmentScreen(top: true);
    }

    /// <summary>Entry 407 section 5.2: marks a barrel as the one on its rifle now, which the setup's list puts first.</summary>
    internal void PutBarrelOn(string rifle, string barrel)
    {
        book = book.PutOn(rifle, barrel);
        SaveBook();
        toaster.Show(new Confirmation($"{barrel} is on {rifle} now.", null));
        ShowEquipmentScreen();
        Refresh();
    }

    /// <summary>The barrels table's names, the one on now first, for the headless tests.</summary>
    internal IReadOnlyList<string> BarrelRows(string rifle) => [.. book.BarrelsOf(rifle).Select(b => b.Name)];

    /// <summary>The fixed lists, which are choices rather than free text: a scope's units, a twist's hand, a barrel's rifle, a drag model.</summary>
    private List<string>? ChoicesFor(string key) => key switch
    {
        "firearm" => ["Rifle", "Pistol"],
        "clickUnit" => ["mil", "MOA", "SMOA"],
        "twistDirection" => ["right", "left"],
        "scopeTravelKind" => ["the total travel", "most up from my zero"],
        "dragModel" => ["G1", "G7"],
        // Entry 407 section 5.5: a barrel under "Barrels not yet on a rifle" is moved by choosing its rifle here.
        "rifle" => [.. book.Rifles.Select(r => r.Name)],
        _ => null,
    };

    /// <summary>What the record being edited holds in one field, or null when adding a new one.</summary>
    private string? CurrentEquipmentValue(string key)
    {
        if (equipmentEditing is null)
        {
            // A new barrel's rifle is the rifle whose page it was added from.
            return equipmentKind == EquipmentKind.Barrel && key == "rifle" ? barrelRifle : null;
        }

        return equipmentKind switch
        {
            EquipmentKind.Rifle => book.FindRifle(equipmentEditing) is { } r ? EquipmentForm.OfRifle(r, key) : null,
            EquipmentKind.Barrel => book.FindBarrel(equipmentEditing) is { } b ? EquipmentForm.OfBarrel(b, key) : null,
            _ => book.FindLoad(equipmentEditing) is { } l ? EquipmentForm.OfLoad(l, key) : null,
        };
    }

    /// <summary>Earlier values for this field, offered as buttons beneath it so one press fills it.</summary>
    private void ShowEquipmentSuggestions(string key, TextBox box)
    {
        if (equipmentForm.Children.FirstOrDefault(c => c.Name == "EquipmentHints_" + key) is not StackPanel hints)
        {
            return;
        }

        hints.Children.Clear();
        if (box.Text is { Length: > 0 } typed && !string.Equals(typed, CurrentEquipmentValue(key), StringComparison.Ordinal))
        {
            foreach (string suggestion in EquipmentForm.Suggestions(book, equipmentKind, key, typed, most: 4))
            {
                if (string.Equals(suggestion, typed, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var pick = Button(suggestion, () =>
                {
                    box.Text = suggestion;
                    hints.Children.Clear();
                });
                hints.Children.Add(pick);
            }
        }
    }

    /// <summary>What the form holds in one field now.</summary>
    private string Typed(string key) => equipmentFields.TryGetValue(key, out var c)
        ? c switch
        {
            TextBox t => t.Text?.Trim() ?? "",
            ComboBox b => b.SelectedItem as string ?? "",
            _ => "",
        }
        : "";

    private static double? Number(string text) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : null;

    private static int? Whole(string text) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i) ? i : null;

    /// <summary>Saves the form as a record, or says plainly why it cannot.</summary>
    internal void SaveEquipment()
    {
        string name = Typed("name");
        if (EquipmentForm.WhyNotSaveable(book, equipmentKind, name, equipmentEditing) is { } why)
        {
            equipmentProblem.Text = why;
            return;
        }

        switch (equipmentKind)
        {
            case EquipmentKind.Rifle:
                // Entry 294 section 1: the scope unit as a shooter says it, mil, MOA or SMOA, and a new rifle starts in Settings' own.
                var unit = Typed("clickUnit") switch
                {
                    "mil" or "Mrad" => AngularUnit.Mrad,
                    "SMOA" or "Smoa" => AngularUnit.Smoa,
                    "MOA" or "Moa" => AngularUnit.Moa,
                    _ => units.Angular,
                };
                double click = Number(Typed("clickValue")) ?? ScopeClicks.Usual(unit).Value;

                // Entry 407 section 5.2: the barrel's length and twist are the barrel's now, and come to the rifle from the one on it, so they
                // are kept from the rifle as it was rather than read off a form that no longer has them.
                var was = equipmentEditing is { } editing ? book.FindRifle(editing) : null;
                book = book.With(new Rifle(name, click, unit)
                {
                    BarrelOn = was?.BarrelOn,
                    Firearm = Typed("firearm") is "Pistol" ? FirearmType.Pistol : FirearmType.Rifle,
                    Manufacturer = Blank(Typed("manufacturer")),
                    Cartridge = Blank(Typed("cartridge")),
                    BarrelLengthInches = was?.BarrelLengthInches,
                    TwistInches = was?.TwistInches,
                    TwistDirection = was?.TwistDirection,
                    Scope = Blank(Typed("scope")),
                    SightHeightInches = Number(Typed("sightHeightInches")),
                    ZeroDistanceYards = Number(Typed("zeroDistanceYards")),
                    Stock = Blank(Typed("stock")),
                    Notes = Blank(Typed("notes")),

                    // Entry 455: a zero kept from the point blank view is not on this form, so it is kept from the rifle as it was.
                    PreviousZerosJson = was?.PreviousZerosJson,

                    // Entry 456: the scope and mount.
                    ScopeTravel = Number(Typed("scopeTravel")),
                    ScopeTravelKind = Typed("scopeTravelKind") is "most up from my zero" ? ScopeTravelKind.MostUpFromZero : Number(Typed("scopeTravel")) is null ? null : ScopeTravelKind.Total,
                    RailCant = Number(Typed("railCant")),
                    SightScaleFactor = Number(Typed("sightScaleFactor")),
                    WeightAsFiredLb = Number(Typed("weightAsFiredLb")),
                });
                break;

            case EquipmentKind.Barrel:
                book = book.With(new Barrel(name, Blank(Typed("rifle")), Whole(Typed("rounds")) ?? 0)
                {
                    LengthInches = Number(Typed("lengthInches")),
                    TwistInches = Number(Typed("twistInches")),
                    TwistDirection = Typed("twistDirection") is "left" ? -1 : Typed("twistDirection") is "right" ? 1 : null,
                    Installed = DateOnly.TryParse(Typed("installed"), CultureInfo.InvariantCulture, out var day) ? day : null,
                    Notes = Blank(Typed("notes")),
                });

                // A rifle's first barrel is the one on it until another is chosen.
                if (Blank(Typed("rifle")) is { } on && book.OnNow(on) is null)
                {
                    book = book.PutOn(on, name);
                }

                break;

            default:
                book = book.With(new Load(name, Blank(Typed("components")))
                {
                    BulletDiameterInches = Number(Typed("bulletDiameterInches")),
                    BulletWeightGrains = Number(Typed("bulletWeightGrains")),
                    BulletName = Blank(Typed("bulletName")),
                    BulletLengthInches = Number(Typed("bulletLengthInches")),
                    BallisticCoefficient = Number(Typed("ballisticCoefficient")),
                    DragModel = Typed("dragModel") is "G1" ? GroupLab.Core.Ballistics.DragModel.G1 : Typed("dragModel") is "G7" ? GroupLab.Core.Ballistics.DragModel.G7 : null,
                    BrassManufacturer = Blank(Typed("brassManufacturer")),
                    BrassCartridge = Blank(Typed("brassCartridge")),
                    Powder = Blank(Typed("powder")),
                    PowderChargeGrains = Number(Typed("powderChargeGrains")),
                    Primer = Blank(Typed("primer")),
                    OverallLengthInches = Number(Typed("overallLengthInches")),
                    BaseToOgiveInches = Number(Typed("baseToOgiveInches")),
                    MuzzleVelocityFps = Number(Typed("muzzleVelocityFps")),
                    MuzzleVelocitySdFps = Number(Typed("muzzleVelocitySdFps")),
                    MinExpansionFps = Number(Typed("minExpansionFps")),
                    Notes = Blank(Typed("notes")),
                });
                break;
        }

        SaveBook();
        toaster.Show(new Confirmation($"Saved the {equipmentKind.ToString().ToLowerInvariant()} {name}.", null));

        // Entry 407 section 5: a barrel saved goes back to its rifle's page, where it is listed.
        if (equipmentKind == EquipmentKind.Barrel && book.FindBarrel(name)?.Rifle is { } home)
        {
            equipmentKind = EquipmentKind.Rifle;
            equipmentEditing = home;
            barrelRifle = null;
        }
        else
        {
            equipmentEditing = name;
        }

        ShowEquipmentScreen();
        Refresh();
    }

    private bool EquipmentSaved(string name) => equipmentKind switch
    {
        EquipmentKind.Rifle => book.FindRifle(name) is not null,
        EquipmentKind.Barrel => book.FindBarrel(name) is not null,
        _ => book.FindLoad(name) is not null,
    };

    /// <summary>Error report 38: what deleting takes away, said in one line, with Delete and Keep it in place of the form's buttons.</summary>
    private void AskDeleteEquipment(WrapPanel buttons, string name)
    {
        int barrels = equipmentKind == EquipmentKind.Rifle ? book.BarrelsOf(name).Count : 0;
        string kind = equipmentKind.ToString().ToLowerInvariant();
        string question = $"Delete the {kind} {name}" + (barrels > 0 ? $" and its {barrels} barrel{(barrels == 1 ? "" : "s")}" : "") + "? Sessions saved with it keep its name.";
        buttons.Children.Clear();
        buttons.Children.Add(new TextBlock { Text = question, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.Warn } });
        var yes = Button("Delete", () => DeleteEquipment(name));
        yes.Name = "equipment-delete-yes";
        buttons.Children.Add(yes);
        buttons.Children.Add(Button("Keep it", () => ShowEquipmentScreen()));
    }

    /// <summary>Deletes the record being edited, which the daily copy and the sync folder's kept versions can still bring back.</summary>
    internal void DeleteEquipment(string name)
    {
        string? home = equipmentKind == EquipmentKind.Barrel ? book.FindBarrel(name)?.Rifle : null;
        book = equipmentKind switch
        {
            EquipmentKind.Rifle => book.WithoutRifle(name),
            EquipmentKind.Barrel => book.WithoutBarrel(name),
            _ => book.WithoutLoad(name),
        };
        SaveBook();
        DiagnosticLog.Info("equipment.delete", ("kind", equipmentKind.ToString()));
        toaster.Show(new Confirmation($"Deleted the {equipmentKind.ToString().ToLowerInvariant()} {name}.", null));
        if (home is not null && book.FindRifle(home) is not null)
        {
            equipmentKind = EquipmentKind.Rifle;
            equipmentEditing = home;
        }
        else
        {
            equipmentEditing = null;
        }

        barrelRifle = null;
        ShowEquipmentScreen();
        Refresh();
    }

    private static string? Blank(string text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    /// <summary>What the Equipment screen is showing, for the headless tests.</summary>
    internal IReadOnlyList<string> EquipmentNames => [.. equipmentLists.Children.OfType<Button>().Where(b => b.Name == "EquipmentRow").Select(b => b.Content as string ?? "")];

    /// <summary>The fields the form is showing, in order, for the headless tests.</summary>
    internal IReadOnlyList<string> EquipmentFieldKeys => [.. EquipmentForm.For(equipmentKind).Select(f => f.Key)];

    /// <summary>Types into one of the form's fields, for the headless tests.</summary>
    /// <summary>One of the form's text fields, for the clips that type into it (entry 475).</summary>
    internal TextBox EquipmentBox(string key) => equipmentFields.TryGetValue(key, out var c) && c is TextBox box
        ? box
        : throw new InvalidOperationException($"the form has no text field \"{key}\"");

    internal void TypeEquipment(string key, string value)
    {
        if (equipmentFields.TryGetValue(key, out var c) && c is TextBox t)
        {
            t.Text = value;
        }
        else if (equipmentFields.TryGetValue(key, out var d) && d is ComboBox b && b.ItemsSource is IEnumerable<string> items)
        {
            b.SelectedIndex = items.ToList().FindIndex(i => string.Equals(i, value, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>Opens the Equipment screen on one of its forms, for the headless tests: a rifle, a new barrel, or a load.</summary>
    internal void ShowEquipment(EquipmentKind kind)
    {
        equipmentKind = kind;
        equipmentEditing = null;
        barrelRifle = null;
        Go(Destination.Equipment);
        ShowEquipmentScreen(top: true);
    }

    /// <summary>Opens a rifle's page, with its barrels, for the headless tests and the screenshot walk.</summary>
    internal void EditRifle(string rifle)
    {
        ShowEquipment(EquipmentKind.Rifle);
        equipmentEditing = rifle;
        ShowEquipmentScreen(top: true);
    }

    /// <summary>Whether the Equipment screen is the one showing.</summary>
    internal bool ShowingEquipment => destination == Destination.Equipment;

    /// <summary>What the screen is complaining about, for the headless tests.</summary>
    internal string EquipmentProblemText => equipmentProblem.Text ?? "";
}

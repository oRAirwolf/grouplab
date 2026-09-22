using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using GroupLab.App.Theme;
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
/// </summary>
public sealed partial class MainWindow
{
    private readonly StackPanel equipmentLists = new() { Spacing = Tokens.Space12 };

    private readonly StackPanel equipmentForm = new() { Spacing = Tokens.Space8 };

    private readonly TextBlock equipmentProblem = new() { TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Alert } };

    /// <summary>Which list is being shown and edited.</summary>
    private EquipmentKind equipmentKind = EquipmentKind.Rifle;

    /// <summary>The record being edited by name, or null where the form is adding a new one.</summary>
    private string? equipmentEditing;

    /// <summary>Every field's box, by key, so the form can be read back without hunting through the tree.</summary>
    private readonly Dictionary<string, Control> equipmentFields = [];

    private Control BuildEquipment()
    {
        var column = new StackPanel { Margin = new Thickness(Tokens.Space24, Tokens.Space20), Spacing = Tokens.Space12 };
        column.Children.Add(new TextBlock { Text = "Equipment", Classes = { AppStyles.Title } });
        column.Children.Add(Line("Your rifles, barrels and loads. A sheet names one of each, and the analysis and the ballistics page take what they need from here. Every field is optional except a name."));

        var tabs = new StackPanel { Orientation = Orientation.Horizontal, Spacing = Tokens.Space4 };
        foreach (var kind in new[] { EquipmentKind.Rifle, EquipmentKind.Barrel, EquipmentKind.Load })
        {
            var button = new Button { Content = kind switch { EquipmentKind.Rifle => "Rifles", EquipmentKind.Barrel => "Barrels", _ => "Loads" }, Name = "EquipmentTab" + kind };
            button.Click += (_, _) =>
            {
                equipmentKind = kind;
                equipmentEditing = null;
                ShowEquipmentScreen();
            };
            tabs.Children.Add(button);
        }

        column.Children.Add(tabs);
        column.Children.Add(equipmentProblem);

        var two = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), MinHeight = 360 };
        var left = new Border { Child = new ScrollViewer { Content = equipmentLists }, Padding = new Thickness(0, Tokens.Space8, Tokens.Space16, 0) };
        var right = new Border { Child = new ScrollViewer { Content = equipmentForm }, Padding = new Thickness(Tokens.Space16, Tokens.Space8, 0, 0), Classes = { AppStyles.Side } };
        Grid.SetColumn(left, 0);
        Grid.SetColumn(right, 1);
        two.Children.Add(left);
        two.Children.Add(right);
        column.Children.Add(two);
        return new ScrollViewer { Content = column };
    }

    /// <summary>The list on the left and the form on the right, rebuilt from the record book.</summary>
    private void ShowEquipmentScreen()
    {
        equipmentLists.Children.Clear();
        equipmentProblem.Text = "";

        var names = equipmentKind switch
        {
            EquipmentKind.Rifle => book.Rifles.Select(r => r.Name).ToList(),
            EquipmentKind.Barrel => book.Barrels.Select(b => b.Name).ToList(),
            _ => book.Loads.Select(l => l.Name).ToList(),
        };

        equipmentLists.Children.Add(new TextBlock
        {
            Text = names.Count == 0
                ? $"No {equipmentKind.ToString().ToLowerInvariant()}s yet. Fill the form and press Save."
                : string.Create(CultureInfo.InvariantCulture, $"{names.Count} {equipmentKind.ToString().ToLowerInvariant()}{(names.Count == 1 ? "" : "s")}."),
            Classes = { AppStyles.Secondary },
            TextWrapping = TextWrapping.Wrap,
        });

        foreach (string name in names)
        {
            var row = new Button { Content = name, Classes = { AppStyles.TableRow }, HorizontalAlignment = HorizontalAlignment.Stretch, Name = "EquipmentRow" };
            row.Click += (_, _) =>
            {
                equipmentEditing = name;
                ShowEquipmentScreen();
            };
            equipmentLists.Children.Add(row);
        }

        var add = Button("Add another", () =>
        {
            equipmentEditing = null;
            ShowEquipmentScreen();
        });
        equipmentLists.Children.Add(add);

        BuildEquipmentForm();
    }

    /// <summary>The form for the chosen record, generated from the field list so the screen and the record cannot drift apart.</summary>
    private void BuildEquipmentForm()
    {
        equipmentForm.Children.Clear();
        equipmentFields.Clear();

        equipmentForm.Children.Add(new TextBlock
        {
            Text = equipmentEditing is null ? $"New {equipmentKind.ToString().ToLowerInvariant()}" : equipmentEditing,
            Classes = { AppStyles.Section },
        });

        foreach (var field in EquipmentForm.For(equipmentKind))
        {
            string? value = CurrentEquipmentValue(field.Key);
            equipmentForm.Children.Add(new TextBlock
            {
                Text = field.Unit is null ? field.Label : $"{field.Label} ({field.Unit})",
                Classes = { AppStyles.Label },
            });

            Control box;
            if (field.Kind == FieldKind.Choice && ChoicesFor(field.Key) is { Count: > 0 } choices)
            {
                var combo = new ComboBox { ItemsSource = choices, HorizontalAlignment = HorizontalAlignment.Stretch, Name = "EquipmentField_" + field.Key };
                combo.SelectedIndex = value is null ? -1 : choices.FindIndex(c => string.Equals(c, value, StringComparison.OrdinalIgnoreCase));
                box = combo;
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

                // Entry 131 section 7.5: as a person types, what they put in this field before, most used first.
                if (field.Kind is FieldKind.Words or FieldKind.Number)
                {
                    text.TextChanged += (_, _) => ShowEquipmentSuggestions(field.Key, text);
                }

                box = text;
            }

            equipmentFields[field.Key] = box;
            equipmentForm.Children.Add(box);
            equipmentForm.Children.Add(new StackPanel { Spacing = Tokens.Space4, Name = "EquipmentHints_" + field.Key });
        }

        equipmentForm.Children.Add(Row(
            Primary("Save", SaveEquipment),
            Button("Cancel", () =>
            {
                equipmentEditing = null;
                ShowEquipmentScreen();
            })));
    }

    /// <summary>The fixed lists, which are choices rather than free text: a scope's units, a twist's hand, a barrel's rifle, a drag model.</summary>
    private List<string>? ChoicesFor(string key) => key switch
    {
        "firearm" => ["Rifle", "Pistol"],
        "clickUnit" => ["Moa", "Mrad", "Smoa"],
        "twistDirection" => ["right", "left"],
        "dragModel" => ["G1", "G7"],
        "rifle" => [.. book.Rifles.Select(r => r.Name)],
        _ => null,
    };

    /// <summary>What the record being edited holds in one field, or null when adding a new one.</summary>
    private string? CurrentEquipmentValue(string key)
    {
        if (equipmentEditing is null)
        {
            return null;
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
                double click = Number(Typed("clickValue")) ?? 0.25;
                var unit = Typed("clickUnit") switch { "Mrad" => AngularUnit.Mrad, "Smoa" => AngularUnit.Smoa, _ => AngularUnit.Moa };
                book = book.With(new Rifle(name, click, unit)
                {
                    Firearm = Typed("firearm") is "Pistol" ? FirearmType.Pistol : FirearmType.Rifle,
                    Manufacturer = Blank(Typed("manufacturer")),
                    Cartridge = Blank(Typed("cartridge")),
                    BarrelLengthInches = Number(Typed("barrelLengthInches")),
                    TwistInches = Number(Typed("twistInches")),
                    TwistDirection = Typed("twistDirection") is "left" ? -1 : Typed("twistDirection") is "right" ? 1 : null,
                    Scope = Blank(Typed("scope")),
                    SightHeightInches = Number(Typed("sightHeightInches")),
                    ZeroDistanceYards = Number(Typed("zeroDistanceYards")),
                    Stock = Blank(Typed("stock")),
                    Notes = Blank(Typed("notes")),
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
                    Notes = Blank(Typed("notes")),
                });
                break;
        }

        SaveBook();
        toaster.Show(new Confirmation($"Saved the {equipmentKind.ToString().ToLowerInvariant()} {name}.", null));
        equipmentEditing = name;
        ShowEquipmentScreen();
        Refresh();
    }

    private static string? Blank(string text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    /// <summary>What the Equipment screen is showing, for the headless tests.</summary>
    internal IReadOnlyList<string> EquipmentNames => [.. equipmentLists.Children.OfType<Button>().Where(b => b.Name == "EquipmentRow").Select(b => b.Content as string ?? "")];

    /// <summary>The fields the form is showing, in order, for the headless tests.</summary>
    internal IReadOnlyList<string> EquipmentFieldKeys => [.. EquipmentForm.For(equipmentKind).Select(f => f.Key)];

    /// <summary>Types into one of the form's fields, for the headless tests.</summary>
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

    /// <summary>Opens the Equipment screen on one of its three lists, for the headless tests.</summary>
    internal void ShowEquipment(EquipmentKind kind)
    {
        equipmentKind = kind;
        equipmentEditing = null;
        Go(Destination.Equipment);
        ShowEquipmentScreen();
    }

    /// <summary>Whether the Equipment screen is the one showing.</summary>
    internal bool ShowingEquipment => destination == Destination.Equipment;

    /// <summary>What the screen is complaining about, for the headless tests.</summary>
    internal string EquipmentProblemText => equipmentProblem.Text ?? "";
}

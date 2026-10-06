using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Input;
using GroupLab.Core.Marking;

namespace GroupLab.App;

/// <summary>
/// Tap a number to switch units, NOTES-FROM-PLANNING.md entries 272 and 273, on the desktop and the phone alike: a value on screen becomes
/// tappable. Entry 280 section 1, Alan: "when you tap a value, it should only change that individual value and not all of the values
/// displayed on the screen." A tap switches the number tapped, from what it shows (<see cref="UnitSwitch.Convert"/>), and the unit is
/// remembered for that figure, so the same figure shows in it wherever it appears again; Settings keeps the units of figures never tapped.
/// Right-click, or press and hold on a phone, lists every unit the value can take. What kind a value is comes from the unit written after
/// its number, so a value is tappable exactly when it shows a unit; its label still explains it, and a value is never given the label's
/// dotted underline or the glossary's tap.
/// </summary>
public static partial class UnitTap
{
    /// <summary>The class a tappable value carries, which the glossary's walk passes over so a tap on a value never explains it.</summary>
    public const string Value = "unit-value";

    /// <summary>The units in force now, and what switching them does: set by the window or the phone's shell.</summary>
    public static Func<UnitSettings>? Current { get; set; }

    public static Action<UnitSettings, UnitKind>? Apply { get; set; }

    /// <summary>The unit remembered for a figure and kind (<see cref="Key"/>), or null: set by the window or the phone's shell.</summary>
    public static Func<string, string?>? Remembered { get; set; }

    /// <summary>A number was switched: the figure's key where it has a name, the unit now shown, and its kind.</summary>
    public static Action<string?, string, UnitKind>? Tapped { get; set; }

    /// <summary>What a figure's unit is remembered under: its name and the kind of number, so a size and an angle of one figure keep their own.</summary>
    public static string Key(string figure, UnitKind kind) => figure + "|" + kind;

    /// <summary>The kind of the first number with a unit after it in <paramref name="text"/>, or null where there is none.</summary>
    public static UnitKind? KindOf(string? text)
    {
        if (string.IsNullOrEmpty(text) || Unit().Match(text) is not { Success: true } m)
        {
            return null;
        }

        return m.Groups["u"].Value switch
        {
            "MOA" or "SMOA" or "mil" or "NATO mil" => UnitKind.Angle,
            "in" or "cm" or "mm" => UnitKind.Length,
            _ => UnitKind.Distance,
        };
    }

    [GeneratedRegex(@"\d\s?(?<u>NATO mil|SMOA|MOA|mil|in|cm|mm|yd|m)(?![A-Za-z])")]
    private static partial Regex Unit();

    /// <summary>
    /// Makes a value tappable where its text shows a unit: a tap switches that number, and a right-click or press and hold lists every unit
    /// for it. Given the <paramref name="figure"/>'s name, the value is shown at once in the unit remembered for that figure, and a switch is
    /// remembered for it. The kind is read from the text when it is tapped, so a value rewritten in other units still switches the right thing.
    /// </summary>
    public static T Attach<T>(T block, string? figure = null)
        where T : TextBlock
    {
        ArgumentNullException.ThrowIfNull(block);
        if (KindOf(block.Text) is null)
        {
            return block;
        }

        if (figure is not null && Remembered is { } remembered)
        {
            foreach (var kind in new[] { UnitKind.Angle, UnitKind.Length, UnitKind.Distance })
            {
                if (remembered(Key(figure, kind)) is { } symbol && UnitSwitch.SymbolIn(block.Text, kind) is { } shown && shown != symbol)
                {
                    block.Text = UnitSwitch.Convert(block.Text ?? "", kind, symbol);
                }
            }
        }

        block.Classes.Add(Value);
        block.Cursor = new Cursor(StandardCursorType.Hand);
        block.Tapped += (_, e) =>
        {
            if (KindOf(block.Text) is { } kind && UnitSwitch.SymbolIn(block.Text, kind) is { } from)
            {
                Switch(block, figure, kind, UnitSwitch.Next(kind, from));
                e.Handled = true;
            }
        };
        block.ContextRequested += (_, e) =>
        {
            if (KindOf(block.Text) is not { } kind || UnitSwitch.SymbolIn(block.Text, kind) is not { } from)
            {
                return;
            }

            var menu = new MenuFlyout();
            foreach (string symbol in UnitSwitch.Symbols(kind))
            {
                var item = new MenuItem { Header = symbol == from ? symbol + "  (now)" : symbol };
                item.Click += (_, _) => Switch(block, figure, kind, symbol);
                menu.Items.Add(item);
            }

            menu.ShowAt(block);
            e.Handled = true;
        };
        return block;
    }

    /// <summary>
    /// Entry 376 item B5, Alan: a tap anywhere in a value's box switches its units, not only a tap on the text. A tap in
    /// <paramref name="box"/> that the value itself did not take switches <paramref name="block"/>, as a tap on it would. The box is given a
    /// clear background where it has none, so its empty space takes the tap.
    /// </summary>
    public static T Widen<T>(T box, TextBlock block, string? figure = null)
        where T : Control
    {
        ArgumentNullException.ThrowIfNull(box);
        ArgumentNullException.ThrowIfNull(block);
        if (box is Avalonia.Controls.Panel { Background: null } panel)
        {
            panel.Background = GroupLab.App.Theme.Tokens.Clear;
        }
        else if (box is Border { Background: null } border)
        {
            border.Background = GroupLab.App.Theme.Tokens.Clear;
        }

        box.Tapped += (_, e) =>
        {
            if (!e.Handled && KindOf(block.Text) is { } kind && UnitSwitch.SymbolIn(block.Text, kind) is { } from)
            {
                Switch(block, figure, kind, UnitSwitch.Next(kind, from));
                e.Handled = true;
            }
        };
        return box;
    }

    /// <summary>This number shown in <paramref name="to"/>, and the switch told to the host, which remembers it for the figure.</summary>
    public static void Switch(TextBlock block, string? figure, UnitKind kind, string to)
    {
        ArgumentNullException.ThrowIfNull(block);
        block.Text = UnitSwitch.Convert(block.Text ?? "", kind, to);
        Tapped?.Invoke(figure is null ? null : Key(figure, kind), to, kind);
    }
}

using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Input;
using GroupLab.Core.Marking;

namespace GroupLab.App;

/// <summary>
/// Tap a number to switch units, NOTES-FROM-PLANNING.md entries 272 and 273, on the desktop and the phone alike: a value on screen becomes
/// tappable, and a tap switches every number of its kind, everywhere, through the one unit setting Settings shows (<see cref="UnitSwitch"/>).
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
    /// Makes a value tappable where its text shows a unit: a tap switches its kind, and a right-click or press and hold lists every unit.
    /// The kind is read from the text when it is tapped, so a value rewritten in other units still switches the right thing.
    /// </summary>
    public static T Attach<T>(T block)
        where T : TextBlock
    {
        ArgumentNullException.ThrowIfNull(block);
        if (KindOf(block.Text) is null)
        {
            return block;
        }

        block.Classes.Add(Value);
        block.Cursor = new Cursor(StandardCursorType.Hand);
        block.Tapped += (_, e) =>
        {
            if (KindOf(block.Text) is { } kind && Current?.Invoke() is { } now)
            {
                Apply?.Invoke(UnitSwitch.Switch(now, kind), kind);
                e.Handled = true;
            }
        };
        block.ContextRequested += (_, e) =>
        {
            if (KindOf(block.Text) is not { } kind || Current?.Invoke() is not { } now)
            {
                return;
            }

            var menu = new MenuFlyout();
            foreach (var (symbol, units, current) in UnitSwitch.Choices(now, kind))
            {
                var item = new MenuItem { Header = current ? symbol + "  (now)" : symbol };
                item.Click += (_, _) => Apply?.Invoke(units, kind);
                menu.Items.Add(item);
            }

            menu.ShowAt(block);
            e.Handled = true;
        };
        return block;
    }
}

using Avalonia;
using Avalonia.Input;

namespace GroupLab.App;

/// <summary>
/// The platform's command key, NOTES-FROM-PLANNING.md entry 166 section 2. On a Mac the Command key arrives as <see cref="KeyModifiers.Meta"/>,
/// so every shortcut that was built on Control needed a key no Mac user presses, and Command Z did nothing for the first person to try it.
/// Every modified shortcut asks here, and so does every label that names one, so the key a label names is the key that works.
/// </summary>
internal static class CommandKey
{
    /// <summary>What Avalonia's platform says its command modifier is, and the operating system's convention when it says nothing.</summary>
    public static KeyModifiers Modifier => Application.Current?.PlatformSettings?.HotkeyConfiguration.CommandModifiers ?? For(OperatingSystem.IsMacOS());

    /// <summary>The convention: Command on a Mac, Control everywhere else.</summary>
    public static KeyModifiers For(bool mac) => mac ? KeyModifiers.Meta : KeyModifiers.Control;

    /// <summary>Whether the command key is held.</summary>
    public static bool Held(KeyModifiers modifiers) => (modifiers & Modifier) == Modifier;

    private static bool Mac => Modifier == KeyModifiers.Meta;

    /// <summary>A shortcut as the platform writes it: the Command symbol on a Mac, "Ctrl+" everywhere else.</summary>
    public static string Label(string key) => Label(key, Mac);

    public static string Label(string key, bool mac) => mac ? "⌘" + key : "Ctrl+" + key;

    /// <summary>Redo as the platform spells it: Shift Command Z on a Mac, Ctrl+Y elsewhere. Both work on both.</summary>
    public static string RedoLabel => RedoLabelFor(Mac);

    public static string RedoLabelFor(bool mac) => mac ? "⇧⌘Z" : "Ctrl+Y";

    /// <summary>Redo is Command Y everywhere, and Shift Command Z as well, which is what a Mac user presses.</summary>
    public static bool IsRedo(KeyEventArgs e) => Held(e.KeyModifiers) && (e.Key == Key.Y || (e.Key == Key.Z && e.KeyModifiers.HasFlag(KeyModifiers.Shift)));

    /// <summary>
    /// Switching target tabs (planning, 2026-10-07): Control Tab, and Control Shift Tab backwards, on every platform, the browser's keys. It is
    /// the one shortcut that is not the command key, because Command Tab on a Mac is the system's own switch between applications and never
    /// reaches GroupLab, and a Mac's browsers use Control Tab for their tabs too. Null where the key is not a tab switch, true for backwards.
    /// </summary>
    public static bool? TabSwitch(KeyEventArgs e) =>
        e.Key == Key.Tab && (e.KeyModifiers & KeyModifiers.Control) != 0 ? (e.KeyModifiers & KeyModifiers.Shift) != 0 : null;

    /// <summary>Undo is Command Z without Shift, so Shift Command Z is never read as undo.</summary>
    public static bool IsUndo(KeyEventArgs e) => Held(e.KeyModifiers) && e.Key == Key.Z && !e.KeyModifiers.HasFlag(KeyModifiers.Shift);
}

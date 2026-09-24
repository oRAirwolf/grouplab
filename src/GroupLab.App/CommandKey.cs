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

    /// <summary>Undo is Command Z without Shift, so Shift Command Z is never read as undo.</summary>
    public static bool IsUndo(KeyEventArgs e) => Held(e.KeyModifiers) && e.Key == Key.Z && !e.KeyModifiers.HasFlag(KeyModifiers.Shift);
}

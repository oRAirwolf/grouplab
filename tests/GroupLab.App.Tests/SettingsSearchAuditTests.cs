using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;

namespace GroupLab.App.Tests;

/// <summary>
/// Entry 474 section 2: Find a setting finds every row of every page by the words on it, and the settings people look for by other names
/// by those names too.
/// </summary>
public class SettingsSearchAuditTests
{
    /// <summary>The first words a row shows: its name, as a person reads it there.</summary>
    internal static string? Label(Control row)
    {
        string? text = row as TextBlock is { } own ? own.Text : null;
        text ??= row.GetLogicalDescendants().Select(c => c switch
        {
            TextBlock t => t.Text,
            ContentControl { Content: string s } => s,
            _ => null,
        }).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
        text ??= row is ContentControl { Content: string s2 } ? s2 : null;
        if (text is null)
        {
            return null;
        }

        var words = Regex.Matches(text, @"\p{L}[\p{L}\p{Nd}'-]*").Select(m => m.Value).Where(w => w.Length >= 3).Take(4).ToList();
        return words.Count == 0 ? null : string.Join(' ', words);
    }

    internal static List<string> Unfound(SettingsPages pages)
    {
        var missed = new List<string>();
        foreach (var page in pages.Pages.ToList())
        {
            foreach (var card in page.Cards)
            {
                foreach (var (row, _) in card.Rows)
                {
                    if (Label(row) is not { } label)
                    {
                        continue;
                    }

                    pages.Filter(label);
                    if (!(row.IsVisible && card.Root.IsVisible && page.Root.IsVisible))
                    {
                        missed.Add($"{SettingsMenu.Name(page.Category)}: \"{label}\"");
                    }
                }
            }
        }

        pages.Filter(null);
        return missed;
    }

    /// <summary>Other words a person types for a setting, and a word of the row each should find.</summary>
    internal static readonly (string Query, string Finds)[] OtherWords =
    [
        ("dark", "Theme"), ("light", "Theme"), ("font", "Text size"), ("bigger", "Text size"), ("animation", "motion"), ("metric", "unit"),
        ("millimeters", "unit"), ("MOA", "unit"), ("yards", "unit"), ("backup", "copy"), ("restore", "Restore"), ("cloud", "Sync"),
        ("OneDrive", "Sync"), ("crash", "report"), ("bug", "problem"), ("version", "build"), ("update", "update"),
        ("printer", "rinter"), ("paper", "aper"), ("calibre", "caliber"), ("distance", "distance"), ("rifle", "rifle"), ("demo", "demo"),
        ("privacy", "send"),
    ];

    internal static List<string> OtherWordsMissed(SettingsPages pages)
    {
        var missed = new List<string>();
        foreach (var (query, finds) in OtherWords)
        {
            pages.Filter(query);
            bool found = pages.Pages.Any(p => p.Root.IsVisible && p.Cards.Any(c => c.Root.IsVisible
                && (c.Rows.Any(r => r.Row.IsVisible && (Label(r.Row) ?? "").Contains(finds, StringComparison.OrdinalIgnoreCase))
                    || c.Words.Contains(finds, StringComparison.OrdinalIgnoreCase))));
            if (!found)
            {
                missed.Add($"\"{query}\" finds nothing named {finds}");
            }
        }

        pages.Filter(null);
        return missed;
    }

    [AvaloniaFact]
    public void EveryRowIsFoundByItsOwnWordsAndTheObviousOtherOnes()
    {
        var (window, _, _) = Entry109Tests.Sheet();
        try
        {
            window.ShowSettings(SettingsCategory.General);
            Dispatcher.UIThread.RunJobs();
            var missed = Unfound(window.SettingsPagesShown).Concat(OtherWordsMissed(window.SettingsPagesShown)).ToList();
            Assert.True(missed.Count == 0, "Find a setting misses:\n" + string.Join("\n", missed));
        }
        finally
        {
            window.Close();
        }
    }
}

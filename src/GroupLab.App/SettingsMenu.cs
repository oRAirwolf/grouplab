using Avalonia.Controls;

namespace GroupLab.App;

/// <summary>NOTES-FROM-PLANNING.md entry 468 section 1, concept A: Settings as eight categories, one page at a time.</summary>
public enum SettingsCategory
{
    General,
    Units,
    Shooting,
    Printers,
    Data,
    Sharing,
    Updates,
    Help,
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 468 section 1, Alan: Settings "is very cluttered". The eight categories' names and their one line, the
/// search's words, and the reset's question, shared by the computer and the phones so both say the same thing.
/// </summary>
public static class SettingsMenu
{
    public const string Find = "Find a setting";

    public const string FindHint = "Search, for example MOA";

    public const string Remembered = "Every choice here is remembered as soon as it is made.";

    public const string PutBack = "Put this page back as it came";

    public const string NothingFound = "No setting matches that. Try another word, such as units, dark or backup.";

    public const string BackToList = "‹ Settings";

    public static IReadOnlyList<SettingsCategory> All { get; } = Enum.GetValues<SettingsCategory>();

    public static string Name(SettingsCategory category) => category switch
    {
        SettingsCategory.General => "General",
        SettingsCategory.Units => "Units and figures",
        SettingsCategory.Shooting => "Shooting defaults",
        SettingsCategory.Printers => "Printers and paper",
        SettingsCategory.Data => "Sync and your data",
        SettingsCategory.Sharing => "Sharing and privacy",
        SettingsCategory.Updates => "Updates and about",
        _ => "Help and problems",
    };

    /// <summary>The line under a page's title.</summary>
    public static string Intro(SettingsCategory category) => category switch
    {
        SettingsCategory.General => "How GroupLab looks and moves, and its guided demo.",
        SettingsCategory.Units => "How every figure is shown. Nothing stored changes.",
        SettingsCategory.Shooting => "What a new target starts with. The caliber is still asked for every target.",
        SettingsCategory.Printers => "The printers GroupLab has checked, and the paper it prints on.",
        SettingsCategory.Data => "Where your records are kept, copied and synchronized.",
        SettingsCategory.Sharing => "What GroupLab may send, and to whom. Nothing is sent unless you say so.",
        SettingsCategory.Updates => "Which builds this copy follows, and which build it is.",
        _ => "Reporting a problem, and what GroupLab keeps to help with one.",
    };

    /// <summary>The question "Put this page back as it came" asks before it does anything.</summary>
    public static string PutBackAsks(SettingsCategory category) =>
        $"Put \"{Name(category)}\" back as it came? Its choices go back to how GroupLab starts. Your sessions, rifles, printers, copies and synchronization are not touched.";

    public static string PutBackSaid(SettingsCategory category) => $"\"{Name(category)}\" is back as it came.";

    /// <summary>
    /// Whether every word of <paramref name="query"/> is in <paramref name="words"/>, ignoring case: a row's name, its help line and the
    /// other words a person might look for it by, such as "metric" for the units.
    /// </summary>
    public static bool Matches(string? query, string words)
    {
        var asked = (query ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return asked.Length > 0 && asked.All(word => words.Contains(word, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 468 sections 1 and 2.5: which control belongs to which page and card, what each row is found by, and what
/// putting a page back does. The look is each platform's own; this only shows and hides. A search shows the matching rows of every page in
/// place, so a choice made among the results is the same control as on its page.
/// </summary>
public sealed class SettingsPages
{
    private readonly Dictionary<SettingsCategory, Page> pages = [];

    public SettingsCategory Current { get; private set; } = SettingsCategory.General;

    /// <summary>The words being searched for, or null when one page is shown.</summary>
    public string? Query { get; private set; }

    public Page this[SettingsCategory category] => pages[category];

    public IReadOnlyCollection<Page> Pages => pages.Values;

    /// <summary>Registers <paramref name="root"/> as <paramref name="category"/>'s page; <paramref name="onlyOnItsOwn"/> are hidden while searching.</summary>
    public Page Add(SettingsCategory category, Control root, params Control[] onlyOnItsOwn)
    {
        var page = new Page(category, root, onlyOnItsOwn);
        pages[category] = page;
        root.IsVisible = Query is null ? category == Current : false;
        return page;
    }

    /// <summary>Shows <paramref name="category"/> alone, and ends any search.</summary>
    public void Show(SettingsCategory category)
    {
        Current = category;
        Filter(null);
    }

    /// <summary>
    /// Shows the rows matching <paramref name="query"/> on every page, each page under its own title, or the current page alone when it is
    /// empty. Returns how many rows match.
    /// </summary>
    public int Filter(string? query)
    {
        Query = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        int found = 0;
        foreach (var page in pages.Values)
        {
            if (Query is null)
            {
                page.ShowAll(page.Category == Current);
                continue;
            }

            int here = page.Filter(Query);
            found += here;
        }

        return found;
    }

    /// <summary>Puts <paramref name="category"/>'s settings back as GroupLab starts with them, through each row's own control.</summary>
    public void PutBack(SettingsCategory category)
    {
        foreach (var reset in pages[category].Resets)
        {
            reset();
        }
    }

    public sealed class Page(SettingsCategory category, Control root, IEnumerable<Control> onlyOnItsOwn)
    {
        private readonly List<Card> cards = [];
        private readonly List<Control> alone = [.. onlyOnItsOwn];

        public SettingsCategory Category { get; } = category;

        public Control Root { get; } = root;

        internal List<Action> Resets { get; } = [];

        public IReadOnlyList<Card> Cards => cards;

        /// <summary>Registers <paramref name="root"/> as a card on this page, found as a whole by <paramref name="heading"/> or <paramref name="words"/>.</summary>
        public Card Card(Control root, string heading = "", string words = "")
        {
            var card = new Card(root, (heading + " " + words).Trim());
            cards.Add(card);
            return card;
        }

        /// <summary>Registers <paramref name="control"/> as shown on the page alone and hidden while searching.</summary>
        public void OnItsOwn(Control control) => alone.Add(control);

        /// <summary>Something "Put this page back as it came" does: a row's control set to how GroupLab starts.</summary>
        public void OnPutBack(Action reset) => Resets.Add(reset);

        internal void ShowAll(bool shown)
        {
            Root.IsVisible = shown;
            foreach (var extra in alone)
            {
                extra.IsVisible = true;
            }

            foreach (var card in cards)
            {
                card.ShowAll();
            }
        }

        internal int Filter(string query)
        {
            int found = 0;
            foreach (var card in cards)
            {
                found += card.Filter(query, SettingsMenu.Name(Category));
            }

            Root.IsVisible = found > 0;
            foreach (var extra in alone)
            {
                extra.IsVisible = false;
            }

            return found;
        }
    }

    public sealed class Card(Control root, string words)
    {
        private readonly List<(Control Row, string Words)> rows = [];

        public Control Root { get; } = root;

        /// <summary>What finds the card as a whole: its heading and any other words for it.</summary>
        public string Words { get; } = words;

        /// <summary>Registers <paramref name="row"/>, found by <paramref name="words"/>: its name, its help line and any other words for it.</summary>
        public T Row<T>(T row, string words)
            where T : Control
        {
            rows.Add((row, words));
            return row;
        }

        public IReadOnlyList<(Control Row, string Words)> Rows => rows;

        internal void ShowAll()
        {
            Root.IsVisible = true;
            foreach (var (row, _) in rows)
            {
                row.IsVisible = true;
            }
        }

        internal int Filter(string query, string pageName)
        {
            // A card found by its heading shows whole; a card with no rows of its own is one row.
            bool whole = SettingsMenu.Matches(query, Words) || (rows.Count == 0 && SettingsMenu.Matches(query, Words + " " + pageName));
            int found = 0;
            foreach (var (row, words) in rows)
            {
                bool shown = whole || SettingsMenu.Matches(query, words);
                row.IsVisible = shown;
                found += shown ? 1 : 0;
            }

            if (rows.Count == 0 && whole)
            {
                found = 1;
            }

            Root.IsVisible = found > 0;
            return found;
        }
    }
}

/// <summary>NOTES-FROM-PLANNING.md entry 468 section 2.1: the daily copies' words, the same on the computer and the phones.</summary>
public static class CopiesWords
{
    public const string Heading = "Daily copies of your records";

    public const string Keep = "Keep a daily copy of your records";

    public const string KeepSays = "Once a day, at the first open. The newest 14 are kept; older ones wait 14 days in GroupLab's trash.";

    public const string BackUpNow = "Back up now";

    public const string Restore = "Restore a copy\u2026";

    public const string OpenFolder = "Open the folder";

    public const string None = "No copy has been made yet.";

    public const string NoneToRestore = "There is no copy to restore yet.";

    public const string OnThePhone = "On this phone the copies stay in GroupLab's own storage. To keep one off the phone, use Export all my data.";

    public const string WhichCopy = "Which copy should your records go back to?";

    public const string LeaveIt = "Leave my records as they are";

    /// <summary>When the newest copy was made and how big it is, or that none has been.</summary>
    public static string Last(GroupLab.Core.Records.RecordsCopy? newest) => newest is null
        ? None
        : $"The last copy was made on {GroupLab.Core.Records.RecordsCopies.Words(newest.Made)}, {GroupLab.Core.Records.RecordsCopies.Size(newest.Bytes)}.";

    /// <summary>A copy as it is offered for restoring: its day, how many sessions it holds and its size.</summary>
    public static string Choice(GroupLab.Core.Records.RecordsCopy copy) =>
        GroupLab.Core.Records.RecordsCopies.Words(copy.Made) + ", " + Sessions(copy.Sessions) + ", " + GroupLab.Core.Records.RecordsCopies.Size(copy.Bytes);

    /// <summary>The one question before a restore.</summary>
    public static string Ask(GroupLab.Core.Records.RecordsCopy copy) =>
        $"Restore the copy from {GroupLab.Core.Records.RecordsCopies.Words(copy.Made)}? It holds {Sessions(copy.Sessions)}. Your records as they are now are copied first, then replaced by it, and GroupLab opens the restored records at once.";

    public const string RestoreIt = "Restore this copy";

    private static string Sessions(int? count) => count switch
    {
        null => "sessions that could not be counted",
        1 => "1 session",
        _ => $"{count} sessions",
    };
}

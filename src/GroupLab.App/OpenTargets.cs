namespace GroupLab.App;

/// <summary>
/// One target open in a tab, Alan's concept A for several targets open at once (planning, 2026-10-07): what its tab says, and while it is
/// not the one showing, everything the window holds for it, parked until it is shown again.
/// </summary>
internal sealed class OpenTarget
{
    internal OpenTarget(int number) => Number = number;

    /// <summary>The order the tab was opened in, from 1, which tells two tabs on the same picture apart in the log.</summary>
    public int Number { get; }

    /// <summary>The target's name, the picture's file name or the sheet's own label, or null while nothing is open in the tab.</summary>
    public string? Name { get; set; }

    /// <summary>Whether the tab is showing its analysis rather than its marks.</summary>
    public bool Analysing { get; set; }

    /// <summary>Whether the tab holds marks that are not in a saved session, which is what closing it asks about.</summary>
    public bool Unsaved { get; set; }

    /// <summary>The window's own state for this target while another tab is showing; null while this tab is the one showing.</summary>
    public object? Parked { get; set; }

    /// <summary>The name on the tab.</summary>
    public string Label => TargetTabWords.Label(Name);

    /// <summary>The state on the tab, "Analysis" or "Marking, not saved".</summary>
    public string State => TargetTabWords.State(Analysing, Unsaved);

    /// <summary>What a screen reader says for the tab: its name and its state.</summary>
    public string Spoken => Label + ", " + State;
}

/// <summary>The words on a target's tab, kept in one place so the tab, the tests and the guide say the same thing.</summary>
internal static class TargetTabWords
{
    /// <summary>A tab with nothing opened in it yet.</summary>
    public const string NewTarget = "New target";

    public const string Analysis = "Analysis";

    public const string Marking = "Marking";

    public const string NotSaved = "not saved";

    /// <summary>The dot's tooltip on a tab whose marks are all saved.</summary>
    public const string SavedDot = "Saved";

    /// <summary>The dot's tooltip on a tab with marks that are not saved.</summary>
    public const string UnsavedDot = "Not saved";

    /// <summary>The close button's name.</summary>
    public const string Close = "Close this target";

    /// <summary>The "+" tab's name.</summary>
    public const string Add = "Open another target";

    /// <summary>What the status line says when a tab cannot be left because the detection on it is still running.</summary>
    public const string WaitForDetection = "The detection on this target is still running. Wait for it to finish, or cancel it, before switching targets.";

    public static string Label(string? name) => string.IsNullOrWhiteSpace(name) ? NewTarget : name.Trim();

    public static string State(bool analysing, bool unsaved) => (analysing ? Analysis : Marking) + (unsaved ? ", " + NotSaved : "");
}

/// <summary>
/// The targets open at once, in the order of their tabs, and which one is showing. Pure bookkeeping: the window binds the target showing in
/// and out, and this decides which tab comes next, which one shows after a close, and whether a close has to ask first.
/// </summary>
internal sealed class OpenTargets
{
    private readonly List<OpenTarget> tabs = [];
    private int opened;

    /// <summary>Starts with one tab, the target the window opens on.</summary>
    public OpenTargets()
    {
        Current = New();
        tabs.Add(Current);
    }

    /// <summary>Every tab, left to right.</summary>
    public IReadOnlyList<OpenTarget> All => tabs;

    /// <summary>The tab showing.</summary>
    public OpenTarget Current { get; private set; }

    public int Count => tabs.Count;

    /// <summary>The position of the tab showing, from 0.</summary>
    public int CurrentIndex => tabs.IndexOf(Current);

    /// <summary>A new tab at the right-hand end, as the "+" tab opens one. It is not shown until <see cref="Show"/> is called.</summary>
    public OpenTarget Add()
    {
        var tab = New();
        tabs.Add(tab);
        return tab;
    }

    /// <summary>Makes a tab the one showing.</summary>
    public void Show(OpenTarget tab)
    {
        ArgumentNullException.ThrowIfNull(tab);
        if (!tabs.Contains(tab))
        {
            throw new ArgumentException("That tab is not open.", nameof(tab));
        }

        Current = tab;
    }

    /// <summary>The tab Ctrl+Tab goes to, the next one to the right and round to the first; Ctrl+Shift+Tab goes the other way.</summary>
    public OpenTarget Next(bool backwards = false)
    {
        int at = CurrentIndex;
        int step = backwards ? -1 : 1;
        return tabs[(at + step + tabs.Count) % tabs.Count];
    }

    /// <summary>Whether closing the tab has to ask first: it holds marks that are not saved.</summary>
    public static bool CloseAsks(OpenTarget tab) => tab?.Unsaved ?? false;

    /// <summary>
    /// Closes a tab and returns the one to show afterwards. Closing a tab that is not showing leaves the one showing where it is; closing the
    /// one showing shows its right-hand neighbour, or its left where it was the last. Closing the only tab leaves a new, empty one, so there is
    /// always a target to open a picture into.
    /// </summary>
    public OpenTarget Close(OpenTarget tab)
    {
        ArgumentNullException.ThrowIfNull(tab);
        int at = tabs.IndexOf(tab);
        if (at < 0)
        {
            throw new ArgumentException("That tab is not open.", nameof(tab));
        }

        tabs.RemoveAt(at);
        if (tabs.Count == 0)
        {
            Current = New();
            tabs.Add(Current);
            return Current;
        }

        if (ReferenceEquals(tab, Current))
        {
            Current = tabs[Math.Min(at, tabs.Count - 1)];
        }

        return Current;
    }

    private OpenTarget New() => new(++opened);
}

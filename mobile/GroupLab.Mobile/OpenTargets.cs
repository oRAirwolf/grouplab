namespace GroupLab.Mobile;

/// <summary>
/// What the list of open targets needs to know of one target, so the list and its words are tested without a screen. Concept A, several
/// targets open at once (Alan, 2026-10-07): the phone's half.
/// </summary>
internal interface IOpenTarget
{
    /// <summary>The target's name as the top of its screen shows it.</summary>
    string TargetName { get; }

    /// <summary>Whether a marking is open on it that is kept only when it is finished: marking by hand, or holes being fixed.</summary>
    bool Marking { get; }

    /// <summary>Whether that marking holds marks that closing the target would lose.</summary>
    bool UnsavedMarks { get; }

    /// <summary>The shots it has now.</summary>
    int Shots { get; }

    /// <summary>The session it is saved as, where it has been saved.</summary>
    long? SessionId { get; }
}

/// <summary>
/// The targets open on the phone at once, in the order they were opened, and the one on screen. Each keeps its own screen, its own state
/// and its own unsaved marks; switching shows another without closing any. Pure, with no screen in it: <see cref="Shell"/> holds one.
/// </summary>
internal sealed class OpenTargets<T>
    where T : class, IOpenTarget
{
    /// <summary>
    /// The most kept open. Each open target holds its picture in memory, which a phone has little of; past this the oldest that loses
    /// nothing by closing is closed, since an analysis is already saved in Sessions.
    /// </summary>
    internal const int Most = 8;

    private readonly List<T> open = [];

    /// <summary>Every open target, oldest first.</summary>
    public IReadOnlyList<T> All => open;

    public int Count => open.Count;

    /// <summary>The target on screen, or the one shown last.</summary>
    public T? Current { get; private set; }

    public bool Contains(T target) => open.Contains(target);

    /// <summary>
    /// A target opened, and now the one showing. Opening one already open only switches to it. Returns the targets closed to make room,
    /// never the one opened and never one with unsaved marks.
    /// </summary>
    public IReadOnlyList<T> Opened(T target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (!open.Contains(target))
        {
            open.Add(target);
        }

        Current = target;
        var closed = new List<T>();
        while (open.Count > Most && open.FirstOrDefault(t => !ReferenceEquals(t, target) && !OpenTargetWords.AsksBeforeClosing(t)) is { } oldest)
        {
            open.Remove(oldest);
            closed.Add(oldest);
        }

        return closed;
    }

    /// <summary>The open target saved as <paramref name="sessionId"/>, so a session is never open twice.</summary>
    public T? Find(long? sessionId) => sessionId is null ? null : open.FirstOrDefault(t => t.SessionId == sessionId);

    /// <summary>Makes an open target the one showing; false where it is not open.</summary>
    public bool SwitchTo(T target)
    {
        if (!open.Contains(target))
        {
            return false;
        }

        Current = target;
        return true;
    }

    /// <summary>
    /// A target closed. Where it was the one showing, the one to show next: the one after it in the list, else the one before, else none.
    /// Asking first is the caller's, by <see cref="OpenTargetWords.AsksBeforeClosing"/>.
    /// </summary>
    public T? Close(T target)
    {
        int at = open.IndexOf(target);
        if (at < 0)
        {
            return Current;
        }

        open.RemoveAt(at);
        if (ReferenceEquals(Current, target))
        {
            Current = open.Count == 0 ? null : open[Math.Min(at, open.Count - 1)];
        }

        return Current;
    }

    /// <summary>How many open targets have marks that are not saved.</summary>
    public int Unsaved => open.Count(OpenTargetWords.AsksBeforeClosing);

    public void Clear()
    {
        open.Clear();
        Current = null;
    }
}

/// <summary>The words of the open targets' sheet, in one place so the phone and its tests say the same.</summary>
internal static class OpenTargetWords
{
    public const string SheetTitle = "Open targets";

    public const string Analysis = "Analysis";

    public const string MarkingNotSaved = "Marking, not saved";

    public const string Showing = "showing now";

    public const string Another = "Open another target";

    public const string CloseConfirm = "Close and lose the marks";

    public const string KeepOpen = "Keep it open";

    public const string CloseWhy = "Its marks are not saved, and closing it loses them. A target is saved in Sessions once its marking is finished.";

    /// <summary>A target with a marking open asks before it is closed, and only where that marking holds something to lose.</summary>
    public static bool AsksBeforeClosing(IOpenTarget target) => target.Marking && target.UnsavedMarks;

    /// <summary>"Analysis", or "Marking, not saved".</summary>
    public static string State(IOpenTarget target) => target.Marking ? MarkingNotSaved : Analysis;

    /// <summary>The dim line under a row's name: its state, its shots, and whether it is the one on screen.</summary>
    public static string Detail(IOpenTarget target, bool showing)
    {
        var parts = new List<string> { State(target) };
        if (target.Shots > 0)
        {
            parts.Add(target.Shots == 1 ? "1 shot" : $"{target.Shots} shots");
        }

        if (showing)
        {
            parts.Add(Showing);
        }

        return string.Join(" · ", parts);
    }

    /// <summary>The small words beside the name at the top of a target's screen: how many are open, where more than one is.</summary>
    public static string Count(int open) => open > 1 ? $"{open} open" : "";

    /// <summary>What a screen reader says for the name at the top, which opens the sheet.</summary>
    public static string SwitcherName(string name, int open) =>
        name + ", " + (open > 1 ? $"{open} targets open, choose another" : "open targets");

    /// <summary>What a screen reader says for a row's close button.</summary>
    public static string CloseName(IOpenTarget target) => "Close " + target.TargetName;

    /// <summary>The question before closing a target with marks not saved.</summary>
    public static string CloseQuestion(IOpenTarget target) => $"Close {target.TargetName}?";

    /// <summary>The note when the oldest target was closed to make room for a new one.</summary>
    public static string MadeRoom(IOpenTarget target) =>
        $"{target.TargetName} was closed to make room" + (target.SessionId is null ? "." : "; it is still in Sessions.");
}

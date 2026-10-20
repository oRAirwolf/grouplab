using System.Globalization;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Marking;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 423, Alan, Quack and Jylee: several pictures of one set, dropped together, open in ONE tab and pooled
/// without being asked. One picture in the set's tab, its picture, its marking and its saved record, as a tab holds them for one sheet.
/// </summary>
internal sealed class SetMember
{
    internal SetMember(string path, string hash, int? sheet)
    {
        Path = path;
        Hash = hash;
        Sheet = sheet;
    }

    /// <summary>The picture.</summary>
    public string Path { get; }

    /// <summary>The picture's SHA-256, so the same picture dropped twice is one picture.</summary>
    public string Hash { get; }

    /// <summary>Which sheet of the set its codes say it is, from 0; null for a picture with no codes.</summary>
    public int? Sheet { get; }

    /// <summary>Whether it has been opened and read in the tab.</summary>
    public bool Read { get; set; }

    /// <summary>The window's state for this picture while it is not the one showing; null while it is, or before it is read.</summary>
    public object? Parked { get; set; }

    public string Name => System.IO.Path.GetFileName(Path);
}

/// <summary>
/// Entry 423: the pictures of one set in one tab. While the tab shows the pool, every member is parked here; while one member is shown to
/// be checked or fixed, the others are. The pool is worked out again from the members whenever it is shown, so a fix to one sheet is in it.
/// </summary>
internal sealed class SetGroup
{
    internal SetGroup(TargetDefinition? definition, string? definitionId)
    {
        Definition = definition;
        DefinitionId = definitionId;
    }

    /// <summary>The set's definition; null for pictures with no codes, pooled because the person said so.</summary>
    public TargetDefinition? Definition { get; }

    /// <summary>
    /// Entry 460 section 2: for copies of one single-page design pooled because the person said so, that design. They pool as pictures with
    /// no codes do, so <see cref="Definition"/> is null, but their codes read, so each is read and its holes found as it opens.
    /// </summary>
    public TargetDefinition? CopiesOf { get; init; }

    public string? DefinitionId { get; }

    public List<SetMember> Members { get; } = [];

    /// <summary>The member shown alone to be checked or fixed, or null while the pool is showing.</summary>
    public SetMember? Showing { get; set; }

    /// <summary>For a sheet in two different pictures, the one the person chose to count. Until then neither counts.</summary>
    public Dictionary<int, SetMember> Chosen { get; } = [];

    /// <summary>How many sheets the set has; for pictures with no codes, how many pictures there are.</summary>
    public int Size => Definition?.Tiling is { } set ? set.Cols * set.Rows : Members.Count;

    /// <summary>The sheets in two or more different pictures that nobody has chosen between yet.</summary>
    public IReadOnlyList<int> Contested => [.. Members.Where(m => m.Sheet is not null).GroupBy(m => m.Sheet!.Value)
        .Where(g => g.Count() > 1 && !Chosen.ContainsKey(g.Key)).Select(g => g.Key).Order()];

    /// <summary>Whether a member's sheet counts in the pool: its sheet is in no other picture, or it is the one chosen.</summary>
    public bool Counts(SetMember member) => member.Sheet is not { } k
        || (Chosen.TryGetValue(k, out var chosen) ? ReferenceEquals(chosen, member) : Members.Count(m => m.Sheet == k) == 1);

    /// <summary>The tab's name: "GroupLab 3x3 2 MOA, Set of 3 Letter Sheets, 3 sheets", or "3 pictures pooled".</summary>
    public string Name
    {
        get
        {
            if (Definition is null)
            {
                return CopiesOf is { } copied ? SetTabWords.CopiesName(copied.Name, Members.Count) : SetTabWords.PicturesName(Members.Count);
            }

            int sheets = Members.Select(m => m.Sheet).Distinct().Count();
            return string.Create(CultureInfo.InvariantCulture, $"{Definition.Name}, {sheets} sheet{(sheets == 1 ? "" : "s")}");
        }
    }

    /// <summary>A member's place in the set as a person says it: "sheet 2" from its codes, or "picture 2" by the order dropped.</summary>
    public string Called(SetMember member) => member.Sheet is { } k
        ? string.Create(CultureInfo.InvariantCulture, $"sheet {k + 1}")
        : string.Create(CultureInfo.InvariantCulture, $"picture {Members.IndexOf(member) + 1}");
}

/// <summary>What one dropped picture's codes said, read before deciding which tab it goes to.</summary>
internal sealed record DroppedPicture(string Path, string Hash, TargetDefinition? Definition, string? DefinitionId, int? SetSheet)
{
    /// <summary>Whether its codes named a sheet.</summary>
    public bool Named => Definition is not null;
}

/// <summary>
/// Where the pictures of one drop go: the sets of two or more sheets, the pictures alone, the pictures with no codes, and the copies of one
/// single-page design.
/// </summary>
internal sealed record DropSorting(IReadOnlyList<IReadOnlyList<DroppedPicture>> Sets, IReadOnlyList<DroppedPicture> Alone, IReadOnlyList<DroppedPicture> Unnamed,
    IReadOnlyList<IReadOnlyList<DroppedPicture>> Copies)
{
    /// <summary>
    /// Entry 423 sections 1 and 3: the same picture twice is one picture; pictures of one set (same definition, a set sheet number on each) go
    /// together where there are two or more of them; any other picture whose codes named it goes alone; the rest have no codes to read.
    /// Entry 460 section 2: two or more pictures of the same single-page design go together as copies, to be asked about once.
    /// </summary>
    public static DropSorting Of(IReadOnlyList<DroppedPicture> pictures)
    {
        ArgumentNullException.ThrowIfNull(pictures);
        var once = pictures.GroupBy(p => p.Hash, StringComparer.Ordinal).Select(g => g.First()).ToList();
        var sets = new List<IReadOnlyList<DroppedPicture>>();
        var copies = new List<IReadOnlyList<DroppedPicture>>();
        var alone = new List<DroppedPicture>();
        foreach (var group in once.Where(p => p.Named).GroupBy(Key, StringComparer.Ordinal))
        {
            var list = group.ToList();
            if (list.Count < 2)
            {
                alone.AddRange(list);
            }
            else if (list[0].SetSheet is null)
            {
                copies.Add(list);
            }
            else
            {
                sets.Add(list);
            }
        }

        return new DropSorting(sets, alone, [.. once.Where(p => !p.Named)], copies);

        // A sheet of a set by its set, a copy of a single-page design by its design, and anything else by itself.
        static string Key(DroppedPicture p) => p.SetSheet is not null ? p.DefinitionId ?? ""
            : p.Definition is { } d && CopiesQuestion.SinglePage(d) && p.DefinitionId is { } id ? "\u0001" + id
            : "\u0000" + p.Hash;
    }
}

/// <summary>The words of a set's tab, in one place for the window, the tests and the guide.</summary>
internal static class SetTabWords
{
    public const string PoolQuestionDefault = "Pool them in one tab";

    public const string PoolQuestionOther = "One tab each";

    public const string CheckOneSheet = "Check one sheet";

    public const string BackToSet = "Back to the set";

    public const string PoolSaved = "Each sheet is saved as its own session in grouplab.db · safe to close";

    public const string WaitForSet = "GroupLab is reading the sheets of a set in this tab. Wait for it to finish before switching targets.";

    public static string PoolQuestion(int n) => string.Create(CultureInfo.InvariantCulture, $"Pool these {n} pictures as one group?");

    public static string PicturesName(int n) => string.Create(CultureInfo.InvariantCulture, $"{n} pictures pooled");

    /// <summary>Entry 460 section 2: "GroupLab 3x3 2 MOA, Letter, 3 copies pooled".</summary>
    public static string CopiesName(string design, int n) => string.Create(CultureInfo.InvariantCulture, $"{design}, {n} copies pooled");

    public static string Reading(int k, int n, string called) => string.Create(CultureInfo.InvariantCulture, $"Reading sheet {k} of {n} ({called}). ");
}

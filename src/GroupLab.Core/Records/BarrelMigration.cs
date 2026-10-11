using System.Globalization;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Records;

/// <summary>What the move to barrels under their rifle found and did, before and after, for the report and the store's meta table.</summary>
/// <param name="RiflesBefore">Rifles before.</param>
/// <param name="RiflesAfter">Rifles after: one more where a rifle for unplaced barrels had to be made.</param>
/// <param name="BarrelsBefore">Barrels before.</param>
/// <param name="BarrelsAfter">Barrels after: more where a rifle's own length and twist became its first barrel.</param>
/// <param name="WithoutRifleBefore">Barrels that named no rifle in the book, before.</param>
/// <param name="WithoutRifleAfter">The same after, which is always nought.</param>
/// <param name="PlacedBySessions">Barrels placed under the one rifle their sessions name.</param>
/// <param name="NotYetOnARifle">Barrels put under <see cref="RecordBook.NotYetOnARifle"/>.</param>
/// <param name="MadeFromRifles">Barrels made from a rifle's own length and twist.</param>
public sealed record BarrelMove(int RiflesBefore, int RiflesAfter, int BarrelsBefore, int BarrelsAfter, int WithoutRifleBefore, int WithoutRifleAfter,
    int PlacedBySessions, int NotYetOnARifle, int MadeFromRifles)
{
    /// <summary>One line, as the store keeps it and the report gives it.</summary>
    public string Says() => string.Create(CultureInfo.InvariantCulture,
        $"rifles {RiflesBefore} to {RiflesAfter}, barrels {BarrelsBefore} to {BarrelsAfter}, barrels with no rifle {WithoutRifleBefore} to {WithoutRifleAfter}; {PlacedBySessions} placed by their sessions, {NotYetOnARifle} under \"{RecordBook.NotYetOnARifle}\", {MadeFromRifles} made from a rifle's own length and twist");
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 407 section 5.5 (concept 5B, Alan's pick): every barrel goes under a rifle. Before this a barrel was a list of its
/// own, chosen per session, and its rifle was an optional field that most barrels left empty.
/// <list type="number">
/// <item>A rifle with its own barrel length, twist or twist direction and no barrel under it gets a first barrel holding them, named after the
/// rifle, and it is the barrel on now.</item>
/// <item>A barrel naming no rifle in the book goes under the rifle its sessions name, where they name exactly one.</item>
/// <item>Any other goes under a rifle named <see cref="RecordBook.NotYetOnARifle"/>, made for it, which the person can move it from.</item>
/// <item>A rifle with exactly one barrel and none marked on now has that barrel on now.</item>
/// </list>
/// Nothing is taken away: every barrel keeps its name, so every session still names the same rifle and barrel. Running it twice changes
/// nothing the second time.
/// </summary>
public static class BarrelMigration
{
    /// <param name="book">The records as they are.</param>
    /// <param name="sessions">Each saved session's rifle and barrel, as the sessions table holds them.</param>
    public static (RecordBook Book, BarrelMove Move) Run(RecordBook book, IEnumerable<(string? Rifle, string? Barrel)> sessions)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(sessions);
        var used = sessions.ToList();
        int riflesBefore = book.Rifles.Count, barrelsBefore = book.Barrels.Count;
        bool Known(string? rifle) => rifle is not null && book.FindRifle(rifle) is not null;
        int withoutBefore = book.Barrels.Count(b => !Known(b.Rifle));
        int placed = 0, stray = 0, made = 0;

        // 1. A rifle's own length and twist become its first barrel.
        foreach (var rifle in book.Rifles.ToList())
        {
            bool hasOwn = rifle.BarrelLengthInches is not null || rifle.TwistInches is not null || rifle.TwistDirection is not null;
            if (!hasOwn || book.BarrelsOf(rifle.Name).Count > 0)
            {
                continue;
            }

            string name = Unique(book, rifle.Name + " barrel");
            book = book.With(new Barrel(name, rifle.Name, 0)
            {
                LengthInches = rifle.BarrelLengthInches,
                TwistInches = rifle.TwistInches,
                TwistDirection = rifle.TwistDirection,
            }).PutOn(rifle.Name, name);
            made++;
        }

        // 2 and 3. Every barrel under a rifle.
        foreach (var barrel in book.Barrels.Where(b => !Known(b.Rifle)).ToList())
        {
            var rifles = used.Where(s => string.Equals(s.Barrel?.Trim(), barrel.Name.Trim(), StringComparison.OrdinalIgnoreCase) && s.Rifle is not null)
                .Select(s => book.FindRifle(s.Rifle)?.Name).Where(n => n is not null).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (rifles.Count == 1)
            {
                book = book.With(barrel with { Rifle = rifles[0] });
                placed++;
                continue;
            }

            if (book.FindRifle(RecordBook.NotYetOnARifle) is null)
            {
                var usual = ScopeClicks.Usual(AngularUnit.Moa);
                book = book.With(new Rifle(RecordBook.NotYetOnARifle, usual.Value, usual.Unit)
                {
                    Notes = "Made by GroupLab for barrels whose rifle it could not tell. Open each barrel and choose its rifle.",
                });
            }

            book = book.With(barrel with { Rifle = RecordBook.NotYetOnARifle });
            stray++;
        }

        // 4. A rifle's only barrel is the one on it.
        foreach (var rifle in book.Rifles.Where(r => r.BarrelOn is null || book.OnNow(r.Name) is null).ToList())
        {
            if (book.BarrelsOf(rifle.Name) is { Count: 1 } only && !string.Equals(rifle.Name, RecordBook.NotYetOnARifle, StringComparison.Ordinal))
            {
                book = book.PutOn(rifle.Name, only[0].Name);
            }
        }

        var move = new BarrelMove(riflesBefore, book.Rifles.Count, barrelsBefore, book.Barrels.Count, withoutBefore,
            book.Barrels.Count(b => b.Rifle is null || book.FindRifle(b.Rifle) is null), placed, stray, made);
        return (book, move);
    }

    private static string Unique(RecordBook book, string name)
    {
        string tried = name;
        for (int n = 2; book.FindBarrel(tried) is not null; n++)
        {
            tried = string.Create(CultureInfo.InvariantCulture, $"{name} {n}");
        }

        return tried;
    }
}

"""The scopes and magnifications the aim point score sheet asks for, and the checks that keep the sheet possible to fill in.

NOTES-FROM-PLANNING.md entries 226 section 3 and 229 section 5.3. The rows used to be typed by hand. The developer's sheet of 2026-09-26 listed the
PLxC 1-8x at 4x, 8x and 8x again, the second 8x a 50 yd row whose distance change was printed like every other cell and so looked like a
repeat; he and Justin tested 4x, 6x and 8x. Justin's older sheet, the card's own page 2, had the friend's scope rows printed 10x, 18x and
"max", copied from the high power scopes, and he wrote the PLxC's 4, 6 and 8 over them. So the rows are built from this table, and building
them fails on a magnification the scope does not have or one asked for twice at the same distance. A change of distance gets its own bold
heading row.
"""

# name, lowest and highest magnification, then the magnifications to score at 100 yd, then any (magnification, yards) rows elsewhere.
SCOPES = [
    ("Razor HD Gen III 6-36x56", 6, 36, [10, 18, 25, 36], []),
    ("DNT TheOne 7-35x56", 7, 35, [10, 18, 25, 35], []),
    ("Strike Eagle 5-25x56", 5, 25, [10, 18, 25], []),
    ("PLxC 1-8x24 (FFP)", 1, 8, [4, 6, 8], []),
]

FRIEND_ROWS = 3


def rows(scopes=SCOPES):
    """The sheet's rows, in order: ("scope", name) opens a scope, ("distance", yards) a change of distance, ("score", mag, yards) a row
    to fill in, and ("friend", None, 100) a blank row for a scope the sheet does not know."""
    out = []
    for name, low, high, at_100, elsewhere in scopes:
        seen = set()
        out.append(("scope", name))
        for yards, mags in [(100, at_100)] + [(y, [m for m, yy in elsewhere if yy == y]) for y in sorted({y for _, y in elsewhere})]:
            if yards != 100:
                out.append(("distance", yards))
            for mag in mags:
                if not low <= mag <= high:
                    raise ValueError(f"{name} goes from {low}x to {high}x, and the sheet asks for {mag}x")
                if (mag, yards) in seen:
                    raise ValueError(f"{name} is asked for {mag}x at {yards} yd twice")
                seen.add((mag, yards))
                out.append(("score", mag, yards))
    out.append(("scope", "Friend's scope:"))
    out.extend(("friend", None, 100) for _ in range(FRIEND_ROWS))
    return out

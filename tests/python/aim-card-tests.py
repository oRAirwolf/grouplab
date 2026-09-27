#!/usr/bin/env python3
"""The aim point score sheet's rows, NOTES-FROM-PLANNING.md entries 226 section 3 and 229 section 5.

Every magnification on the sheet is one the scope has, none is asked for twice at one distance, a change of distance opens its own heading,
and a table that breaks either rule is refused rather than printed. The sheet's words are in American spelling.

    python3 tests/python/aim-card-tests.py
"""

from __future__ import annotations

import importlib.util
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
FIGURES = REPO / "website" / "research" / "can-you-see-the-bull" / "figures"
spec = importlib.util.spec_from_file_location("scopes", FIGURES / "scopes.py")
scopes = importlib.util.module_from_spec(spec)
spec.loader.exec_module(scopes)

passed = 0
failed: list[str] = []


def check(what: str, ok: bool, detail: str = "") -> None:
    global passed
    if ok:
        passed += 1
        print("ok   " + what)
    else:
        failed.append(what)
        print("FAIL " + what + (": " + detail if detail else ""))


rows = scopes.rows()
current = None
for row in rows:
    if row[0] == "scope":
        current = next((s for s in scopes.SCOPES if s[0] == row[1]), None)
    elif row[0] == "score":
        check(f"{current[0]} has {row[1]}x", current[1] <= row[1] <= current[2])
plxc = [r[1] for r in rows[rows.index(("scope", "PLxC 1-8x24 (FFP)")):] if r[0] == "score"]
check("the PLxC is scored at 4x, 6x and 8x, as Alan and Justin tested it", plxc[:3] == [4, 6, 8], str(plxc))
per_scope: dict[str, list] = {}
for row in rows:
    if row[0] == "scope":
        name = row[1]
    elif row[0] == "score":
        per_scope.setdefault(name, []).append((row[1], row[2]))
check("no scope is asked for one magnification twice at one distance", all(len(v) == len(set(v)) for v in per_scope.values()))


def refused(table) -> bool:
    try:
        scopes.rows(table)
    except ValueError:
        return True
    return False


check("a magnification the scope does not have is refused", refused([("PLxC 1-8x24", 1, 8, [4, 18], [])]))
check("the same magnification twice at 100 yd is refused", refused([("PLxC 1-8x24", 1, 8, [4, 8, 8], [])]))
elsewhere = scopes.rows([("PLxC 1-8x24", 1, 8, [4, 6, 8], [(8, 50)])])
check("8x at 50 yd is allowed beside 8x at 100, under its own heading", ("distance", 50) in elsewhere
      and elsewhere.index(("distance", 50)) < elsewhere.index(("score", 8, 50)))

for name in ("score.py", "card.py"):
    words = " ".join(re.findall(r'"([^"]*)"', (FIGURES / name).read_text(encoding="utf-8")))
    british = re.findall(r"\b(centre|centred|favourite|colour|metre)s?\b", words)
    check(f"{name} prints American spelling", not british, ", ".join(british))

print(f"{passed} passed, {len(failed)} failed")
sys.exit(1 if failed else 0)

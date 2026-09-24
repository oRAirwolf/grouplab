#!/usr/bin/env python3
"""Did anything that ships inside the executable change?

NOTES-FROM-PLANNING.md entries 150 and 168. Alan, reading the releases page: a lot of builds were
release note pages brought up to date or research articles written, and each one compiled and tested a
new executable. Entry 150 built a gate. Nightly 94 went straight through it, and its own notes said
nothing in it changed the application.

**Why the first gate failed.** It classified by top level directory, and `tests`, `scripts` and
`.github` were all "ships". So a new test, the release notes generator or a site script produced a
release. What made nightly 94 build was `scripts/claims.py`, `scripts/counts.py`,
`scripts/split-logs.py` and five test files, none of which is in the executable.

**Three classes, entry 168 section 2.2.**

- **ships**: an input to the published executable or its package. Taken from the build, not from a
  judgement about directories: `--generate` asks MSBuild what `dotnet publish src/GroupLab.App` reads,
  follows every project reference, and adds what the packaging scripts copy. A release is produced.
- **checked**: tests, tools, the build and release tooling, the workflows. CI runs the full suite and a
  failure still blocks main, but no release is produced.
- **content**: the website, the documentation, the research, the release notes. Published by the site.

A path matched by the generated ships list is ships. Anything else takes its top level entry's class
from `.github/shipping-paths.json`. **A path in neither is a failure**, because a new top level directory
should make somebody decide which side it is on.

    python3 scripts/shipping-gate.py --decide <base> <head>   yes or no, with the paths that decided it
    python3 scripts/shipping-gate.py --lists                  every top level entry is in exactly one class
    python3 scripts/shipping-gate.py --generate               rewrite the ships list from what the build reads
    python3 scripts/shipping-gate.py --check-generated        fail if the ships list is not what the build reads
    python3 scripts/shipping-gate.py --history 30             what the gate would have done to the last N nightlies
"""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
from pathlib import Path, PureWindowsPath

HERE = Path(__file__).resolve().parent.parent
PATHS = HERE / ".github" / "shipping-paths.json"
GENERATED = HERE / ".github" / "shipping-generated.json"

# The one published project. Its project references are followed, so Core and Cli come in through it.
PUBLISHED = "src/GroupLab.App/GroupLab.App.csproj"

# The item types a build reads into the executable. Content and None only count where they are copied out.
READ = ["Compile", "EmbeddedResource", "AvaloniaResource", "AvaloniaXaml", "Content", "None", "ProjectReference"]


def config() -> dict:
    return json.loads(PATHS.read_text(encoding="utf-8"))


def lists() -> tuple[dict[str, str], dict[str, str]]:
    """The top level defaults: checked and content. Kept under the old name for the tests that read it."""
    data = config()
    return data["checked"], data["content"]


def git(*args: str) -> str:
    return subprocess.run(["git", *args], cwd=HERE, capture_output=True, text=True, check=True).stdout.strip()


def ships_list() -> list[str]:
    """The generated list: exact files, and directories ending in a slash whose every tracked file ships."""
    if not GENERATED.exists():
        return []
    return json.loads(GENERATED.read_text(encoding="utf-8"))["ships"]


def side(path: str) -> str | None:
    """ships, checked or content for one changed path, or None where nothing says."""
    for entry in ships_list():
        if path == entry or (entry.endswith("/") and path.startswith(entry)):
            return "ships"
    checked, content = lists()
    first = path.split("/", 1)[0]
    if first in checked:
        return "checked"
    if first in content:
        return "content"
    return None


# ---------------------------------------------------------------------------------------------- generate

def relative(full: str) -> str | None:
    """A path MSBuild reported, relative to the repository, or None where it is outside it (the SDK, NuGet)."""
    try:
        return Path(full).resolve().relative_to(HERE).as_posix()
    except ValueError:
        return None


def evaluate(project: Path) -> dict:
    out = subprocess.run(
        ["dotnet", "msbuild", str(project), "-p:Configuration=Release",
         *[f"-getItem:{i}" for i in READ], "-getProperty:MSBuildAllProjects", "-getProperty:DirectoryBuildPropsPath"],
        cwd=HERE, capture_output=True, text=True, check=True).stdout
    return json.loads(out)


def read_by_build() -> set[str]:
    """Every file in this repository that `dotnet publish` of the application reads."""
    found: set[str] = set()
    todo, seen = [HERE / PUBLISHED], set()
    while todo:
        project = todo.pop().resolve()
        if project in seen:
            continue
        seen.add(project)
        found.add(relative(str(project)) or "")
        data = evaluate(project)

        props = data.get("Properties", {})
        for p in props.get("MSBuildAllProjects", "").split(";") + [props.get("DirectoryBuildPropsPath", "")]:
            if p and (r := relative(p)):
                found.add(r)

        for kind, items in data.get("Items", {}).items():
            for item in items:
                full = item.get("FullPath") or str(project.parent / PureWindowsPath(item["Identity"]).as_posix())
                if kind == "ProjectReference":
                    todo.append(Path(full))
                    continue
                if kind in ("Content", "None"):
                    copied = (item.get("CopyToOutputDirectory") or "") + (item.get("CopyToPublishDirectory") or "")
                    if not copied or copied.lower() in ("never", "nevernever"):
                        continue
                if r := relative(full):
                    found.add(r)

    found.discard("")
    return found


def packaged() -> dict[str, str]:
    """What the packaging reads that MSBuild does not: from the config file, each with its reason."""
    return config()["ships-packaging"]


def compress(files: set[str]) -> list[str]:
    """Directories whose every tracked file ships become one entry, so a new source file is covered by its folder."""
    tracked = [p for p in git("ls-files").splitlines() if p]
    by_dir: dict[str, list[str]] = {}
    for p in tracked:
        parts = p.split("/")
        for i in range(1, len(parts)):
            by_dir.setdefault("/".join(parts[:i]) + "/", []).append(p)

    out: list[str] = []
    for f in sorted(files):
        if any(f.startswith(d) for d in out if d.endswith("/")):
            continue
        # The widest folder above this file whose tracked files all ship.
        parts = f.split("/")
        chosen = f
        for i in range(1, len(parts)):
            d = "/".join(parts[:i]) + "/"
            if d.count("/") < 2:
                continue    # never a whole top level entry: those have a class of their own
            under = by_dir.get(d, [])
            if under and all(u in files for u in under):
                chosen = d
                break
        out.append(chosen)
    return sorted(set(out))


def generated_now() -> dict:
    files = read_by_build() | set(packaged())
    return {
        "_": ["Generated by scripts/shipping-gate.py --generate from what dotnet publish of the application reads,",
              "and from ships-packaging in shipping-paths.json. Do not edit: CI regenerates it and fails if it differs."],
        "ships": compress(files),
    }


def generate(check: bool) -> int:
    wanted = json.dumps(generated_now(), indent=2) + "\n"
    have = GENERATED.read_text(encoding="utf-8") if GENERATED.exists() else ""
    if check:
        if have.replace("\r\n", "\n") != wanted:
            print(".github/shipping-generated.json is not what the build reads. Run: python3 scripts/shipping-gate.py --generate",
                  file=sys.stderr)
            return 1
        print("the ships list is what the build reads.")
        return 0
    GENERATED.write_text(wanted, encoding="utf-8", newline="\n")
    print(f"{len(json.loads(wanted)['ships'])} entries written to {GENERATED.relative_to(HERE).as_posix()}")
    return 0


# ---------------------------------------------------------------------------------------------- decide

def decide(base: str, head: str) -> int:
    """yes when something that ships changed between the two commits, no when only checked or content did."""
    changed = [p for p in git("diff", "--name-only", f"{base}..{head}").splitlines() if p]
    if not changed:
        print("application-changed=no")
        print("nothing changed at all between those two commits")
        return 0

    unknown = sorted({p.split("/", 1)[0] for p in changed if side(p) is None})
    if unknown:
        print("application-changed=fail", file=sys.stderr)
        for name in unknown:
            print(f"{name} is in no class in .github/shipping-paths.json", file=sys.stderr)
        print("Decide which class it is and add it. A path in no class is a failure, not a default.", file=sys.stderr)
        return 2

    shipping = [p for p in changed if side(p) == "ships"]
    print(f"application-changed={'yes' if shipping else 'no'}")
    if shipping:
        for p in shipping[:20]:
            print(f"  {p}")
        if len(shipping) > 20:
            print(f"  and {len(shipping) - 20} more")
    else:
        checked = sum(1 for p in changed if side(p) == "checked")
        print(f"  {len(changed)} changed path{'' if len(changed) == 1 else 's'}: {checked} checked and "
              f"{len(changed) - checked} content, and nothing that ships")
    return 0


def check_lists() -> int:
    """Every top level entry has exactly one default class, and no class names something absent."""
    checked, content = lists()
    here = {p for p in git("ls-tree", "--name-only", "HEAD").splitlines() if p}
    faults = [f"{n} is in both checked and content" for n in sorted(set(checked) & set(content))]
    faults += [f"{n} is in no class" for n in sorted(here - set(checked) - set(content))]
    faults += [f"{n} is listed but is not in the repository" for n in sorted((set(checked) | set(content)) - here)]
    for fault in faults:
        print(fault, file=sys.stderr)
    if faults:
        return 1
    print(f"{len(here)} top level entries, {len(checked)} checked and {len(content)} content by default, "
          f"with {len(ships_list())} ships entries taken from the build.")
    return 0


def history(count: int) -> int:
    """What the gate would have done to the last N nightlies, against the real history rather than a fixture."""
    tags = sorted(git("tag", "--list", "v*-nightly.*").splitlines(),
                  key=lambda t: int(t.rsplit(".", 1)[1]), reverse=True)[:count + 1]
    skipped = 0
    for newer, older in zip(tags, tags[1:]):
        a, b = git("rev-list", "-n1", older), git("rev-list", "-n1", newer)
        changed = [p for p in git("diff", "--name-only", f"{a}..{b}").splitlines() if p]
        shipping = [p for p in changed if side(p) == "ships"]
        unknown = sorted({p.split("/", 1)[0] for p in changed if side(p) is None})
        verdict = ("unclassified: " + ", ".join(unknown)) if unknown else ("built" if shipping else "SKIPPED")
        skipped += verdict == "SKIPPED"
        print(f"{newer:<28} {len(changed):>4} changed  {verdict}")
    print(f"\n{skipped} of the last {len(tags) - 1} nightlies would not have been built.")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Did anything that ships inside the executable change?")
    parser.add_argument("--decide", nargs=2, metavar=("BASE", "HEAD"))
    parser.add_argument("--lists", action="store_true")
    parser.add_argument("--generate", action="store_true")
    parser.add_argument("--check-generated", action="store_true")
    parser.add_argument("--history", type=int, metavar="N")
    args = parser.parse_args()

    if args.decide:
        return decide(*args.decide)
    if args.lists:
        return check_lists()
    if args.generate or args.check_generated:
        return generate(check=args.check_generated)
    if args.history:
        return history(args.history)
    parser.print_help()
    return 1


if __name__ == "__main__":
    sys.exit(main())

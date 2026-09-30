#!/usr/bin/env python3
"""The phones Firebase Test Lab runs GroupLab Dev's scenario on, chosen from Test Lab's own list each time.

NOTES-FROM-PLANNING.md entry 318 section 3. Test Lab's catalogue changes as phones are added and retired, so no model is written into the
workflow: this reads `gcloud firebase test android models list --format=json` on standard input and prints one `--device` value a line:

- one real phone from each of Samsung, Google (Pixel) and Xiaomi or Oppo, where Test Lab offers one, each on its newest Android version;
- one virtual phone, on its newest version.

That is three real-phone runs and one virtual one a day, inside the free plan's five and ten (entry 315, amendment 2: nothing paid without
Alan). A model tagged deprecated is never chosen. --self-test holds the choice to a made-up catalogue.
"""

from __future__ import annotations

import json
import sys

# One real phone from each maker, in this order; the last group takes whichever of the two Test Lab offers.
MAKERS = (("samsung",), ("google",), ("xiaomi", "oppo"))
MOST_PHYSICAL = 3


def newest(model: dict) -> str | None:
    """The model's newest Android version id, as Test Lab numbers them (API levels, compared as numbers)."""
    versions = [v for v in model.get("supportedVersionIds", []) if str(v).isdigit()]
    return max(versions, key=int) if versions else None


def usable(model: dict) -> bool:
    tags = [str(t).lower() for t in model.get("tags", [])]
    return newest(model) is not None and not any("deprecated" in t for t in tags)


def choose(models: list[dict]) -> list[str]:
    """The --device values: real phones by maker first, then one virtual phone."""
    physical = [m for m in models if m.get("form") == "PHYSICAL" and usable(m)]
    virtual = [m for m in models if m.get("form") == "VIRTUAL" and usable(m)]
    chosen: list[dict] = []
    for makers in MAKERS:
        pool = [m for m in physical if str(m.get("manufacturer", "")).lower() in makers and m not in chosen]
        if pool:
            # The newest Android first, then the id, so the choice is the same from one day to the next.
            chosen.append(sorted(pool, key=lambda m: (-int(newest(m) or 0), str(m.get("id"))))[0])
    chosen = chosen[:MOST_PHYSICAL]
    if virtual:
        chosen.append(sorted(virtual, key=lambda m: (-int(newest(m) or 0), str(m.get("id"))))[0])
    return [f"model={m['id']},version={newest(m)},locale=en,orientation=portrait" for m in chosen]


def self_test() -> int:
    catalogue = [
        {"id": "a52", "manufacturer": "Samsung", "form": "PHYSICAL", "supportedVersionIds": ["30", "33"]},
        {"id": "b0q", "manufacturer": "Samsung", "form": "PHYSICAL", "supportedVersionIds": ["34"]},
        {"id": "oriole", "manufacturer": "Google", "form": "PHYSICAL", "supportedVersionIds": ["33", "34"]},
        {"id": "old", "manufacturer": "Google", "form": "PHYSICAL", "supportedVersionIds": ["35"], "tags": ["deprecated=35"]},
        {"id": "CPH2305", "manufacturer": "OPPO", "form": "PHYSICAL", "supportedVersionIds": ["31"]},
        {"id": "MediumPhone.arm", "manufacturer": "Generic", "form": "VIRTUAL", "supportedVersionIds": ["33", "34", "35"]},
        {"id": "Nexus5", "manufacturer": "LG", "form": "VIRTUAL", "supportedVersionIds": ["23"]},
    ]
    got = choose(catalogue)
    want = [
        "model=b0q,version=34,locale=en,orientation=portrait",
        "model=oriole,version=34,locale=en,orientation=portrait",
        "model=CPH2305,version=31,locale=en,orientation=portrait",
        "model=MediumPhone.arm,version=35,locale=en,orientation=portrait",
    ]
    if got != want:
        print("testlab-devices.py self-test: failed", got)
        return 1
    if choose([]) != []:
        print("testlab-devices.py self-test: failed on an empty catalogue")
        return 1
    print("testlab-devices.py self-test: passed")
    return 0


def main(argv: list[str]) -> int:
    if argv[1:] == ["--self-test"]:
        return self_test()
    devices = choose(json.load(sys.stdin))
    if not devices:
        print("Test Lab offered no phone GroupLab can use.", file=sys.stderr)
        return 1
    print("\n".join(devices))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))

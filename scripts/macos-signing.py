#!/usr/bin/env python3
"""Whether the nightly can sign and notarize the macOS build, and why not, without ever printing a secret.

NOTES-FROM-PLANNING.md entry 306, Alan: "Mac notarization: yes". A Developer ID Application signature with the hardened runtime, then
Apple's notarization, lets macOS open GroupLab normally, with no Terminal command first. It needs two secrets of its own, which Alan sets in
request 55 after the iOS ones, and four it shares with the iOS build: the team and the App Store Connect API key notarytool signs in with.

    python3 scripts/macos-signing.py --check      one line per secret and the decision; exit 0 sign, 3 unsigned, 1 malformed
    python3 scripts/macos-signing.py --self-test  checks the checker against made-up values, none of them real

Until both of the Mac's own secrets are set, the build is left unsigned as it always was, whatever the shared ones are, so a nightly never
fails for want of them. Once either is set, all six are checked for their shape, and a malformed or missing one fails loudly, naming the
secret and never its value. The decision is also written to $GITHUB_OUTPUT as `sign=true` or `sign=false` when that variable is set.
"""
from __future__ import annotations

import base64
import importlib.util
import os
import sys
from pathlib import Path

OWN = ["MACOS_DEVID_CERT_P12", "MACOS_DEVID_CERT_PASSWORD"]
SHARED = ["APPLE_TEAM_ID", "APPLE_API_ISSUER_ID", "APPLE_API_KEY_ID", "APPLE_API_KEY_P8"]
SECRETS = OWN + SHARED

_spec = importlib.util.spec_from_file_location("ios_signing", Path(__file__).with_name("ios-signing.py"))
ios = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(ios)


def problem(name: str, value: str, env: dict[str, str]) -> str | None:
    """What is wrong with one present secret, in words that never contain it; None where its shape is right."""
    if name == "MACOS_DEVID_CERT_P12":
        return ios.problem("IOS_DIST_CERT_P12", value, env)
    if name == "MACOS_DEVID_CERT_PASSWORD":
        return None if value.strip() else "is empty"
    return ios.problem(name, value, env)


def decide(env: dict[str, str]) -> tuple[str, list[str]]:
    """The decision (sign, unsigned or malformed) and one line per secret."""
    if not any(env.get(n, "").strip() for n in OWN):
        return "unsigned", [f"{n}: not set" for n in OWN] + [
            "Neither of the Mac's own secrets is set, so the Mac build is not signed or notarized (request 55, the Mac steps)."]
    lines = []
    faults = 0
    for name in SECRETS:
        value = env.get(name, "")
        if not value.strip():
            lines.append(f"{name}: not set")
            faults += 1
            continue
        why = problem(name, value, env)
        lines.append(f"{name}: set, " + ("its shape is right" if why is None else "MALFORMED: " + why))
        faults += why is not None
    if faults:
        return "malformed", lines + [f"{faults} of the six are missing or malformed; the Mac build is not signed. Set them as request 55 says."]
    return "sign", lines + ["All six are set and look right; the Mac build is signed with the hardened runtime and notarized."]


def self_test() -> int:
    failed = 0
    p12 = base64.b64encode(b"\x30" + b"\x00" * 200).decode()
    p8 = "-----BEGIN PRIVATE KEY-----\nMIGTAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBHkwdwIBAQQg\n-----END PRIVATE KEY-----\n"
    good = {"MACOS_DEVID_CERT_P12": p12, "MACOS_DEVID_CERT_PASSWORD": "a password", "APPLE_TEAM_ID": "ABCDE12345",
            "APPLE_API_ISSUER_ID": "12345678-1234-1234-1234-123456789012", "APPLE_API_KEY_ID": "KEY1234567", "APPLE_API_KEY_P8": p8}

    def expect(what: str, env: dict[str, str], want: str) -> None:
        nonlocal failed
        got, lines = decide(env)
        leaked = any(v in line for v in env.values() if len(v) > 3 for line in lines)
        if got != want or leaked:
            failed += 1
            print(f"FAIL {what}: {got}{' and a value was printed' if leaked else ''}")

    expect("nothing set", {}, "unsigned")
    expect("only the iOS build's shared secrets", {k: good[k] for k in SHARED}, "unsigned")
    expect("everything right", good, "sign")
    expect("the certificate without its password", {k: v for k, v in good.items() if k != "MACOS_DEVID_CERT_PASSWORD"}, "malformed")
    expect("a certificate that is not base64", {**good, "MACOS_DEVID_CERT_P12": "not base64 at all!"}, "malformed")
    expect("no API key for notarytool", {k: v for k, v in good.items() if k != "APPLE_API_KEY_P8"}, "malformed")
    print("macos-signing self-test: " + ("passed" if not failed else f"{failed} failed"))
    return 1 if failed else 0


def main(argv: list[str]) -> int:
    if argv[1:2] == ["--self-test"]:
        return self_test()
    if argv[1:2] != ["--check"]:
        print(__doc__)
        return 2
    decision, lines = decide(dict(os.environ))
    for line in lines:
        print(line)
    if out := os.environ.get("GITHUB_OUTPUT"):
        with open(out, "a", encoding="utf-8", newline="\n") as f:
            f.write(f"sign={'true' if decision == 'sign' else 'false'}\n")
    return {"sign": 0, "unsigned": 3, "malformed": 1}[decision]


if __name__ == "__main__":
    sys.exit(main(sys.argv))

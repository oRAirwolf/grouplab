#!/usr/bin/env python3
"""Whether the nightly can sign the iOS build and send it to TestFlight, and why not, without ever printing a secret.

NOTES-FROM-PLANNING.md entry 290 section 2 item 7 and docs/IOS-PLAN.md section 3. The nightly's iOS job reads seven repository secrets that
Alan sets himself (request 55). Until all seven are there, the job builds without signing and uploads nothing, so a nightly never fails for
want of them. Once any of them is there, each is checked for its shape before anything uses it, and a malformed one fails loudly, saying
which secret and what is wrong with it, never its value.

    python3 scripts/ios-signing.py --check        prints one line per secret and the decision; exit 0 sign, 3 unsigned, 1 malformed
    python3 scripts/ios-signing.py --self-test    checks the checker against made-up values, none of them real

The decision is also written to $GITHUB_OUTPUT as `sign=true` or `sign=false` when that variable is set.
"""
from __future__ import annotations

import base64
import binascii
import os
import re
import sys

SECRETS = [
    "APPLE_TEAM_ID",
    "IOS_DIST_CERT_P12",
    "IOS_DIST_CERT_PASSWORD",
    "IOS_PROFILE",
    "APPLE_API_ISSUER_ID",
    "APPLE_API_KEY_ID",
    "APPLE_API_KEY_P8",
]

# The bundle the profile must be for: docs/IOS-PLAN.md section 2.
BUNDLE_ID = "org.grouplab.app"

TEN = re.compile(r"^[A-Z0-9]{10}$")
UUID = re.compile(r"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$")


def decoded(value: str) -> bytes | None:
    """The bytes of a base64 secret, or None where it is not base64."""
    try:
        return base64.b64decode("".join(value.split()), validate=True)
    except (binascii.Error, ValueError):
        return None


def problem(name: str, value: str, env: dict[str, str]) -> str | None:
    """What is wrong with one present secret, in words that never contain it; None where its shape is right."""
    value = value.strip()
    if name in ("APPLE_TEAM_ID", "APPLE_API_KEY_ID"):
        return None if TEN.match(value) else "is not ten capital letters and digits"
    if name == "APPLE_API_ISSUER_ID":
        return None if UUID.match(value) else "is not a UUID such as the one App Store Connect shows above its keys"
    if name == "IOS_DIST_CERT_PASSWORD":
        return None if value else "is empty"
    if name == "IOS_DIST_CERT_P12":
        data = decoded(value)
        if data is None:
            return "is not base64; encode the .p12 file with base64 before setting it"
        # A PKCS#12 file is a DER SEQUENCE: its first byte is 0x30.
        return None if len(data) > 100 and data[0] == 0x30 else "decodes, but not to a .p12 certificate file"
    if name == "IOS_PROFILE":
        data = decoded(value)
        if data is None:
            return "is not base64; encode the .mobileprovision file with base64 before setting it"
        if b"<plist" not in data or b"<key>TeamIdentifier</key>" not in data:
            return "decodes, but not to a provisioning profile"
        team = env.get("APPLE_TEAM_ID", "").strip()
        if team and f"<string>{team}</string>".encode() not in data:
            return "is for another team than APPLE_TEAM_ID"
        if f".{BUNDLE_ID}</string>".encode() not in data:
            return f"is not for the bundle {BUNDLE_ID}"
        return None
    if name == "APPLE_API_KEY_P8":
        text = value if "-----BEGIN PRIVATE KEY-----" in value else (decoded(value) or b"").decode("latin-1")
        return None if "-----BEGIN PRIVATE KEY-----" in text and "-----END PRIVATE KEY-----" in text else "is not the .p8 key's text, nor that text in base64"
    return "is not a secret this check knows"


def decide(env: dict[str, str]) -> tuple[str, list[str]]:
    """The decision (sign, unsigned or malformed) and one line per secret."""
    present = [n for n in SECRETS if env.get(n, "").strip()]
    lines = []
    faults = 0
    for name in SECRETS:
        if name not in present:
            lines.append(f"{name}: not set")
            continue
        why = problem(name, env[name], env)
        lines.append(f"{name}: set, " + ("its shape is right" if why is None else "MALFORMED: " + why))
        faults += why is not None
    if not present:
        return "unsigned", lines + ["None of the seven is set, so the build is not signed and nothing is sent to TestFlight (request 55)."]
    if faults:
        return "malformed", lines + [f"{faults} of the secrets that are set are malformed; nothing is signed. Set them again as request 55 says."]
    if len(present) < len(SECRETS):
        missing = ", ".join(n for n in SECRETS if n not in present)
        return "malformed", lines + [f"Only {len(present)} of the seven are set; still missing: {missing}. Nothing is signed until all seven are."]
    return "sign", lines + ["All seven are set and look right; the build is signed and sent to TestFlight."]


def self_test() -> int:
    failed = 0

    def expect(what: str, env: dict[str, str], want: str) -> None:
        nonlocal failed
        got, lines = decide(env)
        secret_values = [v for v in env.values() if len(v) > 3]
        leaked = any(v in line for v in secret_values for line in lines)
        if got != want or leaked:
            failed += 1
            print(f"FAIL {what}: {got}{' (a value was printed)' if leaked else ''}")

    profile = base64.b64encode(b"\x30\x80" + b"<?xml version=\"1.0\"?><plist><dict><key>TeamIdentifier</key><array><string>ABCDE12345</string></array>"
                               b"<key>application-identifier</key><string>ABCDE12345.org.grouplab.app</string></dict></plist>").decode()
    good = {
        "APPLE_TEAM_ID": "ABCDE12345",
        "IOS_DIST_CERT_P12": base64.b64encode(b"\x30" + b"\x82" * 200).decode(),
        "IOS_DIST_CERT_PASSWORD": "not a real password",
        "IOS_PROFILE": profile,
        "APPLE_API_ISSUER_ID": "12345678-90ab-cdef-1234-567890abcdef",
        "APPLE_API_KEY_ID": "KEY1234567",
        "APPLE_API_KEY_P8": "-----BEGIN PRIVATE KEY-----\nnot a real key\n-----END PRIVATE KEY-----",
    }
    expect("none set", {}, "unsigned")
    expect("all set and right", good, "sign")
    expect("the key in base64", {**good, "APPLE_API_KEY_P8": base64.b64encode(good["APPLE_API_KEY_P8"].encode()).decode()}, "sign")
    expect("one missing", {k: v for k, v in good.items() if k != "IOS_PROFILE"}, "malformed")
    expect("a team id too short", {**good, "APPLE_TEAM_ID": "ABC"}, "malformed")
    expect("a certificate not base64", {**good, "IOS_DIST_CERT_P12": "this is not base64 at all!"}, "malformed")
    expect("a profile for another team", {**good, "APPLE_TEAM_ID": "ZZZZZ99999"}, "malformed")
    other_app = base64.b64encode(base64.b64decode(profile).replace(b"org.grouplab.app", b"org.example.app")).decode()
    expect("a profile for another app", {**good, "IOS_PROFILE": other_app}, "malformed")
    expect("an issuer that is not a UUID", {**good, "APPLE_API_ISSUER_ID": "issuer"}, "malformed")
    expect("a key that is not a key", {**good, "APPLE_API_KEY_P8": "just some words here"}, "malformed")
    print("ios-signing self-test: " + ("passed" if not failed else f"{failed} failed"))
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

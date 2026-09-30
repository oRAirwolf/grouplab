#!/usr/bin/env python3
"""Whether the nightly can sign the iOS build and send it to TestFlight, and why not, without ever printing a secret.

NOTES-FROM-PLANNING.md entry 290 section 2 item 7 and docs/IOS-PLAN.md section 3. The nightly's iOS job reads eight repository secrets that
Alan sets himself (request 55). Until all eight are there, the job builds without signing and uploads nothing, so a nightly never fails for
want of them. Once any of them is there, each is checked for its shape before anything uses it, and a malformed one fails loudly, saying
which secret and what is wrong with it, never its value.

Entry 292 section 2.3 added the share extension, org.grouplab.app.share, which is signed with its own App Store profile (IOS_SHARE_PROFILE),
and both profiles must carry the app group group.org.grouplab.app, through which the extension hands shared pictures to the application.

    python3 scripts/ios-signing.py --check        prints one line per secret and the decision; exit 0 sign, 3 unsigned, 1 malformed
    python3 scripts/ios-signing.py --check-dev    GroupLab Dev's two profiles (entry 315, request 61), as sign-dev; exit 0, 3 or 1 as above,
                                                  and the nightly never fails for this one
    python3 scripts/ios-signing.py --self-test    checks the checker against made-up values, none of them real
    python3 scripts/ios-signing.py --properties <identity> <application profile UUID> <extension profile UUID>
                                                  the MSBuild properties the signed publish is given, one per line, so the nightly and
                                                  its dry run build them the same way

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
    "IOS_SHARE_PROFILE",
    "APPLE_API_ISSUER_ID",
    "APPLE_API_KEY_ID",
    "APPLE_API_KEY_P8",
]

# The bundle each profile must be for: docs/IOS-PLAN.md section 2, and the share extension of entry 292 section 2.3.
BUNDLE_ID = "org.grouplab.app"
PROFILE_BUNDLES = {"IOS_PROFILE": BUNDLE_ID, "IOS_SHARE_PROFILE": BUNDLE_ID + ".share"}

# The app group both profiles must allow: ios/Shared/Handoff.cs and each project's Entitlements.plist.
APP_GROUP = "group.org.grouplab.app"

# Entry 315's amendment: GroupLab Dev, org.grouplab.app.dev, signed with the same certificate and key and two App Store profiles of its own
# (request 61), for TestFlight's internal group only. Until both are set it is built unsigned and sent nowhere; the public application's
# signing never depends on them.
DEV_SECRETS = ["IOS_DEV_PROFILE", "IOS_DEV_SHARE_PROFILE"]
DEV_BUNDLE_ID = "org.grouplab.app.dev"
PROFILE_BUNDLES.update({"IOS_DEV_PROFILE": DEV_BUNDLE_ID, "IOS_DEV_SHARE_PROFILE": DEV_BUNDLE_ID + ".share"})
DEV_APP_GROUP = "group.org.grouplab.app.dev"
GROUPS = {"IOS_PROFILE": APP_GROUP, "IOS_SHARE_PROFILE": APP_GROUP, "IOS_DEV_PROFILE": DEV_APP_GROUP, "IOS_DEV_SHARE_PROFILE": DEV_APP_GROUP}

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
    if name in PROFILE_BUNDLES:
        bundle = PROFILE_BUNDLES[name]
        data = decoded(value)
        if data is None:
            return "is not base64; encode the .mobileprovision file with base64 before setting it"
        if b"<plist" not in data or b"<key>TeamIdentifier</key>" not in data:
            return "decodes, but not to a provisioning profile"
        team = env.get("APPLE_TEAM_ID", "").strip()
        if team and f"<string>{team}</string>".encode() not in data:
            return "is for another team than APPLE_TEAM_ID"
        # The application identifier, the team then the bundle; the app group's own name ends in the application's bundle, so a plain
        # search for the bundle would take the extension's profile for the application's.
        if not re.search(rb"<key>application-identifier</key>\s*<string>[A-Z0-9]{10}\." + re.escape(bundle.encode()) + rb"</string>", data):
            return f"is not for the bundle {bundle}"
        group = GROUPS[name]
        if b"<key>com.apple.security.application-groups</key>" not in data or f"<string>{group}</string>".encode() not in data:
            return f"does not allow the app group {group}; turn App Groups on for {bundle}, tick the group, and download the profile again"
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
        return "unsigned", lines + ["None of the eight is set, so the build is not signed and nothing is sent to TestFlight (request 55)."]
    if faults:
        return "malformed", lines + [f"{faults} of the secrets that are set are malformed; nothing is signed. Set them again as request 55 says."]
    if len(present) < len(SECRETS):
        missing = ", ".join(n for n in SECRETS if n not in present)
        return "malformed", lines + [f"Only {len(present)} of the eight are set; still missing: {missing}. Nothing is signed until all eight are."]
    return "sign", lines + ["All eight are set and look right; the build is signed and sent to TestFlight."]


def decide_dev(env: dict[str, str]) -> tuple[str, list[str]]:
    """
    GroupLab Dev's decision: signed only where the public application's eight are right and both of its own profiles are set and right;
    unsigned where neither is set; malformed where one is missing or wrong, which the nightly reports without failing anything else.
    """
    public, _ = decide(env)
    present = [n for n in DEV_SECRETS if env.get(n, "").strip()]
    lines = []
    faults = 0
    for name in DEV_SECRETS:
        if name not in present:
            lines.append(f"{name}: not set")
            continue
        why = problem(name, env[name], env)
        lines.append(f"{name}: set, " + ("its shape is right" if why is None else "MALFORMED: " + why))
        faults += why is not None
    if not present:
        return "unsigned", lines + ["Neither of GroupLab Dev's profiles is set, so GroupLab Dev is built unsigned and sent nowhere (request 61)."]
    if faults or len(present) < len(DEV_SECRETS):
        return "malformed", lines + ["GroupLab Dev's profiles are not both set and right, so GroupLab Dev is not signed. Set them again as request 61 says."]
    if public != "sign":
        return "unsigned", lines + ["GroupLab Dev's profiles are right, but the public application's eight secrets are not, so nothing is signed."]
    return "sign", lines + ["GroupLab Dev's profiles are right; GroupLab Dev is signed and sent to TestFlight's internal group."]


def properties(identity: str, app_profile: str, share_profile: str) -> list[str]:
    """
    The signing properties for the publish. The profile is given to each project by its own name, because a property set on the command
    line reaches every project the build touches, and one CodesignProvision would sign the share extension with the application's profile;
    ios/GroupLab.iOS and ios/GroupLab.Share each turn their own into CodesignProvision. The identity is the same distribution certificate.
    """
    for what, uuid in (("application", app_profile), ("extension", share_profile)):
        if not UUID.match(uuid):
            raise ValueError(f"the {what} profile's UUID is not a UUID")
    if app_profile.lower() == share_profile.lower():
        raise ValueError("the application and the extension were given the same profile")
    return [f"-p:CodesignKey={identity}", f"-p:GroupLabAppProvision={app_profile}", f"-p:GroupLabShareProvision={share_profile}"]


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

    def made_up(bundle: str, group: bool = True) -> str:
        groups = (b"<key>com.apple.security.application-groups</key><array><string>group.org.grouplab.app</string></array>" if group else b"")
        return base64.b64encode(b"\x30\x80" + b"<?xml version=\"1.0\"?><plist><dict><key>TeamIdentifier</key><array><string>ABCDE12345</string></array>"
                                b"<key>Entitlements</key><dict><key>application-identifier</key><string>ABCDE12345." + bundle.encode()
                                + b"</string>" + groups + b"</dict></dict></plist>").decode()

    profile = made_up("org.grouplab.app")
    share = made_up("org.grouplab.app.share")
    good = {
        "APPLE_TEAM_ID": "ABCDE12345",
        "IOS_DIST_CERT_P12": base64.b64encode(b"\x30" + b"\x82" * 200).decode(),
        "IOS_DIST_CERT_PASSWORD": "not a real password",
        "IOS_PROFILE": profile,
        "IOS_SHARE_PROFILE": share,
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
    expect("the extension missing", {k: v for k, v in good.items() if k != "IOS_SHARE_PROFILE"}, "malformed")
    expect("the application's profile given for the extension", {**good, "IOS_SHARE_PROFILE": profile}, "malformed")
    expect("the extension's profile given for the application", {**good, "IOS_PROFILE": share}, "malformed")
    expect("an application profile without the app group", {**good, "IOS_PROFILE": made_up("org.grouplab.app", group=False)}, "malformed")
    expect("an extension profile without the app group", {**good, "IOS_SHARE_PROFILE": made_up("org.grouplab.app.share", group=False)}, "malformed")

    app_uuid, share_uuid = "11111111-2222-3333-4444-555555555555", "66666666-7777-8888-9999-aaaaaaaaaaaa"
    built = properties("Apple Distribution: Made Up (ABCDE12345)", app_uuid, share_uuid)
    if built != ["-p:CodesignKey=Apple Distribution: Made Up (ABCDE12345)", f"-p:GroupLabAppProvision={app_uuid}", f"-p:GroupLabShareProvision={share_uuid}"]:
        failed += 1
        print("FAIL the publish properties: " + " ".join(built))
    if any("CodesignProvision=" in p and "GroupLab" not in p for p in built):
        failed += 1
        print("FAIL one CodesignProvision would reach the extension too")
    for bad in ((app_uuid, app_uuid), ("not-a-uuid", share_uuid)):
        try:
            properties("x", *bad)
            failed += 1
            print(f"FAIL the publish properties took {bad}")
        except ValueError:
            pass
    expect("an issuer that is not a UUID", {**good, "APPLE_API_ISSUER_ID": "issuer"}, "malformed")
    expect("a key that is not a key", {**good, "APPLE_API_KEY_P8": "just some words here"}, "malformed")

    # Entry 315's amendment: GroupLab Dev's two profiles, which never change the public application's decision.
    def made_up_dev(bundle: str) -> str:
        return base64.b64encode(base64.b64decode(made_up(bundle)).replace(b"group.org.grouplab.app<", b"group.org.grouplab.app.dev<")).decode()

    dev = {"IOS_DEV_PROFILE": made_up_dev("org.grouplab.app.dev"), "IOS_DEV_SHARE_PROFILE": made_up_dev("org.grouplab.app.dev.share")}

    def expect_dev(what: str, env: dict[str, str], want: str) -> None:
        nonlocal failed
        got, lines = decide_dev(env)
        leaked = any(v in line for v in env.values() if len(v) > 3 for line in lines)
        if got != want or leaked:
            failed += 1
            print(f"FAIL GroupLab Dev, {what}: {got}{' (a value was printed)' if leaked else ''}")

    expect_dev("none set", good, "unsigned")
    expect_dev("both right", {**good, **dev}, "sign")
    expect_dev("one missing", {**good, "IOS_DEV_PROFILE": dev["IOS_DEV_PROFILE"]}, "malformed")
    expect_dev("the public profile given for Dev", {**good, **dev, "IOS_DEV_PROFILE": profile}, "malformed")
    expect_dev("Dev's profile without Dev's app group", {**good, **dev, "IOS_DEV_PROFILE": made_up("org.grouplab.app.dev")}, "malformed")
    expect_dev("the extension's profile given for the application", {**good, **dev, "IOS_DEV_PROFILE": dev["IOS_DEV_SHARE_PROFILE"]}, "malformed")
    expect_dev("right, but the public secrets missing", dev, "unsigned")
    expect("Dev's profiles leave the public decision alone", {**good, **dev}, "sign")
    expect("a Dev profile given for the public application", {**good, "IOS_PROFILE": dev["IOS_DEV_PROFILE"]}, "malformed")
    print("ios-signing self-test: " + ("passed" if not failed else f"{failed} failed"))
    return 1 if failed else 0


def main(argv: list[str]) -> int:
    if argv[1:2] == ["--self-test"]:
        return self_test()
    if argv[1:2] == ["--properties"] and len(argv) == 5:
        try:
            print("\n".join(properties(argv[2], argv[3], argv[4])))
        except ValueError as e:
            print(f"ios-signing: {e}", file=sys.stderr)
            return 1
        return 0
    if argv[1:2] == ["--check-dev"]:
        decision, lines = decide_dev(dict(os.environ))
        for line in lines:
            print(line)
        if out := os.environ.get("GITHUB_OUTPUT"):
            with open(out, "a", encoding="utf-8", newline="\n") as f:
                f.write(f"sign-dev={'true' if decision == 'sign' else 'false'}\n")
        return {"sign": 0, "unsigned": 3, "malformed": 1}[decision]
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

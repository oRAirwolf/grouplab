"""The weekly look at the server's Oracle boot volume backups, prepared and off until Alan adds a read-only key (NOTES-FROM-PLANNING.md entry
462 section 5, request 106).

Request 46 asks Alan to look in the Oracle console for a recent Full backup. This lists the boot volume backups with a key that can only
read them (Oracle's policy "read boot-volume-backups"), and says what is wrong in plain words: no Full backup in the last eight days, or no
backup that will still be kept a week from now. It changes nothing and cannot delete anything; the key cannot either.

    python3 scripts/oracle-backups.py            reads OCI_USER, OCI_TENANCY, OCI_FINGERPRINT, OCI_REGION, OCI_KEY (and optionally
                                                 OCI_COMPARTMENT, the tenancy by default) from the environment
    python3 scripts/oracle-backups.py --self-test

Exit 0 when all is well or the key is not there yet (it says which), 1 when something is wrong, 2 when Oracle could not be asked.
"""

from __future__ import annotations

import os
import sys
import tempfile
from datetime import datetime, timedelta, timezone

NEEDED = ["OCI_USER", "OCI_TENANCY", "OCI_FINGERPRINT", "OCI_REGION", "OCI_KEY"]


def judge(backups: list[dict], now: datetime) -> list[str]:
    """What is wrong with these backups, each {type, created, expires} with datetimes (expires may be None, kept until deleted)."""
    wrong = []
    fulls = [b for b in backups if b["type"].upper() == "FULL"]
    newest = max((b["created"] for b in fulls), default=None)
    if newest is None:
        wrong.append("There is no Full boot volume backup at all.")
    elif now - newest > timedelta(days=8):
        wrong.append(f"The newest Full backup is from {newest:%Y-%m-%d}, more than eight days ago; the weekly schedule should have made a newer one.")
    if backups and not any(b["expires"] is None or b["expires"] - now > timedelta(days=7) for b in backups):
        wrong.append("Every backup expires within a week, so none will outlast the next one if the schedule has stopped.")
    if not backups:
        wrong.append("Oracle lists no boot volume backups at all.")
    return wrong


def listed() -> list[dict]:
    import oci  # installed by the workflow only when the key is there

    with tempfile.NamedTemporaryFile("w", suffix=".pem", delete=False) as key:
        key.write(os.environ["OCI_KEY"])
    try:
        config = {"user": os.environ["OCI_USER"], "tenancy": os.environ["OCI_TENANCY"], "fingerprint": os.environ["OCI_FINGERPRINT"],
                  "region": os.environ["OCI_REGION"], "key_file": key.name}
        client = oci.core.BlockstorageClient(config)
        compartment = os.environ.get("OCI_COMPARTMENT") or os.environ["OCI_TENANCY"]
        items = oci.pagination.list_call_get_all_results(client.list_boot_volume_backups, compartment).data
    finally:
        os.unlink(key.name)
    return [{"type": b.type, "created": b.time_created, "expires": b.expiration_time} for b in items if b.lifecycle_state == "AVAILABLE"]


def main() -> int:
    missing = [n for n in NEEDED if not os.environ.get(n)]
    if missing:
        print("Oracle backups: not checked; the read-only key is not set up yet (request 106 in docs/notes/for-alan.md).")
        return 0
    try:
        backups = listed()
    except Exception as error:  # the SDK raises its own kinds; the message is Oracle's and carries no secret
        print(f"Oracle backups: Oracle could not be asked ({type(error).__name__}).")
        return 2
    now = datetime.now(timezone.utc)
    wrong = judge(backups, now)
    fulls = sorted((b["created"] for b in backups if b["type"].upper() == "FULL"), reverse=True)
    print(f"Oracle backups: {len(backups)} available, newest Full {fulls[0]:%Y-%m-%d %H:%M} UTC." if fulls else f"Oracle backups: {len(backups)} available.")
    for line in wrong:
        print("- " + line)
    return 1 if wrong else 0


def self_test() -> int:
    now = datetime(2026, 10, 21, tzinfo=timezone.utc)
    week = timedelta(days=7)
    good = [{"type": "FULL", "created": now - timedelta(days=2), "expires": now + 2 * week}, {"type": "INCREMENTAL", "created": now, "expires": now + week}]
    checks = [
        ("a recent Full kept two weeks is well", judge(good, now) == []),
        ("an old Full is said", any("eight days" in w for w in judge([{"type": "FULL", "created": now - 3 * week, "expires": now + 3 * week}], now))),
        ("no Full is said", any("no Full" in w for w in judge([{"type": "INCREMENTAL", "created": now, "expires": now + 3 * week}], now))),
        ("all expiring soon is said", any("expires within a week" in w for w in judge([{"type": "FULL", "created": now, "expires": now + timedelta(days=3)}], now))),
        ("none at all is said", any("no boot volume backups" in w for w in judge([], now))),
    ]
    for name, ok in checks:
        print(("ok   " if ok else "FAIL ") + name)
    return 0 if all(ok for _, ok in checks) else 1


if __name__ == "__main__":
    sys.exit(self_test() if "--self-test" in sys.argv else main())

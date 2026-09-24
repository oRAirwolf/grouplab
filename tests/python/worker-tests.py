#!/usr/bin/env python3
"""The intake worker, run for real. NOTES-FROM-PLANNING.md entry 176 section 7.

The worker was killed for memory on every run once real submissions arrived, and no test had caught it,
because none had ever run the scanner. This one runs the real worker, with a real ClamAV daemon and the
real clamdscan, under the same systemd memory limit as the server, on the Ubuntu runner, which has the same
distribution packages as the server. It needs root for systemd-run and for clamdscan's configuration file,
so it is for CI:

    sudo apt-get install -y clamav-daemon clamdscan libheif-examples libheif-plugin-libde265 libheif-plugin-x265 python3-pil
    python3 tests/python/worker-tests.py

Three submissions go in: a phone-shaped JPEG stored on its side with EXIF Orientation 6 and a GPS block, a
HEIC file, and a JPEG carrying a test signature the daemon is given. The first two must come out in ready,
rebuilt, upright and with no GPS, each file recorded as scanned; the third must be refused because the
scanner found it.
"""

from __future__ import annotations

import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import time
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
failed = 0


def check(what: str, ok: bool, detail: str = "") -> None:
    global failed
    print(("ok      " if ok else "FAILED  ") + what + ("" if ok or not detail else "\n        " + detail))
    if not ok:
        failed += 1


def main() -> int:
    from PIL import Image

    root = Path(tempfile.mkdtemp(prefix="grouplab-worker-", dir=os.environ.get("RUNNER_TEMP")))
    os.chmod(root, 0o755)
    private = root / "private"
    quarantine = private / "quarantine"
    quarantine.mkdir(parents=True)

    # The worker, pointed at the test tree and nothing else changed.
    source = (REPO / "website/server/grouplab-intake-worker.py").read_text(encoding="utf-8")
    patched = source.replace('PRIVATE = Path("/home/airwolf/web/grouplab.org/private")', f"PRIVATE = Path({str(private)!r})")
    patched = re.sub(r'LOG = Path\("[^"]*"\)', f"LOG = Path({str(root / 'worker.log')!r})", patched)
    check("the worker could be pointed at the test tree", patched != source and str(private) in patched)
    worker = root / "worker.py"
    worker.write_text(patched, encoding="utf-8")

    # A phone photograph as a phone stores it: landscape pixels, a tag saying to turn them a quarter turn, and a location. A red block in
    # the stored top left corner ends up at the top right once it is upright.
    phone = Image.new("RGB", (4000, 3000), (200, 200, 190))
    phone.paste((220, 20, 20), (0, 0, 400, 400))
    exif = Image.Exif()
    exif[0x0112] = 6            # Orientation
    exif[0x010F] = "TestMake"   # Make
    exif[0x0110] = "TestPhone"  # Model
    gps = exif.get_ifd(0x8825)
    gps[1] = "N"
    gps[2] = (40.0, 1.0, 2.0)
    folder = quarantine / "2026-09-24_aaaaaaa1"
    folder.mkdir()
    phone.save(folder / "001_phone.jpg", "JPEG", quality=90, exif=exif)
    (folder / "meta.json").write_text(json.dumps({"consent": {"version": "consent_v1"}}), encoding="utf-8")

    # A HEIC, as an iPhone sends, made by libheif's own encoder from a PNG.
    folder = quarantine / "2026-09-24_aaaaaaa2"
    folder.mkdir()
    Image.new("RGB", (1200, 900), (180, 190, 200)).save(root / "source.png")
    made = subprocess.run(["heif-enc", "-o", str(folder / "001_phone.heic"), str(root / "source.png")], capture_output=True, text=True)
    check("heif-enc made a HEIC file", made.returncode == 0 and (folder / "001_phone.heic").is_file(), made.stderr[-300:])
    (folder / "meta.json").write_text(json.dumps({"consent": {"version": "consent_v1"}}), encoding="utf-8")

    # A file the scanner is told to find: a valid JPEG with a marker after it, and a signature for the marker.
    marker = b"GroupLab-intake-worker-test-marker"
    folder = quarantine / "2026-09-24_aaaaaaa3"
    folder.mkdir()
    Image.new("RGB", (64, 64), (1, 2, 3)).save(folder / "001_marked.jpg", "JPEG")
    with (folder / "001_marked.jpg").open("ab") as f:
        f.write(marker)
    (folder / "meta.json").write_text(json.dumps({"consent": {"version": "consent_v1"}}), encoding="utf-8")

    # Entry 182: a photograph whose rebuilt PNG is over 25 MB, Ubuntu's default StreamMaxLength, so the stream is proved above it.
    import random as _random
    folder = quarantine / "2026-09-24_aaaaaaa4"
    folder.mkdir()
    rng = _random.Random(182)
    noisy = Image.frombytes("RGB", (4000, 3000), bytes(rng.getrandbits(8) for _ in range(4000 * 3000 * 3)))
    noisy.save(folder / "001_large.jpg", "JPEG", quality=90)
    (folder / "meta.json").write_text(json.dumps({"consent": {"version": "consent_v1"}}), encoding="utf-8")

    # clamd as the server runs it: the packaged clamav-daemon service, with its own configuration, socket and AppArmor profile, given the
    # one test signature and nothing else. A clamd started by hand in the test tree was refused its log file by that profile, which is
    # exactly the difference between a test and the server that this test exists to close.
    subprocess.run(["sudo", "systemctl", "stop", "clamav-daemon", "clamav-freshclam"], check=False)
    subprocess.run(["sudo", "sh", "-c", "rm -f /var/lib/clamav/*.cvd /var/lib/clamav/*.cld /var/lib/clamav/*.ndb"], check=False)
    (root / "grouplab-test.ndb").write_text(f"GroupLab.Test.Marker:0:*:{marker.hex()}" + chr(10), encoding="ascii")
    subprocess.run(["sudo", "install", "-m", "0644", str(root / "grouplab-test.ndb"), "/var/lib/clamav/grouplab-test.ndb"], check=True)
    # Ubuntu's unit starts only where the real daily database exists; the runner has only the test signature, so that one condition is
    # cleared for the runner. The server has the real databases and is not touched by this.
    subprocess.run(["sudo", "mkdir", "-p", "/etc/systemd/system/clamav-daemon.service.d"], check=True)
    subprocess.run(["sudo", "tee", "/etc/systemd/system/clamav-daemon.service.d/grouplab-test.conf"], input="[Unit]\nConditionPathExistsGlob=\n", text=True, capture_output=True, check=True)
    subprocess.run(["sudo", "systemctl", "daemon-reload"], check=True)

    # Entry 182: the limits the worker needs, set the way Alan sets them on the server, one line each.
    for key, value in (("StreamMaxLength", "400M"), ("MaxFileSize", "400M"), ("MaxScanSize", "400M"), ("AlertExceedsMax", "yes")):
        subprocess.run(["sudo", "sh", "-c", f"grep -q '^{key} ' /etc/clamav/clamd.conf && sed -i 's/^{key} .*/{key} {value}/' /etc/clamav/clamd.conf || echo '{key} {value}' >> /etc/clamav/clamd.conf"], check=True)
    subprocess.run(["sudo", "systemctl", "restart", "clamav-daemon"], check=False)
    answering = False
    for _ in range(90):
        if subprocess.run(["clamdscan", "--ping=1"], capture_output=True).returncode == 0:
            answering = True
            break
        time.sleep(2)
    status = subprocess.run(["sudo", "journalctl", "-u", "clamav-daemon", "-n", "15", "--no-pager"], capture_output=True, text=True).stdout
    check("the packaged clamd answers with the test signature", answering, status[-600:])

    # The worker, as the server runs it: its own memory limit, Unix sockets only, no network.
    run = subprocess.run([
        "sudo", "systemd-run", "--wait", "--pipe", "--collect", "--quiet",
        f"--uid={os.getuid()}", f"--gid={os.getgid()}",
        "-p", "MemoryMax=1600M", "-p", "RestrictAddressFamilies=AF_UNIX", "-p", "PrivateNetwork=yes",
        # Entry 182: the unit's own mount sandbox, which is what made clamd refuse a passed descriptor as a disconnected path. Without it
        # this test could not have seen that failure, and cannot now prove that streaming avoids it.
        "-p", "ProtectSystem=strict", "-p", "ProtectHome=read-only", "-p", f"ReadWritePaths={root}", "-p", "PrivateTmp=yes",
        "-p", "NoNewPrivileges=yes",
        "/usr/bin/python3", str(worker),
    ], capture_output=True, text=True, timeout=900)
    log = (root / "worker.log").read_text(encoding="utf-8", errors="replace") if (root / "worker.log").exists() else ""
    check("the worker ran to the end under MemoryMax=1600M", run.returncode == 0, run.stderr[-500:] + "\n" + log[-800:])

    ready = private / "ready"
    refused = private / "refused"

    phone_dir = ready / "2026-09-24_aaaaaaa1"
    check("the phone photograph is in ready", phone_dir.is_dir(), log[-800:])
    if phone_dir.is_dir():
        meta = json.loads((phone_dir / "meta.json").read_text(encoding="utf-8"))
        stored = meta["files"][0]["stored"]
        with Image.open(phone_dir / stored) as rebuilt:
            check("it is rebuilt upright, taller than wide", rebuilt.size == (3000, 4000), str(rebuilt.size))
            check("the red corner is at the top right once upright", rebuilt.getpixel((2990, 10))[0] > 200 and rebuilt.getpixel((10, 10))[0] < 210, str(rebuilt.getpixel((2990, 10))))
            tags = rebuilt.getexif()
            check("no GPS survives", 0x8825 not in tags and not tags.get_ifd(0x8825), str(dict(tags)))
            check("the camera's make survives", tags.get(0x010F) == "TestMake", str(dict(tags)))
            # Entry 177 section 2: upright pixels say they are upright, so nothing turns them a second time.
            check("the orientation tag says upright", tags.get(0x0112, 1) == 1, str(tags.get(0x0112)))
        check("the file is recorded as scanned", str(meta["files"][0].get("scan", "")).startswith("clean"), str(meta["files"][0]))
        check("and nothing on the submission went unscanned", meta.get("notScanned") == 0)
        check("the original bytes are gone", not (phone_dir / "001_phone.jpg").exists())

    heic_dir = ready / "2026-09-24_aaaaaaa2"
    check("the HEIC photograph is in ready", heic_dir.is_dir(), log[-800:])
    if heic_dir.is_dir():
        meta = json.loads((heic_dir / "meta.json").read_text(encoding="utf-8"))
        check("recorded as HEIC", meta["files"][0].get("heic") is True, str(meta["files"][0]))
        with Image.open(heic_dir / meta["files"][0]["stored"]) as rebuilt:
            check("rebuilt at its own size", rebuilt.size == (1200, 900), str(rebuilt.size))

    large_dir = ready / "2026-09-24_aaaaaaa4"
    check("the large photograph is in ready", large_dir.is_dir(), log[-800:])
    if large_dir.is_dir():
        meta = json.loads((large_dir / "meta.json").read_text(encoding="utf-8"))
        size = (large_dir / meta["files"][0]["stored"]).stat().st_size
        check("its rebuilt file is over 25 MB", size > 25 * 1024 * 1024, str(size))
        check("and was scanned whole, as a stream", str(meta["files"][0].get("scan", "")).startswith("clean"), str(meta["files"][0]))

    marked_dir = refused / "2026-09-24_aaaaaaa3"
    check("the file the scanner found is refused", marked_dir.is_dir(), log[-800:])
    if marked_dir.is_dir():
        check("with the reason beside it", "found" in (marked_dir / "refused.txt").read_text(encoding="utf-8"))

    check("nothing is left in quarantine", not any(quarantine.iterdir()))

    # Entry 177 section 1.3: the pull script's own check, run on meta.json exactly as the worker wrote it. The two were written apart and
    # the pull script crashed on the first real submissions because they disagreed about one key.
    for folder in sorted(ready.iterdir()) if ready.is_dir() else []:
        script = f". '{REPO / 'scripts' / 'SubmissionCheck.ps1'}'; Test-SubmissionFolder -Folder '{folder}' | ConvertTo-Json -Compress"
        out = subprocess.run(["pwsh", "-NoProfile", "-NonInteractive", "-Command", script], capture_output=True, text=True)
        verdict = json.loads(out.stdout) if out.returncode == 0 and out.stdout.strip() else None
        check(f"the pull script's check passes {folder.name}", verdict is not None and not verdict["Bad"] and verdict["Checked"] >= 1, (out.stdout + out.stderr)[-400:])
    shutil.rmtree(root, ignore_errors=True)
    print("all checks passed" if failed == 0 else f"{failed} failed")
    return 0 if failed == 0 else 1


if __name__ == "__main__":
    sys.exit(main())

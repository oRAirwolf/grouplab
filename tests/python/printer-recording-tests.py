#!/usr/bin/env python3
"""scripts/printer-recording.py on recordings made here, NOTES-FROM-PLANNING.md entry 363 section 2b.

A Bluetooth LE session as Phomemo's app would make it (the services discovered, a print job of a small raster written in chunks across ACL
fragments, the printer's answers) is read back to the service, the write characteristic, the chunk size, every command and the raster
itself, which matches the page it came from. The same session inside a bug report's text summary is found and said to be cut short, and a
classic Bluetooth session is named as classic, so the iPhone is ruled out in plain words.

    python3 tests/python/printer-recording-tests.py
"""

from __future__ import annotations

import base64
import importlib.util
import io
import struct
import sys
import tempfile
import zipfile
import zlib
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("recording", REPO / "scripts" / "printer-recording.py")
recording = importlib.util.module_from_spec(spec)
sys.modules["recording"] = recording
spec.loader.exec_module(recording)

FAILED = []


def check(ok: bool, what: str) -> None:
    print(("ok   " if ok else "FAIL ") + what)
    if not ok:
        FAILED.append(what)


def uuid16(v: int) -> bytes:
    return struct.pack("<H", v)


def acl(handle: int, l2cap: bytes, size: int = 27) -> list[bytes]:
    """An L2CAP frame cut into ACL packets of at most `size` bytes, the first marked as a start and the rest as continuations."""
    out = []
    for i in range(0, len(l2cap), size):
        flags = (0b00 if i == 0 else 0b01) << 12
        piece = l2cap[i:i + size]
        out.append(b"\x02" + struct.pack("<HH", handle | flags, len(piece)) + piece)
    return out


def att(op: int, body: bytes) -> bytes:
    payload = bytes([op]) + body
    return struct.pack("<HH", len(payload), 0x0004) + payload


def btsnoop(packets: list[tuple[bytes, bool]]) -> bytes:
    out = bytearray(b"btsnoop\0" + struct.pack(">II", 1, 1002))
    for i, (data, received) in enumerate(packets):
        out += struct.pack(">IIIIq", len(data), len(data), 1 if received else 0, 0, 0x00DCDDB30F2F8000 + i * 20000) + data
    return bytes(out)


# A 16 by 4 dot raster: a diagonal, so a raster read the wrong way round would not match.
WIDTH_BYTES, HEIGHT = 2, 4
RASTER = bytes([0x80, 0x00, 0x40, 0x00, 0x20, 0x00, 0x10, 0x01])
JOB = (b"\x1b\x4e\x0d\x03" + b"\x1b\x4e\x04\x0a" + b"\x1f\x11\x0a" + b"\x1d\x76\x30\x00" + struct.pack("<HH", WIDTH_BYTES, HEIGHT) + RASTER
       + b"\x1f\xf0\x05\x00\x1f\xf0\x03\x00")


def le_session() -> list[tuple[bytes, bool]]:
    h = 0x0040
    packets: list[tuple[bytes, bool]] = []
    # Primary services: ff00 at 0x0010-0x0020.
    for p in acl(h, att(0x11, bytes([6]) + struct.pack("<HH", 0x0010, 0x0020) + uuid16(0xFF00))):
        packets.append((p, True))
    # Characteristics: ff02 (write without response) at value handle 0x0012, ff01 (notify) at 0x0015.
    decl = struct.pack("<HBH", 0x0011, 0x04, 0x0012) + uuid16(0xFF02) + struct.pack("<HBH", 0x0014, 0x10, 0x0015) + uuid16(0xFF01)
    for p in acl(h, att(0x09, bytes([7]) + decl)):
        packets.append((p, True))
    # The job in 10-byte writes without response, each write's frame cut across ACL packets of 9 bytes.
    for i in range(0, len(JOB), 10):
        for p in acl(h, att(0x52, struct.pack("<H", 0x0012) + JOB[i:i + 10]), size=9):
            packets.append((p, False))
        packets.append((acl(h, att(0x1B, struct.pack("<H", 0x0015) + b"\x01\x01"))[0], True))
    return packets


def run(source: Path, page: Path | None = None) -> dict:
    out = Path(tempfile.mkdtemp(prefix="printer-recording-"))
    saved = sys.stdout
    sys.stdout = io.StringIO()
    try:
        return recording.analyse(source, None, page, out) | {"out": str(out)}
    finally:
        sys.stdout = saved


def main() -> int:
    folder = Path(tempfile.mkdtemp(prefix="printer-recording-tests-"))
    log = folder / "btsnoop_hci.log"
    log.write_bytes(btsnoop(le_session()))
    page = folder / "page.png"
    page.write_bytes(recording.png(recording.Raster(WIDTH_BYTES, HEIGHT, RASTER)))

    report = run(log, page)
    check(report["bluetooth"] == "LE", "an LE session is called LE")
    check(report["service"] == "0000ff00-0000-1000-8000-00805f9b34fb", "the service written in is ff00")
    check(report["write"] == "0000ff02-0000-1000-8000-00805f9b34fb" and report["writeHandle"] == 0x12, "the write characteristic is ff02")
    check(report["withoutResponse"] and report["chunk"] == 10 and report["bytes"] == len(JOB), "the chunks and the bytes are counted")
    check(abs(report["pauseMs"] - 60) < 1e-6, "the pause between writes is measured (three packets 20 ms apart a write)")
    check(any("speed 3" in c for c in report["commands"]) and any("density 10" in c for c in report["commands"]), "speed and density are read")
    check(any("raster 16 dots across" in c and "by 4 lines" in c for c in report["commands"]), "the raster's size is read")
    check(report["rasters"] and "100.00% of the dots agree" in report["rasters"][0]["againstPage"], "the raster matches the page it came from")
    check(report["answers"] and report["answers"][0]["bytes"] == "01 01", "the printer's answers are listed")

    # The same session inside a bug report's text summary, every ACL packet cut to its first 12 bytes as a summary keeps them.
    records, last = bytearray(), 0
    for data, received in le_session():
        kind = {(2, True): 0x11, (2, False): 0x21}[(data[0], received)]
        body = data[1:13]
        records += struct.pack("<HHIb", len(body) + 1, len(data), 20, kind) + body
    blob = struct.pack("<bQ", 2, 1_700_000_000_000) + zlib.compress(bytes(records))
    text = b"== dumpstate\n--- BEGIN:BTSNOOP_LOG_SUMMARY (12345 bytes in) ---\n" + base64.b64encode(blob) + b"\n--- END:BTSNOOP_LOG_SUMMARY ---\n"
    report_zip = folder / "bugreport-test.zip"
    with zipfile.ZipFile(report_zip, "w") as z:
        z.writestr("bugreport-device-2026-10-04.txt", text)
    summary = run(report_zip)
    check(summary["truncatedFrames"] > 0, "a summary's cut packets are said to be cut")
    check("summary" in summary["read"], "the summary in a report's text is found")

    # A classic session: an L2CAP connection to PSM 3, then RFCOMM UIH frames with the job.
    h, classic = 0x0001, []
    signal = struct.pack("<BBH", 0x02, 1, 4) + struct.pack("<HH", 3, 0x0041)
    classic.append((b"\x02" + struct.pack("<HH", h, 4 + len(signal)) + struct.pack("<HH", len(signal), 1) + signal, False))
    answer = struct.pack("<BBH", 0x03, 1, 8) + struct.pack("<HHHH", 0x0050, 0x0041, 0, 0)
    classic.append((b"\x02" + struct.pack("<HH", h, 4 + len(answer)) + struct.pack("<HH", len(answer), 1) + answer, True))
    for i in range(0, len(JOB), 12):
        data = JOB[i:i + 12]
        frame = bytes([(2 << 2) | 3, 0xEF, (len(data) << 1) | 1]) + data + b"\x00"
        classic.append((b"\x02" + struct.pack("<HH", h, 4 + len(frame)) + struct.pack("<HH", len(frame), 0x0050) + frame, False))
    classic_log = folder / "classic.log"
    classic_log.write_bytes(btsnoop(classic))
    report = run(classic_log)
    check(report["bluetooth"] == "classic", "a classic session is called classic")
    check(any("raster 16 dots across" in c for c in report["commands"]), "the classic job's raster is read")

    # Request 73: the M834's raster comes as LZO1X blocks; the app's first block is 4096 bytes of paper in 44.
    white = bytes.fromhex("02000000000020000000000000000000000000000000da10000c000000000000000000000000000000110000")
    check(recording.lzo1x(white) == bytes(4096), "the M834 app's white block unpacks to 4096 bytes of paper")
    m834 = bytes([0x1B, 0x40, 0x1D, 0x76, 0x30, 0x00]) + struct.pack("<HH", 64, 128) + (len(white).to_bytes(3, "little") + white) * 2
    commands, rasters = recording.lay_out(m834)
    check(any("sent as 2 LZO1X blocks" in c for c in commands) and rasters and rasters[0].data == bytes(8192), "an LZO raster is unpacked")

    print(f"{len(FAILED)} failed" if FAILED else "all passed")
    return 1 if FAILED else 0


if __name__ == "__main__":
    sys.exit(main())

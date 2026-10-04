#!/usr/bin/env python3
"""What a label printer's own app sent it, from an Android Bluetooth recording. NOTES-FROM-PLANNING.md entry 363 section 2b.

Request 73 asks Alan for two files: an Android bug report taken with the Bluetooth HCI snoop log on while Phomemo's app printed one page,
and nRF Connect's log of the printer's services. This reads both and says, in plain words and in a JSON file beside them:

- whether the printer spoke Bluetooth LE (GATT) or classic Bluetooth (RFCOMM), and if classic, that the iPhone cannot be its path;
- its services and characteristics as discovered, and which characteristic the app wrote to and how (chunk size, pause between writes,
  write with or without response), and what the printer answered on its notifications;
- the bytes the app sent, laid out as commands: a Phomemo-family header (speed, density, media), raster blocks with their width and
  height, feeds and the end; or TSPL text; or "not a known command" with the first bytes shown, where it is compressed or new;
- each raster block as a PNG, and, given the page that was printed (--page), how well the raster matches it.

With that, the encoder in src/GroupLab.Core/Printing/Labels follows the same day. Standard library only.

    python scripts/printer-recording.py <bugreport.zip | btsnoop_hci.log> [--nrf <nrf-connect-log>] [--page <page.png>] [--out <folder>]
"""

from __future__ import annotations

import argparse
import base64
import io
import json
import re
import statistics
import struct
import sys
import zipfile
import zlib
from dataclasses import dataclass, field
from pathlib import Path

# ----------------------------------------------------------------------------------------------------------------------------------
# Finding the HCI log


@dataclass
class Packet:
    """One HCI packet: H4 type (1 command, 2 ACL, 3 SCO, 4 event), whether the phone received it, its time in milliseconds, its bytes,
    and whether the recording cut it short (a bug report's summary keeps only the start of each ACL packet)."""

    kind: int
    received: bool
    ms: float
    data: bytes
    truncated: bool = False


def read_btsnoop(raw: bytes) -> list[Packet]:
    if not raw.startswith(b"btsnoop\0"):
        raise ValueError("not a btsnoop file")
    _, datalink = struct.unpack_from(">II", raw, 8)
    packets, at = [], 16
    while at + 24 <= len(raw):
        original, included, flags, _, stamp = struct.unpack_from(">IIIIq", raw, at)
        at += 24
        body = raw[at:at + included]
        at += included
        if datalink == 1002:
            kind, body = body[0], body[1:]
        else:
            kind = 4 if flags & 2 and flags & 1 else 1 if flags & 2 else 2
        packets.append(Packet(kind, bool(flags & 1), stamp / 1000.0, body, included < original))
    return packets


# btsnooz, the summary a bug report carries in its text (AOSP's btsnooz.py): version, last timestamp, then zlib records.
SNOOZ_TYPES = {0x10: (4, True), 0x11: (2, True), 0x12: (3, True), 0x17: (5, True), 0x20: (1, False), 0x21: (2, False), 0x22: (3, False), 0x2D: (5, False)}


def read_btsnooz(blob: bytes) -> list[Packet]:
    version, last_ms = struct.unpack_from("<bQ", blob)
    if version not in (1, 2):
        raise ValueError(f"btsnooz version {version} is not one this reads")
    data = zlib.decompress(blob[9:])
    head = "<HIb" if version == 1 else "<HHIb"
    size = struct.calcsize(head)
    records, at, total = [], 0, 0
    while at + size <= len(data):
        fields = struct.unpack_from(head, data, at)
        length, delta, kind = (fields[0], fields[1], fields[2]) if version == 1 else (fields[0], fields[2], fields[3])
        whole = fields[0] if version == 1 else fields[1]
        body = data[at + size: at + size + length - 1]
        records.append((delta, kind, body, whole))
        total += delta
        at += size + length - 1
    packets, t = [], last_ms - total
    for delta, kind, body, whole in records:
        t += delta
        h4, received = SNOOZ_TYPES.get(kind, (0, False))
        packets.append(Packet(h4, received, float(t), body, len(body) + 1 < whole))
    return packets


def find_packets(source: Path) -> tuple[list[Packet], str]:
    """The HCI packets in a bug report zip, a folder from one, or a bare btsnoop file; the full log where there is one, else the summary."""
    if source.is_file() and not zipfile.is_zipfile(source):
        return read_btsnoop(source.read_bytes()), source.name
    texts: list[tuple[str, bytes]] = []
    if source.is_dir():
        files = [(str(p.relative_to(source)), p.read_bytes) for p in source.rglob("*") if p.is_file()]
    else:
        archive = zipfile.ZipFile(source)
        files = [(n, (lambda n=n: archive.read(n))) for n in archive.namelist()]
    for name, read in files:
        base = name.replace("\\", "/").split("/")[-1]
        if base.startswith("btsnoop_hci.log") and not base.endswith(".last"):
            return read_btsnoop(read()), name
        if base.endswith(".zip") and "bugreport" in base:
            inner = zipfile.ZipFile(io.BytesIO(read()))
            for n in inner.namelist():
                if n.split("/")[-1].startswith("btsnoop_hci.log"):
                    return read_btsnoop(inner.read(n)), n
        if base.startswith("bugreport") and base.endswith(".txt"):
            texts.append((name, read()))
    for name, text in texts:
        m = re.search(rb"BTSNOOP_LOG_SUMMARY[^\n]*\n(.*?)\n--- END:BTSNOOP_LOG_SUMMARY", text, re.S)
        if m:
            blob = base64.b64decode(b"".join(m.group(1).split()))
            return read_btsnooz(blob), name + " (the summary in the report's text)"
    raise SystemExit("No Bluetooth HCI snoop log is in it. Turn on Developer options, Enable Bluetooth HCI snoop log, print again, then take the bug report.")


# ----------------------------------------------------------------------------------------------------------------------------------
# ACL, L2CAP, ATT and RFCOMM


@dataclass
class Write:
    ms: float
    handle: int
    value: bytes
    command: bool  # write without response
    truncated: bool


@dataclass
class Session:
    services: list[tuple[int, int, str]] = field(default_factory=list)  # start, end, uuid
    characteristics: dict[int, tuple[str, int]] = field(default_factory=dict)  # value handle -> (uuid, properties)
    writes: list[Write] = field(default_factory=list)
    notifications: list[tuple[float, int, bytes]] = field(default_factory=list)
    rfcomm: list[tuple[float, int, bytes, bool]] = field(default_factory=list)  # ms, dlci, data, truncated
    rfcomm_cids: set[int] = field(default_factory=set)
    truncated: int = 0


def uuid_text(raw: bytes) -> str:
    if len(raw) == 2:
        return f"0000{struct.unpack('<H', raw)[0]:04x}-0000-1000-8000-00805f9b34fb"
    h = raw[::-1].hex()
    return f"{h[:8]}-{h[8:12]}-{h[12:16]}-{h[16:20]}-{h[20:]}"


def l2cap_frames(packets: list[Packet]):
    """Whole L2CAP frames from ACL packets, put back together from their fragments: (ms, sent, cid, payload, truncated)."""
    pending: dict[tuple[bool, int], tuple[float, int, int, bytearray, bool]] = {}
    for p in packets:
        if p.kind != 2 or len(p.data) < 4:
            continue
        handle_flags, length = struct.unpack_from("<HH", p.data)
        handle, boundary = handle_flags & 0x0FFF, (handle_flags >> 12) & 3
        body = p.data[4:4 + length]
        key = (not p.received, handle)
        if boundary in (0, 2) and len(body) >= 4:
            need, cid = struct.unpack_from("<HH", body)
            pending[key] = (p.ms, need, cid, bytearray(body[4:]), p.truncated)
        elif boundary == 1 and key in pending:
            ms, need, cid, got, cut = pending[key]
            got.extend(body)
            pending[key] = (ms, need, cid, got, cut or p.truncated)
        else:
            continue
        ms, need, cid, got, cut = pending[key]
        if len(got) >= need or cut:
            yield ms, not p.received, cid, bytes(got[:need]), cut or len(got) < need
            del pending[key]


def read_session(packets: list[Packet]) -> Session:
    s = Session()
    psm_of: dict[int, int] = {}
    for ms, sent, cid, frame, cut in l2cap_frames(packets):
        s.truncated += cut
        if cid == 0x0004 and frame:
            att(s, ms, sent, frame, cut)
        elif cid == 0x0001:
            # Classic signalling: a connection to PSM 3 is RFCOMM; remember both its channel ids.
            at = 0
            while at + 4 <= len(frame):
                code, _, length = struct.unpack_from("<BBH", frame, at)
                body = frame[at + 4: at + 4 + length]
                if code == 0x02 and len(body) >= 4:
                    psm, source = struct.unpack_from("<HH", body)
                    psm_of[source] = psm
                elif code == 0x03 and len(body) >= 4:
                    destination, source = struct.unpack_from("<HH", body)
                    if psm_of.get(source) == 3 or psm_of.get(destination) == 3:
                        s.rfcomm_cids.update({source, destination})
                at += 4 + length
        elif cid in s.rfcomm_cids and sent:
            rfcomm(s, ms, frame, cut)
    return s


def att(s: Session, ms: float, sent: bool, frame: bytes, cut: bool) -> None:
    op = frame[0]
    if op == 0x11 and len(frame) > 2:  # Read By Group Type Response: primary services
        size = frame[1]
        for at in range(2, len(frame) - size + 1, size):
            start, end = struct.unpack_from("<HH", frame, at)
            s.services.append((start, end, uuid_text(frame[at + 4: at + size])))
    elif op == 0x09 and len(frame) > 2:  # Read By Type Response: characteristic declarations
        size = frame[1]
        for at in range(2, len(frame) - size + 1, size):
            if size in (7, 21):
                properties, value = frame[at + 2], struct.unpack_from("<H", frame, at + 3)[0]
                s.characteristics[value] = (uuid_text(frame[at + 5: at + size]), properties)
    elif op in (0x52, 0x12) and sent and len(frame) >= 3:
        s.writes.append(Write(ms, struct.unpack_from("<H", frame, 1)[0], frame[3:], op == 0x52, cut))
    elif op == 0x1B and not sent and len(frame) >= 3:
        s.notifications.append((ms, struct.unpack_from("<H", frame, 1)[0], frame[3:]))


def rfcomm(s: Session, ms: float, frame: bytes, cut: bool) -> None:
    if len(frame) < 4:
        return
    dlci, control = frame[0] >> 2, frame[1]
    if dlci == 0 or (control & 0xEF) != 0xEF:  # only UIH frames carry data, and DLCI 0 is control
        return
    at = 2
    if frame[at] & 1:
        length, at = frame[at] >> 1, at + 1
    else:
        length, at = (frame[at] >> 1) | (frame[at + 1] << 7), at + 2
    if control & 0x10:  # P/F on a UIH frame carries one credit byte under credit-based flow control
        at += 1
    s.rfcomm.append((ms, dlci, frame[at:at + length], cut))


# ----------------------------------------------------------------------------------------------------------------------------------
# The commands


@dataclass
class Raster:
    width_bytes: int
    height: int
    data: bytes


def lay_out(stream: bytes) -> tuple[list[str], list[Raster]]:
    """The bytes as commands, as the Phomemo family and plain ESC/POS write them; TSPL as text; anything else shown as bytes."""
    head = stream[:64]
    if re.match(rb"\s*(SIZE|CLS|GAP|DENSITY|SPEED|BITMAP)", head):
        return tspl(stream)
    lines, rasters, at, unknown = [], [], 0, bytearray()

    def flush():
        if unknown:
            lines.append(f"  {len(unknown)} bytes not a known command: {bytes(unknown[:32]).hex(' ')}{' ...' if len(unknown) > 32 else ''}")
            unknown.clear()

    names = {(0x1B, 0x4E, 0x0D): "speed", (0x1B, 0x4E, 0x04): "density"}
    while at < len(stream):
        b = stream[at:at + 8]
        if b[:2] == b"\x1b\x40":
            flush(); lines.append("  1B 40      initialise"); at += 2
        elif b[:2] == b"\x1b\x4e" and len(b) >= 4:
            flush(); lines.append(f"  1B 4E {b[2]:02X} {b[3]:02X} {names.get(tuple(b[:3]), 'setting')} {b[3]}"); at += 4
        elif b[:2] == b"\x1f\x11" and len(b) >= 3:
            flush(); lines.append(f"  1F 11 {b[2]:02X}   media or setting {b[2]}"); at += 3
        elif b[:4] == b"\x1d\x76\x30\x00" and len(b) >= 8:
            flush()
            wb, h = struct.unpack_from("<HH", b, 4)
            data = stream[at + 8: at + 8 + wb * h]
            full = len(data) == wb * h
            rasters.append(Raster(wb, h, data))
            lines.append(f"  1D 76 30 00 raster {wb * 8} dots across ({wb} bytes) by {h} lines, {len(data)} bytes{'' if full else ' (cut short)'}")
            at += 8 + wb * h
        elif b[:2] == b"\x1b\x64" and len(b) >= 3:
            flush(); lines.append(f"  1B 64 {b[2]:02X}   feed {b[2]} lines"); at += 3
        elif b[:2] == b"\x1f\xf0" and len(b) >= 4:
            flush(); lines.append(f"  1F F0 {b[2]:02X} {b[3]:02X} end or status"); at += 4
        else:
            unknown.append(stream[at]); at += 1
    flush()
    return lines, rasters


def tspl(stream: bytes) -> tuple[list[str], list[Raster]]:
    lines, rasters, at = [], [], 0
    while at < len(stream):
        end = stream.find(b"\r\n", at)
        end = len(stream) if end < 0 else end
        line = stream[at:end]
        m = re.match(rb"BITMAP (\d+),(\d+),(\d+),(\d+),(\d+),", line)
        if m:
            wb, h = int(m.group(3)), int(m.group(4))
            start = at + m.end()
            data = bytes(~x & 0xFF for x in stream[start:start + wb * h])  # TSPL: a set bit is paper
            rasters.append(Raster(wb, h, data))
            lines.append(f"  BITMAP {wb * 8} dots across by {h} lines (TSPL)")
            at = start + wb * h
            at = stream.find(b"\r\n", at) + 2 if stream.find(b"\r\n", at) >= 0 else len(stream)
            continue
        lines.append("  " + line.decode("ascii", "replace"))
        at = end + 2
    return lines, rasters


def png(raster: Raster) -> bytes:
    """A raster as a black and white PNG, black where the printer heats."""
    width = raster.width_bytes * 8
    rows = b"".join(b"\0" + bytes(~x & 0xFF for x in raster.data[y * raster.width_bytes:(y + 1) * raster.width_bytes]) for y in range(raster.height))

    def chunk(kind: bytes, data: bytes) -> bytes:
        return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data) & 0xFFFFFFFF)

    return b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", width, raster.height, 1, 0, 0, 0, 0)) + chunk(b"IDAT", zlib.compress(rows)) + chunk(b"IEND", b"")


def read_png_bits(raw: bytes) -> tuple[int, int, list[list[int]]] | None:
    """A 1-bit greyscale PNG's pixels as rows of 1 (black) and 0, as GroupLab's printer-app pictures are written; None for anything else."""
    at, idat, header = 8, b"", None
    while at + 8 <= len(raw):
        length, kind = struct.unpack_from(">I4s", raw, at)
        if kind == b"IHDR":
            header = struct.unpack_from(">IIBB", raw, at + 8)
        elif kind == b"IDAT":
            idat += raw[at + 8:at + 8 + length]
        at += 12 + length
    if not header or header[2] != 1 or header[3] != 0:
        return None
    width, height = header[0], header[1]
    data, stride = zlib.decompress(idat), (header[0] + 7) // 8
    rows = []
    for y in range(height):
        line = data[y * (stride + 1) + 1:(y + 1) * (stride + 1)]
        rows.append([0 if line[x >> 3] & (0x80 >> (x & 7)) else 1 for x in range(width)])
    return width, height, rows


def compare(raster: Raster, page: Path) -> str:
    got = read_png_bits(page.read_bytes())
    if got is None:
        return f"{page.name} is not a 1-bit PNG as GroupLab writes them, so it was not compared."
    width, height, rows = got
    across = raster.width_bytes * 8
    words = f"the page is {width} by {height} dots, the raster {across} by {raster.height}"
    if raster.height != height or across < width:
        return f"Not the same size: {words}. The app resized it, or this raster is only part of the page."
    agree = sum(1 for y in range(height) for x in range(width) if rows[y][x] == ((raster.data[y * raster.width_bytes + (x >> 3)] >> (7 - (x & 7))) & 1))
    return f"Same size ({words}); {100 * agree / (width * height):.2f}% of the dots agree with the page GroupLab made."


# ----------------------------------------------------------------------------------------------------------------------------------
# The nRF Connect log


def read_nrf(path: Path) -> dict:
    text = path.read_text(encoding="utf-8", errors="replace")
    uuids = sorted({u.lower() for u in re.findall(r"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", text)})
    short = sorted({f"0000{u.lower()}-0000-1000-8000-00805f9b34fb" for u in re.findall(r"\bUUID:\s*0x([0-9a-fA-F]{4})\b", text)})
    names = re.findall(r"(?:Name|name)[:=]\s*([^\n,]+)", text)
    return {"uuids": sorted(set(uuids) | set(short)), "names": sorted({n.strip() for n in names})[:5]}


# ----------------------------------------------------------------------------------------------------------------------------------


def analyse(source: Path, nrf: Path | None, page: Path | None, out: Path) -> dict:
    packets, where = find_packets(source)
    s = read_session(packets)
    report: dict = {"read": where, "packets": len(packets), "truncatedFrames": s.truncated}
    lines = [f"Read {len(packets)} Bluetooth packets from {where}."]
    if s.truncated:
        lines.append(f"{s.truncated} frames were cut short: this is the bug report's summary, not the full log. Set the snoop log to "
                     "\"Enabled\" (not \"Filtered\") in Developer options and print again; the bytes below are incomplete.")
    if nrf:
        report["nrf"] = read_nrf(nrf)
        lines.append(f"nRF Connect named these services and characteristics: {', '.join(report['nrf']['uuids']) or 'none found'}.")

    if s.writes:
        report["bluetooth"] = "LE"
        by_handle: dict[int, list[Write]] = {}
        for w in s.writes:
            by_handle.setdefault(w.handle, []).append(w)
        handle, writes = max(by_handle.items(), key=lambda kv: sum(len(w.value) for w in kv[1]))
        uuid, properties = s.characteristics.get(handle, ("not seen in the discovery", 0))
        service = next((u for a, b, u in s.services if a <= handle <= b), "not seen in the discovery")
        gaps = [b.ms - a.ms for a, b in zip(writes, writes[1:])]
        stream = b"".join(w.value for w in writes)
        report.update({"services": [u for _, _, u in s.services], "service": service, "write": uuid, "writeHandle": handle,
                       "withoutResponse": all(w.command for w in writes), "writes": len(writes), "bytes": len(stream),
                       "chunk": max(len(w.value) for w in writes), "pauseMs": statistics.median(gaps) if gaps else None})
        lines.append("Bluetooth LE: an iPhone can use this printer.")
        lines.append(f"Services discovered: {', '.join(u for _, _, u in s.services) or 'none (connect in nRF Connect to see them)'}.")
        lines.append(f"The app wrote {len(stream)} bytes in {len(writes)} writes of up to {report['chunk']} bytes to {uuid} (handle {handle:#06x}) "
                     f"in service {service}, {'without' if report['withoutResponse'] else 'with'} response"
                     + (f", a median of {report['pauseMs']:.0f} ms apart." if gaps else "."))
        answers = sorted({(h, v.hex(' ')) for _, h, v in s.notifications})
        report["answers"] = [{"handle": h, "bytes": v} for h, v in answers[:20]]
        if answers:
            lines.append("The printer answered: " + "; ".join(f"{v} on {h:#06x}" for h, v in answers[:8]) + ("; ..." if len(answers) > 8 else "") + ".")
    elif s.rfcomm:
        report["bluetooth"] = "classic"
        stream = b"".join(d for _, _, d, _ in s.rfcomm)
        lines.append("Classic Bluetooth (RFCOMM, the serial port profile) only: an iPhone cannot print to it without Apple's accessory programme, "
                     "so the phone apps on Android, and the computer where its Bluetooth allows a serial port, are its path.")
        lines.append(f"The app sent {len(stream)} bytes on RFCOMM channel {s.rfcomm[0][1] >> 1}.")
    else:
        raise SystemExit("No writes to a printer are in this recording: was the snoop log on while the page printed?")

    commands, rasters = lay_out(stream)
    report["commands"] = commands
    lines.append("What it sent, in order:")
    lines.extend(commands[:60] + (["  ..."] if len(commands) > 60 else []))
    out.mkdir(parents=True, exist_ok=True)
    report["rasters"] = []
    for i, r in enumerate(rasters):
        name = f"raster-{i + 1}.png"
        (out / name).write_bytes(png(r))
        entry = {"file": name, "dotsAcross": r.width_bytes * 8, "lines": r.height}
        if page:
            entry["againstPage"] = compare(r, page)
            lines.append(f"Raster {i + 1}: {entry['againstPage']}")
        report["rasters"].append(entry)
    if not rasters:
        lines.append("No raster in a known form: the bytes may be compressed (Phomemo's newer models are said to use LZO), which the "
                     "first bytes above show.")
    (out / "printer-recording.json").write_text(json.dumps(report, indent=1), encoding="utf-8")
    (out / "printer-recording.txt").write_text("\n".join(lines) + "\n", encoding="utf-8")
    print("\n".join(lines))
    return report


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("recording", type=Path, help="the bug report zip, a folder unpacked from one, or a btsnoop_hci.log")
    parser.add_argument("--nrf", type=Path, help="nRF Connect's log of the printer's services")
    parser.add_argument("--page", type=Path, help="the page that was printed, as GroupLab's printer-app picture")
    parser.add_argument("--out", type=Path, help="where to write the report and the rasters (default: beside the recording)")
    args = parser.parse_args()
    analyse(args.recording, args.nrf, args.page, args.out or args.recording.with_suffix("").parent / "printer-recording")
    return 0


if __name__ == "__main__":
    sys.exit(main())

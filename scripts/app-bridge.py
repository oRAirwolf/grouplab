#!/usr/bin/env python3
"""Drive GroupLab Dev on a phone or iPad by name, over the USB cable, through its automation bridge.

NOTES-FROM-PLANNING.md entry 315 section 1. GroupLab Dev (never GroupLab itself, which is built without it) listens on the device's own
127.0.0.1, port 47315, for one line of JSON a request, and needs this run's key, which it shows in Settings, About and writes to
bridge/key in its own files. This forwards the port over the cable, reads the key the same way, sends one command and prints the answer:

    python scripts/app-bridge.py --android [--serial S] ping
    python scripts/app-bridge.py --ios open place=capture
    python scripts/app-bridge.py --ios press name="Fix holes"
    python scripts/app-bridge.py --android screenshot name=result --out out/bridge     the picture saved there as well
    python scripts/app-bridge.py --key K --port 47315 tree                              a port already forwarded, and a key given
    python scripts/app-bridge.py --self-test                                            against a made-up GroupLab Dev, no device

The commands are the scenario steps (mobile/GroupLab.Mobile/Dev/Scenario.cs): open, picture, wait, press, type, screenshot, tree, sleep,
log, and ping and memory. A value that reads as a number is sent as one. Android forwards with adb; iOS with pymobiledevice3 (request 60),
whose key is read from GroupLab Dev's Documents by scripts/ipad-logs.py's own connection. Nothing else on the device is touched.
"""
from __future__ import annotations

import argparse
import base64
import importlib.util
import json
import socket
import subprocess
import sys
import threading
import time
from pathlib import Path

HERE = Path(__file__).resolve().parent
PORT = 47315
PACKAGE = "org.grouplab.app.dev"
ADB = r"C:\Dev\tools\android-sdk\platform-tools\adb.exe"


def request(command: str, pairs: list[str], key: str) -> dict:
    """The request line's object: the command, each name=value pair (numbers as numbers), and the key."""
    body: dict = {"key": key, "do": command}
    for pair in pairs:
        name, sep, value = pair.partition("=")
        if not sep or not name:
            raise SystemExit(f"app-bridge: {pair!r} is not name=value")
        try:
            body[name] = float(value) if "." in value else int(value)
        except ValueError:
            body[name] = value
    return body


def send(port: int, body: dict, timeout: float = 600) -> dict:
    """One request over the forwarded port, one answer back."""
    with socket.create_connection(("127.0.0.1", port), timeout=timeout) as connection:
        connection.sendall((json.dumps(body) + "\n").encode())
        data = b""
        while not data.endswith(b"\n"):
            chunk = connection.recv(1 << 16)
            if not chunk:
                break
            data += chunk
    if not data:
        raise SystemExit("app-bridge: GroupLab Dev closed the connection without an answer; is its bridge on in Settings, About?")
    return json.loads(data)


def shown(answer: dict, out: Path | None) -> str:
    """The answer for a person: a screenshot's picture saved rather than printed."""
    answer = dict(answer)
    if "png" in answer:
        picture = base64.b64decode(answer.pop("png"))
        if out:
            out.mkdir(parents=True, exist_ok=True)
            target = out / (answer.get("detail") or "screen.png")
            target.write_bytes(picture)
            answer["saved"] = str(target)
        else:
            answer["png"] = f"({len(picture)} bytes; --out saves it)"
    return json.dumps(answer, indent=1)


def android(serial: str | None) -> str:
    """The port forwarded with adb, and the key read from GroupLab Dev's own files, which only its debuggable build allows."""
    base = [ADB] + (["-s", serial] if serial else [])
    subprocess.run(base + ["forward", f"tcp:{PORT}", f"tcp:{PORT}"], check=True, capture_output=True, timeout=30)
    key = subprocess.run(base + ["exec-out", "run-as", PACKAGE, "cat", "files/bridge/key"], capture_output=True, text=True, timeout=30).stdout
    return key.strip()


def ios() -> tuple[str, subprocess.Popen]:
    """The port forwarded with pymobiledevice3, kept open while this runs, and the key read from GroupLab Dev's Documents."""
    forward = subprocess.Popen([sys.executable, "-m", "pymobiledevice3", "usbmux", "forward", str(PORT), str(PORT)],
                               stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    spec = importlib.util.spec_from_file_location("ipad_logs", HERE / "ipad-logs.py")
    logs = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(logs)
    device = logs.Usb()
    try:
        key = device.read(PACKAGE, "/Documents/bridge/key").decode().strip()
    finally:
        device.close()
    time.sleep(1)
    return key, forward


class MadeUp(threading.Thread):
    """A made-up GroupLab Dev for the self-test: its key, a refusal without it, and a screenshot's picture."""

    def __init__(self):
        super().__init__(daemon=True)
        self.server = socket.socket()
        self.server.bind(("127.0.0.1", 0))
        self.server.listen()
        self.port = self.server.getsockname()[1]
        self.seen: list[dict] = []

    def run(self):
        while True:
            try:
                connection, _ = self.server.accept()
            except OSError:
                return
            with connection:
                line = connection.makefile().readline()
                body = json.loads(line)
                self.seen.append(body)
                if body.get("key") != "right":
                    answer = {"ok": False, "refused": True, "detail": "the key is not this run's"}
                elif body["do"] == "screenshot":
                    answer = {"ok": True, "detail": "shot.png", "png": base64.b64encode(b"\x89PNG made up").decode()}
                else:
                    answer = {"ok": True, "detail": body["do"]}
                connection.sendall((json.dumps(answer) + "\n").encode())


def self_test() -> int:
    import tempfile

    failures: list[str] = []

    def expect(what: str, condition: bool) -> None:
        if not condition:
            failures.append(what)

    body = request("press", ["name=Fix holes", "seconds=30", "by=0.02"], "right")
    expect("pairs read", body == {"key": "right", "do": "press", "name": "Fix holes", "seconds": 30, "by": 0.02})
    fake = MadeUp()
    fake.start()
    expect("answered", send(fake.port, request("ping", [], "right")) == {"ok": True, "detail": "ping"})
    expect("refused without the key", send(fake.port, request("ping", [], "wrong")).get("refused") is True)
    with tempfile.TemporaryDirectory() as folder:
        said = shown(send(fake.port, request("screenshot", ["name=shot"], "right")), Path(folder))
        expect("picture saved", (Path(folder) / "shot.png").read_bytes() == b"\x89PNG made up" and "saved" in said)
    expect("picture not printed", "bytes" in shown({"ok": True, "png": base64.b64encode(b"x").decode()}, None))
    expect("the key sent each time", all("key" in seen for seen in fake.seen))
    fake.server.close()
    for failure in failures:
        print("FAILED:", failure)
    print(f"app-bridge.py self-test: {'failed' if failures else 'passed'}")
    return 1 if failures else 0


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description="Drive GroupLab Dev over the USB cable through its automation bridge.")
    parser.add_argument("--self-test", action="store_true")
    where = parser.add_mutually_exclusive_group()
    where.add_argument("--android", action="store_true")
    where.add_argument("--ios", action="store_true")
    parser.add_argument("--serial", help="the adb serial, where several devices are connected")
    parser.add_argument("--key", help="this run's key, where it is not to be read from the device")
    parser.add_argument("--port", type=int, default=PORT)
    parser.add_argument("--out", type=Path, help="where a screenshot is saved")
    parser.add_argument("command", nargs="?")
    parser.add_argument("pairs", nargs="*")
    args = parser.parse_args(argv[1:])
    if args.self_test:
        return self_test()
    if not args.command:
        parser.print_help()
        return 2
    forward = None
    key = args.key
    if args.android:
        key = key or android(args.serial)
    elif args.ios:
        read, forward = ios()
        key = key or read
    if not key:
        print("app-bridge: no key; turn the bridge on in GroupLab Dev's Settings, About, or give --key")
        return 2
    try:
        print(shown(send(args.port, request(args.command, args.pairs, key)), args.out))
    finally:
        if forward:
            forward.terminate()
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))

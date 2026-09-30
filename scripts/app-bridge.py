#!/usr/bin/env python3
"""Drive GroupLab Dev on a phone or iPad by name, over the USB cable, through its automation bridge.

NOTES-FROM-PLANNING.md entry 315 section 1. GroupLab Dev (never GroupLab itself, which is built without it) listens on the device's own
127.0.0.1, port 47315, for one line of JSON a request, and needs this run's key, which it shows in Settings, About and writes to
bridge/key in its own files. This forwards the port over the cable, reads the key the same way, sends one command and prints the answer:

    python scripts/app-bridge.py --platform android ping
    python scripts/app-bridge.py --platform ios open place=capture
    python scripts/app-bridge.py --platform android --push sample.jpg read file=sample.jpg      the picture copied over first, then read
    python scripts/app-bridge.py --platform ios press name=result-fix-holes
    python scripts/app-bridge.py --platform android choose name=settings-keep-pictures on=false
    python scripts/app-bridge.py --platform android screenshot name=result --out out/bridge     the picture saved there as well
    python scripts/app-bridge.py --key K --port 47315 tree                                      a port already forwarded, and a key given
    python scripts/app-bridge.py --self-test                                                    against a made-up GroupLab Dev, no device

The commands are the scenario steps (mobile/GroupLab.Mobile/Dev/Scenario.cs): open, back, picture, read, wait, press, type, choose,
scroll, setting, reset, screenshot, tree, sleep and log; and the bridge's own: ping, tree, log, timings and memory. `ping` lists them.
A value is sent as JSON where it reads as JSON (numbers, true, false, null) and as words otherwise. Android forwards with adb and reads
the key with run-as, which only the debuggable GroupLab Dev allows; iOS forwards with pymobiledevice3 (request 60) and reads the key from
GroupLab Dev's Documents with scripts/ipad-logs.py's own connection. Nothing else on the device is touched.
"""
from __future__ import annotations

import argparse
import base64
import importlib.util
import json
import re
import shutil
import socket
import subprocess
import sys
import threading
import time
from pathlib import Path

HERE = Path(__file__).resolve().parent
PORT = 47315
PACKAGE = "org.grouplab.app.dev"
ADB_HERE = Path(r"C:\Dev\tools\android-sdk\platform-tools\adb.exe")


def adb() -> str:
    """The SDK's own adb on Alan's machine, or whichever is on the path elsewhere."""
    return str(ADB_HERE) if ADB_HERE.exists() else (shutil.which("adb") or "adb")


def value(text: str):
    """A value as JSON where it reads as one (a number, true, false, null), and as words otherwise."""
    try:
        read = json.loads(text)
    except ValueError:
        return text
    return read if isinstance(read, (int, float, bool)) or read is None else text


def request(command: str, pairs: list[str], key: str) -> dict:
    """The request line's object: the command, each name=value pair, and the key."""
    body: dict = {"key": key, "do": command}
    for pair in pairs:
        name, sep, text = pair.partition("=")
        if not sep or not name:
            raise SystemExit(f"app-bridge: {pair!r} is not name=value")
        body[name] = value(text)
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


def safe_name(name: str) -> str:
    """A file name with nothing but letters, digits, dashes, underscores and dots, as the application makes its own."""
    cleaned = re.sub(r"[^A-Za-z0-9._-]", "", name).strip(".")
    return cleaned or "screen.png"


def shown(answer: dict, out: Path | None) -> str:
    """The answer for a person: a screenshot's picture saved rather than printed."""
    answer = dict(answer)
    if "png" in answer:
        picture = base64.b64decode(answer.pop("png"))
        if out:
            out.mkdir(parents=True, exist_ok=True)
            target = out / safe_name(str(answer.get("detail") or "screen.png"))
            target.write_bytes(picture)
            answer["saved"] = str(target)
        else:
            answer["png"] = f"({len(picture)} bytes; --out saves it)"
    return json.dumps(answer, indent=1)


def android(serial: str | None, push: Path | None) -> str:
    """The port forwarded with adb, a picture copied into the scenario folder, and the key read from GroupLab Dev's own files."""
    base = [adb()] + (["-s", serial] if serial else [])
    subprocess.run(base + ["forward", f"tcp:{PORT}", f"tcp:{PORT}"], check=True, capture_output=True, timeout=30)
    if push:
        target = f"files/scenario/{safe_name(push.name)}"
        with push.open("rb") as picture:
            subprocess.run(base + ["exec-in", "run-as", PACKAGE, "sh", "-c", f"mkdir -p files/scenario && cat > {target}"],
                           stdin=picture, check=True, capture_output=True, timeout=120)
    key = subprocess.run(base + ["exec-out", "run-as", PACKAGE, "cat", "files/bridge/key"], capture_output=True, text=True, timeout=30).stdout
    return key.strip()


def listening(port: int, seconds: float) -> bool:
    """Whether something takes connections on the port within the time: the forward, once it is up."""
    until = time.monotonic() + seconds
    while time.monotonic() < until:
        try:
            with socket.create_connection(("127.0.0.1", port), timeout=1):
                return True
        except OSError:
            time.sleep(0.25)
    return False


def ios(push: Path | None) -> tuple[str, subprocess.Popen]:
    """The port forwarded with pymobiledevice3, kept open while this runs, a picture copied over, and the key read from Documents."""
    forward = subprocess.Popen([sys.executable, "-m", "pymobiledevice3", "usbmux", "forward", str(PORT), str(PORT)],
                               stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    spec = importlib.util.spec_from_file_location("ipad_logs", HERE / "ipad-logs.py")
    logs = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(logs)
    device = logs.Usb()
    try:
        if push:
            documents = device._vend(PACKAGE)
            device.run(documents.makedirs("/Documents/scenario"))
            device.run(documents.set_file_contents(f"/Documents/scenario/{safe_name(push.name)}", push.read_bytes()))
        key = device.read(PACKAGE, "/Documents/bridge/key").decode().strip()
    finally:
        device.close()
    listening(PORT, 10)
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
                if not line:
                    continue
                body = json.loads(line)
                self.seen.append(body)
                if body.get("key") != "right":
                    answer = {"ok": False, "refused": True, "detail": "the key is not this run's"}
                elif body["do"] == "screenshot":
                    answer = {"ok": True, "detail": "../shot.png", "png": base64.b64encode(b"\x89PNG made up").decode()}
                else:
                    answer = {"ok": True, "detail": body["do"]}
                connection.sendall((json.dumps(answer) + "\n").encode())


def self_test() -> int:
    import tempfile

    failures: list[str] = []

    def expect(what: str, condition: bool) -> None:
        if not condition:
            failures.append(what)

    body = request("choose", ["name=Fix holes", "seconds=30", "by=0.02", "on=false", "value=null", "file=1.jpg"], "right")
    expect("pairs read", body == {"key": "right", "do": "choose", "name": "Fix holes", "seconds": 30, "by": 0.02, "on": False,
                                  "value": None, "file": "1.jpg"})
    expect("a list stays words", request("type", ["text=[1]"], "k")["text"] == "[1]")
    fake = MadeUp()
    fake.start()
    expect("answered", send(fake.port, request("ping", [], "right")) == {"ok": True, "detail": "ping"})
    expect("refused without the key", send(fake.port, request("ping", [], "wrong")).get("refused") is True)
    expect("the forward seen", listening(fake.port, 2))
    with tempfile.TemporaryDirectory() as folder:
        said = shown(send(fake.port, request("screenshot", ["name=shot"], "right")), Path(folder) / "out")
        expect("picture saved inside the folder asked for", (Path(folder) / "out" / "shot.png").read_bytes() == b"\x89PNG made up" and "saved" in said)
        expect("nothing written beside it", not (Path(folder) / "shot.png").exists())
    expect("picture not printed", "bytes" in shown({"ok": True, "png": base64.b64encode(b"x").decode()}, None))
    expect("the key sent each time", all("key" in seen for seen in fake.seen))
    expect("names made safe", safe_name("../../a b.png") == "ab.png" and safe_name("..") == "screen.png")
    fake.server.close()
    for failure in failures:
        print("FAILED:", failure)
    print(f"app-bridge.py self-test: {'failed' if failures else 'passed'}")
    return 1 if failures else 0


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(
        description="Drive GroupLab Dev over the USB cable through its automation bridge: one command, one answer.",
        epilog="Commands: ping, open, back, picture, read, wait, press, type, choose, scroll, setting, reset, screenshot, tree, sleep, "
               "log, timings, memory. Each takes name=value pairs, as in mobile/GroupLab.Mobile/Dev/Scenario.cs.")
    parser.add_argument("--self-test", action="store_true", help="check this script against a made-up GroupLab Dev; no device")
    parser.add_argument("--platform", choices=["android", "ios"],
                        help="forward the port and read the key over the cable: adb on Android, pymobiledevice3 on iOS")
    parser.add_argument("--serial", help="the adb serial, where several Android devices are connected")
    parser.add_argument("--key", help="this run's key, where it is not to be read from the device")
    parser.add_argument("--port", type=int, default=PORT, help=f"the forwarded port on this computer (default {PORT})")
    parser.add_argument("--push", type=Path, help="a picture copied into GroupLab Dev's scenario folder first, for picture or read")
    parser.add_argument("--out", type=Path, help="where a screenshot is saved")
    parser.add_argument("command", nargs="?", help="what GroupLab Dev is to do")
    parser.add_argument("pairs", nargs="*", help="name=value, each a field of the command")
    args = parser.parse_args(argv[1:])
    if args.self_test:
        return self_test()
    if not args.command:
        parser.print_help()
        return 2
    if args.push and not args.push.is_file():
        print(f"app-bridge: {args.push} is not a file")
        return 2
    if args.push and not args.platform:
        print("app-bridge: --push needs --platform, to reach the device")
        return 2
    forward = None
    key = args.key
    if args.platform == "android":
        read = android(args.serial, args.push)
        key = key or read
    elif args.platform == "ios":
        read, forward = ios(args.push)
        key = key or read
    if not key:
        print("app-bridge: no key; turn the bridge on in GroupLab Dev's Settings, About, or give --key")
        return 2
    try:
        answer = send(args.port, request(args.command, args.pairs, key))
        print(shown(answer, args.out))
    finally:
        if forward:
            forward.terminate()
    return 0 if answer.get("ok") else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv))

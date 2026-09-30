#!/usr/bin/env python3
"""GroupLab's log and folder from an iPad or iPhone on a USB cable, the way adb works for Android.

NOTES-FROM-PLANNING.md entry 311 section 3 item 3. With Apple's device driver on this computer (the Apple Devices app) and the iPad trusted
(docs/notes/for-alan.md request 60), pymobiledevice3 reaches the device over USB. This uses it for two things and nothing else:

1. The live log of GroupLab's own process. The device is asked only for the process named GroupLab.iOS (or its share extension,
   GroupLab.Share), by its process number, so no other app's lines are ever sent to this computer; each line is checked again here.
2. A copy of GroupLab's Documents folder: its log in logs, a sitting's kept pictures in sitting, the self-test's folder, and the sessions.
   iOS lets a computer read that folder because GroupLab turns on file sharing (UIFileSharingEnabled), even for a TestFlight build. Only
   that one application's Documents is opened; nothing is written to the device, and nothing outside that folder is read.

Never read: another application, a notification, the device's own data, a photograph's location. The device's serial number and UDID are
never printed or written.

    python scripts/ipad-logs.py --list                            the devices on the cable, by model and iOS version only
    python scripts/ipad-logs.py --stream [--seconds N] [--out F]  GroupLab's log lines as they happen (Ctrl+C stops)
    python scripts/ipad-logs.py --pull [--into DIR] [--only logs,sitting]
                                                                  GroupLab's Documents copied into DIR (default out/ipad/<time>)
    add --dev to any of them for GroupLab Dev (org.grouplab.app.dev) instead of GroupLab (org.grouplab.app)
    add --dry-run to say what would be done without loading pymobiledevice3 or touching a device
    python scripts/ipad-logs.py --self-test                       every rule against a made-up device, no cable and no pymobiledevice3

Setup: python -m pip install --user pymobiledevice3 (request 60).
"""
from __future__ import annotations

import argparse
import datetime as _dt
import posixpath
import sys
import time
from dataclasses import dataclass, field
from pathlib import Path, PurePosixPath
from typing import Callable, Iterable, Iterator

REPO = Path(__file__).resolve().parent.parent
BUNDLES = {False: "org.grouplab.app", True: "org.grouplab.app.dev"}
# The executables of the application and its share extension: ios/GroupLab.iOS and ios/GroupLab.Share build these names.
PROCESSES = ("GroupLab.iOS", "GroupLab.Share")
# The root iOS hands over with VendDocuments: the application's Documents and nothing else.
DOCUMENTS = "/Documents"


@dataclass
class Line:
    """One log line from the device: its process, time, level and words."""
    process: str
    when: _dt.datetime
    level: str
    message: str


class Device:
    """What this needs from a device. The real one is pymobiledevice3 over USB; the self-test's is made up."""

    def describe(self) -> str: raise NotImplementedError
    def processes(self) -> dict[int, str]: raise NotImplementedError
    def syslog(self, pid: int) -> Iterator[Line]: raise NotImplementedError
    def documents(self, bundle: str) -> Iterator[tuple[str, int]]: raise NotImplementedError
    def read(self, bundle: str, path: str) -> bytes: raise NotImplementedError


def ours(process: str) -> bool:
    """Whether a line's process is GroupLab's: its executable's name, whatever folder it runs from."""
    return posixpath.basename(process) in PROCESSES


def grouplab_pid(device: Device) -> int | None:
    """GroupLab's process number, the application's before the share extension's; None where it is not running."""
    running = device.processes()
    for name in PROCESSES:
        for pid, process in running.items():
            if posixpath.basename(process) == name:
                return pid
    return None


def stream(device: Device, write: Callable[[str], None], seconds: float | None, wait: float = 60.0,
           clock: Callable[[], float] = time.monotonic, sleep: Callable[[float], None] = time.sleep) -> int:
    """
    GroupLab's log lines as the device writes them, each formatted and handed to write; returns how many. The device is asked for GroupLab's
    process alone, and a line from any other process that still arrives is dropped here.
    """
    start = clock()
    pid = grouplab_pid(device)
    while pid is None and clock() - start < wait:
        sleep(2)
        pid = grouplab_pid(device)
    if pid is None:
        write(f"GroupLab is not running on the device; open it and run this again (waited {wait:.0f} s).")
        return 0
    count = 0
    for line in device.syslog(pid):
        if not ours(line.process):
            continue
        write(f"{line.when:%H:%M:%S.%f}"[:-3] + f" {line.level:<7} {line.message}")
        count += 1
        if seconds is not None and clock() - start >= seconds:
            break
    return count


def safe_relative(remote: str) -> PurePosixPath | None:
    """A path under Documents as a relative path, or None where it would leave the folder it is copied into."""
    path = PurePosixPath(remote)
    try:
        relative = path.relative_to(DOCUMENTS)
    except ValueError:
        return None
    if not relative.parts or any(part in ("", ".", "..") or ":" in part or "\\" in part for part in relative.parts):
        return None
    return relative


def pull(device: Device, bundle: str, into: Path, only: set[str] | None, write: Callable[[str], None]) -> tuple[int, int]:
    """Copies GroupLab's Documents into a folder, or only the top-level folders named in only; returns files and bytes copied."""
    files = total = 0
    for remote, size in device.documents(bundle):
        relative = safe_relative(remote)
        if relative is None:
            write(f"skipped a path outside Documents: {remote}")
            continue
        if only and relative.parts[0] not in only:
            continue
        target = into.joinpath(*relative.parts)
        target.parent.mkdir(parents=True, exist_ok=True)
        data = device.read(bundle, remote)
        target.write_bytes(data)
        files += 1
        total += len(data)
    write(f"{files} files, {total / 1_048_576:.1f} MB, from {bundle}'s Documents into {into}")
    return files, total


class Usb(Device):
    """pymobiledevice3 over USB. Every call runs on one event loop of its own, since pymobiledevice3 is asynchronous."""

    def __init__(self):
        import asyncio

        from pymobiledevice3.lockdown import create_using_usbmux

        self.loop = asyncio.new_event_loop()
        self.run = self.loop.run_until_complete
        self.lockdown = self.run(create_using_usbmux())
        self.vended: dict[str, object] = {}

    def describe(self) -> str:
        values = self.lockdown.all_values
        return f"{values.get('DeviceClass', 'device')} {values.get('ProductType', '')}, iOS {values.get('ProductVersion', '')}".strip()

    def processes(self) -> dict[int, str]:
        from pymobiledevice3.services.os_trace import OsTraceService

        payload = self.run(OsTraceService(lockdown=self.lockdown).get_pid_list()).get("Payload", {})
        return {int(pid): str(info.get("ProcessName", "")) for pid, info in payload.items()}

    def syslog(self, pid: int) -> Iterator[Line]:
        from pymobiledevice3.services.os_trace import OsTraceService

        entries = OsTraceService(lockdown=self.lockdown).syslog(pid=pid)
        while True:
            try:
                entry = self.run(entries.__anext__())
            except StopAsyncIteration:
                return
            level = getattr(entry.level, "name", str(entry.level))
            yield Line(entry.filename, entry.timestamp, level, entry.message)

    def _vend(self, bundle: str):
        """One session with the application's Documents, opened once and kept for the whole copy."""
        from pymobiledevice3.services.house_arrest import HouseArrestService

        if bundle not in self.vended:
            self.vended[bundle] = self.run(HouseArrestService.create(lockdown=self.lockdown, bundle_id=bundle, documents_only=True))
        return self.vended[bundle]

    def documents(self, bundle: str) -> Iterator[tuple[str, int]]:
        service = self._vend(bundle)
        walk = service.walk(DOCUMENTS)
        found: list[tuple[str, int]] = []
        while True:
            try:
                folder, _, names = self.run(walk.__anext__())
            except StopAsyncIteration:
                break
            for name in names:
                path = posixpath.join(folder, name)
                found.append((path, int(self.run(service.stat(path)).get("st_size", 0))))
        yield from found

    def read(self, bundle: str, path: str) -> bytes:
        return self.run(self._vend(bundle).get_file_contents(path))

    def close(self) -> None:
        for service in self.vended.values():
            self.run(service.close())
        self.vended.clear()


def devices() -> list[str]:
    """Each device on the cable, by class, model and iOS version; never its serial number."""
    import asyncio

    from pymobiledevice3.lockdown import create_using_usbmux
    from pymobiledevice3.usbmux import list_devices

    async def each() -> list[str]:
        said = []
        for mux in await list_devices():
            lockdown = await create_using_usbmux(serial=mux.serial, autopair=False)
            values = lockdown.all_values
            said.append(f"{values.get('DeviceClass', 'device')} {values.get('ProductType', '')}, iOS {values.get('ProductVersion', '')}"
                        f" ({'USB' if getattr(mux, 'connection_type', 'USB') == 'USB' else 'network'})")
        return said

    return asyncio.new_event_loop().run_until_complete(each())


@dataclass
class MadeUp(Device):
    """A made-up iPad for the self-test: GroupLab and two other applications running, and a Documents folder with a trap in it."""
    running: dict[int, str] = field(default_factory=lambda: {7: "SpringBoard", 42: "GroupLab.iOS", 99: "MobileMail"})
    asked: list[int] = field(default_factory=list)
    files: dict[str, bytes] = field(default_factory=lambda: {
        "/Documents/logs/grouplab-20260930-120000-1.log": b"INFO app.start\n",
        "/Documents/sitting/picture-0001/picture.jpg": b"\xff\xd8 picture",
        "/Documents/sitting/picture-0001/live.txt": b"say=Ready",
        "/Documents/settings.json": b"{}",
        "/Documents/../Library/Preferences/secret.plist": b"never",
        "/private/var/mobile/Library/SMS/sms.db": b"never",
    })
    read_paths: list[str] = field(default_factory=list)

    def describe(self): return "iPad iPad14,1, iOS 26.0"
    def processes(self): return dict(self.running)

    def syslog(self, pid):
        self.asked.append(pid)
        when = _dt.datetime(2026, 9, 30, 12, 0, 0)
        yield Line("/private/var/containers/Bundle/Application/X/GroupLab.iOS.app/GroupLab.iOS", when, "Info", "GroupLab INFO app.start")
        yield Line("/System/Library/CoreServices/SpringBoard.app/SpringBoard", when, "Info", "someone else's notification")
        yield Line("GroupLab.iOS", when, "Error", "GroupLab ERROR read.stage failed")

    def documents(self, bundle):
        assert bundle in BUNDLES.values()
        yield from ((path, len(data)) for path, data in self.files.items())

    def read(self, bundle, path):
        self.read_paths.append(path)
        return self.files[path]


def self_test() -> int:
    import tempfile

    failures: list[str] = []

    def expect(what: str, condition: bool) -> None:
        if not condition:
            failures.append(what)

    # The log: only GroupLab's process is asked for, and another process's line that arrives anyway is dropped.
    device = MadeUp()
    said: list[str] = []
    count = stream(device, said.append, seconds=None)
    expect("asks for GroupLab's process only", device.asked == [42])
    expect("two lines of GroupLab's", count == 2 and len(said) == 2)
    expect("no other app's words", not any("notification" in line for line in said))
    expect("a line's form", said[0].startswith("12:00:00.000 Info    GroupLab INFO app.start"))

    # GroupLab not running: said, after waiting, and nothing read.
    device = MadeUp(running={7: "SpringBoard"})
    ticks = iter(range(0, 1000, 30))
    said = []
    count = stream(device, said.append, seconds=None, wait=60, clock=lambda: next(ticks), sleep=lambda s: None)
    expect("not running", count == 0 and "not running" in said[0] and device.asked == [])

    # The share extension counts as GroupLab.
    expect("the share extension", grouplab_pid(MadeUp(running={5: "GroupLab.Share"})) == 5)

    # The folder: only Documents, nothing that climbs out of it, and only the folders asked for.
    expect("inside", safe_relative("/Documents/logs/a.log") == PurePosixPath("logs/a.log"))
    expect("climbing out", safe_relative("/Documents/../Library/x") is None)
    expect("elsewhere", safe_relative("/private/var/mobile/Library/SMS/sms.db") is None)
    expect("the folder itself", safe_relative("/Documents") is None)
    with tempfile.TemporaryDirectory() as folder:
        into = Path(folder) / "pulled"
        device = MadeUp()
        said = []
        files, _ = pull(device, BUNDLES[False], into, None, said.append)
        expect("four files copied", files == 4)
        expect("nothing outside read", not any("Library" in p or "private" in p for p in device.read_paths))
        expect("nothing written outside", all(Path(folder) in p.parents for p in Path(folder).rglob("*") if p.is_file()))
        expect("the picture copied", (into / "sitting" / "picture-0001" / "picture.jpg").read_bytes() == b"\xff\xd8 picture")
        device = MadeUp()
        files, _ = pull(device, BUNDLES[True], Path(folder) / "logs-only", {"logs"}, said.append)
        expect("only logs", files == 1 and device.read_paths == ["/Documents/logs/grouplab-20260930-120000-1.log"])

    # The description names the model and never a serial number.
    expect("described", MadeUp().describe() == "iPad iPad14,1, iOS 26.0")

    for failure in failures:
        print("FAILED:", failure)
    print(f"ipad-logs.py self-test: {'failed' if failures else 'passed'}")
    return 1 if failures else 0


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description="GroupLab's log and Documents from an iPad or iPhone on a USB cable.")
    what = parser.add_mutually_exclusive_group(required=True)
    what.add_argument("--list", action="store_true")
    what.add_argument("--stream", action="store_true")
    what.add_argument("--pull", action="store_true")
    what.add_argument("--self-test", action="store_true")
    parser.add_argument("--dev", action="store_true", help="GroupLab Dev instead of GroupLab")
    parser.add_argument("--seconds", type=float, help="stop streaming after this long")
    parser.add_argument("--out", type=Path, help="also write the streamed lines to this file")
    parser.add_argument("--into", type=Path, help="where --pull copies to")
    parser.add_argument("--only", help="--pull only these top-level folders, such as logs,sitting")
    parser.add_argument("--dry-run", action="store_true", help="say what would be done, and touch no device")
    args = parser.parse_args(argv[1:])

    if args.self_test:
        return self_test()

    bundle = BUNDLES[args.dev]
    into = args.into or REPO / "out" / "ipad" / _dt.datetime.now().strftime("%Y%m%d-%H%M%S")
    only = {part.strip() for part in args.only.split(",")} if args.only else None
    if args.dry_run:
        if args.list:
            print("Would list the devices on the cable by model and iOS version.")
        elif args.stream:
            print(f"Would stream the log of {' or '.join(PROCESSES)} ({bundle}), asking the device for that process alone"
                  + (f", for {args.seconds:.0f} s" if args.seconds else "") + (f", into {args.out}" if args.out else "") + ".")
        else:
            print(f"Would copy {bundle}'s Documents" + (f" ({', '.join(sorted(only))} only)" if only else "") + f" into {into}.")
        return 0

    try:
        import pymobiledevice3  # noqa: F401
    except ImportError:
        print("pymobiledevice3 is not installed: python -m pip install --user pymobiledevice3 (docs/notes/for-alan.md request 60).")
        return 2

    if args.list:
        found = devices()
        print("\n".join(found) if found else "No device on the cable. Plug it in, unlock it and tap Trust.")
        return 0 if found else 1

    device = Usb()
    print(f"Connected: {device.describe()}")
    if args.stream:
        out = args.out.open("a", encoding="utf-8") if args.out else None

        def write(line: str) -> None:
            print(line, flush=True)
            if out:
                out.write(line + "\n")
                out.flush()

        try:
            stream(device, write, args.seconds)
        except KeyboardInterrupt:
            pass
        finally:
            if out:
                out.close()
        return 0

    try:
        pull(device, bundle, into, only, print)
    finally:
        device.close()
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))

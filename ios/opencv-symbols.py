#!/usr/bin/env python3
"""The OpenCV entry points the iOS head keeps, NOTES-FROM-PLANNING.md entry 290 section 2 item 3.

    python3 ios/opencv-symbols.py <OpenCvSharpExtern.xcframework> <OpenCvSharp.dll> <output .props>

OpenCvSharp's managed half calls its native half through P/Invoke by name, resolved at run time (ios/GroupLab.iOS/NativeOpenCv.cs points the
library's name at the application itself). The iOS build links the application with -dead_strip and an exported symbols list made from the
P/Invokes it knows are linked in, and OpenCvSharp's are not among them, so without this every OpenCV entry point was stripped: the first
build linked and had none. This writes an MSBuild file of ReferenceNativeSymbol items, one for each function OpenCvSharp's assembly names
that the framework defines in both its slices, which the build keeps and exports. Run on macOS, with Xcode's nm.
"""

from __future__ import annotations

import re
import subprocess
import sys
from pathlib import Path


def defined(archive: Path) -> set[str]:
    """The C functions an archive defines globally, without the leading underscore; C++ names (mangled, __Z) are left out."""
    out = subprocess.run(["xcrun", "nm", "-gU", str(archive)], capture_output=True, text=True, check=True).stdout
    names = set()
    for line in out.splitlines():
        parts = line.split()
        if len(parts) == 3 and parts[1] == "T" and parts[2].startswith("_") and not parts[2].startswith("__Z"):
            names.add(parts[2][1:])
    return names


def named(assembly: Path) -> set[str]:
    """
    Every identifier in the assembly's metadata strings, and every tail of each: its P/Invokes' entry points among them. The compiler
    stores a name that ends another only once, inside the longer one, so vector_Point2f_delete is found only as the tail of
    vector_vector_Point2f_delete; reading whole strings alone missed it, and the QR reader stopped on it on the simulator.
    """
    names = set()
    for m in re.findall(rb"[A-Za-z_][A-Za-z0-9_]{2,}", assembly.read_bytes()):
        word = m.decode()
        names.update(word[i:] for i in range(len(word) - 2))
    return names


def main(argv: list[str]) -> int:
    if len(argv) != 3:
        print(__doc__)
        return 2
    framework, assembly, output = Path(argv[0]), Path(argv[1]), Path(argv[2])
    slices = sorted(framework.glob("*/libOpenCvSharpExtern.a"))
    if len(slices) < 2:
        print(f"::error::{framework} does not hold the device and simulator archives")
        return 1
    common = set.intersection(*(defined(s) for s in slices))
    keep = sorted(common & named(assembly))
    # A few names every build needs, checked so a change of OpenCvSharp that renamed them is seen here rather than on a phone.
    for needed in ("core_Mat_new1", "imgproc_phaseCorrelate", "aruco_ArucoDetector_detectMarkers", "wechat_qrcode_WeChatQRCode_detectAndDecode",
                   "vector_Point2f_delete"):
        if needed not in keep:
            print(f"::error::{needed} is not among the entry points kept")
            return 1
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_bytes(("<Project>\n  <!-- Written by ios/opencv-symbols.py; never committed. -->\n  <ItemGroup>\n"
                        + "".join(f'    <ReferenceNativeSymbol Include="{name}" SymbolType="Function" />\n' for name in keep)
                        + "  </ItemGroup>\n</Project>\n").encode())
    print(f"{len(keep)} OpenCV entry points kept, of {len(common)} C functions the framework defines in both slices")
    # What OpenCvSharp names in the families GroupLab's modules build and the framework does not define: called, they would fail.
    families = re.compile(r"^(core|imgproc|imgcodecs|calib3d|features2d|flann|objdetect|dnn|aruco|wechat_qrcode|vector|std|string)_[A-Za-z0-9_]+$")
    absent = sorted(n for n in named(assembly) if families.match(n) and n not in common)
    print(f"{len(absent)} such names in OpenCvSharp are not in the framework, for example: " + ", ".join(absent[:40]))
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))

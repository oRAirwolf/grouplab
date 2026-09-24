# 2026-09-24, entry 164: the first macOS log, and two things it raises that need no tester

A friend of Alan's ran nightly 93 on a MacBook Pro with an M5 Max and sent a diagnostics report
(`grouplab-report-20260924-031526.zip`, description "testtest123", saved through the report dialog).
The planning session read it. This entry records what it proves and asks two questions that can be
answered from this repository without the tester. His answers to the checklist in entry 163 section 6
will follow as a separate entry, and `docs/PLATFORM-SUPPORT.md` waits for that one.

## 1. What the log proves

| | from the report |
|---|---|
| build | 0.2.0-nightly.93, commit aa8c559 |
| macOS | 27.0.0 |
| process and OS architecture | Arm64 and Arm64: native, not Rosetta |
| runtime and renderer | .NET 10.0.12, Skia |
| display scale | 2, window 1400 by 900 |
| log location | `~/Library/Logs/GroupLab` |
| native open dialog | works, including cancel |
| the published sample, `gl-cf25-ltr-d-25-shots-600-dpi.png` | opened, identified automatically, 25 holes on 25 bulls, one to one |
| accept and save | session saved with 25 shots |
| updater | checked the nightly train, refused as not newer |
| diagnostics report | produced and saved |
| errors or warnings | none |

The updater is behaving as entry 147 intends: `UpdateRun` refuses on macOS with the manual download
message when a newer build exists. Nothing to do there.

## 2. Question one: 33 of 34 markers on the published sample

Both runs on the Mac read **33 of 34 markers**, registration RMS 0.0026 in. The detection was still
correct, but the sample is the package's self-test sheet, so its marker count should be known exactly.

Run the same file on Windows and on the Linux runner and report the marker count and RMS on each. If all
three read 33, say which marker is missed and why, most likely a hole across it, and record it with the
sample's ground truth so nobody chases it again. If Windows reads 34 and the Mac reads 33 from the same
PNG, that is a platform difference in decoding or arithmetic and it is a real defect.

## 3. Question two: detection is slower on an M5 Max than on the Windows baseline

The Mac took **5418 ms** on the first detection and **4789 ms** on the second. `docs/PERFORMANCE.md`
records 3649 ms for a 600 dpi scan on the Windows baseline. An M5 Max is not a slower machine than that
baseline, so a 30 to 50 percent slowdown is more likely to be the code than the hardware.

Check, and report which it is:

1. Whether the hole detection stages use x86 specific intrinsics with a scalar fallback on Arm64. If they
   do, the portable vector types, which map to Neon on Arm64, remove the gap.
2. Whether the macOS publish is ReadyToRun for osx-arm64, as the Windows publish is for win-x64.
3. Whether the difference is on first run only. The second run was 12 percent faster, which is
   consistent with part of it being start-up cost.

Confirm first that the 3649 ms figure is for the same file. If it is not, time the published sample on
Windows so the comparison is like for like.

## 4. A small privacy note

The log records the opened image's file name in plain text beside its path hash. The report is saved by
the user and shared by hand, so this is low risk, but a file name can carry a person's name or a place.
Decide whether the report should carry the path hash only, and say what you decided and why.

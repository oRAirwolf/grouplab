# The sample sheet, and the consent that lets it be here

`gl-cf25-ltr-d-25-shots-600-dpi.png` is the only shot GroupLab sheet in this repository. It ships inside the Windows package so that somebody who has never shot a GroupLab sheet can open one and see an analysis in the first minute, and it is what the package's own self-test analyses.

**No consent record, no publication.** That rule is `NOTES-FROM-PLANNING.md` entry 34 and entry 37, and it applies to Alan's own photographs exactly as it applies to a stranger's.

## Consent

| | |
|---|---|
| **Given by** | Alan Hayes, who shot the sheet, scanned it and owns the copyright in the scan |
| **Given on** | 2026-09-21 |
| **Given how** | In writing, to the planning session, and recorded in `docs/NOTES-FROM-PLANNING.md` entry 120 section 9 |
| **What was said** | "Use scan 3 as the sample scan. I dont care about my load data being shared." |
| **Covers** | This one file. Every other scan and every photograph from that range day stays private. |
| **Licence** | The repository's, GPL-3.0, as for everything else published here |

The load block written on the sheet is part of the image and is published with it: the date, the distance, the cartridge, the bullet, the powder charge, the brass, the primer and the seating depth. That is what "I dont care about my load data being shared" permits.

## The file

| | |
|---|---|
| **Original** | `3-600-dpi09202026.png`, 16,830,839 bytes, SHA-256 `93140a6a37777667d0c47d61a3a9c53e7ba9c7fb5c4e67038e3e4206ed8ededa` |
| **Published** | `gl-cf25-ltr-d-25-shots-600-dpi.png`, SHA-256 in `sample.json` beside this file |
| **Decoded pixels** | SHA-256 in `sample.json`, and identical in both files |
| **Image** | 4958 by 6458 pixels, 8 bits per channel, RGB, not interlaced, 600 dpi stated in its `pHYs` chunk |

**What was removed.** Every ancillary chunk except the resolution: one `tIME` and ten `tEXt` chunks, all of whose values were empty. The image data itself was re-encoded, which is why the file hash differs from the original's; **the decoded pixels are identical, and both hashes are recorded** so anybody can check that for themselves. There was never any location data in it: a flatbed scanner records none, and the text chunks were empty before they were dropped.

## What is on the sheet

GroupLab's own `GL-CF25-LTR-D`, the 5 by 5 load development sheet with a load block, printed by Alan, shot at 100 yards on 2026-09-20 and scanned at 600 dpi.

**Alan's own account of it, which is the truth this sample is scored against:** 25 shots, one at each of bulls 1 to 25.

**The load block is written wrongly on one line.** It reads `GM205MAR` for the primer; the primer was in fact 7.5BR, the same as the sheet shot before it. The sheet is published as it is, mistake and all, because that is what the sheet says and correcting a photograph of a thing is not what a sample is for. `NOTES-FROM-PLANNING.md` entry 120 records the correction.

## What GroupLab makes of it

Analysed at 600 dpi with no calibre named, GroupLab reads the sheet's codes, registers it from 33 of its 34 markers and finds **25 holes, one on each of the 25 bulls**, which agrees with Alan's account exactly. That is the expected result the Windows package's self-test holds it to: a sample the package cannot analyse correctly must fail the build rather than reach a tester.

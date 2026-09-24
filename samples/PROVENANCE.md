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
| **Covers** | This one file. Written before Alan's standing consent of 2026-09-24, below, which now covers the rest of his own photographs and scans too. |
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

---

# Alan's own photographs and scans: a standing consent

| | |
|---|---|
| **Given by** | Alan Hayes |
| **Given on** | 2026-09-24 |
| **Given how** | In writing, to the planning session, and recorded in `docs/NOTES-FROM-PLANNING.md` entry 171 section 6 |
| **What was said** | "Yes any of my photographs or scans can be published unless I specify one cannot." |
| **Covers** | Every photograph and scan Alan took himself, of his own targets, unless he names one as an exception. None is named yet. |
| **Does not cover** | Anything anybody else shot. Each friend's sheet keeps its own record, and **the 2026-09-16 friend scan is never published.** |
| **Licence** | The repository's, GPL-3.0 |

**What does not change.** Every published copy is rebuilt from its pixels and carries no metadata. GPS, location and timestamps are never
read, printed or logged, from any photograph, at any point. A consent to publish is not a reason to publish: an image goes where it shows
something a reader needs, and the rest stays where it is.

**Exceptions.** When Alan names a photograph or scan that cannot be published, it is listed here with the date he said so, and nothing
published from it before that date is kept on the site.

# The 2026-09-23 friend scan, and the consent that lets it be used

`Scan_20260923.png`, **shot 2026-09-23**, is a scan of a GroupLab sheet shot by a friend of Alan's. It is the sheet that exposed the
defect in `NOTES-FROM-PLANNING.md` entry 161: naming the correct calibre made the reading worse.

**It is not the 2026-09-16 friend scan.** Both are from the same friend and the two have different consent. The 2026-09-16 scan is
**never published**, and nothing in this record applies to it. Two scans from one person with different consent is exactly the case where
one gets published by mistake, so each is named here by its file name and its date, and neither by the friend's name.

## Consent

| | |
|---|---|
| **Given by** | The friend who shot the sheet, **relayed by Alan on the friend's behalf**; the friend is not named |
| **Given on** | 2026-09-24 |
| **Given how** | In writing, relayed to the planning session, and recorded in `docs/NOTES-FROM-PLANNING.md` entry 162 section 1 |
| **What was said** | "Yes he is willing to have his target used as a test fixture and yes it can be published." |
| **Covers** | `Scan_20260923.png`, shot 2026-09-23, and only that file: as a test fixture, and published, including in `samples/` and in research articles. |
| **Does not cover** | The 2026-09-16 friend scan, which is never published. |
| **Licence** | The repository's, GPL-3.0, as for everything else published here |

## The file

| | |
|---|---|
| **Original** | `Scan_20260923.png`, 55,993,456 bytes, SHA-256 `91206e022ef744344935b47a6447f06ab6da795af48a670160e2567f596d6437` |
| **Decoded pixels** | SHA-256 `8515b319cb1613083457a324914c0010b905ef5ce60db581e3393f161f2a6ed1` |
| **Image** | 5100 by 7013 pixels, 8 bits per channel, RGB, 600 dpi |
| **A published copy** | Rebuilt from those pixels with only the resolution chunk, as scan 3's was. Nothing else from the original's metadata is read, printed, logged or carried, at any point. |

**Published as a download, never committed.** Entry 171 section 6, closing request 8: the copy rebuilt from pixels is 59 MB, because
scanner noise does not compress, and a file that size in git would be carried by every clone for ever. So it is attached to the
`test-data` release of this repository, created by `ci.yml` for exactly this, and listed in `tests/test-data.json`. CI downloads it and
verifies its hash before a test reads it, with `scripts/test-data.py`.

| | |
|---|---|
| **Release** | `test-data`, https://github.com/oRAirwolf/grouplab/releases/tag/test-data |
| **Published file** | `Scan_20260923.png`, 59,215,934 bytes, SHA-256 `c2b2e595358cf5dd8d1f932da3f07eb9722ca189c300c4edcf6c68fcbc54570e` |
| **Rebuilt how** | `scripts/test-data.py rebuild`: the original's pixels, byte for byte, and its 600 dpi resolution. Nothing else. |

## What is on the sheet

GroupLab's own `GL-CF25-LTR-D`, printed with no scaling selected, which GroupLab measured as 100.3 percent. Ten shots of **6.5 Creedmoor,
0.264 in**, one per bull on **bulls 1 to 10**, which is the ground truth the tests hold it to.

| | |
|---|---|
| **Paper** | card stock |
| **Backing** | cardboard |
| **Cartridge** | 6.5 Creedmoor, 0.264 in |
| **Printing** | no scaling selected; GroupLab reported 100.3 percent |

On this paper and backing a hole measures 0.301 in across the middle, **1.14 times the bullet**, where the earlier scans on other paper
measured 0.765 to 0.949. That is entry 161's finding and entry 162's reason paper and backing are now recorded fields.


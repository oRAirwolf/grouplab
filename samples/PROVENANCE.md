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
| **Does not cover** | Anything anybody else shot, which has its own record: what Alan passes on from his friends is below. **The 2026-09-16 friend scan is never published.** |
| **Licence** | The repository's, GPL-3.0 |

**What does not change.** Every published copy is rebuilt from its pixels and carries no metadata. GPS, location and timestamps are never
read, printed or logged, from any photograph, at any point. A consent to publish is not a reason to publish: an image goes where it shows
something a reader needs, and the rest stays where it is.

**Exceptions.** When Alan names a photograph or scan that cannot be published, it is listed here with the date he said so, and nothing
published from it before that date is kept on the site.

# Store-bought target fingerprints: what is committed, and what never is

| | |
|---|---|
| **What** | `src/GroupLab.Core/StoreTargets/Fingerprints/*.glfp`, one for each of five Birchwood Casey products: the Shoot-N-C 8 in sight-in grid (34105), 6 in bullseye (34550), 8 in bullseye (34805) and 8 in crosshair (34806), and the Eze-Scorer paper bullseye (37826) |
| **Made from** | Alan's own 600 dpi scans of a blank of each, taken on 2026-09-30 for request 58 (entry 327), by `grouplab store-fingerprints build` |
| **Decided by** | `docs/NOTES-FROM-PLANNING.md` entries 332 and 340: "Fingerprints only, never a scan or image of another maker's target" |
| **Licence** | The repository's, GPL-3.0, for the fingerprints; the printing they describe is Birchwood Casey's and is not in the repository |

**What a fingerprint holds.** Local features (ORB, a 32 byte descriptor each, and its position in inches on the target), the aim points in
the same inches, and the smoothed colour of each half inch cell of the printing, three bytes a cell. Together they are 25 to 60 KB a
product. No picture of the target can be made from them: the colour layout is two points an inch, and a descriptor records which way
brightness changes around a point, not what the point looks like.

**What is never committed or published.** The scans themselves, the packet photographs taken with them, and any picture made from either,
including the trial's test pictures (entry 332) and the crops the tests make where the scans are on the computer. A target fingerprinted from a camera photograph instead (entry 344, `grouplab target-reference`) follows the same rule: its reference file holds the fingerprint, the words, the bulls and the scale source, never the photograph. Those tests return at
once anywhere else, so CI never needs them. The scans are Alan's to keep, and they stay on his computer.

# The 2026-09-26 range day: three load sheets, the aim point card and two score sheets

Alan's, from his range day of 2026-09-26 at 100 yd, passed on as `drive-download-20260927T044320Z-1-001.zip` (entries 226 and 229). Kept
whole and unchanged in `C:\Dev\grouplab-originals\range-2026-09-26\`, outside the repository; the zip stays where it was. Each is a
4958 by 6458 scan at 600 dpi.

| File | SHA-256 | What it is | Consent |
|---|---|---|---|
| `6.arc.dominus.k09262026.png` | `bffda24820bf6c3bd527adf22cea2acc7b5aed56e3c50b5ac457ef52c1630fc5` | GL-CF25-LTR-D, 25 shots of 6 ARC, Thunder Beast Dominus K, shot first; serial box "K" | Alan's own, entry 171 |
| `6.arc.magnus09262026.png` | `7ace1d9608cf97147dd103361d642392c90e4cefd29278a705c0905277d710cf` | GL-CF25-LTR-D, 25 shots of 6 ARC, Magnus S, 20 to 30 minutes later; serial box "M" | Alan's own, entry 171 |
| `6.5.creedmoor09262026.png` | `d86cda8d10b57bed4ad8e070354ebe3defae04cad914eb2c85ef0f38ea2b80c1` | GL-CF25-LTR-D, 25 shots of 6.5 Creedmoor, Magnus S; serial box "C" | Alan's own, entry 171 |
| `aim test09262026.png` | `b246d99b51e80fe464cf861ec3ba50e404a32b923893f202a00765f59eb4c017` | The aim point test card with Alan's shots | Alan's own, entry 171 |
| `aim.test.alan09262026.png` | `b41d9f16e7be35501f472e8e882df0a32a2f5fef3679ebf066630d86fe900f5e` | Alan's score sheet | Alan's own, entry 171 |
| `aim.test.justin09262026.png` | `5432e2cfe2fd49bf451eb079eb531f1e7f0026c90d8c4561885e135e64a00eed` | Justin's score sheet | Entry 190, through Alan; credited as "Justin" only (entry 230) |

**The load blocks**, as written on the sheets and read from the scans:

| Sheet | Load | Notes field |
|---|---|---|
| Both 6 ARC | 105 gr Aeromatch, 24.2 gr N135, Starline brass, GM205MAR, 2.250 in | "Dominus K" and "Magnus S" |
| 6.5 Creedmoor | 153.5 gr LRHT, 42.4 gr H4350, Alpha SRP brass, GM205MAR, 2.873 in | "Magnus S" |

**Backer:** OSB, for all three load sheets (entry 226 section 2.3), not the corrugated plastic of earlier range days. None was
photographed on the backer.

**A hole cut from the aim point card** (entry 254, which names the file): `tests/GroupLab.Core.Tests/Fixtures/real-hole-in-black-2026-09-26.png`,
a 256 by 256 square at 600 dpi around the shot through the black of the card's C diamond, just above its white centre, with every pixel
that is not the hole set to 0, so only the hole's own greys are kept (its bright core 0.194 in across). Cut from `aim test09262026.png`
above; Alan's own, entry 171. It is set into rendered sheets by `ZeroGridC3Tests.ARealHoleInSolidBlackIsFound`.

**Published so far:** the research article "Did the suppressor move the point of impact?" publishes the offsets GroupLab measured on the
two 6 ARC sheets and a chart drawn from them. The 6.5 Creedmoor scan is on the `test-data` release as a test fixture (entry 229 section 4),
`load-sheet-6.5-wrong-bull-2026-09-26.png`, 14,797,501 bytes, SHA-256 `1f43bab71b79c3005e4213a1e4b9b9eb1f015459a982fdbf4a2d2878fdc985fe`,
rebuilt from its pixels with only the resolution kept. The two 6 ARC scans are there too (entry 243 section 1.3), for the Compare loads
screenshot: `load-sheet-6arc-dominus-k-2026-09-26.png`, 15,775,852 bytes, SHA-256
`d1fdd053251649a0e2929a7ed1d83a5043036bd658e9995e114ea4cb74be4d9d`, and `load-sheet-6arc-magnus-m-2026-09-26.png`, 14,882,203 bytes,
SHA-256 `83f5335c2b71588b5f8f19eb28075f78be8e520030e564a4edbad3649f293bc3`. Anything published from these files is rebuilt from its pixels,
and GPS, location and time metadata are never read, printed or logged.

**The scanner** (entry 235): a Brother MFC-J430W, a Letter size flatbed, so a commercial target larger than Letter is photographed rather
than scanned.

**Photographs of the same sheets** (entry 233), taken on 2026-09-26 on a kitchen counter with the Galaxy Z Fold 7's own camera app at its
default lens and settings, 4000 by 3000. The originals are kept unchanged in `C:\Dev\grouplab-originals\range-2026-09-26\photos\`,
outside the repository, and their metadata was never read beyond the orientation flag. What is published is each photograph's pixels,
turned upright by that flag and written again as a JPEG with no metadata of any kind, renamed, on the `test-data` release; local copies
are in `photos-clean\` beside the originals. Alan's own photographs, under entry 171.

| File | Bytes | SHA-256 | What it is |
|---|---|---|---|
| `photo-6arc-dominus-k-2026-09-26.jpg` | 3,250,152 | `15e89efbc9a8ae811a89d7f9e74376f3f063f1ee20f11fcd49abe7eea8852ed5` | The Dominus K sheet, portrait, nearly straight down |
| `photo-6arc-magnus-m-2026-09-26.jpg` | 3,223,976 | `57f6f7ee7e77ff469936d169a2f6ce4c040f3bc22b91547fe9bca2f584e8835c` | The Magnus S 6 ARC sheet, turned a quarter in the frame |
| `photo-6.5-magnus-c-2026-09-26.jpg` | 3,188,780 | `3f6ff3b15e8098744d6cf9d7b5f6e55f48b7fa1e869fa292d6715eff22a8458d` | The 6.5 Creedmoor sheet, portrait |
| `photo-aim-card-2026-09-26.jpg` | 2,880,132 | `4f9d5adc54e3ca571be400f1b6357ac0825900597d3f50b06d1df8b53c95a382` | The aim point card, turned a quarter; it has no markers |

**Angled photographs of the Dominus K sheet** (entry 238), 2026-09-27, nineteen, on Alan's desk in dim room light with the Fold 7's own
camera, from straight down to about 66 degrees. Kept unchanged in `C:\Dev\grouplab-originals\range-2026-09-26\photos-angled\`, outside
the repository. None is published: only what GroupLab measured on each (the angle, markers, holes found, distances from the scan) is, in
`website/research/curled-angled-paper/data/angled-2026-09-27.csv`, under Alan's standing consent of entry 171. Their metadata was read
for the camera's focal length alone, which the angle needs.

# What Alan passes on from Unholy and his other friends: a standing consent

| | |
|---|---|
| **Given by** | Alan Hayes, for the friends whose targets and feedback he passes on |
| **Given on** | 2026-09-24 |
| **Given how** | In writing, to the planning session, and recorded in `docs/NOTES-FROM-PLANNING.md` entry 190 |
| **What was said** | "Anything from Unholy/TNA (same person) or another friend can be used for testing or publication unless I specify otherwise." |
| **Covers** | Photographs, scans, sessions and feedback that come through Alan from Unholy, who is also TNA, or from another friend of his: for testing, and published, including as a test fixture, the same as Alan's own. |
| **Does not cover** | Anything Alan names as an exception. **The 2026-09-16 friend scan is never published**, and this consent does not change that. A submission a stranger sends through the upload page or the application, which is governed by the consent level its sender chose. |
| **Licence** | The repository's, GPL-3.0 |

**What does not change.** Everything that arrives is untrusted data until the intake worker or the pull has handled it. GPS, location and
time metadata are never read, printed or logged. Unholy is credited by that name.

# The 2026-09-24 zeroing grid scan by Unholy

**Not `Scan_20260923.png`.** Alan passed it on as `Scan_20260923 (2).png`, a name one character from the scan below, and it is a
different sheet: GroupLab's "Zeroing Grid, mil at 100 yd", printed at actual size on Letter, scanned at 600 dpi, with one shot in it.
So it is named here, in the repository and on the test data release by what it is, and each file by its hash:

| | |
|---|---|
| **Consent** | Entry 190's standing consent, above: may be used for testing and published, including as a test fixture |
| **Original** | kept as `C:\Dev\grouplab-submissions\unholy\2026-09-24_zeroing-grid-mil-100yd.png`, 54,481,600 bytes, SHA-256 `367e55e5c2369f1ac578894bb82139753ba1a572b9db8bff1589740119ac7d73`, identical to what Alan passed on |
| **Published copy** | `zeroing-grid-mil-100yd-unholy-2026-09-24.png`, 57,523,449 bytes, SHA-256 `c6db8580f59412b5152f6d8520c2e9143c174ce397606095cbb4c33e51367b27`, rebuilt from its pixels with only the resolution kept, for the test data release |
| **Not to be confused with** | `Scan_20260923.png`: original SHA-256 `91206e022ef744344935b47a6447f06ab6da795af48a670160e2567f596d6437`, published copy `c2b2e595358cf5dd8d1f932da3f07eb9722ca189c300c4edcf6c68fcbc54570e` |

Nothing from the original's metadata was read, printed or logged: it was decoded to pixels and written again before anything else
looked at it.

# The 2026-09-23 scan by Unholy, and the consent that lets it be used

`Scan_20260923.png`, **shot 2026-09-23**, is a scan of a GroupLab sheet shot by Unholy, a friend of Alan's. It is the sheet that exposed the
defect in `NOTES-FROM-PLANNING.md` entry 161: naming the correct calibre made the reading worse.

**It is not the 2026-09-16 friend scan.** Both are from Unholy and the two have different consent. The 2026-09-16 scan is
**never published**, and nothing in this record applies to it. Two scans from one person with different consent is exactly the case where
one gets published by mistake, so each is always named by its file name and its date, never by his name alone. Entry 190's standing
consent, above, does not reach the 2026-09-16 scan.

## Consent

| | |
|---|---|
| **Given by** | Unholy, who shot the sheet, **relayed by Alan on his behalf** |
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


# Submission 36d3e498, 2026-10-03: two .22 LR sheets photographed on a board

Two phone photographs sent through the upload page on 2026-10-03, and passed on by Alan with the shooter's report (entry 354). The sender's
name is not recorded here or anywhere in the repository; the submission is named by its identifier.

## Consent

| | |
|---|---|
| **Given by** | The sender, on the upload page |
| **Given on** | 2026-10-03, 02:16 UTC |
| **Level** | `publishable`, consent version `consent_v2` |
| **What was agreed** | "I took these photos, or I have permission to share them. GroupLab may use them to test and improve its detection, and I understand they may be published as part of GroupLab's public test data on GitHub and in its research articles, under the GPL-3.0 license, for anyone to download and use. GPS location data is removed from every photo before anything is published." |
| **Covers** | The two photographs below: for testing, and published on the `test-data` release and in research articles |
| **Licence** | GPL-3.0 |

## The files

The server keeps each upload rebuilt as a PNG, 4284 by 5712. Nothing from their metadata was read, printed or logged: each was decoded to
pixels and written again as a JPEG at quality 95 with no metadata of any kind. The phone's focal length, which a photograph's registration
needs, is taken from the submission's own record, where the server wrote it, and given to the tests in their fixture instead.

| Published file | Bytes | SHA-256 | From | What it is |
|---|---|---|---|---|
| `photo-22lr-load-sheet-2026-10-03.jpg` | 5,809,550 | `5435d397065c2fc20db1338521e2a336bca8647f9cf095fe70c6aebf2fb17b0b` | `002_IMG_3819.png`, SHA-256 `395a92f65fab45012cf25b63c7a6b95885500b74c24f2a39be0469b5616b93de` | GroupLab's 5x5 load development sheet on Letter, `GL-CF25-LTR`, 25 shots of .22 LR at 50 yd, stapled to corrugated plastic |
| `photo-22lr-diamond-2026-10-03.jpg` | 5,205,022 | `99bdaf388fb3aecdc735fcf7995aa736ee155a91fd6360ca1b71e9a8c8ee6630` | `001_IMG_3817.png`, SHA-256 `0e1942a0916c32d9193909e04868584a65ad82885e86418f87e7661206817abc` | A sheet the target generator made, 50 yd, 6x, diamond, `GL-MBTW-2V2M-JTPE-4518`, 25 shots of .22 LR at 50 yd |

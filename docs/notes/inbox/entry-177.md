# 2026-09-24, entry 177: the first real submissions arrived, and two defects on the way

After entries 174 to 176's server fixes (entry 175's wider live check, the temporary 3 GB drop-in, and
`python3-pil` installed with apt), Alan's desktop test image and a phone photograph went all the way
through: accepted by the page, rebuilt by the worker, moved to `ready`, and pulled to
`C:\Dev\grouplab-submissions\` by `Get-TargetSubmissions.ps1`. **The web path works end to end.** Do this
entry right after entry 176.

The planning session checked both pulled files:

| submission | stored file | SHA-256 against meta.json | EXIF in the rebuilt PNG |
|---|---|---|---|
| `2026-09-24_58d94b23` | `001_20260920_185956.png`, 6.3 MB | matches | whitelist only: Make, Model, Orientation, ExposureTime, FNumber, ISO, FocalLength, FocalLength35, DigitalZoomRatio. No GPS, no timestamps. |
| `2026-09-24_c80e45a7` | `001_grouplab-e2e-test-173-rebuilt.png`, 64 KB | matches | none |

So the privacy promise holds: the EXIF block is freshly built from the whitelist, as entry 129 section
3.5.2a intended, and nothing about location or time survives.

## 1. The pull script crashes after downloading

    pulling 2026-09-24_58d94b23  ok, 6.3 MB
    pulling 2026-09-24_c80e45a7  ok, 0.1 MB
    Get-TargetSubmissions.ps1 : You cannot call a method on a null-valued expression.

**Cause:** the worker rewrites `meta.json`'s `files` list with the keys `stored`, `sha256`,
`originalSha256` and `facts`. The pull script reads `$f.stored_name`, the old receiver's key. That is
null, so `Join-Path` yields the submission's own folder, `Test-Path` succeeds on the folder,
`Get-FileHash` returns nothing for a folder, and `.Hash.ToLower()` is called on null.

1. Read `stored`, falling back to `stored_name` for the old pissinhot.com submissions.
2. A folder must never pass as a file: check `Test-Path -PathType Leaf`.
3. **A contract test**: the worker's `meta.json` output, from a real worker run, is parsed by the pull
   script's own verification and passes. The two were written separately and nothing held them
   together; that is the same gap as entry 174's field name.
4. The crash stopped the script before it updated the ledger, so run the verification and the ledger
   step again for these two submissions and confirm both are recorded.

## 2. The rebuilt photograph is upright and still says it needs rotating

The worker straightens the pixels with `ImageOps.exif_transpose` and then **copies the original
Orientation value into the new EXIF block**. The phone photograph is stored upright, 3000 by 4000, with
`Orientation = 6`. Anything that honors EXIF orientation, GroupLab included, will rotate it a second time
and show it on its side.

1. When the pixels have been transposed, write `Orientation = 1`, or leave the tag out. Never copy the
   original value onto transposed pixels.
2. Check `ImageScrubber` in the application, which is the authority for the whitelist, for the same fault,
   and fix both together so `WhitelistTests` keeps them in step.
3. Test with a portrait phone JPEG carrying `Orientation = 6`: the rebuilt file must display upright in
   GroupLab and in an ordinary image viewer.
4. Fix the one already pulled by rewriting its Orientation to 1 locally, keeping its pixels and its other
   tags, and record the change beside it.

## 3. What these two submissions are

1. `2026-09-24_c80e45a7` is the entry 173 test image. Its opt out box was not ticked this time, so its
   record says it may be published. **It is a test and must never be published**: mark it as such in the
   ledger and exclude it.
2. `2026-09-24_58d94b23` is Alan's own phone photograph of the 2026-09-20 ST-4 target, the same frame as
   `20260920_185956.jpg` in the range folder. His standing consent covers it. Do not add it to the public
   data set as a second copy of a frame that is already there.

## 4. Where entry 173 stands

Section 1.3's end to end test is **done**, from a desktop browser and from a phone, which is what section
1.4 asked for. What remains is section 2.4, the `pissinhot.com/targets` redirect, which is Alan's to run
from request 1, and one last pull from pissinhot.com afterwards. Close request 1's steps 1 to 3.

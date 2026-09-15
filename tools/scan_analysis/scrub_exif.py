#!/usr/bin/env python3
"""
Strip location and identifying metadata from donated target photographs, keeping
only the camera facts the registration work needs.

THE C# SCRUBBER IS AUTHORITATIVE.  src/GroupLab.Core/Publication/ImageScrubber.cs
is the definition, and it is what `grouplab intake` and the history rewrite of
NOTES-FROM-PLANNING entry 29 use.  This script is for ad hoc use only.  It keeps
the same tags, but unlike the C# version it leaves XMP and any data after the
image's end marker in place, and either can carry a location or a date, so its
output must not be published without `grouplab intake` or PublicationTests
checking it.  Keep the two whitelists identical.

WHY THIS EXISTS.  Photographs contributed by other people may carry GPS
coordinates of a shooting range or of private property, device serial numbers,
and owner names.  None of that belongs in a public repository, and once it is
committed it is in the history whether or not a later commit removes it.  The
project has already had to rewrite its git history once, to purge another
company's files from `reference/`, so the cost of getting this wrong is known.

WHAT IS KEPT.  Only the tags the pipeline reads or the analysis needs:

    Make, Model                      which phone, so frames can be grouped
    FocalLength                      the physical lens
    FocalLengthIn35mmFilm            the effective field of view.  Both are
                                     needed: NOTES-FROM-PLANNING entry 16 found
                                     that a cropped ultrawide keeps the physical
                                     focal length and changes only the
                                     equivalent, and that grouping frames by the
                                     physical value alone mixes two pixel
                                     geometries into one joint fit and crashes it
    FNumber                          depth of field, which drives far-edge
                                     marker defocus
    LensModel                        which camera in a multi-camera phone took
                                     the frame.  The same 35 mm equivalent can
                                     come from different physical lenses in
                                     different modes, and focal length alone
                                     cannot tell them apart
                                     (NOTES-FROM-PLANNING entry 48)
    ExposureTime, ISOSpeedRatings    motion blur and noise
    Orientation                      or the image loads rotated
    DigitalZoomRatio                 digital zoom crops and upscales without
                                     always updating the 35 mm equivalent, so
                                     it is part of the lens grouping key
                                     (NOTES-FROM-PLANNING entries 27 and 29)

WHAT IS REMOVED.  Everything else, which is every GPS field, every maker note,
every serial number, every embedded thumbnail, and every date.  Dates are
dropped because a timestamp plus a target photograph plus a public repository is
more than anyone donating a picture of their range agreed to.

The JPEG itself is not re-encoded.  Only the metadata block is rewritten, so the
pixels are bit-identical and nothing is lost to a second compression pass.

Usage:
    python3 scrub_exif.py IN_DIR OUT_DIR          scrub every image
    python3 scrub_exif.py IN_DIR OUT_DIR --report only say what is there
"""
import os
import shutil
import sys

import piexif

KEEP_0TH = {piexif.ImageIFD.Make, piexif.ImageIFD.Model, piexif.ImageIFD.Orientation}
KEEP_EXIF = {
    piexif.ExifIFD.FocalLength,
    piexif.ExifIFD.FocalLengthIn35mmFilm,
    piexif.ExifIFD.FNumber,
    piexif.ExifIFD.ExposureTime,
    piexif.ExifIFD.ISOSpeedRatings,
    piexif.ExifIFD.PixelXDimension,
    piexif.ExifIFD.PixelYDimension,
    piexif.ExifIFD.DigitalZoomRatio,
    piexif.ExifIFD.LensModel,
}

EXTS = (".jpg", ".jpeg", ".JPG", ".JPEG")


def describe(path):
    """What is in this file, before anything is touched."""
    try:
        ex = piexif.load(path)
    except Exception as e:
        return None, "unreadable metadata (%s)" % e
    gps = len(ex.get("GPS") or {})
    note = []
    if gps:
        note.append("GPS (%d fields)" % gps)
    if ex.get("thumbnail"):
        note.append("embedded thumbnail")
    zeroth = ex.get("0th") or {}
    for tag, label in ((piexif.ImageIFD.Artist, "Artist"),
                       (piexif.ImageIFD.Copyright, "Copyright"),
                       (piexif.ImageIFD.XPAuthor, "XPAuthor")):
        if tag in zeroth:
            note.append(label)
    if (ex.get("Exif") or {}).get(piexif.ExifIFD.BodySerialNumber):
        note.append("body serial")
    return ex, ", ".join(note) if note else ""


def scrub(src, dst):
    ex, found = describe(src)
    if ex is None:
        # No readable metadata block. Copy as is; there is nothing to strip.
        shutil.copy2(src, dst)
        return found, {}

    kept = {}
    zeroth = {t: v for t, v in (ex.get("0th") or {}).items() if t in KEEP_0TH}
    exif = {t: v for t, v in (ex.get("Exif") or {}).items() if t in KEEP_EXIF}
    for t, v in zeroth.items():
        kept[piexif.TAGS["Image"][t]["name"]] = v
    for t, v in exif.items():
        kept[piexif.TAGS["Exif"][t]["name"]] = v

    clean = {"0th": zeroth, "Exif": exif, "GPS": {}, "1st": {}, "thumbnail": None}
    shutil.copy2(src, dst)
    try:
        piexif.insert(piexif.dump(clean), dst)
    except Exception as e:
        # Better to ship a file with NO metadata than one that still has GPS.
        try:
            piexif.remove(dst)
            return found, {"_all_metadata_removed": str(e)}
        except Exception:
            os.remove(dst)
            raise
    return found, kept


def main():
    if len(sys.argv) < 3:
        print(__doc__)
        sys.exit(2)
    src_dir, out_dir = sys.argv[1], sys.argv[2]
    report_only = "--report" in sys.argv
    if not report_only:
        os.makedirs(out_dir, exist_ok=True)

    files = sorted(f for f in os.listdir(src_dir) if f.endswith(EXTS))
    n_gps = 0
    for f in files:
        src = os.path.join(src_dir, f)
        if report_only:
            _, found = describe(src)
            print("%-44s %s" % (f[:43], found or "clean"))
            n_gps += "GPS" in found
            continue
        found, kept = scrub(src, os.path.join(out_dir, f))
        n_gps += "GPS" in found
        focal = kept.get("FocalLength")
        f35 = kept.get("FocalLengthIn35mmFilm")
        print("%-44s removed[%s]  kept %s %s/%s" % (
            f[:43], found or "nothing",
            str(kept.get("Model", "?"))[:18],
            "%s" % (focal,), "%s" % (f35,)))

    print("\n%d file%s, %d carried GPS." % (len(files), "" if len(files) == 1 else "s", n_gps))
    if not report_only:
        print("Scrubbed copies in %s. Check a couple with --report before "
              "committing anything." % out_dir)


if __name__ == "__main__":
    main()

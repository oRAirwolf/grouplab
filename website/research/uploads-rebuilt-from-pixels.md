---
title: Every upload is rebuilt from pixels
description: A photograph you send is stripped of everything except the camera and the exposure. Not edited, not blurred: the metadata around the image is thrown away and written fresh, and the pixels are copied untouched.
group: How GroupLab is built
number: 7
written: 2026-09-22
data_date: 2026-09-22
samples: not a measurement: an account of the code, with the rule it follows and what it keeps
status: published
found: The keep list is nine fields about the camera and the exposure. Everything else goes, including every GPS field, every date, every serial number, every maker note, and whatever a phone appended after the end of the image.
sure: This describes the code and the rule it implements. It is not a guarantee about any other software that has touched your file before it reached GroupLab.
sources:
  - "The keep list and the rule behind it: `src/GroupLab.Core/Publication/ImageScrubber.cs`."
  - "Where the rule came from: `docs/NOTES-FROM-PLANNING.md`, entry 22 section 2 and entry 48 section 3."
---

## What is in a photograph besides the photograph

A picture from a phone carries a great deal that is not the picture. Where it was taken, to a few metres. When, to the second. Which phone, by serial number. What the owner called the device. Sometimes a thumbnail that survives edits to the main image. Sometimes several seconds of video, appended after the end of the image data, because the phone takes a motion photo and hides it there.

A target photograph is usually taken where somebody shoots. That is not information this project wants to hold.

## What GroupLab does with it

Before a photograph is published, the metadata is **thrown away and rebuilt**. Not filtered in place: a fresh block is written containing only what is on the keep list, and the rest never makes it into the new file.

The compressed image data is copied **byte for byte**. Nothing is re-encoded, so the picture is not degraded by a second round of JPEG compression, and nothing is blurred or painted over.

## The keep list, in full

Kept, because they describe the camera and the exposure and are what makes a photograph useful as evidence about photographs:

- Make, Model, Orientation
- Exposure time, f-number, ISO
- Focal length and its 35 mm equivalent
- Pixel dimensions
- Digital zoom ratio
- Lens model

Removed, which is everything else:

- **Every GPS field.** Latitude, longitude, altitude, bearing, the lot.
- **Every date and time.**
- **Maker notes and serial numbers**, which on some cameras identify the individual body.
- **XMP**, which can carry its own copy of the location and dates.
- **IPTC** and every other application segment except the basic header and a colour profile.
- **Comments**, and anything a person typed.
- **The embedded thumbnail**, which can outlive edits to the main image.
- **Anything after the image's end marker**, which is where motion-photo video lives.

## Why a keep list and not a remove list

This is the part worth copying if you are building something similar.

A **remove list** is a promise you have to keep updating. Every new camera, every new phone, every revision of a metadata standard can add a field, and your list does not know about it. The field arrives, your code does not recognise it, and it passes straight through. You find out when somebody notices their address in a published file.

A **keep list** fails the other way. A field nobody has heard of is not on the list, so it is dropped. The cost of being wrong is losing something mildly useful. The cost of the other design being wrong is publishing where somebody lives.

The rule is written down so the next argument is decided rather than negotiated: *keep what describes the camera and the exposure; drop everything that describes where, when, who, or anything a person typed.*

## The file that cannot be parsed

If the metadata cannot be read at all, the file loses all of it.

That is deliberate and it is the same reasoning again. A parser that has given up is a parser that does not know what it is holding, and the safe move when you do not know is to keep nothing. Better no metadata than GPS.

## What this does not protect you from

**It is not a promise about the file you send.** GroupLab strips what it publishes. If you have already posted the same photograph elsewhere, this does nothing about that copy.

**Location is never read in the first place.** Separately from all of the above, no part of GroupLab reads GPS or location values from any photograph at any point, including the diagnostic log, which has its own whitelist following the same rule. The stripping is for what gets published; the not-reading is for everything else.

**A photograph can still identify a place.** A recognisable backdrop is a recognisable backdrop, and no metadata rule touches pixels. If where you shoot is sensitive, photograph the sheet and not the view.

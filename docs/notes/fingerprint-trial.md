# Fingerprint trial for store-bought targets (entry 332)

2026-10-01. A trial only: nothing from it ships in the application. Alan asked whether GroupLab could keep a fingerprint of a store-bought
target so that a scan or a photograph of one is recognized and its scale remembered. This measures whether that works on the five
Birchwood Casey blanks Alan scanned at 600 dpi (request 58, entry 327).

**Recommendation for planning to take to Alan: ship recognition without the scale, not the scale.** Recognition is safe on everything
measured here: no false match on 123 pictures that were not one of the five products, including 81 real photographs and scans and a
different maker's commercial target, and no wrong product in 200. The scale from a fingerprint is accurate when the product is the one
fingerprinted (median 0.06 percent), but two of the five products are, in effect, the same artwork at two sizes, and a crop that hides the
few parts that do not scale can match the wrong one at the wrong scale. Until the library holds whole families and the print-to-print
spread is measured (request 64), GroupLab should recognize the product, place the bulls, and still ask for a scale check.

## What was built

Everything is in `grouplab fingerprint-trial` (`src/GroupLab.Cli/Spike/FingerprintTrial.cs`), a command-line trial no screen reaches:
`build` makes the fingerprints, `make` writes the test pictures with the true transform of each, `match` runs recognition and
registration and prints every number below. The blanks, the test pictures and the fingerprints stay on this computer, because they are
another maker's printing; nothing derived from them is committed.

A fingerprint holds:

1. **Local features: ORB**, at 100 and at 50 points an inch of the target, the strongest at most a share of them in each half inch, with
   a slight blur and a strength floor so the scanner's dust is left out. Positions are kept in inches on the target.
2. **The bull centers** in the same inches: red marks ringed by black ink at least an inch inside the printing (the five diamonds of the
   sight-in grid, the one center of each of the others; the repair pasters in the corners are left out).
3. **A color layout**: the smoothed color of each half inch cell of the printing, three bytes a cell (a 16 by 21 cell sheet is 1 KB).
4. **A color signature** of fourteen numbers for shortlisting.

Matching a picture: the phone's 8 megapixel working copy, features found at 3000 pixels on the long side, then for each fingerprint two
starts (ratio-tested matches, and the three nearest matches of each feature for patterns the target repeats), a robust homography from
target inches to picture pixels, every fingerprint feature looked for where that fit puts it, and the fit made again from what was
found. **The decision:** of the products with at least 25 features agreeing with the fit, the one whose color layout, read through the
fit, correlates best with the picture, claimed only if that correlation is at least 0.85 and ahead of the next product by 0.1.

### ORB or AKAZE

ORB. Both were built and measured on the same pictures. AKAZE found more of the true products at low thresholds, but under the same
decision rule, both finding features at 2000 pixels, it identified fewer (152 of 200 against ORB's 157), claimed one wrong product,
and claimed a product for 14 of 200 pictures when that picture's own product was taken out of the library, against 1 for ORB (none for
ORB at 3000 pixels). Its features also took about ten times as long to find
(213 ms against 20 ms a picture at 2000 pixels). Its fingerprints were larger: 57 to 121 KB.

## Sizes

ORB fingerprints, features at 100 and 50 points an inch, positions as two 32-bit numbers, 32-byte descriptors, the layout and the bulls:

| product | features | size | compressed |
|---|---|---|---|
| Shoot-N-C sight-in grid, BC-34105 | 2115 | 83.5 KB | 57.6 KB |
| Shoot-N-C 6 in bull, BC-34550 | 994 | 39.4 KB | 33.3 KB |
| Shoot-N-C 8 in bull, BC-34805 | 830 | 33.3 KB | 24.6 KB |
| Shoot-N-C 8 in crosshair, BC-34806 | 957 | 38.5 KB | 32.3 KB |
| Eze-Scorer bull, 37826 | 1692 | 67.2 KB | 60.2 KB |

All well under the 200 KB aim. Positions in 16 bits would save another 4 bytes a feature.

## The test pictures

From each blank: 30 phone views and 10 flatbed scans, 200 in all, made from the 600 dpi scan with the true transform kept for each.

- **Phone views:** a 4000 by 3000 picture with a lens like a phone's main camera, 1 to 3 ft away, tilted up to 37 degrees in any
  direction, turned any way (half within 20 degrees of upright), on a plaster, plywood, cardboard or brick background; uneven light from
  0.45 to 1, a color cast, blur up to 2.5 pixels, sensor noise, JPEG quality 60 to 92; 30 percent cut to half the target.
- **Flatbed scans:** 150, 300 or 600 dpi, the sheet anywhere on a letter glass at any angle, part of it off the glass.
- **Shot holes** on three quarters of the phone views and half the scans: one to three groups of 3 to 10 ragged holes, .22 to .35 in,
  around the bulls; on the Shoot-N-C targets a chartreuse halo where a hole struck black ink, and a repair paster, black or chartreuse,
  over about one hole in five.

Not one of the five products, 123 pictures: the GroupLab sample sheet (10 phone views and 2 scans made the same way), 30 blank walls of
the same textures, and 81 real pictures already on this computer: the 60 photographs from the 2026-09-20 range day (GroupLab sheets and the
National Target Company ST-4, a commercial sight-in target much like the Shoot-N-C grid), the 7 corpus photographs, the 5 camera-0929
pictures and the 9 scale-test photographs and scans. Unholy's range screenshot was not used: the only screenshots of his on this
computer are TestFlight feedback, which were not opened for this. No range photograph on this computer shows any of the five products.

## Recognition

ORB, features at 3000 pixels, the decision above:

| | result |
|---|---|
| phone views identified | 121 of 150 (81 percent) |
| flatbed scans identified | 42 of 50 (84 percent) |
| wrong product claimed | 0 of 200 |
| claims on pictures of none of the five | 0 of 123 (sheet 0 of 12, wall 0 of 30, real 0 of 81) |
| claims when the picture's own product is taken out of the library | 0 of 200 |

**The threshold.** The layout correlation of the true product, once found, is mostly 0.95 to 1.00. The highest seen on any picture of
none of the five was 0.21; the highest on a wrong product was 0.81. So 0.85 sits above every wrong match seen. At 0.80 four more phone
views are identified and 2 of 200 pictures are claimed as another product when their own is absent; at 0.90 eight scans are lost.

**Without the color layout** (features alone, at any threshold from 15 to 40 features), 3 to 7 of the 200 were claimed as the wrong
product, 2 to 6 of the 81 real pictures and up to 2 of the 12 GroupLab sheets were claimed as a store-bought target, and with the true
product absent a wrong one was claimed for 111 to 138 of the 200. Features alone are not safe: the five products share a black disc on chartreuse, repair pasters,
the maker's words and ring numbers in the same type.

**What is missed.** The sight-in grid 24 of its 40 (shot holes with halos on an almost all black sheet move the few cells that differ,
and half the sheet is close to symmetric), the 8 in bull 11 of 40 (from 2 to 3 ft, or cut in half, its features are mostly its rings),
the 6 in bull 2, the crosshair and the Eze-Scorer none. A miss is the safe outcome: GroupLab would ask, as it does today.

**The family problem.** The 6 in and 8 in bulls are close to the same artwork at two sizes. With AKAZE, the 6 in fingerprint fitted to an
8 in picture (scaled by a third) reached a layout correlation of 0.98. ORB did not produce such a fit on these pictures, but that is
the features' luck, not a guarantee. Birchwood Casey sells the same bull at more sizes than the two here; a picture of a 12 in bull, or
an 8 in bull cut so that its corners and words are out of frame, could be taken for one in the library at the wrong scale.

**The shortlist.** The color signature put the true product first in 72 of 200 and in the first three in 153: no use as a shortlist.
With five products every fingerprint is tried; a library of more than a few dozen would need an index over all the fingerprints' features.

## Registration

For the pictures identified correctly: each bull in view, taken from where it truly is in the picture back through the fitted homography
to the target, against where the fingerprint has it; and the scale, as the length the fit gives to two inches about the bull.

| | bull error, median / 95th / largest | scale error, median / 95th / largest |
|---|---|---|
| phone views (143 bulls) | 0.007 / 0.023 / 0.086 in | 0.06 / 0.45 / 5.24 percent |
| flatbed scans (53 bulls) | 0.010 / 0.017 / 0.062 in | 0.05 / 0.74 / 1.61 percent |

One identified sight-in picture (cut to half) was fitted turned by the sheet's own symmetry: its scale was right (0.54 percent) and its
bulls were each given another bull's place, 6.84 in away. The bull error above leaves that one out. The scans' scale would improve with a
fit limited to turning and scaling, since a flatbed has no perspective; that was not tried.

**What this does not measure:** every picture was made from the same sheet the fingerprint came from. The difference between one printed
sheet and the next, which is what a remembered scale really depends on, is not in these numbers. That is request 64.

## Time and memory

On this desktop's processor (AMD Ryzen 7 9800X3D), features at 3000 pixels, medians over 323 pictures:

- decoding the picture and making the 8 megapixel working copy: 28 ms
- features and the color layout: 37 ms
- matching against five fingerprints: 509 ms (95th percentile 900 ms), about 100 ms a fingerprint
- memory: the whole process peaked at 275 MB matching one 12 megapixel phone picture (26 MB at start), 432 MB over all 323

**The phone, through the replay path:** not measured on a phone. The phone runs the same OpenCV through the same code, and the replay path
(entry 315) feeds it a picture, so the figures above are what the same work costs on this desktop's processor, not on a phone; a phone
is slower by a factor this trial did not measure. At about 100 ms a fingerprint on the desktop, a library much past five needs the index
before it goes near a phone.

## Print consistency: what is still unknown

The scale from a fingerprint is only as good as the press. Request 64 asks Alan for a second sheet of each of the five, scanned at 600 dpi
in the same corner of the glass, from a second pack where he has one. When they arrive, `grouplab fingerprint-trial` measures the scale
between the two sheets of each product; a remembered scale is trusted only if they agree within about 0.2 percent, otherwise
recognition still finds the bulls but GroupLab asks for a scale check.

## Worth an article?

**Not yet.** The test: would this change what another shooter does, or what another developer builds? The developer's half, that local
features alone match the wrong store-bought target and that a coarse color layout read through the fit removes it, is worth writing,
but the result that matters to a shooter (can the remembered scale be trusted) waits on request 64, and every picture here is synthetic.
Worth one when the second sheets have been measured and a few real photographs of shot Shoot-N-C targets have been through it.

## To run it again

```
grouplab fingerprint-trial build C:\Dev\grouplab-local\commercial-targets <scratch folder>
grouplab fingerprint-trial make C:\Dev\grouplab-local\commercial-targets <scratch folder> --sheet samples\gl-cf25-ltr-d-25-shots-600-dpi.png --real <folder>...
grouplab fingerprint-trial match <scratch folder> --method orb --side 3000
```

The pictures are made from seed 332 and take about 190 MB; they and the fingerprints were deleted after the run.

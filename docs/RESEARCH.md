# Research articles

NOTES-FROM-PLANNING.md entry 142. Thirty articles for <https://grouplab.org/research/>, with who writes each and where it has got to. Updated after every commit that moves one.

## Batch 1 is ready for review, and nothing is published

The Research page, the navigation link, the article format, the build and its checks are in, with **articles 1, 3, 5 and 6 written**. Nothing is on grouplab.org: entry 142 section 4 publishes a batch only after Alan sends "publish research batch 1".

**To review it**, the pages are rendered under `docs/figures/research/`, one narrow and one desktop for each. **All eighteen of my own articles are written and rendered as of 2026-09-22**, in three batches; the twelve planning drafts are imported. Nothing is published: the whole section waits for Alan's review. The site also builds locally with `python website/build.py`, and `website/_site` can be served with `python -m http.server` from inside it.

**About those renders.** Entry 142 section 4.1 asks for phone and desktop widths. Chrome's command line screenshot on this machine does not emulate a phone: it lays the page out at about 800 pixels whatever window size it is given and then crops the picture to the size asked for, so a "390 wide" render is a crop of a wider page and shows text running off an edge that is not really there. I checked this against an existing page, `/guides/`, which crops identically. So the narrow renders are taken at 860, the width where the site's own mobile layout begins, which Chrome does honour. A true phone render needs device emulation through the DevTools protocol, which is not set up here.

## Importing a planning draft

The twelve drafts arrived in `C:\Dev\grouplab-research-drafts` on 2026-09-22, one folder each with `article.md`, its figures and the script that makes them, its data and its sources. Nothing there is written to, renamed or deleted.

Each one ends with a list of what to check against the repository before it goes in. **Article 4's three checked out exactly**: the 86 percent ammunition figure at 25 shots is `docs/STATISTICS.md` section 5's own number, the mean radius scale marks are word for word what `MeanRadiusScale.cs` holds, and the headline figure is in the logo's amber as the draft says.

**Their figure scripts need numpy, scipy and matplotlib, and this machine has no package source to install from.** The build says so by name and uses the committed figure beside the script, rather than failing: a script that needs a package that is not here is not a broken script. Anything else a script does wrong still fails the build. `_style.py`, the shared chart style the drafts import, sits at `website/research/_style.py` and the research folder is on the path when a script runs.

## What is built

- `website/research/<slug>.md`, front matter and body, one file an article.
- `website/research/<slug>/figures/*.py`, a script for every chart, run on every site build before the pages are written. `lead.svg` or `lead.png` is the index thumbnail.
- `website/research/<slug>/*.csv`, the data behind a chart, downloadable from the article.
- The build fails on an article with incomplete front matter, a data file it offers that is not there, a figure script that does not run, a link to a figure that is not built, or any published image carrying EXIF, XMP, IPTC or GPS data.
- `ResearchArticleTests` fails if an article quotes one of GroupLab's own constants that the code no longer uses, if it uses an em dash or a banned term, or if its samples line names no number.

## The thirty

| # | Slug | Title | Group | Writer | State |
|---|---|---|---|---|---|
| 1 | `photo-hole-size` | Why a photo cannot tell you your bullet's size | Reading targets | Code | **written**, batch 1 |
| 2 | `hole-is-not-the-bullet` | A bullet hole is not the bullet | Reading targets | Code | **written 2026-09-22**, batch 2, waiting with batch 1 for review |
| 3 | `primer-comparison` | Did the primer matter? A real comparison | Range tests | Code | **written**, batch 1 |
| 4 | `mean-radius-or-extreme-spread` | Mean radius or extreme spread? | Measuring groups | Planning | **imported, draft**: its three checks verified against the repository |
| 5 | `how-grouplab-reads-a-target` | How GroupLab reads a target | Reading targets | Code | **written**, batch 1 |
| 6 | `wrong-bull` | When shots land on the wrong bull | Reading targets | Code | **written**, batch 1 |
| 7 | `uploads-rebuilt-from-pixels` | Every upload is rebuilt from pixels | How GroupLab is built | Code | **written 2026-09-22**, batch 3, waiting with batches 1 and 2 for review |
| 8 | `can-you-see-the-bull` | Can you see the bull? Aim points and optics at 100 yards | Range tests | Planning | **imported, draft**: results wait for the 2026-09-23 test |
| 9 | `scans-against-photos` | Scans against phone photos: how close is close enough? | Reading targets | Code | **written 2026-09-22**, batch 3, waiting with batches 1 and 2 for review |
| 10 | `safe-updates` | How GroupLab updates itself safely | How GroupLab is built | Code | **written 2026-09-22**, batch 3, waiting with batches 1 and 2 for review |
| 11 | `how-many-shots` | How many shots do you need? | Measuring groups | Planning | **imported, draft** |
| 12 | `pooling-groups` | Pooling groups: when two sheets are one load | Measuring groups | Code | **written 2026-09-22**, batch 2, waiting with batch 1 for review |
| 13 | `cep-explained` | CEP 50 and 90 explained | Measuring groups | Planning | **imported, draft**: corrected, the roundness verdict is a test rather than an aspect threshold |
| 14 | `velocity-sd-small-samples` | Velocity SD from 5, 10 and 20 shots | Measuring groups | Planning | **imported, draft** |
| 15 | `moa-mils-inches` | MOA, mils and inches: one group four ways | Measuring groups | Planning | **imported, draft** |
| 16 | `when-to-adjust-zero` | Zeroing: when to adjust and when to leave it | Measuring groups | Planning | **imported, draft**: corrected, the zero figures are at 95 percent |
| 17 | `one-hole-or-two` | One hole or two? | Reading targets | Code | **written 2026-09-22**, batch 2, waiting with batch 1 for review |
| 18 | `curled-angled-paper` | Curled, angled and wrinkled paper | Reading targets | Code | **written 2026-09-22**, batch 2, waiting with batch 1 for review |
| 19 | `wind-or-rifle` | Wind or rifle? | Range tests | Code | **written 2026-09-22**, batch 2, waiting with batch 1 for review |
| 20 | `blank-sheet-zero` | Zeroing on a blank sheet with a hand-drawn cross | Reading targets | Code | **written 2026-09-22**, batch 2, waiting with batch 1 for review |
| 21 | `photographing-targets` | How to photograph a target so it measures well | Guides | Planning | **imported, draft**: it caught an arithmetic error of mine, see the questions file |
| 22 | `printer-true-size` | Does your printer print at true size? | Guides | Planning | **imported, draft** |
| 23 | `scanner-traps` | Scanner traps: cropping, DPI and colour | Guides | Planning | **imported, draft** |
| 24 | `choosing-the-markers` | Choosing the markers | How GroupLab is built | Code | **written 2026-09-22**, batch 3, waiting with batches 1 and 2 for review |
| 25 | `designing-a-readable-target` | Designing a target GroupLab can read | How GroupLab is built | Code | **written 2026-09-22**, batch 3, waiting with batches 1 and 2 for review |
| 26 | `what-grouplab-sends` | What GroupLab sends from your computer | How GroupLab is built | Code | **written 2026-09-22**, batch 3, waiting with batches 1 and 2 for review |
| 27 | `nightly-builds` | Nightly builds, from commit to installer | How GroupLab is built | Code | **written 2026-09-22**, batch 3, waiting with batches 1 and 2 for review |
| 28 | `smaller-installer` | Cutting the installer from 97 MB to 81 MB | How GroupLab is built | Code | **written 2026-09-22**, batch 3, waiting with batches 1 and 2 for review |
| 29 | `aim-points-by-optic-class` | Aim points for 1x to high power optics | Range tests | Planning | **imported, draft**: results wait for the class tests |
| 30 | `range-test-log` | The range test log | Range tests | Planning | **imported, draft**: the 2026-09-23 entry waits for results |

## Rules that apply to every one

- Every figure states its sample size. Nothing is called proven that the shots cannot prove.
- No em dash, no OnTarget, no barrel harmonics, no barrel time, no velocity or accuracy nodes.
- Images of Alan's sheets are re-encoded from pixels at a web size with no metadata. The orange commercial target is never shown or named, and the friend's scan waits for its consent record.
- No GPS, location or timestamp data is read, printed or published, from anything.
- Nothing in `C:\Dev\grouplab-research-drafts` is written to, renamed or deleted. Drafts are read and brought across.

## Figures come in both themes

NOTES-FROM-PLANNING.md entry 143 section 1.2. Every figure used to be drawn on a near-white surface, which glared against the site's dark theme and looked pasted on.

`_style.py` now writes two files for every figure. `save(fig, path)` draws it once, writes the light version, recolours the same figure to the dark palette, and writes it beside the first as `<name>-dark.png`. **No figure script had to change**, and recolouring rather than redrawing is what keeps the two identical in everything but colour: same artists, same data, same layout, so a figure cannot come out saying two different things in two themes.

Only the neutrals move. The three data colours, blue, orange and aqua, were chosen to sit on either background and are untouched. Anything a script coloured deliberately still means what it meant.

**Two faults the first run produced, both invisible to a build that only checks files exist:**

- Every dark figure had a black title on a black background. An axes has three title artists, one for each position, and this style puts titles on the left, so recolouring `ax.title` recoloured an empty string.
- "Chasing the zero" came out with a white cross on the chart and a black one in the key for it, because a legend's sample marks are copies made when the legend was built.

So the build looks at the picture: every figure must have a dark version, and every dark version's corner must be the dark surface. Two figures are exempt and named in `PAPER`, because they are photographs of white paper, where white is the subject rather than the theme.

## Three states, and a record of what went live

Entry 143 section 1.3. Front matter said `status: published` on articles that were not published, because the word was doing two jobs: finished, and on the site.

- `state: draft` is being written. It is built and reachable by its own address, carries a notice saying so, and is not on the index.
- `state: ready` is finished and reviewed, waiting for its batch. Same treatment, different notice.
- `state: published` is on the site.

**The third one is not a thing an article can say about itself.** `website/research/PUBLISHED.md` is the record of publishing having happened, with a date and a batch, and the build refuses both halves of a disagreement: a page claiming to be published that is not listed, and a page listed there that does not claim it. So publishing stays a decision somebody took on a day.

## Worth an article? The standing rule, and what was decided

NOTES-FROM-PLANNING.md entry 158 section 1. After any research, measurement or investigation, decide whether it is worth an article and
record the decision either way. **The test: would this change what another shooter does, or what another developer builds?** If yes, it
is written, and a negative result is no reason to skip it. The two reasons expected most often for not writing are "the data cannot
separate the effect from the confounds" and "already covered by article N".

| investigation | decision | why |
|---|---|---|
| Question 38: a photographed hole has no size constant | **already covered** | Articles 1, `photo-hole-size`, and 2, `hole-is-not-the-bullet`, are that finding. |
| Question 44: the bent-sheet model predicts a held-out marker as well as a fitted one | **not written** | It improved the bull centers on seven of seven photographs and worsened the hole positions on seven of seven, so there is no conclusion yet that a developer could build on, and the model is not adopted. Revisit if a model improves both. |
| Entry 130: photographs against scans of the same sheets | **already covered** | `scans-against-photos` is that comparison. |
| Entry 157: how far off square a photograph can be | **worth an article, not yet written** | Up to 32 degrees the holes in a photograph kept the square-on photographs' error, while the bull centers grew three times worse. That changes how a shooter holds the phone. But nothing past 35 degrees has been measured against a scan, and the article's useful sentence is where it stops working; request 18 asks for the photographs that would say. |
| Entry 157: a white sheet on a white board cannot be outlined | **not written** | A limit of GroupLab's own outline finder, with nothing a shooter would do differently beyond what the application already tells them. |
| Entry 158 program A step 2: the scan detector on overlapping holes in photographs | **not written yet**, see below | It measures a detector built for scans on a case it was never meant for; the article worth writing is program A's, which needs the holes placed. |
| Entry 158 program B: hole size against velocity and nose shape | **covered in part, and extended** | Step 1 is articles 1 and 2. What cannot yet be separated, and the test that would, is added to article 1's "What we still do not know". |

## Program A: the ST-4, 5 and 10 shot groups

Entry 158 section 2, run on the 2026-09-20 ST-4 sheet with Alan's twenty groups and 115 shots, as entry 172 replaced step 1. The ST-4 is the
orange commercial target, so under the rules above no article names or shows it.

**Registration from the sheet's own grid**, `grouplab st4`, `GridRegistration`. The printed 1 in grid's crossings are found from the
orange lines and counted into a lattice, so every crossing's true place is known. Eight of the nine frames registered; the ninth, the whole
sheet from furthest back, gives the grid at 48 pixels an inch and too few crossings. The root mean square residual of the crossings, in grid
inches:

| frame | pixels an inch | homography | with the lens's radial terms |
|---|---|---|---|
| 185950, close | 480 | 0.019 | 0.013 |
| 185953, close | 456 | 0.028 | 0.021 |
| 185956, close | 444 | 0.031 | 0.021 |
| 185958, close | 504 | 0.018 | 0.012 |
| 190001, close | 396 | 0.044 | 0.029 |
| 190005, whole sheet | 180 | 0.018 | 0.013 |
| 190009, whole sheet, oblique | 180 | 0.048 | 0.045 |
| 185944, whole sheet, square on | 168 | 0.050 | 0.044 |

The lens's terms lower the residual on every frame, by a third on the close ups, as entry 157 section 4.4 found on GroupLab's own sheets.
What remains, 0.012 to 0.045 in, holds the crossing finder's own error, a few pixels, and a sheet that is not flat on its board, which a
homography with two radial terms cannot follow.

**Detection on overlapping holes, step 2.** The neutral darkness detector, built for scans, with the 0.264 in calibre, finds **54 of the
400 shots in view across the eight frames as a mark of their own**, about one in seven; every other shot is merged into a neighbour's mark,
or not found. A five shot group is typically one or two marks between 0.3 and 0.6 in across, which is two or three touching holes read as
one. Marks found on the printed grid away from every group are few, 13 in all. Two close ups, 185950 and 185958, and the oblique frame
placed their marks away from the groups they show, because their lattice was offset or miscounted; their counts are not to be read.

**The same group in several frames**, entry 172 section 3 item 2. Where a group's marks were found in more than one frame, their center
agreed within 0.006 to 0.17 in on the frames that aligned, and by up to 0.77 in where a frame found a different subset of the merged holes.
So the photograph does not move a group by much; what it changes is which holes the detector can see.

**Steps 3 and 4 wait.** What five shots tell you needs every shot placed, and this detector cannot place overlapping holes in a photograph.
A 600 dpi scan of the same sheet might: on a scan GroupLab tells one hole from two by their size, which is what that part was built for.
Request 19 asks Alan whether the sheet can still be scanned. Entry 172's zero offsets, section 2 item 4, wait for the same reason, and also
need the aiming marks' own positions measured rather than read off the annotated photograph.

## Program B: what would separate speed from everything else

Entry 158 section 3. **Step 1 is measured and published**: on the 2026-09-20 scans a hole measures 0.765 of the bullet for the .22 LR,
0.92 to 0.95 for the 6 ARC and the two 6.5 Creedmoor sheets, articles 1 and 2. The spread of a single hole about its sheet's median is
large: `docs/SCAN-MEASUREMENTS.md` section 3.5 measured a standard deviation of 0.024 to 0.043 in in hull diameter on four of its five groups of 29 to
104 holes, about 0.09 to 0.16 of the bullet.

**Step 2, what is confounded.** The .22 LR differs from the centerfire sheets in every way at once: speed, about 1080 against 2500 to
2845 ft/s; nose, round against spitzer; construction, bare lead against a jacket; and diameter, 0.222 against 0.243 and 0.264. The paper,
the scanner and the afternoon were the same, but the distance each was shot at and its backing were not recorded as the same. So the data
says a .22 LR hole closes up more and cannot say why. Question 38 already showed how strongly the light moves a photographed hole, which is
why only scans are used here.

**Step 3, the test that would settle it.** Everything held but the cartridge:

- One printed batch of GroupLab 25-bull Letter sheets, one backing, stapled the same way, one distance, 50 yd, and one flatbed at 600 dpi.
- The cartridges, the subsonic set at about 1000 to 1080 ft/s beside the supersonic ones: subsonic .22 LR, .300 Blackout subsonic, 8.6
  Blackout subsonic, .510 Whisper; and supersonic .22 LR, 6 ARC, 6.5 Creedmoor, and .300 Blackout supersonic, which gives one diameter at
  two speeds.
- One shot to a bull, so no hole touches another, **two sheets, 50 holes, for each cartridge**, the sheets shot in an interleaved order so
  the light and the paper's age are not on one cartridge's side.

**How many holes.** With a per-hole standard deviation of 0.12 of the bullet, the middle of section 3.5's range, comparing two cartridges'
mean ratios at the 5 percent level with 80 percent power needs 2 (1.96 + 0.84)^2 (0.12 / d)^2 holes each to see a difference d: 90 for
0.05, 23 for 0.10, 10 for 0.15. The .22 LR sits 0.17 below the centerfire mean, so 50 each sees a difference that size with room to spare
and one of 0.067 at the 80 percent power. Two sheets per cartridge, rather than one of 50, keep a single sheet's paper or backing from
passing as the cartridge's.

**The two comparisons that decide it:** the four subsonic cartridges against each other, 0.222 to 0.510 in at one speed, which is diameter
alone; and .300 Blackout subsonic against supersonic, and .22 LR subsonic against high velocity, which is speed alone at one diameter. If
the subsonic ratios agree whatever their diameter and differ from the supersonic ones, it is speed. The round nose against spitzer cannot
be separated by this set and is said so.

**Step 4.** The article waits for the data. Until then article 1 says what can and cannot be separated. Request 20 asks Alan to shoot the
test when he can.

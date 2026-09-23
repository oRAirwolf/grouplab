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

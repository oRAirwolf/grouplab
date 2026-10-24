# Every picture of GroupLab that is published

NOTES-FROM-PLANNING.md entry 253 section 1, as of 2026-09-28. Alan: "at some point code should update all of the screenshots on the
website." This is the list: every picture on grouplab.org, in the README and in the guides that shows GroupLab, where each is used, what
it was made from and the build it shows. **Every one is produced by the screenshot walk or the phone sitting; there is no gap.**

## How they are kept current

- **The desktop screens** are rendered by `Entry109Tests.EveryScreenIsPhotographedInBothThemesAtBothSizes`, headless, from the code on
  main, by the `screenshots` workflow: every Monday, by hand, and (since entry 253) whenever a screen's own source files reach main.
  `docs/figures/screens/current/SOURCES.md` says what every picture shows; `PublishedRendersTests` holds it to that.
- **The stale check.** `scripts/screens-stamp.py` stamps the pictures with the newest nightly and a hash of each screen's source files
  (`docs/figures/screens/screens.json`). The site build fails when a screen's files changed and a whole nightly has shipped them
  without a new picture, when the desktop pictures are more than **30 nightlies** behind the newest (up to nine nightlies have shipped in a day; the weekly job restamps them), or
  when the phone's are more than **60 nightlies** behind: they can only be retaken with Alan's devices, in a sitting.
- **The phone and tablet pictures** are taken over adb in a device sitting (`docs/figures/screens/phone/SOURCES.md`), the status bar cut
  off; `python scripts/screens-stamp.py --phone N` records the nightly they show.
- **The guides' PDFs** embed the same renders: `grouplab user-guide docs` rebuilds them after a render.
- **The link preview** (`/assets/img/og.png`, on every page) is cut by the site build from `analysis-dark-1400x900.png`.
- **The sheets** the site offers (`website/donor/GL-CF25-LTR.pdf` and `-D.pdf`) are byte for byte what the current library renders
  (checked 2026-09-28). No page shows a zeroing grid except the Targets tour stop and the Features page, both now C3.

## The desktop screens

Rendered 2026-09-28 from the code of main after nightly 117, which nightly 118 ships. The site shows the 1400x900 pictures, as WebP; the
Store listing uses 1920x1080.

| Screen | Pictures | Where it is used | Made from |
|---|---|---|---|
| `analysis` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /, /features/, /guides/testing-guide/, /tour/, /tour/analysis/, README.md, docs/TESTING-GUIDE.md, docs/store/LISTING.md | Entry109Tests synthetic sheet |
| `analysis-open` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/, /guides/user-guide/, /tour/, /tour/analysis-open/, docs/USER-GUIDE.md | Entry109Tests synthetic sheet |
| `keyboard` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/ | Entry109Tests synthetic sheet, a row of the shot table chosen with Space |
| `ballistics` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /guides/user-guide/, /tour/, /tour/ballistics/, docs/USER-GUIDE.md, docs/store/LISTING.md | Entry109Tests synthetic sheet |
| `ballistics-hit` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/, /tour/ballistics/ | Entry109Tests synthetic sheet |
| `ballistics-point` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/, /tour/ballistics/ | Entry109Tests synthetic sheet, the Test load, a whitetail at 9 in 10 from a 0.63 MOA mean radius (entry 455) |
| `equipment-scope` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/, /tour/equipment/ | Entry109Tests synthetic sheet, the test rifle's page scrolled to What the scope and mount allow (entry 456) |
| `compare` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/, /guides/user-guide/, /tour/, /tour/compare/, README.md, docs/USER-GUIDE.md, docs/store/LISTING.md | Alan's own scans, entry 171; Entry109Tests synthetic sheet |
| `equipment` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/, /tour/, /tour/equipment/ | no sheet at all: a rifle's page with its two barrels, the one on now chosen (entry 407 section 5) |
| `bull-lines` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/ | Entry109Tests synthetic sheet: bull 1 chosen and given bull 2's hole, zoomed in, the panel's block with its banner (entry 407 section 1) |
| `groups` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/ | Entry109Tests synthetic sheet: How did you shoot this sheet? with two bulls that took two, and Loads on this sheet (entry 407 section 2) |
| `shots-open` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/ | Entry109Tests synthetic sheet: the Shots list with one shot open (entry 407 section 3) |
| `paper-fold` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/, /tour/marking/ | Entry109Tests synthetic sheet: the setup's For the hole size studies fold opened (entry 407 section 6) |
| `readings-here` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/ | Entry109Tests synthetic sheet: Velocity and the vertical with no readings, speeds typed in its box (entry 407 section 7) |
| `scale-markers` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/ | built-in library sheet |
| `fingerprint` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/, /tour/, /tour/fingerprint/ | Entry109Tests stand-in poster, drawn by GroupLab, at the second step of Add a store-bought target (entry 348) |
| `firstrun` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /tour/, /tour/firstrun/ | no sheet at all |
| `marking` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /, /features/, /guides/user-guide/, /tour/, /tour/marking/, README.md, docs/USER-GUIDE.md, docs/store/LISTING.md | Entry109Tests synthetic sheet |
| `optic` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/, /tour/, /tour/optic/ | sheet made by the generator |
| `optic-4x` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /tour/optic/ | sheet made by the generator |
| `pairing` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/, /tour/ballistics/ | Entry109Tests plain sample target, marked by hand with a 300 yd group, and a timed chronograph string made by the walk (entry 351) |
| `scope-unit` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/ | no sheet at all |
| `sessions` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/, /guides/user-guide/, /tour/, /tour/sessions/, README.md, docs/USER-GUIDE.md | Alan's own scans, entry 171; Entry109Tests synthetic sheet |
| `settings` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/, /guides/user-guide/, /tour/, /tour/settings/, docs/USER-GUIDE.md | Entry109Tests synthetic sheet; no sheet at all |
| `settings-shooting` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/ | Entry109Tests synthetic sheet |
| `shots-to-zero` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/, /tour/analysis-open/ | Entry109Tests synthetic sheet |
| `velocity` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/ | Entry109Tests plain sample target, marked by hand with a 300 yd group, and readings made by the walk, each paired with its shot |
| `targets` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/, /guides/user-guide/, /tour/, /tour/targets/, README.md, docs/USER-GUIDE.md | built-in library sheet |
| `targets-zero` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/, /tour/targets/ | built-in library sheet |

## The phone and tablet

Taken 2026-09-28 on nightly 115 (Galaxy Z Fold 7 cover screen, Galaxy Tab S8 Ultra). Retaken in request 50's device sitting on the
nightly that carries the C3 grids and Shots Needed to Zero (entry 253 section 3).

| Phone picture | Where it is used |
|---|---|
| `fold-fingerprint-dark.png` | /features/ |
| `fold-fingerprint-light.png` | /features/ |
| `fold-open-targets-dark.png` | /features/ |
| `fold-open-targets-light.png` | /features/ |
| `fold-pairing-dark.png` | /features/ |
| `fold-pairing-light.png` | /features/ |
| `fold-scale-markers-dark.png` | /features/ |
| `fold-scale-markers-light.png` | /features/ |
| `fold-capture-dark.png` | /features/, /tour/, /tour/capture/ |
| `fold-capture-light.png` | /features/, /tour/capture/ |
| `fold-firstrun-dark.png` | /tour/, /tour/firstrun/ |
| `fold-firstrun-light.png` | /tour/firstrun/ |
| `fold-result-dark.png` | /features/, /tour/, /tour/analysis/ |
| `fold-result-landscape-dark.png` | /features/, /tour/, /tour/marking/ |
| `fold-result-landscape-light.png` | /features/, /tour/marking/ |
| `fold-result-light.png` | /features/, /tour/analysis/ |
| `fold-sessions-landscape-dark.png` | /features/, /tour/, /tour/sessions/ |
| `fold-sessions-landscape-light.png` | /features/, /tour/sessions/ |
| `fold-settings-dark.png` | /features/, /tour/, /tour/settings/ |
| `fold-settings-light.png` | /features/, /tour/settings/ |
| `fold-targets-dark.png` | /features/, /tour/, /tour/optic/, /tour/targets/ |
| `fold-targets-light.png` | /features/, /tour/optic/, /tour/targets/ |
| `icons-fold.png` | /features/ |
| `icons-tab.png` | taken, and no page shows it |
| `tab-capture-landscape-dark.png` | taken, and no page shows it |
| `tab-capture-landscape-light.png` | taken, and no page shows it |
| `tab-result-landscape-dark.png` | /features/ |
| `tab-result-landscape-light.png` | /features/ |
| `tab-result-portrait-dark.png` | taken, and no page shows it |
| `tab-result-portrait-light.png` | taken, and no page shows it |
| `tab-sessions-landscape-dark.png` | taken, and no page shows it |
| `tab-sessions-landscape-light.png` | taken, and no page shows it |
| `tab-settings-landscape-dark.png` | taken, and no page shows it |
| `tab-settings-landscape-light.png` | taken, and no page shows it |
| `tab-targets-landscape-dark.png` | /features/ |
| `tab-targets-landscape-light.png` | /features/ |

## The Features page: each entry's own picture (entry 256)

**The rule.** When a Features entry names a particular thing (a bull, a grid, a sheet, a view, a result), its picture shows that thing,
large enough to see what makes it different; a general screen that only happens to contain it does not count. A bull, grid or sheet is
drawn by the screenshot walk straight from the library as it prints (`sheet-*.png`, one picture for both themes on its own paper, the
same on both sides of the Desktop and Mobile switch), and the stale check holds it to the renderer and its target file. **A new bull,
sheet or view is not finished until the Features page has its own picture of it.**

**Their own picture now:** the E bull (`sheet-e-bull`), the C bull with its dot (`sheet-c-bull`), the zeroing grids (the four C3
sheets, MOA and mil at 100 yd and 100 m, credited to Alan with Jylee and Unholy), large format on a home printer (`sheet-large-set`,
the four Letter sheets), the six-bull label on 4x6 as it prints and as a thermal printer's dots (`sheet-x6-4x6`, `sheet-x6-4x6-thermal`), Shots Needed to Zero (credited to Jylee), Velocity and the vertical (the block and the band, entry 323), Ballistics and hit chances (the Hit probability view), Made for
your optic, the Targets screen, which bull each shot belongs to (`bull-lines`), how you shot the sheet (`groups`), the Shots list
(`shots-open`), chronograph readings on the analysis (`readings-here`), paper and backing (`paper-fold`), barrels under their rifle (`equipment`), all entry 407, CEP circles and every figure explained (the analysis open), compare loads, sessions, the figures you
read off a target, the three sharing choices (Settings, where they are made), and a set's tab after the 2 MOA set's three sheets were dropped together (`set-tab`, entry 423).

**Their own picture since entry 462:** Conditions on the day (`conditions`), Every shot, and which count (`shots-window`), Zero from this group (`zero-window`), A picture to share (`share-window`) and Words explained where they appear (`word-explained`).

**Gaps: still a general screen**, each to get a crop of the view it names, and why it waits:

| Entry | Its picture now | What it should show | Why it waits |
|---|---|---|---|
| A GroupLab sheet reads itself | the marking screen | a sheet's codes and markers found | Show work at its codes stage draws the codes and markers found; the walk opens Show work only on a failed sheet so far |
| Every hole found | the marking screen | a few holes close up with their rings | needs the marking screen zoomed onto a few holes with their rings, a view the walk has no way yet to frame on holes |
| The bulls you aimed at | the marking screen | the bull picker with rows chosen | the bull picker's chosen rows are set by clicks the headless walk does not make yet |
| A sheet shot off by the same amount | the analysis | the whole-sheet assignment's statement | needs a synthetic sheet shot a bull off, which the walk's one sheet is not |
| Any target you already shoot | the marking screen | bulls placed by hand and a scale drawn at one | marking-other shows bulls placed by hand; the scale drawn at one is a drag the walk does not make yet |
| Open by dropping or pasting | the marking screen | the drop target over the window | the drop target is drawn only while a drag is over the window, which headless rendering does not hold |
| Shots in and out as CSV | the analysis | the export and import commands | the export and import are file pickers of the operating system, not drawn by GroupLab |
| Large sheets | the Targets screen | the photograph detail statement for a tiled target | needs a tiled target in the library chosen on Targets, not yet in the walk |
| Real inches from a scan | the analysis | the scale line saying the print's own size | needs a flatbed scan with a stated resolution, which the walk's synthetic sheet does not carry |
| Photographs at an angle | the marking screen | the angle and its limit named | needs a photograph taken at an angle; the walk's pictures are drawn square |
| Tap a number to switch units | the analysis, with the one-time hint | a figure switched, with the note at the bottom | the one-time hint shows on a real tap, which the walk cannot make; switching a figure without it would show half the feature |
| Several aim points on one target | the analysis | aim points placed by hand, each in its color with its figures | aim points are placed by clicks the walk does not make yet |
| The one-page report | the analysis | the one-page report itself | the report is a PDF and PNG file GroupLab writes, not a screen; its picture would be the file itself, which the walk does not render yet |
| Several targets open at once | the marking screen | the tab row with two targets, one of them not saved | needs a second target opened unsaved beside the first; the walk keeps one target so every later picture is the same |

**Settings in eight categories, Daily copies of your records and Shooting defaults** (entry 468) have their own pictures: `settings`, the
side menu with the Units and figures page; `settings-data`, Sync and your data with the daily copies first; and `settings-shooting`, the
Shooting defaults page. The phone's list of categories is `fold-settings`, taken on nightly 218 (entry 469 section 3), and its two
pages are `fold-settings-shooting` and `fold-settings-data`, first taken by the emulator's walk on 2026-10-23 (nightly 220) and shown on
the Features page beside the computer's since entry 472 section 4.

**Sync between your devices** (entry 463) has its own picture, `sync-settings`: Settings at Sync between devices with a folder the walk makes and a second store, Range laptop, in it; and `sync-conflict`, a session changed on both, side by side. The phone's Sync page waits for the emulator's walk.

**Store-bought targets recognized** (entries 340 and 341) has its own picture, `store-target`: a bullseye the walk draws itself, standing
in for the Shoot-N-C since no maker's artwork is published, recognized with its bull placed and the scale warning, and the window that asks
the 6 inch or the 8 inch laid over it.

**Teach GroupLab a store-bought target** (entry 348) has its own picture, `fingerprint`: Add a store-bought target at its second step, How
big is this target?, with a 12 by 18 in poster the walk draws and photographs itself, its corners found and its printed size typed. The
phone's second step is `fold-fingerprint`, taken on the emulator (entry 388).

**Pair readings with shots, a row per reading** (entry 351) has its own picture, `pairing`: Ballistics, Chronograph, with a timed string
the walk makes, a row per reading, a six minute pause between two groups, and the fourth reading's choices open under its row. The phone's
rows are `fold-pairing`, taken on the emulator (entry 388).

**Several targets open at once on the phone** (concept A, Alan 2026-10-07) is on the phone only, so it has no desktop picture; its Open
targets sheet is `fold-open-targets`, taken on the emulator (entry 388).

**The bar along the bottom at Android's largest text size** (entry 413, Alan chose B: the icons alone, larger) is taken by the emulator
at the Fold's cover screen with Android's text size at 2.0, on Targets, `fold-targets-largest-text` (first taken after nightly 188), and
is the Features entry's own picture.

**Walk me through a demo on the phone** (entry 446) is pictured by the emulator at its second step, the sample read and its plot outlined
with the step's card, `fold-demo`, taken on the emulator (first after nightly 201); it is the Features entry's Mobile picture.

## Not published

`docs/figures/screens/*.png` (2026-09-14), `before/` and `after/` (entry 247's comparison for Alan) are the record of their day. No
page, README or guide shows them.

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
| `ballistics` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /guides/user-guide/, /tour/, /tour/ballistics/, docs/USER-GUIDE.md, docs/store/LISTING.md | Entry109Tests synthetic sheet |
| `ballistics-hit` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/, /tour/ballistics/ | Entry109Tests synthetic sheet |
| `compare` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/, /guides/user-guide/, /tour/, /tour/compare/, README.md, docs/USER-GUIDE.md, docs/store/LISTING.md | Alan's own scans, entry 171; Entry109Tests synthetic sheet |
| `equipment` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /tour/, /tour/equipment/ | no sheet at all |
| `firstrun` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /tour/, /tour/firstrun/ | no sheet at all |
| `marking` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /, /features/, /guides/user-guide/, /tour/, /tour/marking/, README.md, docs/USER-GUIDE.md, docs/store/LISTING.md | Entry109Tests synthetic sheet |
| `optic` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/, /tour/, /tour/optic/ | sheet made by the generator |
| `optic-4x` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /tour/optic/ | sheet made by the generator |
| `sessions` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/, /guides/user-guide/, /tour/, /tour/sessions/, README.md, docs/USER-GUIDE.md | Alan's own scans, entry 171; Entry109Tests synthetic sheet |
| `settings` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/, /guides/user-guide/, /tour/, /tour/settings/, docs/USER-GUIDE.md | Entry109Tests synthetic sheet; no sheet at all |
| `shots-to-zero` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/, /tour/analysis-open/ | Entry109Tests synthetic sheet |
| `targets` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/, /guides/user-guide/, /tour/, /tour/targets/, README.md, docs/USER-GUIDE.md | built-in library sheet |
| `targets-zero` | 8: dark and light at 1280x720, 1400x900, 1920x1080, 2560x1440 | /features/, /tour/targets/ | built-in library sheet |

## The phone and tablet

Taken 2026-09-28 on nightly 115 (Galaxy Z Fold 7 cover screen, Galaxy Tab S8 Ultra). Retaken in request 50's device sitting on the
nightly that carries the C3 grids and Shots Needed to Zero (entry 253 section 3).

| Phone picture | Where it is used |
|---|---|
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

## Not published

`docs/figures/screens/*.png` (2026-09-14), `before/` and `after/` (entry 247's comparison for Alan) are the record of their day. No
page, README or guide shows them.

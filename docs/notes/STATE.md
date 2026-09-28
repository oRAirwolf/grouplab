# Where this project is, right now

**Rewritten at the end of every entry, never appended to. Under 120 lines, and a test holds it there.**

NOTES-FROM-PLANNING.md entries 160 and 180. Both sessions read this file first, instead of searching
`NOTES-FROM-PLANNING.md` and `PHASE1-RESULTS.md`. Alan does not read the Claude Code panel: what is in it for
him is mirrored in `docs/notes/panel.md` (local, not committed), and what needs him is in
`docs/notes/for-alan.md`, which starts with the count of open requests.

If something here disagrees with the logs, the logs are right and this file is out of date. Say so.

**Last rewritten:** 2026-09-28, after entries 253 to 271 were folded (the night of entry 263).

---

## In flight

- **The phone's camera screen is rebuilt** (entry 260): a native Capture B over the live preview, Guided and Manual, the quality bar,
  the torch on Auto, and Feedback B checking every picture with a score from 0 to 100 (`PictureCheck`). Nightly 119 carries it; it
  was checked over adb on the Fold 7's cover screen only. **The camera test with Alan** (request 33, entry 255) is the next step.
- **A curled sheet is registered through every marker** (`MarkerMesh`, a thin-plate spline) when the radial fit keeps fewer than half
  the corners; a saved session keeps the mesh as its corners (entry 260).
- **Real inches on photographs** (entry 271): a printer profile from a scan or one ruler distance corrects photographs and says so in
  one line; Settings chooses the printer on the desktop, the last one kept on the phone. **Entries 272 and 273 build the rest as Alan
  approved it**: the Scale check page (GL-SCALE-LTR-1 and an A4 one), the three-screen wizard, card photos, Printers in Settings, the
  paper-edge check on every photo, and scale across and down separately.
- **Tap a number to switch units** (entries 272 and 273): approved as the concept's working demo; not started.
- **The phone does what the desktop does** (entries 258, 259): full figures, the bulls you fired at, Shots Needed to Zero, compare loads,
  Ballistics as a fifth tab, the set as a checklist and the scan pill are on the phone; `docs/PHONE-PARITY.md` holds the rest (marking
  by touch, CSV, large sheet advice, a picture shared in), and the site build fails on a feature with no row.
- **Any target, and the sheet as the fast lane** (entry 270): the README, the home page, the tour index, Features and the user guide
  lead with it. Its picture waits on question 66.
- **The README is Alan's chosen design** (entry 266), its pictures made by `scripts/readme-images.py`; `scripts/consistency.py` audits
  the README, the site and the assets (entry 267), including retired wording in `docs/RETIRED-WORDING.json`.
- **The donor pack** is one sheet of each style, built from the library (entry 264); /download/ lists Android (entry 265).
- **GroupLab Dev has a black idle screen** for the OLED devices (entry 268); every device session ends on it.

## The next three

1. Entries 272 and 273: the printer check (the measuring logic first, then the check page, the wizard and Printers in Settings) and
   tap to switch units, with their Features entries, pictures, guide, tour and README in the same change.
2. The camera test with Alan, then the device sitting: the phone and tablet pictures (253 section 3), the inner Fold screen (257),
   torch strength in a session (262), and trying entry 259's screens.
3. Entry 258's remaining phone features, in its order.

## Blocked, and on what

- **The tablet** did not answer adb all night (from 11:19 UTC on 2026-09-28): its pictures and torch reading wait for it.
- **Entry 261 section 6**, the server's capacity: one read-only ssh command, which needs Alan's approval.
- **Entry 170 section 4.4.** Request 9: the same scan marked by hand twice.
- **Entry 166 sections 3.2 and 5.** Request 16: the Mac tester's measurement and his name for a thanks.

Open requests in `docs/notes/for-alan.md`: **7** (50 the device sitting with the camera test of 33 in it; 46 the backups on
4 October; 38 the Store; then 9, 16 and 20).

## Open questions

Seven, all in `docs/QUESTIONS-FOR-PLANNING.md`.

- **66** which target the "Your own targets" picture may show (most commercial scans here are OnTarget designs)
- **65** entry 261's `tools/study/` against CLAUDE.md's read-only `tools/`
- **51** which hole centre GroupLab should report; agreed to wait on request 9
- **44, the part still open** the bent-sheet model throws at a point outside the page
- **43** entry 137 names an image safety the desktop does not have
- **36** a light installer, measured, and why shrinking the one we have beat it
- **34** pooling two sheets of one load needs a rule for what a pooled group's center means

## Builds and the site

- **Last nightly:** 0.2.0-nightly.119: the rebuilt capture screen, the phone's parity screens, the idle screen.
- **Entries 260 (the mesh), 264 to 267, 270 and 271 are pushed together** after nightly 119; the site follows each push by itself.
- **The site sync** checks for as long as nginx can serve a replaced file, read from nginx at run time.

## The inbox

`docs/notes/inbox/` holds the entries below. A test reads this line and the directory and fails when
they differ. The planning session's files are not committed, so while a run is working through them the
line reads what the repository holds, and the test fails locally until the last is done.

**Holds:** none

## Things that would surprise somebody who was not here yesterday

- **The logs were split again** on 2026-09-28 (entry 160's rule): the live files keep the newest fifteen entries and twelve results
  sections; the rest is whole in `docs/notes/archive/`. A question is taken as open only when its line reads `**Status: open`.
- **A UI dump cannot see the phone's camera screen**: its views are native, inside Avalonia's host. The device check reads the screen's
  own `camera.layout` log line instead (`scripts/device-capture-check.py`).
- **Starting GroupLab Dev's main screen over the idle screen makes a second window, which crashes**; press Back first.
- **A photograph's scale comes from the printer chosen**, where one is; a scan's own measured scale always wins.
- **A disc can be a square** (entry 243 section 4): its diameter is the diagonal, and a square is measured as itself.
- **A zeroing grid is drawn by its style**: style 3's strokes, ticks, numbers, legend and bar are fixed by `GridStyle3`. A real hole in
  black is as large as on white; a synthetic one is not.
- **Inbox files are moved to `C:\Dev\grouplab-trash\<date>\`**, not deleted (entry 222); the Holds line never lists them.
- **The tour and Features have two sides** (entry 249): every stop and phone feature needs a phone screenshot or words.
- **Every published sentence has its backing**: `scripts/claims.py --check` fails CI otherwise (entry 159).
- **Nothing under `website/server/` may hold a carriage return**: it is copied to Linux as it is.
- **Nothing is written into a HestiaCP `conf/web/<domain>/` folder** but the include itself.
- **Alan's own photographs and scans may be published**, and so may what he passes on from Unholy (also TNA) and his other friends;
  the 2026-09-16 friend scan never is. Justin is credited as "Justin" only.
- **A sample over about 10 MB is never committed**; it goes on the `test-data` release.
- **Requests for Alan go in `docs/notes/for-alan.md`**, never only in the panel.

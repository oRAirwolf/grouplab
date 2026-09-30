# Where this project is, right now

**Rewritten at the end of every entry, never appended to. Under 120 lines, and a test holds it there.**

NOTES-FROM-PLANNING.md entries 160 and 180. Both sessions read this file first, instead of searching
`NOTES-FROM-PLANNING.md` and `PHASE1-RESULTS.md`. Alan does not read the Claude Code panel: what is in it for
him is mirrored in `docs/notes/panel.md` (local, not committed), and what needs him is in
`docs/notes/for-alan.md`, which starts with the count of open requests.

If something here disagrees with the logs, the logs are right and this file is out of date. Say so.

**Last rewritten:** 2026-09-30 20:50 UTC, during entry 318 (sections 3 and 4 done, 1 with a worker, the list of what is left added).

---

## In flight

- **Nightly 137** (15:30 UTC) carries everything below, and was signed and accepted by TestFlight (both bundles carry the encryption
  setting as false). The iPad mini updated itself to 137 by 16:20 UTC (read over USB); request 59's groups are for the public beta.
- **Nightly 138** carries the replay camera for GroupLab Dev, one shared frame judge for both camera screens, and the TestFlight
  feedback step's import fix.
- **Merged today** (on main, first in nightly 137): the iPad reading hang, Cancel and a one-minute limit (313); Guided about a second
  after ready and the green level (311); Send diagnostics, the iPad's Files folder, its log over USB, TestFlight feedback summaries
  (311 section 3); the iPad screenshot fixes (312); crash reports 11 and 12 fixed; the cartridge lookup and its setting (314); scenario
  files, GroupLab Dev for iOS, the automation bridge, and Show diagnostics on the camera (315 sections 1, 2, 4); the Mac download says
  M-series (316). Entry 315 is done; Firebase Test Lab is reported (free: 15 runs a day) and not set up.
- **iOS until 2026-10-01 02:00 UTC** (entry 290): the summary is written at the top of `for-alan.md` (15:40 UTC).
- **The Microsoft Store's first submission** (request 38 Part B) went to certification on 2026-09-30; `store-status.yml` reads its
  status every six hours (read only) into the run summary: "Certification" at 15:45 UTC.
- **GroupLab Dev updates itself** (entry 288): the silent second update waits for the phones to be reachable over adb again.
- **Colored bulls** (entry 297): question 75 with planning; request 57 for Alan.

## The next three

**What is left of the plan** (entry 318 section 5, for planning's next batch; one line each, with what blocks it):
- **Detection, angled photos** (318.1): being built; the scoreboard judges it. **Holes on any target** (318.2): next, nothing blocks.
- **Detection on blank paper with no definition** (Phase 4): needs a photo of plain paper with real holes at a known scale (54, 58).
- **Garmin Xero import** (Phase 5): needs one sample export file from anybody with a Xero.
- **Velocity regression, predicted against measured vertical** (Phase 5): not started; nothing blocks but priority.
- **Synchronization, cloud adapters over three-tier storage** (Phase 7): not started; needs planning's design.
- **iOS** (Phase 8): on TestFlight and on the iPad; the device checks need a sitting (50), the public beta request 59, GroupLab Dev
  on the iPad request 61, an App Store release Alan's word.
- **Performance** (Phase 9): the baseline gate and any optimization; waits for planning to say the application has settled.
- **Proof of the 27 "built, not proven" features**: the gates need real material: requests 9, 16, 20, 33, 56, 57, 58.
- **Stores**: Microsoft in certification (38); Google Play past internal test is Alan's call; Test Lab waits on request 62.
- **Deferred on purpose**: the full visual designer and the full detector on a bought target (DESIGN.md section 3).
- **A beta or stable release**: only when Alan asks, after the eight checks in `docs/RELEASE-PLAN.md`.

1. Entry 318 sections 1 and 2 (the worker is on 1), then this list's first free item.
2. Watch the Store's certification (store-status.yml) and close request 38 when GroupLab is listed.
3. Each nightly's notes need placing in `website/features.json`, or the site stops building.

## Blocked, and on what

- **TestFlight distribution and the public beta link**: request 59 (the two groups, then the public link).
- **The iOS GroupLab Dev upload**: request 61 (its App ID, profiles and record).
- **The phones**: not reachable over adb since 2026-09-30 morning.
- **Entry 170 section 4.4.** Request 9. **Entry 166 section 3.2.** Request 16.

Open requests in `docs/notes/for-alan.md`: **14** (59 TestFlight groups; 62 Firebase Test Lab; 56 printer scale; 50 the device sitting; 54, 57, 58 at the
range; 46 backups on 4 October; 38 waits on Microsoft; 61 GroupLab Dev's Apple steps; then 33, 9, 16 and 20).

## Open questions

Six, all in `docs/QUESTIONS-FOR-PLANNING.md` (75 and 76 answered in entry 318, as built):

- **67** the printer check page as grid style 4 (with Alan)
- **51** which hole center GroupLab should report; waits on request 9
- **44, the part still open** the bent-sheet model throws at a point outside the page
- **43** entry 137 names an image safety the desktop does not have
- **36** a light installer, measured
- **34** pooling two sheets of one load needs a rule for a pooled group's center

## Builds and the site

- **Last nightly:** 0.2.0-nightly.139 (2026-09-30 17:07 UTC): crash report 9's fix; 138 had the replay camera; iOS signed and sent to
  TestFlight each time since 137.
- **The site** publishes each push; a234baeb carries the M-series wording.
- Crash reports open: none. 9 was fixed by 10259c6e from the stack in the iPad's own log and closed with nightly 139; 11 and 12 closed
  with nightly 137.

## The inbox

`docs/notes/inbox/` holds the entries below. A test reads this line and the directory and fails when
they differ.

**Holds:** none

Entries 311 to 317 are folded; their files are in `C:\Dev\grouplab-trash\2026-09-30\`.

## Things that would surprise somebody who was not here yesterday

- **The logs were split again** on 2026-09-28 (entry 160's rule): the live files keep the newest fifteen entries and twelve results
  sections; the rest is whole in `docs/notes/archive/`. A question is taken as open only when its line reads `**Status: open`.
- **The Mac packaging can be run by hand**: `gh workflow run package.yml --ref <branch> -f ref=<sha>` proves the signing and
  notarization on a commit without a nightly, and publishes nothing.
- **A UI dump cannot see the phone's camera screen**: its views are native, inside Avalonia's host. The device check reads the screen's
  own `camera.layout` log line instead (`scripts/device-capture-check.py`).
- **Starting GroupLab Dev's main screen over the idle screen makes a second window, which crashes**; press Back first.
- **A photograph's scale comes from the printer chosen**, where one is, across and down; a scan's own measured scale always wins.
- **`android/GroupLab.Android/` ships** (entry 275): a change to the Android application alone now starts a nightly and is in its notes.
- **The check page is in the library but is not a target**: it counts as no sheet in the README, and a picture of it opened as a target
  goes to the printer check. A value on screen is tappable exactly when it shows a unit (`UnitTap.KindOf`).
- **A disc can be a square** (entry 243 section 4): its diameter is the diagonal, and a square is measured as itself.
- **A zeroing grid is drawn by its style**: style 3's strokes, ticks, numbers, legend and bar are fixed by `GridStyle3`. A real hole in
  black is as large as on white; a synthetic one is not.
- **Inbox files are moved to `C:\Dev\grouplab-trash\<date>\`**, not deleted (entry 222); the Holds line never lists them.
- **The tour and Features have two sides** (entry 249): every stop and phone feature needs a phone screenshot or words.
- **Requests for Alan go in `docs/notes/for-alan.md`**, never only in the panel. It holds only open requests and the latest summary
  (entry 317); answered ones are whole in `docs/notes/for-alan-archive.md`.
- **Any push to main cancels the running build and nightly**, and a push headed `[notes] ` then builds nothing, so the last commit of a push that should make a nightly must not be one (it cancelled
  a234baeb's build on 2026-09-30, and e8fa5ce0 rebuilt it).
- **Entry 317's budget is in force:** one worker by default, the ccusage line once a day in for-alan.md.

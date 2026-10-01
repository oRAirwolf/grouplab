# Where this project is, right now

**Rewritten at the end of every entry, never appended to. Under 120 lines, and a test holds it there.**

NOTES-FROM-PLANNING.md entries 160 and 180. Both sessions read this file first, instead of searching
`NOTES-FROM-PLANNING.md` and `PHASE1-RESULTS.md`. Alan does not read the Claude Code panel: what is in it for
him is mirrored in `docs/notes/panel.md` (local, not committed), and what needs him is in
`docs/notes/for-alan.md`, which starts with the count of open requests.

If something here disagrees with the logs, the logs are right and this file is out of date. Say so.

**Last rewritten:** 2026-10-01 05:15 UTC, entry 329 done (Velocity and the vertical uses the conditions entered); entry 328, Unholy's
two TestFlight bugs, is with the other session.

---

## In flight

- **Two Claude sessions share this working tree** (2026-10-01): grouplab-3b did entry 323; grouplab-1f is doing entry 324 (its section
  2, the sync options paper, is committed; section 1, the bent-sheet registration, runs in its own worktree). Each stages only its own
  files by name and says before pushing to main.
- **Entries 323 and 324 section 1 on main** (fb088362 at the head, with the inbox line fixed); its build and the nightly that carries
  both follow. The `[screens]` catch-up (7cbc13be) is on main and the site is live at 4ec87124, Velocity and the vertical included.
- **Entry 329** (the block in the conditions entered) is committed after that nightly, not before, so nothing is cancelled.
- **Nightly 144** (03:17 UTC) carries entry 322: targets read from 2 and 3 ft, the lens refit.
- **iOS until 2026-10-01 02:00 UTC** (entry 290) is over; the summary is at the top of `for-alan.md`.
- **The Microsoft Store's first submission** (request 38 Part B) is in certification; `store-status.yml` reads it every six hours.
- **GroupLab Dev updates itself** (entry 288): the silent second update waits for the phones to be reachable over adb again.
- **Colored bulls** (entry 297): question 75 answered; request 57 for Alan.

## The next three

**What is left of the plan** (entry 318 section 5, one line each, with what blocks it):
- **Detection, angled photos**: the far column's 0.08 in errors come from a lifted margin; the bent-sheet registration is entry 324
  section 1, with the other session. **Holes on any target**: Experimental, measured; harder cases wait on request 58.
- **Detection on blank paper with no definition** (Phase 4): needs a photo of plain paper with real holes at a known scale (54, 58).
- **Garmin Xero import** (Phase 5): needs one sample export file from anybody with a Xero.
- **Velocity regression** (Phase 5): on both screens since entry 323, in the conditions entered on Ballistics since 329; a
  session's own conditions would need a form (DESIGN NEEDED if wanted); the phone's picture waits for request 50's sitting.
- **Synchronization** (Phase 7): the options paper is `docs/notes/sync-options.md` (entry 324 section 2), for Alan to choose from.
- **iOS** (Phase 8): on TestFlight and on the iPad; the device checks need a sitting (50), the public beta request 59, GroupLab Dev
  on the iPad request 61, an App Store release Alan's word.
- **Performance** (Phase 9): the baseline gate and any optimization; waits for planning to say the application has settled.
- **Proof of the 27 "built, not proven" features**: the gates need real material: requests 9, 16, 20, 33, 56, 57, 58.
- **Stores**: Microsoft in certification (38); Google Play past internal test is Alan's call; Test Lab waits on request 62.
- **Deferred on purpose**: the full visual designer and the full detector on a bought target (DESIGN.md section 3).
- **A beta or stable release**: only when Alan asks, after the eight checks in `docs/RELEASE-PLAN.md`.

1. The nightly after fb088362 (323 and 324), then 329's push; entry 326 as soon as request 63 is done.
2. Watch the Store's certification (store-status.yml) and close request 38 when GroupLab is listed.
3. Each nightly's notes need placing in `website/features.json`, or the site stops building (144's went to notFeatures).

## Blocked, and on what

- **The public beta link**: Apple's first review of build 134 (entries 319, 320); the link is in
  `C:\Dev\grouplab-local\testflight-public-link.txt`, published only after approval. Nothing touches 134's review.
- **The iOS GroupLab Dev upload**: request 61 (its App ID, profiles and record).
- **The phones**: not reachable over adb since 2026-09-30 morning.
- **Entry 170 section 4.4.** Request 9. **Entry 166 section 3.2.** Request 16.

Open requests in `docs/notes/for-alan.md`: **15** (63 a token so TestFlight feedback reaches Code, most urgent; 59 TestFlight groups; 62 Firebase Test Lab; 56 printer scale; 50 the device sitting,
now with a look at the velocity card; 54, 57, 58 at the range; 46 backups on 4 October; 38 waits on Microsoft; 61 GroupLab Dev's Apple
steps; then 33, 9, 16 and 20).

## Open questions

Seven, all in `docs/QUESTIONS-FOR-PLANNING.md`:

- **78** the plot's toggles cover the last lines of its key at 1400x900 (found in entry 323; a change to the look)
- **67** the printer check page as grid style 4 (with Alan)
- **51** which hole center GroupLab should report; waits on request 9
- **44, the part still open** the bent-sheet model throws at a point outside the page
- **43** entry 137 names an image safety the desktop does not have
- **36** a light installer, measured
- **34** pooling two sheets of one load needs a rule for a pooled group's center

## Builds and the site

- **Last nightly:** 0.2.0-nightly.144 (2026-10-01 03:17 UTC): entry 322's distance reading and lens refit.
- **The site** is live at 4ec87124 (published by hand: a bot's `[screens]` push starts no other workflow).
- Crash reports open: none.

## The inbox

`docs/notes/inbox/` holds the entries below. A test reads this line and the directory and fails when
they differ.

**Holds:** none

Inbox files are never committed, so CI sees an empty inbox and this line says none. Waiting locally: 326 (this session's, on
request 63) and 328 (the other session's: Unholy's two TestFlight bugs, fix first).

Entry 323 is folded; its file is in `C:\Dev\grouplab-trash\2026-10-01\`. Entry 326 waits on request 63: no token can write to the private crash-reports repository, and the App Store Connect key is only in
GitHub's secrets.

## Things that would surprise somebody who was not here yesterday

- **The chronograph store counts readings from 1**, and accepting a pairing used to store them from 0: the first reading paired with a
  shot was refused, and every other pairing was one reading off. Fixed in entry 323 on both platforms.
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
- **Inbox files are moved to `C:\Dev\grouplab-trash\<date>\`**, not deleted (entry 222); the Holds line never lists them.
- **The tour and Features have two sides** (entry 249): every stop and phone feature needs a phone screenshot or words.
- **Requests for Alan go in `docs/notes/for-alan.md`**, never only in the panel. It holds only open requests and the latest summary
  (entry 317); answered ones are whole in `docs/notes/for-alan-archive.md`.
- **Any push to main cancels the running build and nightly**, and a push headed `[notes] ` then builds nothing, so the last commit of a
  push that should make a nightly must not be one.
- **Entry 317's budget is in force:** one worker by default, the ccusage line once a day in for-alan.md.

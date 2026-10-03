# Where this project is, right now

**Rewritten at the end of every entry, never appended to. Under 120 lines, and a test holds it there.**

NOTES-FROM-PLANNING.md entries 160 and 180. Both sessions read this file first, instead of searching
`NOTES-FROM-PLANNING.md` and `PHASE1-RESULTS.md`. Alan does not read the Claude Code panel: what is in it for
him is mirrored in `docs/notes/panel.md` (local, not committed), and what needs him is in
`docs/notes/for-alan.md`, which starts with the count of open requests.

If something here disagrees with the logs, the logs are right and this file is out of date. Say so.

**Last rewritten:** 2026-10-03 11:14 UTC, stopped under entry 360 (budget); entry 358 part built on a worker's branch.

---

## In flight

- **STOPPED at Alan's word (entry 360), 2026-10-03 11:14 UTC.** Stay stopped until he says otherwise, even after the weekly window
  resets (2026-10-08 02:00 UTC). Last real reading: **60% of the week** at 09:55 UTC; the status line does not run in the VS Code
  extension, so no fresh reading exists here. Before starting anything: read the week (docs/notes/usage-now.json, or Claude Code's
  cachedUsageUtilization in ~/.claude.json); start nothing at 80% or more, or when the reading is over 10 minutes old.
  `scripts/usage-guard.js`, a PreToolUse hook in .claude/settings.local.json, refuses every tool call at 85%.
- **Entry 358, sections 1 to 3 built, not merged:** commits 6175d457 (label page sizes), 6ab485e6 (thermal print mode) and 9472f323
  (the X6 labels and check labels) on branch `worktree-agent-a2a1221f838518e94` (`.claude/worktrees/agent-a2a1221f838518e94`). Only their
  own test groups ran: run the three suites, the site build and consistency before cherry-picking. Not started: sections 4
  (printer framework), 6 (darkness) and 8 (public pages).
- **Entry 359 is unread** (corrections, per entry 360's order after 358). **Entry 361 is on hold** at Alan's word (11:12 UTC: "We will look again tomorrow").
- **Entry 360's hook has no finishing flag yet** (section 3): the session's safety check refused the change; Alan's to decide (for-alan.md).
- **Waiting on Alan:** requests 70 (Fenix's report package), 71 (switching on entry 357, the store forms), 72 and 73 (the label
  printers on arrival). **Waiting on planning:** questions 79 and 81.

## The next three

**What is left of the plan** (entry 318 section 5, one line each, with what blocks it):
- **Detection, angled photos**: the far column's 0.08 in errors come from a lifted margin; the bent-sheet registration is entry 324
  section 1, with the other session. **Holes on any target**: Experimental, measured; harder cases wait on request 58.
- **Detection on blank paper with no definition** (Phase 4): needs a photo of plain paper with real holes at a known scale (54, 58).
- **Chronograph files** (Phase 5): every one of Alan's Xero exports reads, and a timed string proposes its pairing (entry 342);
  checking a proposal needs a sheet shot with its string.
- **Velocity regression** (Phase 5): on both screens since entry 323, in the conditions entered on Ballistics since 329; a
  session's own conditions would need a form (DESIGN NEEDED if wanted); the phone's picture waits for request 50's sitting.
- **Synchronization** (Phase 7): the options paper is `docs/notes/sync-options.md` (entry 324 section 2), for Alan to choose from.
- **iOS** (Phase 8): in a public beta through TestFlight; the device checks need a sitting (50), GroupLab Dev on the iPad request
  61, an App Store release Alan's word.
- **Performance** (Phase 9): the baseline gate and any optimization; waits for planning to say the application has settled.
- **Proof of the 31 "built, not proven" features**: docs/PROOF-CHECKLIST.md, one checklist of material; question 79 on five met.
- **Stores**: Microsoft published, carrying 0.2.0, new submissions on request 66; Google Play past internal test is Alan's call; Test Lab waits on request 62.
- **Deferred on purpose**: the designer's canvas and automatic detection on a bought target; five are recognized by fingerprint.
- **A beta or stable release**: only when Alan asks, after the eight checks in `docs/RELEASE-PLAN.md`.

1. Speed up the "looks like a GroupLab sheet" test (5.4 s on a picture with no GroupLab marks), and make the phone's page failures sheets.
2. The phone pictures of the fingerprint and pairing screens, at the next sitting with a phone.
3. Each nightly's notes need placing in `website/features.json`, or the site stops building (144's went to notFeatures).

## Blocked, and on what

- **The iOS GroupLab Dev upload**: request 61 (its App ID, profiles and record).
- **The phones**: not reachable over adb since 2026-09-30 morning.
- **Entry 170 section 4.4.** Request 9. **Entry 166 section 3.2.** Request 16.

Open requests in `docs/notes/for-alan.md`: **17** (71 switching on entry 357, the store forms; 70 Fenix's report package; 67 TestFlight team distribution off; 66 the Store's cadence; 59 TestFlight groups; 62 Firebase Test Lab; 56 printer scale; 50 the device sitting,
now with a look at the velocity card; 54, 57, 58 at the range; 46 backups on 4 October; 61 GroupLab Dev's Apple
steps; then 33, 9, 16 and 20).

## Open questions

Eight, all in `docs/QUESTIONS-FOR-PLANNING.md`:

- - **79** five built-not-proven lines whose written gates are met (entry 331 section 3)
- **78** the plot's toggles cover the last lines of its key at 1400x900 (found in entry 323; a change to the look)
- **67** the printer check page as grid style 4 (with Alan)
- **51** which hole center GroupLab should report; waits on request 9
- **44, the part still open** the bent-sheet model throws at a point outside the page
- **43** entry 137 names an image safety the desktop does not have; **36** a light installer, measured
- **34** pooling two sheets of one load needs a rule for a pooled group's center

## Builds and the site

- **Last nightly:** 0.2.0-nightly.158 (2026-10-02): entry 352.
- **The site** is live at 6308cf5c, after nightly 153 (a bot's `[screens]` or notes push starts no workflow; publish by hand).
- Crash reports open: none.

## The inbox

`docs/notes/inbox/` holds the entries below. A test reads this line and the directory and fails when
they differ.

**Holds:** none

Inbox files are never committed, so CI sees an empty inbox and this line says none. Waiting locally: 358 (part built, see In flight), 359, and 361 (on hold).

## Things that would surprise somebody who was not here yesterday

- **The chronograph store counts readings from 1**, and accepting a pairing used to store them from 0: the first reading paired with a
  shot was refused, and every other pairing was one reading off. Fixed in entry 323 on both platforms.
- **`docs/notes/external-status.md`** holds TestFlight's and the Store's state, written by their workflows when it changes; read it
  at the start of a run (entries 335 and 336).
- **A worker's commits can be stranded in its worktree.** Entries 325 and 327's detection work and the fold of 324, 325 and 327 sat
  unpushed in `.claude/worktrees/push-324` until entry 342 found them; compare a finished worker's subjects with main before removing it.
- **The Mac packaging can be run by hand**: `gh workflow run package.yml --ref <branch> -f ref=<sha>` proves the signing and
  notarization on a commit without a nightly, and publishes nothing.
- **A UI dump cannot see the phone's camera screen**: its views are native, inside Avalonia's host. The device check reads the screen's
  own `camera.layout` log line instead (`scripts/device-capture-check.py`).
- **A photograph's scale comes from the printer chosen**, where one is, across and down; a scan's own measured scale always wins.
- **`android/GroupLab.Android/` ships** (entry 275): a change to the Android application alone now starts a nightly and is in its notes.
- **The check page is in the library but is not a target**: it counts as no sheet in the README, and a picture of it opened as a target
  goes to the printer check. A value on screen is tappable exactly when it shows a unit (`UnitTap.KindOf`).
- **Inbox files are moved to `C:\Dev\grouplab-trash\<date>\`**, not deleted (entry 222); the Holds line never lists them.
- **Requests for Alan go in `docs/notes/for-alan.md`**, never only in the panel. It holds only open requests and the latest summary
  (entry 317); answered ones are whole in `docs/notes/for-alan-archive.md`.
- **Any push to main cancels the running build and nightly**, and a push headed `[notes] ` then builds nothing, so the last commit of a
  push that should make a nightly must not be one.
- **Entry 317's budget is in force:** one worker by default, the ccusage line once a day in for-alan.md.

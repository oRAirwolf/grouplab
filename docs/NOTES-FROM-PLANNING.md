# Notes from the planning session

Instructions, decisions and measurements coming into the Claude Code session from the planning session, which has read and write access to this repository but cannot see or type into the Claude Code panel.

**How to use this file.** Entries are dated sections, newest first. Act on every entry marked `Status: open`, in order, then change its status to `actioned <date>` in the same commit as the work. Never delete an entry. This file is a log, and the reasoning in it is often the only written record of why something is the way it is.

**How entries arrive, from entry 44 on.**
- **Delivery:** the planning session delivers each new entry as its own file in `docs/notes/inbox/`, named `entry-NN.md`, and never writes this log or any other existing file.
- **The only writer:** the Claude Code session is the only writer of this log.
- **Actioning includes three steps:** folding the entry into the top of the log, setting its status, and deleting its inbox file.
- **Several files waiting:** fold them in ascending entry number, so the newest ends up first.
- **Why:** two writers rewriting one file with no locking overwrote this log once, and separate paths cannot collide.

Questions going the other way belong in `docs/QUESTIONS-FOR-PLANNING.md`.

---

## The archive

Older entries, whole and unedited, one file per month. Nothing here is ever deleted; this log is the
only written record of why much of this project is the way it is.

- [`docs/notes/archive/notes-2026-09.md`](notes/archive/notes-2026-09.md), entries 1 to 256, 255 of them.

---

## 2026-09-28, entry 271: real inches on photographs too (the print scale, measured once and remembered)

**Status: done 2026-09-28** (3a1e70b), sections 1, 2, 4 and 5; section 3 is written up as a study. The wizard, the check page and card detection were held by the entry and are released by entries 272 and 273.

Alan: "I was under the impression that the photos could also check the scale of the prints. I thought this was a design requirement."
DESIGN.md's "Print scale verification" still says that because the fiducials are at known coordinates, GroupLab detects the print scale
"automatically". That is true for a scan only. WHAT-CAN-BE-MEASURED.md (entry 171) explains why a photograph cannot: with no absolute
ruler in the frame, a sheet printed small is indistinguishable from a full-size sheet a little farther away. Fix the wording, and then
close the gap as far as physics allows, with as little work for the shooter as possible.

1. **Correct DESIGN.md** (and anything else that says photos measure the print scale) to match WHAT-CAN-BE-MEASURED.md: the tags measure
   the sheet's shape (perspective, curl, lens) on any picture, and its absolute size on a scan.
2. **A printer profile, measured once and applied from then on.** Print scale is a property of a printer and its settings, and it is
   stable. So:
   - When a **scan** of a GroupLab sheet measures its print scale, offer to save it: "Your printer printed this sheet at 99.2 percent.
     Use this for photos of sheets from the same printer?" The person names the printer (default: "My printer").
   - Or, with no scanner, **type one ruler measurement** (bull 1 to bull 5, the distance printed on the sheet) once, on the desktop or the
     phone, and save it the same way.
   - After that, **photographs are corrected automatically** using the chosen printer profile (a setting, with the last one used as the
     default), and the result says so in one line: "Corrected for My printer's 99.2 percent, measured from a scan on 28 September." With
     no profile, the existing line stays ("measured in the sheet's own inches").
   - A per-sheet override: on any photo, "Measure this sheet with a ruler" corrects just that one.
   - Profiles travel with sessions and between phone and desktop (the session file carries the factor used). Record the method in the
     saved marking, as today.
3. **A known object in the frame, as a study (add to entry 261's study, no build yet):** could a credit card sized card (ISO/IEC 7810 ID-1,
   85.60 by 53.98 mm, a size every wallet has) laid flat on the sheet give the absolute scale from a photo alone, to better than about
   0.5 percent at phone resolutions, given its rounded corners and edge contrast? Measure it on real photos before promising anything.
   Phone depth estimates and autofocus distance are not accurate enough (percent-level at best) and are not to be used for scale.
4. **Tell users plainly** in the tour, the user guide and the capture screen: for real inches from a photo, scan one sheet or measure one
   ruler distance once per printer; GroupLab remembers it.
5. Tests: a synthetic sheet printed at 96.2 percent, photographed, reads true size once the profile is applied; a missing profile reads
   in sheet inches with the line; a profile from a scan and from a ruler agree within their stated uncertainty.

**Update, same day:** Alan asked planning to study easy ways for an average person to verify the print scale: a first-run wizard,
also reachable from Settings, that prints a check page with outlines of known objects (a credit card, a coin) and marks for a ruler or a
digital caliper. The options are with Alan for decision. **Build now:** the printer profile plumbing (items 1, 2's storage and the
automatic correction of photos, 4's wording, 5's tests), and the scan and typed-ruler paths. **Hold** the wizard, the check page and
any card or coin detection until a later entry brings Alan's choices.

## 2026-09-28, entry 270: say plainly, everywhere, that GroupLab works on any target, not only its own sheets

**Status: in part 2026-09-28** (3693025). Not done: section 3's picture of a commercial target marked by hand, which waits on question 66.

Feedback from the reloading Discord, after Alan's announcement: "The whole 'print a target sheet' bit is gonna be a barrier to entry that most people won't bother with. When your competitors can do it from just a picture, why bother?" Alan: this person did not understand that GroupLab works with non-GroupLab targets, and that needs emphasis here and on the site.

The README, the site's home page, the Features page and the tour all lead with "print a GroupLab sheet". That reads as a requirement. Change the message to:

- **GroupLab works on any target.** Photograph or scan whatever you shot on. You set the scale once, then mark the holes by hand today. **Automatic hole detection on any target is the goal** (entry 261, section 7), so say it as a goal, not as a feature.
- **A GroupLab sheet is the fast lane, not a requirement.** On its own printed sheets everything is automatic (scale, every hole, which bull each shot belongs to), and one shot per bull gives large groups.

Changes:
1. **The pitch** in the README (entry 266) and on the home page, for example: "Photograph any target. GroupLab measures the group and tells you honestly what the size is worth. Print a GroupLab sheet and it does everything by itself." Keep it to two short sentences.
2. **The README's three captions under the product shot:** caption 1 names both paths, any target or a GroupLab sheet. The mosaic's first tile reads "Any target, or print a sheet".
3. **The home page and the tour:** a clear "Your own targets" section near the top, with a picture of a commercial target marked by hand (the desktop's marking screen on a non-GroupLab target). Once the phone can do it (entry 258), use the phone too.
4. **The Features page:** move "Targets GroupLab did not print" into the first group, and rename it to something plainer, such as "Any target you already shoot".
5. **The mission,** in one line wherever the pitch is expanded (README "Why it exists", home page): GroupLab also aims to help shooters think in mean radius and confidence rather than extreme spread from a few shots.
6. The consistency audit (entry 267) now also checks that no page implies GroupLab sheets are required.

## 2026-09-28, entry 269: answer to DESIGN NEEDED, Shots Needed to Zero colors

**Status: done 2026-09-28** (246f6c5).

Alan chose **option 1** (the parity canvas, board "ZeroColors"): **"Within 1 click" is amber everywhere, and "Closest click" is teal everywhere**, on the phone and on the desktop. Change the desktop chart to match: the within-1-click line becomes amber and the closest-click line teal, with the legend and table row names written as they are now. Update the website's pictures and the Features page entry through the screenshot job. Remove the question from `for-alan.md`.

## 2026-09-28, entry 268: both devices are on the charger for the night (read now, with entry 263)

**Status: followed 2026-09-28.** GroupLab Dev's black idle screen (f2b1b81) was shown whenever a device was not in use. The Tab S8 Ultra did not answer adb all night.

Alan, about 11:20 UTC (05:20 his time): he is putting the Tab S8 Ultra on the charger now, with Wireless debugging and Stay awake on. The Fold 7 goes on a charging cable with the same settings while he sleeps. If a device is not reachable at first, it should be shortly, so retry every 10 to 15 minutes for the first hour before marking it skipped.

- Use both overnight for the unattended work in entry 263: checking entry 260's camera fix with an adb screenshot or UI dump on the Fold 7 (front and inner screens only if it is unfolded; do not ask), entry 253's phone and tablet screenshots, entry 262's torch-strength readings, and entry 258 and 259's screens as they land.
- Nothing needs Alan tonight. The camera test with the printed sheet waits for the morning. Put its steps at the top of `for-alan.md`.
- The usual device rules: touch only GroupLab and GroupLab Dev, never read notifications, put every changed setting back, and do not unlock or change the lock screen. If a device locks, note it and move on.

### OLED screens: keep them dark whenever they are not in use (Alan, standing rule)

Alan: "both my phone and tablet have OLED screens and I don't like keeping them on at the risk of burn in. I have the brightness on my tablet set to the absolute minimum and will do the same to my phone before I go to bed, but as a general rule, it makes me uncomfortable leaving my screen on constantly."

This applies to every device sitting, not only tonight:
- **A black idle screen.** Add to GroupLab Dev a full-screen, pure black idle view with no text, no status bar and no navigation bar (immersive mode). On an OLED screen, black pixels are switched off, so nothing can burn in, and the device stays awake and unlocked for adb. Whenever Code is not actively driving a device for more than about a minute, bring up that view over adb, for example with an intent extra to GroupLab Dev, the way the existing test extras work.
- **Keep the screen on only for work.** Batch device work so the screen shows real content for as short a time as possible. Never leave a static screen (a result, Settings, the camera) on while waiting.
- **Brightness:** do not raise it. Take screenshots over adb, which do not depend on brightness. If a camera test needs light, that is a morning task with Alan anyway.
- **At the end of the night, or when device work is done,** leave the black idle view up and write in `for-alan.md` that the devices can be picked up. Do not turn off Stay awake or lock the device, since that would stop the next session from reaching it. Alan turns Stay awake off in the morning.
- Record the rule in `docs/ANDROID.md` under the device-testing section, and in the device scripts.
- **Alan must always be able to leave it easily.** Use ordinary immersive mode only, never screen pinning, kiosk or lock-task mode, and never block Home, Back or Recents. A swipe up from the bottom (Home) or the Back gesture closes it as with any app. A single tap anywhere shows, for a few seconds, one dim line of text, "GroupLab Dev idle screen, used for overnight testing", and a Close button at least 48 dp. Tapping Close ends it. Test on both devices that each of these works.

## 2026-09-28, entry 267: no lawyer review, and a standing consistency audit of the README, the site and every asset

**Status: done 2026-09-28** (dac0f4b): no lawyer's review; scripts/consistency.py runs in CI and weekly, and opens an issue on a finding.

### 1. No lawyer, for now

Alan: "I am not going to get a lawyer's review unless this application really takes off. As of right now, I am not willing to pay a lawyer hundreds or thousands of dollars for this."

- **Take out every claim that something is "with a lawyer" or "waits on the attorney"**: README (License, Planned), `docs/ANDROID.md` (line 25, the public Play listing), `DESIGN.md`, `docs/CLAIMS.md` and `docs/claims-backing.json`, STATE.md, the site, and anywhere else the search finds (`grep -rni "lawyer\|attorney"`). Leave the planning history and the archive as they are.
- **The public Google Play listing is no longer blocked by a lawyer.** Its remaining gate is Google's own rule for new personal accounts: a closed test with at least 12 testers for 14 days. Record that in ANDROID.md as the path to a public listing.
- **The GPL section 7 app-store permission** concerns Apple's App Store, and iOS is not planned. Record it as "not in force, and not pursued now; it would be revisited only before an iOS release". Keep one plain sentence on the reason in the License section, without any mention of a lawyer.
- **The effect on outside contributions:** adding such a permission later needs every copyright holder's agreement. In CONTRIBUTING.md, say plainly that contributions are accepted under GPL-3.0 and may later be offered under an added app-store permission, and ask contributors to agree to that when they open a pull request. Keep it one or two sentences.
- The patent and trademark searches (docs/PATENT-SEARCH.md, TRADEMARK-SEARCH.md) stay as they are; only change a line that says a lawyer is reviewing something now.

### 2. The consistency audit, as a standing rule

Alan: "the github readme, grouplab.org website and all of the assets need to be checked every so often to make sure they are up to date and agree with what is currently happening in the project. I should not have to find these oversights because they should be analyzed. It does not have to happen with every build, but it should at least happen once every few days or a week."

Two layers, both automatic:

**(a) The mechanical checks, in CI, weekly, plus on demand.** Add a scheduled workflow (for example weekly, on Monday at 13:07 UTC) that runs a consistency script, and opens or updates one GitHub issue labelled `consistency` listing every finding. It closes the issue when there are none. It checks:
- every platform with a published build is offered on /download/, in the README and in PLATFORM-SUPPORT.md (entry 265);
- the version, date and commit named in the README, on the site and in the release notes agree with the newest nightly;
- counted facts agree everywhere, taken from the source, not typed: sheets in the library (`<!--count-->` markers), platforms, features;
- every feature in `features.json` appears on the Features page, in the README summary and in PHONE-PARITY.md with a phone status;
- every screenshot and generated image is current (entry 253's stale check), including the README's two new images (entry 266) and the donor pack (entry 264);
- every link on the site and in the README resolves (internal links always; external links, allowing for temporary failures);
- the claims in CLAIMS.md still have their backing (claims-backing.json);
- no retired wording remains, from a list kept in the repository: for now "lawyer", "attorney", and any feature named as missing that has since shipped, such as "marking a target by hand is not on the phone yet" once it ships;
- the site's banned-term and IP-address checks.

The script also runs in the ordinary build as a warning, so it can be run locally at any time.

**(b) The judgement review, every three days.** A scheduled planning session (set up by planning, not Code) reads the README, the live site, the release notes, STATE.md, for-alan.md, the inbox and recent commits. It looks for what a script cannot see: a page describing something that has changed, a promise that no longer holds, wording that contradicts a recent decision, a missing mention of something new. It writes its findings as a numbered inbox entry for Code, and tells Alan only when something needs him. Code actions those entries like any other.

**The rule, from now on:** whenever Code changes behaviour, a platform, a decision or a user-facing name, it updates the README, the site, the guides and the assets in the same change, or lists why not in the commit. The audits catch whatever slips through; they do not replace doing it.

## 2026-09-28, entry 266: the new README, as Alan chose it

**Status: done 2026-09-28** (f1d775e). Entry 270 then changed its pitch and first caption.

Alan chose, on the canvas claude.ai/artifact/KeUwJotHu9SvkkGUPoPBSJ (boards "Chosen: B's picture over C's mosaic" and its light-theme twin): **design B's product shot at the top, with design C's six-screen mosaic under it**, and the new order. Build it so the README stays fully automatic.

**Approved by Alan (2026-09-28): "I like the new design. Lets go with it."** One change: **take out the "What GroupLab is not" section.** Alan: "It comes off kind of mean." It is not moved into a fold either. The rules it stated still hold for the project: no OnTarget compatibility, no pseudoscience, not commercial, not an electronic target system. Keep them where contributors read them (CONTRIBUTING.md or DESIGN.md), worded as what GroupLab does rather than as what it is not, and keep the site build's banned-term check. Check grouplab.org for a similar "is not" section, and soften or remove it the same way.

**The top, in order:**
1. `# GroupLab`, then the pitch in bold: "Print a target, shoot it, photograph it. GroupLab measures every hole and tells you honestly what your group size is worth." Then one line: free and open source under GPL-3.0, no account, no ads, no paid tier, on Windows, macOS, Linux and Android. (Keep "GroupLab is a working name" somewhere lower down, for example in License.)
2. Badges: license, the newest nightly, the tests, platforms, and Discord. Use shields.io or GitHub's own badges; the nightly badge must update itself.
3. **The product shot (B),** one image made by the screenshot job:
   - a shot GroupLab sheet (the published sample scan), tilted, behind;
   - the desktop analysis screen in a window;
   - the phone result overlapping at the right;
   - numbered amber callouts 1, 2 and 3.
   Make a dark and a light version (a `<picture>` with `prefers-color-scheme` sources), and alt text that describes it.
4. Under it, a **markdown table** of the three numbered captions, so the captions are real text:
   - 1, print and shoot a GroupLab sheet;
   - 2, on the computer, every hole found, then the group with honest ranges;
   - 3, on the phone, photograph it at the range.
5. **The mosaic (C),** one image, 3 by 2 tiles with their captions inside the image: Print a sheet, Photograph it, Every hole found, Honest numbers, Compare loads, Ballistics and hit chance. Dark and light versions again. Under it, one centred line saying the pictures come from the current build, with a link to the tour.
6. One centred line of links: Download · Website · Features · Tour · User guide · Discord.

**Then the sections, in order:**
- **What it does:** three columns (Reads targets, Honest statistics, Prints sheets), a few lines each, generated from `features.json` groups, with a link to the Features page. This replaces the long link list.
- **Download:** the nightly's number and date, and a single table by platform: Windows (installer, zip), macOS (Apple silicon, Intel), Linux (tarball), Android (APK, Play test). The long notes go into a `<details>` titled "Before you install": unsigned builds, the Mac quarantine command in a code block, the samples, where it keeps files, and updates.
- **What is new:** in `<details>`, generated as today.
- **What is supported, and what is not:** in `<details>`, generated as today.
- **Why it exists:** two short paragraphs.
- **How it works:** the flow, then one line on targets GroupLab did not print.
- **Status and plan:** Status, Planned and Deferred, each in `<details>`, generated as today.
- **For developers:** Built with, Architecture, Building, Repository layout and Test data, each in `<details>`.
- **Thanks:** Unholy, Jylee (Shots Needed to Zero, and helped choose the C3 grid, with Unholy) and Fenix, by the names they gave.
- **License.**

**Rules:**
- Nothing else is deleted: apart from "What GroupLab is not", every sentence of today's README lands in a fold, or on the page a fold links to.
- Keep every generated marker and `scripts/readme.py`. Blank lines inside `<details>` let GitHub render markdown there.
- Both images come from the screenshot job and join entry 253's stale check.
- Render the README with GitHub's markdown (for example `gh api markdown`) in both themes before committing, and look at it.
- The website's home page may borrow the two images later; that is a separate decision.

## 2026-09-28, entry 265: the download page on grouplab.org is missing Android

**Status: done 2026-09-28** (0a81418): Android is on /download/, and the site build fails when a published platform has no download. Section 4 is confirmed with the push that carries it.

Alan noticed that grouplab.org/download/ does not offer the Android build. The Android decisions (entries 198 and 199) said the nightly APK goes on GitHub **and grouplab.org**. So this is an omission, not a decision. The page mentions Android only in the minimums table, and its description still says "for Windows, Linux or macOS".

1. Add Android to /download/ beside the desktop builds:
   - the signed APK (`grouplab-android.apk`), with its minimums (Android 10 or later, arm64, 4 GB);
   - how to install it (allow installing from the browser, and remove any Play copy first, since the keys differ);
   - the Google Play internal test, by invitation, with how to ask for an invitation (the Discord);
   - GroupLab Dev for testers.
   Mark it as an early test build, and state the known camera problem plainly until entry 260's fix ships. Once it ships, the note comes out.
2. Update the page description and meta text, the home page's download button text if it names platforms, and anywhere else on the site that lists only three platforms.
3. Add a site build check that every platform in `docs/PLATFORM-SUPPORT.md` with a published build has a download on /download/, so a platform cannot go missing again.
4. Publish the site as usual and confirm it is live.

## 2026-09-28, entry 264: the donor pack and the Shoot a target page get the new sheets

**Status: done 2026-09-28** (bde8f6b, 1009cd2): the donor pack is one sheet of each style, built from the library, and /shoot-a-target/ offers each sheet or the whole pack.

Alan: "the donor page of the grouplab website may need updating with the new targets, as the default ones were hard to use. Maybe one of each?"

The donor pack (`website/donor/`, shown on /shoot-a-target/) still offers only GL-CF25-LTR and GL-CF25-LTR-D, which Alan and his testers found hard to use.

1. **One of each current style.** Offer one sheet of each style a donor might shoot:
   - the E bull sheet;
   - the C bull sheet;
   - the C bull with the centre dot, if it is a separate sheet;
   - the C3 zeroing grid, mil at 100 yd and MOA at 100 yd (and 100 m where it exists);
   - the standard 5x5 load sheet;
   - Made for your optic, as a link to the Targets screen and the website's generator, since it is made for each person.
   Letter first, A4 beside it. Each sheet gets a one-line description of when to use it, and a picture of the sheet itself (entry 256's rule), not a screen.
2. **Built, not copied.** Generate the donor PDFs from the target library in the site build, as the other sheets are, so a redrawn sheet updates the pack by itself. Add the pack to entry 253's stale check, so the site refuses to publish a pack that no longer matches the library.
3. **The instructions.** Update `docs/VOLUNTEER-PACK.md` and the instructions PDF:
   - how to check the printed size for each sheet style, since the bull 1 to bull 5 measure does not fit a zeroing grid;
   - the photo guidance as it will be after entry 260 (Guided or Manual, the torch, and whatever the check says);
   - what to write on each sheet.
   Keep "Submitting means following the terms on that page."
4. **The page.** On /shoot-a-target/, let the donor download the whole pack or any single sheet. List which sheet is most useful to the project right now. For example, if the corpus lacks photos of zeroing grids or of the C bull, say so, and take the list from entry 261's study once it exists.

Tell Alan when it is live, with the page link.

## 2026-09-28, entry 263: overnight rules, read now, before continuing entry 260

**Status: followed through the night of 2026-09-28.** The morning summary is at the top of for-alan.md.

Alan is going to bed (about 04:45 his time, 10:45 UTC) and wants Code busy all night without waiting on him.

1. **Never stop to wait for Alan overnight.** When a step needs him (a camera test with the sheet, a decision, a secret), write it at the top of `for-alan.md` for the morning. Then **move straight on** to the next piece of work that does not need him. Where a DESIGN NEEDED question comes up, write the line and keep building the parts it does not affect.
2. **The order:**
   - Entry 260 part 1 and the checking logic of part 2. Verify over adb, with no one present, that the words, shutter and Back show over the live preview on every screen and orientation available.
   - Then entry 259's screens, starting with the shared code of entry 258.
   - Then entry 261's study.
   - Then entry 262's device measurements (reading the torch strength characteristics needs no person).
   - Entries 253, 255 and 257's remaining device work wherever it can run unattended.
   Ship nightlies as usual.
3. **Devices overnight:** Alan will say whether he leaves them on the charger, unlocked, with Stay awake and Wireless debugging on. If a device is not reachable, skip its steps and note it. Touch only GroupLab and GroupLab Dev, never read notifications, and put every changed setting back.
4. **The morning summary:** at the top of `for-alan.md`, in five lines or fewer: what was finished, what is waiting on Alan (with the camera test steps if the fix is in), and what is next.

## 2026-09-28, entry 262: torch brightness, a bracketed burst, and combining frames (investigate, then build what the phones support)

**Status: in part 2026-09-28.** Step 1 on the Fold 7 only: Android 16, strength levels 1 to 5, default 1 (docs/ANDROID.md section 16). Not done: the Tab S8 Ultra, which did not answer; setting the level during a session and timing the settling; steps 2 to 4.

After entry 260. Alan: "Can the application adjust the flash's brightness? Can it progressively increase the brightness of the flash and take a burst of photos at different brightnesses and then try and composite them together or do some post processing to get the best image possible?"

**What the platform allows (planning's reading; confirm on the devices):**

- Android 13 added torch strength levels (`CameraManager.turnOnTorchWithStrengthLevel`). That call cannot be used while the camera is open, and CameraX always has it open.
- Android 15 added `CaptureRequest.FLASH_STRENGTH_LEVEL`, which sets the torch strength **during** a camera session. It is reached through Camera2Interop, or through CameraX 1.5's own torch strength API.
- It is optional for phone makers. A device supports it only if `FLASH_INFO_STRENGTH_MAXIMUM_LEVEL` is greater than 1.
- GroupLab supports Android 10 and later, so on older or unsupported phones the torch is simply on or off.

**Step 1, measure, at the next device sitting:**

- Read and record `FLASH_INFO_STRENGTH_MAXIMUM_LEVEL` and the default level on the Fold 7 and the Tab S8 Ultra, with each device's Android version.
- Check whether setting the level during a session works on each.
- Time how long exposure takes to settle after each torch change.

Record the results in `docs/ANDROID.md`.

**Step 2, a bracketed burst, where supported:** one press takes a short series. For example: torch off, then low, medium and high, or as many levels as the device offers, each after exposure settles. It must stay short enough that a hand-held phone does not drift badly. Because the sheet carries its own tags, every frame can be aligned exactly to the others, so small movement between frames does not matter.

**Step 3, choose or combine. Measure each against the corpus and real photos before shipping:**

- **(a) Choose the best single frame** by the quality score (entry 260). This is the simplest option and keeps the measurement on one real exposure.
- **(b) Combine per region:** for each area of the sheet, take the frame with the least shadow and no glare, after aligning them all on the tags.
- **(c) Flash and no-flash processing:** use the torch frame to find and remove the shadows cast by the phone and hand, and the no-torch frame to find and remove glare.

Hole edges and centres must not move because of combining. Measure the centre error of (b) and (c) against (a). Ship a combined image only where it is measured to be at least as accurate, and record which method made each picture in the capture record.

**Step 4, the setting:** under Torch, Auto uses the burst when the light is dim or there are shadows. On and Off stay available. The panel says what was done, for example "Torch at 3 levels, best frame kept".

If the devices do not support strength levels, report that. Fall back to a torch-on and torch-off pair, which works on every phone with a torch, and keep steps 3 and 4 for that pair.

## 2026-09-28, entry 261: STUDY ONLY. Is a self-improving detection engine worth building?

**Status: done 2026-09-28** (0d2dd5c): docs/DETECTION-LEARNING-STUDY.md; entry 271 added section 8. Not done: section 6's server capacity, one read-only ssh command that waits for Alan's approval.

**Do this after entry 260. Do not build the engine.** Alan: "Do you think we need to build an engine that can analyze photos and refine the detection and machine vision models without relying on claude itself to analyze? Is that possible? Dont start making this, we should research if this is worth the effort and if it is even possible."

The output is one document, `docs/DETECTION-LEARNING-STUDY.md`, plus a short summary for Alan in `for-alan.md`. No product code. A throwaway script to take a measurement for the study is fine, as long as it is kept under `tools/study/` and not wired into the app.

Answer these, with measurements where the repository allows and plain estimates, marked as estimates, where it does not:

1. **Where the current detector fails.** Run the existing corpus plus the photos already kept as tests. Add synthetic degradations: hard and soft shadows, a hand's shadow, dim and uneven light, curl and wave warps, blur, noise, JPEG, glare. Report recall, false marks and centre error for each condition against DETECTION-PIPELINE.md's gates (G1 to G5). Which failures could parameter changes fix, and which could not?
2. **The data we have and will get.** Count the labelled holes available today. Every sent target already carries "what GroupLab found" and "what you changed", which are labels for free. Check that the existing consent wording ("GroupLab may use them to test and improve its detection") covers training, and whether "testing only" and "may be published" differ for this. Estimate how many labelled holes, and how much variety, a learned model would need, and how long the project would take to collect them.
3. **The options, from cheapest up:**
   - (a) a standing evaluation harness with synthetic degradation, run in CI;
   - (b) automatic tuning of the classical parameters against that harness, keeping G3's stability rule;
   - (c) a small learned model that only classifies or refines hole candidates, trained offline, shipped as ONNX and run through ONNX Runtime on Windows, Linux, macOS and Android, while markers, codes, geometry and every measurement stay classical and exact.
   For each: effort, what it would gain, app size and speed on a 4 GB phone, how reproducible the results are and how easy they are to explain, and licensing (GPL-3.0; the licences of any pretrained weights and of the training data).
4. **Whether Claude is needed.** Say plainly where Claude is and is not involved. The engine would run and retrain without Claude; Claude Code would only write and maintain the code.
5. **A recommendation**, with the evidence behind it: what to build now, what to build later, and what not to build, plus the conditions (data volume, measured failure rates) that would change the answer.

6. **Fully automatic, and where it runs (Alan, 2026-09-28).** Alan: "can it automatically scan new submissions on my web server? Does it have enough horsepower to handle this or does it need to be done somewhere else? ... I do not want to have to manually pull photos and feed them to claude or the detection engine." Design the whole loop with no human step, and answer:
   - **The flow today.** A submission goes from the receiver to quarantine, then the intake worker rebuilds it from its pixels into `ready`, then the archive worker puts it in the private `grouplab-submissions-archive` releases and deletes it from the server. The natural hook is a new worker that analyzes each rebuilt submission in `ready` before archiving. It compares GroupLab's finding with the person's own changes, keeps only numbers and labels (never republishing the photo), and records the result.
   - **The server's capacity.** Read (read only) the instance's cores, memory, disk use and load: `nproc`, `free -h`, `df -h`, `uptime`, and the shape in `/sys` or cloud-init if shown. Estimate seconds and memory per submission for the CLI analysis on aarch64. Say whether that fits beside pissinhot.com and grouplab.org under systemd limits like the other workers (CPUQuota, MemoryMax, Nice, IO weight), and at what volume of submissions a day it would stop fitting. The 47 GB boot volume holds everything, so say how much room the results and any corpus copy would take.
   - **Arm64.** The server is aarch64. Today's Linux download is x64 only. Check that GroupLab.Cli with its OpenCV native library can be built and run for linux-arm64, and what that takes.
   - **The heavier jobs.** The nightly re-run of every labelled submission and the synthetic-degradation corpus against the current build, and any automatic parameter tuning. Compare running them on the server at night (niced, capped), in GitHub Actions in the **private** archive repository (never the public one, because "testing only" photos must never appear in public logs or artifacts; count the free private-repository minutes), and on Alan's PC (last choice, since Alan wants automation off his desktop). Training a learned model, option (c), would need a GPU or many CPU hours, so say where that would run if it is ever justified.
   - **What stays human.** A measured improvement to the detector still reaches users only through the normal path: code, tests, the nightly. Nothing changes the shipped detector by itself. The loop's output is a scoreboard and proposals that Code reads, for example the build's recall on real submissions by condition, and regressions raised as issues automatically, the way error reports already are.
   - **Privacy and consent.** Analysis stays on the server or in the private archive. Results carry no photo, no GPS and no names. Submissions marked "testing only" are never published, and are used only as the consent wording allows.

Keep it short and readable for Alan: the summary first, the detail after.

7. **A stated goal to study (Alan, 2026-09-28):** on commercial and other non-GroupLab targets, the person sets the scale by hand, and **hole detection is automatic**, on the phone as on the desktop. Study how well the current detector finds holes on such targets, with no declared artwork to subtract: commercial bullseyes, grid targets, colored and splatter targets. Say what it would take to reach usable recall, with the person correcting the misses by touch, and whether the options in section 3 (tuning, or a learned hole classifier) are what gets it there.

## 2026-09-28, entry 260: the camera test failed. Fix the capture screen first, then guided and manual modes

**Status: in part 2026-09-28.** Part 1 and part 2 done (c5cc04d): the native capture screen shows its words, shutter and Back over the live preview, Guided and Manual, the quality bar, the torch on Auto, and Feedback B checks every picture; checked over adb on the Fold 7's cover screen, and the check now reads the screen's own layout log. The log showed why it failed: 640 by 480 frames too small to read the codes. Paper that is not flat is registered through every marker (19a6d67), and the study measured the rest. Not done: the inner screen and landscape (the Fold was never opened), the camera test with Alan, shadow normalisation, the torch pair, the check of the score against test photos, and an off-white background test case.

**Do this before everything else in the inbox**, including entries 258 and 259. Request 50's sitting is on hold until this is fixed.

### What Alan saw (nightly 118, GroupLab Dev, Fold 7, 10:05 to 10:10 UTC)

Screenshots: `C:\Dev\grouplab-local\planning\camera-0928\`. They are local only; never commit them, since they show things on Alan's counter.

1. **Front (outer) screen, portrait:** after pressing Take a picture, the camera preview filled the screen above the bottom bar. There was **no instruction, no Take button, no zoom and no Back**, only the preview and the app's bottom bar. The shutter **never fired**, whatever the distance, focus or angle, with or without caliber and distance entered. Leaving for Sessions and coming back showed "Move back." at the top for a moment, and it vanished as soon as the camera started again.
2. **Inner screen, portrait:** the preview covered the **whole display, bottom bar included**. There were no controls and no words.
3. **Inner screen, landscape:** the bottom bar came back, but again there were no words or controls, and no shutter.
4. The sheet was a GroupLab 5x5 load sheet (GL-R0T0-384Z-HRBE-M0EW), flat on an off-white counter, in ordinary kitchen light, and fully in view in every screenshot.

### Likely cause (confirm, do not assume)

`CameraView` puts CameraX's `PreviewView` in a `NativeControlHost` that spans all three grid rows. The instruction border and the button row are Avalonia controls drawn in the same place. On Android a native view hosted this way is drawn **above** Avalonia's own surface, so everything Avalonia puts over the preview is hidden. The words exist ("Move back." shows before the preview surface attaches), but the preview covers them. On the inner screen the host is also sized over the bottom bar. The shutter not firing is a separate question. Read the entry 255 camera log for this run (`adb logcat`, and the app's own log) to learn whether frames were judged, what they said, and whether "Ready" ever came three frames in a row. Report what the log shows.

### Fix, part 1: the screen works

- **Never draw Avalonia controls over the native preview.** Either give the preview its own middle row, with the words above it and the controls below, no overlap, or draw the words, the outline and the buttons as native Android views in the same layout as the `PreviewView`. Choose whichever is reliable on the Fold 7 (both screens, both orientations) and the Tab S8 Ultra. The preview must never cover the app's bottom bar, or hide the way back.
- **Every capture screen always has a shutter button and a way back,** visible and at least 44 px, in every mode and on every screen size.
- **Add a device test** that fails if the words or the shutter button are not visible while the preview runs, using a screenshot or UI dump over adb on the Fold 7 (front and inner, both orientations) and the tablet. That test would have caught this before the sitting.

### Fix, part 2: Guided and Manual modes (Alan asked for this)

Alan: "there should be an option to manually take an image with a shutter button and the application should determine whether the photo was acceptable and if not, say why. The application should have a 'guided' and 'manual' mode for taking photos and both should analyze the image afterwards and give a score or feedback on the image."

- **Guided:** what exists now, working. One instruction at a time over the preview, and the shutter fires by itself after the ready frames. The shutter button stays available to take it early.
- **Manual:** no automatic shutter. The person frames it and presses the shutter. Guidance may still show as a hint, but it never blocks.
- The mode is chosen on the capture screen and remembered. **Guided is the default** until the camera tests show it works.
- **Both modes check every picture afterwards,** with the same checks the guidance uses plus the analysis's own: markers found, codes read, angle, sharpness and blur, lighting and shadow, glare, how much of the sheet is in the frame, and resolution at the bulls. The result is a **score or verdict** (verdicts as in "Tolerance comes first" below) with **the reasons in plain words**, each naming what to change: "Hold the phone flatter: the sheet is tilted 24 degrees", "A shadow falls across bulls 11 to 15", "Move closer: the bulls are too small to measure well". The person can then use the picture, or retake it. A picture chosen from the phone's files gets the same check.
- **Design: Alan chose Capture B and Feedback B** (canvas claude.ai/artifact/GpEU9qYHHkBymNMJAN5HqD, boards "Capture B" and "Feedback B").
  - **Capture B:** camera-app style. The camera fills most of the screen and the app's bottom bar is hidden while capturing. At the top sits a floating panel with a round Back button and the instruction. Alan asked for **more live feedback** in that panel: focus (sharp or not), light (good, dim, or uneven, and whether the torch is on), **how many tags (markers) are read out of how many**, and **how many QR codes are read out of how many**. At the top right is a torch button (Auto, On, Off), and a small level sits near the bottom of the camera. Under the camera is the shutter: a big round button whose amber ring fills in Guided. To its left is a photo picker, to its right the lens. The words GUIDED and MANUAL sit under the shutter as the mode switch. Every part of this must actually show over the live camera (part 1).
  - **Feedback B:** the photo large, with each problem outlined and numbered on it. A panel below gives the verdict word, the numbered notes, a line summing up what was fine (focus, light, tags read, QR codes read, torch used), and "Take it again" and "Use this picture" side by side. **Most notes should say what GroupLab corrected**, for example "Shadow across bulls 11 to 15, evened out: check those 5 holes if you like", rather than asking for a retake.
  Build part 1 and the checking logic now, then these two screens.

### Tolerance comes first (Alan, 2026-09-28)

Alan: "We need to make sure that the application is tolerant of shadows, lighting, and paper that is not perfectly flat. Most people are not as anal as a computer or even myself and this application needs to work for people under less than ideal conditions."

- **Retake is the last resort.** Ask for one only when GroupLab truly cannot measure: markers or codes unreadable, the sheet cut off, or blur too heavy for the holes. Shadows, uneven light, moderate tilt, and curled or wavy paper are **handled in processing and reported**, not refused. The verdicts are, for example, Good, Good with notes, and Retake. Holes affected by a problem are flagged in the review queue, not silently dropped.
- **A quality score and a colour bar (Alan, 2026-09-28):** "I want the app to want good pictures but it should be able to handle less than ideal pictures." Every picture gets a score from 0 to 100 and a bar that runs from red (Retake) through amber (Usable) to green (Good), with a marker at the score. The number and the band word are always written beside the bar, so colour is never the only signal. The message leads with the judgement and then what would help, for example "Good enough to measure. Here is what would make the next one better: ...". The same bar shows live in Capture B's panel, as a forecast of the picture you would get now. Define the score from the measured parts (tags and codes read, sharpness, light evenness, glare, tilt, the warp's residual, bull size in pixels), document the formula in MOBILE-CAPTURE.md, and check against the test photos that it agrees with how well each picture actually measured. The canvas boards "Capture B" and "Feedback B" now show the bar.
- **Shadows and uneven light:** normalise against a locally modelled paper level (DETECTION-PIPELINE.md already calls for this). Measure recall and hole size with hard shadows, soft shadows, a hand's shadow, a phone's shadow, dim light, warm light and mixed light.
- **Paper that is not flat:** the 30 or more tags on a GroupLab sheet allow a local, piecewise or mesh warp instead of one homography. Use them, so that curl, a fold or a wave is followed. Report the residual after the warp, and flag areas where too few tags were read.
- **Torch and flash (Alan's idea):** follow MOBILE-CAPTURE.md L1 (torch at low power, not a burst). In Auto, turn the torch on when the metered light is dim or when shadows are detected, and say so in the panel. The person can force it On or Off. Because the torch sits beside the lens, it fills in the shadow of the phone and the hand. It can also cause glare on glossy paper, so check for a hot spot. Where it helps, take a torch and no-torch pair in one press and keep the better frame, or use both. Record in the capture record whether the torch was used.
- **Measure it.** Add synthetic degradations to the test corpus (shadows, uneven light, curl and wave warps, blur, noise, JPEG), plus real photos. Report recall and centre error under each condition, so "tolerant" is a number and not a claim. Alan will stage bad conditions on purpose, and will ask other users for photos (through the existing Send your targets consent). List the conditions most wanted, so he can ask for them.

- The camera log from entry 255 keeps recording each instruction, the mode, the score and the reasons, so a camera test can be read afterwards.

### Then

Ship it in a nightly, and post a READY line for a new camera test. The steps are the same as before, plus one Manual picture and one deliberately bad Manual picture, to see the feedback. Add an off-white or low-contrast background case to the tests (entry 259). Then continue with entries 253, 255, 257, 258 and 259. The request 50 sitting resumes with the camera working.

## 2026-09-28, entry 259: Alan's choices for the phone parity screens (entry 258)

**Status: done 2026-09-28**, screens 1 to 7 (f2b1b81, faba30d, fbf718e, ffc5c21). Not done: trying each on the devices at the next sitting.

Alan chose **A for all six** and **approved the scan proposal**. Canvas: claude.ai/artifact/WGFnJWf6dKpBF46taq7m7u. Every number drawn there is a sample for the layout, not a calculation. Every screen is look B (entry 246), with the desktop's colors and IBM Plex, and works in both themes. The descriptions below are the build spec, so the canvas is not needed to build them.

1. **Full figures, A ("tiles, then sections you open").** The result keeps look B's four tiles at the top: Mean radius (the amber headline, with the size on paper beneath), Extreme spread, CEP 50, and Center from aim. A units switch (in, MOA, mil) sits at the top right. Below the tiles is the plot card, with chips under it: CEP 50, CEP 90, CEP 95, CEP 99 and Sheet. They turn the circles and the photo on and off, and are drawn dotted, solid, dashed and short dashes as on the desktop. Then come the sections, as cards:
   - "All figures", open by default: Group width × height, CEP 90, CEP 95 and CEP 99 with their ranges, the mean radius range, and the zero correction.
   - "Advanced": a CEP of your own percent (a number field), sigma, and the across and up-and-down strips with their sentence.
   - "Bull by bull".
   - "Shots Needed to Zero", which opens the page in item 3.
   - "Full CEP table and the fitted ellipse".
   Every figure label and glossary word is dotted-underlined. **The explanation sheet:** tapping one opens a bottom sheet with the name, the value, the plain explanation, and an amber box headed "What N shots can say" giving the range and what fewer shots would do. It closes with Close. The same sheet serves glossary words (sigma, CEP, MOA, bull), in place of the desktop's hover.
2. **Aimed bulls, A ("tap the bulls on the sheet").** A page titled "Bulls you fired at", reached from the result, with the line "Tap each bull you fired at. Tap it again to take it out." It shows the sheet's own layout with every bull as a tap target of at least 44 px, numbered. A chosen bull gets an amber fill and ring, and its state is announced to screen readers. Under it are chips: Every bull, Clear, Whole row and Whole column (the last two take one tap on a bull). A live count reads "N of M bulls chosen, S shots". At the bottom: "These ones" (amber, primary) and "Not said: nearest bull for each shot". The typed form from the desktop (1-10, rows 1-3, columns 2-5) is left out of this screen unless a sheet has too many bulls to tap. In that case, offer it under the chips.
3. **Shots Needed to Zero, A ("its own page").** Reached from the result's section and from the zeroing sheets. At the top, a card holds:
   - the click value as chips (0.1 mil, 1/4 MOA, 1/8 MOA, Other);
   - "Your spread", taken from the open result, with its shot count;
   - "Adjusting" as chips (Both ways, Up and down).
   Then a table: columns 90, 95 and 99 in 100; rows "Within 1 click" (amber) and "Closest click". Under the table, a chart of chance against shots on a log axis, both curves, with a legend. Then one plain sentence on why the closest click takes so many more shots. At the bottom, the seed and the number of draws, and a Calculate again button. It uses the same core as the desktop, with progress and Cancel within the entry 252 budget. Keep the credit to Jylee wherever the desktop has it.
4. **Compare loads, A ("one figure at a time").** Reached from Sessions: choose two or more sessions, then Compare. A figure picker as chips (Mean radius, Extreme spread, CEP 90, Velocity SD where there is a chronograph string). A card draws each load as a dot with its range on one shared scale, in the load's color. Use the desktop's compare colors, telling loads apart by lightness as well as hue. Beneath the chart is the load list (color, name, value, shots). Then an amber card headed "What these shots can tell", with the desktop's plain verdict, then "Add or take out a load".
5. **Ballistics, A ("its own tab").** The bottom bar grows to five places: Capture, Sessions, **Ballistics**, Targets, Settings. Ballistics uses a trajectory-arc icon. The page has a large title, then three summary chips (Rifle, Load, Air), each opening its edit form. The desktop's Setup fields go there, grouped as on desktop concept B. Then tabs:
   - **Dope:** To and every fields, and a table of range, elevation in the person's unit, clicks, wind for 10 mph, and clicks. The last row is highlighted. On a phone turned sideways, add drop, velocity and energy. Print the card from the menu.
   - **Trajectory.**
   - **Hit chance:** the desktop's Hit probability view (entry 247). It has target chips (10 in plate, 2 MOA, Other); range, wind and their uncertainties; and your group, from the open result or a saved one. The result is a large amber percentage with one sentence, then a chart of hit chance by range with the chosen range marked, then "What costs the most" as bars.
   A result's section list also links to Ballistics with that group carried in.
6. **Pooling, A ("the set as a checklist").** A page for a set from Made for your optic. The title reads "Your set, N of M read". Pooled tiles (mean radius so far, extreme spread, shots N of total). Then a card listing every sheet: a thumbnail, "Sheet k", and either the shot count and time, marked read, or "still to read" in amber with a dashed empty thumbnail. Then the line "Sheets can be photographed in any order. Each one names itself from its code." At the bottom: "Photograph the next sheet" (primary) and "See the pooled group". Photographing any sheet of the set brings the person back to this page updated.
7. **Real inches from a scan: approved.** On a result opened from a scan, a teal pill reads, for example, "Printed 1.4% small, every size corrected". Tapping it opens a card: a plain sentence; the markers' distance as drawn and as printed; the correction factor in amber; and a note that a phone photograph cannot measure this and scans can. Check that note is true of GroupLab's method before shipping, and reword it if not. Figures on that result say "corrected".

**Order:** as in entry 258. The shared-code parts first, then these screens in the order 1 to 7. Each ships in its own nightly and is tested at the next sitting. Record the choices in `docs/PHONE-PARITY.md` and in `docs/ANDROID.md`'s design section. Anything these descriptions do not settle visually goes to `for-alan.md` under "DESIGN NEEDED:".

**Camera test note (request 33, running now):** Alan is doing the camera test on his kitchen counter, which is an off-white surface. A white sheet on an off-white counter has little contrast at the sheet's edge. When reading the results, record the surface. If the words or the automatic shutter behaved differently because of it, say so and handle it: the detector should rely on the markers, not on the paper edge, and a capture tip may be needed. Add a low-contrast background case to the camera tests.

## 2026-09-28, entry 258: the phone does everything the desktop does

**Status: in part 2026-09-28.** The shared code, docs/PHONE-PARITY.md and the site check that every feature has a row are in; items 1 to 7 reached the phone as entry 259's screens (f2b1b81 to ffc5c21), and real inches from a photograph came with entry 271. Not done: marking targets GroupLab did not print by touch, CSV through the share sheet, large sheet advice and tiled printing, opening a picture shared from another app, and the device test of each group.

Alan: "The mobile application should be able to do everything that the desktop can except for features that require a desktop or are completely unsuited for a mobile device."

He named these first, in this order:

1. **Saying which bulls you aimed at** (by rows, columns or a list), so each shot is measured from its own bull.
2. **Shots Needed to Zero** (suggested by Jylee). There is a real screen from the result, not only the GroupLab Dev timing test. Use the same core code, the same seed and trials display, and Calculate again. Keep to the entry 252 section 4 budget: about 2 s on a 4 GB phone, with progress and Cancel.
3. **Full figures.** Everything the desktop analysis shows: CEP 50, 90, 95 and 99 circles on the plot with their ranges, a CEP for any percent under Advanced, sigma, the zero correction in clicks, and every other figure the desktop lists. Every figure can explain itself in plain sentences, including what a small shot count does to it. Keep look B's tiles for the headline figures and put the rest in a section the person can open.
4. **Compare loads,** with each load's range drawn, from the phone's saved sessions.
5. **Ballistics and hit probability,** the same solver and the Hit probability view (entry 247).
6. **Pooling the sheets of a set** from Made for your optic into one group, read in any order, and saying which sheets are still missing. On the phone this should work sheet by sheet at the range: photograph one, see the pooled group so far and what is still missing, then photograph the next.
7. **Real inches from a scan,** when the picture opened on the phone is a scan (from the phone's files, Google Drive and so on). Also use any printed-size information a phone photo gives, where the markers allow it.

**Then everything else, under the same rule.** Everything else the desktop does comes to the phone, except what needs a desktop or clearly does not suit a phone. That includes:

- Marking targets GroupLab did not print: bulls placed by touch, a scale drawn at each bull, and templates.
- Shots out and in as CSV, through Android's share sheet and file picker.
- Large sheet advice and tiled printing with cut lines, through Android's print dialog.
- Word explanations by tap instead of hover.
- Opening a picture shared into GroupLab from another app, and pasting one. This is the phone's version of drag and drop.

Leave out anything that truly depends on a desktop (for example, building for the Mac, or the desktop's own updater when Google Play updates the app). List each item left out, with a one-line reason, in the new document below.

**How:**

- **Shared code.** Move what the desktop screens compute into shared code wherever it is not already there, so the phone and the desktop cannot give different numbers. The same tests cover both.
- **Design.** Build to look B (entry 246) and the desktop's design language: sections and cards, and the one main action in amber. Lay things out for touch, not as shrunken desktop screens. **Standing rule from Alan (2026-09-28):** any design decision that can be settled by seeing it (layout, interface, how a screen is organized, what goes where) is not made by Code. Planning makes concepts in Claude Design and Alan picks one. For this entry, planning is making phone concepts now for all seven features. Build the shared-code and engine parts first, while the concepts are pending. Build each screen once its concept is chosen; the choice will arrive as a new inbox entry. From now on, whenever Code meets a visual decision that no chosen concept covers, add a line to `for-alan.md` under a heading "DESIGN NEEDED:" that names the screen and the question. Keep working on other things meanwhile. Planning turns each one into concepts.
- **Big screens.** Use the side-by-side layout on the tablet and the unfolded Fold 7 where it helps, and test the unfolded Fold (entry 257).
- **Parity document and check.** Add `docs/PHONE-PARITY.md`. It lists every feature in `website/features.json` as on the phone, coming (with its stage), or left out (with the reason). Make the site build fail when a feature has no Android status. From now on, a new desktop feature is not finished until it is either on the phone or listed as coming or left out.
- **Features page.** As each feature reaches the phone, add Android to its platforms in `features.json` and give it a mobile picture of that feature itself (entry 256). Update the platform statements in README, `PLATFORM-SUPPORT.md` and `ANDROID.md` ("marking a target by hand is not on the phone yet" and similar lines).
- **Order and size.** Work in the order above. Ship each feature in its own nightly when it is ready rather than holding them all. After each group, test on the Fold 7 and the Tab S8 Ultra in the usual sitting, with the READY line.
- **Performance.** Measure anything heavy against the 4 GB phone budget, and scale memory to the device (entry 240).

Tell Alan in `for-alan.md` the planned order with rough sizes, then report as each feature lands.

## 2026-09-28, entry 257: the Fold 7 can be opened for the big-screen tests

**Status: standing.** Nobody could open the Fold 7 overnight, so no big-screen test ran. Not done: section 2's inner-screen pictures and fold-unfold check, and section 3's record, at the next sitting with the Fold open.

Alan: "I can open the fold if it wants to use the larger screen for any tests"

1. **During this sitting (request 50, nightly 118):** if any test, screenshot or check would be better on the Fold 7's inner (unfolded) screen, ask for it. Put a plain step at the top of `for-alan.md` under the READY line, for example "Open the Fold 7 now and leave it open, unlocked, on the charger." Put the matching "you can close the Fold 7" step there when it is no longer needed. Group the unfolded work together so Alan opens and closes it once, not over and over.
2. **Worth doing unfolded:** the phone screenshots for entry 253 (inner screen in portrait and landscape, both themes, beside the outer screen ones), the design B layout at the inner screen's width, the Targets preview words, the Ballistics and Hit probability views, and the fold and unfold change itself (the app keeps its place and state when the Fold is opened or closed mid-task). Only do the camera test (request 33) unfolded if it adds something. The folded Fold 7 stays the main phone case.
3. **Features page:** where the Desktop/Mobile toggle (entry 249) shows phone pictures, the unfolded Fold 7 can supply the large-phone pictures. Record which screen each picture came from in `docs/figures/SCREENSHOTS.md`.
4. **Later sittings:** the unfolded Fold 7 is a standing option. Ask for it the same way whenever a test needs the larger screen.

The same device rules apply as before. Only GroupLab and GroupLab Dev are touched. Lock-screen notifications are never read. Every setting changed is put back.


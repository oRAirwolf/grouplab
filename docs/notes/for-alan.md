**Open: 19.** Most urgent: **56**, your printer's scale from one scan (ten minutes), and turn off the photo correction meanwhile. Then **50**, the camera test of 33 inside it. **54** the store-bought target whenever suits. **46** waits until Sunday 4 October. **61**, the Apple steps for GroupLab Dev, whenever suits. **62**, Firebase Test Lab, ten minutes whenever you choose. **57** and **58**, red bulls and store-bought targets, at the range. Then **33**, ten minutes with the Fold 7. Then 9, 16 and 20 (rewritten: eight sheets, and a page to print).
Running from the terminal since 03:06 UTC, 4 October, at 62% of the week (a live reading from the status line); working on entry 362 (store-bought targets), then the rest of 358.
Done, from entry 361: the hook lets a block already under way finish between 85% and 88% while its finishing flag is fresh, and stops everything at 88%.

**THE WEEKLY BUDGET, ENTRY 360** (not a request): the hard stop is in place. A check runs before every action Code takes, workers
included, and refuses to go on once the week reads **85%** or more; I stop starting anything at **80%**. The last real reading of your
week is **60%**, from Claude Code's own record at 09:55 UTC on 3 October (window resets 8 October 02:00 UTC). The status line that
should refresh that figure every minute does not run in this VS Code session (no reading appeared), so the number cannot be kept
fresh here; as entry 360 says, I am finishing only the step in hand and starting nothing new. The worker on entry 358 stops at its next
commit. To get live readings, start Code from a terminal (`claude` in C:\Dev\grouplab), where the status line runs; the stop works
either way.

**FOR FENIX, HIS TWO PHOTOS** (entry 354, not a request; to pass on): "Thanks for the two photos, they found three real problems. On the
load development sheet GroupLab marked 32 spots for your 25 shots: the torn top corner and the curled top edge where the board showed
through, the printed 'Print at actual size' line along the lifted bottom edge, and the bull numbers '25' and 'S2', which the photo's
slight blur made look like small holes; on some readings the square marker beside the hole between bulls 12 and 13 was counted as a
second shot. GroupLab now ignores marks in a photo's margin unless they are clean round bullet holes, knows its own printed numbers and
words, and no longer counts a marker's edge as a shot: on your photo it finds 24 of your 25 holes and nothing else. The one it still
misses, on bull 23, touches a marker; add that one by hand. The '49 of 29 need review' counter counted questions instead of shots; it
now counts shots. The diamond sheet failed for a different reason: you made it with the target generator and printed it without saving
it, so GroupLab had no copy and refused it, although the codes on the sheet read fine and hold the whole design, and it wrongly said the
codes could not be read. GroupLab now reads the sheet's design from those codes and finds all 25 holes; check a few white centres it
marks on the wavy left side before you accept. Both fixes are in nightly 162, published 3 October."

**WHAT THE SENDING SETTINGS DO, AND WHAT HAPPENED TO FENIX'S** (entries 354 and 355, not a request; to read and pass on)

*What each setting sends, and when.*
- **Send every target automatically**: a target goes by itself when you press **Accept and analyze**, at the level you chose. **Ask me
  each time**: a short panel under the figures offers it then, and nothing goes until you press Send. **Never**: nothing goes. A
  picture GroupLab could not read, or one you leave before Accept and analyze, is never sent, whatever the setting.
- **Testing only**: used to test and improve detection, never published. **May be published**: may also be published in GroupLab's
  public test data and research, only after it has been looked at.
- **A target** carries: the image with its pixels untouched and every location, date, time and serial number taken out; what GroupLab
  found before you changed anything; what you changed (every mark moved, added, deleted, split, reassigned or excluded); what you told
  it (caliber, distance, rounds fired, paper, backing); the figures and how the scale was set; that session's log with file names
  reduced to a code; and the version. A JPEG or PNG goes as it is; anything else (an iPhone's HEIC, say) goes as a lossless PNG, and
  one still over 30 MB is not sent and says why. One that cannot go now is kept and tried at each start for seven days.
- **Send error reports automatically**: a report goes when GroupLab hits an error or closes without shutting down. It carries the
  version and the system, the error and where in GroupLab it happened, and the names of the last few things done. Never a photograph, a
  file name, a location, or anything typed. A sheet that simply will not read is not an error to GroupLab, so it sends no report.
- **Never in either**: a location (never read at all), your name (only a credit you type yourself, on the website), or a file path.

*What happened to Fenix's.* Nothing from his desktop reached the server: the private archive for October holds only your own app
submission of 1 October and his website upload of the two photos, and no error report came from him. The likeliest reason, from the
code: a target goes only after **Accept and analyze**, and he stopped at the review of the 5x5 sheet (29 marks for 25 shots) and the
diamond sheet never read, so there was nothing to send, and a sheet that fails to read is no error. That cannot be proven without his
log; request 70 asks for it. The words in GroupLab's settings and on the "What GroupLab sends" page now say Accept and analyze plainly.

*What happens to a submission afterwards.* The server's receiver puts it in quarantine; within minutes the intake worker rebuilds the
picture from its pixels (dropping everything else) and moves it to a ready folder; the archive worker copies it into the private
archive on GitHub (never public), proves the copy, and deletes it from the server. Your pull also copies it to this computer. One the
server refuses is deleted after 7 days, one never archived after 60. **What Code does with them today:** reads them by hand when an
entry names them, and adds them to the scoreboard as test cases (Fenix's two are being added now). **Not built yet:** the automatic
comparison of GroupLab's marks with the person's corrections (DETECTION-LEARNING-STUDY.md section 6 describes it as planned).
**Published:** nothing so far; a "may be published" target could be, by Code with your approval, after a look at it.


**URGENT, FENIX'S REPORT ON THE IPHONE** (entry 353, not a request): on TestFlight builds 157 to 159, taps on the iPhone did nothing
while a box had been typed in: Continue and Done for the caliber and distance, Take a picture, Choose photo, and a suggested caliber
picked the wrong one. **What broke:** the fix for your friend's floating keyboard bar closed the keyboard the instant a finger touched
the screen, so the page moved before the finger lifted and the button under it never saw the tap. **Why the checks missed it:** every
automatic check pressed buttons directly, never the way a finger does, down then up, so the page never moved between the two. **Fixed:**
the keyboard now closes only after the finger lifts and the button has done its job; tests now tap the way a finger does, and they fail
on the old code. **Install build 160 from TestFlight:** both groups have it as of 2 October 19:51 your time; Fenix and Unholy can update now. Real taps on Apple's iPhone simulator and an Android emulator now check this with every nightly, and all pass.

**TONIGHT'S LIST, 2 OCTOBER** (entry 352, not a request): done, one worker. Reading a target is now about twice as fast on the computer
and a quarter faster on a phone, every measurement exactly as before. Find holes no longer takes most of the Eze-Scorer's printed
numbers for shots (6 false marks on a clean sheet down to 2, the logo's letters). A damaged or oversized chronograph or fingerprint file
is refused with a plain sentence; a damaged spreadsheet could close GroupLab before. Every phone screen is checked on Apple's iPhone
simulator with each nightly, and now on an Android emulator too (its first run is being repeated after a fix). **One draft to read,
whenever suits:** "Printed numbers are not holes" (website/research/printed-numbers-are-not-holes.md, unpublished): why a scoring
target's bold 6s fooled the hole finder and what tells them apart, from your blank scans and made-up holes only. Say "publish it" or what
to change. All of it is in **nightly 158**, and both phone sweeps pass.

**ADD A STORE-BOUGHT TARGET, AND CHANGING A PAIRING** (entries 348 and 351, not a request): both built as you chose, concept A,
in **nightly 156**. On the phone and the computer, Targets has **Add a store-bought target**: five steps, from a photo of the blank
target to a small file of its fingerprint, name, size and bulls (never the photo), saved or shared for you to send. That nightly is the
one to try your Cabela's and Walmart targets with; the phone gets it from GroupLab Dev's own update, Google Play's test, or TestFlight
once Apple has it. Changing what a chronograph reading goes with is now a row per reading with one mark to tap, the same choices on
both. Also in nightly 155: a target added to the library later reaches every copy without a new build.

**YOUR FRIEND'S TESTFLIGHT FEEDBACK** (entry 350, not a request): retrieved; you did not need to do anything. It came in on 1 October
at 12:50 UTC from an iPhone on build 150, both on the Targets screen, and was filed privately an hour later (issues 15 and 16).
1. "The Done button and bar ended up floating over the app and clicking done did nothing": after the number pad closed, the bar that
sits on it stayed behind in the middle of the screen. 2. "Keyboard doesn't go away making the targets": the number pad stayed up and
nothing closed it. Both came from GroupLab waiting for the iPhone to say when the keyboard opened and closed, which it did not always do.
Now a tap anywhere outside a box closes the keyboard, Done always closes it and clears the bar, and the bar removes itself as soon as no
box is being typed in. **Correction, 2 October 10:40 UTC:** it did not reach TestFlight in nightlies 154 to 156, because one line of the fix did not build for the iPhone and that build failed by itself while the rest published. Fixed: **nightly 157** built for the iPhone again and carries it to TestFlight, with everything 154 to 156 had. Also changed: the regular check for new
feedback looked back only an hour but GitHub runs it every few hours, so it now looks back a day (nothing is ever filed twice).

**YOUR USAGE** (entry 317, not a request): 1 October ended at 0.65 billion tokens with two workers; 2 October so far 0.04 billion,
this session and one worker.

**YOUR OVERNIGHT LIST, 1 OCTOBER** (entry 342, not a request): done apart from one item. In nightly 152: GroupLab recognizes the five
store-bought targets you scanned, names them, places the bulls and sets the scale with a warning to check it, and asks 6 or 8 inch for
the Shoot-N-C only when the picture cannot tell. In the next nightly: the phone reads a sheet's codes in about half the time, with every
measurement unchanged; every box on the computer has a name a screen reader reads, the keyboard reaches and presses every button, and
the Targets screen fits a small window. An imported Garmin Xero string now proposes which shots were this group from its own timing,
and says why. The download page names the Store's version (0.2.0). `grouplab target-reference` makes a fingerprint from a photo of a
poster on the computer; its screens wait for planning's drawings (69). **Next, and what each waits on:** the phone sweep on the
emulators in CI (nothing; first tomorrow); the fingerprint and pairing screens on the phone (planning's concepts, 68 and 69); where a
newer fingerprint library is published (planning, question 80); a Store submission (your cadence, 66); proving the 31 features (one
sitting, docs/PROOF-CHECKLIST.md, now one ordered list). The day stopped at entry 317's share.

**TESTFLIGHT FEEDBACK** (entry 326, not a request): thank you for the token. Every tester's screenshot, comment and crash is now
filed privately as it arrives, and I fix each in turn; Unholy's two are filed and linked to their fixes below.

**UNHOLY'S TESTFLIGHT REPORTS** (entry 328, not a request): both fixed, in **nightly 147**; Unholy can retest on it once it reaches
TestFlight. 1. "Keyboard covers the fields": on the iPhone the number pad hid the distance on the caliber question, and it has no return
key. Now, on every screen with a box to type in, the page moves up so the box and its Continue button stay above the keyboard, a bar on
the keyboard says Next (to the next box) or Done (keeps what was typed and closes it), and tapping outside a box closes it too.
2. "This text is nonsense": the note under a 95 picture said the markers agree "only to 0.005 in, where a flat sheet gives 0.005". The
difference was too small to matter, so that note no longer appears unless the sheet is curled enough to cost the score, and then it
says "The sheet looks slightly curled. GroupLab allowed for it; flattening the sheet would measure a little better." Two other notes
that could print the same number twice were fixed the same way.

**WHERE TO LOOK** (entry 323, not a request): **Velocity and the vertical** is built as you chose, Desktop B and Phone B. On the
desktop it is the block under the group's figures; add a group's chronograph readings on Ballistics, Chronograph, and it shows how much of
the vertical is velocity, with the amber band on the group picture and its Velocity band switch beside CEP and Extreme spread. On the
phone it is the card above All figures, with Add readings and a Velocity band chip under the plot. It is in the next nightly; the phone
check rides along with request 50's sitting.

PUBLIC BETA AND THE STORE (entries 335 to 337, not a request): Apple approved the Public Beta, and both TestFlight groups now get each
build by themselves: build 148, which carries Unholy's two fixes, is in GroupLab Team and was added to the Public Beta and submitted
automatically. Unholy can install 148 from TestFlight now to retest the keyboard and the curl note. GroupLab is in the Microsoft Store
(request 38 closed). With your approval, the TestFlight invitation and the "Get it from Microsoft" badge are on the download page, in the
README's download table and at the start of the guide. The download page is also redesigned as you chose (entry 338, concept A): pick your device at
the top, the newest build on the left and the steady store copy on the right, everything else folded; grouplab.org/download/ once
this push is live, and /download/?device=mac (or iphone, android, linux, windows) to point at one device.

THE LEVEL AT THE RANGE (entry 321, 22:55 UTC, not a request; in the next nightly): the camera's level now works with the phone upright
at a target on its backer as well as flat over a table, choosing by itself; the word "Upright" or "Looking down" sits under the crosshair,
and once the sheet's corner codes are seen, the sheet's own angle decides, so a leaning backer still reads as square. To try at the next
sitting: both positions, and the phone turned sideways. Also new: "Find holes (Experimental)" when marking a target GroupLab did not
print, on the computer and in GroupLab Dev; and a mark much bigger than your bullet is ringed in amber on the result for you to check.

## 73. When the Phomemo M834 arrives: two recordings from your Android phone, about twenty minutes, that day or later (entry 358)

**Opened 2026-10-03.** **Why:** Phomemo publishes nothing about how the M834 talks, so GroupLab learns it from one page printed by
Phomemo's own app while the phone records the Bluetooth traffic. Nothing is sent anywhere; you copy two files to this computer.
**Steps:**
1. **The scan.** Install **nRF Connect for Mobile** (Nordic Semiconductor) from Google Play. Turn the M834 on. Open nRF Connect, tab
   **Scanner**, press **Scan**. Find the printer in the list (its name may be a code or a serial number rather than "M834"; it is the
   entry whose signal grows strong when the phone is next to it). Press **Connect** on it. When the list of services appears, press
   the three dots at the top right, **Save** or **Share** the log, and save it as a file. If it never appears in nRF Connect, say so:
   that tells us it uses classic Bluetooth instead.
2. **The recording.** On the phone, Settings, About phone, tap **Build number** seven times to unlock Developer options (skip if done).
   Settings, Developer options, turn on **Enable Bluetooth HCI snoop log**. Turn Bluetooth off and on again. Open the **Phomemo app**,
   print **one** page with a little text on it, and nothing else. Back in Developer options, turn the snoop log **off**.
3. **Getting the recording off the phone.** Connect the phone to this computer with the cable (USB debugging on, as for GroupLab Dev),
   and in PowerShell run: `adb bugreport C:\Dev\grouplab-local\printers\m834-bugreport.zip`. It takes a minute or two. Copy the nRF
   Connect file into the same folder.
**A good answer:** "both saved in grouplab-local\printers", or what went differently.

## 72. When the 4x6 Bluetooth label printer arrives: one scan from your phone, about five minutes, that day or later (entry 358)

**Opened 2026-10-03.** **Why:** GroupLab will print straight to it over Bluetooth, with no maker's app. To do that it must know how the
printer appears to a phone: as a Bluetooth LE device (which an iPhone can use) or only as classic Bluetooth. **Steps:** install **nRF
Connect for Mobile** (Nordic Semiconductor) from Google Play or the App Store; turn the printer on; in nRF Connect, **Scanner**, **Scan**;
find the printer (it may show a code or serial number rather than its model; it is the one whose signal grows strongest next to the
phone); press **Connect**; when its services are listed, take a screenshot of the list, scroll and take another until all are captured,
and save them in `C:\Dev\grouplab-local\printers\`. If it never appears in nRF Connect, say so: that means classic Bluetooth only.
**A good answer:** "screenshots saved", or "it does not appear".

## 71. Switching on "send everything I open" and fuller error reports: your yes, and the store privacy forms, about twenty minutes (entry 357)

**Opened 2026-10-03.** **Why:** both are built and switched off, so no build sends more than before. Switching them on changes two
things you have promised: today a picture GroupLab could not read is never sent (the app's words, the website's "What GroupLab sends"
page and nightly 161's notes say so), and today an error report holds only the names of the last few things done. On, the choices
become **Send everything I open** (every picture opened, read or not, with what GroupLab worked out and the log), **Send finished
targets only** (today's automatic sending, renamed; nobody is moved without being asked), **Ask me each time** and **Never**; and an
automatic error report also carries GroupLab's log from this run and the last, with anything typed replaced by its length. **What you
would check in each store** (the repository does not record your current answers):
- **Apple, App Store Connect, App Privacy:** today the honest answers are Diagnostics (crash data; performance data from the survey)
  and an identifier (the survey's random installation number), not linked to you, not used for tracking, all optional. Once switched
  on, add Other Diagnostic Data and Product Interaction (the log). Photos change only when the phone can send targets, which it cannot
  yet (question 81).
- **Google Play, Data safety:** today App info and performance (crash logs, diagnostics) and Device or other IDs, collected, optional,
  encrypted in transit, not shared. Once on, add App activity (app interactions). Photos as for Apple.
- **Microsoft Store:** the privacy link stays the "What GroupLab sends" page; no questionnaire answer changes.
**A good answer:** "yes, switch them on" (or only one), and "the forms are updated" once you have done Apple and Google. Nothing goes
on until you say so.

## 70. Fenix's report package from the desktop, five minutes for him, whenever suits (entry 354 section 4)

**Opened 2026-10-03.** **Why:** nothing he read on the desktop on 2 October reached the server, and no error report did either. The code
says a target goes only after Accept and analyze, so the likeliest reason is that he never got that far; his log would prove it, and show
whether anything was tried and refused. **Steps for Fenix:** on the desktop, the gear at the bottom left, **Report a problem**, then send
the zip it makes to you (it has the log and no photographs unless he adds them). **A good answer:** the zip, saved anywhere on this
computer, and its path.

## 67. Turn off GroupLab Team's automatic distribution in TestFlight, about one minute, whenever suits (entries 319, 320 and 335)

**Opened 2026-10-01.** **Why:** both TestFlight groups now get each build by themselves: build 148 reached GroupLab Team and the Public
Beta with nobody touching App Store Connect, so the group's own automatic distribution, which you left on while Apple reviewed the first
build, is no longer needed, and with it off the two groups can never drift apart. **Steps:** in App Store Connect open Apps, GroupLab,
TestFlight; under Internal Testing choose GroupLab Team; in the group's settings turn off automatic distribution of new builds. **A good
answer:** "done". If the setting is named differently, a screenshot of the group's page is enough.

## 66. How often GroupLab goes to the Microsoft Store: your choice, two minutes, whenever suits (entry 337)

**Opened 2026-10-01.** **Why:** the route is built: a run of "store submit" on GitHub sends a chosen build to the Store through
Microsoft's own submission service, with the keys you set up for request 38, and its result appears in `docs/notes/external-status.md`.
Nothing is sent until you say which builds go and how often. **My proposal:** a build goes to the Store when it has been the newest
nightly for seven days with no new error report and no open TestFlight or feedback problem, at most once a week; I start the run and tell
you here, and you can always say "not this one". **A good answer:** "yes to the proposal", or your own rule (for example "only when I
say", or "every two weeks").

## 62. Firebase Test Lab: GroupLab Dev on real phones every day, free, about ten minutes, whenever you choose (entry 318)

**Opened 2026-09-30.** **Why:** once this is done, every day GroupLab Dev reads the sample scan and walks every tab on three real phones
(a Samsung, a Pixel, and a Xiaomi or Oppo, whichever Test Lab has) and one virtual phone, and I read the screenshots and logs. That is
where differences between phone makers show up, without you buying phones. It stays on the free plan, which has no billing at all, so
nothing can be charged. The workflow is ready and waits for these steps; until then it says "not set up" and tests nothing.

1. Open https://console.firebase.google.com signed in with your Google account. **Add project**, name it `grouplab-testlab`, and turn
   Google Analytics **off**. Leave it on the free Spark plan; never choose Upgrade.
2. In the project, open **Test Lab** (under Run or Release and monitor) once, so it is switched on.
3. Open https://console.cloud.google.com/apis/library/testing.googleapis.com with the `grouplab-testlab` project chosen at the top and
   press **Enable**; then the same for https://console.cloud.google.com/apis/library/toolresults.googleapis.com.
4. Open https://console.cloud.google.com/iam-admin/serviceaccounts (same project), **Create service account**, name `grouplab-ci`.
   Give it exactly two roles: **Firebase Test Lab Admin** and **Firebase Analytics Viewer**. Nothing else. (Those two are what Google
   documents for running tests from CI; they can also see this project's storage, which holds only test results.)
5. Open the new account, **Keys**, **Add key**, **Create new key**, **JSON**. A file downloads.
6. In PowerShell, with the file's real name in the first line (the project ID is shown on the project's home page; it may have a
   short suffix, such as `grouplab-testlab-a1b2c`):

```
Get-Content -Raw "$HOME\Downloads\grouplab-testlab-XXXXXXXX.json" | gh secret set FIREBASE_TESTLAB_KEY -R oRAirwolf/grouplab
gh variable set FIREBASE_PROJECT_ID -R oRAirwolf/grouplab --body "grouplab-testlab"
Remove-Item "$HOME\Downloads\grouplab-testlab-XXXXXXXX.json"
```

   A good result: the first two print nothing or a line saying the secret or variable was set; the key file is then gone from Downloads.
7. Tell planning "request 62 done". I start the first run by hand and put what the phones showed here.

## 61. iOS: the Apple steps for GroupLab Dev, about twenty minutes, whenever suits (entry 315)

**Opened 2026-09-30.** **Why:** entry 315 builds everything that lets me drive and watch the app without your hands (the automation
bridge, scenario files, the replay camera) into a separate iOS app, GroupLab Dev, so the GroupLab that Apple reviews carries none of it.
Until these exist it is built and tested on the simulator only. The distribution certificate and the App Store Connect key you already
set are reused. At developer.apple.com, Certificates, Identifiers & Profiles:

1. Identifiers, the + button, App Groups: create `group.org.grouplab.app.dev` (description: GroupLab Dev).
2. Identifiers, the + button, App IDs, App: Description `GroupLab Dev`, Bundle ID **Explicit** `org.grouplab.app.dev`, turn on App
   Groups, Configure, tick `group.org.grouplab.app.dev`, Continue, Register.
3. The same again for the share extension: Description `GroupLab Dev Share`, Bundle ID `org.grouplab.app.dev.share`, App Groups with
   `group.org.grouplab.app.dev`, Register.
4. Profiles, the + button, Distribution, **App Store Connect**: App ID `org.grouplab.app.dev`, the same distribution certificate, name
   it `GroupLab Dev App Store`, Generate, Download. Then the same for `org.grouplab.app.dev.share`, named `GroupLab Dev Share App Store`.
5. In Git Bash, in the folder with the two downloads:
   ```
   base64 -w0 "GroupLab_Dev_App_Store.mobileprovision" | gh secret set IOS_DEV_PROFILE -R oRAirwolf/grouplab
   base64 -w0 "GroupLab_Dev_Share_App_Store.mobileprovision" | gh secret set IOS_DEV_SHARE_PROFILE -R oRAirwolf/grouplab
   ```
   (use the downloaded files' own names if they differ).
6. In App Store Connect, Apps, the + button, New App: iOS, name **GroupLab Dev** (or **GroupLab Dev Build** if that is taken), Bundle ID
   `org.grouplab.app.dev`, SKU `grouplab-dev`, full access. Then in its TestFlight tab add an internal group named **GroupLab Team**. It
   is never added to an external group, so Apple never reviews it.

**A good answer:** "done", and `gh secret list -R oRAirwolf/grouplab` shows IOS_DEV_PROFILE and IOS_DEV_SHARE_PROFILE.

## 59. TestFlight: GroupLab on your iPad mini, and the two testers' invitations, whenever suits (entries 290 and 310)

**Updated 2026-10-01: mostly done.** Both groups exist, Apple approved the public beta, and the invitation is published (entries 335 to
337); each new build reaches both groups by itself, and request 67 covers turning the team group's own distribution off. What may be
left: on the iPad mini, install Apple's TestFlight app and GroupLab from the invitation; and, if you still want them in GroupLab Team
rather than the public beta, invite Unholy and Fenix in Users and Access with the Marketing role, limited to GroupLab, then add them to
the group. **A good answer:** "GroupLab is on the iPad" and whether it opened, or "nothing left".

## 58. Store-bought targets for the detector, whenever suits you, about ten minutes a target (entry 308)

**Updated 2026-10-01 (entries 325 and 327): in part. Thank you for the five Birchwood Casey blanks** (the 6 in and 8 in Shoot-N-C
bullseyes, the sight-in grid, the crosshair, and the Eze-Scorer), each scanned at 600 dpi with its package photographed. They are on this
computer only and are already being used: GroupLab looks for holes on each clean sheet, where every mark it finds is a false one. **Still
needed, whenever suits you:** shoot each one, then scan it again with the sheet in **the same corner of the glass as its blank** (your
scanner stops at legal size, so the scan holds the same part of the sheet), note how many shots you fired, and take one phone photo of
each whole shot sheet. A good answer: a `shot.png`, the shot count in `notes.txt`, and the photo, in each target's folder.

**Opened 2026-09-30.** **Why:** each blank and shot scan of another maker's target becomes a hard test of finding holes on any target,
kept on your computer only, never committed or shown. **Needed:** yes to buying several that fit on the scanner glass; a spread helps
most: plain black bulls, a fluorescent or splatter target, colored diamonds, and a small grid target. For each one:

1. Scan it at 600 dpi before shooting it.
2. Shoot it, and note how many shots.
3. Scan it again at 600 dpi.
4. Take two or three phone photos, one square on and one at an angle.

Put each target's files in its own folder under `C:\Dev\grouplab-local\commercial-targets\` (for example `splatter-1\blank.png`,
`shot.png`, `photo-1.jpg`) with a line saying how many shots. **A good answer:** "done, N targets".

---

## 57. Red bulls on paper, about fifteen minutes (entry 297)

**Opened 2026-09-30.** **Why:** the sheets can now be printed with red or blue bulls, and GroupLab finds the color from the photo; the
made-up tests read red and blue as well as black, and a real sheet under real light is the proof. **Needed,** once the next nightly is out:

1. In Targets choose GL-CF25-LTR, set "Bulls in" to Red, print it at Actual size.
2. Shoot five shots, or poke five holes with a pen.
3. Take three phone pictures with GroupLab: one with the torch on Auto in a dim room, one under warm indoor lamp light with the torch
   off, and one in daylight.
4. If you have a black-and-white printer, print the same sheet on it too and say whether the bulls read clearly as gray.

**A good answer:** "done"; GroupLab Dev keeps the pictures, and Code reads them from the phone at the next sitting. Every hole found in all
three, with no marks where there is no hole, is the result hoped for.

---

## 56. Your printer's scale, measured properly: one sheet printed now, scanned at 600 dpi (entry 291), about ten minutes

**Opened 2026-09-29.** **Why:** the scale test is done (request 53 has the results). Your three scans show your printer printed those pages
at its true size, within a tenth of a percent. The card photos read it about 0.4% large, and that is the figure GroupLab saved, so photos
since then read groups about 0.4% small. The card method is what needs work, not your printer. And you have calibrated the printer since.
**Needed:**

1. Until you do step 2: in GroupLab Dev, Settings, turn off "Correct photographs by the chosen printer's scale". That is closer to the truth
   than the saved +0.4%.
2. On the computer: print any GroupLab sheet now at Actual size, scan it at 600 dpi, and in GroupLab on the computer choose Printers,
   Add a printer, Scanner, and pick that scan (entry 303). Scanner checks agreed with each other to within 0.07%.
3. Also in Printers, press "Printer calibrated or serviced" on the old printer check, so every result it corrected says so.

**A good answer:** "done", or the percentage the Scanner check showed.

---

## 54. A plain target for the home page's picture, five shots, about fifteen minutes at the range (entries 278 and 308)

**Opened 2026-09-28; clarified 2026-09-30 (entry 308).** **Wanted:** a store-bought target with no brand name or logo printed on it, or
one whose logo sits where it can be cropped off (a plain bull or diamond on white is ideal). Shoot five shots at it, then scan it at 600
dpi or take one square-on phone photo in daylight, and put the files in `C:\Dev\grouplab-local\store-target\`. **Why:** the home page's
"Your own targets" shows a sample GroupLab drew itself until a real one exists; GroupLab never shows or names another maker's target on the
site, so your 20 September photos of the orange target stay test material for the detector only. **A good answer:** "done", and the
distance and caliber if you know them.

## 50. One more short sitting with the Fold 7 and the tablet, later (nothing to do yet)

**Added 2026-09-28 (entries 253 and 255): about thirty minutes now, and the camera test of request 33 is part of it.** Have a printed
GroupLab 5x5 sheet on a table in ordinary room light for the camera test. The sitting also retakes every phone and tablet picture on the
website (entry 253). When the checks that need nobody are done, the panel and the top of this file give you a few short steps with the
Fold 7 over that sheet; nothing else needs your hands.

**Added 2026-10-01 (entries 321 and 322), two more things at that sitting, about five minutes:** the camera's level with the phone
upright at a sheet taped to cardboard as well as flat over the table, in portrait and turned sideways; and the same sheet photographed
from 1.5, 2, 2.5 and 3 ft (45, 60, 75 and 90 cm), so the distance at which GroupLab stops reading the corner codes is measured on a real
phone. The steps come in the panel and at the top of this file when the sitting starts.

**Added 2026-10-01 (entry 323), one more look, about two minutes:** the new Velocity and the vertical card on the Fold 7's cover screen,
with readings pasted in, to see that nothing in it is squeezed and its band chip turns the band off.

**Opened 2026-09-28 (entries 250 and 252).** **Needed:** the phone and the tablet on the charger, unlocked, Wireless debugging off and on,
Stay awake on, for about twenty minutes, **once a line at the top of this file starting "READY FOR THE PHONE AND TABLET:" says so**; not
before. **Why:** three things can only be checked on the devices: the Targets preview now showing a sheet's words and Letter above A4 on the
phone (entry 250), the new C3 zeroing grids on the phone, and how long "Shots Needed to Zero" takes to work out on the Fold and the tablet
(entry 252, which asks for those timings). **A good answer:** nothing written; the devices on and reachable when the line appears. Until
then they can stay put away.

## 46. On or after Sunday 2026-10-04 12:00 UTC: one look at the Oracle backups

**Opened 2026-09-27 (entry 235). Not before Sunday 4 October, 12:00 UTC; then one minute.** In the Oracle console, **Storage**, **Block
Storage**, **Boot Volume Backups**: is there a backup of type **Full** created on 4 October, a little after 12:00 UTC, that expires about
**17 October**? That is the first Full of the schedule you changed on 27 September, and it is all that is needed to know the change worked.

A good answer: "Full, 4 October, expires 17 October", or what the list shows instead.

## 33. The Fold 7's camera: ten minutes with a printed sheet

**2026-09-28 (entry 255): this is now done in request 50's sitting**, while the Fold 7 is connected anyway, with GroupLab Dev rather than
the spike. The steps below are kept as the record; you will be given shorter ones at the time.

**Opened 2026-09-25. Entry 219 item A2.** The capture screen is built: the camera's preview inside GroupLab, one instruction at a time
(move back, move closer, less angle, hold steadier, more or less light, flatten the paper), the lens by zoom, tap to focus, and a shutter
that fires by itself when everything holds. It needs a real camera and a real sheet to measure. Everything is logged to a file on the
phone, so nothing needs watching.

**You need:** the Fold 7 with Wireless debugging on, and a printed GroupLab 5x5 sheet on a table in ordinary room light.

**1. In PowerShell on this machine**, with the address and port on the phone's Wireless debugging screen:

```powershell
$adb = 'C:\Dev\tools\android-sdk\platform-tools\adb.exe'
& $adb connect <address:port shown on the Wireless debugging screen>
$run = gh run list -R oRAirwolf/grouplab --workflow android --status success --limit 1 --json databaseId --jq '.[0].databaseId'
gh run download $run -R oRAirwolf/grouplab -n grouplab-spike-apk -D "$env:TEMP\gl-spike"
& $adb uninstall org.grouplab.app.spike
& $adb install "$env:TEMP\gl-spike\grouplab-spike.apk"
gh run download $run -R oRAirwolf/grouplab -n grouplab-apk -D "$env:TEMP\gl-spike"
& $adb install "$env:TEMP\gl-spike\grouplab.apk"
Remove-Item -Recurse -Force "$env:TEMP\gl-spike"
```

**2. On the phone**, unfolded: open **GroupLab spike**, press **Camera**, allow the camera when asked, and press **Camera** again.

1. Hold the phone over the sheet so the whole sheet shows with a margin. Follow what the words at the top say. When they say **Hold it
   there**, keep still: after a moment the shutter fires by itself and a line appears saying what the picture found.
2. Press **3x** and do the same, then **0.6x**, then **1x**.
3. Tap the sheet in the preview once (focus and exposure lock there), and press **Take**.
4. Move the phone so the sheet runs off the edge, then very close, then at a steep angle, and see that the words change each time.
5. Fold the phone, and do step 1 once on the cover screen.
6. **The application itself**, added by entry 219 item A4: open **GroupLab** (not the spike), answer its first questions, press **Take
   a picture**, allow the camera, and let it take the sheet. **Look for:** a result with the number of shots, the group's size, a plot
   and the photograph with a ring on each hole; drag a ring with **Move** and see the magnifier; then **Sessions** lists it.
7. **Share this session**, and send it to yourself however is easiest (Drive, email, or a cable). On this computer, in GroupLab's menu,
   **Open a session file** and choose it. **Look for:** the same marks on the same picture, and a line naming the phone it came from.

**3. In PowerShell again**, to hand me the log:

```powershell
& 'C:\Dev\tools\android-sdk\platform-tools\adb.exe' pull /sdcard/Android/data/org.grouplab.app.spike/files/spike-log.txt C:\Dev\grouplab-local\spike-log.txt
```

**A good answer.** "Done", and anything that looked wrong: an instruction that did not match what you were doing, a shutter that never
fired or fired on a blurred sheet, or the preview misbehaving when folded. The log is read from `C:\Dev\grouplab-local\spike-log.txt`.
The phone can then be put away again.

---

## 20. The hole size test, when you can shoot it

**Opened 2026-09-24. Entry 158 program B. Nothing waits on it but the article it would make.**

**Rewritten 2026-09-27 (entry 235):** the smallest useful set first, and a page to print.

**What is needed, and it is enough on its own: eight sheets.** Two sheets each of **.22 LR subsonic, .22 LR high velocity, .300 Blackout
subsonic, and 6.5 Creedmoor (or 6 ARC)**, one shot to a bull, 50 yards, all on GroupLab's 25 bull Letter load sheet printed in one batch,
one backing, stapled the same way, shot in the alternating order the plan gives, and each scanned at 600 dpi on your Brother flatbed.
**Print `docs/RANGE-PLAN-HOLE-SIZE.pdf`** (one page: the sheets to print, the order to shoot, what to write on each load block) and take it.

**Only if there is time:** two sheets each of 8.6 Blackout subsonic, .510 Whisper, and whichever of 6 ARC and 6.5 Creedmoor you did not
shoot. **.300 Blackout supersonic is not needed**, since you would have to load it; the article will say what it would have added.

**Why.** A .22 LR hole measures 0.765 of the bullet where centerfire holes measure 0.92 to 0.95, and the one rimfire sheet cannot say
whether that is speed, nose shape, lead against a jacket, or width. This set separates speed from width.

**Commercial gridded sheets** are larger than the flatbed, so they are not scanned (entry 235). When you shoot one, photograph it square
on in even light, with a ruler laid on it if it has no printed scale, and note the distance, the cartridge and which mark each group was
aimed at; GroupLab measures it with the four-point method and a scale at each bull.

**A good answer.** "The scans are in <folder>"; the sheet numbers in the serial boxes say which is which.

---

## 16. The macOS tester: a name for the thanks, and ten minutes on a newer build

**Opened 2026-09-24. Entry 166. Waiting, half of it. Not urgent.** The first half came back on 2026-09-24, entry 189: the
tester is Fenix, and he is thanked in the README with Unholy. The trackpad half below is still open.

**What is needed.** Two things from the tester who ran nightly 93 on the M5 Max, passed on by Alan.

1. **A name, or none.** The project has no list of testers or contributors yet. Would Alan like one, in the README, and
   if so what name should this tester go by: his own, a handle, or "a macOS tester"? Nothing is invented meanwhile.
2. **What his trackpad actually sends.** On any build newer than nightly 94: Settings, tick **Detailed logging**; open
   the sample; press Command Z after moving a shot, and Shift Command Z; drag with two fingers on the sheet; pinch; scroll
   with Command held; and a mouse wheel, if he has a mouse. Then **Report a problem** and send the report.

**Why.** Command Z did nothing on his Mac because every shortcut read the Control key, and pinch zoom was never built.
Both are fixed, and neither fix has been checked on a Mac. The second half also measures what Avalonia delivers for a
trackpad scroll against a wheel on a Mac, which entry 166 asked to measure rather than assume and which nobody here can
measure without a Mac. The report carries each scroll and pinch as numbers, and no path or picture.

**A good answer.** For 1, a name or "anonymous", or "no list". For 2, "Command Z undid it, the drag moved the sheet,
the pinch zoomed", or which of those did not, and the report.

---

## 9. Mark one scan by hand, twice, so a person's click has a number too

**Opened 2026-09-24. Entry 170 section 4.4. Waiting.**

**What is needed.** On the friend's scan `Scan_20260923.png` (the one in your Downloads folder), in GroupLab: detect, then drag each
of the ten hole marks to where you judge the centre of the hole to be, at the zoom you would normally use, and save the session. Then
close it, open the scan fresh, and do the same again without looking at the first session, and save that as a second file. Send the
two session files the way you send anything else, or put them in `C:\Dev\grouplab-submissions\hand-marked\` and say so.

**Why.** The outside user moved nearly every hole centre by a few thousandths of an inch, and that changed his extreme spread by 0.11 MOA.
A person placing a centre by eye is uncertain too, so his corrections are evidence and not ground truth. Two markings of the same scan
by the same person say how uncertain, and the detector is then held to that standard rather than to zero. It cannot be done by
software, because the thing being measured is a person's eye.

**A good answer.** The two session files. Ten minutes, roughly.

---

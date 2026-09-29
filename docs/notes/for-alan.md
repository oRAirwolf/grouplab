UNDER WAY (entries 291 and 292, not requests): your second sitting's pictures and logs are pulled from the Fold 7 and kept on this computer only. Being built now, each by its own worker: the scale test's card readings against the three scans; the result picture upright with no empty space, holes fixed on their own zoomable page, and the Camera and Result buttons; the camera's distance words, "hold steadier" and the live view matched to the picture; a scoreboard that re-reads every sitting's pictures; and photos from Google Photos, Samsung Gallery and any other photo app. What to try at the next sitting comes with each.

GROUPLAB CHECKS ITS OWN HOLE FINDING NOW (entry 291, not a request): every build re-reads made-up targets with shadows, glare, curl, blur and poor light, and fails if it gets worse. It also read every target photo from your second sitting (all four were your 6 ARC Dominus K sheet) and your three photos of 26 September, hole by hole against your scans: 169 of 173 holes found, 6 marks where there was no hole, typically a hundredth of an inch or two off. The two square-on photos were perfect; the two at 9 and 15 degrees missed or misplaced holes in the right-hand column, reading a hole and the paper beside it as one big mark. Next: a mark much bigger than your bullet goes to you to check. At the next sitting, square-on photos give the best results today, and a few angled ones help measure the fix.

FOR UNHOLY (entry 289): once nightly 126 is out, the 2 MOA sheets are in GroupLab under Targets, Centerfire load development: "GroupLab 3x3 2 MOA" for one page, or "..., Set of 3 Letter Sheets" for load development, each also with the C or E bull and on A4. Print at Actual size. Three details differ from what was asked (two corner codes, bulls numbered 1 to 9 on each page, the standard load block), because the sheet format cannot carry them yet; planning has them as questions 71 to 73.

IOS, UNTIL WEDNESDAY 8 PM MOUNTAIN (entry 290; not a request, updated as items land):
1. The shared mobile project, both phones' screens in one place: built; Android unchanged, checked again on the next nightly.
2. OpenCV for iOS in CI: built and proven: a static library for the iPhone and the simulator, 22 MB, rebuilt only when its recipe changes.
3. The iOS build in the nightly: not yet.
4. It runs on the iOS Simulator in CI (launch, every tab, the imaging, the whole pipeline, a picked picture): not yet.
5. The camera screen: not yet.
6. Files, sharing, printing, the idle screen: not yet.
7. The TestFlight path, ready for request 55's secrets: the check is built (it signs only when all seven are set and right, and names a malformed one); the job that uses it comes with item 3.
Also landed: GroupLab Dev updates itself from nightly 125 on (entry 288), and your answers to questions 69 (A) and 70 (B) are recorded.

READY (entry 286, 04:38 UTC): GroupLab Dev nightly 123 is on the Fold 7 (and the tablet), your data kept. The card photo screen opens: Settings, Printers, Add a printer, choose "A card and one photo", Next, then Take the picture. The phone is left on that screen for you.

GOOD MORNING (the night of 28 September, in five lines):
1. Out in nightly 122 (02:10 UTC): the camera test's fixes (line 2), a shot you leave out now left out of every figure (Unholy's report), CSV import that guesses its columns, exact definitions for mean radius and standard deviation, and the plan for iPhone and iPad (docs/IOS-PLAN.md, and request 55 for the Apple steps). The site is current.
2. THE CAMERA FIXES are in nightly 122 (on the tablet now, and on the Fold 7 the moment it is back on Wi-Fi): a bubble level in the middle; the torch goes off after the picture; the camera comes back after you leave GroupLab; what you frame is what is saved; the words no longer flip between closer and back; Camera and Result buttons stay in view; the shutter clicks and flashes at once; the picture on the result keeps its shape and stands upright; and a sheet whose codes were too small to read is read by enlarging each code, which named last night's refused picture. Try it when you like, with a printed sheet, and say "camera done" and anything that felt wrong.
3. The server read is done (request 52, 17:45 UTC): two processors, 10.2 GB of memory free, 36 GB of disk free, load 0.6. It has room to re-read sent targets, capped at one processor.
4. New tonight (entries 278 and 279), none before the camera test: 53, three test pages to print, scan and photograph with a card (ready in C:\Dev\grouplab-local\scale-test); 54, a store-bought target with five shots; 55, the Apple steps for iOS once your membership is active. The tablet has nightly 121 too and shows the black idle screen.
5. Updated 2026-09-29 02:17 UTC. Also built tonight: CSV import with guesses (phone and computer), marking a target by hand on the phone, Fudd buster mode, the target saving itself with the time shown, and a tap on a number switching that number alone.

**The home page's top picture is the README's product picture** (entry 287; not a request): the sheet, the computer and the phone, numbered 1 to 3 with the README's three lines under it, live once this change publishes. On a narrow screen the whole picture fits, the phone uncut, but the small numbered badges are hard to read on a phone; say if you want them bigger in the picture itself.

**The README's empty row is fixed** (entry 285; not a request): the three lines under the main picture are now a numbered list, with no header row for GitHub to draw.

**Behind the curtain is built** (entry 284; not a request, for planning to look at). Locally: build the site with `python website/build.py` and open `website/_site/tour/how-it-works/index.html`; on the site, once this change is published: https://grouplab.org/tour/how-it-works/, with /opencv/, /hole-detection/ and /pipeline/ under it. Every fact was checked; the corrections are listed in `docs/PHASE1-RESULTS.md` under entry 284 (the markers are 38 on today's sheet, 35 of 37 sheets are named from their codes, six draft articles are not linked, the trace is a real run).

# Requests for Alan

**Open: 10.** Most urgent: **56**, your printer's scale from one scan (ten minutes), and turn off the photo correction meanwhile. Then **50**, the camera test of 33 inside it. **54** the store-bought target and **55** the Apple steps whenever suits. **46** waits until Sunday 4 October. Then **38**, the Microsoft Store: your account, the name and the keys, about thirty minutes. Then **33**, ten minutes with the Fold 7. Then 9, 16 and 20 (rewritten: eight sheets, and a page to print).

**Is a self-improving detection engine worth it? The study** (entry 261; not a request; `docs/DETECTION-LEARNING-STUDY.md`). Yes, it is
possible and it needs no Claude to run. Build now a scoreboard that re-reads synthetic and real targets with every build; later, automatic
tuning of today's settings against it; not yet a learned model, because the failures found are not ones it fixes and the labelled holes do
not exist in the thousands it needs. The first measurement already found something: a sheet with a gentle curl across it (0.05 in) did
not register at all, while shadows, dim light, blur, noise and JPEG cost at most two or three holes of 25. On your server, whether it has
room is one read-only command away, which needs your approval in the morning.

**The phone's parity work, in order** (entry 258; not a request). Your "A" choices of entry 259 are the build spec. Each ships in its own
nightly and is tried at the next sitting. Rough sizes: (1) full figures with the explanation sheet, large; (2) the bulls you fired at,
medium; (3) Shots Needed to Zero's own page, medium; (4) compare loads, medium; (5) Ballistics as a fifth tab with Dope, Trajectory and
Hit chance, the largest; (6) the set as a checklist, medium; (7) the scan pill, small. Then marking targets GroupLab did not print, CSV
through the share sheet, large sheet advice, and opening a picture shared from another app. `docs/PHONE-PARITY.md` lists every feature
as on the phone, coming or left out, and the site build now fails on a feature with no row.

**DESIGN NEEDED** (entry 279 section 4; not a request for you; planning, please): **the 2 MOA sheets Unholy asked for.** The sheets with
1 MOA circles are the 5x5 load development family, a 1.00 in bull (0.95 MOA at 100 yd, 0.87 MOA at 100 m, Letter and A4, plain, C and E),
and the 5x6, a 0.87 in bull. A 2 MOA bull is 2.09 in at 100 yd and 2.29 in at 100 m, and cannot sit on the 5x5's 38 mm pitch. Options:
**A**, 3 by 4 on one Letter or A4 page, 12 bulls at about 64 mm; **B**, 5 by 5 as a set of four sheets like the large format sheets, 25
bulls; **C**, both, B for load development and A for a quick group. Which, and which variants first? Everything else is chosen: Marking
A, Fudd buster page A and saving A with a setting are built, the tabs are next, and row 10's screens are chosen by entry 280.

**The Features page shows each new thing itself** (entry 256; not a request). These entries now have their own picture, drawn as the
sheet prints: **The E bull**, **The C bull** (with its dot), **Zeroing grids read through a scope** (all four C3 sheets, credited to you
with Jylee and Unholy) and **Large format on a home printer** (the four Letter sheets); and **Shots Needed to Zero** (credited to
Jylee) and **Ballistics and hit chances** (the Hit probability view) show their own views. Twelve entries still use a general screen and
are listed as gaps in `docs/figures/SCREENSHOTS.md` until each has its own crop. From now on a new bull, sheet or view is not finished
until it has its own picture there.

**The night of 27 September, in five lines** (entry 243 asked for it; not a request):
1. Finished: sets of sheets pool into one group; every analysis shows progress and can be canceled; the phone has a Targets screen and a side by side result on big screens.
2. Finished: the C bull (a diamond standing on a point, with a dot) and the E bull sit beside the usual one on three sheets each, and the designer and Made for your optic can draw them.
3. Finished: the tabloid and A3 sheets now print as sets of four Letter or A4 sheets; your old printouts still read.
4. Waits on you: **49**, pick the phone's look (A, B or C) from the page it links; **45**, reconnect the Fold 7 and the tablet so the screenshots and measurements can run.
5. Also done: entry 244 (the README now keeps itself current) and the server sitting (the survey worker installed, request 21 closed).

**Shots Needed to Zero, and how fast it is** (entry 252 sections 3 and 4; not a request). It is in the analysis screen's Advanced
figures, under the full CEP table, credited to Jylee. **How long:** about 0.15 to 0.2 seconds on your desktop for groups of 5, 10, 25 or
100 shots (measured), off the screen's own thread, only when that section is open and its inputs change, and remembered after that. The
Fold 7 and the tablet are measured at the next sitting (request 50); from the benchmark's ratios they should take about a quarter and half
a second. **What made it fast:** with the spread known, the answer is an exact formula, with no simulation at all; with the spread uncertain,
only the spread is simulated, 4,000 evenly spread draws rather than random ones, and the formula does the rest; the shot counts are found by
halving the range rather than trying every count. **What gave:** nothing; the numbers are good to far better than a shot, and the screen
says so. The short article is `shots-to-zero` on the research pages.

**Jylee's one-shot zero, evaluated** (entry 250 section 4; not a request). It can be done: the phone already reads the zeroing grid and
finds the hole's offset, and GroupLab already turns an offset into clicks. What is missing is the click value on the phone, a rifle on
the phone to take its usual spread from, and the zero correction on the phone's result screen. What one shot can honestly say: only an
error clearly bigger than the rifle's own spread (about 0.7 MOA for a rifle that shoots 1 MOA groups) is worth dialling off one shot;
anything smaller gets "fire more before adjusting". So one shot is for getting on paper and big errors, not a fine zero. The design that
fits GroupLab is "Zero, step by step": shot 1 gives a rough correction only when it is clearly outside the spread, each further shot
updates the clicks and their interval, and the screen says when more shots stop changing the answer; "Shots Needed to Zero" (entry 252)
is the same question from the other end. The full note is `docs/notes/ONE-SHOT-ZERO.md`. Jylee is credited by name.

**The Ballistics screen, laid out as the B you chose** (entry 247; not a request). Before and after, light, 1400 by 900:
[before](../figures/screens/before/ballistics-light-1400x900.png), [after, the trajectory](../figures/screens/after/ballistics-light-1400x900.png),
[after, the hit probability view](../figures/screens/after/ballistics-hit-light-1400x900.png). Three columns like the analysis: the
settings on the left in sections that fold, the chart and the full table in the middle, and the answer at one range on the right, with
the elevation large in amber; a row or a point on the chart chooses the range. The hit probability calculator is the middle's second
view, as your addition drew it. Nothing the solver or the simulation computes changed. Dark and high contrast follow the same tokens.

<!-- automation-week: written by scripts/automation-report.py each week; not a request -->
**This week, by itself** (not a request): backed up on 27 September (543 MB, backup-2026-09-27); the restore test passed on 27 September; 0 archived submissions copied here; cleanup freed 1 MB; on the server, workers deleted or archived: nothing; the server's own backup is from 2026-09-26; the Oracle boot volume backups are not seen by this report: Alan can check them in the Oracle console, under Boot Volume Backups, whenever he wants.
<!-- /automation-week -->

**The Fold 7 and the Tab S8 Ultra stay connected for testing** (entries 234 to 236): leave Wireless debugging on. The Essential PH-1
can be unplugged. When a phone needs you, the whole list comes here first, in one request (entry 212).

Newest first. Each request says what is needed, why it is needed, and what a good answer looks like.
An answered request is marked **answered** with the date and left here, because the reason something was
asked is worth as much later as the answer was at the time.

**How this file works.** NOTES-FROM-PLANNING.md entry 149 section 5: nothing is asked of Alan through the
Claude Code panel except a command he pastes into a shell. Everything else is written here and the run
carries on. The planning session reads this file and puts the requests to him in a form he can answer in
one sitting. His answers come back as an inbox entry, like everything else. A request here never stops
work: whatever does not depend on the answer is built anyway, and the report says which part is waiting.

At the start of a run, the count of open requests in this file is printed and nothing more.

---

## 56. Your printer's scale, measured properly: one sheet printed now, scanned at 600 dpi (entry 291), about ten minutes

**Opened 2026-09-29.** **Why:** the scale test is done (request 53 has the results). Your three scans show your printer printed those pages
at its true size, within a tenth of a percent. The card photos read it about 0.4% large, and that is the figure GroupLab saved, so photos
since then read groups about 0.4% small. The card method is what needs work, not your printer. And you have calibrated the printer since.
**Needed:**

1. Until you do step 2: in GroupLab Dev, Settings, turn off "Correct photographs by the chosen printer's scale". That is closer to the truth
   than the saved +0.4%.
2. Once nightly 126 or later is on the phone and the computer: print any GroupLab sheet now, at 100 percent, scan it at 600 dpi, and in
   GroupLab choose Printers, Add a printer, Scanner, and pick that scan. Scanner checks agreed with each other to within 0.07%.
3. Also in Printers, press "Printer calibrated or serviced" on the old printer check, so every result it corrected says so.

**A good answer:** "done", or the percentage the Scanner check showed.

---

## 55. iOS: the Apple steps after you enrol, about forty minutes, once (entry 278 item 6)

**Opened 2026-09-28 (entry 278 item 6, entry 279 item 1).** **Not before** your Apple Developer Program membership (individual) shows as
active in the Apple Developer app. **Why:** the nightly's iOS job builds on GitHub's Mac machines and sends each build to TestFlight, and
for that it needs four things only you can make: an app identifier, a distribution certificate, a provisioning profile and an App Store
Connect API key. They go straight from your computer into GitHub's secrets with `gh secret set`; nobody else ever sees them, me included.
Keep every file below in `C:\Dev\keys\apple\`, which I never open. **Needed, in order** (the shell is Git Bash in MobaXterm, in
`C:\Dev\keys\apple`):

1. **Team ID.** developer.apple.com, Account, Membership details: copy the Team ID (ten letters and digits). Then
   `gh secret set APPLE_TEAM_ID -R oRAirwolf/grouplab` and paste it when asked. Done: `gh secret list -R oRAirwolf/grouplab` lists it.
2. **The app identifier.** developer.apple.com, Certificates, Identifiers & Profiles, Identifiers, the + button, App IDs, App. Description
   `GroupLab`, Bundle ID **Explicit** `org.grouplab.app`, no capabilities ticked, Continue, Register. Done: it is in the Identifiers list.
3. **A certificate request, made on Windows.** In Git Bash, with your Apple ID's email in place of the words:
   `openssl req -new -newkey rsa:2048 -nodes -keyout grouplab-dist.key -out grouplab-dist.csr -subj "/emailAddress=YOUR-APPLE-ID-EMAIL/CN=Alan Hayes/C=US"`
   Done: two new files, `grouplab-dist.key` (never share it) and `grouplab-dist.csr`.
4. **The distribution certificate.** Certificates, the + button, **Apple Distribution**, Continue, choose `grouplab-dist.csr`, Continue,
   Download. Save the downloaded `distribution.cer` in the same folder. Then, choosing a password when asked:
   `openssl x509 -inform DER -in distribution.cer -out distribution.pem && openssl pkcs12 -export -inkey grouplab-dist.key -in distribution.pem -out grouplab-dist.p12 -certpbe PBE-SHA1-3DES -keypbe PBE-SHA1-3DES -macalg sha1`
   then `base64 -w0 grouplab-dist.p12 | gh secret set IOS_DIST_CERT_P12 -R oRAirwolf/grouplab` and
   `gh secret set IOS_DIST_CERT_PASSWORD -R oRAirwolf/grouplab` (paste the password). Done: both listed.
5. **The provisioning profile.** Profiles, the + button, Distribution, **App Store Connect**, Continue, App ID `org.grouplab.app`, the
   certificate from step 4, name `GroupLab App Store`, Generate, Download. Then
   `base64 -w0 GroupLab_App_Store.mobileprovision | gh secret set IOS_PROFILE -R oRAirwolf/grouplab` (use the downloaded file's name if
   it differs). Done: listed.
6. **The API key.** appstoreconnect.apple.com, Users and Access, Integrations, App Store Connect API, Team Keys, the + button. Name
   `GroupLab CI`, Access **App Manager**, Generate. Copy the **Issuer ID** (above the table) and the **Key ID** (in the row), and Download
   the key: `AuthKey_<KeyID>.p8`, which Apple lets you download **once**. Then `gh secret set APPLE_API_ISSUER_ID -R oRAirwolf/grouplab`,
   `gh secret set APPLE_API_KEY_ID -R oRAirwolf/grouplab` (paste each), and `gh secret set APPLE_API_KEY_P8 -R oRAirwolf/grouplab < AuthKey_<KeyID>.p8`.
   Done: all three listed.
7. **The app record.** App Store Connect, Apps, the + button, New App: Platforms iOS, Name `GroupLab` (if Apple says it is taken, try
   `GroupLab Targets` and tell me which), Primary Language English (U.S.), Bundle ID `org.grouplab.app`, SKU `grouplab-ios`, User Access
   Full Access, Create. Done: the app page opens with a TestFlight tab.

**A good answer:** "Apple steps done", and the app name if it is not GroupLab. The seven secret names above are what the nightly's iOS job
reads; until they are all there it builds without signing and uploads nothing. Nothing here submits anything to the App Store.

## 54. A store-bought target, five shots, for the home page (entry 278 item 4), about fifteen minutes at the range

**Opened 2026-09-28 (entry 278 item 4, question 66).** **Needed:** a plain store-bought target that carries nobody's design (a simple
bullseye or square), five shots at any distance, then photographed flat in good light, straight down, the whole sheet in the picture;
if it is convenient, also scanned at 600 dpi on the Brother flatbed. Put the files in `C:\Dev\grouplab-local\store-target\`, any names.
**Why:** the home page's "Your own targets" and the tour show a sample GroupLab drew itself until a real one exists. I mark yours on the
desktop's marking screen with the scale set by hand, as a new user would, and it replaces the sample. **A good answer:** "store target
done", and the distance and caliber if you know them.

## 53. Question 67 tested: three check pages, one scan each, card photos (entry 278 item 3), about thirty minutes

**ANSWERED 2026-09-29 (entry 291).** The scale test is done: the printer printed at its true size (the scans read 100.01 to 100.07%); the
card photos read about 0.4% large, most of it the card-thickness correction; outline B reads worst and A and C are about equal, C a little
ahead. The picture GroupLab took by itself was page A with your first card on it, so every Guided check passed. Request 56 is what to do
next.

**Opened 2026-09-28 (entry 278 item 3). TEST PAGES READY** (made 2026-09-28 by `grouplab scale-test-pages`; the label sits clear of every marker).
**Needed:**

1. Print `TEST-A.pdf`, `TEST-B.pdf` and `TEST-C.pdf` from `C:\Dev\grouplab-local\scale-test\` in one batch, at **100 percent**
   ("Actual size", never "Fit"), on Letter, on the same printer. Each says TEST A, B or C in large type and "not for use"; they differ
   only in the card outline: A the outline 3 mm outside the card (as built), B a hairline on the card's edge, C corner marks only.
2. Scan each printed page on the Brother flatbed at **600 dpi**, the whole page, before anything is laid on it. Save as `scan-A.png`,
   `scan-B.png`, `scan-C.png` (or .jpg, .tif) in the same folder. These scans are the truth for each page's real print scale.
3. On each page lay a **new** card (any bank or ID card) in its outline and take the printer check's card photo in GroupLab Dev
   (Settings, Printers, Add a printer, "A card and one photo", Next, Take the picture; once a printer is saved the button reads Check again). Then do the same with an **old, worn** card, and with a **dark** card if you have one.
   If GroupLab Dev cannot read page B or C, take ordinary phone photos instead, straight down, in room light, the whole page in view.
4. Copy the photos from the phone into the same folder named `A-new.jpg`, `A-old.jpg`, `A-dark.jpg`, `B-new.jpg` and so on.

**Why:** question 67 asks which outline lets the camera measure the card most accurately. You chose to test rather than choose; I
measure every card photo against its page's scan and report which outline reads most accurately and most consistently, new card and
old, with the numbers. **A good answer:** "scale test done", and which cards you used.

## 52. One approval: a read-only look at the server's size (entry 261 section 6), about one minute

**Answered 2026-09-28**: "run the server read", and the command ran at 17:45 UTC. Two processors, 11.9 GB of memory with 10.2 GB
available and no swap, 36 GB free of 45 GB, load 0.64. It has room; `docs/DETECTION-LEARNING-STUDY.md` section 6 has the sizing.

**What:** when you are at the computer, say "run the server read". I then run this one command, which stops for your approval in the
panel because every ssh does:

`ssh -i "C:\Users\Airwolf\Documents\ssh-key-2026-03-25.key" ubuntu@ssh.pissinhot.com "nproc; free -m; df -h /home; uptime"`

**Why:** entry 261 asked whether the server has the room to re-read every sent target by itself. This prints its processor count, memory,
free disk and load, and changes nothing. **A good answer:** "run the server read", then approve it once.

## 51. Five shots into a black diamond, and one scan (about 20 minutes at the range)

**Answered 2026-09-28 (entry 254). Nothing to do: no shots needed.** Your aim point card of 26 September already had a real shot through
the black of its C diamond. Its bright core is 0.194 in across, about what a hole shows on white paper; the simulation's holes in black
were far smaller than real ones, which is all that was wrong. Cut from the scan and set into GroupLab's own drawing of the C sheet, the E
bull and every C3 diamond, that real hole is found every time, with the caliber named or not. So the C3 zeroing grids are in the library
now, and the C and E sheets need no warning.

**Opened 2026-09-28 (entries 251 and 252, question 64).** **Needed:** print `C:\Dev\grouplab-local\zero-concepts\C3-GL-ZERO-MOA-100Y.pdf`
(or the MIL one) at 100 percent, fire five rounds at the diamond at any distance so most land **in its black**, and scan the sheet at 600
dpi like the others; put the scan with the rest. **Why:** the C3 grids are built, but the check before release found that GroupLab refuses
a hole in solid black as too small in its simulation; the same happens on the C and E bulls. Whether it happens with real holes is the one
thing a simulation cannot say, and one scan settles it. The GroupLab application will not read this printout yet (the C3 sheets are held
back), so I measure the scan here. **A good answer:** the scan, and which calibre it was.

## 50. One more short sitting with the Fold 7 and the tablet, later (nothing to do yet)

**Added 2026-09-28 (entries 253 and 255): about thirty minutes now, and the camera test of request 33 is part of it.** Have a printed
GroupLab 5x5 sheet on a table in ordinary room light for the camera test. The sitting also retakes every phone and tablet picture on the
website (entry 253). When the checks that need nobody are done, the panel and the top of this file give you a few short steps with the
Fold 7 over that sheet; nothing else needs your hands.

**Opened 2026-09-28 (entries 250 and 252).** **Needed:** the phone and the tablet on the charger, unlocked, Wireless debugging off and on,
Stay awake on, for about twenty minutes, **once a line at the top of this file starting "READY FOR THE PHONE AND TABLET:" says so**; not
before. **Why:** three things can only be checked on the devices: the Targets preview now showing a sheet's words and Letter above A4 on the
phone (entry 250), the new C3 zeroing grids on the phone, and how long "Shots Needed to Zero" takes to work out on the Fold and the tablet
(entry 252, which asks for those timings). **A good answer:** nothing written; the devices on and reachable when the line appears. Until
then they can stay put away.

## 49. How GroupLab should look on the phone: A, B or C

**Answered 2026-09-28 (entry 246): B, cards for the thumb. Built, and in nightly 115; nothing to do.** Real screenshots replace the
drawings. The Fold 7: [first run](../figures/screens/phone/fold-firstrun-light.png) ([dark](../figures/screens/phone/fold-firstrun-dark.png)),
[capture](../figures/screens/phone/fold-capture-light.png) ([dark](../figures/screens/phone/fold-capture-dark.png)), [a result](../figures/screens/phone/fold-result-light.png)
([dark](../figures/screens/phone/fold-result-dark.png)), [Targets](../figures/screens/phone/fold-targets-light.png) ([dark](../figures/screens/phone/fold-targets-dark.png)),
[Settings](../figures/screens/phone/fold-settings-light.png) ([dark](../figures/screens/phone/fold-settings-dark.png)). The tablet: [a result held
sideways](../figures/screens/phone/tab-result-landscape-light.png) ([dark](../figures/screens/phone/tab-result-landscape-dark.png)), [upright](../figures/screens/phone/tab-result-portrait-light.png)
([dark](../figures/screens/phone/tab-result-portrait-dark.png)), [Sessions](../figures/screens/phone/tab-sessions-landscape-light.png), [Targets](../figures/screens/phone/tab-targets-landscape-light.png),
[Settings](../figures/screens/phone/tab-settings-landscape-light.png). The Fold held sideways: [a result](../figures/screens/phone/fold-result-landscape-light.png) ([dark](../figures/screens/phone/fold-result-landscape-dark.png)), [Sessions](../figures/screens/phone/fold-sessions-landscape-dark.png).
The new icon beside GroupLab Dev's: [Fold](../figures/screens/phone/icons-fold.png), [tablet](../figures/screens/phone/icons-tab.png).
The Features page shows them for the phone's own features. The result is your Dominus K scan of 26 September.

**Needed:** your pick of three looks for the phone and tablet, or a mix of them. **Why:** entry 243 section 3.5 asked to bring the
desktop's look to the phone, and the look is not changed without you. **Where:** the page is private, at
https://claude.ai/artifact/LvBKewGhR2BaVRYfKeCdpz, and the same file is on this computer at
`C:\Dev\grouplab-local\design-concepts\phone-concepts-2026-09-27.html` (open it in a browser). It shows today's phone screens and
then each concept on four screens (the first run, a result, Settings and the new Targets screen), in dark, light or both.

- **A, the desktop carried over:** flat surfaces with hairlines, IBM Plex, a top bar with the wordmark, one amber button a screen. Closest to the desktop.
- **B, cards for the thumb:** the desktop's colors and type on rounded panels, big tappable choices, figure tiles. Most like other Android apps.
- **C, readout first:** a result opens on mean radius in large amber type with the other figures beneath; one first-run question a page. The most change.

**A good answer:** one letter, or parts from more than one ("C's result screen, A for the rest"). Only the chosen one is built, and its
screenshots from the Fold 7 and the tablet then replace these drawings. Nothing else waits on it: tonight's accent fix (the phone's
blue is now the desktop's amber) is a mismatch fixed, not a choice.

## 48. The Features page and the optic tour stop are up

**Answered 2026-09-27 (entry 242). Nothing to do; look when you like.** https://grouplab.org/features/ lists every feature by group, each
with the build it arrived in, its platforms and links to the tour, the user guide and the research behind it, and the three newest at the
top and on the home page. https://grouplab.org/tour/optic/ is the "Made for your optic" stop, with the Targets screen filled in for 100
yards at 10x and at 4x; every number on it is the generator's own. The planning session will look it over for anything missing.

## 47. Your benchmark questions: what has run, and what the survey page shows

**Answered 2026-09-27 (entry 240). Nothing to do.**

1. **Has the Windows application run a benchmark on any PC?** Yes, yours: your desktop ran it on nightly 111 at 10:29 UTC on 27 September,
   1.8 seconds, at most 482 MB, all 25 holes found, and it went with a report. That is the first desktop benchmark; nightly 110 never ran
   one (request 40). Whether anybody else's PC has is in the server's counts, which I read in the server sitting that closes tonight's queue.
2. **The Android ones:** the Tab S8 Ultra's screen said 4.6 seconds, at most 446 MB, 25 of 25, sent with a report. The Fold 7's line is
   request 44, since it was locked. Beside the spike's reference for the published scan (7.9 s desktop, 17 s Fold 7), which is a different
   and larger piece of work than the benchmark's own sheet, so the two are not the same measure.
3. **The page waits for ten a group, as written.** Until then https://grouplab.org/survey/ shows your three machines by name, as the
   project's own test devices, and says "not enough reports yet" for everybody else; operating systems and memory show the same way.
4. **More memory on bigger phones** (your suggestion): adopted as a rule that reads what each device has, and measured first. Working
   above 8 megapixels changed no result that matters, so every phone stays at 8 and a big phone's memory is kept in reserve; the details
   are in `docs/ANDROID.md` section 14.

## 46. On or after Sunday 2026-10-04 12:00 UTC: one look at the Oracle backups

**Opened 2026-09-27 (entry 235). Not before Sunday 4 October, 12:00 UTC; then one minute.** In the Oracle console, **Storage**, **Block
Storage**, **Boot Volume Backups**: is there a backup of type **Full** created on 4 October, a little after 12:00 UTC, that expires about
**17 October**? That is the first Full of the schedule you changed on 27 September, and it is all that is needed to know the change worked.

A good answer: "Full, 4 October, expires 17 October", or what the list shows instead.

## 45. The Fold 7 and the Tab S8 Ultra: reconnect both, about five minutes

**Answered 2026-09-28 (entry 246). Nothing to do.** You reconnected both; two sittings ran: the Fold's benchmark and memory,
GroupLab Dev, look B on every screen of both, and the icons. The devices can be put away.

**Opened 2026-09-27 (entry 234).** Both dropped off the desktop tonight: the phone script's first run restarted the connection program
while nobody was there to answer it, the Fold 7 now refuses connections (its Wireless debugging closed while it slept) and the tablet left
USB. The script is fixed so it cannot do that unattended again. Tonight's queue needs both: GroupLab Dev's first install and its log
(entry 234), the memory a picked photo costs on each (entry 239), the benchmark numbers (entry 240 and request 44), and the tablet's
side by side layout and the phone's Targets screen (entry 243).

1. **Fold 7:** unlock it; Settings, Developer options, **Wireless debugging** off and on again; in Developer options turn **Stay awake**
   on (the screen stays on while it charges); put it on the charger unlocked. While it is open, request 44's line is on GroupLab's
   Settings screen.
2. **Tab S8 Ultra:** unplug the USB cable and plug it back in; if the tablet asks "Allow USB debugging?", tick **Always allow** and press
   **OK**; turn **Stay awake** on too, and leave it on the charger unlocked.

A good answer is "both reconnected". Nothing on either is touched except GroupLab and GroupLab Dev, and the notifications on the lock
screen are never read.

## 44. The survey page: one line from the Fold 7's screen

**Answered 2026-09-28 (entry 246). Nothing to do.** Read over the desk's connection from the Fold's Settings: last run 27 September
18:22, 2.6 seconds, at most 438 MB, 25 of 25 holes, sent with a report. It is on https://grouplab.org/survey/ beside the desktop and the
tablet.

**Opened 2026-09-27 (entry 241).** The page is at https://grouplab.org/survey/ once the site has published this entry (about eight
minutes after the push). It shows your three machines by name as the project's own test devices: the desktop (1.8 s, from the desktop's
own settings) and the Tab S8 Ultra (4.6 s, from its Settings screen). **The Fold 7's result is missing** because the phone was locked
when the others were read, and I do not unlock your phone.

**What to do, about a minute:** on the Fold 7, open GroupLab, then Settings, and scroll to **The benchmark**. Tell the planning session the
line that begins "Last run", for example "Last run 27 September 2026, 04:15. The benchmark took 5.1 seconds and at most 450 MB of memory,
and found 25 of its 25 holes." That line is all that is needed; it goes on the page as the Fold's result.

**Also on the page, and nothing for you to do:** everybody else's figures appear there once the server side of entry 241 is installed
(the new survey worker and the site sync that keeps its figures in place), which I do with sudo in the same sitting as request 21. Until
then that half says "not enough reports yet", which is also true: a group needs ten machines before it is shown.

## 43. The Tab S8 Ultra: GroupLab on it, what the big screen showed

**Answered 2026-09-27 (entry 236).** Nothing to do. From now on the tablet is used only when a layout change needs it, like the older phones.

1. Nightly 111 is on the tablet and the Fold 7, and each has been started once; the Fold's first-run questions wait for you. The tablet's were answered with your choices during the check, and it was put back to 111 afterwards, so answer them again when you open it.
2. Portrait, landscape, Settings and a window at half and a third of the screen all work. The published sample analyzed the same as on the Fold (25 shots), in about 58 s against the Fold's 50 s, and about 555 MB at most.
3. **Plainly broken, fixed:** when the tablet restarted GroupLab's window while the old one was still closing (a change of window mode does it), GroupLab stopped. Fixed from the next nightly, and tried four times on the tablet without a stop.
4. **For the plan:** in landscape the analysis is a phone-width column in the middle of the screen; a big screen should show the plot and the sheet side by side. DeX was not tried (it is off).
5. Screenshots and logs are in `C:\Dev\grouplab-local\android-tabs8u-110\`.

---

## 42. GroupLab is back on your Fold 7

**Answered 2026-09-27 (entry 235 section 4).** Nothing to do. The nightly app (the APK signed with the upload key, now nightly 111) is installed
over wireless debugging and started once; it is waiting on its first-run sharing questions for your answers. It is the side-loaded copy, so a
later nightly APK installs over it, and the Play copy cannot until this one is uninstalled.

---

## 41. The Play build on your Fold 7, driven from the desktop: what it did

**Answered 2026-09-27 (entry 232).** Nothing to do; this is the report.

1. Connected over wireless debugging with no help needed. The first run had already been answered; Settings matched your choices except targets, which said "Send every target automatically", now **Ask me each time** as you asked; error reports and the survey are on. 110 offers no benchmark on the phone (confirmed; the next nightly does).
2. Cold start to the first screen in 0.52 s. The published 600 dpi sample (a 17 MB scan) analyzed correctly, 25 shots, in about 50 s from choosing it to the result, twice. Turning the phone kept the result. No crash and no ANR in the phone's log.
3. **Wrong and fixed:** the phone asked for the distance in meters and gave sizes in cm on a US phone (it could not see its region), and Settings showed the version as 0.2.0 rather than 0.2.0-nightly.110, which error reports carry too. Both are fixed from the next nightly.
4. **For the plan:** the phone has no Targets screen yet; 50 s for a large scan wants a progress line; the Play copy cannot be replaced by a nightly APK (uninstall first).
5. Screenshots and the log are in `C:\Dev\grouplab-local\android-play-110\`; the test file pushed to the phone was removed.

---

## 40. Did the benchmark run when you clicked Yes? No, and the window no longer leaves you wondering

**Answered 2026-09-27 (entry 227 section 2).** Nothing to do; this is the answer to your question.

**No, it did not run.** In nightly 110, Yes on the survey question only switched the survey on. The benchmark was a separate button
under the question, and answering closed the whole question, button included, so it was never offered again on that screen. Your own
log on this PC says the same: at 05:18 on 27 September (UTC) it records your Yes and a first report sent with no analyses and no
benchmark in it, and no benchmark run at all. The report holds only what the first run screen listed; nothing else was read.

**What changes, from the next nightly:** after Yes, the window asks whether to run the benchmark now or later instead of closing. Running
it shows its progress and a Cancel button, and says when it finished and what it found. Settings, under Sharing, shows whether the survey
is on, when the benchmark last ran and its result, and a button to run it now. The Android app gets the same.

---

## 39. The first full Oracle backup: one look in the console after Sunday 2026-09-27 09:00 UTC

**Answered 2026-09-27 (entry 235).** Your screenshot showed the first Full backup, and the schedules; you moved the weekly Full to Sunday
12:00 UTC, kept 13 days, so it no longer lands on the daily's two day retention. `docs/RESTORE.md` has all of it. Request 46 is the one
look to confirm the new schedule works, on or after 4 October.

What this request asked, kept for the record: five minutes, whenever you are next near the console.

The first backup (26 September, 09:01 UTC) is **Incremental**. Oracle keeps the chain of incrementals it needs by itself, so a restore
works today, but it rests on that chain. The policy makes a **Full** backup every Sunday, the first due on 2026-09-27 at 09:00 UTC. Once
one exists, the server can be brought back from that one backup alone.

In the Oracle console, **Block Storage, Boot Volume Backups**: look for **a second row whose type is Full**, state Available, created on
27 September. **A good answer:** "there is a Full backup dated 27 September", or a screenshot. If there is none by Monday, say so, and
the policy gets checked.

---

## 38. The Microsoft Store: your account, the name, and the keys that let releases go there by themselves

**Opened 2026-09-25. Entry 224 section 3.** The Store package is built (CI makes it on every push) and `release.yml` sends each tagged
stable release to the Store by itself once these are in place. Part A is one sitting, about thirty minutes, mostly Microsoft's identity
check. Part B comes after I reply that the first package is ready, and is the Store's one hand-made first submission.

**Part A**

1. **The developer account.** At https://storedeveloper.microsoft.com, sign up as an **individual** developer (free). It asks for an
   identity check; that is the part that takes time.
2. **Reserve the name.** In Partner Center, **Apps and games**, **New product**, **MSIX or PWA app**, name `GroupLab`.
3. **The identity, into the repository's variables.** In the new product, **Product management**, **Product identity**, copy four values,
   then in PowerShell (each asks for its value):

```powershell
gh variable set STORE_IDENTITY_NAME -R oRAirwolf/grouplab            # Package/Identity/Name
gh variable set STORE_PUBLISHER -R oRAirwolf/grouplab                # Package/Identity/Publisher, the CN=... line
gh variable set STORE_PUBLISHER_DISPLAY_NAME -R oRAirwolf/grouplab   # Package/Properties/PublisherDisplayName
gh variable set STORE_PRODUCT_ID -R oRAirwolf/grouplab               # the Store ID, 9 followed by eleven letters and numbers
```

4. **The Entra application.** In Partner Center, **Account settings**, **Tenants**, associate your Entra tenant if it is not already. In
   Entra, register an application (`grouplab-store-publisher`, single tenant, no redirect) and make it a client secret. Back in Partner
   Center, **Account settings**, **User management**, **Microsoft Entra applications**, add it with the **Manager** role. The Seller ID is
   under **Account settings**, **Legal info** (or **Identifiers**).
5. **The four secrets**, in PowerShell (each asks for its value and does not show it):

```powershell
gh secret set AZURE_AD_TENANT_ID -R oRAirwolf/grouplab
gh secret set AZURE_AD_APPLICATION_CLIENT_ID -R oRAirwolf/grouplab
gh secret set AZURE_AD_APPLICATION_SECRET -R oRAirwolf/grouplab
gh secret set SELLER_ID -R oRAirwolf/grouplab
```

**A good answer for part A:** "done", once the eight lines above each say they were set. I then build the first package with your identity
as a draft release and tell you it is ready.

**Part B, after I say the draft is ready.** In Partner Center, **Start your submission** for GroupLab:
- **Pricing and availability:** free, all markets.
- **Properties:** category Sports; privacy policy `https://grouplab.org/research/what-grouplab-sends/`.
- **Age ratings:** answer as `docs/store/LISTING.md` says; expected 3+.
- **Packages:** upload `grouplab-win-x64.msix` from the draft release on github.com/oRAirwolf/grouplab/releases. Where it asks why the
  package needs **runFullTrust**, paste: "GroupLab is a desktop application built with .NET. It needs full trust to open the scans and
  photographs the person chooses, to print targets, and to save reports and sessions where the person chooses."
- **Store listing:** paste each block from `docs/store/LISTING.md` and upload its five screenshots.
- **Submit.** Certification takes a few days. After it, every tagged release is sent to the Store by itself.

---

## 37. Signing the Windows download: a choice, whenever suits you

**Answered 2026-09-25: not yet** (entry 224 section 2). Alan: "As of right now, nobody is getting windows smart screen warnings. Lets
hold off for now." The comparison stays in `docs/RELEASE-PLAN.md` for when it is revisited. **Opened 2026-09-25. Entry 219 item D4.** Today a download of GroupLab shows Windows' SmartScreen warning. `docs/RELEASE-PLAN.md` sets out
the options with their current costs. **The recommendation: Azure Artifact Signing, about $120 a year**, for the direct download, signed
from CI with no key file for anyone to keep; and later the Microsoft Store, which is free for individual developers and removes the warning
for Store installs, once an MSIX package is worth making. An EV certificate no longer skips the warning, so it is not worth its price.

**A good answer** is one of: "Artifact Signing, go ahead" (then Code writes the exact setup steps, which need your Azure sign-in and an
identity check); "the Store first"; "an OV certificate"; or "not yet". Nothing is bought or set up until you say.

---

## 36. The Play Store install on the Fold 7: one try, two minutes

**Answered 2026-09-27 (entry 231).** You installed GroupLab on the Fold 7 from the Play Store through the testers' link and it opened
to the first-run sharing window. Nothing more to do. The Play copy and a nightly APK cannot be installed over each other; `docs/ANDROID.md`
section 12 says how to move between them. **Update (entry 234):** you uninstalled the Play copy on purpose, since
installing nightlies over adb is easier; from the next nightly, testing uses **GroupLab Dev**, which installs beside the Play copy, so the
two never have to be swapped again.

What this request asked, kept for the record:

On the Fold 7, signed in with an account on the testers list, open https://play.google.com/apps/internaltest/4701684356677501640, accept
the invitation, then install GroupLab from the Play Store page it leads to. If the side-loaded copy is still on the phone, uninstall it
first: Play will not replace a copy signed with a different key.

**A good answer:** "the Play Store install works" (it opens and shows its first screen), or what it said instead. Once it works, the
next request is automatic Play uploads, set up the way the Microsoft Store's are; it waits until then.

---

## 35. Backups and automation: the three things only you can do, one sitting, about fifteen minutes

**Answered 2026-09-27** (entry 230): all three steps are done. Steps 1 and 2 were done on 25 September; the first boot volume backup appeared in the console on 26 September at 09:01 UTC, state Available, type Incremental. Request 39 is the one look left, for the first Full backup. **Opened 2026-09-25. Entry 222.** Everything else in entry 222 is Code's: the nightly backup and its weekly restore test, the server
archiving submissions by itself, and the cleanup with its safety net. These three need you. Nothing here is urgent enough to interrupt
anything; the one that matters most is **3**, because today no copy of the server as a whole exists anywhere but on the server.

**1. The backups repository.** In PowerShell on this machine:

```powershell
gh repo create oRAirwolf/grouplab-backups --private --description "GroupLab nightly backups. Private; never made public."
```

A good result: a line ending `grouplab-backups`. Code never creates repositories, so this one is yours.

**2. The archive token**, so the server can put submissions in the private archive without this computer. On github.com: your picture,
top right, then **Settings**, **Developer settings**, **Personal access tokens**, **Fine-grained tokens**, **Generate new token**.
- Token name: `grouplab-archive-worker`. Expiration: 366 days.
- Resource owner: `oRAirwolf`. Repository access: **Only select repositories**, and choose `grouplab-submissions-archive` only.
- Permissions, Repository permissions: **Contents: Read and write**. Leave everything else as it is (Metadata read only is added by
  itself).
- **Generate token** and copy it. Then in the server's shell, where it will ask for the token and not show it:

```bash
sudo /usr/local/sbin/grouplab-set-archive-token
```

A good result: `the archive token is set`. Code installs that script before your sitting and says so in the panel; nobody but you ever
sees the token.

**3. A whole-server backup that lives off the server** (entry 222 section 6.2). The server's own HestiaCP backups are one copy a user,
kept on the server itself, so a disk failure or a bad command takes them with everything else. Oracle Cloud can copy the whole boot
volume every day, off the machine, inside the free tier. In the Oracle Cloud console:
- **Storage**, **Block Storage**, **Backup Policies**, **Create Backup Policy** in the compartment the server is in. Name it
  `grouplab-daily`. Add two schedules: **Incremental, Daily**, keep **2**; and **Full, Weekly**, keep **2**. That is four backups at most,
  inside the free tier's five.
- **Compute**, **Instances**, the server, **Boot volume**, then the boot volume's page, **Edit** (or **Assign backup policy**), choose
  `grouplab-daily`, save.

A good result: the boot volume's page shows the policy, and the next day **Boot Volume Backups** lists one. Until then Code's use of sudo
on the server stays limited to GroupLab's own files and its installer.

**Nothing else needs you.** The scheduled tasks on this computer run as you while you are logged on, so they need no password.

---

## 34. The hardware survey's server side: in the same sitting as 31, five more minutes

**Answered 2026-09-25** (entry 223): the reload and the checks passed, and the survey is now open. **Opened 2026-09-25. Entry 219 item D1, entries 207 and 208. The planning session checks these commands before you run them.** The
**Partly done 2026-09-25** (entry 220): `install.py --survey` ended `done` and `nginx -t` passed. Only the reload and the checks below
are left, which you already have.
survey is built in GroupLab and switched off: it is not asked and nothing is sent until this is installed and the next entry turns it
on. It adds a worker with no network that counts each report and deletes it within the hour. The receiver arrived with the site on
2026-09-25 and refuses every report until the survey is turned on, so nothing is stored before this is installed.

**1. In the server's shell**, after request 31, having copied these from the repository to `/home/ubuntu/grouplab-server/`:
`website/server/install.py`, `website/server/nginx.ssl.conf_grouplab`, `website/server/grouplab-survey-worker.py`,
`website/server/grouplab-survey-worker.service` and `website/server/grouplab-survey-worker.timer`.

```bash
cd /home/ubuntu/grouplab-server
sudo python3 install.py --survey --dry-run
sudo python3 install.py --survey
sudo nginx -t
sudo systemctl reload nginx
curl -sS -o /dev/null -w '%{http_code}\n' https://pissinhot.com/
curl -sS -o /dev/null -w '%{http_code}\n' https://grouplab.org/
curl -sS -X POST -o /dev/null -w '%{http_code}\n' https://grouplab.org/api/survey.php
systemctl list-timers grouplab-survey-worker.timer --no-pager
```

Run `nginx -t` and the reload only if the installer says the include changed; it prints them itself when it does.

**A good result.** The installer ends `done`; `nginx -t` says the test is successful; the two sites answer `200`; the empty POST to
the survey answers `503`, which is the receiver saying it is closed; and the timer is listed with a next run. Send those lines, and the
survey is turned on in the next entry.

---

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

## 32. The only copy of the submissions on this machine: a backup, your decision

**Answered 2026-09-27 (entry 235): "The archive is enough."** No second copy of `C:\Dev\grouplab-submissions` is set up.

What this request asked, kept for the record. **Opened 2026-09-25. Entry 215 section 4.** Once request 31 has run, the server no longer holds any
submission. What remains is `C:\Dev\grouplab-submissions` on this machine and the private archive on GitHub. The folder here is
outside the repository and, as far as anyone here knows, outside any sync.

**A suggestion, nothing more:** keep a second copy of that one folder somewhere that is not this disk, for example an external drive
copied to now and then, or the folder added to whatever backup this machine already has. I have set nothing up and will not.

**A good answer.** "Backed up to ..." or "the archive is enough". Either closes this.

---

## 31. Take other people's photographs off the web server: the pull again, fixed (the server steps are done)

**Answered 2026-09-25: done by Code** (entry 222 section 2.4): 9 on grouplab.org and 18 on pissinhot.com archived, proven and removed
from the server; `Test-SubmissionsArchive.ps1` restored and verified all 27 under Windows PowerShell 5.1 and PowerShell 7. Nothing is left for
you here. **Opened 2026-09-25. Entries 215 to 218, rewritten by entry 220. The planning session checks these commands before you run them.**
Your first run did the server side cleanly and pulled three submissions, then stopped at the archive with `gh.exe : release not
found`. That was Windows PowerShell 5.1 treating gh's normal words on stderr as fatal. **Nothing was removed from the server**: removal
comes after the archive in the script, and the first archive call stopped it. Every program the scripts run now goes through one helper
that judges by the exit code alone, and the archive was tested for exactly this case under Windows PowerShell 5.1 and PowerShell 7
(`tests/powershell/archive-tests.ps1`, now in CI as well). A dry run now changes nothing and says what it would do.

**Do not repeat the server steps** (`install.py`); they are done.

**In PowerShell on this machine**, a dry run of each first, then the real run. The first is grouplab.org, the second the old
pissinhot.com folder:

```powershell
cd C:\Dev\grouplab\scripts
.\Get-TargetSubmissions.ps1 -RemoteRoot /home/airwolf/web/grouplab.org/private/ready -WhatIf
.\Get-TargetSubmissions.ps1 -RemoteRoot /home/airwolf/web/grouplab.org/private/ready
.\Get-TargetSubmissions.ps1 -WhatIf
.\Get-TargetSubmissions.ps1
.\Test-SubmissionsArchive.ps1
```

**A good result.** Each dry run ends `Dry run: would archive and then remove N from the server; 0 would be kept there. Nothing was
changed.` (9 on grouplab.org). Each real run lists every folder as `archived and removed from the server`, then `N removed from the
server; 0 kept there.` and the ledger line. The last command ends `... restored and verified, 0 problem(s)`. A folder that is kept is
listed with its reason and stays on the server and here. Send the last lines of each.

**Why now.** It is other people's photographs on a web server, kept longer than they were promised.

---

## 30. Your older test phones: a note, nothing to do

**Opened 2026-09-25. Entry 207. Answered 2026-09-25**, entries 211 and 212, and kept as a note of what the phones are and when they are
needed; nothing is asked now. The **Essential PH-1**, Android 10, Snapdragon 835, 4 GB, read from the phone itself; the **Galaxy S20 5G**,
Android 13, 8 GB; and a **OnePlus 6T**, Android 11 (entry 213), optional at the milestone. They come out only at the milestones in `docs/ANDROID.md`: once
before the first Play closed testing release, in one sitting, and otherwise only for a problem the Fold 7 and the emulator cannot show.

---

## 29. The Fold 7: three steps, then put it away

**Opened 2026-09-25. Entry 209. Answered 2026-09-25**, entry 211: both screens turned upside down, and Google Drive offered a
folder in the picker. Everything that could be run over adb is done (entry 209: the resolutions, the cameras, the start up).
These three need your hands. GroupLab spike is already installed and its screen shows a **Screen** list, a **Run detection** button and a
**Choose a folder** button.

1. **Folded, upside down.** With the phone folded, open GroupLab spike on the cover screen. Turn the phone upside down, so the USB-C port
   is at the top, with rotation not locked. **Look for:** the spike's screen turns to read the right way up within a moment.
2. **Unfolded, upside down.** Unfold it and do the same on the inner screen: turn it so the hinge side that was on your left is on your
   right. **Look for:** the same, it turns. (Upside down was fixed in entry 205 and checked over adb with rotation locked at 180; this is
   the sensor doing it by itself.)
3. **The folder picker.** Press **Choose a folder**. Android's picker opens. Open its side menu (the three lines at the top left) and look
   at what is listed. **Look for:** whether **Google Drive** and **OneDrive** appear there at all, and if you tap one, whether its
   **Use this folder** button is offered or greyed out. Then press Back or cancel; there is no need to choose anything. If you do choose a
   folder, the spike notes only which app it came from, never the folder's name.

**A good answer.** Three short lines: "1 turned", "2 turned", and for 3 which of Google Drive and OneDrive appeared and whether either let
you use a folder. Then the phone can be put away.

**Why.** Step 3 decides whether the sync folder of `docs/ANDROID.md` section 8 is possible on Android at all.

---

## 27. Android: fold, unfold and turn the spike, about five minutes

**Opened 2026-09-25. Entry 202. Answered 2026-09-25**, entry 205: all passed; upside down portrait is fixed since. GroupLab spike is installed on the Fold 7 (its icon says "GroupLab spike"). Its screen lists, under
**Screen**, every size it has been given: the time, the size in dp, a word (compact, medium or expanded), and the pixels. This checks
that folding and turning keep it working, which only hands can do.

1. With the phone folded, open **GroupLab spike** on the cover screen and press **Run detection**. Wait until a line naming
   `gl-cf25-ltr-d-25-shots-600-dpi.png` appears, up to a minute.
2. **Unfold the phone** while it is open. It should carry on without restarting: the lines already there stay, and a new line appears
   saying **medium** or **expanded**, with the two panels now side by side.
3. **Turn the phone sideways**, then back. Each turn adds a line; nothing already there disappears.
4. **Fold it again.** It should go back to **compact** on the cover screen, with the two panels one above the other, and all the lines
   still there.
5. Settings, Display, **Font size and style**: move the font size to the largest, go back to the spike, and look at whether any text
   is cut off at its edges. Put the font size back.
6. The Tab S8 Ultra, if it is to hand: steps 1, 3 and 5 on it. Not needed for the first report.

**What to look for, and what to send back.** Anything that restarted (the lines vanished), anything drawn under the hinge or cut off,
and anything that took more than a moment to settle. A photograph of the phone's screen after step 4 is the easiest answer, and it
never needs to show anything but the spike. Plain words are fine too: "all fine", or what went wrong at which step.

**Why.** Entry 199 section 1.2: folding and turning are ordinary events and must keep the work. This decides whether Avalonia is right
for the phone before any real screen is built on it.

---

## 26. Android: the Fold 7 and the tablet, ready for a test build

**Opened 2026-09-25. Entry 198 section 3.1. Answered 2026-09-25**, entry 202: the Fold 7 is paired and `adb` lists it as
`SM_F966U1`. Wireless debugging turns itself off after a while; if it has, entry 202 says a one-line request goes here.

**On the Fold 7:** Settings, About phone, Software information, tap **Build number** seven times. Then Settings, **Developer
options**, turn on **Wireless debugging**, open it, and tap **Pair device with pairing code**. It shows an address with a port, and a
six-digit code.

**In PowerShell on this machine**, with the address and port the phone shows for pairing, then the code when asked:

```powershell
C:\Dev\tools\android-sdk\platform-tools\adb.exe pair <address:port shown under the pairing code>
C:\Dev\tools\android-sdk\platform-tools\adb.exe connect <address:port shown on the Wireless debugging screen itself>
C:\Dev\tools\android-sdk\platform-tools\adb.exe devices -l
```

The same for the Tab S8 Ultra if you want it tested in the same sitting; it is not needed for the first run.

**Why.** The first stage ends with the detector running on the phone and its time measured there. Nothing can be installed on it
without this.

**A good answer.** The last command lists the phone with the word `device` after it and a model name beginning `SM-F`. Say "phone
paired"; the addresses do not need to be sent. Wireless debugging turns itself off after a while, so it may need turning on again
when the test build is ready.

---

## 25. Android: the .NET Android workload and the Android SDK

**Opened 2026-09-25. Entry 198 section 3.2. Answered 2026-09-25**, entries 200 and 201: the workload is installed (10.0.401), the SDK
is in `C:\Dev\tools\android-sdk`, `ANDROID_HOME` points at it, and `adb version` prints `Android Debug Bridge version 1.0.41`. The first
try at the SDK step failed at restore; entry 201 found the likelier cause was running it while the workload install was still going in
the administrator window. **Nothing more to do.** The commands below are the ones that worked, in the order to run them, for the record
or another machine.

**First, in PowerShell run as administrator**, because the workload installs into Program Files. Wait until it prints
`Successfully installed workload(s) android.` before going on:

```powershell
dotnet workload install android
```

**Then in ordinary PowerShell.** A throwaway Android project in the temporary folder, only to ask the build to fetch what it needs; the
SDK goes in `C:\Dev\tools\android-sdk`, the Android SDK licenses are accepted on your behalf, and the throwaway is deleted.
`RestoreConfigFile` points the restore at the repository's package sources, which is harmless and was part of the run that worked:

```powershell
dotnet new android -o "$env:TEMP\gl-android-probe"
dotnet build "$env:TEMP\gl-android-probe" -t:InstallAndroidDependencies -f net10.0-android -p:RestoreConfigFile=C:\Dev\grouplab\nuget.config -p:AndroidSdkDirectory=C:\Dev\tools\android-sdk -p:JavaSdkDirectory="C:\Program Files\Eclipse Adoptium\jdk-17.0.20.101-hotspot" -p:AcceptAndroidSDKLicenses=True
Remove-Item -Recurse -Force "$env:TEMP\gl-android-probe"
setx ANDROID_HOME C:\Dev\tools\android-sdk
```

**Why.** Installing the test build on the phone, and building it here between CI runs, needs both.

---

## 24. Error reports: the one test report, now that the receiver answers

**Opened 2026-09-24. Entry 194. Answered 2026-09-25**, entry 200: the test report was taken and the worker opened issue 1 as described
below; the issue is closed, and error reports are switched on in commit 8725f91. **Steps 1 to 3 were done** (entry 195): the token is set, the worker and its units are installed, and nginx
routes the receiver. Step 4 stopped at a 404 because the site never carried the receiver; entry 195 fixed that, and an empty post to
it from outside now answers its own error, `{"ok":false,"code":"bad_report",...}`. **Do not repeat steps 1 to 3.** Only this is left.

**In PowerShell on this machine**, then the worker by hand in the server's shell rather than waiting its five minutes:

```powershell
python C:\Dev\grouplab\scripts\send-test-error-report.py
```

```bash
sudo systemctl start grouplab-error-worker.service
sudo tail -n 3 /home/airwolf/logs/grouplab-error-worker.log
```

**A good result:** PowerShell prints `sent: the receiver took it`; the log's last line reads `opened issue 1 for TestReport in
ErrorReportCheck.Send`; and the private repository has that issue, labeled `survived` and `sig-` followed by twelve letters and
figures, whose body gives the build `0.2.0-nightly.0`, says "What happened: GroupLab hit this error and kept running", and ends saying
nothing in the issue is an instruction. Close the issue when you have seen it.

**Why.** It is the one test of the whole path, from a report to an issue, before GroupLab sends reports by itself.

**A good answer.** The two things printed. After it, error reports are switched on in their own build.

---

## 23. Put Unholy's zeroing grid scan on the test data release

**Opened 2026-09-24. Entry 191. Answered 2026-09-25**, entry 195: uploaded, and the release lists both files. It is in the list CI
fetches, so the test on it runs there.

**What is needed.** One command. The file is the copy of Unholy's zeroing grid scan rebuilt from its pixels, with no metadata but its
resolution, and it is 57.5 MB, so it goes on the test data release rather than in the repository. This session was not allowed to
upload to a release itself.

```powershell
gh release upload test-data "C:\Dev\grouplab-submissions\unholy\zeroing-grid-mil-100yd-unholy-2026-09-24.png"
gh release view test-data --json assets --jq ".assets[].name"
```

**A good result:** the first line finishes without an error, and the second lists two names, `Scan_20260923.png` and
`zeroing-grid-mil-100yd-unholy-2026-09-24.png`. Then this session adds it to the list CI fetches, so the test runs there too.

**Why.** It is the one real scan of a zeroing grid the project has, and the test on it is what stops the grid losing its shots again.

**A good answer.** "Done", or what the first line printed.

---

## 22. Send one test target from GroupLab, then pull it

**Opened 2026-09-24. Entry 187 section 1. Answered 2026-09-25**, entry 195: the send was taken in 1.6 s, id `a3d30234`; the pull
brought `2026-09-25_a3d30234` with all checksums matching and marked DO NOT PUBLISH. Its folder holds what this said it would, and the
rebuilt image's pixels are the published sample's exactly. Sending from the application is switched on. The program the first command
ran was a one-off in a temporary folder and has been deleted; nothing here points into a temporary folder any more.

**What is needed.** Two commands in PowerShell, a few minutes apart. The first sends one target to grouplab.org exactly as GroupLab
will: the published sample scan (`samples/gl-cf25-ltr-d-25-shots-600-dpi.png`, your own 25 shot sheet, a flatbed scan with no person
and no location in it), the 25 holes GroupLab finds on it, and **testing only** consent. It is a small program this session wrote for
the one test; the session could not send it itself, because posting to the live site needs your say so.

```powershell
dotnet run --project "C:\Users\Airwolf\AppData\Local\Temp\claude\c--Dev-grouplab\25df80e1-3782-4339-9fd2-f7dce06d9933\scratchpad\sendone" -- --send
```

**A good result:** two lines. `detected 25 marks; image target.png, 17602175 bytes, sha256 c52412d8...` and then
`answer after N s: 200 {"ok":true,"id":"xxxxxxxx"}`. The eight characters after `id` name the folder: today's date in UTC, an
underscore, and those eight, for example `2026-09-24_1a2b3c4d`. If the answer is anything but 200, or takes more than a minute, that
is worth a line back as it is; a slow send is what request 21 is for.

Wait three minutes for the worker, then pull:

```powershell
cd C:\Dev\grouplab\scripts
.\Get-TargetSubmissions.ps1 -RemoteRoot /home/airwolf/web/grouplab.org/private/ready
```

**A good result:** `Pulled 2 submission(s)`, the test and the photograph request 15 brought back, then `All checksums match.`, and
both named under `marked DO NOT PUBLISH`. The test's folder in `C:\Dev\grouplab-submissions` holds `001_target-rebuilt.png`,
`meta.json`, `DO-NOT-PUBLISH` and `CONSENT.txt`. `Pulled 0` means the worker has not finished yet; run the pull again in two minutes.

**Why.** Every part of sending has been tested against a stand in, and none of it against the real server. One real target from the
real program to the real worker is the test the upload page had before it opened.

**A good answer.** The two lines from the send and the last lines of the pull. Then sending is switched on in its own build, and the
test is added to request 12's list for removal.

---

## 21. Optional: longer timeouts for the application's receiver

**Answered 2026-09-27 (entry 235 section 3). Nothing to do.** Done without you, as you said to: the server's copy of the include was
already the repository's, so the five minute block was in and nothing needed reloading. The checks: the receiver answers an empty post
with 400, grouplab.org/targets/ 200 and pissinhot.com 200. The survey worker of entry 241 went in at the same sitting, and the four
workers' time limits, which systemd had been ignoring, are now in force.

**Opened 2026-09-24. Entry 165. Optional, and nothing waits on it.** After this was first written, the new receiver on the live site replied to an empty post
with its own 400 through the nginx include already installed, so the receiver is reachable without this. What the
block below adds is a five minute timeout for a large photograph on a slow line, and a server copy of the include that matches the
repository's. Whether and when sending is switched on is question 55, for the planning session.

**What is needed, in the server's shell.** Copy `website/server/nginx.ssl.conf_grouplab` from the repository to
`/home/ubuntu/grouplab-server/`, then:

```bash
cd /home/ubuntu/grouplab-server
sudo python3 install.py --intake --dry-run
sudo python3 install.py --intake
sudo nginx -t && sudo systemctl reload nginx
sleep 60
curl -s -o /dev/null -w '%{http_code}\n' -X POST https://grouplab.org/api/app-submission.php
curl -s -o /dev/null -w '%{http_code}\n' https://grouplab.org/targets/
curl -s -o /dev/null -w '%{http_code}\n' https://pissinhot.com/
```

**A good result:** the dry run names the nginx include as the one file it would replace and nothing missing; `nginx -t` says the
syntax is ok and the test is successful; and the three lines read **400**, **200** and **200**. The 400 is the new receiver
answering an empty post with "The target arrived without its package", which is right. A **404** on the first line would mean the include is
not the one expected, and nginx should be reloaded only after `nginx -t` passes.

**Why.** The application cannot run Turnstile, so it posts to its own receiver, `api/app-submission.php`, protected by the same size
limits, rate limits, hourly cap and disk floor as the upload page, and landing in the same quarantine for the same worker. The receiver
publishes with the site on its own. The include answers every `.php` name it does not list with a 404, so the new receiver needs its
own block there, with a longer timeout because a phone photograph on a slow line takes a while. The worker does not change.

**A good answer.** The output of the block.

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

## 19. Can the ST-4 sheet of 2026-09-20 still be scanned?

**Opened 2026-09-24. Entry 158 program A. Answered 2026-09-24**, entry 187 section 7: the sheet no longer exists. Program A steps 3
and 4 now wait on another commercial gridded sheet, which request 20 asks for in one line.

**What is needed.** If you still have the orange ST-4 sheet with the twenty groups, a 600 dpi scan of it, on the flatbed you used for the
GroupLab sheets. It is larger than Letter, so two overlapping scans are fine; say which half is which.

**Why.** What five shots tell you needs every shot placed, and in the photographs the holes of a group touch and merge: GroupLab found about
one shot in seven as a mark of its own. On a scan it tells one hole from two by their size.

**A good answer.** "The scan is <file>", or "the sheet is gone", which settles that program A needs another sheet shot for it.

---

## 18. Photographs of a scanned GroupLab sheet at 40 to 60 degrees

**Answered 2026-09-27 (entry 238).** Your nineteen photographs of the Dominus K sheet, from straight down to 66 degrees, did it: everything up to 36 degrees matched the scan, and from 38.5 degrees GroupLab started reading marks that are not shots. The limit is now 37 degrees, and the article on curled and angled paper shows it. No more photographs are needed, and the dim light did no harm.

**Opened 2026-09-24. Entry 157. Optional, and nothing waits on it: a limit is set without it.**

**What is needed.** Any GroupLab sheet you have shot and also scanned at 600 dpi, or one you will scan: five or six photographs of it at
about 40, 45, 50, 55 and 60 degrees off square, the phone's normal camera, the whole sheet in the frame, and the scan beside them. The
photographs can go in `C:\Dev\grouplab-range-2026-09-20\photos` or a new folder of the same kind; say which.

**Why.** Entry 157 asks for the angle past which GroupLab refuses a photograph to be measured, not chosen. Your 2026-09-20 photographs
reach 35 degrees and every one of them worked, so the measurement says the limit is somewhere above 35 without saying where. GroupLab
refuses past 40 for now (question 54). Photographs of a scanned sheet at steeper angles show where the holes and bulls stop agreeing with
the scan.

**A good answer.** "The photographs are in <folder>, of the sheet in <scan file>." Nothing else.

---

## 17. Take the test data release off the releases page

**Opened 2026-09-24. Entry 185. Answered 2026-09-24**, entry 188: Alan made the release a draft and it has left the releases page. The
`test-data` tag is still there; CI finds the draft through the API by that tag and the tests that read the file ran. If it ever has to
be undone, the command is `gh release edit test-data --draft=false`.

**What is needed**, in Git Bash on your machine, once the checks on the newest commit on main are green:

```bash
cd /c/Dev/grouplab
gh release edit test-data --draft=true
gh release list --limit 5
```

**A good result:** the list shows the builds and no longer shows "Test data, not a build". The release is not deleted: a draft is only
hidden from the public page, and its file stays attached.

**Why.** It sat among the builds, where people look for something to download. A draft is off that page. Its file is then no longer at
the public download address, so the CI job that can see drafts now fetches it and hands it to the tests, which was pushed first so this
command breaks nothing. The command needs your approval, which is why it is here.

**A good answer.** "Done", and the next CI run's test data job saying it read the release through the API. If that run cannot read the
file, this puts it back exactly as it was:

```bash
gh release edit test-data --draft=false
```

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

## 15. Install the worker that keeps the opt out, and send back the one it refused

**Opened 2026-09-24. Entry 183. Answered 2026-09-24**, entry 186: Alan ran it as written at 11:10 MDT. The dry run named the worker as
the one file to replace; the install kept the old one as a dated backup and re-enabled the timer; the refused submission went back to
quarantine and at 11:13 the log read `back from refused, tried again`, `rebuilt ... again`, `clean, clamdscan` and `ready, 1 files,
opted out of the public data set`. Its folder in ready holds the PNG, `meta.json` and `DO-NOT-PUBLISH`, and no `refused.txt`.
Opted out submissions are accepted from here on.

**One check, only if you are curious.** The installer removes its own older dated backups of a file when it makes a new one, so
`/usr/local/sbin` should hold exactly one `grouplab-intake-worker.py.*.bak`. This line lists them; one line of output is right,
and more than one means the pruning missed, which is worth a line back and nothing else. Do not delete anything.

```bash
ls -la /usr/local/sbin/grouplab-intake-worker.py.*.bak
```

**What is needed, in the server's shell.** First the fixed worker. Copy `website/server/grouplab-intake-worker.py` from the
repository to `/home/ubuntu/grouplab-server/`, then:

```bash
cd /home/ubuntu/grouplab-server
sudo python3 install.py --intake --dry-run
sudo python3 install.py --intake
```

**A good result:** the dry run names the worker as the one file it would replace and nothing missing, and the second
line installs it. The receiver has not changed.

Then the submission it refused, back to quarantine so the worker does it again. The first line shows what is there: a
PNG, `meta.json`, `DO-NOT-PUBLISH`, `refused.txt` and `.attempts`.

```bash
sudo ls -la /home/airwolf/web/grouplab.org/private/refused/2026-09-24_272b33e2
sudo mv /home/airwolf/web/grouplab.org/private/refused/2026-09-24_272b33e2 /home/airwolf/web/grouplab.org/private/quarantine/
sudo touch /home/airwolf/web/grouplab.org/private/quarantine/2026-09-24_272b33e2
```

Within two minutes the worker's timer runs. Then:

```bash
sudo tail -n 6 /home/airwolf/logs/grouplab-intake-worker.log
sudo ls -la /home/airwolf/web/grouplab.org/private/ready/2026-09-24_272b33e2
```

**A good result:** the log's last lines say `back from refused, tried again`, then that the photograph `was rebuilt by an
earlier run`, `rebuilt ... again`, `clean, clamdscan`, and `ready, 1 files, opted out of the public data set`; and the
folder in ready holds the PNG, `meta.json` and `DO-NOT-PUBLISH`, with no `refused.txt`.

**Why.** The receiver writes a `DO-NOT-PUBLISH` marker beside `meta.json` when the box is ticked, and the worker tried to
decode every file in the folder as an image, the marker included, so it refused the whole submission. The worker now
rebuilds only the files the receiver recorded, carries the marker through to ready, and refuses loudly if the marker and
`meta.json` ever disagree. That submission's original was already deleted by the run that refused it, so the worker
rebuilds again from its own PNG, through the same scan.

**A good answer.** The output of the three blocks.

---

## 14. Let the virus scanner take whole files as a stream

**Answered 2026-09-24.** Entry 183: Alan made the change, and the next upload's log read `clean, clamdscan`. Every upload is virus scanned from here on.

**Opened 2026-09-24. Entry 182.**

**What is needed, in the server's shell.** First clamd's limits. The backup goes to your server folder, which nothing reads as
configuration; the first line shows whether the package manages `clamd.conf` from debconf, and the fourth stops it doing so, so a package
upgrade does not put the old limits back; the third and sixth show the four values before and after.

```bash
sudo debconf-show clamav-daemon | grep -E 'debconf|StreamMaxLength|MaxFileSize|MaxScanSize'
sudo cp -p /etc/clamav/clamd.conf /home/ubuntu/grouplab-server/clamd.conf.before-stream
grep -E '^(StreamMaxLength|MaxFileSize|MaxScanSize|AlertExceedsMax) ' /etc/clamav/clamd.conf
echo 'clamav-daemon clamav-daemon/debconf boolean false' | sudo debconf-set-selections
for kv in 'StreamMaxLength 400M' 'MaxFileSize 400M' 'MaxScanSize 400M' 'AlertExceedsMax yes'; do k=${kv%% *}; if grep -q "^$k " /etc/clamav/clamd.conf; then sudo sed -i "s/^$k .*/$kv/" /etc/clamav/clamd.conf; else echo "$kv" | sudo tee -a /etc/clamav/clamd.conf >/dev/null; fi; done
grep -E '^(StreamMaxLength|MaxFileSize|MaxScanSize|AlertExceedsMax) ' /etc/clamav/clamd.conf
sudo systemctl restart clamav-daemon
until clamdscan --ping=1 >/dev/null 2>&1; do sleep 5; done; echo "clamd is answering"
head -c 60000000 /dev/urandom > /tmp/grouplab-stream-check.bin
clamdscan --stream --no-summary /tmp/grouplab-stream-check.bin; echo "exit $?"
rm /tmp/grouplab-stream-check.bin
```

**A good result:** the sixth command prints `StreamMaxLength 400M`, `MaxFileSize 400M`, `MaxScanSize 400M` and `AlertExceedsMax yes`;
then "clamd is answering"; then a line ending `OK` and `exit 0` for a 60 MB file, which the old 25 MB limit would have refused. If
anything goes wrong, `sudo cp -p /home/ubuntu/grouplab-server/clamd.conf.before-stream /etc/clamav/clamd.conf` and a restart put it back.

Then the worker that streams, and the installer that checks those limits. Copy `website/server/grouplab-intake-worker.py` and
`website/server/install.py` from the repository to `/home/ubuntu/grouplab-server/`, and:

```bash
cd /home/ubuntu/grouplab-server
sudo python3 install.py --intake --dry-run
sudo python3 install.py --intake
```

**A good result:** the dry run lists the worker as the file it would replace and names nothing missing. If it lists a clamd.conf line,
the first block did not take.

**Why.** The worker handed clamd an open file from inside its sandbox, and clamd's AppArmor profile refused it as a disconnected path, so
nothing has been scanned. Streaming sends the bytes instead, so nothing about the sandbox or AppArmor changes. The limits are 400 MB
because the largest file the worker scans is the rebuilt PNG, up to about 361 MB at the worker's 120 megapixel cap; Ubuntu's 25 MB would
refuse even a 600 dpi scan's rebuild, and a file over `MaxFileSize` or `MaxScanSize` is skipped and called clean unless
`AlertExceedsMax` is on.

**A good answer.** The output of both blocks, and then send one photograph through grouplab.org/targets: the worker's log should say
`clean, clamdscan` for it.

---

## 13. Clear what the test suite left in %TEMP% before entry 179

**Opened and answered 2026-09-24**, entry 179 section 1.1: Alan asked for the cleanup to be done for him rather than by hand, so I did
it in one listed command: 15,456 `grouplab-*` entries and 4,292 empty random folders made by `dotnet test` since 2026-09-13, about
1.2 GB. Nothing to do.

---

## 12. Remove the old pissinhot.com submissions from that server, when you choose

**Opened 2026-09-24. Entry 178 section 4. Answered 2026-09-25 by being replaced**: entry 215 made the pull remove what it has
verified and archived, and request 31 is the one sitting that clears the backlog this asked for. Kept for the record.

**What is needed.** Your entry 129 decision is that the server keeps nothing once it has been read. The 18 old submissions are still on
pissinhot.com. In PowerShell:

```powershell
cd C:\Dev\grouplab\scripts
.\Remove-ReadSubmissions.ps1 -WhatIf
.\Remove-ReadSubmissions.ps1
```

**A good result:** the dry run lists what it would remove and what it would leave alone and why; the real run removes those and ends with
`done: N removed`. **It removes only what the ledger marks ingested**, after checking the copy here still matches, and the ledger marks 6 of
the 18: the other 12 are left alone, and say so, until they are ingested. That is the rule working, not a fault.

**Added by entry 187, ready since entry 195: the test target from request 22.** It is pulled, checked and marked read in the ledger.
It is removed from grouplab.org, not pissinhot.com, and never kept as test data:

```powershell
cd C:\Dev\grouplab\scripts
.\Remove-ReadSubmissions.ps1 -RemoteRoot /home/airwolf/web/grouplab.org/private/ready -Only 2026-09-25_a3d30234
```

**Why.** A photograph somebody sent, sitting on a web server that no longer receives any, is a risk nobody agreed to.

**A good answer.** "Done", or "not yet".

---

## 11. The virus scanner as a daemon, HEIC, and the committed intake worker

**Opened 2026-09-24. Entry 176. Answered 2026-09-24**, entry 181: installed at 03:57 Mountain, the ClamAV daemon and HEIC support
in cleanly and the drop-in out; the worker then would not start on Windows line endings, which Alan's hot fix removed and entry 181
stops for good.

**What is needed, in the server's shell, in this order.**

1. The scanner's daemon and client, and HEIC decoding. Your server has the room: 11,927 MB, and clamd keeps about a gigabyte.

   ```bash
   sudo apt-get install -y clamav-daemon clamdscan libheif-examples
   sudo systemctl enable --now clamav-daemon
   until clamdscan --ping=1 >/dev/null 2>&1; do sleep 5; done; echo "clamd is answering"
   echo hello > /tmp/grouplab-clamd-check.txt && clamdscan --fdpass --no-summary /tmp/grouplab-clamd-check.txt; echo "exit $?"
   heif-convert --version | head -1
   free -m
   ```

   **A good result:** "clamd is answering" within a minute or two, then a line ending `OK` and `exit 0`, a version from heif-convert, and
   `free -m` showing about a gigabyte less available than before.

2. The committed worker, its unit and the installer. Copy `website/server/grouplab-intake-worker.py`,
   `website/server/grouplab-intake-worker.service` and `website/server/install.py` from the repository to `/home/ubuntu/grouplab-server/`,
   then:

   ```bash
   cd /home/ubuntu/grouplab-server
   sudo python3 install.py --intake --dry-run
   sudo python3 install.py --intake
   ```

   **A good result:** the dry run lists the worker and its unit as the files it would replace and nothing about missing packages. If it
   names a missing package, install that one and run it again.

3. The hot fix out, so the committed limit applies:

   ```bash
   sudo rm /etc/systemd/system/grouplab-intake-worker.service.d/memory.conf
   sudo rmdir /etc/systemd/system/grouplab-intake-worker.service.d
   sudo systemctl daemon-reload
   systemctl show grouplab-intake-worker -p MemoryMax -p RestrictAddressFamilies
   sudo systemctl start grouplab-intake-worker.service
   journalctl -u grouplab-intake-worker.service -n 20 --no-pager
   ```

   **A good result:** `MemoryMax=1677721600`, `RestrictAddressFamilies=AF_UNIX`, and a journal with no `oom-kill`, ending in the worker's
   own lines.

**Why.** The worker was killed for memory on every run, because standalone clamscan loads its whole database into the worker's 1 GB.
With the daemon the database lives once in clamd, and the worker keeps a tight limit that is now derived from its pixel cap. Phones send
HEIC, which Ubuntu's Pillow cannot read. And a scanner that does not complete is now said in each file's record and by the pull script.

**A good answer.** The outputs of the three blocks.

---

## 10. Replace the site sync's hot fix with the committed version

**Opened 2026-09-24. Entries 174 and 175. Answered 2026-09-24**, entry 178 section 5: installed at 03:37 Mountain, both hashes
`bb8a8636...`, and the server's sync matches the repository again. Its comment was reworded afterwards in `dcf01a1` and nothing else;
the next reinstall picks that up, and nothing needs doing for it.

**What is needed.** Your hot fix of entry 175, `CHECK_TRIES = 12` and `CHECK_WAIT_SECONDS = 10` edited into
`/usr/local/sbin/grouplab-site-sync.py`, is now what the repository says, and the repository's copy also reads nginx's
`open_file_cache_valid` when it runs and waits that long plus half a minute. Copy `website/server/grouplab-site-sync.py` from the
repository to `/home/ubuntu/grouplab-server/`, then in the server's shell:

```bash
cd /home/ubuntu/grouplab-server
sudo python3 install.py --dry-run
sudo python3 install.py
sha256sum /usr/local/sbin/grouplab-site-sync.py grouplab-site-sync.py
```

**A good result:** the dry run says it would replace the sync script and nothing else, the real run says it wrote it and kept the old one,
and the two hashes at the end are the same. The hot fix's backup, `grouplab-site-sync.py.before-window`, can stay where it is.

**Why.** So the server runs what the repository says again, and the next change to the sync is not undone by an edit made by hand.

**A good answer.** The four lines ran, and the hashes match.

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

## 8. The friend's 2026-09-23 scan is 56 MB: publish it whole, or smaller?

**Opened 2026-09-24. Entry 162 section 1. Answered 2026-09-24**, by entry 171 section 6: Alan left it to the planning session on one
condition, nothing that causes space problems later, and it chose 3. The rebuilt scan is a download on the `test-data` release, CI
fetches it and checks its SHA-256, and `samples/PROVENANCE.md` names the release and the hash. Any future sample over about 10 MB goes
the same way, and `TestDataTests` fails if one is committed.

**What is needed.** A choice of how to publish `Scan_20260923.png`, which the friend has consented to.

**Why.** Rebuilt from its pixels with no metadata, it is still 55,971,430 bytes, because scanner noise does
not compress. Scan 3, the other published sample, is 16.8 MB. A file committed to git is carried by every
clone for ever and can only be removed by rewriting history, which this project has done once already and
does not want to repeat. The consent record is written in `samples/PROVENANCE.md` and the tests already run
the scan wherever it is on the machine, so nothing waits on this except CI running those tests too.

**The choices.**

1. **Commit it whole, 56 MB.** Every test runs everywhere, including CI, on exactly the sheet that showed
   the defect. GitHub warns above 50 MB and refuses above 100.
2. **Commit it at 300 dpi, about 14 MB.** Still a real scan of the real sheet, and GroupLab reads 300 dpi
   routinely. The figures move slightly, so the tests would hold the 300 dpi numbers.
3. **Publish it as a download on a GitHub release, not in the repository.** Nothing is added to every clone;
   CI fetches it by its SHA-256 when it runs the tests.

**A good answer.** 1, 2 or 3. I would pick 3: it keeps the repository small and the tests exact.

---

## 7. What name, if any, should the macOS tester be thanked under?

**Opened 2026-09-24. Entry 166 section 5. Answered 2026-09-24**, by entry 171 section 6: thank him as **Fenix**. The credit itself is
entry 166 section 5's work, in the queue.

**What is needed.** The name the macOS tester would like to appear under in the project's list of people who
tested it, or a line saying he would rather be listed as an anonymous macOS tester.

**Why.** He ran nightly 93 on a MacBook Pro, sent the first macOS diagnostic log and answered the checklist,
which found two real defects: Command shortcuts that do nothing and pinch zoom that was never built. He is
owed credit, and a name is not something to invent.

**A good answer.** A name and how he wants it written, or "anonymous". Until then he is credited as an
anonymous macOS tester.

**Also coming, and not yet written here:** entry 165's receiver for targets sent from the analysis screen
reuses entry 129's quarantine, worker and folders. If building it shows it needs anything on the server
beyond request 1's five steps, those commands are added here in full as soon as that is known.

---

## 6. May one crop of one photographed hole be published?

**Opened 2026-09-23. Entry 153 section 5. Answered 2026-09-24**, by entry 171 section 6. Alan: "Yes any of my photographs or scans can
be published unless I specify one cannot." Recorded in `samples/PROVENANCE.md` as a standing consent for his own photographs and
scans, which does not reach anything a friend shot; the 2026-09-16 friend scan is still never published.

**What is needed.** Permission to publish a crop of a single bullet hole from one of the photographs from
the 2026-09-20 range day, about three quarters of an inch square, showing the hole and the paper around
it. Not the sheet, not the load block, not anything that identifies the place.

**Why.** Entry 153 section 5 asks for three crops of real holes with their measurements drawn on: a clean
one, one with a shadow on one edge, and a torn or overlapping pair. Three crops are now on the article and
all three are from the sample scan, because that scan is the only real material with a consent record. The
shadow case is the whole finding of question 38, it is the reason a photographed hole measures anywhere
from 0.9 to 1.45 times the bullet, and it exists only in a photograph. The article currently describes it
and cannot show it, which is exactly the gap this entry was written to close.

**A good answer.** Yes or no. If yes, a line saying so, and the crop is made the same way the others were:
rebuilt from pixels, no metadata carried, no location read at any point, and a consent record committed
before the image is. If no, the article keeps the line explaining why there is no photograph there, which
is honest and costs nothing.

---

## 5. Pre-approve the commands ordinary work needs, so you are asked once instead of fifty times

**Opened 2026-09-23. Entry 160 section 6. Answered 2026-09-24**, entry 186: Alan applied the commands. Before that, entry 171 section 6: Alan applied it with one change. The existing
`settings.local.json` allowed `git push *`, which covers a force push and contradicted this request's own promise that a force push
would still ask, so that rule is removed and the narrow push rule below replaces it. Closed when Alan confirms.

**What is needed.** One setting change on your side, so that ordinary commands in this repository run
without a prompt each time.

Create `C:\Dev\grouplab\.claude\settings.local.json` with the content below, or add these lines to the
`permissions.allow` list if that file already exists. It is your own local file, it is not committed,
and nothing here changes what the repository does.

```json
{
  "permissions": {
    "allow": [
      "Bash(git status:*)", "Bash(git diff:*)", "Bash(git log:*)", "Bash(git show:*)",
      "Bash(git add:*)", "Bash(git commit:*)", "Bash(git fetch:*)", "Bash(git rebase:*)",
      "Bash(git push origin phase-1:main)", "Bash(git ls-tree:*)", "Bash(git for-each-ref:*)",
      "Bash(grep:*)", "Bash(rg:*)", "Bash(find:*)", "Bash(ls:*)", "Bash(wc:*)",
      "Bash(head:*)", "Bash(tail:*)", "Bash(cat:*)", "Bash(sed -n:*)",
      "Bash(dotnet build:*)", "Bash(dotnet test:*)", "Bash(dotnet run --project src/GroupLab.Cli:*)",
      "Bash(python:*)", "Bash(python3:*)", "Bash(php:*)",
      "Bash(gh run list:*)", "Bash(gh run view:*)", "Bash(gh release list:*)", "Bash(gh release view:*)"
    ]
  }
}
```

**Why.** You have been approving every command, including `git status`. A one hour run becomes several
hours of your attention, and each prompt costs a round trip of context as well as your time. The list
above is written out in `CLAUDE.md` under "The commands ordinary work needs", so what it covers is on
the record rather than only in a settings file.

**What it deliberately leaves out, and what will still stop and ask you every single time:** anything
with `sudo`, any `ssh` or `scp`, `rm -rf`, `git push --force`, any `git tag`, any change to a repository
setting, and anything writing outside the repository. Those are the ones worth reading before you say
yes, and they stay that way.

**A good answer.** "Done", or a narrower list if any line above is more than you want to pre-approve.
Removing lines only costs prompts; it breaks nothing.

---

## 4. The Discord channel names, and the server's own rules

**Opened 2026-09-23. Entry 151 sections 1.2 and 1.5. Answered 2026-09-24**, by entry 171 section 3: the planning session built the
server and gave the channels and the rules itself. They are in `website/links.json`, which the community page reads.

**What is needed.** The names of the channels inside each of the five groups on the Discord server, and the
text of the server's rules.

**Why.** `grouplab.org/discord` is a real page now instead of a redirect, and entry 151 asks it to show the
channel list in plain words so somebody can see what they are joining before they join, and to summarise the
rules so they are readable before joining rather than only after. The five group names are known, so the page
is live and honest with a line about each group. The channels inside them are not, and nothing invented them:
the page says what each group is for and stops there. The rules section on the page is the project's own
expectations, written here, not a copy of the server's.

**A good answer.** A paste of the channel list, and a paste of the rules channel. Both go into
`website/links.json`, which is the one source the page reads, and the page becomes exact without a rewrite.

---

## 3. The photograph annotations, for the paper-tearing program

**Opened 2026-09-23. Entry 158 section 2. Answered 2026-09-24**, by entry 172: the ST-4 target's annotated photograph is the only
annotated material, and program A runs on it. The twenty six photographs stay available without ground truth.

**What is needed.** For the 100 yard precision rifle photographs, which shot is which in each group, and
the load behind it: most were five shots with a 6.5 Creedmoor, and the annotations say which photograph
is which so a hole can be tied to a shot rather than guessed at.

**Why.** Entry 158 asks whether the conclusions drawn about supersonic bullets tearing paper are real or
an artefact of how the holes were measured. That question cannot be answered from unlabelled holes. Step 1
of the program is the part that needs Alan, and the rest of the program is designed around what the
annotations turn out to say.

**A good answer.** Per photograph: the number of shots, the cartridge, and anything known about which hole
came from which shot. Approximate is useful. "I do not remember for that one" is also useful, because it
takes that photograph out of the evidence rather than leaving it in as a guess.

**Already in hand with the planning session.** Written down here so it is on the record; not chased.

---

## 2. The hit probability screenshots

**Opened 2026-09-23. Entry 156. Answered 2026-09-24**, entry 180: done on Alan's side, as entry 156's sections 7 and 8 record the
Blackburn Defense calculator's inputs, outputs and the two ideas to take from it. Entry 156 is built from those.

**What is needed.** Screenshots of the hit probability tools Alan already uses, as a reference for what a
shooter expects to see and which inputs are worth showing first.

**Why.** Entry 156 puts hit probability on the Ballistics screen, computed from the shooter's own measured
dispersion rather than from a number typed in. The mathematics is decided. The layout and the defaults are
not, and guessing them produces a screen that is correct and unfamiliar. A screen a shooter recognises
from the tools they already use is one they can read without being taught.

**A good answer.** Any screenshots at all, with a line saying which tool each one is from and which parts
of it he actually looks at. The parts he ignores are as useful as the parts he uses.

**Already in hand with the planning session.** Written down here so it is on the record; not chased.

---

## 1. The target upload page: the end to end test, then the redirect

**Opened 2026-09-23. Answered 2026-09-24**, entries 177 and 178. The end to end test went through from a desktop browser and a phone,
`pissinhot.com/targets` redirects to `grouplab.org/targets/` and the old receiver answers 410, and the last pull from pissinhot.com found
nothing new. **Entry 129 is complete.** Request 12 is the one thing left from it, and it is optional.

**The redirect as it was actually run, which is the version to follow if it is ever done again.** Entry 178: the first version of these
instructions put the backup beside the include, and HestiaCP loads every `nginx.ssl.conf_` file in that folder, so nginx loaded the backup
too and `nginx -t` failed on a duplicate directive. The backup goes outside the folder:

```bash
sudo cp -p /home/airwolf/conf/web/pissinhot.com/nginx.ssl.conf_targets /home/ubuntu/grouplab-server/pissinhot-nginx.ssl.conf_targets.before-redirect
sudo tee /home/airwolf/conf/web/pissinhot.com/nginx.ssl.conf_targets >/dev/null <<'EOF'
# pissinhot.com/targets moved to grouplab.org/targets. NOTES-FROM-PLANNING.md entries 129 and 173.
client_max_body_size 96m;

location = /targets      { return 301 https://grouplab.org/targets/; }
location = /targets.html { return 301 https://grouplab.org/targets/; }
location = /api/upload.php {
    default_type text/plain;
    return 410 "Target uploads have moved to https://grouplab.org/targets/\n";
}
EOF
sudo nginx -t && sudo systemctl reload nginx
sleep 60
curl -s -o /dev/null -w '%{http_code} %{redirect_url}\n' https://pissinhot.com/targets
curl -s -o /dev/null -w '%{http_code}\n' -X POST https://pissinhot.com/api/upload.php
curl -s -o /dev/null -w '%{http_code}\n' https://pissinhot.com/
```

The minute's wait is on purpose: a graceful reload lets an old nginx worker answer a request or two before it exits, and the first check
straight after the reload read 200 for `/targets` before every later one read 301. It had not failed.


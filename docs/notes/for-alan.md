# Requests for Alan

**Open: 9.** Most urgent: **31**, one sitting that takes other people's photographs off the web server, with **34**, the
survey's server side, in the same sitting. Then **33**, ten minutes with the Fold 7 and a printed sheet for the camera. Then 9,
16, 20, 18, 32, which is optional, and 21, which is optional.

**The phones are no longer needed: the Fold 7's Wireless debugging can be turned off and its screen timeout put back, and the
Essential PH-1 can be unplugged.** Nothing is running on either. The next time the Fold 7 is needed, the whole list comes here
first, in one request (entry 212).

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

## 34. The hardware survey's server side: in the same sitting as 31, five more minutes

**Opened 2026-09-25. Entry 219 item D1, entries 207 and 208. The planning session checks these commands before you run them.** The
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

**Opened 2026-09-25. Entry 215 section 4. Optional; nothing waits on it.** Once request 31 has run, the server no longer holds any
submission. What remains is `C:\Dev\grouplab-submissions` on this machine and the private archive on GitHub. The folder here is
outside the repository and, as far as anyone here knows, outside any sync.

**A suggestion, nothing more:** keep a second copy of that one folder somewhere that is not this disk, for example an external drive
copied to now and then, or the folder added to whatever backup this machine already has. I have set nothing up and will not.

**A good answer.** "Backed up to ..." or "the archive is enough". Either closes this.

---

## 31. Take other people's photographs off the web server: one sitting, about fifteen minutes

**Opened 2026-09-25. Entries 215 to 218. It replaces request 12. The planning session checks these commands before you run them**
(entry 218); the archive is confirmed private. Photographs people sent sit on the server until you remove them by
hand, which entry 129 said should never happen. The pull now removes each submission from the server itself, but only after it has
checked it here, put it in the private archive (`grouplab-submissions-archive`, which you have created), downloaded it back and
compared it. This sitting installs the matching server side and runs that pull once on both servers' folders, which clears the backlog.

**1. In the server's shell**, after copying `website/server/grouplab-intake-worker.py`, `website/server/grouplab-error-worker.py` and
`website/server/install.py` from the repository to `/home/ubuntu/grouplab-server/`. The workers now also delete, by themselves, a
refused upload after 7 days, one nobody pulls after 60, an error report that cannot be sent after 30, and a set-aside file after 7:

```bash
cd /home/ubuntu/grouplab-server
sudo python3 install.py --intake --dry-run
sudo python3 install.py --intake
sudo python3 install.py --errors --dry-run
sudo python3 install.py --errors
```

**2. In PowerShell on this machine**, a dry run of each first, which lists and changes nothing, then the real runs. The first is
grouplab.org, the second the old pissinhot.com folder:

```powershell
cd C:\Dev\grouplab\scripts
.\Get-TargetSubmissions.ps1 -RemoteRoot /home/airwolf/web/grouplab.org/private/ready -WhatIf
.\Get-TargetSubmissions.ps1 -RemoteRoot /home/airwolf/web/grouplab.org/private/ready
.\Get-TargetSubmissions.ps1 -WhatIf
.\Get-TargetSubmissions.ps1
```

**A good result.** Each real run ends with a line per folder, `archived and removed from the server`, then `N removed from the server;
0 kept there.`, then the ledger line `wrote docs\notes\STORAGE.md ...`. A folder that is kept is listed with the reason, and nothing
about it is lost: it stays on the server and here. Afterwards `.\Test-SubmissionsArchive.ps1` should end `... restored and verified,
0 problem(s)`. Send those last lines.

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

**What is needed.** One afternoon's shooting, set out in full in `docs/RESEARCH.md`, "Program B". In short: one printed batch of GroupLab
25-bull Letter sheets, one backing stapled the same way, 50 yards, **two sheets of one shot to a bull for each cartridge**, and every sheet
scanned at 600 dpi on the same flatbed. The cartridges: subsonic .22 LR, .300 Blackout subsonic, 8.6 Blackout subsonic, .510 Whisper,
high velocity .22 LR, 6 ARC, 6.5 Creedmoor, and .300 Blackout supersonic. Shoot the sheets in a mixed order rather than one cartridge
after another. Any subset helps; the four subsonic ones and the two .300 Blackouts are the heart of it.

**Why.** A .22 LR hole measures 0.765 of the bullet where centerfire holes measure 0.92 to 0.95, and the one rimfire sheet cannot say
whether that is speed, nose shape, lead against a jacket, or width. This set separates speed from width.

**One more thing, whenever you shoot any commercial gridded sheet** (entry 187 section 7): before it goes in the bin, scan it flat at
600 dpi and note the distance, the cartridge, and which mark each group was aimed at. The ST-4 of 2026-09-20 is gone, and the
research on reading a sheet GroupLab did not print needs another one.

**A good answer.** "The scans are in <folder>", with which sheet is which cartridge.

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


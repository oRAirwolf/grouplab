# Requests for Alan

**Open: 10.** Most urgent: **15**, the worker that keeps the opt out, because until it is installed every opted out submission is refused.
Then 9, 16, 17, 19, 20, 18, 12, which is optional, and 5, which Alan is applying. 21 is optional. Entry 180: this line is rewritten whenever a request opens or closes.

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

**A good answer.** "The scans are in <folder>", with which sheet is which cartridge.

---

## 19. Can the ST-4 sheet of 2026-09-20 still be scanned?

**Opened 2026-09-24. Entry 158 program A. Steps 3 and 4 wait on it.**

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

**Opened 2026-09-24. Entry 185. Waiting, and it needs one command. Not urgent: nothing breaks meanwhile.**

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

**Opened 2026-09-24. Entry 166. Waiting. Not urgent; either half can come back on its own.**

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

**Opened 2026-09-24. Entry 183. Waiting, and it needs a shell. Most urgent: until it is done every submission with "Do not include my photos in the public data set" ticked is refused.**

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

**Opened 2026-09-24. Entry 178 section 4. Waiting, optional, and it needs PowerShell on this machine.**

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

**Opened 2026-09-23. Entry 160 section 6. Being applied**, entry 171 section 6: Alan is applying it with one change. The existing
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


# 2026-09-24, entry 171: answers to questions 48 and 49, request 4 answered, and stale items closed

Written after the planning session read `docs/notes/STATE.md`, `docs/notes/for-alan.md` and the open
questions. Short, and it unblocks things. Do it next, before entry 164; it takes minutes.

## 1. Question 49: report real inches on a scan

**Option 3, as you recommend.** A group size is a physical quantity, and on a scan GroupLab has the number
that makes it physical.

1. On a scan, every distance is multiplied by the measured print scale and reported in real inches.
2. On a photograph, the figures stay in sheet inches and the screen says so in one line: "measured in the
   sheet's own inches; if the sheet was not printed at actual size, the figures are off by the same
   percentage."
3. That line is also the honest reason to print at actual size, and it belongs on the print screen and in
   `docs/WHAT-CAN-BE-MEASURED.md`: **it matters for photographs, because a photograph cannot measure the
   print scale; a scan can and corrects for it.** Entry 152's tour text is replaced by that, from the one
   source.
4. The session records which of the two it used, so an exported group can be told apart.
5. Tests: a scan printed at 96 percent reads its true size; the same sheet photographed reads 4 percent
   large and says why.

## 2. Question 48: the fourteen day rule

Accepted as you applied it. The entry count is what the section is for; the day count was a proxy that
turned out to move nothing. Drop the fourteen day clause from the rule and keep the entry count. Close
the question.

## 3. Request 4 answered: the Discord channels and rules

The planning session built the server and can answer this without Alan. Close request 4.

**Channels, by category:**

- Information: `announcements`, `start-here`, `rules`. Read only for members.
- Using GroupLab: `feature-requests` (forum), `bug-reports` (forum), `help` (forum), `target-sheets`,
  `show-your-groups`.
- Shooting: `general`, `load-development`, `optics-and-gear`, `range-reports`.
- Development: `development`, `github`, `testing`.
- Voice: Voice chat, Range day, Screen share.
- There is also a private moderators' channel. **Do not name or list it on the site.**

**The rules as posted in `#rules`:**

1. Be civil. Disagree with the idea, not the person.
2. Use the channel that fits the subject. Long side discussions belong in a thread.
3. No firearm, ammunition, or component sales, trades, or transfers here.
4. No load data handed out as safe to copy. Say what was observed and let people work up their own loads
   from published data.
5. No advertising or self promotion without asking a moderator first.
6. Safety is not a joke. Muzzle discipline and range rules get discussed seriously.
7. Post only images that belong to you. Anything posted here can be seen by anyone.
8. No personal information about yourself or anyone else, including addresses and anything that
   identifies where someone shoots.
9. English in the public channels so moderation can keep up.
10. Moderator decisions stand in the moment. Appeals go to a direct message.

Put the channel list and a summary of the rules on the community page from entry 151, from a data file
the page reads, so the page and the server can be compared in one place when either changes.

## 4. Stale questions: close them

Entry 143 answered questions 41, 42, 44, 45 and 46, and your own notes record the work done under each.
Question 39 you already describe as moot since entry 161. Move all six to the answered archive with a
pointer to entry 143 or 161. If any of them has a part genuinely still open, say which part in one line
and keep only that part open.

## 5. STATE.md is out of date

It says it was last rewritten after entry 160, but it also records 168 as done; it lists the inbox as
154 to 159 and 161, where the inbox actually holds 154 to 159, 164 to 167, 169 and 170, plus this one; and
it gives the last nightly as 93, where 94 exists. Rewrite it at the end of this entry, and make the test
that holds it under 120 lines also check that its inbox list matches the directory. That is the one fact
in it a machine can verify, and it was the one that was wrong.

## 6. Alan's answers to requests 6, 7 and 8, and a correction to request 1

**Request 6, published photographs.** Alan, 2026-09-24: "Yes any of my photographs or scans can be
published unless I specify one cannot." Record this in `samples/PROVENANCE.md` as a standing consent for
every photograph and scan Alan took himself, quoted exactly and dated, with the exception clause. It does
not cover anything a friend shot, each of which keeps its own record, and **the 2026-09-16 friend scan is
still never published.** Every published copy is still stripped of all metadata, and GPS, location and
timestamps are still never read, printed or logged. Close request 6, and publish the shadow crop entry 153
section 5 asked for.

**Request 7, the macOS tester.** Thank him as **Fenix**. Close request 7.

**Request 8, the 56 MB scan.** Alan left it to the planning session, with one condition: nothing that
causes space problems later. **Option 3: attach it to a GitHub release as a download, not to the
repository.** Nothing is added to any clone, ever. CI fetches it by URL and verifies its SHA-256 before the
tests use it, and the provenance record names the release and the hash. Use a dedicated release for test
data, created once by the workflow that uses it, not a nightly, so it is never swept up by anything that
rewrites or prunes nightly releases. Do the same for any future sample over about 10 MB. Close request 8.

**Request 1 is stale.** The intake install it asks for was done by Alan with the planning session on
2026-09-22 and 2026-09-23: the folders, the worker and its units, `.user.ini`, the nginx include, the
Turnstile secret, `nginx -t`, the reload, and both sites answering 200. ClamAV was set up as on demand
`clamscan` with no daemon, which the worker already supports, so **step 5 about the daemon's memory does
not apply**. The planning session is checking with Alan now that the installed files match the
repository's current copies, and will say in a later entry whether anything needs copying up again. What
remains of entry 129 is therefore not the install but the parts after it: setting `open` to true, the end
to end test, the `pissinhot.com/targets` redirect of section 6.2, and ingesting and deleting the waiting
submissions. Rewrite request 1 to say exactly that, with the commands for the redirect, which is the one
change to pissinhot.com that Alan has approved.

**Checked and brought current on 2026-09-24 at 00:21 Mountain.** Every intake file on the server matched
the repository except that `public_html/.user.ini` was missing. The cause: the server's copy of
`grouplab-site-sync.py` predated the `KEEP_IN_PLACE` exclusion and had deleted it on a sync. Alan copied
the current `website/server/` files to `/home/ubuntu/grouplab-server/`, ran both installers dry and then
for real, and the sync script on the server now hashes the same as the repository's, `6bf8b72d...`.
`.user.ini` is back, owned by airwolf, 2383 bytes. Both timers are running. The nginx include was
unchanged, so no reload was needed. **The server side of entry 129 is finished.** Two things to add:

1. **Check `.user.ini` survives the next real deploy**, the first sync that actually changes the site, and
   say so in the report. The dry run and the real run both found nothing to deploy, so the new exclusion
   has not yet been exercised.
2. **The installer's closing reminder is misleading once the install is done.** It tells the person to
   set the Turnstile secret and reload nginx every time, even when the secret is already set and nothing
   nginx reads has changed. Make it check: say the secret is present, without printing it, and only
   print the nginx steps when the include was actually written.

**Request 5, pre-approved commands.** Alan is applying it now, with one change the planning session
made: the existing `settings.local.json` already allowed `git push *`, which covers a force push and
contradicted the request's own promise that a force push would still ask. That rule is removed and the
narrow push rule from the request replaces it. Close request 5 when Alan confirms.

## 6.1 Entry 149 sections 3 and 4 must not fall out of the queue

Entry 149's status records sections 3 and 4 as not done: question 37's A and D (clicking the bulls that
were aimed at, and the review queue offering the offset where it would move shots) and re-running the
entry 121 survey's baselines against the narrowed rule of question 35. Neither is in any later entry, so
nothing would bring them back. Do section 3 together with entry 170 section 2, since that freeze is in the
same control, and section 4 together with entry 172 section 3 item 1, since both measure false and missed
holes. Say in each report that 149's section is now closed.

## 7. The order from here

171, 164, 170, 166, 169, 159, 154, 155, 156, 157, 158, 165, with 167 fitted in between.

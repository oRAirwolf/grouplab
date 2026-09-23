# Requests for Alan

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

## 4. The Discord channel names, and the server's own rules

**Opened 2026-09-23. Entry 151 sections 1.2 and 1.5. Waiting.**

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

**Opened 2026-09-23. Entry 158 section 2. Waiting.**

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

**Opened 2026-09-23. Entry 156. Waiting.**

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

## 1. Entry 129 sections 4, 6 and 8.2: the server work for the upload page

**Opened 2026-09-23. Needs a shell, so the commands also go in the panel. Waiting.**

**What is needed.** Five things on the server, in this order. Everything in the repository is built and
tested; the send page and the receiver are gated out of the site build by `"open": false` in
`website/api/limits.json` until this has run, so nothing is live until Alan says it is.

1. **Copy the intake files up.** The six files `install.py --intake` installs all sit in
   `website/server/`: `grouplab-intake-worker.py`, `grouplab-set-turnstile-secret`,
   `grouplab-intake-worker.service`, `grouplab-intake-worker.timer`, `user.ini` and
   `nginx.ssl.conf_grouplab`, alongside `install.py` itself.
2. **Run the installer, dry first.** `sudo python3 install.py --intake --dry-run`, read what it says it
   would do, then `sudo python3 install.py --intake`. It backs up anything it replaces. It installs the
   worker, its two systemd units, PHP's per-directory settings and the nginx include, and nothing that
   belongs to any other domain is touched.
3. **The Turnstile secret, which nobody but Alan ever sees.**
   `sudo /usr/local/sbin/grouplab-set-turnstile-secret`, and paste the secret half of the Cloudflare
   Turnstile key at its prompt. The public half is already in the page. The secret is never typed into
   the panel, a file here, or a commit.
4. **nginx, tested before it is reloaded, and pissinhot.com checked afterwards.** `sudo nginx -t`, then
   `sudo systemctl reload nginx`, then curl both `https://pissinhot.com/` and `https://grouplab.org/` and
   confirm both answer 200. If `nginx -t` complains about a duplicate `client_max_body_size`, another
   include for this site already sets it: raise that one instead and delete the line from
   `nginx.ssl.conf_grouplab`, and do not reload until `-t` passes.
5. **ClamAV's memory.** `systemctl status clamav-daemon` and `free -m`. The scanner wants about a
   gigabyte resident. If it is not running or the machine cannot spare it, say so and the receiver holds
   submissions unscanned in quarantine rather than pretending they were scanned.

**Why.** Six submissions are waiting and there is no page for anybody to send a seventh through. The
receiver refuses PDFs, rebuilds every image from its pixels, drops GPS and timestamps, records consent
with the submission, and verifies Turnstile server-side before it writes anything. None of that can be
proved live until it is installed.

**A good answer.** The installer's output, the two curl codes, and what `systemctl status clamav-daemon`
and `free -m` said. After that, the end-to-end test and the redirect in section 6.2 follow, and the six
waiting submissions are ingested and deleted from the server.

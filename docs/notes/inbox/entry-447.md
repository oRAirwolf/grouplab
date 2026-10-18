## 2026-10-18, entry 447: an overnight queue after entries 445 and 446 (Alan asleep; nothing here needs him)

From the planning session, 2026-10-18 10:45 UTC. Alan chose all four parts below for tonight. Take them in order after entries 445 and
446, one commit per part, STATE rewritten after each. **This queue's own budget limit is 50% of the week** (usage-now.json
`seven_day`; it was 16% when written; the week resets 2026-10-22 02:00 UTC). At 50% finish the part in hand cleanly and stop; what is left
stays here for the next session. The usual guard (85%) still applies. Anything that needs Alan or a look decision goes to for-alan.md
(DESIGN NEEDED for looks) and the work moves on to the next part. Main model for application work (CLAUDE.md).

## 1. The marking picture's arrow keys (entry 442's suggestion, accepted)

With a hole selected on the picture, an arrow nudges it one pixel of the picture and Shift+arrow ten, one Undo step per run of presses;
with nothing selected the arrows pan the view. Never while a text or number box has focus. Tests, user guide keyboard section, release
notes.

## 2. Keyboard and screen reader pass, desktop first, then phones

Follow-on to Quack's request (entry 442). Desktop: walk every screen and dialog with Tab, Shift+Tab, arrows, Space, Enter and Esc:
a sensible order, focus always visible, never lost after a rebuild, never trapped, every control named for a screen reader (VoiceOver
on macOS, Narrator on Windows), and every keyboard shortcut listed in the user guide. A headless test per screen that tabs through it
and checks every stop has a name. Phones: every control has a TalkBack and VoiceOver label and a sensible reading order; check at the
largest text size. Fix what is found; a list of what is left goes in STATE. The focus ring stays Fluent's own (restyling is a look
question).

## 3. The iOS "Real taps" flake

The self-test sometimes loses the first Next on the Targets keyboard (5 of 18 red runs; `keyboard.bar` and `keyboard.next` log it).
Find the cause from the logs and fix it at the cause (no added sleeps or retries that hide it). Prove it with enough runs to be sure
(say 20 green in a row) and record the run numbers.

## 4. Tests and speed

1. Rewrite the committed corpus counts record that is behind main on 29 of 55 images (entry 433). Planning's review: approved, as long
   as the commit lists every image whose count changed, old and new, and no image loses a real hole.
2. docs/PROOF-CHECKLIST.md: prove any of the 31 "built, not proven" rows that material already in the repo can prove, with tests, and
   update the README and site rows in the same change (entry 267 rule). Do not mark a row proven without the material it names.
3. Phase 9: measure, then speed up the slowest step that remains in reading a scan or a photo, desktop or phone, results identical,
   before and after in the commit message.

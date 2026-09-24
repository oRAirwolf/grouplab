# 2026-09-24, entry 180: Alan does not read the panel, so everything he needs goes where the planning session reads it

Alan: "It is almost impossible to keep up with code and when it gives me any feedback because it is
working on so many things. For that reason, I rely on cowork to read what it is saying and tell me what
needs action from me."

The planning session cannot see the Claude Code panel. It reads this repository. So everything in the
panel that Alan might need has to exist in a file as well. Do this entry **first**, before continuing the
queue; it takes minutes and it changes how every later entry reports.

## 1. `docs/notes/panel.md`, a local mirror of the panel

1. Add `docs/notes/panel.md` to `.gitignore`. It is a local file, never committed, so it costs nothing in
   history and can be written as often as needed.
2. **Every message you put in the panel for Alan, write to it as well**, newest first, each with the time
   and the entry number: the five line report after each entry, anything you are waiting on, and any
   question.
3. **Before any command that will stop and ask Alan for approval**, a deletion, a `sudo`, an `ssh`, a push
   that needs a prompt, write to the top of `panel.md` first: the exact command, what it does, what it will
   delete or change, and why. The planning session will read it and tell Alan whether to approve. A prompt
   Alan cannot see the reason for is one he cannot answer.
4. Keep the last 50 messages and drop older ones. It is a mirror, not a log; the notes log is the record.

## 2. `docs/notes/for-alan.md` stays the list of open requests

1. Close requests as soon as they are done, the same day. Right now requests 1 and 10 are done (entries 177
   and 178 record them) and still listed as open, and request 2 is effectively done on Alan's side, per
   entry 156's section 7 and 8. A list of open requests that includes finished ones makes Alan do work
   twice.
2. At the top of `for-alan.md`, one line: how many requests are open, and which one is most urgent.

## 3. `docs/notes/STATE.md` is rewritten after every entry, not every run

It currently says it was last rewritten after entry 171, and entries 164 and 173 to 177 have been actioned
since. Rewrite it at the end of every entry. Its inbox list must match the directory, which entry 171
already asked a test to check.

## 4. What this replaces

Nothing in the panel changes. This adds a copy where the planning session can read it, so that Alan can ask
"what needs me?" in Cowork and get a complete answer.

# Working rules for the Claude Code session

This file is read at the start of every session. It is public: no secrets, nothing private, and no em dashes.

## Where the work comes from

The planning session delivers instructions as numbered entries in `docs/notes/inbox/`, named `entry-NN.md`. It never edits any other file here.

Actioning an entry means four things, in the same commit as the work:

1. Do every numbered section, in order.
2. Fold the entry into the top of `docs/NOTES-FROM-PLANNING.md`, newest first, with a status line naming every section not done.
3. Delete its inbox file.
4. Write the results into `docs/PHASE1-RESULTS.md`, as a section before `## Decision log`.

`docs/NOTES-FROM-PLANNING.md` is the log of why things are the way they are. Never delete an entry from it. Questions going the other way belong in `docs/QUESTIONS-FOR-PLANNING.md`, newest at the top.

A design question never stops the run. Record it as a question, build what does not depend on the answer, and carry on.

## Waiting

**Do not end a turn to wait for something you can wait for yourself.** A CI run, a test suite, a nightly publishing, a download: poll it in the same run, a sleep and a check repeated, and carry on when it finishes.

Ending a turn while something is still running is allowed only when the wait is longer than an hour, or when it needs something only Alan can do.

## How every turn ends

The first line of the last message of a turn is one of exactly these three, alone on its line, before anything else.

- `STATUS: DONE`
- `STATUS: WAITING, NOT FINISHED`
- `STATUS: NEEDS YOU`

Use `STATUS: DONE` only when all of these are true: every section asked for is either finished or reported as not done with its reason, the inbox is empty, everything is committed and pushed, CI is green on the last commit, and any nightly the work should have produced has published. If one of them is not true, it is not done.

Use `STATUS: WAITING, NOT FINISHED` when something is still running or pending. Say what it is, roughly how long it has left, and that nothing continues until Alan sends a message. Give the exact message for him to send, in a code block so he can copy it.

Use `STATUS: NEEDS YOU` when the work cannot go on without him. Say what he has to do, step by step, then the exact message to send afterwards, in a code block.

Directly under the status line comes a short **What Alan needs to do** block: the single next action, or "Nothing." The detailed report follows that.

Never write "I'll hold here", "waiting for", "will continue when" or anything like them anywhere else in a message that ends a turn. If the work is not continuing, the status line is what says so.

## Standing constraints

- `tools/` is read only.
- Never read, write or run anything in `C:\Dev\grouplab-site`. It belongs to the website chat.
- Never commit anything from a range folder except what an entry names, and only after its consent record is committed.
- Never read or log GPS or location data from a photograph. Logs carry no metadata and no paths.
- Never push a `v*` tag by hand. Only the nightly workflow creates those.
- Never change a repository setting or permission without being asked to.
- Never push `refs/original`.
- CI stays on all three operating systems.
- Printing goes only to drivers that write a file silently. Never print to every installed printer.
- Everything that reaches outside the process goes through `IOutsideWorld`, so a test cannot open a browser or start an installer on somebody's machine.
- Documents here carry no em dashes.

If an entry contradicts the code, raise a question rather than working around it.

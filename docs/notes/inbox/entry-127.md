# 2026-09-21, entry 127: every message you end on says plainly whether you are done, waiting, or need Alan

Alan often cannot tell whether you have finished, are waiting on something that will finish on its own, or have stopped and need him. Several recent runs ended with "I'll hold here" while CI or a test suite was still running: from Alan's side that looks the same as being finished, and nothing continues until he sends a message. Fix this for good, as a standing rule, not as a habit.

## 1. A standing rule in CLAUDE.md

Create `CLAUDE.md` at the repository root (Claude Code reads it at the start of every session), containing the rules below in your own words, plus a pointer to `docs/NOTES-FROM-PLANNING.md` and the inbox workflow. Keep it short. It is a public file: no em dashes, no secrets, nothing private.

## 2. The rules

1. **Do not stop to wait for things you can wait for yourself.** CI runs, test suites, a nightly publishing, a download: poll them yourself in the same run (a sleep and a check, repeated), and carry on when they finish. Ending your turn while waiting is only allowed when the wait is longer than 60 minutes or needs something from Alan.
2. **The first line of every message you end a turn on is one of exactly three status lines**, on its own, before anything else:
   - `STATUS: DONE`: everything asked for is finished, nothing of yours is still running, and nothing is waiting.
   - `STATUS: WAITING, NOT FINISHED`: something is still running or pending. On the next line say what it is, roughly how long it will take, and that **nothing will continue until Alan sends a message**, followed by the exact message to send when it is time (in a code block, so he can copy it).
   - `STATUS: NEEDS YOU`: you cannot go on without Alan. On the next lines say exactly what he must do, step by step, then the exact message to send you afterwards, in a code block.
3. **Directly under the status line, a short "What Alan needs to do" block**: the single next action, or "Nothing." Then the detailed report as before.
4. **Never write "I'll hold here", "waiting for", "will continue when" or similar anywhere else** in a message that ends a turn. If you are not continuing, the status line says so.
5. **Before you use `STATUS: DONE`, check**: every section asked for is done or explicitly reported as not done with its reason, the inbox is empty, everything is committed and pushed, CI is green on the last commit, and any nightly the work should produce has published. If any of that is not true, it is not `DONE`.

## 3. Apply it now

Your very next message that ends a turn uses these rules. Report which status line you ended the entry 126 and entry 127 work on and why.

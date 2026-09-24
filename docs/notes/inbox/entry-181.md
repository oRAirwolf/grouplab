# 2026-09-24, entry 181: Windows line endings broke the intake worker on the server

Do this **immediately**, before continuing the queue. It is small, and the worker is down until Alan's
hot fix below is in.

## 1. What happened

Alan installed request 11 on 2026-09-24 at 03:57 Mountain. The ClamAV daemon and HEIC support went in
cleanly, the installer wrote the new worker and unit, and the hot fix drop-in came out. Then the worker
failed to start:

    /usr/bin/env: 'python3\r': No such file or directory
    grouplab-intake-worker.service: Main process exited, code=exited, status=127/n/a

## 2. The cause

The planning session checked the working tree on Alan's machine. These files in `website/server/` have
CRLF line endings there:

| file | lines ending in CR |
|---|---|
| `grouplab-intake-worker.py` | 483 |
| `grouplab-intake-worker.service` | 58 |
| `install.py` | 369 |

Every other file in `website/server/` is LF. `.gitattributes` has `* text=auto`, so Git stores LF but a
Windows checkout, or an edit on Windows, leaves CRLF in the working tree. Alan copies these files to the
server straight from the working tree with `scp`, so the server got CRLF, and the shebang line read
`python3\r`. `install.py` still ran because it is invoked as `python3 install.py`, which tolerates CR.

## 3. Alan's hot fix

The planning session has given Alan a command that strips the CR characters from the installed worker and
unit, and from the copies in `/home/ubuntu/grouplab-server/`, then reloads systemd and starts the worker.
After it, the installed files match what Git stores, so no repository change is needed to bring the server
into line; only this checkout's working copies are wrong.

## 4. The fix in the repository

1. Add `website/server/** text eol=lf` to `.gitattributes`, so these files are LF in every working tree,
   Windows included. Anything that is copied to a Linux server belongs in that rule; check `scripts/` for
   any file that is too.
2. Renormalize the working tree for those paths so Alan's copies are LF now.
3. **The installer refuses to install a file containing CR characters**, naming the file, instead of
   installing something that cannot run. A shebang with a CR in it is the specific case to test.
4. A test that every file under `website/server/` is LF in the working tree. The CRLF test failure earlier
   this week, which `STATE.md` records, was the same class of fault in a different place.
5. Every request in `for-alan.md` that copies files to the server should mention nothing about line endings,
   because after this they cannot be wrong.

## 5. After the hot fix, and one more thing in the unit (added 2026-09-24)

Alan's hot fix worked: both installed files have no CR left, and the worker started and finished cleanly at
03:59:05 Mountain. The failure logged at 03:59:02 was the timer's run a few seconds before the fix.

systemd logs this on every load of the unit:

    grouplab-intake-worker.service: RuntimeMaxSec= has no effect in combination with Type=oneshot. Ignoring.

So the time limit the unit means to put on the worker does nothing, and a worker that hung, on a huge
image or a stuck scan, would run until someone noticed. For `Type=oneshot` the limit is `TimeoutStartSec=`.
Use that, derive the value from the worst case the worker can meet, and add a test that the unit carries no
directive systemd ignores for its type.

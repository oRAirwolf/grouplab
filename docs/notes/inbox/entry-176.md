# 2026-09-24, entry 176: the intake worker is killed for memory, so no submission ever reaches ready

Do this **immediately** after entry 175. Together they are the last things between the upload page and a
working path.

## 1. What happened

After entry 174's fix went live, Alan sent a test image from his desktop and a photograph from his phone.
Both were accepted by the page and both sit in `private/quarantine/` (`2026-09-24_58d94b23` and
`2026-09-24_c80e45a7`). Nothing is in `ready/` or `refused/`, so `Get-TargetSubmissions.ps1` finds nothing.

The worker's journal, every run since the uploads arrived:

    grouplab-intake-worker.service: A process of this unit has been killed by the OOM killer.
    Failed with result 'oom-kill'.   Consumed 10.450s CPU time.

## 2. The cause

`grouplab-intake-worker.service` sets `MemoryMax=1G`. The server has no ClamAV daemon, so the worker runs
standalone `clamscan`, **which loads the entire signature database into memory on every run**. That
alone is on the order of a gigabyte today, so it is killed inside the worker's own 1 GB limit, every
time, about 13 seconds in. The submissions are never rebuilt or moved, and the worker tries again every
two minutes forever.

It was never caught because no real submission had reached the worker before tonight: every test ran
without ClamAV or against a fixture.

## 3. There is a second inconsistency in the same limit

`MAX_PIXELS` in the worker is 600,000,000. Decoding an image that size needs about 1.8 GB at three bytes a
pixel and 2.4 GB at four, so the worker would be killed by the same 1 GB limit on an image it has already
declared acceptable. The memory limit and the pixel cap must agree. Choose one and derive the other.

## 4. The fix

The memory limit on the worker is a defence against decompression bombs, and it should stay tight. So do
not simply raise it to fit the virus database; separate the two:

1. **Preferred: run ClamAV as a daemon** (`clamav-daemon`) and let the worker use `clamdscan`, which it
   already prefers when present. The database then lives once in the daemon's own memory, and the worker
   stays small and tightly limited. The cost is the daemon's resident memory, permanently. The planning
   session is asking Alan for the server's memory figures now; write the install steps for him in the
   panel, as commands, only if the server has the room.
2. **If it does not have the room**, run `clamscan` as a separate systemd unit with its own higher limit,
   so the image rebuild keeps its 1 GB limit and only the scan gets more.
3. Whichever is chosen, set `MAX_PIXELS` to what the worker's memory limit can actually decode, with a
   margin, and write the arithmetic beside it.

## 5. Stuck work must not be silent

1. A submission that fails in the worker three times is moved to `refused/` with a reason file, instead of
   being retried forever at 10 seconds of CPU every two minutes.
2. `Get-TargetSubmissions.ps1` also reports how many submissions are waiting in `quarantine/`, and how old
   the oldest is. "No submissions on the server yet" was untrue tonight: there were two, stuck.
3. The worker logs one line per submission per run, saying what it did or why it failed, so the journal
   says more than "killed".

## 6. A hot fix may already be in place

If Alan's server has the memory, the planning session may have given him a temporary systemd drop-in,
`/etc/systemd/system/grouplab-intake-worker.service.d/memory.conf`, raising `MemoryMax` so tonight's two
submissions get through. If so, the proper fix replaces it: tell Alan in the panel to remove the drop-in
once the committed version is installed, with the exact commands.

## 7. Test

Run the real worker, with real `clamscan` or `clamdscan`, under the same systemd memory limit as the
server, on the CI Linux runner or in a container, against a real phone photograph and a HEIC file. A
worker test that never ran the scanner is what let this through.

## 8. The server's memory, confirmed (added 2026-09-24)

Alan ran `free -m` and `nproc`: **11,927 MB total, 11,149 MB available, no swap, 2 CPUs.** So option 1, the
ClamAV daemon, is affordable, and it is the one to build. Alan has been given the temporary drop-in of
section 6 with `MemoryMax=3G` to clear tonight's two submissions.

Two things to get right when moving to `clamdscan`, because the worker runs sandboxed:

1. `clamd` runs as its own user and cannot read `private/quarantine/`, which is `0750 airwolf`. Use
   `clamdscan --fdpass` (or `--stream`), so the worker hands the daemon an open file rather than a path the
   daemon cannot open. Do not loosen the folder permissions to make it work.
2. Check the worker unit's sandbox allows the Unix socket `clamdscan` talks to (`RestrictAddressFamilies`
   must include `AF_UNIX`, and the socket's path must be reachable under `ProtectSystem=strict`). Test it
   on the server's configuration, not a guess at it.

No swap on the server means an out of memory condition kills rather than slows. Keep every limit derived
and tight.

## 9. After the memory raise, three more faults (added 2026-09-24)

With the drop-in in place the worker ran to the end, and refused both submissions. Its log:

    clamscan could not scan 001_20260920_185956.jpg, exit -9; letting it through on the rebuild instead
    2026-09-24_58d94b23: 001_20260920_185956.jpg would not decode cleanly: ModuleNotFoundError: No module named 'PIL'
    2026-09-24_c80e45a7: 001_grouplab-e2e-test-173.png would not decode cleanly: ModuleNotFoundError: No module named 'PIL'

1. **Pillow is not installed on the server.** The worker's whole job is rebuilding images with it, and
   `install.py --intake` never checked it was there. The planning session has given Alan
   `sudo apt-get install -y python3-pil`, the distribution's package, which is the right way on the
   system Python. Make the installer **check every dependency the worker imports** and refuse to finish,
   naming the package to install, if one is missing. HEIC needs more than `python3-pil`; decide how HEIC is
   handled and have the installer check that too, because phones send HEIC.
2. **A killed scanner is waved through silently.** Exit -9 is the scanner being killed, which was the
   memory limit, and the worker logged it as a routine pass. Rebuilding from pixels is the real defence and
   entry 129 accepted that; keep the policy, but a scanner that has not completed a scan is a broken
   installation and must be loud: count it, and have `Get-TargetSubmissions.ps1` report "the scanner did
   not run on N files" so it cannot go unnoticed for weeks.
3. **The quarantine sweep deletes submissions that were never processed.** `sweep()` removes any folder
   older than `QUARANTINE_HOURS`, one hour, from `quarantine/`. Tonight the worker failed on every run; had
   nobody looked for an hour, both real submissions would have been deleted with a one line log entry.
   A folder that has not been processed must never be deleted for age. Delete from quarantine only what the
   worker has already rebuilt or refused, and move anything that keeps failing to `refused/` with a reason
   per section 5, where the seven day rule applies.

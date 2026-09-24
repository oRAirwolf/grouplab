# 2026-09-24, entry 182: clamd cannot scan a file handed over from the worker's sandbox

Do this right after entry 181. The upload path works end to end, but no upload is being virus scanned.

## 1. What happened

Alan sent a photograph through grouplab.org/targets at 04:03 Mountain, after request 11 and entry 181's
hot fix. It was rebuilt, reached `ready`, and pulled with every checksum matching. Entry 177's pull script
fix works, and entry 176's loud reporting works: both the worker and the pull script said plainly that the
scanner did not complete. The worker's log:

    SCANNER DID NOT COMPLETE on 001_20260920_153318.jpg: clamdscan exit 2 ... Not a regular file ERROR

## 2. The cause, confirmed on the server

The kernel log:

    apparmor="DENIED" operation="getattr" info="Failed name lookup - disconnected path" error=-13
    profile="/usr/sbin/clamd" name="home/airwolf/web/grouplab.org/private/quarantine/.../001_20260920_153318.jpg"

And the same command outside the sandbox works:

    sudo -u airwolf clamdscan --fdpass --no-summary .../ready/2026-09-24_1e9ab01b/001_20260920_153318.png
    ...: OK    exit 0

So `clamd`'s AppArmor profile does allow files under `/home`. What it refuses is a file descriptor whose path
it cannot resolve. The worker runs in its own mount namespace, because of `ProtectSystem=strict`,
`ProtectHome=read-only`, `ReadWritePaths=` and `PrivateTmp=yes`. A descriptor opened inside that namespace
and passed to `clamd` with `--fdpass` refers to a mount `clamd` cannot see from its own root. AppArmor
calls that a disconnected path and denies it. `clamd` never gets to read the file.

## 3. The fix: stream the file instead of passing a descriptor

Use `clamdscan --stream`. The worker sends the file's bytes over the Unix socket and `clamd` scans the
stream, so `clamd` never opens or resolves anything, AppArmor has no path to check, and the worker's sandbox
stays exactly as tight as it is. Do **not** loosen the worker's sandbox, and do not edit the distribution's
AppArmor profile to add `attach_disconnected`: a package upgrade overwrites that file, and it would widen
what `clamd` accepts for everything on the server, not only GroupLab.

The one thing streaming needs is `clamd`'s stream size limit. It has to exceed the largest file the worker
scans. That is not only the 30 MB upload limit, because the worker also scans the rebuilt PNG, which can be
much larger than the photograph it came from: a 600 dpi scan rebuilds to about 56 MB. Derive the limit from
the worker's own `MAX_PIXELS` and the worst case PNG size, and write the arithmetic beside it.

1. Change the worker to `--stream`.
2. Have the worker refuse to call a scan clean when `clamd` reports the stream was over its limit, and say so
   in the loud way entry 176 set up. A silent truncation would be worse than no scan.
3. Put the command that sets `StreamMaxLength` in `/etc/clamav/clamd.conf` in the panel and in `panel.md`
   for Alan, with `nginx`-style care: back up the file somewhere that is not read as configuration, show the
   old value, change one line, restart `clamav-daemon`, and prove it with a scan of a file larger than the
   old limit. Check whether Ubuntu's package regenerates `clamd.conf` from debconf on upgrade, and if it
   does, set the value the way that survives.
4. The installer checks `StreamMaxLength` is large enough and refuses to finish if it is not.
5. Test the worker under its real systemd unit on a file larger than 25 MB.

## 4. Until then

The rebuild from pixels is still the primary defence, as entry 129 decided, and every unscanned file is now
reported by the worker and by the pull script. So nothing unsafe is being accepted silently, but "scanned"
is not true for any upload until this is done.

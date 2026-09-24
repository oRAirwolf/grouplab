# 2026-09-24, entry 178: pissinhot.com/targets redirects to grouplab.org, and a backup nginx loaded

Alan ran request 1 step 4 on 2026-09-24 at about 03:20 Mountain. **The move from pissinhot.com to
grouplab.org is done**, apart from the one last pull of step 5. Record it, close request 1, and mark
entry 129 complete once that pull is in.

## 1. The result

    origin: 301 https://grouplab.org/targets/                 (asked directly, bypassing Cloudflare)
    https://pissinhot.com/targets       location: https://grouplab.org/targets/   cf-cache-status: DYNAMIC
    https://www.pissinhot.com/targets   location: https://grouplab.org/targets/   cf-cache-status: DYNAMIC
    POST https://pissinhot.com/api/upload.php   410
    https://pissinhot.com/   200        https://grouplab.org/   200

The first check straight after the reload read 200 for `/targets`; a minute later every path read 301.
That is nginx's graceful reload, where an old worker answers a request or two before it exits. Say so in
the instructions, so a person does not think it failed.

Before replacing the include, Alan confirmed that the old file only served `/targets` and set
`client_max_body_size 96m`, and that nothing on pissinhot.com references `/api/upload.php` except
`targets.html`, so the 410 breaks nothing else on that site.

## 2. What went wrong on the way, and it was this project's instruction

Request 1 step 4 told Alan to back up the include **beside itself**, as
`nginx.ssl.conf_targets.before-redirect`. HestiaCP loads every file in that folder whose name starts with
`nginx.ssl.conf_`, so nginx loaded the backup and the new file together:

    nginx: [emerg] "client_max_body_size" directive is duplicate in .../nginx.ssl.conf_targets.before-redirect:31

The `&&` stopped the reload, so nothing live broke, but the configuration on disk failed its test until
the backup was moved out to `/home/ubuntu/grouplab-server/pissinhot-nginx.ssl.conf_targets.before-redirect`.
Had anything else reloaded nginx in between, a certificate renewal for instance, that reload would have
failed too.

1. **Never write a backup, a temporary file or anything else into a HestiaCP `conf/web/<domain>/` folder.**
   Anything whose name starts with `nginx.conf_` or `nginx.ssl.conf_` is live configuration there. Put that
   rule in `CLAUDE.md` and in `docs/WEBSITE.md`.
2. Check `install.py`. It backs up what it replaces "beside itself" for its own files; confirm it never does
   that for `nginx.ssl.conf_grouplab` in `/home/airwolf/conf/web/grouplab.org/`, and if it does, move those
   backups somewhere HestiaCP does not read. The folder listing Alan sent shows no stray backup there today.
3. Correct request 1's text in `for-alan.md` so the instructions on record are the ones that worked.

## 3. What is left on the server, all of it in entries 176 and 177

- The temporary `MemoryMax=3G` drop-in for the intake worker, to be replaced by the ClamAV daemon.
- `python3-pil`, installed by Alan with apt; the installer must check for it from now on.
- The sync script's `CHECK_TRIES = 12` and `CHECK_WAIT_SECONDS = 10`, hot fixed on the server, to be matched
  in the repository by entry 175.

## 4. The last pull (added 2026-09-24)

Alan ran `Get-TargetSubmissions.ps1` against pissinhot.com after the redirect: **18 on the server, 18
already here, 0 new.** Step 5 is done, request 1 is closed, and entry 129 is complete.

The 18 old submissions are still on the pissinhot.com server. Alan's entry 129 decision is that the server
keeps nothing once it has been read, and `Remove-ReadSubmissions.ps1` is how they are removed. That is his
to run when he chooses; say so in `for-alan.md` as its own short request rather than folding it into
anything else.

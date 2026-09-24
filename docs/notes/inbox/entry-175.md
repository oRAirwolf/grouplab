# 2026-09-24, entry 175: the site sync has rolled back every deploy since 02:36 Mountain, and why

Do this **immediately**, before the current entry continues. It is why entry 174's fix never reached
the live site.

## 1. What happened

From 02:36 Mountain on 2026-09-24, every run of `grouplab-site-sync` installed the new build, ran its live
check, and rolled back:

    the live check failed after 5 attempts: the home page is not serving the new commit
    rolled back: the site did not answer correctly after installing

So the live site stayed on the build before entry 174, and the upload page kept refusing every photo
after the fix was pushed.

## 2. The cause, confirmed on the server

`/etc/nginx/nginx.conf` on the server:

    open_file_cache                 max=10000 inactive=30s;
    open_file_cache_valid           60s;
    open_file_cache_min_uses        2;

`rsync` replaces each file by writing a new one and renaming it over the old, so the file gets a new
inode. nginx's open file cache keeps serving the **old, already replaced** file for up to 60 seconds
before it checks again. The live check waits at most about 15 seconds in total, 5 tries 3 seconds
apart. **And the check itself requests the home page every 3 seconds, which keeps that cache entry in
use**, so it can never expire inside the window. The comment above `CHECK_TRIES` already describes this
exact symptom from the first deploy; the window that was chosen then is shorter than nginx's.

Earlier deploys passed only because the home page's cache entry happened to be cold when they ran.

## 3. What Alan did on the server

The planning session gave Alan a hot fix, applied only after the `open_file_cache_valid 60s` line was
confirmed: he backed up the installed script as `grouplab-site-sync.py.before-window` and changed two
constants in `/usr/local/sbin/grouplab-site-sync.py`:

    CHECK_TRIES = 12
    CHECK_WAIT_SECONDS = 10

That is a window of about two minutes, longer than nginx's 60 seconds. **The server's copy therefore no
longer matches the repository.**

## 4. What to do in the repository

1. Set the same two values in `website/server/grouplab-site-sync.py`, exactly as above, so that the next
   `install.py` run makes the server match the repository again.
2. **Derive the window, do not guess it.** The check must wait longer than nginx's `open_file_cache_valid`.
   Write that down beside the constants, with the value found on the server, and add a test that the total
   window, tries times wait, exceeds 60 seconds with margin.
3. Better still, make the window follow the server: read `open_file_cache_valid` from the nginx
   configuration at run time if it is readable, and wait at least that long plus a margin. Fall back to
   the fixed values if it cannot be read.
4. Do not reload nginx from the sync, and do not change nginx's cache settings: they belong to the whole
   server, and pissinhot.com shares them.
5. Tell Alan in the panel when the repository matches, with the one command that reinstalls the script,
   so the hot fix is replaced by the committed version.

## 5. Report

Say which deploy first passed with the wider window, on which attempt, and confirm the live
`/targets/` page now carries `name="photos[]"`.

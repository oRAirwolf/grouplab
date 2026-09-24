
## 5. Request 10 done (added 2026-09-24)

Alan copied the committed `grouplab-site-sync.py` up and ran `install.py` at 03:37 Mountain. The dry run
said it would replace the sync script and nothing else; the real run wrote it and kept the hot fixed copy
as `grouplab-site-sync.py.20260924-033740.bak`. Both hashes read
`bb8a86364792c4ab524a942b9e4931a14536bbe62943d2e83023e7c86b71462a`. **The server's sync matches the
repository again. Close request 10.**

`/usr/local/sbin/` now holds three old copies of the sync script: `.before-window` and two dated `.bak`
files. Nothing runs them, so they are harmless, but the installer should keep only the most recent backup
of each file it replaces, so they do not accumulate there any more than scratch files should in `%TEMP%`.

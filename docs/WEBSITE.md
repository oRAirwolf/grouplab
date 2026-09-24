# grouplab.org: how it is built, published and served

NOTES-FROM-PLANNING.md entries 128 and 144. The website lives in this repository and is published from it. Nothing about it is kept anywhere else.

## The shape of it, and why

**This repository publishes. The server pulls.**

1. `website/build.py` builds the site into `website/_site/` from what is already here: the guides, the screenshots, the fonts and the logos.
2. CI builds it on every push, as a check, and publishes nothing.
3. `.github/workflows/website.yml` builds it again, signs it, and puts three files in a rolling GitHub release tagged `site`. **It runs by itself on any push to `main` that touches something the site is built from**, and it can still be started by hand.
4. The server checks that release every 5 minutes, verifies the signature, and installs it.

From a commit to a live page is about seven or eight minutes.

## Stopping a publish

Entry 144 replaced entry 128 section 6's rule that nothing published the site by itself. **There is no dispatch to withhold any more**, so stopping a publish means one of two things:

- **Do not mark the page ready.** A research article appears on the index when its own front matter says `published`, and nothing else puts it there. An unfinished page is simply not marked, and it can sit in the repository for as long as it likes: it is built, it is reachable by its own address, and it carries a notice saying it is a draft.
- **Push a fix.** A page that is wrong is corrected the way anything else here is corrected. The site follows within a couple of minutes.

**What still stops a bad page reaching the web on its own:** the site workflow runs the site's own tests before it publishes anything, and if the build or those tests fail it publishes nothing and the last good parcel stays where it is. The signature check, the live check and the rollback on the server are all unchanged.

**What is deliberately not a stop:** the other workflow's Windows and macOS test runs. The site no longer waits on them, because a C# test failing on Windows says nothing about whether a page is right, and waiting half an hour for it was the cost that entry 144 removed.

The point of pulling rather than pushing is what it makes unnecessary. **GitHub holds no SSH key, no server address and no password, and the server needs no inbound access at all.** A GitHub account that fell into the wrong hands could publish a bad site; it could not reach the server. And the server will not install a site whose signature does not verify against the key compiled into GroupLab itself.

## Building it here

```
python website/build.py
```

It needs `markdown`, `Pillow`, `fonttools` and `brotli`. The output goes to `website/_site/`, which git ignores.

**The build fails rather than publishing something wrong.** It refuses an em dash, any of the banned terms, anything shaped like an address, a link to `releases/latest` or `v0.1.0`, and download buttons that do not point at the rolling nightly's stable asset names. The last two are there because both have already happened: a link to `releases/latest` returns nothing while no numbered release exists, and a versioned asset name is right for about a day.

Every asset URL carries a hash of its own contents, so a changed screenshot, PDF or font can never be served stale from a cache. Every page carries the commit it was built from in `<meta name="grouplab-site-build">`, which is how the server proves afterwards that it is serving what it just installed.

## Publishing it

**A push publishes it.** Any commit on `main` that touches something the site is built from starts `website.yml` by itself: anything under `website/`, the release notes, the glossary, the three guides and their PDFs, the screenshots, `targets/`, and the workflow file itself. Nothing else does, so a change to the application alone publishes nothing.

Starting it by hand still works, and is how a publish is forced when nothing the filter watches has changed:

```
gh workflow run website.yml --ref main -f reason="<one line saying why>"
gh run watch
```

The reason is optional now. On a push the release notes record the commit subject instead, which says as much and needs nobody to write it. Tests fail if anything other than a push or a person starts the workflow, or if anything else writes to the `site` release.

It signs the archive with `GROUPLAB_UPDATE_SIGNING_KEY`, the same key that signs update manifests, and verifies that signature against the public half in `UpdateKeys.cs` before publishing anything. A key that cannot verify its own signature fails in the workflow rather than on the server, where the only symptom would be a site that quietly stopped updating.

Three files end up at `https://github.com/oRAirwolf/grouplab/releases/download/site/`: `grouplab-site.tar.gz`, `.sha256` and `.sig`.

## What the server does

`grouplab-site-sync.timer` runs `grouplab-site-sync.py` as root every 5 minutes and 2 minutes after boot (entry 144 section 3; it was 15 while a publish was something a person asked for). A systemd timer rather than a crontab entry, because HestiaCP's `v-rebuild-user` wipes user crontabs.

Each run:

1. Reads the published `.sha256`. **A missing release is nothing to do, not an error**, so the timer is quiet until the first publish. An unchanged hash is nothing to do as well, which is almost every run.
2. Downloads the archive and its signature, refusing anything over 200 MB.
3. Checks the SHA-256, then verifies the signature against `/etc/grouplab-site-sync/update-signing.pub`. Either failing: the live site is kept and the run logs why.

   That key is `website/server/update-signing.pub` in this repository, and it is the public half of the key the application already trusts for its own updates, so no second secret exists. It is public by nature: the same bytes ship inside every build. A test holds the file and the application's copy in step, because if they drifted the server would refuse every release it was sent and the only sign would be a line in a log nobody reads.
4. Unpacks it, refusing any member that is absolute, contains `..`, is a link, a device or a pipe, or is not a plain file or directory.
5. Refuses the build unless it has `index.html`, `404.html`, `download/index.html`, `support/index.html` and `assets/css/site.css`, every page carries the build commit, and the file count is between 20 and 2000.
6. Backs up the live site to `/home/airwolf/backups/grouplab.org/web-<timestamp>.tar.gz`, keeping the newest 10.
7. `rsync -a --delete` into the site root, and installs `404.html` into the error pages folder.
8. Asks the server itself, over TLS, that `/`, `/download/` and `/support/` return 200 and that the home page carries the new commit. **If not, it restores the backup it just took and logs a rollback.**
9. Only then records the new hash.

`--dry-run` does everything up to the backup and changes nothing.

**It never changes nginx, never reloads it, and never touches anything belonging to another domain.** The systemd unit enforces that as well as the script: `ProtectSystem=strict` with `ReadWritePaths=` limited to grouplab.org's own folders, its backups, its log and its state.

### Where things are

| what | where |
|---|---|
| the site | `/home/airwolf/web/grouplab.org/public_html/` |
| the 404 page | `/home/airwolf/web/grouplab.org/document_errors/404.html` |
| the sync script | `/usr/local/sbin/grouplab-site-sync.py` |
| the units | `/etc/systemd/system/grouplab-site-sync.{service,timer}` |
| the public key | `/etc/grouplab-site-sync/update-signing.pub`, installed from `website/server/update-signing.pub` |
| what is deployed | `/var/lib/grouplab-site-sync/deployed.sha256` |
| backups | `/home/airwolf/backups/grouplab.org/` |
| the log | `/home/airwolf/logs/grouplab-site-sync.log` |

### Installing it, once

Copy `website/server/` and the public key to the server, then:

```
sudo python3 install.py --dry-run
sudo python3 install.py
```

It is idempotent, backs up anything it would overwrite, and checks that `python3`, `curl`, `rsync`, `openssl` and `systemctl` are there before it starts.

### Rolling back by hand

The sync rolls itself back when its own check fails. To go back further:

```
sudo systemctl stop grouplab-site-sync.timer
ls -t /home/airwolf/backups/grouplab.org/
sudo tar -xzf /home/airwolf/backups/grouplab.org/web-<timestamp>.tar.gz -C /home/airwolf/web/grouplab.org/public_html
```

Then either publish a good site and start the timer again, or leave it stopped while the cause is found. **Starting the timer while the bad build is still the newest published one will reinstall it**, so fix the publish first.

### Pausing it

```
sudo systemctl stop grouplab-site-sync.timer      # until the next boot
sudo systemctl disable --now grouplab-site-sync.timer   # for good
```

## The target upload page, and what happens to a photograph

NOTES-FROM-PLANNING.md entries 129 and 173. The page is `/targets/`, opened on 2026-09-24; the old `/shoot-a-target/send/` answers with a plain page linking to it, and `pissinhot.com/targets` is to redirect there. The receiver is `website/api/upload.php`, built and delivered by the same pipeline as every page.

**The strongest single protection here is that nothing you send is ever opened.**

The receiver writes each accepted upload into `private/quarantine/` and does nothing else with it: no decode, no GD, no Imagick, nothing but sniffing the first 32 bytes and hashing the file. `grouplab-intake-worker.py`, a systemd service running as the site's own user with no network and a read-only view of everything but its three folders, then decodes each file and writes a **new** image from the decoded pixels alone, and **deletes the original bytes**.

So a payload hidden in an upload never reaches anybody. Not data appended after the image, not a polyglot that is a valid image and a valid script at once, not a metadata block crafted for whatever opens it next. None of it survives being decoded to a pixel array and written out again, because nothing but the pixels is carried over.

A short whitelist of camera facts crosses, as validated numbers written freshly into the new file rather than copied as bytes: resolution, orientation, camera make and model, focal length and its 35 mm equivalent, f-number, digital zoom, lens model, ISO and exposure time. **GPS, location, dates and times, maker notes, thumbnails, XMP, comments, serial numbers and owner fields do not, and cannot**, because the new file is built from pixels and a list of numbers. That is what makes the consent text's promise about GPS true on the server rather than only in the intake on one machine.

**How the worker runs, entry 176.** It scans each file through the ClamAV daemon with `clamdscan --fdpass`, so the database lives once in
clamd and the worker keeps a tight memory limit, 1600 MB, derived from its 120 megapixel cap. HEIC is decoded by libheif's own
`heif-convert` and rebuilt like any other file. A scanner that did not complete is recorded in the file's `scan` field and counted by the
pull script. A submission the worker cannot finish in three runs goes to `refused/` with a reason, and nothing still in quarantine is
deleted for age. `install.py --intake` refuses to finish while any of Pillow, heif-convert, clamdscan or the clamd daemon is missing.

### What the page and the receiver agree on

| | |
|---|---|
| types | JPEG, PNG, HEIC/HEIF, TIFF, sniffed by magic bytes, never by extension |
| refused | PDF, and anything whose extension and content disagree |
| sizes | 30 MB a file, 90 MB a submission, 10 files |
| per address | 5 an hour, 20 a day, on a salted hash of `CF-Connecting-IP` taken only from Cloudflare's own ranges |
| everybody at once | 60 an hour, so a botnet spread thin enough to stay under the per-address limit still cannot fill the disk |
| disk | 20 GB cap, 3 GB free floor |
| bots | Cloudflare Turnstile, verified server side, plus a honeypot |

`website/api/limits.json` is the one place those numbers live. The page is built by Python and the receiver is PHP, neither can read the other's constants, and the build fails if they disagree: a page saying 30 MB while the receiver refuses at 20 wastes somebody's upload and tells them nothing useful about why.

**A Turnstile check that cannot be reached refuses the submission.** So does a missing secret. Refusing is the safe direction when the check itself is unavailable, and it is entry 129 section 2.1's rule.

### The secret

The site key is public and is in the page. **The secret half never enters this repository, a commit, a log or a report.** Alan types it into `grouplab-set-turnstile-secret` in his own SSH session, with the echo off, and it goes straight into a file under `private/` with mode 600. It is not passed as an argument, because an argument is visible to every process on the machine and lands in the shell history.

### Installing it

```
sudo python3 ~/grouplab-server/install.py --intake --dry-run
sudo python3 ~/grouplab-server/install.py --intake
```

Separate from the site sync, and additive: it creates the quarantine folders, installs the worker and its units, the secret script, PHP's per-directory settings and the nginx include, and touches nothing the site sync owns and nothing belonging to another domain. **It never runs `nginx -t` and never reloads nginx**; it prints those commands for a person to run and read.

### One thing that would have broken quietly

PHP's per-directory settings for grouplab.org live in `public_html/.user.ini`, because HestiaCP regenerates the FPM pool file on a template rebuild and a direct edit of one does not survive. That file is not part of the built site, so the sync's `rsync --delete` would have removed it on the first run after the installer put it there. The only symptom would have been every real photograph failing to upload, with nothing anywhere saying why. `grouplab-site-sync.py` excludes it by name.

### The page is not live until the receiver is

`website/api/limits.json` carries an `open` flag. While it is false the page is not built, the receiver is not shipped and nothing links to either, because a form posting to a path the server does not serve takes somebody's photographs, spends their upload and tells them nothing. It goes true in the same commit that follows the install.

## The Discord, and one thing not automated

Entry 148. The invite is in `website/links.json` and nowhere else. Everything published points at `https://grouplab.org/discord`, which the build makes as a redirect page, so replacing the invite later is a single edit and no published link ever breaks. A test fails if the invite is written out anywhere else, and a test fails if any built page carries an invite that is not the one in that file.

**Release announcements into Discord are not automated, and entry 148 section 5 says they are worth doing later.** How it would work: the nightly already writes the plain-words release note before it publishes, so posting it is a single HTTP call to a Discord webhook URL held as a repository secret, in the same job, after the release is made. Alan is using GitHub's own webhook to begin with.

## Backups do not cover this yet

The server's existing backup and offsite scripts (`pih-backup` and the Google Drive copy) cover pissinhot.com only. When those scripts are next touched, these should be added to them:

- `/etc/grouplab-site-sync/`
- `/var/lib/grouplab-site-sync/`
- `/home/airwolf/backups/grouplab.org/`
- the systemd units

Nothing in this entry changed those scripts. **Nothing here is irreplaceable**: the site is rebuilt from this repository by one command, and the backups above are only there to make a rollback quick.

## The live check waits, because the first publish rolled itself back

The first real publish, on 2026-09-21, installed correctly and then rolled itself back:

```
the live check failed: the home page is not serving the new commit
rolled back: the site did not answer correctly after installing
```

**The safety worked**, and grouplab.org kept serving the site it had. The check was simply reading too early: it ran the instant the directory was replaced and got the page the web server was still holding open, so it saw the old commit and called a good install a failure.

It now asks up to **five times, three seconds apart**, and only calls the install a failure when all five say the same thing. A retry that succeeds is not an error, and the log says which attempt answered.

The same change carried a second fix. `install.py --dry-run` had created `/var/lib/grouplab-site-sync` on a machine that had never run the installer, because the installer passed a hard-coded false where it meant its own dry run flag, and the sync made its folders on the way in rather than when it had something to put in them. A dry run now creates nothing, which is what the words mean, and a test walks both scripts for a `mkdir` a dry run could reach.

Both fixes went on the server on 2026-09-22. Alan runs every `sudo` command himself; the copy is `scp` of the five files `install.py` needs into `~/grouplab-server/`, proved by comparing their SHA-256 against the repository, and then:

```
sudo python3 ~/grouplab-server/install.py --dry-run
sudo python3 ~/grouplab-server/install.py
```

The installer is idempotent and keeps the old script beside the new one as a timestamped `.bak`.

## If it goes wrong

The workflow failing, or the new commit not being live 30 minutes after a publish, is reported with its evidence rather than retried blindly. The server keeps serving the last good site in the meantime, which is the whole point of checking before recording success.

Reading the sync log over SSH is allowed for diagnosis. Any other change to the server needs its own entry.

## The fallback

The manual deploy brief in the website chat's own folder still exists and still works. It is not used any more, and it is not kept in step with this document; it is there only in case this pipeline has to be bypassed once.

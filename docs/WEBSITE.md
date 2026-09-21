# grouplab.org: how it is built, published and served

NOTES-FROM-PLANNING.md entry 128. The website lives in this repository and is published from it. Nothing about it is kept anywhere else.

## The shape of it, and why

**This repository publishes. The server pulls.**

1. `website/build.py` builds the site into `website/_site/` from what is already here: the guides, the screenshots, the fonts and the logos.
2. CI builds it on every push and publishes nothing.
3. `.github/workflows/website.yml` builds it again, signs it, and puts three files in a rolling GitHub release tagged `site`.
4. The server checks that release every 15 minutes, verifies the signature, and installs it.

The point of pulling rather than pushing is what it makes unnecessary. **GitHub holds no SSH key, no server address and no password, and the server needs no inbound access at all.** A GitHub account that fell into the wrong hands could publish a bad site; it could not reach the server. And the server will not install a site whose signature does not verify against the key compiled into GroupLab itself.

## Building it here

```
python website/build.py
```

It needs `markdown`, `Pillow`, `fonttools` and `brotli`. The output goes to `website/_site/`, which git ignores.

**The build fails rather than publishing something wrong.** It refuses an em dash, any of the banned terms, anything shaped like an address, a link to `releases/latest` or `v0.1.0`, and download buttons that do not point at the rolling nightly's stable asset names. The last two are there because both have already happened: a link to `releases/latest` returns nothing while no numbered release exists, and a versioned asset name is right for about a day.

Every asset URL carries a hash of its own contents, so a changed screenshot, PDF or font can never be served stale from a cache. Every page carries the commit it was built from in `<meta name="grouplab-site-build">`, which is how the server proves afterwards that it is serving what it just installed.

## Publishing it

**Only a person publishes the site.** The workflow's single trigger is `workflow_dispatch`, and tests fail if it ever gains another, if any other workflow starts it, or if anything else writes to the `site` release.

```
gh workflow run website.yml --ref main -f reason="<one line saying why>"
gh run watch
```

The reason is required and goes in the release notes, so every publish can be accounted for later.

It signs the archive with `GROUPLAB_UPDATE_SIGNING_KEY`, the same key that signs update manifests, and verifies that signature against the public half in `UpdateKeys.cs` before publishing anything. A key that cannot verify its own signature fails in the workflow rather than on the server, where the only symptom would be a site that quietly stopped updating.

Three files end up at `https://github.com/oRAirwolf/grouplab/releases/download/site/`: `grouplab-site.tar.gz`, `.sha256` and `.sig`.

## What the server does

`grouplab-site-sync.timer` runs `grouplab-site-sync.py` as root every 15 minutes and 2 minutes after boot. A systemd timer rather than a crontab entry, because HestiaCP's `v-rebuild-user` wipes user crontabs.

Each run:

1. Reads the published `.sha256`. **A missing release is nothing to do, not an error**, so the timer is quiet until the first publish. An unchanged hash is nothing to do as well, which is almost every run.
2. Downloads the archive and its signature, refusing anything over 200 MB.
3. Checks the SHA-256, then verifies the signature against `/etc/grouplab-site-sync/update-signing.pub`. Either failing: the live site is kept and the run logs why.
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
| the public key | `/etc/grouplab-site-sync/update-signing.pub` |
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

## Backups do not cover this yet

The server's existing backup and offsite scripts (`pih-backup` and the Google Drive copy) cover pissinhot.com only. When those scripts are next touched, these should be added to them:

- `/etc/grouplab-site-sync/`
- `/var/lib/grouplab-site-sync/`
- `/home/airwolf/backups/grouplab.org/`
- the systemd units

Nothing in this entry changed those scripts. **Nothing here is irreplaceable**: the site is rebuilt from this repository by one command, and the backups above are only there to make a rollback quick.

## If it goes wrong

The workflow failing, or the new commit not being live 30 minutes after a publish, is reported with its evidence rather than retried blindly. The server keeps serving the last good site in the meantime, which is the whole point of checking before recording success.

Reading the sync log over SSH is allowed for diagnosis. Any other change to the server needs its own entry.

## The fallback

The manual deploy brief in the website chat's own folder still exists and still works. It is not used any more, and it is not kept in step with this document; it is there only in case this pipeline has to be bypassed once.

# 2026-09-21, entry 128: you own grouplab.org from now on

Alan has moved grouplab.org from the website chat to you. From now on you keep the site's content in step with the application, and you publish an update **only when you decide one is appropriate, as a step in whatever build task you are on**. A push or a nightly build must never change the site by itself, and Alan must never have to do anything to keep it current.

Read these two project documents' content, reproduced in substance here, and the existing builder, before you start. The website chat wrote them; where this entry adds to or tightens them, this entry wins.

- The design: you publish on GitHub, the server pulls. GitHub holds no SSH key, no server address and no password, and the server needs no new inbound access.
- What is live today: grouplab.org over HTTPS on the same Ubuntu 24.04 ARM64 HestiaCP server as pissinhot.com, nginx only, HestiaCP user `airwolf`, site root `/home/airwolf/web/grouplab.org/public_html/`, error pages `/home/airwolf/web/grouplab.org/document_errors/` (the global nginx `error_page 404 /error/404.html` serves `404.html` from there). Cloudflare proxies it (Full strict); pages are not cached by Cloudflare. Pages: `/`, `/download/`, `/shoot-a-target/`, `/guides/`, `/support/`, and a 404 page. The design was approved by Alan on 2026-09-21 and is fixed: the application's tokens from `Tokens.cs`, IBM Plex, the logo lockups, dark and light following the system with a toggle. Content is yours to change as the application changes. **A change to the look is Alan's decision, not yours.**

This entry supersedes entry 126 section 3.3 in one respect only: you may **read** `C:\Dev\grouplab-site\build\build.py`, `C:\Dev\grouplab-site\CLAUDE.md` and the reports under `C:\Dev\grouplab-site\ops\` for facts. You never write to, run anything in, or delete anything from `C:\Dev\grouplab-site`; after this entry it is a record of how the site was set up, and its manual deploy brief stays there only as a fallback.

## 1. The site's source moves into the repository

1. `website/build.py`: port `C:\Dev\grouplab-site\build\build.py` into the repository, with every path relative to the repository root instead of `C:\Dev`. It reads `docs/USER-GUIDE.md`, `docs/TESTING-GUIDE.md` and their PDFs, `docs/figures/screens/current/*-1400x900.png`, and the fonts and logos under `src/GroupLab.App/Assets/`. It writes to `website/_site/` (ignored by git). The pages it produces must match what is live today apart from the changes listed in this entry; check that by building both and comparing the HTML, and report the differences.
2. Keep every check the builder has: the build fails on an em dash, on a banned term (OnTarget, harmonics, barrel time, velocity node, accuracy node), or on anything that looks like an IP address. Add two: it fails if any page links to `releases/latest` or `v0.1.0`, and if the download buttons do not point at `releases/download/nightly/<stable name>`.
3. `website/donor/`: copy the four donor PDFs once from `C:\Dev\grouplab-donor-pack` (`grouplab-donor-pack.pdf`, `grouplab-donor-instructions.pdf`, `GL-CF25-LTR-D.pdf`, `GL-CF25-LTR.pdf`), recording each file's SHA-256 in the results. Before committing, confirm they carry nothing private: they are the blank sheets and the instructions, made in the planning session.
4. **Fingerprint every asset URL**, not only the stylesheet and script: screenshots, PDFs and fonts get a content hash in the URL, so Cloudflare can never serve a stale file after an update.
5. Every page carries `<meta name="grouplab-site-build" content="<commit>">` naming the commit the site was built from. The server's check (section 4) and your live check (section 5) use it to prove the new build is the one being served.
6. The support page and the application must agree: `https://grouplab.org/support/` and `support@grouplab.org`, as entry 126 set out.

## 2. Built in CI on every push, published never

Add a job to `ci.yml` (or a test in the suite) that runs `python website/build.py` on every push and fails CI if the build or any of its checks fails. It only builds. It never publishes, uploads or touches a release.

## 3. The website workflow: `.github/workflows/website.yml`

1. Trigger: `workflow_dispatch` **only**, with one required input, `reason`. No `push`, `schedule`, `workflow_run` or any other trigger, now or later. Add a test that fails if `website.yml` ever gains another trigger, and one that fails if any other workflow starts it or writes to the `site` release.
2. It checks out `main`, runs the builder, and packs `website/_site/` as `grouplab-site.tar.gz` with `grouplab-site.tar.gz.sha256`.
3. **It signs the archive** with the existing update signing key (`GROUPLAB_UPDATE_SIGNING_KEY`, entry 119 section 3), producing `grouplab-site.tar.gz.sig`, the same algorithm the update manifest uses. The server verifies that signature against the public key (section 4). The SHA-256 alone only proves the file arrived whole; the signature proves it came from this project's build. No new secret is needed.
4. It publishes the three files to a rolling release tagged `site`, marked pre-release and not latest, replacing the assets each time, with the commit and the `reason` input in the notes. Only `GITHUB_TOKEN` with `contents: write` and the signing secret. No other secrets.
5. The files are then at `https://github.com/oRAirwolf/grouplab/releases/download/site/grouplab-site.tar.gz` (and `.sha256`, `.sig`).
6. Make sure the nightly workflow's "keep the newest thirty" step can never touch the `site` release, and add a test for it.

## 4. The server pulls: `website/server/`

A script, a systemd service and timer, and an installer, all in the repository, installed once on the server.

**`grouplab-site-sync.py`**, run as root by `grouplab-site-sync.timer` every 15 minutes and 2 minutes after boot:

1. Download `grouplab-site.tar.gz.sha256`. If the `site` release does not exist yet (404), or the hash matches the last deployed hash (kept in `/var/lib/grouplab-site-sync/`, outside any web root), exit quietly. A missing release is "nothing to do", never an error.
2. Download the archive and its `.sig` to a fresh temporary folder, refusing anything over 200 MB. Check the SHA-256, then **verify the signature with the public key installed at `/etc/grouplab-site-sync/update-signing.pub`** (the same public half that is in `UpdateKeys.cs`), using `openssl`. Either check failing: keep the live site, log it, exit non-zero.
3. Unpack safely: refuse any member that is absolute, contains `..`, is a symlink or hard link, a device or a FIFO, or is not a regular file or directory (Python's `tarfile` with the `data` filter, plus an explicit check). Refuse the build unless `index.html`, `404.html`, `download/index.html`, `support/index.html` and `assets/css/site.css` exist, every page carries the `grouplab-site-build` meta tag, and the file count is within a sane range you state.
4. Back up the live `public_html` to `/home/airwolf/backups/grouplab.org/web-<timestamp>.tar.gz`, keeping the newest 10.
5. `rsync -a --delete --chown=airwolf:airwolf --chmod=D755,F644` into `/home/airwolf/web/grouplab.org/public_html/`, then install `404.html` into `/home/airwolf/web/grouplab.org/document_errors/404.html`.
6. Check through the server itself (`curl --resolve grouplab.org:443:<address from hostname -I, first field>`, never `-k`) that `/`, `/download/` and `/support/` return 200 and that `/` carries the new commit in its `grouplab-site-build` tag. If not, restore the backup just taken, check again, and log a rollback.
7. Log one line per run to `/home/airwolf/logs/grouplab-site-sync.log` (keep it from growing without bound), and only after a successful check write the new hash.
8. `--dry-run` does everything up to step 4 and changes nothing. The script never changes nginx, never reloads it, and never touches anything belonging to pissinhot.com or any other domain. It never writes an address into any file.

**Units**: a systemd service and timer, not a crontab entry (HestiaCP's `v-rebuild-user` wipes crontabs; the existing `pih-backup` timer is the model). Harden the service where it costs nothing: `ProtectSystem=strict` with `ReadWritePaths=` limited to the site root, the error pages folder, the backup and log folders, `/var/lib/grouplab-site-sync` and a private temp folder; `PrivateTmp=yes`; `NoNewPrivileges=yes`.

**`install.py`**: idempotent, supports `--dry-run`, makes a timestamped `.bak` of anything it would overwrite, copies the script to `/usr/local/sbin/`, the units to `/etc/systemd/system/`, the public key to `/etc/grouplab-site-sync/`, runs `systemctl daemon-reload`, `systemctl enable --now grouplab-site-sync.timer`, and then one sync with `--dry-run` and one without. It checks that `python3`, `curl`, `rsync` and `openssl` exist first and stops with a plain message if not.

**Tests** for the sync script in the suite, with no network and no root: a missing release, an unchanged hash, a bad hash, a bad signature, a hostile archive (absolute path, `..`, symlink), a build missing a required page, a failed post-install check that rolls back, and the dry run changing nothing.

## 5. The one-time server install over SSH

Alan approves each command you run on his machine, so keep them few, plain and one per call.

- Host `ssh.pissinhot.com`, user `ubuntu`, key `C:\Users\Airwolf\Documents\ssh-key-2026-03-25.key`, passed **only by path** to `ssh -i` and `scp -i`. Never read, print, copy or move the key.
- One command per `ssh` call. No `sleep`. No `curl -k`. Use `sudo` for anything that needs root.
- Copy `website/server/` and the public key to a folder in the `ubuntu` user's home with `scp`, run `sudo python3 install.py --dry-run`, show Alan what it would do in your report, then `sudo python3 install.py`.
- Then confirm: `systemctl list-timers grouplab-site-sync.timer` shows it scheduled, and the log shows the first run as "nothing to do" (no `site` release exists yet) with the live site untouched.
- Never write the server's IP address anywhere: not in the repository, not in a commit message, not in `docs/`, not in your report.

## 6. The first publish, and the proof

1. After the install, publish once with `gh workflow run website.yml --ref main -f reason="First publish from the repository"`, then `gh run watch` until it succeeds.
2. Within 20 minutes, confirm from your machine that `https://grouplab.org/` carries the new commit in its `grouplab-site-build` tag (`curl -s https://grouplab.org/ | grep grouplab-site-build`), that `/download/`, `/support/`, `/guides/` and `/shoot-a-target/` return 200, and that a donor PDF and a screenshot load through their fingerprinted URLs.
3. Read the sync log over SSH (one command) and report the run that installed it.

## 7. The standing rule for when to publish: add it to CLAUDE.md

Add a section to `CLAUDE.md`, in your own words:

1. **You own grouplab.org.** Keep it in step with the application. Publish only when you decide an update is appropriate, as a step in the task you are on. Alan never has to do anything to keep it current, and nothing else ever publishes it.
2. **Publish when** a user-visible feature lands or changes (the home page's status text and screenshots), the guides change, new renders that the site shows are committed, the support details change, the donor pack changes, the first beta or full release exists (the Download page gains a section, following Alan's README rule), or something on the site is wrong. **Do not publish** for internal refactors, test-only changes or work in progress. When in doubt, publish at the end of the task rather than in the middle of it, at most once per task.
3. **Each publish, in the same task**: build locally with `python website/build.py` and look at every page it changed; commit and push with the task's other work; wait for CI to be green on that commit; `gh workflow run website.yml --ref main -f reason="<one line>"`; `gh run watch`; then confirm the new commit is live in the `grouplab-site-build` tag within 20 minutes, and record in the task's report what was published and why.
4. **If it fails**: if the workflow fails, or the new commit is not live after 30 minutes, report it with the evidence rather than retrying blindly. The server keeps the last good site meanwhile. Reading the sync log over SSH is allowed for diagnosis; any other server change needs its own entry.
5. **Never**: change the look without Alan; publish from a failing commit; put an address, key or password in the repository; touch pissinhot.com; publish anything from the submissions, Alan's range photos, or any image other than synthetic renders and scan 3 under its consent record.

## 8. Documents

1. `docs/WEBSITE.md`: how the site is built, published and pulled, where each file lives on the server, how to roll back by hand (restore a backup from `/home/airwolf/backups/grouplab.org/`), how to pause the timer, and the fallback of the old manual deploy brief.
2. Note in `docs/WEBSITE.md` that the server's existing backup and offsite scripts cover pissinhot.com only; the new units, `/etc/grouplab-site-sync/`, `/var/lib/grouplab-site-sync/` and `/home/airwolf/backups/grouplab.org/` should be added to them when those scripts are next touched. Do not touch those scripts in this entry.

## 9. Report

Under the entry 127 status rules: the HTML differences from section 1.1, the donor PDF hashes, the install dry run and real run output (with any address removed), the first publish's run URL, the live check, and the sync log line that installed it.

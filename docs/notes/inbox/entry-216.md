## 2026-09-25, entry 216: nothing stays on the server; the long term copy goes to a private GitHub repository

Read with entry 215, which this extends. Alan, 2026-09-25: "I want anything that is submitted to my pissinhot server to be deleted from it
after being ingested or processed. I dont want anything left on there longer than is needed. Can submissions be backed up to github for
long term storage?"

## 1. Nothing left on the server longer than needed

Entry 215 stands and this makes it firmer: every kind of thing people send (targets from the page and the application, error reports,
survey reports) is removed from the server as soon as it has been processed and a verified copy exists elsewhere, and every holding
folder has a stated maximum age after which the worker deletes it regardless, with the reason in the log. No folder on the server may grow
without bound.
The server here means the one machine that hosts grouplab.org and pissinhot.com. The privacy text names grouplab.org only.

## 2. The long term copy: a private GitHub repository, if Alan creates it

The planning session told Alan this is workable, with these facts (GitHub's own documentation, September 2026): an ordinary file in a
repository is limited to 100 MB and a repository should stay under about 1 to 5 GB; Git LFS on a free personal account includes 10 GiB of
storage and 10 GiB of download a month, with files up to 2 GB, and over that uploads stop until paid for. Submissions are roughly 3 to 60
MB each, so 10 GiB is several hundred of them. Alan has been asked to create the repository; until he does, build the parts that do not
need it.

1. **The repository:** private, created by Alan (Code never creates repositories or changes their settings), name suggested
   `oRAirwolf/grouplab-submissions-archive`. Git LFS for every image. One folder per submission as pulled, with its `meta.json`, consent
   file and `DO-NOT-PUBLISH` marker kept exactly. It is never made public and never merged into the public test data repository; the
   consent recorded in each folder decides what may ever be published, and nothing is published from it without a separate entry.
2. **The order in the pull**, so there are always two copies before the server's is removed:
   1. pull and verify the checksums here (as today);
   2. commit the new folders to a local clone of the archive and push;
   3. verify the push (the remote holds the same objects, by hash);
   4. only then remove the folders from the server (entry 215 section 1).
   If step 2 or 3 fails, the server copy stays and the run says why. A switch runs the old behavior without the archive.
3. **Quota:** the pull reports the archive's LFS use against the 10 GiB allowance each run, and at 80 percent adds a line to for-alan.md
   with the choices (pay GitHub for more, or move the archive to other storage such as Cloudflare R2, which Alan's Cloudflare account can
   hold). Nothing is ever deleted from the archive to make room without Alan saying so.
4. **CI never downloads the archive.** Test data stays on the `test-data` release; the archive's download allowance is for Alan's own
   restores.
5. **Privacy text:** the upload page, the application's consent wording and the privacy page say where a submission ends up: removed from
   the web server once processed, kept by the project in a private repository hosted by GitHub, published only if the sender chose "may be
   published". Wording change only; no change to what anyone has already agreed to, because a private copy with the project is what
   sending to the project already meant. If you judge the new wording changes the meaning for people who already sent, say so and stop.
6. **A restore test:** a script that clones the archive to a temporary folder and checks every folder's checksums against its
   `meta.json`, run once when the archive is first filled and then on demand.
7. The backlog of entry 215 section 2 goes into the archive first, then leaves the servers.

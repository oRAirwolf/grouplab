## 2026-09-25, entry 224: requests 35 steps 1 and 2 and request 36 steps 1 and 2 done; the Microsoft Store with automatic releases; Windows signing on hold

## 1. What Alan did

1. **Request 35 step 1:** `gh repo create oRAirwolf/grouplab-backups --private ...` printed `Created repository oRAirwolf/grouplab-backups`.
   Point the nightly backup at it, run it once now, run the restore test against it, and say both passed.
2. **Request 35 step 2:** Alan made the fine-grained token and ran `sudo /usr/local/sbin/grouplab-set-archive-token`, which printed
   `Written to /etc/grouplab/archive-token, owned by root, mode 600.` **Its closing lines are wrong:** it said "The error worker picks it up
   on its next run" and suggested starting and tailing `grouplab-error-worker`. That is the error worker's script text copied into the archive
   one. Fix the wording to name the archive worker. Then start the archive worker yourself, check its log shows it read the token and can
   reach the archive (an upload of nothing, or listing the releases), and say so.
3. **Request 35 step 3,** the Oracle boot volume backup policy: Alan is doing it now with the planning session's help. The planning session
   told him: a custom policy, daily incremental keep 2 and weekly full keep 2, which stays inside the Always Free limit of five volume backups
   (backups do not count against the 200 GB and are not charged on an Always Free account; a sixth backup simply fails to be created). Write
   the restore procedure for a whole-server restore from a boot volume backup into `docs/RESTORE.md`. When Alan confirms the first backup
   exists, widen Code's sudo use as entry 222 section 6.2 allows.
4. **Request 36 steps 1 and 2:** the upload keystore is `C:\Dev\keys\grouplab-upload.jks` (alias `grouplab-upload`, RSA 4096, CN=Alan,
   OU=GroupLab, O=GroupLab, L=Centennial, ST=CO, C=US, 10000 days), and `ANDROID_UPLOAD_KEYSTORE`, `ANDROID_UPLOAD_KEYSTORE_PASSWORD` and
   `ANDROID_UPLOAD_KEY_PASSWORD` are set in the repository's secrets. Never read the key file or ask for the password; CI has what it needs.
   Confirm the next nightly carries a signed `grouplab-android.aab`, then rewrite request 36 as step 3 only (the Play Console), with the
   exact AAB to upload.

## 2. Windows signing: on hold

Alan: "As of right now, nobody is getting windows smart screen warnings. Lets hold off for now." Close request 37 as "not yet"; keep
`docs/RELEASE-PLAN.md`'s comparison for when it is revisited.

## 3. The Microsoft Store, with new releases pushed to it automatically

Alan: "I do want to setup the Microsoft store version and new releases should be automatically pushed to it."

1. **An MSIX package** of GroupLab built in CI beside the existing packages. The Store re-signs it, so no certificate is needed for the
   Store copy. The Store build switches GroupLab's own updater off (the Store updates it) and says so in Settings.
2. **What is pushed, and when:** stable releases, when Alan asks for one by name as today, go to the Store's public listing automatically.
   If a beta train exists by then, it can go to a Store package flight for testers; nightlies stay on GitHub only. Say if a different split is
   better.
3. **The automation:** publish from the release workflow with Microsoft's Store developer tooling (the `msstore` CLI or the Store submission
   API), authenticated with an Entra ID app registration that Partner Center trusts as a Manager. Store its tenant id, client id, client
   secret and seller id as repository secrets set by Alan. Research the current method and its exact steps; Alan administers Entra ID for a
   living, so the app registration steps can be brief.
4. **What only Alan can do, as one request:** create his free individual Microsoft Store developer account in Partner Center (identity
   verification), reserve the name GroupLab, complete the first submission's one-time parts (age rating questionnaire, store listing text and
   screenshots that you prepare, the privacy policy URL `https://grouplab.org/research/what-grouplab-sends/`), create the Entra app
   registration and add it in Partner Center, and set the four secrets. The first submission may have to be made by hand in Partner Center;
   everything after it is automatic.
5. Store listing text, screenshots in both themes, and the feature list come from you, ready to paste, in `docs/store/`.
6. Keep the Store in `docs/RELEASE-PLAN.md` and the minimums table (the Store requires Windows 10 version 1809 or later for MSIX if that is
   higher than .NET's floor; check).

## 4. A note on secrets

Alan pasted the Android keystore password into the planning chat and asked for it to be saved. The planning session declined to store it
anywhere (memory, project documents, the repository), because it is already in Alan's Bitwarden and in the repository's secrets, which is
everything that needs it. Nothing to do; do not look for it.

# Apple's and Microsoft's stores, as the scheduled checks last saw them

Written by scripts/status-note.py from the testflight and store status workflows (entries 335 and 336), only when a state changes. Both sessions read it at the start of a run; anything here that needs Alan goes into for-alan.md the same day.

<!-- status:testflight -->
## TestFlight, 2026-11-16 13:45 UTC

- Build 228 not submitted yet: Apple said 422 Another build in the same train is already in beta review. Please submit it again once it gets completed.. The next run tries again.
- Build 228 for Public Beta: ready for beta submission.
- Both groups have build 225.
- GroupLab Team also has build 228, 227, 226, newer than Public Beta's; it was added by hand and is left there.
<!-- /status:testflight -->

<!-- status:app-store -->
## The App Store version, 2026-11-16 03:36 UTC

- App Store version 1.0 (build 226): waiting for review.
- On the App Store now: nothing yet; TestFlight stays the way in.
<!-- /status:app-store -->

<!-- status:store -->
## The Microsoft Store, 2026-11-15 18:51 UTC

- Microsoft Entra gave the Store API a token: HTTP 200.
- The Store login works: the Store API answered 200 and the product is named GroupLab. Nothing was submitted.
- Published in the Store: yes.
- The Store carries package version 0.2.225.0.
- The waiting submission's status: Certification.
<!-- /status:store -->

<!-- status:store-submission -->
## store-submission, 2026-11-15 14:35 UTC

- The Store submission of grouplab-win-x64.msix, 93265018 bytes
- Submission 1152921505702409508: PreProcessing.
<!-- /status:store-submission -->

<!-- status:store-search -->
## store-search, 2026-10-06 00:04 UTC

- Store search for GroupLab: found
<!-- /status:store-search -->

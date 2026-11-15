# Apple's and Microsoft's stores, as the scheduled checks last saw them

Written by scripts/status-note.py from the testflight and store status workflows (entries 335 and 336), only when a state changes. Both sessions read it at the start of a run; anything here that needs Alan goes into for-alan.md the same day.

<!-- status:testflight -->
## TestFlight, 2026-11-15 14:47 UTC

- Build 226 added to Public Beta, with its release notes as What to Test.
- Build 226 submitted for Beta App Review.
- Build 226 for Public Beta: waiting for beta review, review waiting for review.
- Both groups have build 225.
- GroupLab Team also has build 226, newer than Public Beta's; it was added by hand and is left there.
<!-- /status:testflight -->

<!-- status:store -->
## The Microsoft Store, 2026-11-09 22:22 UTC

- Microsoft Entra gave the Store API a token: HTTP 200.
- The Store login works: the Store API answered 200 and the product is named GroupLab. Nothing was submitted.
- Published in the Store: yes.
- The Store carries package version 0.2.225.0.
- No submission is waiting on Microsoft.
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

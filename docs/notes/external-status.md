# Apple's and Microsoft's stores, as the scheduled checks last saw them

Written by scripts/status-note.py from the testflight and store status workflows (entries 335 and 336), only when a state changes. Both sessions read it at the start of a run; anything here that needs Alan goes into for-alan.md the same day.

<!-- status:testflight -->
## TestFlight, 2026-10-09 11:21 UTC

- Build 183 added to Public Beta, with its release notes as What to Test.
- Build 183 submitted for Beta App Review.
- Both groups have build 183.
<!-- /status:testflight -->

<!-- status:store -->
## The Microsoft Store, 2026-10-09 06:09 UTC

- Microsoft Entra gave the Store API a token: HTTP 200.
- The Store login works: the Store API answered 200 and the product is named GroupLab. Nothing was submitted.
- Published in the Store: yes.
- The Store carries package version 0.2.178.0.
- The waiting submission's status: Certification.
<!-- /status:store -->

<!-- status:store-submission -->
## store-submission, 2026-10-09 11:02 UTC

- The Store submission of grouplab-win-x64.msix, 93234201 bytes
- Submission 1152921505702084325: Certification.
<!-- /status:store-submission -->

<!-- status:store-search -->
## store-search, 2026-10-06 00:04 UTC

- Store search for GroupLab: found
<!-- /status:store-search -->

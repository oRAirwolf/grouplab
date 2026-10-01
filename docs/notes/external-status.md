# Apple's and Microsoft's stores, as the scheduled checks last saw them

Written by scripts/status-note.py from the testflight and store status workflows (entries 335 and 336), only when a state changes. Both sessions read it at the start of a run; anything here that needs Alan goes into for-alan.md the same day.

<!-- status:testflight -->
## TestFlight, 2026-10-01 13:00 UTC

- Build 150 added to Public Beta, with its release notes as What to Test.
- Build 150 submitted for Beta App Review.
- Both groups have build 150.
<!-- /status:testflight -->

<!-- status:store -->
## The Microsoft Store, 2026-10-01 13:16 UTC

- Microsoft Entra gave the Store API a token: HTTP 200.
- The Store login works: the Store API answered 200 and the product is named GroupLab. Nothing was submitted.
- Published in the Store: yes.
- The Store carries package version 0.2.0.0.
- No submission is waiting on Microsoft.
<!-- /status:store -->

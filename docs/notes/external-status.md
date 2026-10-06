# Apple's and Microsoft's stores, as the scheduled checks last saw them

Written by scripts/status-note.py from the testflight and store status workflows (entries 335 and 336), only when a state changes. Both sessions read it at the start of a run; anything here that needs Alan goes into for-alan.md the same day.

<!-- status:testflight -->
## TestFlight, 2026-10-06 05:56 UTC

- Both groups have build 173.
<!-- /status:testflight -->

<!-- status:store -->
## The Microsoft Store, 2026-10-06 00:04 UTC

- Microsoft Entra gave the Store API a token: HTTP 200.
- The Store login works: the Store API answered 200 and the product is named GroupLab. Nothing was submitted.
- Published in the Store: yes.
- The Store carries package version 0.2.170.0.
- No submission is waiting on Microsoft.
<!-- /status:store -->

<!-- status:store-submission -->
## store-submission, 2026-10-06 05:50 UTC

- The Store submission of grouplab-win-x64.msix, 93169283 bytes
- Submission 1152921505702052045: PreProcessing.
<!-- /status:store-submission -->

<!-- status:store-search -->
## store-search, 2026-10-06 00:04 UTC

- Store search for GroupLab: found
<!-- /status:store-search -->

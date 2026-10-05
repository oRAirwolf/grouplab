# Apple's and Microsoft's stores, as the scheduled checks last saw them

Written by scripts/status-note.py from the testflight and store status workflows (entries 335 and 336), only when a state changes. Both sessions read it at the start of a run; anything here that needs Alan goes into for-alan.md the same day.

<!-- status:testflight -->
## TestFlight, 2026-10-04 17:35 UTC

- Both groups have build 169.
<!-- /status:testflight -->

<!-- status:store -->
## The Microsoft Store, 2026-10-05 05:41 UTC

- Microsoft Entra gave the Store API a token: HTTP 200.
- The Store login works: the Store API answered 200 and the product is named GroupLab. Nothing was submitted.
- Published in the Store: yes.
- The Store carries package version 0.2.169.0.
- No submission is waiting on Microsoft.
<!-- /status:store -->

<!-- status:store-submission -->
## store-submission, 2026-10-04 22:01 UTC

- The Store submission of grouplab-win-x64.msix, 93154007 bytes
- Submission 1152921505702039785: PreProcessing.
<!-- /status:store-submission -->

<!-- status:store-search -->
## store-search, 2026-10-04 12:22 UTC

- Store search for GroupLab: not found
<!-- /status:store-search -->

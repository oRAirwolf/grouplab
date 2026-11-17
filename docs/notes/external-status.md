# Apple's and Microsoft's stores, as the scheduled checks last saw them

Written by scripts/status-note.py from the testflight and store status workflows (entries 335 and 336), only when a state changes. Both sessions read it at the start of a run; anything here that needs Alan goes into for-alan.md the same day.

<!-- status:testflight -->
## TestFlight, 2026-11-17 21:51 UTC

- Both groups have build 228.
<!-- /status:testflight -->

<!-- status:app-store -->
## The App Store version, 2026-11-17 20:51 UTC

- App Store version 1.0 (build 226): released on the App Store.
- The App Store page: https://apps.apple.com/app/id6817635137
<!-- /status:app-store -->

<!-- status:store -->
## The Microsoft Store, 2026-11-17 20:55 UTC

- Microsoft Entra gave the Store API a token: HTTP 200.
- The Store login works: the Store API answered 200 and the product is named GroupLab. Nothing was submitted.
- Published in the Store: yes.
- The Store carries package version 1.0.226.0.
- The waiting submission's status: Certification.
<!-- /status:store -->

<!-- status:store-submission -->
## store-submission, 2026-11-17 15:20 UTC

- The Store submission of grouplab-win-x64.msix, 93348203 bytes
- Submission 1152921505702430420: PreProcessing.
<!-- /status:store-submission -->

<!-- status:store-search -->
## store-search, 2026-10-06 00:04 UTC

- Store search for GroupLab: found
<!-- /status:store-search -->

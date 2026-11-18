# Apple's and Microsoft's stores, as the scheduled checks last saw them

Written by scripts/status-note.py from the testflight and store status workflows (entries 335 and 336), only when a state changes. Both sessions read it at the start of a run; anything here that needs Alan goes into for-alan.md the same day.

<!-- status:testflight -->
## TestFlight, 2026-11-18 22:35 UTC

- Build 233 for Public Beta: waiting for beta review, review waiting for review.
- Both groups have build 228.
- GroupLab Team also has build 233, newer than Public Beta's; it was added by hand and is left there.
<!-- /status:testflight -->

<!-- status:app-store -->
## The App Store version, 2026-11-17 20:51 UTC

- App Store version 1.0 (build 226): released on the App Store.
- The App Store page: https://apps.apple.com/app/id6817635137
<!-- /status:app-store -->

<!-- status:store -->
## The Microsoft Store, 2026-11-18 20:31 UTC

- Microsoft Entra gave the Store API a token: HTTP 200.
- The Store login works: the Store API answered 200 and the product is named GroupLab. Nothing was submitted.
- Published in the Store: yes.
- The Store carries package version 1.0.230.0.
- The waiting submission's status: Certification.
<!-- /status:store -->

<!-- status:store-submission -->
## store-submission, 2026-11-18 12:20 UTC

- The Store submission of grouplab-win-x64.msix, 93364481 bytes
- Submission 1152921505702437788: PreProcessing.
<!-- /status:store-submission -->

<!-- status:store-search -->
## store-search, 2026-10-06 00:04 UTC

- Store search for GroupLab: found
<!-- /status:store-search -->

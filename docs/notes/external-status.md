# Apple's and Microsoft's stores, as the scheduled checks last saw them

Written by scripts/status-note.py from the testflight and store status workflows (entries 335 and 336), only when a state changes. Both sessions read it at the start of a run; anything here that needs Alan goes into for-alan.md the same day.

<!-- status:testflight -->
## TestFlight, 2026-11-20 19:31 UTC

- Both groups have build 235.
- Apple approved Mac build 233 for Mac testers, who can install it; Alan copies the public link from App Store Connect (Apps, GroupLab, TestFlight, Mac testers, Public Link). The link is never written here.
<!-- /status:testflight -->

<!-- status:app-store -->
## The App Store version, 2026-11-17 20:51 UTC

- App Store version 1.0 (build 226): released on the App Store.
- The App Store page: https://apps.apple.com/app/id6817635137
<!-- /status:app-store -->

<!-- status:store -->
## The Microsoft Store, 2026-11-20 20:20 UTC

- Microsoft Entra gave the Store API a token: HTTP 200.
- The Store login works: the Store API answered 200 and the product is named GroupLab. Nothing was submitted.
- Published in the Store: yes.
- The Store carries package version 1.0.235.0.
- No submission is waiting on Microsoft.
<!-- /status:store -->

<!-- status:store-submission -->
## store-submission, 2026-11-20 12:19 UTC

- The Store submission of grouplab-win-x64.msix, 93402737 bytes
- Submission 1152921505702459456: PreProcessing.
<!-- /status:store-submission -->

<!-- status:store-search -->
## store-search, 2026-10-06 00:04 UTC

- Store search for GroupLab: found
<!-- /status:store-search -->

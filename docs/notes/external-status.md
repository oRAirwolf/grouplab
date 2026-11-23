# Apple's and Microsoft's stores, as the scheduled checks last saw them

Written by scripts/status-note.py from the testflight and store status workflows (entries 335 and 336), only when a state changes. Both sessions read it at the start of a run; anything here that needs Alan goes into for-alan.md the same day.

<!-- status:testflight -->
## TestFlight, 2026-11-23 12:01 UTC

- Build 242 for Public Beta: waiting for beta review, review waiting for review.
- Both groups have build 239.
- GroupLab Team also has build 242, 241, 240, newer than Public Beta's; it was added by hand and is left there.
- Mac testers: App Store Connect refused a request (401 Provide a properly configured and signed bearer token, and make sure that it has not expired. Learn more about Generating Tokens for API Requests https://developer.apple.com/go/?id=api-generating-tokens); the next run tries again.
<!-- /status:testflight -->

<!-- status:app-store -->
## The App Store version, 2026-11-23 12:01 UTC

- App Store version 1.0.242 (build 242): waiting for review.
- The App Store page: https://apps.apple.com/app/id6817635137
- On the App Store now: version 1.0; TestFlight stays the way in.
- App Store, iPhone and iPad: build 242 submitted for App Review as version 1.0.242, released by itself once approved.
- App Store, Mac: build 240 submitted for App Review as version 1.0, released by itself once approved.
<!-- /status:app-store -->

<!-- status:store -->
## The Microsoft Store, 2026-11-23 07:39 UTC

- Microsoft Entra gave the Store API a token: HTTP 200.
- The Store login works: the Store API answered 200 and the product is named GroupLab. Nothing was submitted.
- Published in the Store: yes.
- The Store carries package version 1.0.240.0.
- The waiting submission's status: CommitStarted.
<!-- /status:store -->

<!-- status:store-submission -->
## store-submission, 2026-11-23 11:05 UTC

- The Store submission of grouplab-win-x64.msix, 93494585 bytes
- Submission 1152921505702481612: PreProcessing.
<!-- /status:store-submission -->

<!-- status:store-search -->
## store-search, 2026-10-06 00:04 UTC

- Store search for GroupLab: found
<!-- /status:store-search -->

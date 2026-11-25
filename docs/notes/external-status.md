# Apple's and Microsoft's stores, as the scheduled checks last saw them

Written by scripts/status-note.py from the testflight and store status workflows (entries 335 and 336), only when a state changes. Both sessions read it at the start of a run; anything here that needs Alan goes into for-alan.md the same day.

<!-- status:testflight -->
## TestFlight, 2026-11-25 12:58 UTC

- Build 247 for Public Beta: waiting for beta review, review waiting for review.
- Both groups have build 244.
- GroupLab Team also has build 247, 246, 245, newer than Public Beta's; it was added by hand and is left there.
- Mac testers: Mac build 244 is with Apple's beta review; nothing newer is added until it is decided.
- Apple approved Mac build 243 for Mac testers, who can install it; Alan copies the public link from App Store Connect (Apps, GroupLab, TestFlight, Mac testers, Public Link). The link is never written here.
<!-- /status:testflight -->

<!-- status:app-store -->
## The App Store version, 2026-11-25 12:58 UTC

- App Store version 1.0.247 (build 247): waiting for review.
- The App Store page: https://apps.apple.com/app/id6817635137
- On the App Store now: version 1.0.244; TestFlight stays the way in.
- App Store, iPhone and iPad: build 247 submitted for App Review as version 1.0.247, released by itself once approved.
- App Store, Mac: App Store Connect refused a request (409 This resource cannot be reviewed, please check associated errors to see why.); the next run tries again.
<!-- /status:app-store -->

<!-- status:store -->
## The Microsoft Store, 2026-11-25 08:34 UTC

- Microsoft Entra gave the Store API a token: HTTP 200.
- The Store login works: the Store API answered 200 and the product is named GroupLab. Nothing was submitted.
- Published in the Store: yes.
- The Store carries package version 1.0.247.0.
- No submission is waiting on Microsoft.
<!-- /status:store -->

<!-- status:store-submission -->
## store-submission, 2026-11-25 05:17 UTC

- The Store submission of grouplab-win-x64.msix, 93551009 bytes
- Submission 1152921505702500716: PreProcessing.
<!-- /status:store-submission -->

<!-- status:store-search -->
## store-search, 2026-10-06 00:04 UTC

- Store search for GroupLab: found
<!-- /status:store-search -->

<!-- status:play -->
## Google Play production, 2026-11-25 03:04 UTC

- Production, 2026-11-25: blocked: Google refused it (HTTP 400: {
-   "error": {
-     "code": 400,
-     "message": "Precondition check failed.",
-     "status": "FAILED_PRECONDITION"
-   }
- }
- ). It goes by itself the first night Google allows it.
<!-- /status:play -->

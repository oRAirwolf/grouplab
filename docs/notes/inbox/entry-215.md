## 2026-09-25, entry 215: submissions leave the server as soon as a verified copy is here

Alan, 2026-09-25: "Shouldn't target submissions be deleted from the pissinhot server after they are downloaded and processed?"

His entry 129 decision already says the server keeps nothing once read. In practice it keeps everything until he runs
`Remove-ReadSubmissions.ps1` by hand (request 12, marked optional), and that removes only what the ledger marks ingested: 6 of the 18 old
pissinhot.com submissions, and none of the grouplab.org ones he has pulled since. So photographs people sent sit on the web server
indefinitely. Make the rule happen by itself.

1. **The pull removes what it has verified.** `Get-TargetSubmissions.ps1`, after a folder's checksums match here, removes that folder from
   the server in the same run, and says so per folder. The copy here, rebuilt and scanned by the worker, is what "downloaded and processed"
   means; waiting for a later ingest step is what left them there. A folder whose checksums do not match is left on the server and
   reported. A `-KeepOnServer` switch keeps the old behavior for a run when wanted. Same for grouplab.org's `ready` and pissinhot.com's old
   folder.
2. **The backlog, once:** every submission already pulled and verified here, on both servers (the 18 on pissinhot.com, the ones on
   grouplab.org including request 22's test target and request 15's photograph), is removed by the first run of the new pull, or by one
   `Remove-ReadSubmissions.ps1` line that you write into for-alan.md with its dry run first. Folders not yet pulled are pulled first, then
   removed. Replace request 12 with that one request, and make it the next thing for Alan, since it is about other people's photographs.
3. **The other places submissions sit on the server**, say what each keeps and for how long, and make each finite:
   - `quarantine`, while the worker runs: gone once the worker moves the folder on;
   - `refused`: kept long enough to look into (say 14 days, your call with a reason), then deleted by the worker, and the log keeps only the
     reason and the folder name;
   - `error-reports/incoming`: deleted once its issue is opened or updated;
   - the survey's stored reports (entry 207): kept only as long as the aggregate page needs, and never individual records beyond that.
   Write the retention rules in one place (the upload page's privacy text and `docs/PRIVACY.md` or wherever the site says what happens to
   what people send) so what the site promises is what the server does.
4. **The copy here becomes the only copy.** Say so in for-alan.md, and suggest how Alan might back up `C:\Dev\grouplab-submissions` (it is
   outside the repository and outside any sync today, as far as the planning session knows). His decision; do not set up a backup yourself.
5. Deletion on the server needs sudo, which Code never runs: the pull and removal run from Alan's PowerShell as today.

## 2026-09-25, entry 201: request 25 done; correction to entry 200 section 2's diagnosis

Read with entry 200.

Alan reran the SDK step with `-p:RestoreConfigFile=C:\Dev\grouplab\nuget.config`: `Build succeeded in 23.0s`, and
`C:\Dev\tools\android-sdk\platform-tools\adb.exe version` prints `Android Debug Bridge version 1.0.41, Version 36.0.0-13206524`.
**Request 25 is done**; close it.

**Correction:** `dotnet nuget list source` shows his user level configuration does have `nuget.org [Enabled]` at
`https://api.nuget.org/v3/index.json`. So entry 200's reading, that his user configuration lacked nuget.org, was wrong. The likelier cause
of the first failure is ordering: he ran the SDK step in an ordinary PowerShell window while the workload install was still running as
administrator in another, so the restore ran against a half installed workload. The retry with the repository config also worked, which
does not prove which of the two mattered. Do not add entry 200's NuGet claim to STATE.md. In request 25's rewrite, say to run the SDK step
only after the workload install has printed `Successfully installed workload(s) android.`, and keep `-p:RestoreConfigFile` as harmless.

Request 26, pairing the Fold 7, is next for Alan.

## 2026-09-25, entry 200: request 24 passed; request 25 half done, the SDK step failed on NuGet sources

## 1. Request 24 passed: switch error reports on

Alan, 2026-09-25: `send-test-error-report.py` printed `sent: the receiver took it`, and after starting the worker by hand its log read
`2026-09-25T03:29:32Z 3a6cc8fc9476: opened issue 1 for TestReport in ErrorReportCheck.Send`. Check issue 1 in the private repository
looks as request 24 described, then switch automatic error reports on in their own commit with a `Release-note:` trailer, close request
24, and start reading the open issues at the start of each run as entry 194 section 4 says. Alan was told to close issue 1 after looking;
if it is still open, close it yourself with a note that it was the test.

## 2. Request 25: the workload is installed, the SDK is not

- `dotnet workload install android` (as administrator) succeeded: workload version 10.0.401, Microsoft.Android.Sdk 36.1.69 and 35.0.105,
  `Successfully installed workload(s) android.`
- `dotnet new android -o "$env:TEMP\gl-android-probe"` succeeded.
- The `InstallAndroidDependencies` build failed at restore, before fetching anything:
  `error NU1100: Unable to resolve 'Microsoft.NET.ILLink.Tasks (>= 10.0.12)' for 'net10.0-android'` (and the same for android-arm64 and
  android-x64).
- `setx ANDROID_HOME C:\Dev\tools\android-sdk` succeeded, so the variable now points at a folder that may not exist yet.
- The JDK path in the request is right: `C:\Program Files\Eclipse Adoptium\jdk-17.0.20.101-hotspot` is the only folder there.

The planning session's reading: the probe sits in `%TEMP%`, outside the repository, so the repository's `nuget.config` (which clears the
sources and adds nuget.org) does not apply, and Alan's user level NuGet configuration evidently has no usable nuget.org source. The planning
session has given Alan a diagnostic and a retry that points the restore at the repository's config with `-p:RestoreConfigFile=...`, which
changes nothing on his machine. When his answer comes back:

1. If the retry works, rewrite request 25 so its commands are the ones that worked, and add the NuGet source finding to STATE.md's list of
   things that would surprise somebody, because every build outside the repository on this machine will hit it.
2. Do not change Alan's user level NuGet configuration yourself. If a permanent fix is wanted, write it as a request with the one command.

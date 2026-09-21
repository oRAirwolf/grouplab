# 2026-09-21, entry 125: the published nightly calls itself a development build

Alan downloaded `grouplab-setup-win-x64.exe` from the README, installed it and opened it; so did a friend. Neither saw a SmartScreen warning. Record that in the testing guide as observed on two machines on 2026-09-21, without promising it for everyone.

His screenshot of the settings page on that install shows a blocking defect and three small ones.

## 1. Blocking: the nightly is stamped as a development build

The page reads: "GroupLab 0.2.0-nightly.12, development build, commit 862aab2", and under Updates: "This is a development build, so it does not update itself. A build from the nightly train does."

So the version and the commit were stamped into the published build, but the train was not. As long as that is true, no nightly will ever offer or install an update, and entry 123 section 2.7 cannot pass. The nightly workflow passes `train: nightly` to `package.yml`, which passes `-Train` to `scripts/package-windows.ps1`, which passes `-p:GroupLabTrain=$Train` to `dotnet publish`, and `Directory.Build.props` defaults it to `development` when empty. Somewhere in that chain the value is lost or not read. Find where, fix it, and prove it:

1. A test that builds or inspects a packaged build (or the publish step's output) and fails unless the App assembly's `GroupLabTrain` metadata is `nightly` when packaged for the nightly train.
2. A check in `nightly.yml`, after packaging and before publishing, that runs the packaged application's own build description (or reads the assembly metadata of the packaged `GroupLab.App.dll`) and fails the run unless it says nightly. A nightly that would call itself a development build must never be published again.
3. Also check the Linux tarball the same way.
4. Report the cause in one sentence.

## 2. The status bar on the settings page

The settings page's status bar reads "Drag to move the image. Zoom with the wheel or the buttons.", which belongs to another screen. Each screen sets its own status text on arrival, or clears it. Add a test that visits every screen and checks the status text belongs to it.

## 3. The labels in the Updates section

"Train" and "Check" sit higher than the controls beside them. Align each label with its control's text the way the Units rows above already do (Lengths, Angles, Distances). There is also an empty gap between the Check row and the privacy note; remove it unless it holds something (the last check time and result from entry 119 section 6.2 belong there: if they are meant to be there and are not showing, that is the defect).

## 4. Report

The cause from section 1, and a new settings page render at 1280 by 720 and 2560 by 1440 under `docs/figures/screens/current/`, looked at yourself, showing a nightly build's description.

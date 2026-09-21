# 2026-09-21, entry 122: the tests open GitHub in Alan's browser

Alan reports that something keeps opening three new browser tabs at `https://github.com/oRAirwolf/grouplab` while you work. The cause is in the code: the settings page's "The project on GitHub" button calls `OpenInTheBrowser` (`MainWindow.cs`), which starts the address with `UseShellExecute = true`, and that label is not in `InterfaceBench.NotClicked`, so the control walk from entry 117 clicks it on every pass and the real default browser opens it. Several walks per test run gives several tabs. This is the same class of fault as the OneNote print in entry 114: a test reaching outside the process into the person's own applications.

## 1. One way out of the process

Every way GroupLab opens something outside itself (a web address, the download page from entry 119 section 4.7, a folder, a file in another application, a print device, a network request such as the update check, starting the installer) goes through one small interface the application owns. Tests and the benchmark replace it with a recorder that logs what would have happened and does nothing. The real implementation is used only by the running application.

## 2. A guard

Add a test that fails if any code outside that one implementation calls `Process.Start` with `UseShellExecute = true`, a launcher API, or an HTTP client directly.

## 3. Measured, not excluded

The control walk clicks these buttons against the recorder, so they are measured rather than excluded by name, and a test asserts the recorder saw the expected request for each: the GitHub link, the support placeholder (which must open nothing), the download page, and Check now.

## 4. Report

Say how many real browser openings, network requests and other launches a full test run made before the fix and after. The answer after must be zero.

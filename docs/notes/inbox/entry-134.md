# 2026-09-21, entry 134: the installer carries the GroupLab icon

Alan asked for the installer executable to show the GroupLab icon. Today `packaging/windows/grouplab.iss` sets no `SetupIconFile`, so `grouplab-setup-win-x64.exe` shows Inno Setup's default icon, while the application itself already uses `src/GroupLab.App/Assets/icons/grouplab.ico`.

1. Set `SetupIconFile` to that same `.ico`, by a path relative to the script, so the setup executable shows the GroupLab mark in Explorer, the Downloads list and the taskbar while it runs.
2. Check the `.ico` holds the sizes Windows asks for (16, 24, 32, 48, 64 and 256 pixels); if any are missing, regenerate it from the approved mark A (`src/GroupLab.App/Assets/grouplab-mark.svg`) without changing the design.
3. While there: give the installer's wizard the GroupLab look where Inno Setup allows it without a new design (`WizardSmallImageFile` with the mark on the pages the silent update never shows, using the existing assets), and confirm the uninstall entry in Add or remove programs shows the icon (it already points at `GroupLab.App.exe`).
4. Add a CI check that fails if `SetupIconFile` is missing or points at a file that does not exist.
5. Report how you checked the icon on the built installer (for example by extracting the executable's icon resources in CI), and include a small image of the result in the results.

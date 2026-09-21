; GroupLab's Windows installer, NOTES-FROM-PLANNING.md entry 116 section 2a. Inno Setup, because it is free, it is on the GitHub Windows
; runner, and it makes the kind of setup.exe a shooter recognises. Nothing of Inno Setup is linked into GroupLab, so its licence does not
; touch GroupLab's.
;
; It installs into the person's own profile with no administrator rights, puts GroupLab in the Start menu, and appears in Add or remove
; programs. Uninstalling removes the program and leaves %APPDATA%\GroupLab alone, so sessions, settings and logs survive; the finish page
; and the README say where that folder is.
;
; Built by scripts/package-windows.ps1, which passes the version, the commit and where the published files are.

#define AppName "GroupLab"
#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif
#ifndef AppCommit
  #define AppCommit "unknown"
#endif
#ifndef SourceDir
  #define SourceDir "..\..\out\package\grouplab"
#endif
#ifndef OutputDir
  #define OutputDir "..\..\out\package"
#endif
#ifndef OutputName
  #define OutputName "grouplab-setup-win-x64"
#endif

[Setup]
AppId={{8E6A7C21-2C4B-4E2E-9E0B-6C2A4C6E9B11}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=GroupLab
AppPublisherURL=https://github.com/oRAirwolf/grouplab
AppSupportURL=https://github.com/oRAirwolf/grouplab/issues
VersionInfoVersion=0.1.0
DefaultDirName={autopf}\GroupLab
DefaultGroupName=GroupLab
DisableProgramGroupPage=yes
DisableDirPage=no
; The person's own profile, so no administrator rights are needed and nothing outside it is touched.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir={#OutputDir}
OutputBaseFilename={#OutputName}
Compression=lzma2/max
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
; NOTES-FROM-PLANNING.md entry 134. Without this the setup executable carries Inno Setup's own icon, so the file a person downloads, the one
; in their Downloads list and the one on the taskbar while it runs are not recognisably GroupLab, while the application it installs is. The
; path is relative to this script, and it is the same icon the application uses: one mark, one file, no second copy to drift.
SetupIconFile=..\..\src\GroupLab.App\Assets\icons\grouplab.ico
; The mark on the wizard pages, at the two scalings Windows asks for. Inno Setup takes only BMP here, so these are generated from that same
; icon by make-wizard-images.py rather than drawn again. The silent update never shows a wizard page; this is for the person who installs by
; hand the first time.
WizardSmallImageFile=wizard-small-55.bmp,wizard-small-110.bmp
LicenseFile={#SourceDir}\LICENSE
InfoAfterFile={#SourceDir}\README.txt
UninstallDisplayName={#AppName} {#AppVersion}
UninstallDisplayIcon={app}\GroupLab.App.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Put a shortcut on the desktop"; GroupDescription: "Shortcuts:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\GroupLab"; Filename: "{app}\GroupLab.App.exe"
Name: "{group}\GroupLab read me"; Filename: "{app}\README.txt"
Name: "{autodesktop}\GroupLab"; Filename: "{app}\GroupLab.App.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\GroupLab.App.exe"; Description: "Open GroupLab"; Flags: nowait postinstall skipifsilent
; Entry 123 section 2.3: GroupLab updating itself runs this installer silently and passes /relaunch=yes, so the new version comes straight
; back up on the screen the person was on. An ordinary silent install, which is what a deployment script runs, starts nothing: only a person
; pressing Install and restart inside GroupLab asks for this.
Filename: "{app}\GroupLab.App.exe"; Flags: nowait; Check: WantsRelaunch

[Messages]
; Said in the same plain words the README uses: what the person will see, and what is true about it.
FinishedLabel=GroupLab {#AppVersion} is installed, from commit {#AppCommit}.%n%nIt is an unsigned test build, so Windows may warn about it the first time you run it: click More info, then Run anyway.%n%nGroupLab keeps your sessions, settings and log in %%APPDATA%%\GroupLab. Uninstalling leaves that folder alone; delete it yourself when you want it gone.

[Code]
function WantsRelaunch(): Boolean;
begin
  Result := CompareText(ExpandConstant('{param:relaunch|no}'), 'yes') = 0;
end;

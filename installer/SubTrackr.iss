; SubTrackr installer script (Inno Setup 6)
; Per-user install: no admin/UAC required, installs to %LocalAppData%\Programs\SubTrackr,
; adds a Start-menu shortcut, and registers an uninstaller in "Installed apps".

#ifndef MyAppVersion
  #error MyAppVersion must be supplied by build-installer.ps1
#endif
#ifndef MyVersionInfoVersion
  #error MyVersionInfoVersion must be supplied by build-installer.ps1
#endif
#ifndef PublishDir
  #error PublishDir must be supplied by build-installer.ps1
#endif

#define MyAppName "SubTrackr"
#define MyAppPublisher "Lukáš Krejčí"
#define MyAppExeName "SubTrackr.exe"
#define MyAppURL "https://github.com/lukr-99/SubTrackr"

[Setup]
AppId={{C4B1F0E2-7A3D-4E56-9B0C-2F8A1D6E33B7}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\SubTrackr
DisableProgramGroupPage=yes
DisableDirPage=auto
PrivilegesRequired=lowest
; The installer waits for a running SubTrackr to close (the app's single-instance mutex).
AppMutex=SubTrackr_SingleInstance_7f3a
ArchitecturesAllowed=x64compatible
OutputDir=dist
OutputBaseFilename=SubTrackr-Setup-{#MyAppVersion}
SetupIconFile=..\desktop\SubTrackr.Desktop\Assets\SubTrackr.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
VersionInfoVersion={#MyVersionInfoVersion}
VersionInfoProductVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoProductName={#MyAppName}
LicenseFile=..\LICENSE.md

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion
Source: "..\THIRD-PARTY-NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

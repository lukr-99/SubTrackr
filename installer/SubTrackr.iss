; SubTrackr installer script (Inno Setup 6)
; Per-user install: no admin/UAC required, installs to %LocalAppData%\Programs\SubTrackr,
; adds a Start-menu shortcut, and registers an uninstaller in "Installed apps".

#define MyAppName "SubTrackr"
#define MyAppVersion "0.2.0"
#define MyAppPublisher "lukr-99"
#define MyAppExeName "SubTrackr.exe"
#define MyAppURL "https://github.com/lukr-99/SubTrackr"

; Path to the self-contained publish output (passed in via ISCC /D, with a fallback).
#ifndef PublishDir
  #define PublishDir "..\publish"
#endif

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
ArchitecturesAllowed=x64compatible
OutputDir=.
OutputBaseFilename=SubTrackr-Setup-{#MyAppVersion}
SetupIconFile=..\desktop\SubTrackr.Desktop\Assets\SubTrackr.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
VersionInfoVersion=0.2.0.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoProductName={#MyAppName}
LicenseFile=..\LICENSE.md

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

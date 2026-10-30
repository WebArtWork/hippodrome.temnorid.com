; Inno Setup script for the Windows installer: Temnorid-Hippodrome-Setup.exe
; Build the player first (Unity: Temnorid > Hippodrome > Build Windows Player), then compile this with Inno Setup:
;   "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\Hippodrome.iss
; The installer is written to Builds\Installer.

#define AppName "Temnorid Hippodrome"
#define AppVersion "0.2.0"
#define AppExe "TemnoridHippodrome.exe"

[Setup]
AppId={{6B0B2B1E-4C63-4F0A-9E57-7D6B5B3D0A11}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Temnorid
AppPublisherURL=https://hippodrome.temnorid.com
DefaultDirName={autopf}\Temnorid Hippodrome
DefaultGroupName=Temnorid Hippodrome
DisableProgramGroupPage=yes
OutputDir=..\Builds\Installer
OutputBaseFilename=Temnorid-Hippodrome-Setup
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequiredOverridesAllowed=dialog
UninstallDisplayIcon={app}\{#AppExe}
WizardStyle=modern

[Files]
Source: "..\Builds\Windows\TemnoridHippodrome\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Run]
Filename: "{app}\{#AppExe}"; Description: "Start {#AppName}"; Flags: nowait postinstall skipifsilent

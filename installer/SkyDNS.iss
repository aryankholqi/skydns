; SkyDNS installer (Inno Setup 6). Built by build.bat from publish\SkyDNS.exe.
; The in-app updater downloads the release asset "SkyDNS-Setup.exe" and runs it with /SILENT.

#define Exe "..\publish\SkyDNS.exe"
#define Major 0
#define Minor 0
#define Rev 0
#define Build 0
#expr GetVersionComponents(Exe, Major, Minor, Rev, Build)
#define AppVer Str(Major) + "." + Str(Minor) + "." + Str(Rev)

[Setup]
; never change AppId: it is how upgrades find the existing install
AppId={{2A8D66F7-0D0A-475D-A6E6-E457113F6E5C}
AppName=SkyDNS
AppVersion={#AppVer}
AppVerName=SkyDNS {#AppVer}
AppPublisher=SkyDNS
AppPublisherURL=https://github.com/aryankholqi/skydns
AppSupportURL=https://github.com/aryankholqi/skydns/issues
AppUpdatesURL=https://github.com/aryankholqi/skydns/releases
VersionInfoVersion={#AppVer}
DefaultDirName={autopf}\SkyDNS
DefaultGroupName=SkyDNS
DisableProgramGroupPage=yes
; the app needs admin to change DNS, so install machine-wide
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir=..\publish
OutputBaseFilename=SkyDNS-Setup
SetupIconFile=..\SkyDNS\app.ico
UninstallDisplayIcon={app}\SkyDNS.exe
UninstallDisplayName=SkyDNS
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
; the app hides to the tray on close, so force-close it during upgrades
CloseApplications=force
RestartApplications=no

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#Exe}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\SkyDNS"; Filename: "{app}\SkyDNS.exe"
Name: "{autodesktop}\SkyDNS"; Filename: "{app}\SkyDNS.exe"; Tasks: desktopicon

[Run]
; interactive install: optional "Launch SkyDNS" checkbox on the last page
Filename: "{app}\SkyDNS.exe"; Description: "{cm:LaunchProgram,SkyDNS}"; Flags: nowait postinstall skipifsilent
; silent install (in-app update): always relaunch
Filename: "{app}\SkyDNS.exe"; Flags: nowait skipifnotsilent

[UninstallRun]
Filename: "{sys}\taskkill.exe"; Parameters: "/f /im SkyDNS.exe"; Flags: runhidden; RunOnceId: "KillApp"
; "Launch at Windows startup" is a scheduled task named SkyDNS (see StartupService)
Filename: "{sys}\schtasks.exe"; Parameters: "/delete /tn ""SkyDNS"" /f"; Flags: runhidden; RunOnceId: "DelTask"

[UninstallDelete]
; installer left by the in-app updater
Type: files; Name: "{app}\SkyDNS-Setup.exe"

; Parent Site Guardian — Windows installer (Inno Setup 6)
; Build staging with: scripts\windows\Build-Installer.ps1

#define MyAppName "Parent Site Guardian"
#define MyAppVersion "0.1.0"
#define MyPublisher "Parent Site Guardian"
#define ServiceName "ParentSiteGuardianApi"

[Setup]
AppId={{E8F9D4C2-5B1A-4F6E-9D2C-8A7E6F5D4C3B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64
OutputDir=..\dist\installer
OutputBaseFilename=ParentSiteGuardian-Setup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
MinVersion=10.0
DisableProgramGroupPage=yes
InfoAfterFile=AfterInstallInfo.txt
UninstallDisplayIcon={app}\service\ParentSiteGuardian.Service.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "staging\service\*"; DestDir: "{app}\service"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "staging\admin-ui\*"; DestDir: "{app}\admin-ui"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "staging\extension\*"; DestDir: "{app}\extension"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}\Open Admin dashboard"; Filename: "{sys}\cmd.exe"; Parameters: "/c start http://127.0.0.1:5050/admin/"; WorkingDir: "{app}"; Comment: "Open the local admin UI in your browser"

[Run]
Filename: "{sys}\sc.exe"; Parameters: "create {#ServiceName} binPath= ""{app}\service\ParentSiteGuardian.Service.exe"" start= auto DisplayName= ""Parent Site Guardian API"""; StatusMsg: "Creating Windows service..."; Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "description {#ServiceName} ""Local guardian API (127.0.0.1:5050)."""; Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "start {#ServiceName}"; StatusMsg: "Starting service..."; Flags: runhidden waituntilterminated

[UninstallRun]
Filename: "{sys}\sc.exe"; Parameters: "stop {#ServiceName}"; Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "delete {#ServiceName}"; Flags: runhidden waituntilterminated

[Code]
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{sys}\sc.exe'), 'stop {#ServiceName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec(ExpandConstant('{sys}\sc.exe'), 'delete {#ServiceName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := '';
end;

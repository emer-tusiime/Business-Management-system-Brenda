; Inno Setup Script — Alinda Brenda Business Manager

#define AppName      "Alinda Brenda Business Manager"
#define AppVersion   "1.0.0"
#define AppPublisher "Alinda Brenda"
#define AppExeName   "BusinessManager.exe"
#define SourceDir    "..\dist\AlindaBrend"
#define OutputDir    "..\dist\Installer"

[Setup]
AppId={{A3F8B2D1-7C4E-4F92-8B1A-2E6D3C5F9A0B}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\AlindaBrend
DefaultGroupName={#AppName}
AllowNoIcons=yes
OutputDir={#OutputDir}
OutputBaseFilename=AlindaBrend_Setup_v{#AppVersion}
Compression=lzma2/fast
SolidCompression=no
WizardStyle=modern
SetupIconFile=..\brenda.ico
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
DisableProgramGroupPage=yes
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}

; Minimum Windows version: Windows 10
MinVersion=10.0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon";  Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"
Name: "startupicon";  Description: "Launch automatically at &Windows startup"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}";  Filename: "{app}\{#AppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#AppName}";  Filename: "{app}\{#AppExeName}"; Tasks: startupicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Leave the database in %LOCALAPPDATA%\AlindaBrenda — preserves business data on uninstall
Type: filesandordirs; Name: "{app}"

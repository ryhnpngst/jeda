; =====================================================================
; Inno Setup Script for JEDA – Desktop Presentation Utility
; Version: 1.0.0
; =====================================================================

#define MyAppName "JEDA"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "JEDA Open Source"
#define MyAppURL "https://github.com/username/jeda"
#define MyAppExeName "Jeda.exe"

[Setup]
AppId={{D37E6F10-1845-4271-92D9-4B52822C79A4}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\release
OutputBaseFilename=Jeda-v1.0.0-Setup
SetupIconFile=..\src\Jeda\Assets\app.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupicon"; Description: "Jalankan otomatis saat Windows dinyalakan"; GroupDescription: "Opsi Tambahan:"; Flags: unchecked

[Files]
Source: "..\dist\portable\Jeda-v1.0.0-win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\app.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\app.ico"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent


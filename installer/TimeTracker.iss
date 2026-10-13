#define MyAppName "Time Tracker"
#define MyAppVersion "0.1.0"
#define MyAppPublisher "Robert Koernke's Consulting"
#define MyAppURL "https://github.com/robertkjr3d/Time-Tracker"
#define MyAppExeName "Time Tracker.exe"
#define MyPublishDir "..\Time Tracker\bin\Release\publish\"

[Setup]
AppId={{7A3907FE-046B-4A32-9B46-463C2E313A0B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
ChangesAssociations=no
DisableProgramGroupPage=yes
PrivilegesRequired=admin
OutputDir=.\Output
OutputBaseFilename=TimeTracker-Setup-{#MyAppVersion}
SetupIconFile=..\Time Tracker\Assets\avalonia-logo.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
Source: "{#MyPublishDir}*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

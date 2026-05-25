#define MyAppName "Balcao Livre PDV"
#define MyAppVersion "1.2.2026"
#define MyAppPublisher "Balcao Livre"
#define MyAppExeName "BalcaoLivrePDV.exe"
#define ProjectRoot AddBackslash(SourcePath) + ".."
#define PublishDir ProjectRoot + "\RestaurantePro.Windows\bin\Release\net9.0-windows\win-x64\publish-cliente-limpo-self-contained"
#define OutputRoot ProjectRoot + "\dist"

[Setup]
AppId={{7F47A4D7-5B79-4C22-A0B0-D0E3A3D4E3DD}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\Balcao Livre PDV
DefaultGroupName={#MyAppName}
OutputDir={#OutputRoot}
OutputBaseFilename=BalcaoLivrePDV-Setup-{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
SetupIconFile={#ProjectRoot}\RestaurantePro.Windows\Assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na area de trabalho"; GroupDescription: "Atalhos:"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "BalcaoLivrePDV"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir {#MyAppName}"; Flags: nowait postinstall skipifsilent

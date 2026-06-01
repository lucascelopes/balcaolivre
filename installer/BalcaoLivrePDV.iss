#define MyAppName "Balcao Livre PDV Online"
#define MyAppVersion "1.8.2026.1"
#define MyAppPublisher "Balcao Livre"
#define MyAppExeName "BalcaoLivrePDVOnline.exe"
#define ProjectRoot AddBackslash(SourcePath) + ".."
#define PublishDir ProjectRoot + "\BalcaoLivre.Online.Windows\bin\Release\net9.0-windows\win-x64\publish-online-installer-final"
#define OutputRoot ProjectRoot + "\dist"

[Setup]
AppId={{A45F7540-0C8D-4BEA-A42A-A4F73C5E8261}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\Balcao Livre PDV Online
DefaultGroupName={#MyAppName}
OutputDir={#OutputRoot}
OutputBaseFilename=BalcaoLivrePDVOnline-Setup-{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
SetupIconFile={#ProjectRoot}\BalcaoLivre.Online.Windows\Assets\app.ico
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
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb,BalcaoLivrePDVOnline.exe.WebView2\*,cs\*,de\*,es\*,fr\*,it\*,ja\*,ko\*,pl\*,ru\*,tr\*,zh-Hans\*,zh-Hant\*,createdump.exe,mscordaccore*.dll,mscordbi.dll,Microsoft.DiaSymReader.Native*.dll,System.Windows.Forms.Design*.dll"

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "BalcaoLivrePDVOnline"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir {#MyAppName}"; Flags: nowait postinstall skipifsilent
Filename: "{app}\{#MyAppExeName}"; Flags: nowait; Check: IsSilentInstall

[Code]
function IsSilentInstall: Boolean;
begin
  Result := WizardSilent;
end;

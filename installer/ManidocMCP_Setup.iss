; Manidoc MCP Server - Inno Setup Script

#define AppName "Manidoc MCP Server"
#define AppVersion "1.0.0"
#define AppPublisher "Manidoc"
#define AppExeName "ManidocMCP.exe"
#define SourceDir "..\publish"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL=https://github.com/ichiroabe/manidoc
DefaultDirName={autopf}\ManidocMCP
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=ManidocMCP_Setup_{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
; 64bit Windows 専用
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
; アンインストーラーの設定
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
; （ショートカット等は不要なためなし）

[Files]
; メイン実行ファイルと DLL 群
Source: "{#SourceDir}\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\ManidocMCP.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\ManidocMCP.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\ManidocMCP.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\*.dll"; DestDir: "{app}"; Flags: ignoreversion
; 設定ファイル（既存がある場合は上書きしない）
Source: "{#SourceDir}\appsettings.json"; DestDir: "{app}"; Flags: ignoreversion onlyifdoesntexist
; Assets フォルダ（スプライト画像）
Source: "{#SourceDir}\Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion recursesubdirs createallsubdirs
; ユーザーマニュアル
Source: "..\UserManual.md"; DestDir: "{app}"; Flags: ignoreversion

[Run]
; インストール後にマニュアルを開く（任意）
Filename: "{app}\UserManual.md"; Description: "ユーザーマニュアルを開く / Open User Manual"; Flags: postinstall shellexec skipifsilent unchecked

[Code]
// .NET 8 ランタイムの確認
function IsDotNet8Installed(): Boolean;
var
  ResultCode: Integer;
begin
  Result := Exec('cmd.exe', '/C dotnet --list-runtimes | findstr "Microsoft.NETCore.App 8."', '',
                 SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  if not IsDotNet8Installed() then
  begin
    if MsgBox(
      '.NET 8 ランタイムがインストールされていません。' + #13#10 +
      '.NET 8 Runtime is not installed.' + #13#10#13#10 +
      'https://dotnet.microsoft.com/download/dotnet/8.0 からインストールしてください。' + #13#10 +
      'Please install it from: https://dotnet.microsoft.com/download/dotnet/8.0' + #13#10#13#10 +
      'インストールを続行しますか？ / Continue anyway?',
      mbConfirmation, MB_YESNO) = IDNO then
    begin
      Result := False;
    end;
  end;
end;

[Messages]
japanese.WelcomeLabel1=Manidoc MCP Server セットアップへようこそ
japanese.WelcomeLabel2=このウィザードはあなたのコンピューターに [name] をインストールします。%n%nセットアップを開始する前に、他のアプリケーションをすべて終了してください。
japanese.FinishedHeadingLabel=Manidoc MCP Server のインストール完了
japanese.FinishedLabel=セットアップが完了しました。%n%nインストール後は Claude Desktop の設定ファイルに以下を追加してください:%n%n  "mcpServers": { "manidoc": { "command": "{app}\ManidocMCP.exe", "env": { "MANIDOC_WORKSPACE": "<データフォルダのパス>" } } }

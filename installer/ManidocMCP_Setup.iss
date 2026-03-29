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
; 設定ファイル（インストール後にウィザード入力値で上書きするため常にコピー）
Source: "{#SourceDir}\appsettings.json"; DestDir: "{app}"; Flags: ignoreversion
; Assets フォルダ（スプライト画像）
Source: "{#SourceDir}\Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion recursesubdirs createallsubdirs
; ユーザーマニュアル
Source: "..\UserManual.md"; DestDir: "{app}"; Flags: ignoreversion

[Run]
; インストール後にマニュアルを開く（任意）
Filename: "{app}\UserManual.md"; Description: "ユーザーマニュアルを開く / Open User Manual"; Flags: postinstall shellexec skipifsilent unchecked

[Code]
var
  SettingsPage: TInputQueryWizardPage;

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

procedure InitializeWizard;
var
  DefaultOutDir: String;
begin
  DefaultOutDir := GetEnv('USERPROFILE') + '\Videos';

  SettingsPage := CreateInputQueryPage(wpSelectDir,
    '動画設定 / Video Settings',
    'FFmpeg と動画出力フォルダを設定してください。' + #13#10 +
    'Configure FFmpeg path and video output folder.',
    '');

  SettingsPage.Add('FFmpeg のパス / FFmpeg path:', False);
  SettingsPage.Add('動画出力フォルダ / Video output folder:', False);

  SettingsPage.Values[0] := 'C:\Program Files\ManidocMCP\ffmpeg\bin\ffmpeg.exe';
  SettingsPage.Values[1] := DefaultOutDir;
end;

function EscapeBackslashes(const S: String): String;
var
  I: Integer;
begin
  Result := '';
  for I := 1 to Length(S) do
  begin
    if S[I] = '\' then
      Result := Result + '\\'
    else
      Result := Result + S[I];
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  JsonContent: String;
  AppSettingsPath: String;
  FfmpegPathVal: String;
  OutDirVal: String;
begin
  if CurStep = ssPostInstall then
  begin
    AppSettingsPath := ExpandConstant('{app}\appsettings.json');
    FfmpegPathVal := EscapeBackslashes(SettingsPage.Values[0]);
    OutDirVal     := EscapeBackslashes(SettingsPage.Values[1]);

    JsonContent :=
      '{' + #13#10 +
      '  "Video": {' + #13#10 +
      '    "FfmpegPath": "' + FfmpegPathVal + '",' + #13#10 +
      '    "OutDir": "' + OutDirVal + '"' + #13#10 +
      '  }' + #13#10 +
      '}';

    SaveStringToFile(AppSettingsPath, JsonContent, False);
  end;
end;

[Messages]
japanese.WelcomeLabel1=Manidoc MCP Server セットアップへようこそ
japanese.WelcomeLabel2=このウィザードはあなたのコンピューターに [name] をインストールします。%n%nセットアップを開始する前に、他のアプリケーションをすべて終了してください。
japanese.FinishedHeadingLabel=Manidoc MCP Server のインストール完了
japanese.FinishedLabel=セットアップが完了しました。%n%nClaude Desktop の設定ファイルに以下を追加してください:%n%n  "mcpServers": { "manidoc": { "command": "{app}\ManidocMCP.exe", "env": { "MANIDOC_WORKSPACE": "<データフォルダのパス>" } } }%n%n動画は設定したフォルダに保存されます。ログは %LOCALAPPDATA%\ManidocMCP\ に保存されます。

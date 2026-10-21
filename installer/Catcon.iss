; AppIdentity and the per-user/32-bit install mode must remain stable across releases.
#ifndef AppVersion
  #error AppVersion is required
#endif
#ifndef ReleaseDir
  #error ReleaseDir is required
#endif
#ifndef OutputFolder
  #error OutputFolder is required
#endif
#ifndef RuntimeInstaller
  #error RuntimeInstaller is required
#endif
#ifndef AppIdentity
  #define AppIdentity "Catcon.Desktop"
#endif
#ifndef OutputName
  #define OutputName "Catcon-" + AppVersion + "-Setup"
#endif
#ifdef InstallerTest
  #define DataRoot "{app}\Test User Data"
  #define ShortcutRoot "{app}\Test Shortcuts"
  #define CatMutex "Local\Catcon.InstallerTest"
#else
  #define DataRoot "{localappdata}\Catcon"
  #define ShortcutRoot "{group}"
  #define CatMutex "Local\Catcon.Desktop.1"
#endif

[Setup]
AppId={#AppIdentity}
AppName=Catcon · 蜜柑
AppVersion={#AppVersion}
AppPublisher=Catcon
AppComments=32 × 32 像素猫咪桌宠
DefaultDirName={localappdata}\Programs\Catcon
DefaultGroupName=Catcon
PrivilegesRequired=lowest
MinVersion=10.0
ArchitecturesAllowed=x86 x64os
UsePreviousAppDir=yes
UsePreviousTasks=yes
UsePreviousGroup=yes
DisableWelcomePage=no
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputDir={#OutputFolder}
OutputBaseFilename={#OutputName}
SetupIconFile={#ReleaseDir}\assets\catcon.ico
UninstallDisplayIcon={app}\Catcon.exe
UninstallDisplayName=Catcon · 蜜柑
AppMutex={#CatMutex}
CloseApplications=yes
CloseApplicationsFilter=*.exe
RestartApplications=no
WizardStyle=modern dynamic
Compression=lzma2/max
SolidCompression=yes
SetupLogging=yes
VersionInfoVersion={#AppVersion}.0
VersionInfoDescription=Catcon · 蜜柑安装程序
VersionInfoProductName=Catcon

[Languages]
Name: "chinesesimplified"; MessagesFile: "ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "启动入口："

[Files]
Source: "{#RuntimeInstaller}"; DestName: "ndp48-web.exe"; Flags: dontcopy
Source: "{#ReleaseDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{#ShortcutRoot}\蜜柑"; Filename: "{app}\Catcon.exe"; WorkingDir: "{app}"; AppUserModelID: "Catcon.Desktop"
Name: "{#ShortcutRoot}\卸载蜜柑"; Filename: "{uninstallexe}"
#ifndef InstallerTest
Name: "{autodesktop}\蜜柑"; Filename: "{app}\Catcon.exe"; WorkingDir: "{app}"; Tasks: desktopicon; AppUserModelID: "Catcon.Desktop"
#endif

[Run]
Filename: "{app}\Catcon.exe"; Description: "启动蜜柑"; Flags: nowait postinstall skipifsilent

[Code]
const
  UninstallKey = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#AppIdentity}_is1';
var
  EnvironmentPage: TOutputMsgMemoWizardPage;

function ExistingVersion: String;
begin
  if not RegQueryStringValue(HKCU, UninstallKey, 'DisplayVersion', Result) then Result := '';
end;

function IsNewer(const Version: String): Boolean;
var Installed, Incoming: Int64;
begin
  Result := StrToVersion(Version, Installed) and StrToVersion('{#AppVersion}', Incoming);
  if Result then Result := ComparePackedVersion(Installed, Incoming) > 0;
end;

procedure InitializeWizard;
var Status, OldVersion: String;
begin
  OldVersion := ExistingVersion;
  if OldVersion = '' then OldVersion := '尚未安装';
  if IsDotNetInstalled(net48, 0) then Status := '已就绪（.NET Framework 4.8 或更高）'
  else Status := '需要补装 .NET Framework 4.8；点击安装后将调用微软安装程序，需要联网和管理员授权';
  EnvironmentPage := CreateOutputMsgMemoPage(wpWelcome, '安装前检查', '安装文件和现有数据将如何处理', '',
    '安装版本：{#AppVersion}'#13#10'当前版本：' + OldVersion + #13#10#13#10 +
    '运行环境：' + Status + #13#10#13#10 +
    '程序默认安装在当前用户目录，无需管理员权限。'#13#10 +
    '覆盖升级沿用原安装目录，保留位置、显示大小等设置和自定义皮肤。'#13#10 +
    '旧行为配置、识别规则、花园配置和默认皮肤源文件会先备份，再更新到新版。'#13#10 +
    '卸载仅移除安装器管理的程序文件，保留用户设置及备份。');
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var ExitCode: Integer; FileVersion: String;
begin
  Result := '';
  if IsNewer(ExistingVersion) then begin
    Result := '已经安装了更高版本，请使用相同或更新版本的安装包。现有文件未被修改。';
    Exit;
  end;
  if GetVersionNumbersString(ExpandConstant('{app}\Catcon.exe'), FileVersion) and IsNewer(FileVersion) then begin
    Result := '目标目录中有更高版本的蜜柑，已阻止降级覆盖。';
    Exit;
  end;
  if IsDotNetInstalled(net48, 0) then Exit;
  if WizardSilent then begin
    Result := '缺少 .NET Framework 4.8。请先安装微软运行库，或使用交互式安装完成补装。';
    Exit;
  end;
  ExtractTemporaryFile('ndp48-web.exe');
  if not ShellExec('runas', ExpandConstant('{tmp}\ndp48-web.exe'),
    '/passive /norestart /ChainingPackage Catcon', '', SW_SHOWNORMAL, ewWaitUntilTerminated, ExitCode) then begin
    Result := '无法启动运行库安装，或管理员授权已取消：' + SysErrorMessage(ExitCode);
    Exit;
  end;
  if (ExitCode = 3010) or (ExitCode = 1641) then begin
    NeedsRestart := True;
    Result := '微软运行库已安装，需要先重启 Windows。请保存工作，重启后重新运行蜜柑安装包。';
  end else if (ExitCode <> 0) or not IsDotNetInstalled(net48, 0) then
    Result := '运行库尚未安装成功（返回码 ' + IntToStr(ExitCode) + '）。请检查网络或 Windows 更新后重试。';
end;

procedure BackupExistingConfiguration;
var BackupDir, BackupBase, Source: String; Files: TArrayOfString; I, Suffix: Integer; HasOldFiles: Boolean;
begin
  Files := ['config\behaviors.json', 'config\classifiers.json', 'config\garden.json', 'assets\skins\mikan\skin.json'];
  HasOldFiles := False;
  for I := 0 to GetArrayLength(Files)-1 do
    if FileExists(ExpandConstant('{app}\') + Files[I]) then HasOldFiles := True;
  if not HasOldFiles then Exit;
  BackupBase := ExpandConstant('{#DataRoot}\Backups\') + GetDateTimeString('yyyymmdd-hhnnss', '-', ':');
  BackupDir := BackupBase;
  Suffix := 0;
  while DirExists(BackupDir) do begin Suffix := Suffix + 1; BackupDir := BackupBase + '-' + IntToStr(Suffix); end;
  for I := 0 to GetArrayLength(Files)-1 do begin
    Source := ExpandConstant('{app}\') + Files[I];
    if FileExists(Source) then begin
      if not ForceDirectories(ExtractFileDir(BackupDir + '\' + Files[I])) then
        RaiseException('无法创建升级备份目录，已停止安装。');
      if not FileCopy(Source, BackupDir + '\' + Files[I], True) then
        RaiseException('无法备份旧配置，已停止安装：' + Files[I]);
    end;
  end;
  SaveStringToFile(BackupDir + '\previous-version.txt', ExistingVersion, False);
  Log('Old configuration backup: ' + BackupDir);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then BackupExistingConfiguration;
end;

#define AppName "AspenBurner"
#ifndef AppVersion
  #error AppVersion is required.
#endif
#ifndef ReleaseName
  #error ReleaseName is required.
#endif
#ifndef SourceDir
  #error SourceDir is required.
#endif
#ifndef OutputDir
  #error OutputDir is required.
#endif

[Setup]
AppId={{5A9B93E7-B6C3-4F7A-9A72-1B5D43FBE9C3}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Aspen.zhang
DefaultDirName={localappdata}\Programs\AspenBurner
DefaultGroupName=AspenBurner
OutputDir={#OutputDir}
OutputBaseFilename={#ReleaseName}-setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\AspenBurner.exe

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加任务:"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\AspenBurner"; Filename: "{app}\Start-Crosshair.cmd"; WorkingDir: "{app}"
Name: "{group}\AspenBurner 设置"; Filename: "{app}\Configure-Crosshair.cmd"; WorkingDir: "{app}"
Name: "{group}\AspenBurner 停止"; Filename: "{app}\Stop-Crosshair.cmd"; WorkingDir: "{app}"
Name: "{autodesktop}\AspenBurner"; Filename: "{app}\Start-Crosshair.cmd"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\Start-Crosshair.cmd"; Description: "立即启动 AspenBurner"; Flags: nowait postinstall skipifsilent shellexec

[UninstallDelete]
Type: filesandordirs; Name: "{app}\logs"
Type: filesandordirs; Name: "{app}\config"

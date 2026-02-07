# Simple Installer Creator
# Run this AFTER building through Visual Studio
# Usage: powershell -File .\build-installer.ps1

$ErrorActionPreference = "Stop"
$ProjectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$OutputBase = Join-Path $ProjectDir "Publish\Installers"

Write-Host @"

MicMuteNet Installer Creator
============================
This script creates installers from your Visual Studio builds.

Make sure you've built the project first:
- For x64: Build with 'Release | x64' in Visual Studio
- For ARM64: Build with 'Release | ARM64' in Visual Studio

"@ -ForegroundColor Cyan

# Create output directory
if (!(Test-Path $OutputBase)) {
    New-Item -ItemType Directory -Path $OutputBase -Force | Out-Null
}

# Check for x64 Debug build
$x64DebugPath = Join-Path $ProjectDir "MicMuteNet\bin\x64\Debug\net10.0-windows10.0.19041.0\"
$hasx64Debug = Test-Path "$x64DebugPath\MicMuteNet.exe"

# Check for x64 Release build
$x64ReleasePath = Join-Path $ProjectDir "MicMuteNet\bin\x64\Release\net10.0-windows10.0.19041.0\"
$hasx64Release = Test-Path "$x64ReleasePath\MicMuteNet.exe"

# Check for x86 Debug build
$x86DebugPath = Join-Path $ProjectDir "MicMuteNet\bin\x86\Debug\net10.0-windows10.0.19041.0\"
$hasx86Debug = Test-Path "$x86DebugPath\MicMuteNet.exe"

# Check for x86 Release build
$x86ReleasePath = Join-Path $ProjectDir "MicMuteNet\bin\x86\Release\net10.0-windows10.0.19041.0\"
$hasx86Release = Test-Path "$x86ReleasePath\MicMuteNet.exe"

# Check for ARM64 Debug build
$arm64DebugPath = Join-Path $ProjectDir "MicMuteNet\bin\ARM64\Debug\net10.0-windows10.0.19041.0\"
$hasArm64Debug = Test-Path "$arm64DebugPath\MicMuteNet.exe"

# Check for ARM64 Release build  
$arm64ReleasePath = Join-Path $ProjectDir "MicMuteNet\bin\ARM64\Release\net10.0-windows10.0.19041.0\"
$hasArm64Release = Test-Path "$arm64ReleasePath\MicMuteNet.exe"

# Use Release if available, otherwise Debug
$x64Path = if ($hasx64Release) { $x64ReleasePath } elseif ($hasx64Debug) { $x64DebugPath } else { $null }
$x86Path = if ($hasx86Release) { $x86ReleasePath } elseif ($hasx86Debug) { $x86DebugPath } else { $null }
$arm64Path = if ($hasArm64Release) { $arm64ReleasePath } elseif ($hasArm64Debug) { $arm64DebugPath } else { $null }

$hasX64 = $x64Path -ne $null
$hasX86 = $x86Path -ne $null
$hasArm64 = $arm64Path -ne $null

if (!$hasX64 -and !$hasX86 -and !$hasArm64) {
    Write-Host "ERROR: No builds found!" -ForegroundColor Red
    Write-Host "Please build the project in Visual Studio first" -ForegroundColor Yellow
    Write-Host "Supported: x64, x86, ARM64 (Debug or Release)" -ForegroundColor Yellow
    exit 1
}

# Find Inno Setup
$InnoPath = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (!$InnoPath) {
    Write-Host @"
Inno Setup 6 not found!

Download from: https://jrsoftware.org/isinfo.php
Install it, then run this script again.

"@ -ForegroundColor Yellow
    exit 1
}

# Create x64 installer
if ($hasX64) {
    Write-Host "`nCreating x64 installer..." -ForegroundColor Green
    
    $x64Script = @"
#define MyAppName "MicMuteNet"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "TNBB"
#define MyAppExeName "MicMuteNet.exe"

[Setup]
AppId={{A7B8C9D0-1234-5678-9ABC-DEF012345678}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=$OutputBase
OutputBaseFilename=MicMuteNet-Setup-x64
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=lowest
SetupIconFile=$ProjectDir\MicMuteNet\Assets\Icons\microphoneEnabled.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Files]
Source: "$x64Path\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{userdesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create desktop shortcut"

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Code]
function InitializeUninstall(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  if MsgBox('Do you want to completely remove MicMuteNet and all of its components?', mbConfirmation, MB_YESNO) = IDNO then
  begin
    Result := False;
  end;
end;

[UninstallRun]
; Remove registry startup entry (normal mode)
Filename: "reg.exe"; Parameters: "delete ""HKCU\Software\Microsoft\Windows\CurrentVersion\Run"" /v ""MicMuteNet"" /f"; Flags: runhidden; RunOnceId: "RemoveStartupReg"
; Remove scheduled task (admin mode)  
Filename: "schtasks.exe"; Parameters: "/Delete /TN ""MicMuteNet"" /F"; Flags: runhidden; RunOnceId: "RemoveStartupTask"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
"@
    
    $x64ScriptPath = Join-Path $OutputBase "setup-x64.iss"
    Set-Content -Path $x64ScriptPath -Value $x64Script -Encoding UTF8
    
    & $InnoPath $x64ScriptPath
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? x64 installer created!" -ForegroundColor Green
    }
}

# Create x86 installer
if ($hasX86) {
    Write-Host "`nCreating x86 installer..." -ForegroundColor Green
    
    $x86Script = @"
#define MyAppName "MicMuteNet"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "TNBB"
#define MyAppExeName "MicMuteNet.exe"

[Setup]
AppId={{A7B8C9D0-1234-5678-9ABC-DEF012345677}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=$OutputBase
OutputBaseFilename=MicMuteNet-Setup-x86
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=lowest
SetupIconFile=$ProjectDir\MicMuteNet\Assets\Icons\microphoneEnabled.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x86compatible
ArchitecturesInstallIn64BitMode=

[Files]
Source: "$x86Path\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{userdesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create desktop shortcut"

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Code]
function InitializeUninstall(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  if MsgBox('Do you want to completely remove MicMuteNet and all of its components?', mbConfirmation, MB_YESNO) = IDNO then
  begin
    Result := False;
  end;
end;

[UninstallRun]
; Remove registry startup entry (normal mode)
Filename: "reg.exe"; Parameters: "delete ""HKCU\Software\Microsoft\Windows\CurrentVersion\Run"" /v ""MicMuteNet"" /f"; Flags: runhidden; RunOnceId: "RemoveStartupReg"
; Remove scheduled task (admin mode)
Filename: "schtasks.exe"; Parameters: "/Delete /TN ""MicMuteNet"" /F"; Flags: runhidden; RunOnceId: "RemoveStartupTask"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
"@
    
    $x86ScriptPath = Join-Path $OutputBase "setup-x86.iss"
    Set-Content -Path $x86ScriptPath -Value $x86Script -Encoding UTF8
    
    & $InnoPath $x86ScriptPath
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? x86 installer created!" -ForegroundColor Green
    }
}

# Create ARM64 installer
if ($hasArm64) {
    Write-Host "`nCreating ARM64 installer..." -ForegroundColor Green
    
    $arm64Script = @"
#define MyAppName "MicMuteNet"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "TNBB"
#define MyAppExeName "MicMuteNet.exe"

[Setup]
AppId={{A7B8C9D0-1234-5678-9ABC-DEF012345679}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=$OutputBase
OutputBaseFilename=MicMuteNet-Setup-ARM64
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=lowest
SetupIconFile=$ProjectDir\MicMuteNet\Assets\Icons\microphoneEnabled.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64

[Files]
Source: "$arm64Path\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{userdesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create desktop shortcut"

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Code]
function InitializeUninstall(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  if MsgBox('Do you want to completely remove MicMuteNet and all of its components?', mbConfirmation, MB_YESNO) = IDNO then
  begin
    Result := False;
  end;
end;

[UninstallRun]
; Remove registry startup entry (normal mode)
Filename: "reg.exe"; Parameters: "delete ""HKCU\Software\Microsoft\Windows\CurrentVersion\Run"" /v ""MicMuteNet"" /f"; Flags: runhidden; RunOnceId: "RemoveStartupReg"
; Remove scheduled task (admin mode)
Filename: "schtasks.exe"; Parameters: "/Delete /TN ""MicMuteNet"" /F"; Flags: runhidden; RunOnceId: "RemoveStartupTask"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
"@
    
    $arm64ScriptPath = Join-Path $OutputBase "setup-arm64.iss"
    Set-Content -Path $arm64ScriptPath -Value $arm64Script -Encoding UTF8
    
    & $InnoPath $arm64ScriptPath
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? ARM64 installer created!" -ForegroundColor Green
    }
}

Write-Host @"

Done! Installers created in:
$OutputBase

"@ -ForegroundColor Cyan

# MicMuteNet Build Script
# Usage: powershell -File .\build-app.ps1 -portable
#        powershell -File .\build-app.ps1 -portableZip
#        powershell -File .\build-app.ps1 -installer

param(
    [switch]$portable,
    [switch]$portableZip,
    [switch]$installer,
    [switch]$help
)

$ErrorActionPreference = "Stop"
$ProjectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$OutputBase = Join-Path $ProjectDir "Publish"
$PortableDir = Join-Path $OutputBase "MicMuteNet-Portable"

# Show help if no args or --help
if ($help -or (-not $portable -and -not $portableZip -and -not $installer)) {
    Write-Host @"
MicMuteNet Build Script

Usage: powershell -File .\build-app.ps1 [option]

Options:
  -portable      Create portable folder (no installation required)
  -portableZip   Create portable folder + ZIP archive
  -installer     Create Inno Setup installer (.exe)

Examples:
  powershell -File .\build-app.ps1 -portable
  powershell -File .\build-app.ps1 -installer

Output will be in: $OutputBase
"@
    exit 0
}

# Build the project first
function Build-Project {
    Write-Host "`nBuilding MicMuteNet..." -ForegroundColor Cyan
    
    # Clean previous build
    if (Test-Path $PortableDir) {
        Remove-Item $PortableDir -Recurse -Force
    }
    
    # Publish self-contained with Windows App SDK bundled
    Write-Host "Publishing self-contained release..." -ForegroundColor Yellow
    
    $result = & dotnet publish "$ProjectDir\MicMuteNet\MicMuteNet.csproj" `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:PublishReadyToRun=true `
        -p:PublishTrimmed=false `
        -p:WindowsAppSDKSelfContained=true `
        -o $PortableDir 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        Write-Host $result
        exit 1
    }
    
    Write-Host "Build succeeded!" -ForegroundColor Green
}

# Portable folder
if ($portable -or $portableZip) {
    Build-Project
    Write-Host "`nPortable folder: $PortableDir" -ForegroundColor Cyan
    
    if ($portableZip) {
        $ZipPath = Join-Path $OutputBase "MicMuteNet-Portable.zip"
        if (Test-Path $ZipPath) {
            Remove-Item $ZipPath -Force
        }
        Write-Host "Creating ZIP archive..." -ForegroundColor Yellow
        Compress-Archive -Path "$PortableDir\*" -DestinationPath $ZipPath
        Write-Host "ZIP archive: $ZipPath" -ForegroundColor Cyan
    }
}

# Installer
if ($installer) {
    Build-Project
    
    Write-Host "`nCreating Inno Setup installer..." -ForegroundColor Cyan
    
    # Create Inno Setup script
    $InnoScript = Join-Path $OutputBase "setup.iss"
    $InstallerOutput = Join-Path $OutputBase "Installer"
    
    if (-not (Test-Path $InstallerOutput)) {
        New-Item -ItemType Directory -Path $InstallerOutput -Force | Out-Null
    }
    
    $InnoContent = @"
; MicMuteNet Inno Setup Script
[Setup]
AppName=MicMuteNet
AppVersion=1.0.0
AppPublisher=MicMuteNet
DefaultDirName={autopf}\MicMuteNet
DefaultGroupName=MicMuteNet
OutputDir=$InstallerOutput
OutputBaseFilename=MicMuteNet-Setup
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=lowest
DisableDirPage=yes
SetupIconFile=$ProjectDir\MicMuteNet\Assets\Icons\microphoneEnabled.ico
UninstallDisplayIcon={app}\MicMuteNet.exe

[Files]
Source: "$PortableDir\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\MicMuteNet"; Filename: "{app}\MicMuteNet.exe"
Name: "{group}\Uninstall MicMuteNet"; Filename: "{uninstallexe}"
Name: "{autodesktop}\MicMuteNet"; Filename: "{app}\MicMuteNet.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"

[Run]
Filename: "{app}\MicMuteNet.exe"; Description: "Launch MicMuteNet"; Flags: nowait postinstall skipifsilent
"@
    
    Set-Content -Path $InnoScript -Value $InnoContent -Encoding UTF8
    
    # Check if Inno Setup is installed
    $InnoPath = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    ) | Where-Object { Test-Path $_ } | Select-Object -First 1
    
    if (-not $InnoPath) {
        Write-Host @"

Inno Setup 6 not found!

To create the installer:
1. Download Inno Setup 6 from: https://jrsoftware.org/isinfo.php
2. Install it (default location)
3. Run this script again

Or manually compile: $InnoScript

"@ -ForegroundColor Yellow
        exit 0
    }
    
    Write-Host "Compiling installer with Inno Setup..." -ForegroundColor Yellow
    & $InnoPath $InnoScript
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`nInstaller created: $InstallerOutput\MicMuteNet-Setup.exe" -ForegroundColor Green
    } else {
        Write-Host "Inno Setup compilation failed!" -ForegroundColor Red
        exit 1
    }
}

Write-Host "`nDone!" -ForegroundColor Green

# MicMuteNet Build & Distribution Guide

## Quick Start

### Development Build

```powershell
cd MicMuteNet
dotnet build
```

### Debug Run

Open in Visual Studio → Set **MicMuteNet (Package)** as Startup Project → Press F5

---

## Creating Installers

### Option 1: MSIX Package (Recommended for Windows)

MSIX is the modern Windows installer format. It provides:

- Clean install/uninstall
- Auto-updates (if distributed via Store or sideloading)
- Sandboxed installation

**Steps:**

1. **Build Release Package:**

   ```powershell
   # From solution root
   dotnet publish MicMuteNet\MicMuteNet.csproj -c Release -r win-x64 --self-contained
   ```

2. **Create MSIX via Visual Studio:**
   - Right-click **MicMuteNet (Package)** project
   - Select **Publish** → **Create App Packages**
   - Choose **Sideloading** (or Store if you have a dev account)
   - Select **Yes, use the current certificate** or create one
   - Select architecture (x64 recommended)
   - Click **Create**

3. **Output Location:**

   ```
   MicMuteNet.Package\AppPackages\
   ```

4. **Install:**
   - Right-click the `.msix` file → **Install**
   - Or run the `Install.ps1` script generated with the package

---

### Option 2: Portable ZIP (No Installation Required)

Create a self-contained folder that runs anywhere:

```powershell
# Build self-contained release
dotnet publish MicMuteNet\MicMuteNet.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o .\Portable

# The output will be in the .\Portable folder
# Zip it up for distribution
Compress-Archive -Path .\Portable\* -DestinationPath MicMuteNet-Portable.zip
```

**Portable Structure:**

```
MicMuteNet-Portable/
├── MicMuteNet.exe        (Main executable)
├── Assets/
│   ├── Icons/
│   │   ├── microphoneEnabled.ico
│   │   └── microphoneDisabled.ico
│   └── Sounds/
│       ├── beep300.wav
│       └── beep750.wav
└── settings.json         (Created on first run)
```

---

### Option 3: MSI Installer (Traditional)

Requires **WiX Toolset**:

1. **Install WiX:**

   ```powershell
   dotnet tool install -g wix
   ```

2. **Add WiX Project to Solution:**

   ```powershell
   wix init -n MicMuteNet.Installer
   ```

3. **Configure Product.wxs** with your app files

4. **Build MSI:**
   ```powershell
   wix build .\MicMuteNet.Installer\Product.wxs -o MicMuteNet.msi
   ```

> **Note:** MSI creation requires WiX v4+ configuration. For most scenarios, MSIX or Portable is simpler.

---

## Configuration File

Settings are stored in:

```
%LOCALAPPDATA%\MicMuteNet\settings.json
```

The file is auto-generated on first run with defaults:

```json
{
  "SelectedDeviceId": null,
  "MuteMode": "Toggle",
  "VolumeControlEnabled": false,
  "Hotkey": { "Key": "None", "Control": false, "Alt": false, "Shift": false },
  "OverlayEnabled": true,
  "OverlayOpacity": 0.8,
  "NotificationEnabled": true,
  "NotificationVolume": 0.5,
  "OutputDeviceNumber": -1,
  "RunAtStartup": false,
  "StartMinimized": false
}
```

---

## Icons

The app uses icons from the Assets folder:

- **Taskbar/Tray Icon:** `Assets/Icons/microphoneEnabled.ico` (unmuted) / `microphoneDisabled.ico` (muted)
- **Application Icon:** `microphoneEnabled.ico` (set in .csproj)

---

## Troubleshooting

### System Tray Not Showing

- Ensure you're running from the packaged app (F5 with Package as startup)
- Check Windows taskbar settings → "Select which icons appear on the taskbar"

### Audio Notifications Not Playing

- Check the output device selection in Settings
- Verify the sound files exist in `Assets/Sounds/`

### Hotkey Not Working

- Ensure the key isn't bound by another application
- Try enabling "Suppress key" option
- Some games may block global hotkeys

---

## Architecture

- **.NET 10** with **WinUI 3**
- **NAudio** for audio device control
- **H.NotifyIcon** for system tray
- **MVVM** pattern with **CommunityToolkit.Mvvm**
- **JSON** settings persistence

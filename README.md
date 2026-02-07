# MicMuteNet

A modern, lightweight Windows application to mute your microphone globally using hotkeys. Built with .NET 10 and WinUI 3.

## Features

- **Global Hotkeys**: Control your microphone from any application.
- **Mute Modes**:
  - **Toggle**: Press to mute/unmute.
  - **Push-to-Talk**: Hold to talk, release to mute.
  - **Push-to-Mute**: Hold to mute, release to talk.
- **Visual Overlay**: Customizable on-screen overlay for mute status (opacity, duration).
- **Audio Feedback**: Optional sound effects when toggling mute.
- **Device Control**: Select specific input/output devices or follow system defaults.
- **System Tray**: Runs silently in the background with tray icon support.
- **Startup Options**: Auto-start with Windows, start minimized, and mute on startup.

## Downloads

[**Download Latest Release**](https://github.com/psppspnaik209/MicMuteNet/releases/latest)

### 1. Choose your version

Determine your system type (Settings > System > About > System type).

| System Type        | Installer (Recommended)      | Portable (.zip)                 |
| :----------------- | :--------------------------- | :------------------------------ |
| **x64** (Standard) | `MicMuteNet-Setup-x64.exe`   | `MicMuteNet-Portable-x64.zip`   |
| **x86** (32-bit)   | `MicMuteNet-Setup-x86.exe`   | `MicMuteNet-Portable-x86.zip`   |
| **ARM64**          | `MicMuteNet-Setup-ARM64.exe` | `MicMuteNet-Portable-ARM64.zip` |

### 2. Install Prerequisites

You **must** install the **.NET 10 Desktop Runtime** for your system architecture if you haven't already.

- **x64**: [Download Runtime (x64)](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-10.0.2-windows-x64-installer)
- **x86**: [Download Runtime (x86)](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-10.0.2-windows-x86-installer)
- **ARM64**: [Download Runtime (ARM64)](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-10.0.2-windows-arm64-installer)

## Requirements

- Windows 10 (1809+) or Windows 11
- .NET 10 Runtime

## Building from Source

1. Install **Visual Studio 2026** with:
   - .NET Desktop Development workload
   - Windows App SDK
2. Clone this repository.
3. Open `MicMuteNet.slnx`.
4. Build and Run.

## Usage

1. Launch MicMuteNet.
2. Select your microphone from the dropdown.
3. Click "Record" in the Hotkey section and press your desired key combination.
4. Choose your preferred Mute Mode (Toggle is default).
5. Minimize the app to the tray and use your hotkey.

---

Heavily inspired by https://github.com/iXab3r/MicSwitch

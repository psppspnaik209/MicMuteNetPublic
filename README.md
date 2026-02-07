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

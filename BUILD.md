# MicMuteNet Build Scripts

## Quick Portable Build

This will create a self-contained portable folder you can zip and distribute:

```powershell
# From solution root
cd C:\Users\psppsp\source\repos\MicMuteNet
.\build-portable.ps1
```

## What the script creates:

```
Publish\MicMuteNet-Portable\
├── MicMuteNet.exe
├── Assets\
│   ├── Icons\
│   │   ├── microphoneEnabled.ico
│   │   └── microphoneDisabled.ico
│   └── Sounds\
│       ├── beep300.wav
│       └── beep750.wav
└── (runtime files)
```

## Notes

- The portable version is self-contained (no .NET required on target machine)
- Config file `settings.json` is created at `%LOCALAPPDATA%\MicMuteNet\` on first run
- For MSIX: Requires certificate signing (see BUILD.md for details)

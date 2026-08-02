# SideHustle

SideHustle is a small .NET 8 WinForms tray app for one very specific desktop setup:

- Fan Control stays pinned on the left side of the leftmost monitor.
- Steam Friends stays pinned on the right side of the same monitor.
- A focused, maximized non-pinned window fills the space between them and stops at the taskbar.

The app is intentionally narrow in scope. It is built around this exact monitor/window layout, not as a general-purpose window manager.

## Requirements

- Windows
- .NET 8 SDK for building
- Administrator privileges to control Fan Control

## Build

```powershell
dotnet build SideHustle.csproj
```

## Debug

Use VS Code as administrator, then launch the built EXE directly through the repo's `.vscode/launch.json` configuration.

## Publish

Single-file publish for x64 Windows:

```powershell
dotnet publish .\SideHustle.csproj -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true -o .\bin\pub
```

If you want it to stay smaller and rely on the machine already having the right .NET runtime installed, use:

```powershell
dotnet publish .\SideHustle.csproj -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -o .\bin\pub
```

Both produce a single executable under `.\bin\pub\`.

## Project Layout

- `Program.cs` starts WinForms and runs the tray context.
- `TrayApplicationContext.cs` owns the tray icon and controller lifecycle.
- `WindowLayoutController.cs` handles window matching, pinning, and center-window sizing.
- `SideHustle.csproj` contains the app settings and manifest reference.
- `app.manifest` requests administrator privileges.

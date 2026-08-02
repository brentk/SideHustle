# WindowManager

`WindowManager` is a small .NET 8 WinForms tray app built for one specific desktop setup: it keeps Fan Control pinned on the left and Steam Friends pinned on the right of my leftmost monitor, while letting the active maximized window fill the space between them.

Current behavior:
- Fan Control is kept on the left side.
- Steam Friends is kept on the right side.
- A maximized focused window, when it is not one of those pinned windows, is resized into the space between them.
- All three windows stop at the top of the taskbar.
- The center window intentionally overlaps a few pixels into Fan Control and Steam Friends to remove visible seams and reclaim dead space.

Fan Control runs elevated, so `WindowManager` must also run elevated to control it successfully.

## Requirements

- Windows
- .NET 8 SDK
- Administrator privileges when running the tray app

## Run In Debug

If you use VS Code:
- Open the workspace as administrator.
- Use the `Launch WindowManager` debug configuration.
- That configuration launches `bin/Debug/net8.0-windows/WindowManager.exe` directly so the manifest can request elevation.

## Build

```powershell
dotnet build WindowManager.csproj
```

## Publish Single EXE

For a self-contained single-file build:

```powershell
dotnet publish .\WindowManager.csproj -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true -o .\publish
```

If you want it to stay smaller and rely on the machine already having the right .NET runtime installed, use:

```powershell
dotnet publish .\WindowManager.csproj -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -o .\publish
```

Both produce a single executable under `publish\`.

## Project Files

- [`Program.cs`](Program.cs) starts the WinForms application.
- [`TrayApplicationContext.cs`](TrayApplicationContext.cs) creates the tray icon and starts the controller.
- [`WindowLayoutController.cs`](WindowLayoutController.cs) contains the window-matching and layout logic.
- [`app.manifest`](app.manifest) requests administrator privileges.
- [`.vscode/launch.json`](.vscode/launch.json) launches the elevated EXE for debugging.
- [`.vscode/tasks.json`](.vscode/tasks.json) builds the project before debugging.

## Layout Notes

- The app targets the leftmost monitor in `Screen.AllScreens`.
- Fan Control is kept at its own minimum usable width; the center window overlaps the unused right portion instead of trying to force Fan Control thinner.
- The center window uses the taskbar-aware working area vertically, but it intentionally overlaps slightly into the side windows horizontally to avoid visible seams.

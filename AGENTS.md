# AGENTS.md

## Project Overview
- `WindowManager` is a small .NET 8 WinForms tray app.
- Entry point:
  - [`Program.cs`](Program.cs) initializes WinForms and runs [`TrayApplicationContext`](TrayApplicationContext.cs).
- Current behavior:
  - Fan Control is pinned on the left side of the first/leftmost monitor.
  - Steam Friends is pinned on the right side of the same monitor.
  - A maximized, non-pinned focused window is resized into the space between those two side windows.
  - All three windows use the monitor working area vertically, so they stop at the top of the taskbar.
- Fan Control runs elevated, so the tray app must also run elevated to control it successfully.
- The center window is allowed to overlap the unusable portion of Fan Control's right side and slightly overlap Steam Friends at the right edge; do not treat the full visible widths as hard boundary space.

## Codebase Conventions
- Keep changes small and local.
- Prefer `apply_patch` for edits.
- Default to ASCII unless a file already uses non-ASCII.
- Do not revert user changes unless explicitly asked.
- Do not use destructive git commands.
- If behavior is already working for one window, avoid broad refactors that could break the working path while fixing the other side.

## Windows / WinForms Notes
- This project targets `net8.0-windows` and uses WinForms APIs plus Win32 interop.
- Be careful with P/Invoke signatures and `WINDOWPLACEMENT` initialization.
- The target monitor is the leftmost screen in `Screen.AllScreens`, not necessarily `Screen.PrimaryScreen`.
- Preserve the pinned windows' positions unless a change is explicitly intended.
- Fan Control may refuse resizing below its own minimum width; if the center gap needs to change, prefer adjusting the center calculation rather than forcing Fan Control smaller.
- The current layout intentionally leaves Fan Control at its minimum usable width and uses overlap constants to reclaim dead space in the center window.
- If a window seems uncontrollable, check integrity/elevation first before assuming the matcher is wrong.

## Build And Verify
- Build with:
  - `dotnet build`
- For a single-file release build, use:
  - `dotnet publish .\WindowManager.csproj -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true -o .\publish`
- If changing window behavior, verify the build and mention any assumptions about monitor targeting, elevation, or window matching.
- If VS Code debugging is involved:
  - Use `.vscode/launch.json` to launch `bin/Debug/net8.0-windows/WindowManager.exe` directly.
  - Start VS Code as administrator so the debugger can attach to the elevated tray app.

## Editing Targets
- `Program.cs` for app startup only.
- `TrayApplicationContext.cs` for tray/menu wiring and controller lifecycle.
- `WindowLayoutController.cs` for window matching, pinning, maximize behavior, and monitor selection.
- `WindowManager.csproj` only when changing project settings or framework behavior.
- `.vscode/launch.json` and `.vscode/tasks.json` if debugger startup behavior changes.
- `app.manifest` when changing elevation requirements.

## Current Implementation Notes
- `TrayApplicationContext` owns the tray icon and starts/stops the controller.
- `WindowLayoutController` runs a timer, enumerates windows, and applies the layout policy every few hundred milliseconds.
- Fan Control is discovered first by `Process.GetProcessesByName("FanControl")` and then by window title/class as a fallback.
- Steam Friends is discovered by title/process name matching.
- The controller uses `SetWindowPos` and `ShowWindow` directly; if a window does not move, check whether the process is elevated or whether the app is reasserting its own bounds.
- The controller caches desired pinned bounds by dock side, so any change to left/right sizing should be reflected in both the pinned window placement and the center-gap calculation.
- The center rectangle is taskbar-aware vertically and intentionally overlaps a few pixels into the side windows horizontally to remove visible seams.
- If you need to understand the layout math, inspect [`WindowLayoutController.cs`](WindowLayoutController.cs) before anything else.

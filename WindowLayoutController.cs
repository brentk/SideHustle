using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

internal sealed class WindowLayoutController : IDisposable
{
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Dictionary<string, Rectangle> _desiredPinnedBounds = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sync = new();

    private bool _running;

    public WindowLayoutController()
    {
        _timer = new System.Windows.Forms.Timer
        {
            Interval = 200
        };
        _timer.Tick += (_, _) => ApplyPolicy();
    }

    public void Start()
    {
        if (_running)
            return;

        _running = true;
        _timer.Start();
        ApplyPolicy();
    }

    public void Stop()
    {
        if (!_running)
            return;

        _running = false;
        _timer.Stop();
    }

    public void Dispose()
    {
        Stop();
        _timer.Dispose();
    }

    private void ApplyPolicy()
    {
        lock (_sync)
        {
            var primary = Screen.PrimaryScreen;
            if (primary is null)
                return;

            var pinnedWindows = FindPinnedWindows();

            foreach (var pinned in pinnedWindows)
            {
                var key = pinned.Side.ToString();

                if (!_desiredPinnedBounds.TryGetValue(key, out var desired))
                {
                    desired = pinned.Bounds;
                    _desiredPinnedBounds[key] = desired;
                }

                if (pinned.Bounds != desired)
                    RestoreBounds(pinned.Handle, desired);
            }

            var foreground = GetForegroundWindow();
            if (foreground == IntPtr.Zero)
                return;

            if (pinnedWindows.Any(p => p.Handle == foreground))
                return;

            if (!IsWindowVisible(foreground))
                return;

            if (!IsOnPrimaryMonitor(foreground, primary))
                return;

            if (!IsWindowMaximized(foreground))
                return;

            var centerBounds = TryGetCenterBounds(primary, pinnedWindows);
            if (centerBounds is null)
                return;

            RestoreToBounds(foreground, centerBounds.Value);
        }
    }

    private static void RestoreBounds(IntPtr hwnd, Rectangle bounds)
    {
        SetWindowPos(
            hwnd,
            IntPtr.Zero,
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height,
            SetWindowPosFlags.SWP_NOZORDER |
            SetWindowPosFlags.SWP_NOACTIVATE |
            SetWindowPosFlags.SWP_FRAMECHANGED);
    }

    private static void RestoreToBounds(IntPtr hwnd, Rectangle bounds)
    {
        ShowWindow(hwnd, ShowWindowCommands.Restore);
        SetWindowPos(
            hwnd,
            IntPtr.Zero,
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height,
            SetWindowPosFlags.SWP_NOZORDER |
            SetWindowPosFlags.SWP_NOACTIVATE |
            SetWindowPosFlags.SWP_FRAMECHANGED);
    }

    private static Rectangle? TryGetCenterBounds(Screen primary, IReadOnlyList<PinnedWindow> pinnedWindows)
    {
        var leftEdge = primary.WorkingArea.Left;
        var rightEdge = primary.WorkingArea.Right;

        var leftPinned = pinnedWindows.FirstOrDefault(p => p.Side == DockSide.Left);
        if (leftPinned.Handle != IntPtr.Zero)
            leftEdge = Math.Max(leftEdge, leftPinned.Bounds.Right);

        var rightPinned = pinnedWindows.FirstOrDefault(p => p.Side == DockSide.Right);
        if (rightPinned.Handle != IntPtr.Zero)
            rightEdge = Math.Min(rightEdge, rightPinned.Bounds.Left);

        if (rightEdge <= leftEdge + 100)
            return null;

        return Rectangle.FromLTRB(
            leftEdge,
            primary.WorkingArea.Top,
            rightEdge,
            primary.WorkingArea.Bottom);
    }

    private static bool IsOnPrimaryMonitor(IntPtr hwnd, Screen primary)
    {
        var windowScreen = Screen.FromHandle(hwnd);
        return windowScreen.Primary;
    }

    private static bool IsWindowMaximized(IntPtr hwnd)
    {
        var placement = new WINDOWPLACEMENT
        {
            length = Marshal.SizeOf<WINDOWPLACEMENT>()
        };

        return GetWindowPlacement(hwnd, ref placement) && placement.showCmd == ShowWindowCommands.Maximize;
    }

    private static List<PinnedWindow> FindPinnedWindows()
    {
        var windows = new List<PinnedWindow>();

        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd))
                return true;

            if (!GetWindowRect(hwnd, out var rect))
                return true;

            var windowRect = rect.ToRectangle();
            if (windowRect.Width <= 0 || windowRect.Height <= 0)
                return true;

            var title = GetWindowTitle(hwnd);
            var processName = GetProcessName(hwnd);

            if (IsFanControlWindow(title, processName))
            {
                windows.Add(new PinnedWindow(hwnd, DockSide.Left, windowRect, title, processName));
                return true;
            }

            if (IsSteamFriendsWindow(title, processName))
            {
                windows.Add(new PinnedWindow(hwnd, DockSide.Right, windowRect, title, processName));
                return true;
            }

            return true;
        }, IntPtr.Zero);

        return windows;
    }

    private static bool IsFanControlWindow(string title, string processName)
    {
        return title.Contains("fan control", StringComparison.OrdinalIgnoreCase) ||
               processName.Contains("fancontrol", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSteamFriendsWindow(string title, string processName)
    {
        return title.Contains("steam friends", StringComparison.OrdinalIgnoreCase) ||
               title.Contains("friends list", StringComparison.OrdinalIgnoreCase) ||
               (processName.Contains("steam", StringComparison.OrdinalIgnoreCase) &&
                title.Contains("friends", StringComparison.OrdinalIgnoreCase));
    }

    private static string GetWindowTitle(IntPtr hwnd)
    {
        var length = GetWindowTextLength(hwnd);
        if (length <= 0)
            return string.Empty;

        var builder = new StringBuilder(length + 1);
        _ = GetWindowText(hwnd, builder, builder.Capacity);
        return builder.ToString();
    }

    private static string GetProcessName(IntPtr hwnd)
    {
        _ = GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == 0)
            return string.Empty;

        try
        {
            return Process.GetProcessById((int)processId).ProcessName;
        }
        catch
        {
            return string.Empty;
        }
    }

    private readonly record struct PinnedWindow(IntPtr Handle, DockSide Side, Rectangle Bounds, string Title, string ProcessName);

    private enum DockSide
    {
        Left,
        Right
    }

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        SetWindowPosFlags uFlags);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommands nCmdShow);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Width => Right - Left;
        public int Height => Bottom - Top;

        public Rectangle ToRectangle() => Rectangle.FromLTRB(Left, Top, Right, Bottom);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WINDOWPLACEMENT
    {
        public int length;
        public int flags;
        public ShowWindowCommands showCmd;
        public POINT ptMinPosition;
        public POINT ptMaxPosition;
        public RECT rcNormalPosition;
    }

    [Flags]
    private enum SetWindowPosFlags : uint
    {
        SWP_NOSIZE = 0x0001,
        SWP_NOMOVE = 0x0002,
        SWP_NOZORDER = 0x0004,
        SWP_NOREDRAW = 0x0008,
        SWP_NOACTIVATE = 0x0010,
        SWP_FRAMECHANGED = 0x0020,
        SWP_SHOWWINDOW = 0x0040,
        SWP_HIDEWINDOW = 0x0080,
        SWP_NOCOPYBITS = 0x0100,
        SWP_NOOWNERZORDER = 0x0200,
        SWP_NOSENDCHANGING = 0x0400
    }

    private enum ShowWindowCommands
    {
        Hide = 0,
        Normal = 1,
        ShowMinimized = 2,
        Maximize = 3,
        ShowNoActivate = 4,
        Show = 5,
        Minimize = 6,
        ShowMinNoActive = 7,
        ShowNA = 8,
        Restore = 9
    }
}

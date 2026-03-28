using System.Drawing;
using System.Runtime.InteropServices;

namespace AspenBurner.App.Native;

/// <summary>
/// Win32 interop used by the overlay runtime.
/// </summary>
internal static class NativeMethods
{
    internal const int GwlExStyle = -20;
    internal const int WsExTransparent = 0x00000020;
    internal const int WsExLayered = 0x00080000;
    internal const int WsExToolWindow = 0x00000080;
    internal const int WsExNoActivate = 0x08000000;
    internal const uint SwpNomove = 0x0002;
    internal const uint SwpNosize = 0x0001;
    internal const uint SwpNoactivate = 0x0010;
    internal static readonly IntPtr HwndTopmost = new(-1);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern bool SetDllDirectory(string lpPathName);

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public Rectangle ToRectangle()
        {
            return Rectangle.FromLTRB(this.Left, this.Top, this.Right, this.Bottom);
        }
    }
}

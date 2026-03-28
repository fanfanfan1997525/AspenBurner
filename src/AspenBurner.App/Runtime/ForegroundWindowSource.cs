using System.Diagnostics;
using AspenBurner.App.Native;

namespace AspenBurner.App.Runtime;

/// <summary>
/// Reads the foreground window via Win32 APIs.
/// </summary>
public sealed class ForegroundWindowSource : IForegroundWindowSource
{
    /// <inheritdoc />
    public TargetWindowInfo? TryGetForegroundWindow()
    {
        IntPtr handle = NativeMethods.GetForegroundWindow();
        if (handle == IntPtr.Zero)
        {
            return null;
        }

        _ = NativeMethods.GetWindowThreadProcessId(handle, out uint processId);
        if (processId == 0 || !NativeMethods.GetWindowRect(handle, out NativeMethods.RECT rect))
        {
            return null;
        }

        try
        {
            using Process process = Process.GetProcessById((int)processId);
            return new TargetWindowInfo(handle, process.ProcessName, rect.ToRectangle());
        }
        catch
        {
            return null;
        }
    }
}

using System.Drawing;

namespace AspenBurner.App.Runtime;

/// <summary>
/// Represents the currently focused top-level window of interest.
/// </summary>
public sealed record TargetWindowInfo(IntPtr Handle, string ProcessName, Rectangle Bounds);

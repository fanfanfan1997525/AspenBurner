using System.Drawing.Drawing2D;
using AspenBurner.App.Configuration;
using AspenBurner.App.Core;
using AspenBurner.App.Native;

namespace AspenBurner.App.UI;

/// <summary>
/// Transparent click-through overlay used to render the crosshair.
/// </summary>
public sealed class CrosshairOverlayForm : Form
{
    private CrosshairConfig config = new();
    private Color crosshairColor = Color.Lime;

    /// <summary>
    /// Initializes a new overlay form.
    /// </summary>
    public CrosshairOverlayForm()
    {
        this.FormBorderStyle = FormBorderStyle.None;
        this.ShowInTaskbar = false;
        this.StartPosition = FormStartPosition.Manual;
        this.TopMost = true;
        this.BackColor = Color.Magenta;
        this.TransparencyKey = Color.Magenta;
        this.DoubleBuffered = true;
    }

    /// <inheritdoc />
    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams createParams = base.CreateParams;
            createParams.ExStyle |= NativeMethods.WsExTransparent |
                                    NativeMethods.WsExLayered |
                                    NativeMethods.WsExToolWindow |
                                    NativeMethods.WsExNoActivate;
            return createParams;
        }
    }

    /// <inheritdoc />
    protected override bool ShowWithoutActivation => true;

    /// <summary>
    /// Applies the current drawing configuration.
    /// </summary>
    public void ApplyConfig(CrosshairConfig nextConfig)
    {
        bool changed = this.config != nextConfig ||
                       this.crosshairColor.ToArgb() != ColorResolver.ResolveCrosshairColor(nextConfig).ToArgb();
        this.config = nextConfig ?? throw new ArgumentNullException(nameof(nextConfig));
        this.crosshairColor = ColorResolver.ResolveCrosshairColor(nextConfig);

        if (!changed)
        {
            return;
        }

        if (this.Visible)
        {
            this.Refresh();
            return;
        }

        this.Invalidate();
    }

    /// <summary>
    /// Reasserts the topmost flag without stealing focus.
    /// </summary>
    public void PinTopMost()
    {
        _ = NativeMethods.SetWindowPos(
            this.Handle,
            NativeMethods.HwndTopmost,
            0,
            0,
            0,
            0,
            NativeMethods.SwpNomove | NativeMethods.SwpNosize | NativeMethods.SwpNoactivate);
    }

    /// <inheritdoc />
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        this.PinTopMost();
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.None;
        int centerX = this.ClientSize.Width / 2;
        int centerY = this.ClientSize.Height / 2;
        IReadOnlyList<CrosshairSegment> segments = CrosshairGeometry.GetSegments(
            centerX,
            centerY,
            this.config.Length,
            this.config.Gap,
            this.config.ShowLeftArm,
            this.config.ShowRightArm,
            this.config.ShowTopArm,
            this.config.ShowBottomArm);

        if (this.config.OutlineThickness > 0)
        {
            using Pen outlinePen = this.CreatePen(Color.FromArgb(this.crosshairColor.A, Color.Black), this.config.Thickness + (this.config.OutlineThickness * 2));
            this.DrawSegments(e.Graphics, outlinePen, segments);
        }

        using Pen crosshairPen = this.CreatePen(this.crosshairColor, this.config.Thickness);
        this.DrawSegments(e.Graphics, crosshairPen, segments);
    }

    private Pen CreatePen(Color color, int width)
    {
        Pen pen = new(color, width)
        {
            StartCap = LineCap.Square,
            EndCap = LineCap.Square,
        };
        return pen;
    }

    private void DrawSegments(Graphics graphics, Pen pen, IEnumerable<CrosshairSegment> segments)
    {
        foreach (CrosshairSegment segment in segments)
        {
            graphics.DrawLine(pen, segment.X1, segment.Y1, segment.X2, segment.Y2);
        }
    }
}

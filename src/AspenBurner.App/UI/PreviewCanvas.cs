using System.Drawing.Drawing2D;
using AspenBurner.App.Configuration;
using AspenBurner.App.Core;

namespace AspenBurner.App.UI;

/// <summary>
/// Draws a live preview of the crosshair and CPU status overlay.
/// </summary>
public sealed class PreviewCanvas : Control
{
    private Rectangle statusBounds = Rectangle.Empty;
    private bool draggingStatus;
    private Point dragOffset;

    /// <summary>
    /// Initializes a new preview canvas.
    /// </summary>
    public PreviewCanvas()
    {
        this.Config = new CrosshairConfig();
        this.BackColor = Color.FromArgb(18, 20, 26);
        this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    /// <summary>
    /// Raised when the user drags the status badge to a new anchored placement.
    /// </summary>
    public event EventHandler<StatusPlacement>? StatusPlacementChanged;

    /// <summary>
    /// Gets or sets the configuration used for preview drawing.
    /// </summary>
    public CrosshairConfig Config { get; set; }

    /// <summary>
    /// Gets or sets the sample frequency value shown in preview.
    /// </summary>
    public int SampleFrequencyMHz { get; set; } = 4227;

    /// <summary>
    /// Gets or sets the sample temperature value shown in preview.
    /// </summary>
    public double? SampleTemperatureC { get; set; } = 82;

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.Clear(this.BackColor);
        e.Graphics.SmoothingMode = SmoothingMode.None;

        int centerX = (this.ClientSize.Width / 2) + this.Config.OffsetX;
        int centerY = (this.ClientSize.Height / 2) + this.Config.OffsetY;
        IReadOnlyList<CrosshairSegment> segments = CrosshairGeometry.GetSegments(
            centerX,
            centerY,
            this.Config.Length,
            this.Config.Gap,
            this.Config.ShowLeftArm,
            this.Config.ShowRightArm,
            this.Config.ShowTopArm,
            this.Config.ShowBottomArm);

        if (this.Config.OutlineThickness > 0)
        {
            using Pen outlinePen = this.CreatePen(Color.FromArgb(this.Config.Opacity, Color.Black), this.Config.Thickness + (this.Config.OutlineThickness * 2));
            this.DrawSegments(e.Graphics, outlinePen, segments);
        }

        using Pen crosshairPen = this.CreatePen(ColorResolver.ResolveCrosshairColor(this.Config), this.Config.Thickness);
        this.DrawSegments(e.Graphics, crosshairPen, segments);

        using Pen guidePen = new(Color.FromArgb(48, Color.White), 1);
        e.Graphics.DrawLine(guidePen, this.ClientSize.Width / 2, 0, this.ClientSize.Width / 2, this.ClientSize.Height);
        e.Graphics.DrawLine(guidePen, 0, this.ClientSize.Height / 2, this.ClientSize.Width, this.ClientSize.Height / 2);

        if (this.Config.StatusEnabled)
        {
            string statusText = StatusTextFormatter.FormatCpuStatus(this.SampleFrequencyMHz, this.SampleTemperatureC, approximateTemperature: false, this.Config.StatusShowTemperature);
            using Font statusFont = new("Consolas", this.Config.StatusFontSize, FontStyle.Bold, GraphicsUnit.Point);
            TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
            Size textSize = TextRenderer.MeasureText(statusText, statusFont, new Size(int.MaxValue, int.MaxValue), flags);
            this.statusBounds = StatusOverlayPlacement.GetBounds(
                0,
                0,
                this.ClientSize.Width,
                this.ClientSize.Height,
                textSize.Width + 12,
                textSize.Height + 8,
                this.Config.StatusPosition,
                this.Config.StatusOffsetX,
                this.Config.StatusOffsetY);

            Color textColor = ColorResolver.ResolveStatusColor(this.Config);
            TextRenderer.DrawText(e.Graphics, statusText, statusFont, new Point(this.statusBounds.Left + 7, this.statusBounds.Top + 5), Color.FromArgb(Math.Max(textColor.A / 2, 90), Color.Black), flags);
            TextRenderer.DrawText(e.Graphics, statusText, statusFont, new Point(this.statusBounds.Left + 6, this.statusBounds.Top + 4), textColor, flags);
        }
        else
        {
            this.statusBounds = Rectangle.Empty;
        }
    }

    /// <inheritdoc />
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button == MouseButtons.Left && this.Config.StatusEnabled && this.statusBounds.Contains(e.Location))
        {
            this.draggingStatus = true;
            this.dragOffset = new Point(e.X - this.statusBounds.Left, e.Y - this.statusBounds.Top);
            this.Cursor = Cursors.SizeAll;
        }
    }

    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (this.draggingStatus)
        {
            StatusPlacement placement = StatusOverlayPlacement.Resolve(
                this.ClientSize.Width,
                this.ClientSize.Height,
                this.statusBounds.Width,
                this.statusBounds.Height,
                e.X - this.dragOffset.X,
                e.Y - this.dragOffset.Y);

            this.StatusPlacementChanged?.Invoke(this, placement);
            this.Cursor = Cursors.SizeAll;
            return;
        }

        this.Cursor = this.Config.StatusEnabled && this.statusBounds.Contains(e.Location)
            ? Cursors.SizeAll
            : Cursors.Default;
    }

    /// <inheritdoc />
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        this.draggingStatus = false;
        this.Cursor = Cursors.Default;
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

using AspenBurner.App.Native;

namespace AspenBurner.App.UI;

/// <summary>
/// Transparent click-through overlay used to render CPU status text.
/// </summary>
public sealed class StatusOverlayForm : Form
{
    private const int PaddingX = 6;
    private const int PaddingY = 4;
    private string displayText = "CPU --.-GHz | --C";
    private float fontSize = 11f;
    private Color textColor = Color.Yellow;

    /// <summary>
    /// Initializes a new status overlay form.
    /// </summary>
    public StatusOverlayForm()
    {
        this.FormBorderStyle = FormBorderStyle.None;
        this.ShowInTaskbar = false;
        this.StartPosition = FormStartPosition.Manual;
        this.TopMost = true;
        this.BackColor = Color.Magenta;
        this.TransparencyKey = Color.Magenta;
        this.DoubleBuffered = true;
        this.UpdateWindowSize();
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
    /// Applies the current status text styling.
    /// </summary>
    public void ApplyStatus(string nextText, float nextFontSize, Color nextTextColor)
    {
        string normalizedText = string.IsNullOrWhiteSpace(nextText) ? "CPU --.-GHz | --C" : nextText;
        bool changed = !string.Equals(this.displayText, normalizedText, StringComparison.Ordinal) ||
                       Math.Abs(this.fontSize - nextFontSize) > 0.01f ||
                       this.textColor.ToArgb() != nextTextColor.ToArgb();

        this.displayText = normalizedText;
        this.fontSize = nextFontSize;
        this.textColor = nextTextColor;
        this.UpdateWindowSize();

        if (changed)
        {
            if (this.Visible)
            {
                this.Refresh();
                return;
            }

            this.Invalidate();
        }
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

        using Font font = this.CreateFont();
        TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
        TextRenderer.DrawText(e.Graphics, this.displayText, font, new Point(PaddingX + 1, PaddingY + 1), Color.FromArgb(Math.Max(this.textColor.A / 2, 90), Color.Black), flags);
        TextRenderer.DrawText(e.Graphics, this.displayText, font, new Point(PaddingX, PaddingY), this.textColor, flags);
    }

    private Font CreateFont()
    {
        return new Font("Consolas", this.fontSize, FontStyle.Bold, GraphicsUnit.Point);
    }

    private void UpdateWindowSize()
    {
        using Font font = this.CreateFont();
        TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
        Size textSize = TextRenderer.MeasureText(this.displayText, font, new Size(int.MaxValue, int.MaxValue), flags);
        this.Size = new Size(Math.Max(40, textSize.Width + (PaddingX * 2)), Math.Max(18, textSize.Height + (PaddingY * 2)));
    }
}

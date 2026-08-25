using System.Drawing.Drawing2D;

namespace PGEScarping.Helpers;

// A Panel-based button so it can be filled with a smooth gradient and fully rounded corners —
// a native WinForms Button can't render either without owner-draw gymnastics.
public sealed class GradientButton : Panel
{
    public string IconGlyph { get; set; } = "";
    public Color ColorStart { get; set; } = UiStyleHelper.AccentStart;
    public Color ColorEnd { get; set; } = UiStyleHelper.AccentEnd;
    public Color TextColor { get; set; } = Color.White;
    public Font TextFont { get; set; } = new("Segoe UI", 10.5f, FontStyle.Bold);

    // Secondary buttons are flat surface-colored rather than a gradient, which on a light
    // background needs a visible border to read as a button at all.
    public bool ShowBorder { get; set; }

    private bool _hover;
    private bool _pressed;
    private bool _enabled = true;

    public new bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            Cursor = value ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
    }

    public GradientButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Cursor = Cursors.Hand;
        Height = 44;
        BackColor = Color.Transparent;

        MouseEnter += (_, _) => { _hover = true; Invalidate(); };
        MouseLeave += (_, _) => { _hover = false; _pressed = false; Invalidate(); };
        MouseDown += (_, _) => { _pressed = true; Invalidate(); };
        MouseUp += (_, _) => { _pressed = false; Invalidate(); };
    }

    protected override void OnClick(EventArgs e)
    {
        if (_enabled)
            base.OnClick(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        var radius = Math.Min(Height, Width) / 2;

        Color start, end;
        if (!_enabled)
        {
            start = end = UiStyleHelper.Surface;
        }
        else if (_pressed)
        {
            start = ControlPaint.Dark(ColorStart, 0.08f);
            end = ControlPaint.Dark(ColorEnd, 0.08f);
        }
        else if (_hover)
        {
            start = ControlPaint.Light(ColorStart, 0.12f);
            end = ControlPaint.Light(ColorEnd, 0.12f);
        }
        else
        {
            start = ColorStart;
            end = ColorEnd;
        }

        using var path = UiStyleHelper.RoundedRectPath(rect, radius);
        using var brush = new LinearGradientBrush(new Rectangle(0, 0, Math.Max(Width, 1), Math.Max(Height, 1)), start, end, LinearGradientMode.Horizontal);
        g.FillPath(brush, path);

        if (ShowBorder)
        {
            using var borderPen = new Pen(_hover ? UiStyleHelper.Accent : UiStyleHelper.Border, 1.4f);
            g.DrawPath(borderPen, path);
        }

        var content = string.IsNullOrEmpty(IconGlyph) ? Text : $"{IconGlyph}  {Text}";
        TextRenderer.DrawText(g, content, TextFont, ClientRectangle, _enabled ? TextColor : UiStyleHelper.TextSecondary,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}

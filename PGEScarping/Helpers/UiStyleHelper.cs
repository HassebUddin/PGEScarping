using System.Drawing.Drawing2D;

namespace PGEScarping.Helpers;

public static class UiStyleHelper
{
    // Minimal black/white palette — near-white surfaces everywhere, ink-black as the one accent
    // color, no gradients or extra hues.
    public static readonly Color Background = Color.FromArgb(250, 250, 250);
    public static readonly Color Sidebar = Color.FromArgb(255, 255, 255);
    public static readonly Color SidebarDeep = Color.FromArgb(255, 255, 255);
    public static readonly Color Surface = Color.FromArgb(255, 255, 255);
    public static readonly Color SurfaceHover = Color.FromArgb(244, 244, 245);
    public static readonly Color Border = Color.FromArgb(228, 228, 231);
    public static readonly Color Accent = Color.FromArgb(17, 17, 17);
    public static readonly Color AccentSoft = Color.FromArgb(244, 244, 245);
    public static readonly Color AccentStart = Color.FromArgb(24, 24, 27);
    public static readonly Color AccentEnd = Color.FromArgb(9, 9, 11);
    public static readonly Color Success = Color.FromArgb(22, 163, 74);
    public static readonly Color Warning = Color.FromArgb(180, 83, 9);
    public static readonly Color Danger = Color.FromArgb(185, 28, 28);
    public static readonly Color TextPrimary = Color.FromArgb(17, 17, 17);
    public static readonly Color TextSecondary = Color.FromArgb(113, 113, 122);
    public static readonly Color LogText = Color.FromArgb(21, 128, 61);

    public static void PaintVerticalGradient(Control control, Color top, Color bottom)
    {
        control.Paint += (_, e) =>
        {
            if (control.Width <= 0 || control.Height <= 0)
                return;

            using var brush = new LinearGradientBrush(control.ClientRectangle, top, bottom, LinearGradientMode.Vertical);
            e.Graphics.FillRectangle(brush, control.ClientRectangle);
        };
    }

    public static GraphicsPath RoundedRectPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void ApplyRoundedCorners(Control control, int radius)
    {
        control.Resize += (_, _) => SetRegion(control, radius);
        SetRegion(control, radius);
    }

    private static void SetRegion(Control control, int radius)
    {
        if (control.Width <= 0 || control.Height <= 0)
            return;

        var path = new GraphicsPath();
        var rect = new Rectangle(0, 0, control.Width, control.Height);
        var diameter = radius * 2;

        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        control.Region = new Region(path);
    }
}

using System.Drawing.Drawing2D;

namespace PGEScarping.Helpers;

public static class UiStyleHelper
{
    public static readonly Color Background = Color.FromArgb(15, 16, 22);
    public static readonly Color Sidebar = Color.FromArgb(19, 20, 28);
    public static readonly Color SidebarDeep = Color.FromArgb(13, 14, 20);
    public static readonly Color Surface = Color.FromArgb(26, 28, 38);
    public static readonly Color SurfaceHover = Color.FromArgb(34, 37, 50);
    public static readonly Color Accent = Color.FromArgb(94, 129, 244);
    public static readonly Color AccentSoft = Color.FromArgb(40, 44, 66);
    public static readonly Color AccentStart = Color.FromArgb(99, 102, 241);
    public static readonly Color AccentEnd = Color.FromArgb(56, 189, 248);
    public static readonly Color Success = Color.FromArgb(84, 209, 143);
    public static readonly Color Warning = Color.FromArgb(224, 168, 84);
    public static readonly Color Danger = Color.FromArgb(239, 83, 96);
    public static readonly Color TextPrimary = Color.FromArgb(232, 234, 240);
    public static readonly Color TextSecondary = Color.FromArgb(140, 145, 165);
    public static readonly Color LogText = Color.FromArgb(150, 230, 170);

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

using System.Drawing.Drawing2D;

namespace PGEScarping.Helpers;

// A slim, on-brand replacement for the native ProgressBar's marquee mode, which can't be recolored
// without touching the Windows theme API — this just slides a gradient chunk across a flat track.
public sealed class PillProgressBar : Panel
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 16 };
    private float _offset;
    private bool _running;

    public PillProgressBar()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Height = 4;
        BackColor = UiStyleHelper.Surface;
        _timer.Tick += (_, _) =>
        {
            _offset = (_offset + 0.015f) % 1f;
            Invalidate();
        };
    }

    public bool IsRunning
    {
        get => _running;
        set
        {
            if (_running == value)
                return;

            _running = value;
            if (value)
            {
                _timer.Start();
            }
            else
            {
                _timer.Stop();
                _offset = 0;
                Invalidate();
            }
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (!_running || Width <= 0)
            return;

        var g = e.Graphics;
        var chunkWidth = Width * 0.28f;
        var x = (Width + chunkWidth) * _offset - chunkWidth;
        var rect = new RectangleF(x, 0, chunkWidth, Height);

        using var brush = new LinearGradientBrush(new RectangleF(0, 0, Math.Max(Width, 1), Math.Max(Height, 1)),
            UiStyleHelper.AccentStart, UiStyleHelper.AccentEnd, LinearGradientMode.Horizontal);
        g.FillRectangle(brush, rect);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _timer.Dispose();

        base.Dispose(disposing);
    }
}

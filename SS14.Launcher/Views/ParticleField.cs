using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Splat;
using SS14.Launcher.Models.Themes;
using SS14.Launcher.Utility;

namespace SS14.Launcher.Views;

/// <summary>
/// Slowly drifting glowing dots drawn over the window for themes that define a particle color.
/// Draws nothing (and stops its timer) when the theme has none or particles are disabled.
/// </summary>
public sealed class ParticleField : Control
{
    private const int ParticleCount = 45;

    private struct Particle
    {
        public double X, Y;       // 0..1 relative
        public double VX, VY;     // relative units per second
        public double Radius;     // px
        public double Alpha;      // peak alpha 0..1
        public double Phase;
    }

    private readonly Random _rng = new();
    private readonly Particle[] _particles = new Particle[ParticleCount];
    private readonly DispatcherTimer _timer;
    private ThemeManager? _themes;
    private DateTime _last;
    private double _time;
    private bool _active;

    public ParticleField()
    {
        IsHitTestVisible = false;
        _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(40) };
        _timer.Tick += OnTick;

        for (var i = 0; i < _particles.Length; i++)
            _particles[i] = NewParticle(randomY: true);
    }

    private Particle NewParticle(bool randomY)
    {
        return new Particle
        {
            X = _rng.NextDouble(),
            Y = randomY ? _rng.NextDouble() : 1.05,
            VX = (_rng.NextDouble() - 0.5) * 0.01,
            VY = -(0.008 + _rng.NextDouble() * 0.02),
            Radius = 1 + _rng.NextDouble() * 2.5,
            Alpha = 0.25 + _rng.NextDouble() * 0.45,
            Phase = _rng.NextDouble() * Math.PI * 2,
        };
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _themes = Locator.Current.GetService<ThemeManager>();
        if (_themes != null)
            _themes.ThemeChanged += Refresh;
        Refresh();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (_themes != null)
            _themes.ThemeChanged -= Refresh;
        _themes = null;
        SetActive(false);
    }

    private void Refresh()
    {
        SetActive(_themes is { ParticlesEnabled: true, Current.ParticleColor: not null });
    }

    private void SetActive(bool active)
    {
        if (_active == active)
        {
            InvalidateVisual();
            return;
        }

        _active = active;
        if (active)
        {
            _last = DateTime.UtcNow;
            _timer.Start();
        }
        else
        {
            _timer.Stop();
        }

        InvalidateVisual();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var now = DateTime.UtcNow;
        var dt = Math.Min((now - _last).TotalSeconds, 0.25);
        _last = now;
        _time += dt;

        for (var i = 0; i < _particles.Length; i++)
        {
            ref var p = ref _particles[i];
            p.X += p.VX * dt;
            p.Y += p.VY * dt;
            if (p.Y < -0.05 || p.X < -0.05 || p.X > 1.05)
                p = NewParticle(randomY: false);
        }

        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (!_active || _themes?.Current.ParticleColor is not { } color)
            return;

        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0)
            return;

        foreach (var p in _particles)
        {
            // Gentle twinkle plus fade-out near the top.
            var twinkle = 0.65 + 0.35 * Math.Sin(_time * 1.5 + p.Phase);
            var fade = Math.Clamp(p.Y * 2.5, 0, 1);
            var a = p.Alpha * twinkle * fade;
            if (a <= 0.01)
                continue;

            var center = new Point(p.X * w, p.Y * h);

            var glow = new SolidColorBrush(color, a * 0.22);
            context.DrawEllipse(glow, null, center, p.Radius * 3.2, p.Radius * 3.2);

            var core = new SolidColorBrush(color, a);
            context.DrawEllipse(core, null, center, p.Radius, p.Radius);
        }
    }
}

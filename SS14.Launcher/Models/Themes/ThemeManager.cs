using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Serilog;
using SS14.Launcher.Models.Data;

namespace SS14.Launcher.Models.Themes;

/// <summary>
/// Applies themes by overwriting the theme resource keys in <see cref="Application.Resources"/>.
/// All XAML uses DynamicResource, so the change is visible immediately without restarting.
/// </summary>
public sealed class ThemeManager
{
    private readonly DataManager _cfg;

    public ThemeDefinition Current { get; private set; } = ThemePalettes.DarkGrayTheme;

    /// <summary>Raised after a theme (or the particle setting) has been applied.</summary>
    public event Action? ThemeChanged;

    public ThemeManager(DataManager cfg)
    {
        _cfg = cfg;
    }

    public bool ParticlesEnabled => _cfg.GetCVar(CVars.ThemeParticles);

    public void Initialize()
    {
        Apply(ThemePalettes.Get(_cfg.GetCVar(CVars.Theme)));
    }

    public void SetTheme(string id)
    {
        _cfg.SetCVar(CVars.Theme, id);
        _cfg.CommitConfig();
        Apply(ThemePalettes.Get(id));
    }

    public void SetParticles(bool enabled)
    {
        _cfg.SetCVar(CVars.ThemeParticles, enabled);
        _cfg.CommitConfig();
        ThemeChanged?.Invoke();
    }

    private void Apply(ThemeDefinition theme)
    {
        var app = Application.Current;
        if (app == null)
            return;

        Current = theme;
        var res = app.Resources;

        foreach (var (key, color) in theme.Colors)
        {
            res[key + "Color"] = color;
            res[key + "Brush"] = new ImmutableSolidColorBrush(color);
        }

        res["ThemeHeaderBackgroundBrush"] = new ImmutableSolidColorBrush(theme.HeaderBackground);
        res["ThemeListBackgroundBrush"] = new ImmutableSolidColorBrush(theme.ListBackground);
        res["ThemeListAltRowBrush"] = new ImmutableSolidColorBrush(theme.ListAltRow);
        res["ThemeSeparatorBrush"] = new ImmutableSolidColorBrush(theme.Separator);
        res["ThemeButtonForegroundBrush"] = new ImmutableSolidColorBrush(theme.ButtonForeground);

        if (theme.ButtonGradient is var (from, to))
        {
            res["ThemeButtonBackgroundBrush"] = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(from, 0),
                    new GradientStop(to, 1),
                }
            };
        }
        else
        {
            res["ThemeButtonBackgroundBrush"] = new ImmutableSolidColorBrush(theme.Colors["ThemeControlMid"]);
        }

        res["ThemeStripeBackBrush"] = MakeStripeBrush(theme.TabStripe.Base, theme.TabStripe.Stripe);

        Log.Information("Applied launcher theme {Theme}", theme.Id);
        ThemeChanged?.Invoke();
    }

    private static IBrush MakeStripeBrush(Color baseColor, Color stripe)
    {
        var stripeBrush = new ImmutableSolidColorBrush(stripe);
        return new VisualBrush
        {
            Visual = new Panel
            {
                Width = 32,
                Height = 32,
                Background = new ImmutableSolidColorBrush(baseColor),
                Children =
                {
                    new Path { Data = Geometry.Parse("M 0 8 L 24 32 L 8 32 L 0 24 Z"), Fill = stripeBrush },
                    new Path { Data = Geometry.Parse("M 8 0 L 24 0 L 32 8 L 32 24 Z"), Fill = stripeBrush },
                }
            },
            TileMode = TileMode.Tile,
            Stretch = Stretch.Fill,
            SourceRect = new RelativeRect(0, 0, 32, 32, RelativeUnit.Absolute),
            DestinationRect = new RelativeRect(0, 0, 32, 32, RelativeUnit.Absolute),
        };
    }
}

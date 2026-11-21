using System.Collections.Generic;
using Avalonia.Media;

namespace SS14.Launcher.Models.Themes;

/// <summary>
/// A launcher theme: a set of colors (written over the default resource keys at runtime) plus a few visual flags.
/// </summary>
public sealed class ThemeDefinition
{
    public required string Id { get; init; }

    /// <summary>Localization key for the display name.</summary>
    public required string NameKey { get; init; }

    /// <summary>Plain colors, keyed by resource name without the "Color"/"Brush" suffix (e.g. "ThemeBackground").</summary>
    public required Dictionary<string, Color> Colors { get; init; }

    /// <summary>Optional gradient replacing the flat button background.</summary>
    public (Color From, Color To)? ButtonGradient { get; init; }

    /// <summary>Colors of the diagonal stripe pattern behind the tab strip.</summary>
    public required (Color Base, Color Stripe) TabStripe { get; init; }

    /// <summary>Particle color; null if this theme has no particles.</summary>
    public Color? ParticleColor { get; init; }

    public Color ListBackground { get; init; }
    public Color ListAltRow { get; init; }
    public Color HeaderBackground { get; init; }
    public Color Separator { get; init; }
    public Color ButtonForeground { get; init; } = Colors_White;

    private static readonly Color Colors_White = Color.FromRgb(0xFF, 0xFF, 0xFF);
}

using System.Collections.Generic;
using Avalonia.Media;

namespace SS14.Launcher.Models.Themes;

public static class ThemePalettes
{
    public const string DarkGray = "dark-gray";
    public const string Violet = "violet";
    public const string Light = "light";

    private static Color C(string hex) => Color.Parse(hex);

    public static readonly ThemeDefinition DarkGrayTheme = new()
    {
        Id = DarkGray,
        NameKey = "theme-name-dark-gray",
        Colors = new Dictionary<string, Color>
        {
            ["ThemeBackground"] = C("#232323"),
            ["ThemePopupBackground"] = C("#1C1C1C"),
            ["ThemeForeground"] = C("#EDEDED"),
            ["ThemeForegroundMuted"] = C("#8A9099"),
            ["ThemeControlMid"] = C("#404348"),
            ["ThemeControlHigh"] = C("#4C7A55"),
            ["ThemeNanoGold"] = C("#B3A857"),
            ["ThemeSubText"] = C("#A8A8A8"),
            ["ThemeStripebackEdge"] = C("#4A4A4A"),
            ["ThemeButtonHovered"] = C("#555A61"),
            ["ThemeTabItemSelected"] = C("#CC4C7A55"),
            ["ThemeTabItemHovered"] = C("#CC555A61"),
            ["ThemeListSeparator"] = C("#AA555A61"),
            ["ThemeListSeparatorTransparent"] = C("#00555A61"),
        },
        TabStripe = (C("#1b1b1b"), C("#222222")),
        ListBackground = C("#1b1b1b"),
        ListAltRow = C("#222222"),
        HeaderBackground = C("#1e1e1e"),
        Separator = C("#303030"),
    };

    public static readonly ThemeDefinition VioletTheme = new()
    {
        Id = Violet,
        NameKey = "theme-name-violet",
        Colors = new Dictionary<string, Color>
        {
            ["ThemeBackground"] = C("#0F0B19"),
            ["ThemePopupBackground"] = C("#0B0813"),
            ["ThemeForeground"] = C("#EFEAFB"),
            ["ThemeForegroundMuted"] = C("#9A92B8"),
            ["ThemeControlMid"] = C("#4B2C9A"),
            ["ThemeControlHigh"] = C("#7C3AED"),
            ["ThemeNanoGold"] = C("#A78BFA"),
            ["ThemeSubText"] = C("#A79FC4"),
            ["ThemeStripebackEdge"] = C("#2E2252"),
            ["ThemeButtonHovered"] = C("#6D3FD0"),
            ["ThemeTabItemSelected"] = C("#CC6D3FD0"),
            ["ThemeTabItemHovered"] = C("#AA4B2C9A"),
            ["ThemeListSeparator"] = C("#AA3A2A6E"),
            ["ThemeListSeparatorTransparent"] = C("#003A2A6E"),
        },
        ButtonGradient = (C("#3B1E86"), C("#7C3AED")),
        TabStripe = (C("#0A0713"), C("#120D22")),
        ParticleColor = C("#A78BFA"),
        ListBackground = C("#0C0916"),
        ListAltRow = C("#130E24"),
        HeaderBackground = C("#0A0712"),
        Separator = C("#241A44"),
    };

    public static readonly ThemeDefinition LightTheme = new()
    {
        Id = Light,
        NameKey = "theme-name-light",
        Colors = new Dictionary<string, Color>
        {
            ["ThemeBackground"] = C("#F2F4F8"),
            ["ThemePopupBackground"] = C("#FFFFFF"),
            ["ThemeForeground"] = C("#1C2333"),
            ["ThemeForegroundMuted"] = C("#6F7A92"),
            ["ThemeControlMid"] = C("#3262CC"),
            ["ThemeControlHigh"] = C("#274FAE"),
            ["ThemeNanoGold"] = C("#2F5BC4"),
            ["ThemeSubText"] = C("#55607A"),
            ["ThemeStripebackEdge"] = C("#C9D2E3"),
            ["ThemeButtonHovered"] = C("#4450C6"),
            ["ThemeTabItemSelected"] = C("#553262CC"),
            ["ThemeTabItemHovered"] = C("#333262CC"),
            ["ThemeListSeparator"] = C("#AAC9D2E3"),
            ["ThemeListSeparatorTransparent"] = C("#00C9D2E3"),
        },
        ButtonGradient = (C("#3262CC"), C("#4A57CF")),
        TabStripe = (C("#E6EBF3"), C("#EDF1F7")),
        ListBackground = C("#FFFFFF"),
        ListAltRow = C("#F3F6FB"),
        // The logo and all icons are white, so the header stays dark.
        HeaderBackground = C("#1B2236"),
        Separator = C("#D5DCEA"),
    };

    public static readonly ThemeDefinition[] All = [DarkGrayTheme, VioletTheme, LightTheme];

    public static ThemeDefinition Get(string? id)
    {
        foreach (var t in All)
        {
            if (t.Id == id)
                return t;
        }

        return DarkGrayTheme;
    }
}

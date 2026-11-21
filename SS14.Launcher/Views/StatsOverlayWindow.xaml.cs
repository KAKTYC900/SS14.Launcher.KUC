using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SS14.Launcher.Views;

/// <summary>
/// Small borderless always-on-top window that is placed over the game window.
/// </summary>
public partial class StatsOverlayWindow : Window
{
    public StatsOverlayWindow()
    {
        InitializeComponent();

        // The name of this property differs between Avalonia versions (SystemDecorations / WindowDecorations),
        // so set it by name instead of failing to compile on one of them.
        var type = typeof(Window);
        var prop = type.GetProperty("WindowDecorations") ?? type.GetProperty("SystemDecorations");
        if (prop != null)
            prop.SetValue(this, Enum.Parse(prop.PropertyType, "None"));

        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
    }
}

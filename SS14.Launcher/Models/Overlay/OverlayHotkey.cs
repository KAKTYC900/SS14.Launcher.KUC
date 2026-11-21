using System.Collections.Generic;
using Avalonia.Input;

namespace SS14.Launcher.Models.Overlay;

/// <summary>
/// The key combination that toggles the in-game stats panel: a Windows virtual-key code plus modifiers.
/// </summary>
public static class OverlayHotkey
{
    public const int DefaultVk = 0x55; // U
    public const int DefaultMods = ModShift;

    public const int ModShift = 1;
    public const int ModCtrl = 2;
    public const int ModAlt = 4;

    private static readonly Dictionary<Key, int> Special = new()
    {
        [Key.Tab] = 0x09,
        [Key.Space] = 0x20,
        [Key.PageUp] = 0x21,
        [Key.PageDown] = 0x22,
        [Key.End] = 0x23,
        [Key.Home] = 0x24,
        [Key.Left] = 0x25,
        [Key.Up] = 0x26,
        [Key.Right] = 0x27,
        [Key.Down] = 0x28,
        [Key.Insert] = 0x2D,
        [Key.Delete] = 0x2E,
        [Key.OemSemicolon] = 0xBA,
        [Key.OemPlus] = 0xBB,
        [Key.OemComma] = 0xBC,
        [Key.OemMinus] = 0xBD,
        [Key.OemPeriod] = 0xBE,
        [Key.OemQuestion] = 0xBF,
        [Key.OemTilde] = 0xC0,
        [Key.OemOpenBrackets] = 0xDB,
        [Key.OemPipe] = 0xDC,
        [Key.OemCloseBrackets] = 0xDD,
        [Key.OemQuotes] = 0xDE,
    };

    public static bool IsModifierKey(Key key) => key is
        Key.LeftShift or Key.RightShift or Key.LeftCtrl or Key.RightCtrl or
        Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin;

    /// <summary>Converts an Avalonia key to a Windows virtual-key code. False if the key is not supported.</summary>
    public static bool TryMapKey(Key key, out int vk)
    {
        vk = 0;

        if (key is >= Key.A and <= Key.Z)
            vk = 0x41 + (key - Key.A);
        else if (key is >= Key.D0 and <= Key.D9)
            vk = 0x30 + (key - Key.D0);
        else if (key is >= Key.NumPad0 and <= Key.NumPad9)
            vk = 0x60 + (key - Key.NumPad0);
        else if (key is >= Key.F1 and <= Key.F12)
            vk = 0x70 + (key - Key.F1);
        else if (!Special.TryGetValue(key, out vk))
            return false;

        return true;
    }

    public static int ModsFromAvalonia(KeyModifiers modifiers)
    {
        var mods = 0;
        if ((modifiers & KeyModifiers.Shift) != 0) mods |= ModShift;
        if ((modifiers & KeyModifiers.Control) != 0) mods |= ModCtrl;
        if ((modifiers & KeyModifiers.Alt) != 0) mods |= ModAlt;
        return mods;
    }

    public static string Format(int vk, int mods)
    {
        var text = "";
        if ((mods & ModCtrl) != 0) text += "Ctrl+";
        if ((mods & ModAlt) != 0) text += "Alt+";
        if ((mods & ModShift) != 0) text += "Shift+";
        return text + VkName(vk);
    }

    private static string VkName(int vk)
    {
        if (vk is >= 0x41 and <= 0x5A)
            return ((char)vk).ToString();
        if (vk is >= 0x30 and <= 0x39)
            return ((char)vk).ToString();
        if (vk is >= 0x60 and <= 0x69)
            return "Num" + (vk - 0x60);
        if (vk is >= 0x70 and <= 0x7B)
            return "F" + (vk - 0x70 + 1);

        foreach (var (key, code) in Special)
        {
            if (code == vk)
                return key.ToString().Replace("Oem", "");
        }

        return "0x" + vk.ToString("X");
    }
}

using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using Serilog;
using Splat;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Models.Logins;
using SS14.Launcher.Utility;
using SS14.Launcher.ViewModels;
using SS14.Launcher.Views;

namespace SS14.Launcher.Models.Overlay;

/// <summary>
/// Shows a small stats panel (round, account name, game mode, round time) over the running game window when
/// the user presses Shift+U in the game. Everything happens inside the launcher: it never touches the game or the server.
///
/// How it works (Windows only):
///  * a low-level keyboard hook notices Shift+U, but only while the game window is in front, and does not swallow the key;
///  * the panel is an ordinary always-on-top launcher window positioned over the game window's top-left corner;
///  * the data comes from the server's public /status endpoint, polled every 10 seconds.
/// The panel is visible over windowed and borderless-windowed games, not over exclusive fullscreen.
/// </summary>
public sealed class GameStatsOverlayManager
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x100;
    private const int WmKeyUp = 0x101;
    private const int VkShift = 0x10;
    private const int VkControl = 0x11;
    private const int VkMenu = 0x12;

    private static readonly TimeSpan StatusInterval = TimeSpan.FromSeconds(10);

    private readonly DataManager _cfg;
    private readonly StatsOverlayViewModel _vm = new();

    private int _gamePid;
    private Process? _process;
    private string? _address;
    private string _account = "";

    private IntPtr _hook;
    private HookProc? _hookProc; // Must be kept referenced, or the GC collects the delegate under the hook.
    private volatile int _hotkeyVk = OverlayHotkey.DefaultVk;
    private volatile int _hotkeyMods = OverlayHotkey.DefaultMods;
    private bool _keyDown;

    private DispatcherTimer? _timer;
    private StatsOverlayWindow? _window;
    private IntPtr _gameHwnd;
    private bool _visible;
    private bool _stylesApplied;

    private DateTime _lastFetch = DateTime.MinValue;
    private bool _fetching;
    private string? _roundId;
    private string? _preset;
    private int? _runLevel;
    private DateTime? _roundStart;

    public GameStatsOverlayManager(DataManager cfg)
    {
        _cfg = cfg;
    }

    public static bool IsSupported => OperatingSystem.IsWindows();

    /// <summary>Starts tracking a running game. Does nothing if the feature is off or unsupported.</summary>
    public void Start(Process gameProcess, string address)
    {
        if (!IsSupported || !_cfg.GetCVar(CVars.StatsOverlay))
            return;

        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                StartCore(gameProcess, address);
            }
            catch (Exception e)
            {
                Log.Warning(e, "Failed to start the stats overlay");
                StopCore();
            }
        });
    }

    /// <summary>Stops tracking when the given game process is the one being tracked.</summary>
    public void Stop(Process gameProcess)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_process == gameProcess)
                StopCore();
        });
    }

    private void StartCore(Process process, string address)
    {
        StopCore();

        _process = process;
        _gamePid = process.Id;
        _address = address;
        _account = Locator.Current.GetService<LoginManager>()?.ActiveAccount?.Username ?? "";
        _visible = false;
        _lastFetch = DateTime.MinValue;
        _roundId = _preset = null;
        _runLevel = null;
        _roundStart = null;

        LoadHotkey();

        _hookProc = HookCallback;
        _hook = SetWindowsHookEx(WhKeyboardLl, _hookProc, GetModuleHandle(null), 0);
        if (_hook == IntPtr.Zero)
            Log.Warning("Could not install the keyboard hook for the stats overlay (error {Error})", Marshal.GetLastWin32Error());

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();

        Log.Debug("Stats overlay tracking game process {Pid}", _gamePid);
    }

    private void StopCore()
    {
        _timer?.Stop();
        _timer = null;

        if (_hook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }

        _hookProc = null;

        if (_window != null)
        {
            _window.Close();
            _window = null;
        }

        _stylesApplied = false;
        _visible = false;
        _process = null;
        _gameHwnd = IntPtr.Zero;
    }

    //
    // Keyboard
    //

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= 0)
            {
                var vk = Marshal.ReadInt32(lParam);
                var msg = (int)wParam;

                if (vk == _hotkeyVk && msg == WmKeyUp)
                {
                    _keyDown = false;
                }
                else if (vk == _hotkeyVk && msg == WmKeyDown && !_keyDown)
                {
                    // Auto-repeat sends KeyDown again and again while the key is held; react once per press.
                    _keyDown = true;

                    var mods = 0;
                    if ((GetAsyncKeyState(VkShift) & 0x8000) != 0) mods |= OverlayHotkey.ModShift;
                    if ((GetAsyncKeyState(VkControl) & 0x8000) != 0) mods |= OverlayHotkey.ModCtrl;
                    if ((GetAsyncKeyState(VkMenu) & 0x8000) != 0) mods |= OverlayHotkey.ModAlt;

                    // Modifiers must match exactly, so Shift+U does not also fire for Ctrl+Shift+U.
                    if (mods == _hotkeyMods && IsGameInFront())
                        Dispatcher.UIThread.Post(Toggle);
                }
            }
        }
        catch (Exception)
        {
            // Never let an exception escape from a hook callback.
        }

        // Always pass the key on: the game still receives Shift+U.
        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    private bool IsGameInFront()
    {
        var fg = GetForegroundWindow();
        if (fg == IntPtr.Zero)
            return false;

        GetWindowThreadProcessId(fg, out var pid);
        return pid == (uint)_gamePid;
    }

    /// <summary>Reads the configured key. Called regularly, so a change in the settings applies immediately.</summary>
    private void LoadHotkey()
    {
        _hotkeyVk = _cfg.GetCVar(CVars.StatsOverlayVk);
        _hotkeyMods = _cfg.GetCVar(CVars.StatsOverlayMods);
    }

    private void Toggle()
    {
        _visible = !_visible;
        Tick();
    }

    //
    // Periodic update
    //

    private void Tick()
    {
        if (_process == null)
            return;

        try
        {
            if (_process.HasExited)
            {
                StopCore();
                return;
            }
        }
        catch (InvalidOperationException)
        {
            StopCore();
            return;
        }

        LoadHotkey();

        var fg = GetForegroundWindow();
        GetWindowThreadProcessId(fg, out var fgPid);
        var gameInFront = fg != IntPtr.Zero && fgPid == (uint)_gamePid;
        if (gameInFront)
            _gameHwnd = fg;

        // Only show over the game, never over other programs.
        var shouldShow = _visible && gameInFront && _gameHwnd != IntPtr.Zero;

        if (shouldShow)
        {
            UpdateTexts();
            ShowOverGame();

            if (DateTime.UtcNow - _lastFetch > StatusInterval)
                FetchStatus();
        }
        else
        {
            _window?.Hide();
        }
    }

    private void ShowOverGame()
    {
        _window ??= new StatsOverlayWindow { DataContext = _vm };

        var origin = new POINT { X = 0, Y = 0 };
        if (ClientToScreen(_gameHwnd, ref origin))
            _window.Position = new PixelPoint(origin.X + 12, origin.Y + 12);

        if (!_window.IsVisible)
            _window.Show();

        if (!_stylesApplied && _window.TryGetPlatformHandle() is { } handle)
        {
            // Click-through, never takes focus, not listed in Alt+Tab.
            const int GwlExStyle = -20;
            const int WsExTransparent = 0x20;
            const int WsExToolWindow = 0x80;
            const int WsExNoActivate = 0x08000000;

            var ex = GetWindowLong(handle.Handle, GwlExStyle);
            SetWindowLong(handle.Handle, GwlExStyle, ex | WsExTransparent | WsExToolWindow | WsExNoActivate);
            _stylesApplied = true;
        }
    }

    private void UpdateTexts()
    {
        var loc = LocalizationManager.Instance;
        var unknown = "—";

        _vm.RoundText = loc.GetString("stats-overlay-round", ("id", _roundId ?? unknown));
        _vm.CkeyText = loc.GetString("stats-overlay-ckey",
            ("name", string.IsNullOrEmpty(_account) ? unknown : _account));
        _vm.ModeText = loc.GetString("stats-overlay-mode", ("mode", string.IsNullOrWhiteSpace(_preset) ? unknown : _preset));

        string time;
        if (_runLevel == 1 && _roundStart is { } start)
        {
            var span = DateTime.UtcNow - start.ToUniversalTime();
            if (span < TimeSpan.Zero)
                span = TimeSpan.Zero;

            time = $"{(int)span.TotalHours}:{span.Minutes:00}:{span.Seconds:00}";
        }
        else if (_runLevel is 0 or 2)
        {
            time = loc.GetString("stats-overlay-lobby");
        }
        else
        {
            time = unknown;
        }

        _vm.TimeText = loc.GetString("stats-overlay-time", ("time", time));
    }

    //
    // Server status
    //

    private async void FetchStatus()
    {
        if (_fetching || _address == null)
            return;

        _fetching = true;
        _lastFetch = DateTime.UtcNow;
        try
        {
            var http = Locator.Current.GetRequiredService<HttpClient>();
            var uri = UriHelper.GetServerStatusAddress(_address);

            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
            var json = await http.GetStringAsync(uri, cts.Token);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Parsed by hand so that unexpected field types on custom servers cannot break anything else.
            _roundId = ReadString(root, "round_id");
            _preset = ReadString(root, "preset");
            _runLevel = root.TryGetProperty("run_level", out var rl) && rl.ValueKind == JsonValueKind.Number
                ? rl.GetInt32()
                : null;

            _roundStart = ReadString(root, "round_start_time") is { } s
                && DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt)
                    ? dt
                    : null;
        }
        catch (Exception e)
        {
            Log.Debug(e, "Stats overlay: could not fetch server status");
        }
        finally
        {
            _fetching = false;
        }
    }

    private static string? ReadString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el))
            return null;

        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.GetRawText(),
            _ => null,
        };
    }

    //
    // Win32
    //

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using Serilog;
using Splat;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Models.ServerStatus;
using SS14.Launcher.Utility;

namespace SS14.Launcher.Models.Notifications;

/// <summary>
/// Periodically checks the servers the user watches and notifies about round starts,
/// player count thresholds and the server going online/offline.
/// Works as long as the launcher is running (also while a game is running or the window is minimized).
/// </summary>
public sealed class ServerWatcher
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(15);

    private readonly DataManager _cfg;
    private readonly Dictionary<WatchedServer, WatchState> _states = new();
    // Not a field initializer: the localization manager is registered after this class is created in Program.Main.
    private static LocalizationManager _loc => LocalizationManager.Instance;

    private HttpClient? _http;
    private DispatcherTimer? _timer;
    private bool _checking;
    private bool _loading;

    private WindowNotificationManager? _toastManager;
    private Window? _toastWindow;

    public ObservableCollection<WatchedServer> Servers { get; } = new();

    public ServerWatcher(DataManager cfg)
    {
        _cfg = cfg;
        Load();
    }

    public bool Enabled
    {
        get => _cfg.GetCVar(CVars.NotificationsEnabled);
        set
        {
            _cfg.SetCVar(CVars.NotificationsEnabled, value);
            _cfg.CommitConfig();
        }
    }

    public bool SystemNotifications
    {
        get => _cfg.GetCVar(CVars.NotificationsSystem);
        set
        {
            _cfg.SetCVar(CVars.NotificationsSystem, value);
            _cfg.CommitConfig();
        }
    }

    /// <summary>Starts polling. Call once after Avalonia has started.</summary>
    public void Initialize()
    {
        _http = Locator.Current.GetRequiredService<HttpClient>();

        _timer = new DispatcherTimer { Interval = PollInterval };
        _timer.Tick += (_, _) => CheckAll();
        _timer.Start();

        // First check shortly after startup. It only sets the baseline, nothing is announced for it.
        DispatcherTimer.RunOnce(CheckAll, TimeSpan.FromSeconds(3));
    }

    //
    // List management
    //

    public enum AddResult
    {
        Ok,
        Invalid,
        Duplicate
    }

    public AddResult Add(string address)
    {
        address = address.Trim();
        if (address.Length == 0 || !UriHelper.TryParseSs14Uri(address, out _))
            return AddResult.Invalid;

        var key = DataManager.NormalizeServerAddress(address);
        if (Servers.Any(s => DataManager.NormalizeServerAddress(s.Address) == key))
            return AddResult.Duplicate;

        var server = new WatchedServer(address);
        Hook(server);
        Servers.Add(server);
        Save();

        CheckOne(server);
        return AddResult.Ok;
    }

    public void Remove(WatchedServer server)
    {
        Servers.Remove(server);
        _states.Remove(server);
        Save();
    }

    public void CheckNow() => CheckAll();

    public void SendTest()
    {
        Notify(_loc.GetString("notifications-test-title"), _loc.GetString("notifications-test-body"), force: true);
    }

    private void Hook(WatchedServer server)
    {
        server.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(WatchedServer.NotifyRoundStart)
                or nameof(WatchedServer.NotifyOnlineChange)
                or nameof(WatchedServer.PlayerThreshold))
            {
                Save();
            }
        };
    }

    //
    // Saving / loading
    //

    private void Load()
    {
        _loading = true;
        try
        {
            var json = _cfg.GetCVar(CVars.WatchedServers);
            var list = JsonSerializer.Deserialize<List<WatchedServerDto>>(json) ?? new();
            foreach (var dto in list)
            {
                var server = new WatchedServer(dto.Address)
                {
                    NotifyRoundStart = dto.RoundStart,
                    NotifyOnlineChange = dto.OnlineChange,
                    PlayerThreshold = dto.Threshold,
                };
                Hook(server);
                Servers.Add(server);
            }
        }
        catch (Exception e)
        {
            Log.Warning(e, "Failed to load watched servers, starting with an empty list");
        }
        finally
        {
            _loading = false;
        }
    }

    private void Save()
    {
        if (_loading)
            return;

        var dtos = Servers.Select(s =>
            new WatchedServerDto(s.Address, s.NotifyRoundStart, s.NotifyOnlineChange, (int)s.PlayerThreshold));
        _cfg.SetCVar(CVars.WatchedServers, JsonSerializer.Serialize(dtos));
        _cfg.CommitConfig();
    }

    //
    // Polling
    //

    private async void CheckAll()
    {
        if (_checking || !Enabled || Servers.Count == 0 || _http == null)
            return;

        _checking = true;
        try
        {
            await Task.WhenAll(Servers.ToArray().Select(CheckOneAsync));
        }
        catch (Exception e)
        {
            Log.Warning(e, "Server check failed");
        }
        finally
        {
            _checking = false;
        }
    }

    private async void CheckOne(WatchedServer server)
    {
        if (_http == null)
            return;

        try
        {
            await CheckOneAsync(server);
        }
        catch (Exception e)
        {
            Log.Warning(e, "Server check failed");
        }
    }

    private async Task CheckOneAsync(WatchedServer server)
    {
        if (_http == null)
            return;

        if (!_states.TryGetValue(server, out var state))
            _states[server] = state = new WatchState(new ServerStatusData(server.Address));

        using var cts = new CancellationTokenSource(CheckTimeout);
        await ServerStatusCache.UpdateStatusFor(state.Data, _http, cts.Token);

        // The server may have been removed while the request was running.
        if (!Servers.Contains(server))
            return;

        Evaluate(server, state);
    }

    private void Evaluate(WatchedServer server, WatchState state)
    {
        var data = state.Data;
        var online = data.Status == ServerStatusCode.Online;

        if (!string.IsNullOrWhiteSpace(data.Name))
            server.ServerName = data.Name;

        server.StatusText = online
            ? _loc.GetString("notifications-status-online", ("players", data.PlayerCount))
            : _loc.GetString("notifications-status-offline");

        var round = online ? data.RoundStatus : state.PrevRound;
        var players = online ? data.PlayerCount : state.PrevPlayers;

        if (state.HasBaseline)
        {
            var name = server.DisplayName;

            if (server.NotifyOnlineChange && online != state.PrevOnline)
            {
                Notify(name, _loc.GetString(online ? "notifications-server-online" : "notifications-server-offline"));
            }

            if (online && state.PrevOnline)
            {
                if (server.NotifyRoundStart
                    && state.PrevRound != GameRoundStatus.InRound
                    && round == GameRoundStatus.InRound)
                {
                    Notify(name, _loc.GetString("notifications-round-started"));
                }

                var threshold = (int)server.PlayerThreshold;
                if (threshold > 0 && state.PrevPlayers < threshold && players >= threshold)
                {
                    Notify(name, _loc.GetString("notifications-players-reached",
                        ("count", players), ("threshold", threshold)));
                }
            }
        }

        state.HasBaseline = true;
        state.PrevOnline = online;
        state.PrevRound = round;
        state.PrevPlayers = players;
    }

    //
    // Showing notifications
    //

    private void Notify(string title, string text, bool force = false)
    {
        if (!force && !Enabled)
            return;

        var window = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        // The launcher is in front: an in-app toast is enough. Otherwise use the system notification.
        if (window is { IsActive: true } || !SystemNotifications)
            ShowToast(window, title, text);
        else
            DesktopNotifier.Show(title, text);
    }

    private void ShowToast(Window? window, string title, string text)
    {
        if (window == null)
            return;

        if (_toastManager == null || _toastWindow != window)
        {
            _toastManager = new WindowNotificationManager(window)
            {
                Position = NotificationPosition.TopRight,
                MaxItems = 3,
            };
            _toastWindow = window;
        }

        _toastManager.Show(new Notification(title, text, NotificationType.Information, TimeSpan.FromSeconds(8)));
    }

    private sealed class WatchState(ServerStatusData data)
    {
        public readonly ServerStatusData Data = data;
        public bool HasBaseline;
        public bool PrevOnline;
        public GameRoundStatus PrevRound;
        public int PrevPlayers;
    }
}

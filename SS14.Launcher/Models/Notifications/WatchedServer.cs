using System.Text.Json.Serialization;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace SS14.Launcher.Models.Notifications;

/// <summary>
/// A server the user wants to be notified about, with what to notify on.
/// </summary>
public sealed partial class WatchedServer : ObservableObject
{
    public WatchedServer(string address)
    {
        Address = address;
    }

    public string Address { get; }

    /// <summary>Notify when a new round starts.</summary>
    [ObservableProperty] private bool _notifyRoundStart = true;

    /// <summary>Notify when the server goes online or offline.</summary>
    [ObservableProperty] private bool _notifyOnlineChange;

    /// <summary>Notify when the player count reaches this number. 0 turns it off.</summary>
    [ObservableProperty] private decimal _playerThreshold;

    /// <summary>Name the server reports, once known.</summary>
    [ObservableProperty] private string? _serverName;

    /// <summary>Short description of the last check ("Online, 12 players", ...). Not saved.</summary>
    [ObservableProperty] private string _statusText = "";

    public string DisplayName => string.IsNullOrWhiteSpace(ServerName) ? Address : ServerName;

    partial void OnServerNameChanged(string? value) => OnPropertyChanged(nameof(DisplayName));
}

/// <summary>What gets stored in the config.</summary>
public sealed record WatchedServerDto(
    [property: JsonPropertyName("address")] string Address,
    [property: JsonPropertyName("round_start")] bool RoundStart,
    [property: JsonPropertyName("online_change")] bool OnlineChange,
    [property: JsonPropertyName("threshold")] int Threshold);

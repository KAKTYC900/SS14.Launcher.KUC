using System;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.ServerStatus;

namespace SS14.Launcher.ViewModels.MainWindowTabs;

/// <summary>
/// One draggable card in the Favorites tab.
/// </summary>
public sealed class FavoriteCardViewModel : ObservableObject
{
    public const double Width = 250;
    public const double Height = 176;

    private readonly Action<FavoriteCardViewModel> _moved;
    private double _x;
    private double _y;
    private string _pingText = "—";
    private bool _pinging;

    public FavoriteCardViewModel(ServerEntryViewModel entry, Action<FavoriteCardViewModel> moved)
    {
        Entry = entry;
        _moved = moved;
    }

    public ServerEntryViewModel Entry { get; }

    public string Address => Entry.CacheData.Address;

    public bool HasPosition { get; private set; }

    public double X
    {
        get => _x;
        set => SetProperty(ref _x, value);
    }

    public double Y
    {
        get => _y;
        set => SetProperty(ref _y, value);
    }

    public string PingText
    {
        get => _pingText;
        private set => SetProperty(ref _pingText, value);
    }

    public void SetPosition(double x, double y)
    {
        X = x;
        Y = y;
        HasPosition = true;
    }

    public void ClearPosition() => HasPosition = false;

    /// <summary>Called by the view when the user finished dragging the card.</summary>
    public void NotifyDragFinished()
    {
        HasPosition = true;
        _moved(this);
    }

    public async void UpdatePing()
    {
        if (_pinging)
            return;

        _pinging = true;
        try
        {
            var ms = await ServerPing.MeasureAsync(Address);
            PingText = ms is { } v
                ? LocalizationManager.Instance.GetString("favorites-ping-value", ("ms", v))
                : "—";
        }
        finally
        {
            _pinging = false;
        }
    }
}

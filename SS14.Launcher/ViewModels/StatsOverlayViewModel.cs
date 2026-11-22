using Avalonia.Media;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace SS14.Launcher.ViewModels;

/// <summary>Texts shown in the in-game stats panel.</summary>
public sealed partial class StatsOverlayViewModel : ObservableObject
{
    [ObservableProperty] private string _roundText = "";
    [ObservableProperty] private string _ckeyText = "";
    [ObservableProperty] private string _modeText = "";
    [ObservableProperty] private string _timeText = "";

    [ObservableProperty] private bool _showPlayers;
    [ObservableProperty] private string _playersText = "";

    [ObservableProperty] private bool _showMap;
    [ObservableProperty] private string _mapText = "";

    [ObservableProperty] private bool _showPanic;
    [ObservableProperty] private string _panicText = "";

    [ObservableProperty] private bool _showPing;
    [ObservableProperty] private string _pingText = "";
    [ObservableProperty] private IBrush? _pingBrush;
}

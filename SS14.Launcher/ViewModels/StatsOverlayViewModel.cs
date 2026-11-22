using Avalonia.Media;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace SS14.Launcher.ViewModels;

/// <summary>Texts shown in the in-game stats panel.</summary>
/// <remarks>
/// Written with plain properties (no source generator), so it also builds when the generator output is stale.
/// </remarks>
public sealed partial class StatsOverlayViewModel : ObservableObject
{
    private string _roundText = "";

    public string RoundText
    {
        get => _roundText;
        set => SetProperty(ref _roundText, value);
    }

    private string _ckeyText = "";

    public string CkeyText
    {
        get => _ckeyText;
        set => SetProperty(ref _ckeyText, value);
    }

    private string _modeText = "";

    public string ModeText
    {
        get => _modeText;
        set => SetProperty(ref _modeText, value);
    }

    private string _timeText = "";

    public string TimeText
    {
        get => _timeText;
        set => SetProperty(ref _timeText, value);
    }

    private bool _showPlayers = false;

    public bool ShowPlayers
    {
        get => _showPlayers;
        set => SetProperty(ref _showPlayers, value);
    }

    private string _playersText = "";

    public string PlayersText
    {
        get => _playersText;
        set => SetProperty(ref _playersText, value);
    }

    private bool _showMap = false;

    public bool ShowMap
    {
        get => _showMap;
        set => SetProperty(ref _showMap, value);
    }

    private string _mapText = "";

    public string MapText
    {
        get => _mapText;
        set => SetProperty(ref _mapText, value);
    }

    private bool _showPanic = false;

    public bool ShowPanic
    {
        get => _showPanic;
        set => SetProperty(ref _showPanic, value);
    }

    private string _panicText = "";

    public string PanicText
    {
        get => _panicText;
        set => SetProperty(ref _panicText, value);
    }

    private bool _showPing = false;

    public bool ShowPing
    {
        get => _showPing;
        set => SetProperty(ref _showPing, value);
    }

    private string _pingText = "";

    public string PingText
    {
        get => _pingText;
        set => SetProperty(ref _pingText, value);
    }

    private IBrush? _pingBrush = null;

    public IBrush? PingBrush
    {
        get => _pingBrush;
        set => SetProperty(ref _pingBrush, value);
    }
}

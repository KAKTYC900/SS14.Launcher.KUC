using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace SS14.Launcher.ViewModels;

/// <summary>Texts shown in the in-game stats panel.</summary>
public sealed partial class StatsOverlayViewModel : ObservableObject
{
    [ObservableProperty] private string _roundText = "";
    [ObservableProperty] private string _ckeyText = "";
    [ObservableProperty] private string _modeText = "";
    [ObservableProperty] private string _timeText = "";
}

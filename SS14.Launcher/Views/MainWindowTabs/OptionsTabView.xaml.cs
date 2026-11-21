using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Splat;
using SS14.Launcher.Localization;
using SS14.Launcher.Utility;
using SS14.Launcher.ViewModels.MainWindowTabs;

namespace SS14.Launcher.Views.MainWindowTabs;

public partial class OptionsTabView : UserControl
{
    public OptionsTabView()
    {
        InitializeComponent();

        // Tunnel: see the key before the button itself reacts to Space/Enter.
        StatsKeyButton.AddHandler(KeyDownEvent, OnStatsKeyDown, RoutingStrategies.Tunnel);
    }

    private void OnStatsKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is OptionsTabViewModel { IsCapturingKey: true } vm && vm.TryCaptureKey(e.Key, e.KeyModifiers))
            e.Handled = true;
    }

    private void StatsKeyLostFocus(object? sender, RoutedEventArgs e)
    {
        (DataContext as OptionsTabViewModel)?.CancelKeyCapture();
    }

    private void Flip(object? o, RoutedEventArgs routedEventArgs)
    {
        var window = (Window?) VisualRoot;
        if (window == null)
            return;

        window.Classes.Add("DoAFlip");

        DispatcherTimer.RunOnce(() => { window.Classes.Remove("DoAFlip"); }, TimeSpan.FromSeconds(1));
    }

    public async void ClearEnginesPressed(object? _1, RoutedEventArgs _2)
    {
        ((OptionsTabViewModel)DataContext!).ClearEngines();
        await ClearEnginesButton.DisplayDoneMessage();
    }

    public async void ClearServerContentPressed(object? _1, RoutedEventArgs _2)
    {
        var blocked = !await ((OptionsTabViewModel)DataContext!).ClearServerContent();
        var locMgr = Locator.Current.GetService<LocalizationManager>()!;

        await ClearServerContentButton.DisplayDoneMessage(
            blocked ? locMgr.GetString("tab-options-clear-content-close-client") : null);
    }

    private async void OpenHubSettings(object? sender, RoutedEventArgs args)
    {
        await new HubSettingsDialog().ShowDialog((Window)TopLevel.GetTopLevel(this)!);
    }
}

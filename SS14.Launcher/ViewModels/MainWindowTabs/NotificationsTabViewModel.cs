using System.Collections.ObjectModel;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Splat;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.Notifications;
using SS14.Launcher.Utility;

namespace SS14.Launcher.ViewModels.MainWindowTabs;

/// <summary>
/// "Notifications" tab: the user's own list of servers to watch, and what to be notified about.
/// The actual checking is done by <see cref="ServerWatcher"/>, which runs in the background.
/// </summary>
public sealed partial class NotificationsTabViewModel : MainWindowTabViewModel
{
    private readonly ServerWatcher _watcher;

    public ObservableCollection<WatchedServer> Servers => _watcher.Servers;

    [ObservableProperty] private string _newAddress = "";
    [ObservableProperty] private string _addError = "";

    public override string Name => LocalizationManager.Instance.GetString("tab-notifications-title");

    public NotificationsTabViewModel()
    {
        _watcher = Locator.Current.GetRequiredService<ServerWatcher>();
    }

    public bool NotificationsEnabled
    {
        get => _watcher.Enabled;
        set
        {
            _watcher.Enabled = value;
            OnPropertyChanged();
        }
    }

    public bool SystemNotifications
    {
        get => _watcher.SystemNotifications;
        set
        {
            _watcher.SystemNotifications = value;
            OnPropertyChanged();
        }
    }

    public bool IsEmpty => Servers.Count == 0;

    public override void Selected()
    {
        // Refresh the status line of every server when the tab is opened.
        _watcher.CheckNow();
        OnPropertyChanged(nameof(IsEmpty));
    }

    public void AddPressed()
    {
        var loc = LocalizationManager.Instance;
        switch (_watcher.Add(NewAddress))
        {
            case ServerWatcher.AddResult.Ok:
                NewAddress = "";
                AddError = "";
                break;
            case ServerWatcher.AddResult.Duplicate:
                AddError = loc.GetString("tab-notifications-error-duplicate");
                break;
            default:
                AddError = loc.GetString("tab-notifications-error-invalid");
                break;
        }

        OnPropertyChanged(nameof(IsEmpty));
    }

    public void RemovePressed(WatchedServer server)
    {
        _watcher.Remove(server);
        OnPropertyChanged(nameof(IsEmpty));
    }

    public void TestPressed()
    {
        _watcher.SendTest();
    }
}

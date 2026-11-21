using Avalonia.Controls;
using Avalonia.Interactivity;
using SS14.Launcher.Models.Notifications;
using SS14.Launcher.ViewModels.MainWindowTabs;

namespace SS14.Launcher.Views.MainWindowTabs;

public partial class NotificationsTabView : UserControl
{
    public NotificationsTabView()
    {
        InitializeComponent();
    }

    private void RemoveClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is NotificationsTabViewModel vm
            && (sender as Control)?.DataContext is WatchedServer server)
        {
            vm.RemovePressed(server);
        }
    }
}

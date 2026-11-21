using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Messaging;
using Splat;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Utility;

namespace SS14.Launcher.ViewModels.MainWindowTabs;

/// <summary>
/// "History" tab: servers the user played on, newest first, with a button to connect again.
/// </summary>
public sealed partial class HistoryTabViewModel : MainWindowTabViewModel
{
    private readonly DataManager _cfg;
    private readonly MainWindowViewModel _windowVm;

    public ObservableCollection<HistoryItemViewModel> Items { get; } = new();

    [ObservableProperty] private bool _isEmpty = true;

    public override string Name => LocalizationManager.Instance.GetString("tab-history-title");

    public HistoryTabViewModel(MainWindowViewModel windowVm)
    {
        _windowVm = windowVm;
        _cfg = Locator.Current.GetRequiredService<DataManager>();

        WeakReferenceMessenger.Default.Register<HistoryTabViewModel, ConnectionHistoryChanged>(
            this, (r, _) => r.Reload());

        Reload();
    }

    public override void Selected()
    {
        // Refresh relative things (e.g. a session that ended while the tab was hidden).
        Reload();
    }

    private void Reload()
    {
        Items.Clear();
        foreach (var entry in _cfg.ConnectionHistory.Reverse())
            Items.Add(new HistoryItemViewModel(_windowVm, entry));

        IsEmpty = Items.Count == 0;
    }

    public void ClearPressed()
    {
        _cfg.ClearConnectionHistory();
        _cfg.CommitConfig();
    }
}

public sealed class HistoryItemViewModel
{
    private readonly MainWindowViewModel _windowVm;
    private readonly ConnectionHistoryEntry _entry;

    public HistoryItemViewModel(MainWindowViewModel windowVm, ConnectionHistoryEntry entry)
    {
        _windowVm = windowVm;
        _entry = entry;
    }

    public string Title => string.IsNullOrWhiteSpace(_entry.Name) ? _entry.Address : _entry.Name;

    /// <summary>Address line; empty when the title already is the address.</summary>
    public string Address => string.IsNullOrWhiteSpace(_entry.Name) ? "" : _entry.Address;

    public string StartText => _entry.Start.ToLocalTime().ToString("g");

    public string DurationText
    {
        get
        {
            if (_entry.End is not { } end)
                return "—";

            var span = end - _entry.Start;
            if (span < TimeSpan.Zero)
                return "—";

            var loc = LocalizationManager.Instance;
            if (span.TotalHours >= 1)
            {
                return loc.GetString("history-duration-hours",
                    ("hours", (int)span.TotalHours), ("mins", span.Minutes));
            }

            return loc.GetString("history-duration-mins", ("mins", Math.Max(1, (int)span.TotalMinutes)));
        }
    }

    public void ConnectPressed()
    {
        ConnectingViewModel.StartConnect(_windowVm, _entry.Address);
    }
}

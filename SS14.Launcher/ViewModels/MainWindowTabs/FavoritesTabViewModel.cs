using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using DynamicData;
using DynamicData.Alias;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Splat;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Models.ServerStatus;
using SS14.Launcher.Utility;
using SS14.Launcher.Views;

namespace SS14.Launcher.ViewModels.MainWindowTabs;

/// <summary>
/// "Favorites" tab: the favorite servers shown as freely movable cards (game mode, ping, players, round time, Connect).
/// The server list is the same as the favorites on the Home tab.
/// </summary>
public partial class FavoritesTabViewModel : MainWindowTabViewModel
{
    private const double Gap = 12;
    private const int Columns = 3;
    private const double SnapGrid = 4;

    private readonly DataManager _cfg;
    private readonly ServerStatusCache _statusCache = new();
    private readonly Dictionary<string, (double X, double Y)> _layout = new();
    private readonly DispatcherTimer _refreshTimer = new() { Interval = TimeSpan.FromSeconds(20) };

    public MainWindowViewModel MainWindowViewModel { get; }
    public Control? Control { get; set; }

    // Assigned as soon as the collection exists: the Subscribe callback below already runs for the initial items.
    private ReadOnlyObservableCollection<FavoriteCardViewModel> _cards = null!;
    public ReadOnlyObservableCollection<FavoriteCardViewModel> Cards => _cards;

    [ObservableProperty] private bool _cardsEmpty = true;
    [ObservableProperty] private double _canvasWidth;
    [ObservableProperty] private double _canvasHeight;

    public override string Name => LocalizationManager.Instance.GetString("tab-favorites-title");

    public FavoritesTabViewModel(MainWindowViewModel mainWindowViewModel)
    {
        MainWindowViewModel = mainWindowViewModel;
        _cfg = Locator.Current.GetRequiredService<DataManager>();

        LoadLayout();

        _cfg.FavoriteServers
            .Connect()
            .Select(x =>
            {
                var entry = new ServerEntryViewModel(MainWindowViewModel, _statusCache.GetStatusFor(x.Address), x,
                    _statusCache, _cfg) { ViewedInFavoritesPane = true };
                var card = new FavoriteCardViewModel(entry, OnCardMoved);
                if (_layout.TryGetValue(x.Address, out var pos))
                    card.SetPosition(pos.X, pos.Y);
                return card;
            })
            .OnItemAdded(card =>
            {
                if (IsSelected)
                {
                    _statusCache.InitialUpdateStatus(card.Entry.CacheData);
                    card.UpdatePing();
                }
            })
            .OnItemRemoved(card => _statusCache.Remove(card.Address))
            .SortAndBind(out var cards, Comparer<FavoriteCardViewModel>.Create((a, b) =>
            {
                var dc = a.Entry.Favorite!.RaiseTime.CompareTo(b.Entry.Favorite!.RaiseTime);
                if (dc != 0)
                    return -dc;

                return string.Compare(a.Entry.Name, b.Entry.Name, StringComparison.CurrentCultureIgnoreCase);
            }))
            .Subscribe(_ =>
            {
                _cards = cards;
                CardsEmpty = cards.Count == 0;
                PlaceDefaultCards();
            });

        _cards = cards;

        _refreshTimer.Tick += (_, _) =>
        {
            if (!IsSelected)
                return;

            _statusCache.Refresh();
            PingAll();
        };
        _refreshTimer.Start();
    }

    public override void Selected()
    {
        foreach (var card in Cards)
            _statusCache.InitialUpdateStatus(card.Entry.CacheData);

        PingAll();
    }

    private void PingAll()
    {
        foreach (var card in Cards)
            card.UpdatePing();
    }

    public void RefreshPressed()
    {
        _statusCache.Refresh();
        PingAll();
    }

    public async void AddFavoritePressed()
    {
        if (!TryGetWindow(out var window))
            return;

        var dialog = new AddFavoriteDialog(
            null,
            null,
            address => _cfg.FavoriteServers.Lookup(address).HasValue,
            editing: false);

        if (await dialog.ShowDialog<(string name, string address)?>(window) is not { } result)
            return;

        try
        {
            _cfg.AddFavoriteServer(new FavoriteServer(string.IsNullOrEmpty(result.name) ? null : result.name, result.address));
            _cfg.CommitConfig();
        }
        catch (ArgumentException)
        {
            // The dialog doesn't allow addresses that are already favorites.
        }
    }

    public void ResetLayoutPressed()
    {
        _layout.Clear();
        foreach (var card in Cards)
            card.ClearPosition();

        PlaceDefaultCards();
        SaveLayout();
    }

    private bool TryGetWindow([NotNullWhen(true)] out Window? window)
    {
        window = (Control is null ? null : TopLevel.GetTopLevel(Control)) as Window;
        return window != null;
    }

    //
    // Layout
    //

    /// <summary>Cards the user hasn't placed yet go into a simple grid, in list order.</summary>
    private void PlaceDefaultCards()
    {
        var i = 0;
        foreach (var card in Cards)
        {
            if (!card.HasPosition)
            {
                var col = i % Columns;
                var row = i / Columns;
                card.SetPosition(
                    Gap + col * (FavoriteCardViewModel.Width + Gap),
                    Gap + row * (FavoriteCardViewModel.Height + Gap));
                // Default positions are not user choices; don't treat them as saved.
                card.ClearPosition();
            }

            i++;
        }

        UpdateCanvasSize();
    }

    private void UpdateCanvasSize()
    {
        var w = 0.0;
        var h = 0.0;
        foreach (var card in Cards)
        {
            w = Math.Max(w, card.X + FavoriteCardViewModel.Width + Gap);
            h = Math.Max(h, card.Y + FavoriteCardViewModel.Height + Gap);
        }

        CanvasWidth = w;
        CanvasHeight = h;
    }

    private void OnCardMoved(FavoriteCardViewModel card)
    {
        // Snap to a small grid so cards line up nicely.
        card.X = Math.Max(0, Math.Round(card.X / SnapGrid) * SnapGrid);
        card.Y = Math.Max(0, Math.Round(card.Y / SnapGrid) * SnapGrid);

        _layout[card.Address] = (card.X, card.Y);
        UpdateCanvasSize();
        SaveLayout();
    }

    private void LoadLayout()
    {
        foreach (var line in _cfg.GetCVar(CVars.FavoriteCardLayout).Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('\t');
            if (parts.Length != 3)
                continue;

            if (double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
                && double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
            {
                _layout[parts[0]] = (x, y);
            }
        }
    }

    private void SaveLayout()
    {
        var lines = _layout.Select(kv => string.Create(CultureInfo.InvariantCulture, $"{kv.Key}\t{kv.Value.X}\t{kv.Value.Y}"));
        _cfg.SetCVar(CVars.FavoriteCardLayout, string.Join('\n', lines));
        _cfg.CommitConfig();
    }
}

using System;
using Avalonia.Controls;
using SS14.Launcher.ViewModels.MainWindowTabs;

namespace SS14.Launcher.Views.MainWindowTabs;

public partial class FavoritesTabView : UserControl
{
    private FavoritesTabViewModel? _viewModel;

    public FavoritesTabView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (_viewModel != null)
            _viewModel.Control = null;

        _viewModel = DataContext as FavoritesTabViewModel;

        if (_viewModel != null)
            _viewModel.Control = this;

        base.OnDataContextChanged(e);
    }
}

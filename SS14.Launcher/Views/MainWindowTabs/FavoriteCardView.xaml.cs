using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.VisualTree;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using SS14.Launcher.ViewModels.MainWindowTabs;

namespace SS14.Launcher.Views.MainWindowTabs;

/// <summary>
/// A card that can be dragged freely inside its parent <see cref="Canvas"/>.
/// </summary>
public partial class FavoriteCardView : UserControl
{
    // Shared counter so the card that was grabbed last is drawn on top of the others.
    private static int _nextZ = 1;

    private Canvas? _canvas;
    private Point _grabOffset;
    private bool _dragging;

    public FavoriteCardView()
    {
        InitializeComponent();

        Root.PointerPressed += OnPointerPressed;
        Root.PointerMoved += OnPointerMoved;
        Root.PointerReleased += OnPointerReleased;
        Root.PointerCaptureLost += OnPointerCaptureLost;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Lets the server entry react to status updates (player count, round time, ...).
        if (DataContext is FavoriteCardViewModel vm && vm.Entry is ObservableRecipient r)
            r.IsActive = true;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not FavoriteCardViewModel vm)
            return;

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        // Don't start a drag when the user pressed one of the buttons.
        if (e.Source is Visual source && source.FindAncestorOfType<Button>(includeSelf: true) != null)
            return;

        _canvas = this.FindAncestorOfType<Canvas>();
        if (_canvas == null)
            return;

        var pos = e.GetPosition(_canvas);
        _grabOffset = new Point(pos.X - vm.X, pos.Y - vm.Y);
        _dragging = true;

        if (this.FindAncestorOfType<ContentPresenter>() is { } container)
            container.ZIndex = _nextZ++;

        e.Pointer.Capture(Root);
        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging || _canvas == null || DataContext is not FavoriteCardViewModel vm)
            return;

        var pos = e.GetPosition(_canvas);
        vm.X = System.Math.Max(0, pos.X - _grabOffset.X);
        vm.Y = System.Math.Max(0, pos.Y - _grabOffset.Y);
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_dragging)
            return;

        e.Pointer.Capture(null);
        FinishDrag();
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        FinishDrag();
    }

    private void FinishDrag()
    {
        if (!_dragging)
            return;

        _dragging = false;
        if (DataContext is FavoriteCardViewModel vm)
            vm.NotifyDragFinished();
    }
}

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using UGSGit.PluginAbstractions;
using SourceGit.ViewModels;

namespace SourceGit.Views;

public partial class CommitActionProgress : ChromelessWindow
{
    public CommitActionProgress()
    {
        CloseOnESC = true;
        InitializeComponent();
    }

    public void SetContributorIcon(string? iconResourceKey)
    {
        if (string.IsNullOrEmpty(iconResourceKey))
            return;

        if (this.TryFindResource(iconResourceKey, out var icon) &&
            icon is Avalonia.Media.StreamGeometry geometry)
        {
            TitleBarIcon.Data = geometry;
        }
    }

    public void SetAction(
        Func<CommitRef, IProgress<string>?, CancellationToken, Task> action,
        CommitRef commitRef)
    {
        _action = action;
        _commitRef = commitRef;
    }

    // ---- Lifecycle -------------------------------------------------------

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is not ViewModels.CommitActionProgress vm)
            return;

        vm.LogChanged += OnLogChanged;

        try
        {
            await _action(_commitRef, vm.LogProgress, vm.CancellationToken);
            vm.MarkComplete();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            vm.MarkError(ex.Message);
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        if (DataContext is ViewModels.CommitActionProgress vm)
            vm.OnWindowClosing();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        if (DataContext is ViewModels.CommitActionProgress vm)
            vm.LogChanged -= OnLogChanged;
    }

    // ---- Auto-scroll -----------------------------------------------------

    private void OnLogChanged()
    {
        LogScrollViewer.ScrollToEnd();
    }

    // ---- Button handlers -------------------------------------------------

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.CommitActionProgress vm)
            vm.Cancel();
        e.Handled = true;
    }

    private void OnDoneClicked(object? sender, RoutedEventArgs e)
    {
        Close();
        e.Handled = true;
    }

    private Func<CommitRef, IProgress<string>?, CancellationToken, Task> _action = (_, _, _) => Task.CompletedTask;
    private CommitRef _commitRef = new(string.Empty);
}

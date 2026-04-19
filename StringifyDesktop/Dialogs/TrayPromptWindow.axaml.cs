using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AvaloniaButton = Avalonia.Controls.Button;
using AvaloniaKeyEventArgs = Avalonia.Input.KeyEventArgs;
using AvaloniaUserControl = Avalonia.Controls.UserControl;

namespace StringifyDesktop.Dialogs;

public enum TrayPromptResult
{
    Cancel,
    MinimizeToTray,
    Quit
}

public partial class TrayPromptWindow : AvaloniaUserControl
{
    private readonly AvaloniaButton? cancelButton;
    private TaskCompletionSource<TrayPromptResult>? pendingResult;

    public TrayPromptWindow()
    {
        InitializeComponent();
        cancelButton = this.FindControl<AvaloniaButton>("CancelButton");
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
    }

    public Task<TrayPromptResult> ShowAsync()
    {
        if (pendingResult is not null)
        {
            return pendingResult.Task;
        }

        pendingResult = new TaskCompletionSource<TrayPromptResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        IsVisible = true;
        Dispatcher.UIThread.Post(() => cancelButton?.Focus(), DispatcherPriority.Input);
        return pendingResult.Task;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        Complete(TrayPromptResult.Cancel);
    }

    private void OnMinimizeClicked(object? sender, RoutedEventArgs e)
    {
        Complete(TrayPromptResult.MinimizeToTray);
    }

    private void OnQuitClicked(object? sender, RoutedEventArgs e)
    {
        Complete(TrayPromptResult.Quit);
    }

    private void OnPreviewKeyDown(object? sender, AvaloniaKeyEventArgs e)
    {
        if (!IsVisible || e.Key != Key.Escape)
        {
            return;
        }

        e.Handled = true;
        Complete(TrayPromptResult.Cancel);
    }

    private void Complete(TrayPromptResult result)
    {
        if (pendingResult is null)
        {
            return;
        }

        var completionSource = pendingResult;
        pendingResult = null;
        IsVisible = false;
        completionSource.TrySetResult(result);
    }
}

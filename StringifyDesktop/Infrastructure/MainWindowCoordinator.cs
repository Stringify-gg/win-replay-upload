using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using StringifyDesktop.Dialogs;
using StringifyDesktop.Services;

namespace StringifyDesktop.Infrastructure;

public sealed class MainWindowCoordinator
{
    private readonly TrayService trayService;
    private readonly UiDispatcher uiDispatcher;
    private Window? window;
    private IClassicDesktopStyleApplicationLifetime? lifetime;
    private Func<bool>? shouldOfferTrayOnClose;
    private Func<Task<TrayPromptResult>>? confirmCloseToTray;
    private bool allowClose;
    private bool isHandlingCloseRequest;

    public MainWindowCoordinator(TrayService trayService, UiDispatcher uiDispatcher)
    {
        this.trayService = trayService;
        this.uiDispatcher = uiDispatcher;
    }

    public void Attach(Window window, IClassicDesktopStyleApplicationLifetime lifetime)
    {
        this.window = window;
        this.lifetime = lifetime;
        trayService.OpenRequested += OnOpenRequested;
        trayService.QuitRequested += OnQuitRequested;
    }

    public void Configure(Func<bool> shouldOfferTrayOnClose, Func<Task<TrayPromptResult>> confirmCloseToTray)
    {
        this.shouldOfferTrayOnClose = shouldOfferTrayOnClose;
        this.confirmCloseToTray = confirmCloseToTray;
    }

    public bool ShouldCancelClose(Window closingWindow, WindowClosingEventArgs e)
    {
        if (!ShouldInterceptCloseRequest(
                allowClose,
                isHandlingCloseRequest,
                e.CloseReason,
                shouldOfferTrayOnClose?.Invoke() ?? false))
        {
            return false;
        }

        if (isHandlingCloseRequest)
        {
            return true;
        }

        isHandlingCloseRequest = true;
        _ = HandleCloseRequestAsync(closingWindow);
        return true;
    }

    internal static bool ShouldInterceptCloseRequest(
        bool allowClose,
        bool isHandlingCloseRequest,
        WindowCloseReason closeReason,
        bool shouldOfferTrayOnClose)
    {
        if (allowClose)
        {
            return false;
        }

        if (isHandlingCloseRequest)
        {
            return true;
        }

        if (closeReason is WindowCloseReason.ApplicationShutdown or WindowCloseReason.OSShutdown)
        {
            return false;
        }

        return shouldOfferTrayOnClose;
    }

    internal static CloseRequestEffect ResolvePromptResult(TrayPromptResult result)
    {
        return result switch
        {
            TrayPromptResult.Quit => CloseRequestEffect.CloseWindow,
            TrayPromptResult.MinimizeToTray => CloseRequestEffect.HideToTray,
            _ => CloseRequestEffect.KeepOpen
        };
    }

    public void ShowWindow()
    {
        if (window is null)
        {
            return;
        }

        uiDispatcher.Post(() =>
        {
            if (!window.IsVisible)
            {
                window.Show();
            }

            window.WindowState = WindowState.Normal;
            window.Activate();
        });
    }

    public async Task ShutdownAsync()
    {
        allowClose = true;

        await uiDispatcher.InvokeAsync(() =>
        {
            if (window is not null)
            {
                window.Close();
            }
            return Task.CompletedTask;
        });
    }

    public void Dispose()
    {
        trayService.OpenRequested -= OnOpenRequested;
        trayService.QuitRequested -= OnQuitRequested;
        trayService.Dispose();
    }

    private void OnOpenRequested(object? sender, EventArgs e)
    {
        ShowWindow();
    }

    private async void OnQuitRequested(object? sender, EventArgs e)
    {
        await ShutdownAsync();
    }

    private async Task HandleCloseRequestAsync(Window closingWindow)
    {
        try
        {
            var result = await (confirmCloseToTray?.Invoke() ?? Task.FromResult(TrayPromptResult.Cancel));
            var effect = ResolvePromptResult(result);

            await uiDispatcher.InvokeAsync(() =>
            {
                if (effect == CloseRequestEffect.HideToTray)
                {
                    closingWindow.Hide();
                    trayService.ShowSyncingNotification();
                    return;
                }

                if (effect == CloseRequestEffect.CloseWindow)
                {
                    allowClose = true;
                    closingWindow.Close();
                }
            });
        }
        catch
        {
            // If the prompt fails, keep the window open rather than unexpectedly quitting.
        }
        finally
        {
            isHandlingCloseRequest = false;
        }
    }

    internal enum CloseRequestEffect
    {
        KeepOpen,
        HideToTray,
        CloseWindow
    }
}

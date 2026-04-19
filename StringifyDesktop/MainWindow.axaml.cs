using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using StringifyDesktop.Dialogs;
using StringifyDesktop.Infrastructure;

namespace StringifyDesktop;

public partial class MainWindow : SukiUI.Controls.SukiWindow
{
    private readonly Infrastructure.AppContext? context;
    private TrayPromptWindow? trayPromptDialog;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(Infrastructure.AppContext context)
        : this()
    {
        this.context = context;
        DataContext = context.MainViewModel;
        context.FilePickerService.Attach(this);
        context.MainWindowCoordinator.Configure(
            () => context.MainViewModel.ShouldOfferTrayOnClose,
            ShowTrayPromptAsync);
        Closing += OnClosing;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        trayPromptDialog = this.FindControl<TrayPromptWindow>("TrayPromptDialog");
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (context is null)
        {
            return;
        }

        e.Cancel = context.MainWindowCoordinator.ShouldCancelClose(this, e);
        if (!e.Cancel)
        {
            await context.DisposeAsync();
        }
    }

    private Task<TrayPromptResult> ShowTrayPromptAsync()
    {
        return trayPromptDialog?.ShowAsync() ?? Task.FromResult(TrayPromptResult.Cancel);
    }
}

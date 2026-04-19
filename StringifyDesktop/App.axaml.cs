using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using StringifyDesktop.Infrastructure;

namespace StringifyDesktop;

public partial class App : Avalonia.Application
{
    private Infrastructure.AppContext? context;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            context = Infrastructure.AppContext.Create(BootstrapState.RequireSingleInstance());
            await context.InitializeAsync();

            var window = new MainWindow(context);
            desktop.MainWindow = window;
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnMainWindowClose;

            context.MainWindowCoordinator.Attach(window, desktop);
            window.Show();
        }

        base.OnFrameworkInitializationCompleted();
    }
}

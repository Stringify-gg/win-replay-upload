using Avalonia;
using Sentry;
using StringifyDesktop.Services;

namespace StringifyDesktop;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {

        SentrySdk.Init(options =>
        {
            // A Sentry Data Source Name (DSN) is required.
            // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
            // You can set it in the SENTRY_DSN environment variable, or you can set it in code here.
            options.Dsn = "https://d7472bba64e06bb11378e1ebc23c7694@o4510541825769472.ingest.us.sentry.io/4511247936126976";

            // When debug is enabled, the Sentry client will emit detailed debugging information to the console.
            // This might be helpful, or might interfere with the normal operation of your application.
            // We enable it here for demonstration purposes when first trying Sentry.
            // You shouldn't do this in your applications unless you're troubleshooting issues with Sentry.
            options.Debug = true;

            // This option is recommended. It enables Sentry's "Release Health" feature.
            options.AutoSessionTracking = true;
        });

        if (!SingleInstanceService.TryCreate("StringifyDesktop", args, out var singleInstance) || singleInstance is null)
        {
            return 0;
        }

        BootstrapState.Initialize(singleInstance);
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
    }
}

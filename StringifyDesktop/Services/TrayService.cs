using System.Drawing;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Forms = System.Windows.Forms;

namespace StringifyDesktop.Services;

public sealed class TrayService : IDisposable
{
    private readonly Forms.NotifyIcon notifyIcon;
    private readonly Icon icon;

    public TrayService(AppPaths paths)
    {
        using var stream = paths.OpenAppIconStream();
        icon = new Icon(stream);

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open Stringify Desktop", null, (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Quit", null, (_, _) => QuitRequested?.Invoke(this, EventArgs.Empty));

        notifyIcon = new Forms.NotifyIcon
        {
            Icon = icon,
            Text = "Stringify Desktop",
            Visible = true,
            ContextMenuStrip = menu
        };

        notifyIcon.DoubleClick += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? OpenRequested;

    public event EventHandler? QuitRequested;

    public void ShowSyncingNotification()
    {
        notifyIcon.ShowBalloonTip(
            2500,
            "Still syncing from the tray",
            "Use the tray icon to reopen the uploader or quit it completely.",
            Forms.ToolTipIcon.Info);
    }

    public void Dispose()
    {
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        icon.Dispose();
    }
}

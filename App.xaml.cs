using System.Drawing;
using System.Windows;
using Forms = System.Windows.Forms;

namespace AiBuddy;

public partial class App : System.Windows.Application
{
    private Forms.NotifyIcon? _tray;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var main = new MainWindow();
        MainWindow = main;
        main.Show();

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open chat", null, (_, _) => main.Dispatcher.Invoke(() =>
        {
            main.Show();
            main.ChatFromTray();
        }));
        menu.Items.Add("Settings", null, (_, _) => main.Dispatcher.Invoke(main.OpenSettingsFromTray));
        var watchItem = new Forms.ToolStripMenuItem();
        watchItem.Click += (_, _) => main.Dispatcher.Invoke(() =>
        {
            main.ToggleWatching();
            watchItem.Checked = main.IsWatching;
            watchItem.Text = main.IsWatching ? "Pause screen watching" : "Enable screen watching";
        });
        menu.Items.Add(watchItem);
        var spiderItem = new Forms.ToolStripMenuItem();
        spiderItem.Click += (_, _) => main.Dispatcher.Invoke(() =>
        {
            main.ToggleSpiderVisibility();
            spiderItem.Text = main.IsSpiderVisible ? "Hide spider" : "Show spider";
        });
        menu.Items.Add(spiderItem);
        menu.Items.Add("Exit", null, (_, _) => Shutdown());
        menu.Opening += (_, _) =>
        {
            watchItem.Checked = main.IsWatching;
            watchItem.Text = main.IsWatching ? "Pause screen watching" : "Enable screen watching";
            spiderItem.Text = main.IsSpiderVisible ? "Hide spider" : "Show spider";
        };
        _tray = new Forms.NotifyIcon
        {
            Text = "AI Buddy",
            Icon = SystemIcons.Application,
            ContextMenuStrip = menu,
            Visible = true
        };
        _tray.DoubleClick += (_, _) => main.Dispatcher.Invoke(() =>
        {
            main.Show();
            main.Activate();
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        base.OnExit(e);
    }
}

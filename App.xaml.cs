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
        menu.Items.Add("Open AI Buddy", null, (_, _) => main.Dispatcher.Invoke(() =>
        {
            main.Show();
            main.Activate();
        }));
        menu.Items.Add("Exit", null, (_, _) => Shutdown());
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

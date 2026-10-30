using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace AiBuddy;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _watchTimer = new() { Interval = TimeSpan.FromSeconds(3) };
    private BuddySettings _settings = SettingsService.Load();
    private byte[]? _lastSample;
    private DateTime _lastProactive = DateTime.MinValue;
    private bool _requestInProgress;
    private ChatWindow? _chat;
    private readonly System.Windows.Media.TranslateTransform _bob = new();
    private readonly DispatcherTimer _animationTimer = new() { Interval = TimeSpan.FromMilliseconds(40) };
    private readonly DateTime _animationStarted = DateTime.UtcNow;

    public MainWindow()
    {
        InitializeComponent();
        RabbitImage.RenderTransform = _bob;
        _watchTimer.Tick += WatchTimer_Tick;
        _animationTimer.Tick += (_, _) => _bob.Y = Math.Sin((DateTime.UtcNow - _animationStarted).TotalSeconds * 2.4) * 4;
        _animationTimer.Start();
        WatchButton.Content = _settings.WatchOnStart ? "Watching · pause" : "Watch off";
        if (_settings.WatchOnStart) _watchTimer.Start();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var area = SystemParameters.WorkArea;
        Left = area.Right - Width - 24;
        Top = area.Bottom - Height - 24;
    }

    private void DragWindow(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void Chat_Click(object sender, RoutedEventArgs e)
    {
        _chat ??= new ChatWindow(() => _settings, OpenSettings);
        _chat.Closed += (_, _) => _chat = null;
        _chat.Show();
        _chat.Activate();
    }

    private void Watch_Click(object sender, RoutedEventArgs e)
    {
        if (_watchTimer.IsEnabled)
        {
            _watchTimer.Stop();
            _lastSample = null;
            WatchButton.Content = "Watch off";
            _settings = _settings with { WatchOnStart = false };
        }
        else
        {
            _watchTimer.Start();
            _lastSample = null;
            WatchButton.Content = "Watching · pause";
            _settings = _settings with { WatchOnStart = true };
        }
        SettingsService.Save(_settings);
    }

    private async void WatchTimer_Tick(object? sender, EventArgs e)
    {
        if (_requestInProgress || !_watchTimer.IsEnabled) return;
        _requestInProgress = true;
        try
        {
            using var frame = ScreenCapture.CaptureFrame();
            if (_lastSample is null)
            {
                _lastSample = frame.ChangeSample;
                return;
            }
            var changed = ScreenCapture.DifferenceRatio(_lastSample, frame.ChangeSample) >= 0.012;
            _lastSample = frame.ChangeSample;
            if (!changed || DateTime.UtcNow - _lastProactive < TimeSpan.FromSeconds(30)) return;
            _lastProactive = DateTime.UtcNow;

            var response = await new OpenAiClient(_settings.Model).AskAsync(
                "You are a quiet desktop helper. Review this screen and offer one brief, practical suggestion only if it is clearly useful. If there is nothing useful to add, respond with exactly [NO_SUGGESTION]. Do not claim you performed actions.",
                frame.ToJpeg());
            if (!string.Equals(response.Trim(), "[NO_SUGGESTION]", StringComparison.OrdinalIgnoreCase))
            {
                _chat?.AddProactive(response);
                SuggestionPopup.ShowNear(this, response);
            }
        }
        catch (Exception ex)
        {
            _watchTimer.Stop();
            _settings = _settings with { WatchOnStart = false };
            SettingsService.Save(_settings);
            WatchButton.Content = "Watch paused";
            WatchButton.ToolTip = ex.Message;
            _chat?.ShowStatus("Screen watching paused: " + ex.Message);
        }
        finally { _requestInProgress = false; }
    }

    private void Settings_Click(object sender, RoutedEventArgs e) => OpenSettings();

    private void OpenSettings()
    {
        var dialog = new SettingsWindow(_settings) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            _settings = dialog.Result;
            SettingsService.Save(_settings);
            if (_settings.WatchOnStart) _watchTimer.Start(); else _watchTimer.Stop();
            WatchButton.Content = _watchTimer.IsEnabled ? "Watching · pause" : "Watch off";
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => System.Windows.Application.Current.Shutdown();
}

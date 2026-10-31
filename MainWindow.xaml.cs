using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace AiBuddy;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _watchTimer = new() { Interval = TimeSpan.FromSeconds(3) };
    private readonly DispatcherTimer _activityTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private BuddySettings _settings = SettingsService.Load();
    private byte[]? _lastSample;
    private DateTime _lastProactive = DateTime.MinValue;
    private string? _lastSuggestion;
    private bool _requestInProgress;
    private bool _isAskingQuestion;
    private bool _petIsActive;
    private ChatWindow? _chat;
    private readonly System.Windows.Media.TranslateTransform _bob = new();
    private readonly DispatcherTimer _animationTimer = new() { Interval = TimeSpan.FromMilliseconds(40) };
    private readonly DateTime _animationStarted = DateTime.UtcNow;

    public MainWindow()
    {
        InitializeComponent();
        PetImage.RenderTransform = _bob;
        _watchTimer.Tick += WatchTimer_Tick;
        _activityTimer.Tick += (_, _) => UpdatePetState();
        _animationTimer.Tick += (_, _) => _bob.Y = Math.Sin((DateTime.UtcNow - _animationStarted).TotalSeconds * (_petIsActive ? 2.4 : 0.7)) * (_petIsActive ? 4 : 1);
        _animationTimer.Start();
        _activityTimer.Start();
        if (_settings.WatchOnStart)
        {
            _watchTimer.Start();
        }
        UpdateWatchMenuItem();
        UpdatePetState();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var area = SystemParameters.WorkArea;
        Left = area.Left + (area.Width - Width) / 2;
        Top = area.Top;
    }

    private void Pet_Click(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
        {
            Chat_Click(sender, new RoutedEventArgs());
        }
    }

    private void Chat_Click(object sender, RoutedEventArgs e)
    {
        _chat ??= new ChatWindow(() => _settings, OpenSettings, SetAskingState);
        _chat.Closed += (_, _) => _chat = null;
        _chat.Show();
        _chat.Activate();
        _chat.ShowStatus(_watchTimer.IsEnabled
            ? "Screen watching is on. AI provider: " + _settings.Provider + "."
            : "Screen watching is off. Your questions still include a one-time current screenshot.");
    }

    private void WatchMenu_Click(object sender, RoutedEventArgs e) => ToggleWatching();

    public bool IsWatching => _watchTimer.IsEnabled;
    public bool IsSpiderVisible => IsVisible;

    public void ToggleWatching() => SetWatching(!_watchTimer.IsEnabled);

    public void SetWatching(bool enabled)
    {
        if (!enabled)
        {
            _watchTimer.Stop();
            _lastSample = null;
            _settings = _settings with { WatchOnStart = false };
        }
        else
        {
            _watchTimer.Start();
            _lastSample = null;
            _settings = _settings with { WatchOnStart = true };
        }
        SettingsService.Save(_settings);
        UpdateWatchMenuItem();
        UpdatePetState();
    }

    private void UpdateWatchMenuItem()
    {
        if (WatchMenuItem is not null)
            WatchMenuItem.Header = _watchTimer.IsEnabled ? "Pause screen watching" : "Enable screen watching";
    }

    public void ToggleSpiderVisibility()
    {
        if (IsVisible) Hide();
        else Show();
    }

    public void OpenSettingsFromTray()
    {
        if (!IsVisible) Show();
        OpenSettings();
    }

    public void ChatFromTray()
    {
        if (!IsVisible) Show();
        Chat_Click(this, new RoutedEventArgs());
    }

    private void HideSpider_Click(object sender, RoutedEventArgs e) => Hide();

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
            if (!changed || DateTime.UtcNow - _lastProactive < TimeSpan.FromSeconds(45)) return;
            _lastProactive = DateTime.UtcNow;

            var response = await AiAssistant.AskAsync(_settings,
                "You are AI Buddy, a quiet coding coach. Inspect the screenshot and first decide whether the user is clearly coding: look for a code editor or IDE, source code, a terminal running development commands, compiler output, a debugger, or technical documentation related to code. If the screen is not clearly about programming, or the visible details do not support useful advice, respond with exactly [NO_SUGGESTION]. Otherwise give one specific, practical coding tip grounded in what is visible. Prefer pointing out a likely bug, explaining an error, or suggesting a small next step. Keep it to at most two short sentences and under 45 words. Do not repeat generic advice, guess at hidden code, interrupt for minor style preferences, or claim you changed anything.",
                frame.ToJpeg());
            var suggestion = response.Trim();
            if (!string.Equals(suggestion, "[NO_SUGGESTION]", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(suggestion, _lastSuggestion, StringComparison.OrdinalIgnoreCase))
            {
                _lastSuggestion = suggestion;
                _chat?.AddProactive(suggestion);
                SuggestionPopup.ShowNear(this, suggestion);
            }
        }
        catch (Exception ex)
        {
            _watchTimer.Stop();
            _settings = _settings with { WatchOnStart = false };
            SettingsService.Save(_settings);
            UpdateWatchMenuItem();
            _chat?.ShowStatus("Screen watching paused: " + ex.Message);
            UpdatePetState();
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
            _lastSample = null;
            if (_settings.WatchOnStart) _watchTimer.Start(); else _watchTimer.Stop();
            UpdateWatchMenuItem();
            UpdatePetState();
        }
    }

    private void SetAskingState(bool asking)
    {
        _isAskingQuestion = asking;
        UpdatePetState();
    }

    private void UpdatePetState()
    {
        var awake = _isAskingQuestion || (_watchTimer.IsEnabled && CodingActivityDetector.IsCodingWindowForeground());
        _petIsActive = awake;
        LeftOpenEye.Visibility = awake ? Visibility.Visible : Visibility.Collapsed;
        RightOpenEye.Visibility = awake ? Visibility.Visible : Visibility.Collapsed;
        LeftClosedEye.Visibility = awake ? Visibility.Collapsed : Visibility.Visible;
        RightClosedEye.Visibility = awake ? Visibility.Collapsed : Visibility.Visible;
        SleepMarks.Visibility = awake ? Visibility.Collapsed : Visibility.Visible;
        ActiveSparkle.Visibility = awake ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => System.Windows.Application.Current.Shutdown();
}

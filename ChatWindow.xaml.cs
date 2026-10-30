using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AiBuddy;

public partial class ChatWindow : Window
{
    private readonly Func<BuddySettings> _getSettings;
    private readonly Action _openSettings;
    private readonly List<ChatTurn> _history = [];
    private bool _sending;

    public ChatWindow(Func<BuddySettings> getSettings, Action openSettings)
    {
        InitializeComponent();
        _getSettings = getSettings;
        _openSettings = openSettings;
        AddBubble("Hi! Ask me about anything on your screen. The current screen is included with each question.", false);
    }

    public void AddProactive(string message) => Dispatcher.Invoke(() => AddBubble(message, false));

    public void ShowStatus(string message) => Dispatcher.Invoke(() => StatusText.Text = message);

    private async void Send_Click(object sender, RoutedEventArgs e) => await SubmitAsync();

    private async void QuestionBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;
            await SubmitAsync();
        }
    }

    private async Task SubmitAsync()
    {
        var question = QuestionBox.Text.Trim();
        if (question.Length == 0 || _sending) return;
        _sending = true;
        SendButton.IsEnabled = false;
        QuestionBox.Clear();
        AddBubble(question, true);
        StatusText.Text = "Looking at your current screen…";

        try
        {
            using var frame = ScreenCapture.CaptureFrame();
            var client = new OpenAiClient(_getSettings().Model);
            var answer = await client.AskAsync(question, frame.ToJpeg(), _history);
            _history.Add(new ChatTurn("user", question));
            _history.Add(new ChatTurn("assistant", answer));
            if (_history.Count > 16) _history.RemoveRange(0, _history.Count - 16);
            AddBubble(answer, false);
            StatusText.Text = "Screen capture sent for this question only.";
        }
        catch (Exception ex)
        {
            AddBubble("I couldn't complete that request. " + ex.Message, false);
            StatusText.Text = "Check Settings and your network, then try again.";
        }
        finally
        {
            SendButton.IsEnabled = true;
            QuestionBox.Focus();
            _sending = false;
        }
    }

    private void AddBubble(string text, bool fromUser)
    {
        var bubble = new Border
        {
            Background = fromUser ? System.Windows.Media.Brushes.White : (System.Windows.Media.Brush)FindResource("Lavender"),
            CornerRadius = new CornerRadius(13),
            Padding = new Thickness(11),
            Margin = new Thickness(fromUser ? 36 : 0, 4, fromUser ? 0 : 36, 4),
            HorizontalAlignment = fromUser ? System.Windows.HorizontalAlignment.Right : System.Windows.HorizontalAlignment.Left,
            Child = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, Foreground = (System.Windows.Media.Brush)FindResource("Ink") }
        };
        Transcript.Children.Add(bubble);
        TranscriptScroll.ScrollToEnd();
    }

    private void Settings_Click(object sender, RoutedEventArgs e) => _openSettings();
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void DragWindow(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;

namespace AiBuddy;

public sealed class SuggestionPopup : Window
{
    private readonly DispatcherTimer _timer;

    private SuggestionPopup(string text)
    {
        Width = 320;
        Height = 150;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = MediaBrushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        var closeButton = new System.Windows.Controls.Button
        {
            Content = "×",
            Width = 26,
            Height = 24,
            Padding = new Thickness(0),
            Background = MediaBrushes.Transparent,
            BorderThickness = new Thickness(0),
            Foreground = new SolidColorBrush(MediaColor.FromRgb(85, 68, 154)),
            FontSize = 18,
            ToolTip = "Dismiss coding tip"
        };
        closeButton.Click += (_, _) => Close();

        var header = new DockPanel { LastChildFill = true, Margin = new Thickness(0, 0, 0, 6) };
        DockPanel.SetDock(closeButton, Dock.Right);
        header.Children.Add(closeButton);
        header.Children.Add(new TextBlock
        {
            Text = "🐰  Coding tip",
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(MediaColor.FromRgb(85, 68, 154)),
            VerticalAlignment = VerticalAlignment.Center
        });

        Content = new Border
        {
            Background = new SolidColorBrush(MediaColor.FromRgb(255, 252, 255)),
            BorderBrush = new SolidColorBrush(MediaColor.FromRgb(217, 208, 246)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(13),
            Child = new StackPanel
            {
                Children =
                {
                    header,
                    new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(MediaColor.FromRgb(38, 51, 68)), MaxHeight = 88 }
                }
            }
        };
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(18) };
        _timer.Tick += (_, _) => { _timer.Stop(); Close(); };
        Closed += (_, _) => _timer.Stop();
    }

    public static void ShowNear(Window anchor, string text)
    {
        var popup = new SuggestionPopup(text);
        var work = SystemParameters.WorkArea;
        popup.Left = Math.Max(work.Left, Math.Min(work.Right - popup.Width, anchor.Left - popup.Width - 10));
        popup.Top = Math.Max(work.Top, Math.Min(work.Bottom - popup.Height, anchor.Top));
        popup.Show();
        popup._timer.Start();
    }
}

using System.Windows;

namespace AiBuddy;

public partial class SettingsWindow : Window
{
    public BuddySettings Result { get; private set; }

    public SettingsWindow(BuddySettings current)
    {
        InitializeComponent();
        Result = current;
        ModelBox.Text = current.Model;
        WatchStartCheck.IsChecked = current.WatchOnStart;
        KeyStateText.Text = CredentialStore.Read() is null
            ? "No key saved. Paste your API key here; it will be stored in Windows Credential Manager."
            : "A key is saved in Windows Credential Manager. Enter a replacement only if you want to change it.";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var model = ModelBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(model))
        {
            ErrorText.Text = "Enter a model name.";
            return;
        }
        try
        {
            if (!string.IsNullOrWhiteSpace(ApiKeyBox.Password)) CredentialStore.Write(ApiKeyBox.Password.Trim());
            Result = new BuddySettings(model, WatchStartCheck.IsChecked == true);
            DialogResult = true;
        }
        catch (Exception ex) { ErrorText.Text = ex.Message; }
    }

    private void RemoveKey_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            CredentialStore.Delete();
            ApiKeyBox.Clear();
            KeyStateText.Text = "Saved API key removed.";
        }
        catch (Exception ex) { ErrorText.Text = ex.Message; }
    }
}

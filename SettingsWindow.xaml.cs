using System.Windows;

namespace AiBuddy;

public partial class SettingsWindow : Window
{
    public BuddySettings Result { get; private set; }

    public SettingsWindow(BuddySettings current)
    {
        InitializeComponent();
        Result = current;
        ProviderBox.SelectedValue = current.Provider;
        ModelBox.Text = current.Model;
        OllamaUrlBox.Text = current.OllamaUrl;
        WatchStartCheck.IsChecked = current.WatchOnStart;
        KeyStateText.Text = CredentialStore.Read() is null
            ? "No key saved. Paste your API key here; it will be stored in Windows Credential Manager."
            : "A key is saved in Windows Credential Manager. Enter a replacement only if you want to change it.";
        UpdateProviderFields();
    }

    private string SelectedProvider => (ProviderBox.SelectedValue as string) ?? "Ollama";

    private void Provider_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (IsInitialized) UpdateProviderFields();
    }

    private void UpdateProviderFields()
    {
        var local = SelectedProvider.Equals("Ollama", StringComparison.OrdinalIgnoreCase);
        OllamaPanel.Visibility = local ? Visibility.Visible : Visibility.Collapsed;
        OpenAiPanel.Visibility = local ? Visibility.Collapsed : Visibility.Visible;
        RemoveKeyButton.Visibility = local ? Visibility.Collapsed : Visibility.Visible;
        PrivacyText.Text = local
            ? "Screen frames are analyzed by Ollama on this PC. Frames are held in memory and are not saved or sent to a cloud service."
            : "Screen frames are sent to OpenAI for analysis and are not saved on this PC.";
        if (local && (ModelBox.Text == "gpt-6-astra" || ModelBox.Text == "gpt-4.1-mini")) ModelBox.Text = "qwen3-vl:2b";
        if (!local && ModelBox.Text == "qwen3-vl:2b") ModelBox.Text = "gpt-4.1-mini";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var provider = SelectedProvider;
        var model = ModelBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(model))
        {
            ErrorText.Text = "Enter a model name.";
            return;
        }
        var ollamaUrl = OllamaUrlBox.Text.Trim();
        if (provider == "Ollama" && (!Uri.TryCreate(ollamaUrl, UriKind.Absolute, out var uri) ||
                                      (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
        {
            ErrorText.Text = "Enter a valid Ollama address, such as http://localhost:11434.";
            return;
        }
        try
        {
            if (provider == "OpenAI")
            {
                if (!string.IsNullOrWhiteSpace(ApiKeyBox.Password)) CredentialStore.Write(ApiKeyBox.Password.Trim());
                if (CredentialStore.Read() is null)
                {
                    ErrorText.Text = "Add an OpenAI API key in Settings, or switch to Ollama for local use without API credits.";
                    return;
                }
            }
            Result = new BuddySettings(provider, model, ollamaUrl, WatchStartCheck.IsChecked == true);
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

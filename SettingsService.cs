using System.Text.Json;
using System.IO;

namespace AiBuddy;

public static class SettingsService
{
    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AiBuddy");
    private static readonly string FilePath = Path.Combine(DirectoryPath, "settings.json");

    public static BuddySettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var saved = JsonSerializer.Deserialize<BuddySettings>(File.ReadAllText(FilePath));
                if (saved is null) return BuddySettings.Default;
                var legacySettings = string.IsNullOrWhiteSpace(saved.Provider);
                return saved with
                {
                    Provider = legacySettings ? BuddySettings.Default.Provider : saved.Provider,
                    Model = legacySettings && saved.Model == "gpt-6-astra"
                        ? BuddySettings.Default.Model
                        : string.IsNullOrWhiteSpace(saved.Model) ? BuddySettings.Default.Model : saved.Model,
                    OllamaUrl = string.IsNullOrWhiteSpace(saved.OllamaUrl) ? BuddySettings.Default.OllamaUrl : saved.OllamaUrl
                };
            }
        }
        catch { }
        return BuddySettings.Default;
    }

    public static void Save(BuddySettings settings)
    {
        Directory.CreateDirectory(DirectoryPath);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
    }
}

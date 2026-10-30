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
                return JsonSerializer.Deserialize<BuddySettings>(File.ReadAllText(FilePath)) ?? BuddySettings.Default;
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

using System.IO;
using System.Text.Json;

namespace BossKey;

public static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly string DirectoryPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BossKey");

    private static readonly string FilePath = Path.Combine(DirectoryPath, "settings.json");

    public static BossKeySettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return new BossKeySettings();
            }

            var json = File.ReadAllText(FilePath);
            var settings = JsonSerializer.Deserialize<BossKeySettings>(json, JsonOptions);
            return settings ?? new BossKeySettings();
        }
        catch
        {
            return new BossKeySettings();
        }
    }

    public static void Save(BossKeySettings settings)
    {
        Directory.CreateDirectory(DirectoryPath);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(FilePath, json);
    }
}

using System.Text.Json;

namespace RateScoutMiner;

/// <summary>Настройки пользователя, автосохранение в %LocalAppData%\RateScoutMiner\settings.json.</summary>
public sealed class Settings
{
    public string Pool { get; set; } = "pool.supportxmr.com:3333";
    public string Wallet { get; set; } = "";
    public string Worker { get; set; } = "rig1";
    public int Cpu { get; set; } = 50;

    private static string FilePath => Path.Combine(XmrigManager.BaseDir, "settings.json");

    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new Settings();
        }
        catch { /* повреждённый файл — берём дефолты */ }
        return new Settings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(XmrigManager.BaseDir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* не критично */ }
    }
}

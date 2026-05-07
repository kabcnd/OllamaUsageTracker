using System.IO;
using System.Text.Json;

namespace OllamaUsageTracker
{
    public class AppSettings
    {
        public string CookieValue { get; set; } = "";
        public bool DebugEnabled { get; set; } = false;
        public int UpdateIntervalSeconds { get; set; } = 10;

        private const string SettingsFile = "settings.json";

        public static AppSettings Load()
        {
            if (File.Exists(SettingsFile))
            {
                try
                {
                    string json = File.ReadAllText(SettingsFile);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                catch { }
            }
            return new AppSettings();
        }

        public void Save()
        {
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }
    }
}

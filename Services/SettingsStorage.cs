using BreathAIClient.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace BreathAIClient.Services
{
    public static class SettingsStorage
    {
        private static string GetConfigDir()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BreathAIClient");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }

        private static string GetSettingsPath() => Path.Combine(GetConfigDir(), "settings.json");

        public static async Task<ApiSettings> LoadAsync()
        {
            var path = GetSettingsPath();
            if (!File.Exists(path)) return new ApiSettings();
            using var fs = File.OpenRead(path);
            var settings = await JsonSerializer.DeserializeAsync<ApiSettings>(fs) ?? new ApiSettings();
            return settings;
        }

        public static async Task SaveAsync(ApiSettings settings)
        {
            var path = GetSettingsPath();
            var opts = new JsonSerializerOptions { WriteIndented = true };
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(settings, opts));
        }
    }
}

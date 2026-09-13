using System;
using System.IO;
using System.Text.Json;

namespace R10CSharp.Services
{
    public class AppSettings
    {
        public int ThumbnailParallelism { get; set; } = 4;
        public string ArchivePath { get; set; } = @"D:\\10_Fotoarchiv";
        public string IndexPath { get; set; } = @"D:\\11_Foto_App\\R10CSharp\\Data\\index.json";
        public string ConnectionString { get; set; } = @"Server=(localdb)\\MSSQLLocalDB;Database=R10PhotoDb;Trusted_Connection=True;";
        // Optional: list of subfolders (names) inside the archive root to include when scanning.
        // If null or empty, all subfolders are scanned.
        public string[]? IncludeFolders { get; set; } = null;
    }

    public static class SettingsService
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "config.json");

        // Shared JsonSerializerOptions for consistent serialization and to avoid repeated allocations
        public static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions { WriteIndented = true };

        public static AppSettings Load()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    var def = new AppSettings();
                    Save(def);
                    return def;
                }

                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public static void Save(AppSettings settings)
        {
            var dir = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
            var json = JsonSerializer.Serialize(settings, SerializerOptions);
            File.WriteAllText(ConfigPath, json);
        }
    }
}

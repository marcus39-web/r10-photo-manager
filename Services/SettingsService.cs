using System;
using System.IO;
using System.Text.Json;

namespace R10CSharp.Services
{
    /// <summary>
    /// Persistente Anwendungseinstellungen.
    /// Definiert Standardpfade sowie Laufzeitoptionen für Scan und Import.
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Maximale Parallelität für die Thumbnail-Erzeugung.
        /// </summary>
        public int ThumbnailParallelism { get; set; } = 4;

        /// <summary>
        /// Wurzelpfad des Fotoarchivs.
        /// </summary>
        public string ArchivePath { get; set; } = @"D:\\10_Fotoarchiv\\Canon_R10_Bilder";

        /// <summary>
        /// Speicherort der JSON-Indexdatei.
        /// </summary>
        public string IndexPath { get; set; } = @"D:\\11_Foto_App\\R10CSharp\\Data\\index.json";

        /// <summary>
        /// Verbindungszeichenfolge für den optionalen Datenbankimport.
        /// </summary>
        public string ConnectionString { get; set; } = @"Server=(localdb)\\MSSQLLocalDB;Database=R10PhotoDb;Trusted_Connection=True;";

        /// <summary>
        /// Optionale Liste direkter Unterordner innerhalb des Archivpfads, die gescannt werden sollen.
        /// Wenn null oder leer, werden alle passenden Unterordner berücksichtigt.
        /// </summary>
        public string[]? IncludeFolders { get; set; } = null;
    }

    /// <summary>
    /// Lädt und speichert die Anwendungseinstellungen als JSON unterhalb des Data-Ordners.
    /// </summary>
    public static class SettingsService
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "config.json");

        // Gemeinsame JsonSerializerOptions für konsistente Serialisierung und weniger wiederholte Allokationen.
        public static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions { WriteIndented = true };

        /// <summary>
        /// Lädt die Einstellungen aus der Konfigurationsdatei.
        /// Existiert die Datei noch nicht, werden Standardwerte erstellt und gespeichert.
        /// </summary>
        public static AppSettings Load()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    AppSettings def = new();
                    Save(def);
                    return def;
                }

                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new();
            }
            catch
            {
                return new();
            }
        }

        /// <summary>
        /// Speichert die Einstellungen in die Konfigurationsdatei.
        /// </summary>
        public static void Save(AppSettings settings)
        {
            var dir = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
            var json = JsonSerializer.Serialize(settings, SerializerOptions);
            File.WriteAllText(ConfigPath, json);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using R10CSharp.Data;
using R10CSharp.Models;

namespace R10CSharp.Services
{
    /// <summary>
    /// Importiert einen bestehenden JSON-Index optional in die Datenbank.
    /// Dient als Brücke zwischen dateibasierter und datenbankgestützter Ablage.
    /// </summary>
    public class IndexImportService(R10PhotoContext db, AppSettings settings)
    {
        private static readonly JsonSerializerOptions ImportJsonOptions = new() { PropertyNameCaseInsensitive = true };
        private readonly R10PhotoContext _db = db;
        private readonly AppSettings _settings = settings;

        /// <summary>
        /// Importiert vorhandene JSON-Indexdatei in die Datenbank, falls Einträge noch nicht vorhanden sind.
        /// Idempotent: bereits vorhandene Einträge (nach FilePath) werden übersprungen.
        /// </summary>
        public async Task ImportIfNeededAsync()
        {
            try
            {
                var path = _settings.IndexPath;
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    Console.WriteLine("IndexImport: keine index.json gefunden.");
                    return;
                }

                var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
                var entries = JsonSerializer.Deserialize<List<PhotoIndexEntry>>(json, ImportJsonOptions);
                if (entries == null || entries.Count == 0)
                {
                    Console.WriteLine("IndexImport: index.json ist leer.");
                    return;
                }

                // Ermittelt bereits vorhandene FilePaths in der DB, damit der Import idempotent bleibt.
                var existingPaths = await _db.PhotoIndexEntries.Select(p => p.FilePath).ToListAsync().ConfigureAwait(false);

                var toImport = entries.Where(e => !existingPaths.Contains(e.FilePath, StringComparer.OrdinalIgnoreCase)).ToList();
                if (toImport.Count == 0)
                {
                    Console.WriteLine("IndexImport: Keine neuen Einträge zum Importieren.");
                    return;
                }

                // Entfernt ggf. alte Id-Werte aus importierten Objekten, da EF eigene Schlüssel vergibt.
                foreach (var e in toImport)
                {
                    e.Id = 0;
                    _db.PhotoIndexEntries.Add(e);
                }

                await _db.SaveChangesAsync().ConfigureAwait(false);
                Console.WriteLine($"IndexImport: {toImport.Count} Einträge importiert.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"IndexImport fehlgeschlagen: {ex.Message}");
            }
        }
    }
}

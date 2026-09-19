using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using R10CSharp.Models;

namespace R10CSharp.Services
{
    /// <summary>
    /// Erstellt, lädt und speichert den Fotoindex.
    /// Zusätzlich werden Vorschaubilder erzeugt, damit die Suche und Anzeige schneller reagieren.
    /// </summary>
    public class IndexBuilder
    {
        private static readonly string[] RawExtensions = { ".cr2", ".cr3", ".nef", ".arw", ".rw2", ".dng" };
        private const int ThumbnailWidth = 200;
        private const int ThumbnailHeight = 200;

        /// <summary>
        /// Stellt sicher, dass das Thumbnail-Zielverzeichnis vorhanden ist.
        /// </summary>
        private static string EnsureThumbnailsFolder()
        {
            var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            var thumbs = Path.Combine(dataDir, "Thumbnails");
            if (!Directory.Exists(thumbs)) Directory.CreateDirectory(thumbs);
            return thumbs;
        }

        /// <summary>
        /// Erzeugt oder aktualisiert ein Thumbnail für eine bestimmte Bilddatei.
        /// </summary>
        public static string? RegenerateThumbnail(string imagePath)
        {
            return CreateThumbnail(imagePath);
        }

        /// <summary>
        /// Erzeugt ein JPEG-Thumbnail für eine Bilddatei.
        /// Bereits aktuelle Vorschaubilder werden wiederverwendet.
        /// </summary>
        private static string? CreateThumbnail(string imagePath)
        {
            try
            {
                // Verwende WPF-Imaging, um ein verkleinertes Vorschaubild zu erstellen
                var thumbs = EnsureThumbnailsFolder();
                var fileName = Path.GetFileNameWithoutExtension(imagePath);

                // Erzeuge einen kurzen Hash aus dem vollständigen Pfad, damit Thumbnails
                // für Dateien mit identischem Dateinamen in unterschiedlichen Ordnern
                // nicht kollidieren.
                string hashHex;
                var data = Encoding.UTF8.GetBytes(imagePath.ToLowerInvariant());
                var hash = SHA1.HashData(data);
                hashHex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()[..8];

                var thumbFileName = fileName + "_" + hashHex + "_thumb.jpg";
                var thumbPath = Path.Combine(thumbs, thumbFileName);

                // Falls das Thumbnail bereits existiert und neuer ist als die Quell-Datei, wiederverwenden
                if (File.Exists(thumbPath))
                {
                    var ti = new FileInfo(thumbPath);
                    var si = new FileInfo(imagePath);
                    if (ti.LastWriteTime >= si.LastWriteTime) return thumbPath;
                }

                // Erzeuge das Thumbnail mit BitmapImage und JpegBitmapEncoder
                var uri = new Uri(imagePath, UriKind.Absolute);
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.UriSource = uri;
                bitmap.DecodePixelWidth = ThumbnailWidth;
                bitmap.DecodePixelHeight = ThumbnailHeight;
                bitmap.EndInit();
                bitmap.Freeze();

                var encoder = new System.Windows.Media.Imaging.JpegBitmapEncoder
                {
                    QualityLevel = 75
                };
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));

                using (var fs = new FileStream(thumbPath, FileMode.Create))
                {
                    encoder.Save(fs);
                }

                return thumbPath;
            }
            catch
            {
                // Bei Fehlern (z. B. nicht unterstütztes Format) null zurückgeben und fortfahren
                return null;
            }
        }

        /// <summary>
        /// Asynchroner Komforteinstieg für den Indexaufbau mit Standardparallelität.
        /// </summary>
        public async Task<List<PhotoIndexEntry>> BuildIndexAsync(string rootPath, IProgress<PhotoIndexEntry>? progress = null, CancellationToken? cancellationToken = null)
        {
            var ct = cancellationToken ?? CancellationToken.None;
            List<PhotoIndexEntry> result = new();
            if (!Directory.Exists(rootPath)) return result;

            // Verwendet FileScanner und berücksichtigt optionale IncludeFolders aus den Einstellungen.
            _ = SettingsService.Load().IncludeFolders;
            // Standard-Parallelität: 4.
            return await BuildIndexAsyncParallel(rootPath, 4, progress, ct);
        }

        /// <summary>
        /// Baut den Index parallel auf und meldet einzelne Fortschrittseinträge an die UI zurück.
        /// </summary>
        public static async Task<List<PhotoIndexEntry>> BuildIndexAsyncParallel(string rootPath, int maxDegreeOfParallelism = 4, IProgress<PhotoIndexEntry>? progress = null, CancellationToken? cancellationToken = null)
        {
            var ct = cancellationToken ?? CancellationToken.None;
            List<PhotoIndexEntry> result = new();
            if (!Directory.Exists(rootPath)) return result;

            var include = SettingsService.Load().IncludeFolders;
            var files = FileScanner.GetImageFiles(rootPath, include)
                .Where(f => RawExtensions.Any(re => f.EndsWith(re, StringComparison.OrdinalIgnoreCase))
                         || f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            // Semaphore begrenzt die gleichzeitige Thumbnail-Erzeugung, damit I/O und Speicher stabil bleiben.
            using var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);

            var tasks = files.Select(async f =>
            {
                ct.ThrowIfCancellationRequested();
                await semaphore.WaitAsync(ct);
                try
                {
                    // Erstellt den eigentlichen Indexeintrag aus Dateisystemdaten.
                    var fi = new FileInfo(f);
                    var entry = new PhotoIndexEntry
                    {
                        FileName = fi.Name,
                        FilePath = fi.FullName,
                        ParentFolder = fi.Directory?.Name ?? string.Empty,
                        RawOrJpg = RawExtensions.Any(re => fi.Extension.Equals(re, StringComparison.OrdinalIgnoreCase)) ? "RAW" : "JPG",
                        Category = string.Empty,
                        Series = string.Empty,
                        Timestamp = fi.LastWriteTime,
                        Tags = string.Empty
                    };

                    // Thumbnail-Erstellung wird ausgelagert, damit die Aufruferoberfläche responsiv bleibt.
                    var thumb = await Task.Run(() => CreateThumbnail(fi.FullName), ct);
                    if (!string.IsNullOrWhiteSpace(thumb)) entry.Thumbnail = thumb!;

                    progress?.Report(entry);
                    return entry;
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToArray();

            var entries = await Task.WhenAll(tasks);
            result.AddRange(entries);
            return result;
        }

        /// <summary>
        /// Einfache synchrone Variante des Indexaufbaus.
        /// Wird vor allem dort genutzt, wo kein asynchroner Workflow benötigt wird.
        /// </summary>
        public static List<PhotoIndexEntry> BuildIndex(string rootPath)
        {
            List<PhotoIndexEntry> result = new();
            if (!Directory.Exists(rootPath)) return result;

            // Direkter rekursiver Dateidurchlauf für den synchronen Neuaufbau.
            var files = Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                         || RawExtensions.Any(re => f.EndsWith(re, StringComparison.OrdinalIgnoreCase))
                         || f.EndsWith(".png", StringComparison.OrdinalIgnoreCase));

            foreach (var f in files)
            {
                var fi = new FileInfo(f);
                var entry = new PhotoIndexEntry
                {
                    FileName = fi.Name,
                    FilePath = fi.FullName,
                    ParentFolder = fi.Directory?.Name ?? string.Empty,
                    RawOrJpg = RawExtensions.Any(re => fi.Extension.Equals(re, StringComparison.OrdinalIgnoreCase)) ? "RAW" : "JPG",
                    Category = string.Empty,
                    Series = string.Empty,
                    Timestamp = fi.LastWriteTime,
                    Tags = string.Empty
                };

                // Versuche, ein Thumbnail zu erstellen; bei Erfolg den Pfad speichern
                var thumb = CreateThumbnail(fi.FullName);
                if (!string.IsNullOrWhiteSpace(thumb)) entry.Thumbnail = thumb!;

                result.Add(entry);
            }

            return result;
        }

        /// <summary>
        /// Speichert den Index formatiert als JSON-Datei.
        /// </summary>
        public static void SaveIndex(List<PhotoIndexEntry> index, string filePath)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(index, options);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Lädt den Index aus einer JSON-Datei.
        /// Fehlerhafte oder leere Dateien führen bewusst zu einem leeren Ergebnis statt zu einer Ausnahme.
        /// </summary>
        public static List<PhotoIndexEntry> LoadIndex(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return Enumerable.Empty<PhotoIndexEntry>().ToList();
                var json = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(json)) return Enumerable.Empty<PhotoIndexEntry>().ToList();
                var list = JsonSerializer.Deserialize<List<PhotoIndexEntry>>(json) ?? new List<PhotoIndexEntry>();

                // Beim Laden des Index unerwünschte Pfade (Google/iCloud/Apple) filtern
                var filtered = list.Where(i => i.FilePath != null
                                               && !i.FilePath.ToLowerInvariant().Contains("google")
                                               && !i.FilePath.ToLowerInvariant().Contains("icloud")
                                               && !i.FilePath.ToLowerInvariant().Contains("apple"))
                                     .ToList();

                return filtered;
            }
            catch
            {
                // Bei Fehlern beim Einlesen/Deserialisieren einen leeren Index zurückgeben
                return Enumerable.Empty<PhotoIndexEntry>().ToList();
            }
        }
    }
}

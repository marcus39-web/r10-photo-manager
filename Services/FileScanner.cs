using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace R10CSharp.Services
{
    /// <summary>
    /// Liefert unterstützte Bilddateien aus dem Archiv.
    /// Berücksichtigt dabei optionale Bibliotheksfilter und blendet bekannte Sync-Ordner aus.
    /// </summary>
    public static class FileScanner
    {
        /// <summary>
        /// Durchsucht ein Wurzelverzeichnis rekursiv nach unterstützten Bilddateien.
        /// Optional kann auf bestimmte direkte Unterordner eingeschränkt werden.
        /// </summary>
        public static IEnumerable<string> GetImageFiles(string rootPath, string[]? includeFolderNames = null)
        {
            if (!Directory.Exists(rootPath)) return Enumerable.Empty<string>();

            // Unterstützte Dateitypen für JPG, PNG und verschiedene RAW-Formate.
            var files = Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".cr2", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".cr3", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".nef", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".arw", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".rw2", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".dng", StringComparison.OrdinalIgnoreCase));

            if (includeFolderNames == null || includeFolderNames.Length == 0)
            {
                // Wenn keine Include-Liste gesetzt ist, standardmäßig nur die direkten
                // Unterordner des rootPath scannen, aber bekannte Sync-Ordner wie
                // Google/iCloud/Apple ausschließen.
                var topDirs = Directory.EnumerateDirectories(rootPath, "*", SearchOption.TopDirectoryOnly)
                    .Select(d => Path.GetFileName(d))
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToArray();

                // Entfernt bekannte Cloud-/Sync-Ordner, damit diese nicht versehentlich indexiert werden.
                var filtered = topDirs.Where(n =>
                {
                    var ln = n!.ToLowerInvariant();
                    if (ln.Contains("google") || ln.Contains("icloud") || ln.Contains("apple")) return false;
                    return true;
                }).ToArray();

                // Keine starre Whitelist verwenden, damit auch abweichende
                // Bibliotheksnamen wie RAW-Ordner mit Tippfehlern oder Korrekturen
                // zuverlässig mitgescannt werden.
                var preferred = filtered.Where(n =>
                    n!.Equals("Eingang", StringComparison.OrdinalIgnoreCase)
                    || n.StartsWith("01_", StringComparison.OrdinalIgnoreCase)
                    || n.StartsWith("02_", StringComparison.OrdinalIgnoreCase)
                    || n.StartsWith("03_", StringComparison.OrdinalIgnoreCase)
                ).ToArray();

                if (preferred.Length > 0)
                {
                    includeFolderNames = preferred;
                }
                else if (filtered.Length > 0)
                {
                    includeFolderNames = filtered;
                }
                else
                {
                    // Keine passenden Unterordner vorhanden -> alle gefundenen Dateien zurückgeben.
                    return files;
                }
            }

            // Nur Dateien zurückgeben, deren erster relativer Ordnername in der erlaubten Menge enthalten ist.
            var set = new HashSet<string>(includeFolderNames, StringComparer.OrdinalIgnoreCase);

            return files.Where(f =>
            {
                try
                {
                    var rel = Path.GetRelativePath(rootPath, f);
                    var parts = rel.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0) return false;
                    // Take the first segment under rootPath (immediate subfolder)
                    var first = parts[0];
                    return set.Contains(first);
                }
                catch
                {
                    return false;
                }
            });
        }
    }
}

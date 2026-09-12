using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using R10CSharp.Models;

namespace R10CSharp.Services
{
    public class IndexBuilder
    {
        private static readonly string[] RawExtensions = new[] { ".cr2", ".nef", ".arw", ".rw2", ".dng" };
        private const int ThumbnailWidth = 200;
        private const int ThumbnailHeight = 200;

        private string EnsureThumbnailsFolder(string rootPath)
        {
            var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            var thumbs = Path.Combine(dataDir, "Thumbnails");
            if (!Directory.Exists(thumbs)) Directory.CreateDirectory(thumbs);
            return thumbs;
        }

        private string? CreateThumbnail(string imagePath)
        {
            try
            {
                // Use WPF imaging to create a resized thumbnail
                var thumbs = EnsureThumbnailsFolder(Path.GetDirectoryName(imagePath) ?? string.Empty);
                var fileName = Path.GetFileNameWithoutExtension(imagePath);
                var thumbPath = Path.Combine(thumbs, fileName + "_thumb.jpg");

                // If thumbnail already exists and is newer than source, reuse it
                if (File.Exists(thumbPath))
                {
                    var ti = new FileInfo(thumbPath);
                    var si = new FileInfo(imagePath);
                    if (ti.LastWriteTime >= si.LastWriteTime) return thumbPath;
                }

                // Create thumbnail using BitmapImage and JpegBitmapEncoder
                var uri = new Uri(imagePath, UriKind.Absolute);
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.UriSource = uri;
                bitmap.DecodePixelWidth = ThumbnailWidth;
                bitmap.DecodePixelHeight = ThumbnailHeight;
                bitmap.EndInit();
                bitmap.Freeze();

                var encoder = new System.Windows.Media.Imaging.JpegBitmapEncoder();
                encoder.QualityLevel = 75;
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));

                using (var fs = new FileStream(thumbPath, FileMode.Create))
                {
                    encoder.Save(fs);
                }

                return thumbPath;
            }
            catch
            {
                // If any error (unsupported format), return null and continue
                return null;
            }
        }

        public List<PhotoIndexEntry> BuildIndex(string rootPath)
        {
            var result = new List<PhotoIndexEntry>();
            if (!Directory.Exists(rootPath)) return result;

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
                    RawOrJpg = RawExtensions.Any(re => fi.Extension.Equals(re, StringComparison.OrdinalIgnoreCase)) ? "RAW" : "JPG",
                    Category = string.Empty,
                    Series = string.Empty,
                    Timestamp = fi.LastWriteTime,
                    Tags = string.Empty
                };

                // Try to create a thumbnail; if successful, store path
                var thumb = CreateThumbnail(fi.FullName);
                if (!string.IsNullOrWhiteSpace(thumb)) entry.Thumbnail = thumb!;

                result.Add(entry);
            }

            return result;
        }

        public void SaveIndex(List<PhotoIndexEntry> index, string filePath)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(index, options);
            File.WriteAllText(filePath, json);
        }

        public List<PhotoIndexEntry> LoadIndex(string filePath)
        {
            if (!File.Exists(filePath)) return new List<PhotoIndexEntry>();
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<PhotoIndexEntry>>(json) ?? new List<PhotoIndexEntry>();
        }
    }
}

using System;

namespace R10CSharp.Models
{
    public class PhotoIndexEntry
    {
        // Primärschlüssel für die DB
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string RawOrJpg { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Series { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.MinValue;
        public string Tags { get; set; } = string.Empty;
        // Relativer oder absoluter Pfad zu einem generierten Vorschaubild
        public string Thumbnail { get; set; } = string.Empty;
    }
}

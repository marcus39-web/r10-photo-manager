using System;

namespace R10CSharp.Models
{
    /// <summary>
    /// Repräsentiert einen indexierten Fotoeintrag.
    /// Wird sowohl für JSON-Speicherung als auch für EF-Core-Datenbankzugriffe verwendet.
    /// </summary>
    public class PhotoIndexEntry
    {
        /// <summary>
        /// Primärschlüssel für die Datenbank.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Dateiname inklusive Erweiterung.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Vollständiger Dateipfad zur Originaldatei.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Kennzeichnet, ob es sich um eine RAW- oder JPG-Datei handelt.
        /// </summary>
        public string RawOrJpg { get; set; } = string.Empty;

        /// <summary>
        /// Fachliche Kategorie bzw. Ordner-/Filterkategorie.
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Serien- oder Projektzuordnung.
        /// </summary>
        public string Series { get; set; } = string.Empty;

        /// <summary>
        /// Zeitstempel der Datei, aktuell aus dem Dateisystem abgeleitet.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Frei nutzbare Schlagwörter für Suche und Organisation.
        /// </summary>
        public string Tags { get; set; } = string.Empty;

        /// <summary>
        /// Relativer oder absoluter Pfad zu einem generierten Vorschaubild.
        /// </summary>
        public string Thumbnail { get; set; } = string.Empty;

        /// <summary>
        /// Name des direkten übergeordneten Ordners, z. B. zur Anzeige in Trefferlisten.
        /// </summary>
        public string ParentFolder { get; set; } = string.Empty;

        /// <summary>
        /// Benutzerfreundlicher Anzeigename.
        /// Für JPG wird der Dateiname direkt verwendet, für RAW wird die Endung entfernt
        /// und um den Zusatz "RAW" ergänzt.
        /// </summary>
        public string DisplayName
        {
            get
            {
                try
                {
                    if (string.Equals(RawOrJpg, "JPG", StringComparison.OrdinalIgnoreCase))
                    {
                        return FileName; // enthält bereits .jpg
                    }
                    // RAW: Dateiname ohne Extension + ' RAW'
                    var name = System.IO.Path.GetFileNameWithoutExtension(FileName);
                    return name + " RAW";
                }
                catch
                {
                    return FileName;
                }
            }
        }
    }
}

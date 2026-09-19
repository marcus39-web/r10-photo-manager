using System;
using System.Windows;
using System.Windows.Controls;
using R10CSharp.Services;

namespace R10CSharp.Pages
{
    /// <summary>
    /// Einstellungsseite für Archivpfade und Laufzeitoptionen.
    /// </summary>
    public partial class SettingsPage : Page
    {
        private readonly AppSettings _settings = new();

        public SettingsPage()
        {
            InitializeComponent();

            // Lädt die aktuellen Einstellungen und befüllt die UI.
            _settings = SettingsService.Load();

            ArchivePathBox.Text = _settings.ArchivePath;
            IndexPathBox.Text = _settings.IndexPath;
            DefaultFilterBox.SelectedIndex = 0; // Standard: RAW
            ThemeBox.SelectedIndex = 0;
            ParallelismBox.Text = _settings.ThumbnailParallelism.ToString();
        }

        /// <summary>
        /// Übernimmt die Eingaben aus der UI und speichert sie dauerhaft.
        /// </summary>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _settings.ArchivePath = ArchivePathBox.Text;
            _settings.IndexPath = IndexPathBox.Text;
            if (int.TryParse(ParallelismBox.Text, out var p) && p > 0) _settings.ThumbnailParallelism = p;
            SettingsService.Save(_settings);

            MessageBox.Show("Einstellungen gespeichert.");
        }
    }
}

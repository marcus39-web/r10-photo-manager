using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using R10CSharp.Services;
using R10CSharp.Models;

namespace R10CSharp.Pages
{
    /// <summary>
    /// Seite zum Scannen des Fotoarchivs und Erzeugen eines aktualisierten Indexes.
    /// Zeigt Fortschritt, Dateinamen und ETA an und unterstützt Abbruch.
    /// </summary>
    public partial class ScanPage : Page
    {
        public ScanPage()
        {
            InitializeComponent();
        }

        // Aktuelle Abbruchquelle für einen laufenden Scan.
        private CancellationTokenSource? _cts;

        /// <summary>
        /// Startet den Scanvorgang mit den aktuellen Einstellungen.
        /// </summary>
        private async void Scan_Click(object sender, RoutedEventArgs e)
        {
            var builder = new IndexBuilder();
            var settings = SettingsService.Load();
            var indexPath = settings?.IndexPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "index.json");
            var root = settings?.ArchivePath ?? @"D:\10_Fotoarchiv";

            try
            {
                ScanButton.IsEnabled = false;
                CancelButton.IsEnabled = true;
                ScanStatus.Text = "Scannen läuft...";
                ScanProgress.Value = 0;

                // Anzahl der Dateien für die Fortschrittsanzeige ermitteln.
                var include = settings?.IncludeFolders;
                var files = FileScanner.GetImageFiles(root, include).ToArray();

                var total = files.Length;
                if (total == 0)
                {
                    ScanStatus.Text = "Keine Dateien im angegebenen Archiv gefunden.";
                    return;
                }

                ScanProgress.Minimum = 0;
                ScanProgress.Maximum = total;

                _cts = new CancellationTokenSource();
                var scanned = 0;
                var sw = System.Diagnostics.Stopwatch.StartNew();

                var parallelism = settings?.ThumbnailParallelism > 0 ? settings.ThumbnailParallelism : 4;

                // Fortschrittsobjekt aktualisiert UI-Elemente nach jedem fertigen Indexeintrag.
                var progress = new Progress<PhotoIndexEntry>(entry =>
                {
                    scanned++;
                    ScanProgress.Value = scanned;
                    ScanFileName.Text = entry.FileName;
                    // ETA-Berechnung auf Basis der bisher durchschnittlichen Bearbeitungsdauer.
                    var elapsed = sw.Elapsed.TotalSeconds;
                    var avg = elapsed / Math.Max(1, scanned);
                    var remaining = total - scanned;
                    var eta = TimeSpan.FromSeconds(avg * remaining);
                    ScanEta.Text = $@"ETA: {eta:mm\:ss}";
                    ScanStatus.Text = $"Scanne... {scanned}/{total}";
                });

                // Paralleler Indexaufbau mit Fortschrittsrückmeldung und optionalem Abbruch.
                var index = await IndexBuilder.BuildIndexAsyncParallel(root, parallelism, progress, _cts.Token);
                IndexBuilder.SaveIndex(index, indexPath);

                ScanStatus.Text = $"Scan abgeschlossen. {index.Count} Dateien gefunden.";
            }
            catch (OperationCanceledException)
            {
                ScanStatus.Text = "Scan abgebrochen.";
            }
            catch (Exception ex)
            {
                ScanStatus.Text = "Fehler beim Scannen: " + ex.Message;
            }
            finally
            {
                ScanButton.IsEnabled = true;
                CancelButton.IsEnabled = false;
                _cts = null;
            }
        }

        /// <summary>
        /// Bricht einen laufenden Scan ab.
        /// </summary>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
        }
        // Navigations-Handler aus der Page entfernt, um doppelte Methodennamen zu vermeiden
    }
}

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
    public partial class ScanPage : Page
    {
        public ScanPage()
        {
            InitializeComponent();
        }

        private CancellationTokenSource? _cts;

        private async void Scan_Click(object sender, RoutedEventArgs e)
        {
            var builder = new IndexBuilder();
            var indexPath = @"D:\11_Foto_App\R10CSharp\Data\index.json";
            var root = @"D:\10_Fotoarchiv";

            try
            {
                ScanButton.IsEnabled = false;
                CancelButton.IsEnabled = true;
                ScanStatus.Text = "Scannen läuft...";
                ScanProgress.Value = 0;

                // determine total files for progress
                var files = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                    .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                             || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                             || f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                             || f.EndsWith(".cr2", StringComparison.OrdinalIgnoreCase)
                             || f.EndsWith(".nef", StringComparison.OrdinalIgnoreCase)
                             || f.EndsWith(".arw", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

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

                var settings = SettingsService.Load();
                var parallelism = settings?.ThumbnailParallelism > 0 ? settings.ThumbnailParallelism : 4;

                var progress = new Progress<PhotoIndexEntry>(entry =>
                {
                    scanned++;
                    ScanProgress.Value = scanned;
                    ScanFileName.Text = entry.FileName;
                    // ETA calculation
                    var elapsed = sw.Elapsed.TotalSeconds;
                    var avg = elapsed / Math.Max(1, scanned);
                    var remaining = total - scanned;
                    var eta = TimeSpan.FromSeconds(avg * remaining);
                    ScanEta.Text = $@"ETA: {eta:mm\:ss}";
                    ScanStatus.Text = $"Scanne... {scanned}/{total}";
                });

                var index = await builder.BuildIndexAsyncParallel(root, parallelism, progress, _cts.Token);
                builder.SaveIndex(index, indexPath);

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

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
        }
        // Navigation handler removed from page to avoid duplicate method names
    }
}

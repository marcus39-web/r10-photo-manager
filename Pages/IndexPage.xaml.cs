using System.Linq;
using System.Windows.Controls;
using R10CSharp.Services;

namespace R10CSharp.Pages
{
    /// <summary>
    /// Zeigt Kennzahlen des aktuell geladenen Indexes an.
    /// Baut bei Bedarf automatisch einen neuen Index auf.
    /// </summary>
    public partial class IndexPage : Page
    {
        public IndexPage()
        {
            InitializeComponent();

            var builder = new IndexBuilder();
            var indexPath = @"D:\11_Foto_App\R10CSharp\Data\index.json";
            var index = IndexBuilder.LoadIndex(indexPath);

            // Wenn der geladene Index leer ist, automatisch einen Neuaufbau starten.
            if (index == null || index.Count == 0)
            {
                // Nutzt bevorzugt den ArchivePath aus den Einstellungen.
                var settings = SettingsService.Load();
                var root = settings?.ArchivePath ?? @"D:\10_Fotoarchiv";
                index = IndexBuilder.BuildIndex(root);
                // Index sofort speichern, damit nachfolgende Seiten denselben Datenstand nutzen.
                IndexBuilder.SaveIndex(index, indexPath);
            }

            // Anzeige einfacher zusammengefasster Kennzahlen für einen schnellen Überblick.
            TotalFiles.Text = $"Gesamtanzahl Dateien: {index.Count}";
            RawCount.Text = $"RAW Dateien: {index.Count(f => f.RawOrJpg == "RAW")}";
            JpgCount.Text = $"JPG Dateien: {index.Count(f => f.RawOrJpg == "JPG")}";
            CategoryCount.Text = $"Kategorien: {index.Select(f => f.Category).Distinct().Count()}";
            SeriesCount.Text = $"Serien: {index.Select(f => f.Series).Distinct().Count()}";
        }
        // Navigations-Handler aus dem Code-Behind der Seite entfernt; die Navigation wird vom MainWindow gesteuert
    }
}

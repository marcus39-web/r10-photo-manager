using System.IO;
using System.Windows;
using System.Windows.Controls;
using R10CSharp.Pages;

namespace R10CSharp
{
    /// <summary>
    /// Hauptfenster mit Navigation und globaler Suchleiste.
    /// Verknüpft die Eingabe oben mit der SearchPage und übernimmt optional
    /// eine von Windows/Explorer übergebene Startdatei als Suchauslöser.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(string? startupFilePath = null)
        {
            InitializeComponent();
            if (!string.IsNullOrWhiteSpace(startupFilePath) && File.Exists(startupFilePath))
            {
                // Wichtige Shell-/Windows-Integration:
                // Wenn die App über eine Datei gestartet wurde, wird direkt die Suche geöffnet
                // und mit dem Dateinamen vorbelegt, damit der Benutzer ähnliche Bilder schnell findet.
                MainFrame.Navigate(new SearchPage(Path.GetFileNameWithoutExtension(startupFilePath)));
            }
            else
            {
                MainFrame.Navigate(new StartPage());
            }

            NavStart.Click += (s, e) => MainFrame.Navigate(new StartPage());
            NavSearch.Click += (s, e) => MainFrame.Navigate(new SearchPage());
            NavScan.Click += (s, e) => MainFrame.Navigate(new ScanPage());
            NavIndex.Click += (s, e) => MainFrame.Navigate(new IndexPage());
            NavSettings.Click += (s, e) => MainFrame.Navigate(new SettingsPage());

            TopSearchButton.Click += (s, e) =>
            {
                // Zentrale Suchleiste des Hauptfensters -> delegiert an die SearchPage.
                var q = TopSearchBox.Text;
                MainFrame.Navigate(new SearchPage(q));
            };
        }
    }
}

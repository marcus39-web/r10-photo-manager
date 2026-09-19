using System.Windows;
using System.Windows.Controls;

namespace R10CSharp.Pages
{
    /// <summary>
    /// Startseite mit Schnellzugriffen auf die Hauptbereiche der Anwendung.
    /// </summary>
    public partial class StartPage : Page
    {
        public StartPage()
        {
            InitializeComponent();
            // Handler programmgesteuert anhängen, um XAML-Compile-Time-Bindungsprobleme zu vermeiden.
            if (this.FindName("SearchButton") is Button sb) sb.Click += Search_Click;
            if (this.FindName("ScanButton") is Button scb) scb.Click += Scan_Click;
            if (this.FindName("IndexButton") is Button ib) ib.Click += Index_Click;
            if (this.FindName("SettingsButton") is Button setb) setb.Click += Settings_Click;
        }

        /// <summary>
        /// Öffnet die Suchseite.
        /// </summary>
        private void Search_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).MainFrame.Navigate(new SearchPage());
        }

        /// <summary>
        /// Öffnet die Scan-Seite.
        /// </summary>
        private void Scan_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).MainFrame.Navigate(new ScanPage());
        }

        /// <summary>
        /// Öffnet die Index-Übersicht.
        /// </summary>
        private void Index_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).MainFrame.Navigate(new IndexPage());
        }

        /// <summary>
        /// Öffnet die Einstellungsseite.
        /// </summary>
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).MainFrame.Navigate(new SettingsPage());
        }

        /// <summary>
        /// Navigiert erneut zur Startseite.
        /// </summary>
        private void Start_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).MainFrame.Navigate(new StartPage());
        }
    }
}

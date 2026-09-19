using System.IO;
using System.Windows;
using System.Windows.Controls;
using R10CSharp.Pages;

namespace R10CSharp
{
    public partial class MainWindow : Window
    {
        public MainWindow(string? startupFilePath = null)
        {
            InitializeComponent();
            if (!string.IsNullOrWhiteSpace(startupFilePath) && File.Exists(startupFilePath))
            {
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
                var q = TopSearchBox.Text;
                MainFrame.Navigate(new SearchPage(q));
            };
        }
    }
}

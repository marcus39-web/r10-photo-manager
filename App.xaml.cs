
using System;
using System.IO;
using System.Windows;

namespace R10CSharp
{
    public partial class App : Application
    {
        // App.xaml defines resources and StartupUri.
        // This partial class provides small helper functions used across the app.

        public static void Log(string message)
        {
            try
            {
                var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, "app_error_log.txt");
                File.AppendAllText(path, $"[{DateTime.Now:O}] {message}{Environment.NewLine}");
            }
            catch
            {
                // Swallow logging errors to avoid recursive failures
            }
        }
    }
}


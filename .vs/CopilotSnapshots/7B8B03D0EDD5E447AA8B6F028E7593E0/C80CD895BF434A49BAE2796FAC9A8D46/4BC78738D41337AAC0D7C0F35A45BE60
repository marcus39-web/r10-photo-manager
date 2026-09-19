
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using R10CSharp.Services;

namespace R10CSharp
{
    public partial class App : Application
    {
        public App()
        {
            // Global handlers to capture exceptions that cause the app to exit silently
            this.DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                ExplorerOpenWithRegistration.EnsureRegistered();
                WindowsAppRegistration.EnsureRegistered();
            }
            catch (Exception ex)
            {
                Log($"Shell registration failed: {ex}");
            }

            var startupFilePath = e.Args.FirstOrDefault(File.Exists);
            var mainWindow = new MainWindow(startupFilePath);
            MainWindow = mainWindow;
            mainWindow.Show();
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log($"DispatcherUnhandledException: {e.Exception}");
            TryWriteCrashFile(e.Exception);
            // Do not swallow by default; let debugger/VS break on thrown exceptions if enabled
            // e.Handled = true; // optional
        }

        private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            Log($"DomainUnhandledException: {ex}");
            TryWriteCrashFile(ex);
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            Log($"UnobservedTaskException: {e.Exception}");
            TryWriteCrashFile(e.Exception);
            e.SetObserved();
        }

        static void TryWriteCrashFile(Exception? ex)
        {
            try
            {
                var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, "last_crash.txt");
                File.AppendAllText(path, $"[{DateTime.Now:O}] {ex}{Environment.NewLine}");
            }
            catch
            {
                // ignore
            }
        }

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


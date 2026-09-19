using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace R10CSharp.Services
{
    /// <summary>
    /// Registriert die Anwendung bei Windows, damit sie im Startmenü, in der Windows-App-Suche
    /// und in der Deinstallationsliste sauber erscheint.
    /// </summary>
    internal static class WindowsAppRegistration
    {
        private const string AppName = "R10 Photo Manager";
        // Alias für die Windows-Suche. So kann die App auch über einen kurzen technischen Namen gefunden werden.
        private const string SearchAlias = "R10CSharp";
        private const string Publisher = "Marcus39Web";
        private const string UninstallKeyName = "R10PhotoManager";

        public static void EnsureRegistered()
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath)) return;

            var installDir = Path.GetDirectoryName(exePath) ?? AppDomain.CurrentDomain.BaseDirectory;
            var iconPath = File.Exists(Path.Combine(installDir, "Assets", "appicon.ico"))
                ? Path.Combine(installDir, "Assets", "appicon.ico")
                : exePath;

            EnsureStartMenuShortcut(exePath, iconPath);
            EnsureUninstallEntry(exePath, installDir, iconPath);
            EnsureAppPath(exePath);
        }

        private static void EnsureStartMenuShortcut(string exePath, string iconPath)
        {
            var programsRoot = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            var programsFolder = Path.Combine(programsRoot, AppName);

            Directory.CreateDirectory(programsFolder);

            var shortcutPath = Path.Combine(programsFolder, $"{AppName}.lnk");
            var rootShortcutPath = Path.Combine(programsRoot, $"{AppName}.lnk");
            var aliasShortcutPath = Path.Combine(programsRoot, $"{SearchAlias}.lnk");
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return;

            var shell = Activator.CreateInstance(shellType);
            if (shell == null) return;

            try
            {
                // Standard-Startmenüeintrag im eigenen Ordner.
                CreateShortcut(shell, shortcutPath, exePath, iconPath, AppName);
                // Zusätzlicher Eintrag direkt im Programme-Stamm für bessere Sichtbarkeit.
                CreateShortcut(shell, rootShortcutPath, exePath, iconPath, AppName);
                // Alias-Verknüpfung: verbessert die Auffindbarkeit über die Windows-App-Suche.
                CreateShortcut(shell, aliasShortcutPath, exePath, iconPath, $"{AppName} ({SearchAlias})");
                NotifyShellStartMenuChanged(programsRoot);
            }
            finally
            {
                try
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
                }
                catch
                {
                }
            }
        }

        private static void NotifyShellStartMenuChanged(string programsRoot)
        {
            try
            {
                const uint SHCNE_UPDATEDIR = 0x00001000;
                const uint SHCNE_ASSOCCHANGED = 0x08000000;
                const uint SHCNF_PATHW = 0x0005;
                const uint SHCNF_IDLIST = 0x0000;

                SHChangeNotify(SHCNE_UPDATEDIR, SHCNF_PATHW, programsRoot, null);
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, null, null);
            }
            catch
            {
            }
        }

        private static void CreateShortcut(object shell, string shortcutPath, string exePath, string iconPath, string description)
        {
            var shortcut = shell.GetType().InvokeMember(
                "CreateShortcut",
                BindingFlags.InvokeMethod,
                null,
                shell,
                new object[] { shortcutPath });

            if (shortcut == null) return;

            try
            {
                var shortcutType = shortcut.GetType();
                shortcutType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { exePath });
                shortcutType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { Path.GetDirectoryName(exePath) ?? string.Empty });
                shortcutType.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut, new object[] { description });
                shortcutType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut, new object[] { iconPath });
                shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, Array.Empty<object>());
            }
            finally
            {
                try
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shortcut);
                }
                catch
                {
                }
            }
        }

        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, string? dwItem1, string? dwItem2);

        private static void EnsureUninstallEntry(string exePath, string installDir, string iconPath)
        {
            // Sorgt dafür, dass Windows die App als installierte Anwendung kennt.
            using var key = Registry.CurrentUser.CreateSubKey($@"Software\Microsoft\Windows\CurrentVersion\Uninstall\{UninstallKeyName}");
            if (key == null) return;

            key.SetValue("DisplayName", AppName);
            key.SetValue("Publisher", Publisher);
            key.SetValue("DisplayVersion", GetDisplayVersion());
            key.SetValue("InstallLocation", installDir);
            key.SetValue("DisplayIcon", iconPath);
            key.SetValue("NoModify", 1, RegistryValueKind.DWord);
            key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            key.SetValue("UninstallString", exePath);
        }

        private static void EnsureAppPath(string exePath)
        {
            var exeName = Path.GetFileName(exePath);
            // App Paths erlaubt Windows, die EXE samt Suchpfad zuverlässig aufzulösen.
            using var key = Registry.CurrentUser.CreateSubKey($@"Software\Microsoft\Windows\CurrentVersion\App Paths\{exeName}");
            if (key == null) return;

            key.SetValue(string.Empty, exePath);
            key.SetValue("Path", Path.GetDirectoryName(exePath) ?? string.Empty);
        }

        private static string GetDisplayVersion()
        {
            try
            {
                return FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion
                       ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                       ?? "1.0.0";
            }
            catch
            {
                return "1.0.0";
            }
        }
    }
}

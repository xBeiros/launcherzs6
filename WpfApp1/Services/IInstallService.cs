using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace WpfApp1.Services
{
    public sealed record InstallOptions(
        string InstallDirectory,
        bool CreateDesktopShortcut,
        bool CreateStartMenuShortcut,
        bool EnableAutoStart);

    public sealed record InstallProgressInfo(string StepTitle, string DetailText, double Percent);

    public sealed record UninstallOptions(
        string InstallDirectory,
        bool RemoveCacheAndTempData);

    public sealed record UninstallProgressInfo(string StepTitle, string DetailText, double Percent);

    public interface IInstallService
    {
        string DefaultInstallDirectory { get; }
        bool IsRunningFromInstallDirectory();
        Task<string> InstallAsync(InstallOptions options, IProgress<InstallProgressInfo>? progress = null, CancellationToken cancellationToken = default);
        Task UninstallAsync(UninstallOptions options, IProgress<UninstallProgressInfo>? progress = null, CancellationToken cancellationToken = default);
        void LaunchInstalledApp(string installedExePath);
        void RefreshInstalledMetadataAndIcons();
    }

    /// <summary>
    /// Selbstständiger Single-EXE-Installer & rückstandsfreier Uninstaller ohne klassisches Windows-MSI:
    /// Installiert die einzelne .exe, legt Verknüpfungen & Registry-Einträge an und entfernt bei der
    /// Deinstallation sämtliche Dateien, Verknüpfungen, Registry-Schlüssel, Cache- & Temp-Daten sauber.
    /// </summary>
    public sealed class LauncherInstallService : IInstallService
    {
        private const string AppFolderName = "ZodiacSixLauncher";
        private const string ExecutableName = "ZodiacSix-Launcher.exe";
        private const string UninstallRegPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\ZodiacSixLauncher";
        private const string RunRegPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public string DefaultInstallDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppFolderName);

        public bool IsRunningFromInstallDirectory()
        {
            var currentExe = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(currentExe))
            {
                return true;
            }

            var currentDir = Path.GetFullPath(Path.GetDirectoryName(currentExe) ?? string.Empty)
                .TrimEnd(Path.DirectorySeparatorChar);
            var defaultDir = Path.GetFullPath(DefaultInstallDirectory)
                .TrimEnd(Path.DirectorySeparatorChar);

            if (string.Equals(currentDir, defaultDir, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Auch benutzerdefinierten Installationspfad aus der Registry anerkennen
            var registeredDir = GetRegisteredInstallDirectory();
            if (!string.IsNullOrWhiteSpace(registeredDir))
            {
                var normalizedRegDir = Path.GetFullPath(registeredDir).TrimEnd(Path.DirectorySeparatorChar);
                if (string.Equals(currentDir, normalizedRegDir, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<string> InstallAsync(
            InstallOptions options,
            IProgress<InstallProgressInfo>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var sourceExe = Environment.ProcessPath
                ?? throw new InvalidOperationException("Aktueller Prozesspfad konnte nicht ermittelt werden.");
            var sourceDir = Path.GetDirectoryName(sourceExe)
                ?? throw new InvalidOperationException("Quellverzeichnis konnte nicht ermittelt werden.");

            progress?.Report(new InstallProgressInfo("Bereite Zielverzeichnis vor...", options.InstallDirectory, 15));
            Directory.CreateDirectory(options.InstallDirectory);
            await Task.Delay(350, cancellationToken);

            // Prüfen, ob wir als veröffentlichte Single-EXE laufen oder als Multi-File-Build (z. B. aus Visual Studio F5)
            var sourceExeName = Path.GetFileName(sourceExe);
            var runtimeConfigPath = Path.Combine(sourceDir, Path.GetFileNameWithoutExtension(sourceExe) + ".runtimeconfig.json");
            var isMultiFileDevBuild = File.Exists(runtimeConfigPath);

            var targetExeName = isMultiFileDevBuild ? sourceExeName : ExecutableName;
            var targetExe = Path.Combine(options.InstallDirectory, targetExeName);

            progress?.Report(new InstallProgressInfo("Kopiere ZODIAC SIX Launcher...", targetExeName, 45));
            if (!string.Equals(Path.GetFullPath(sourceExe), Path.GetFullPath(targetExe), StringComparison.OrdinalIgnoreCase))
            {
                await CopyFileAsync(sourceExe, targetExe, cancellationToken);

                // Im Visual-Studio-Debug-Build benötigt die apphost-.exe ihre zugehörigen .dll/.json-Dateien daneben
                if (isMultiFileDevBuild)
                {
                    var siblingPatterns = new[] { "*.dll", "*.deps.json", "*.runtimeconfig.json" };
                    foreach (var pattern in siblingPatterns)
                    {
                        foreach (var file in Directory.GetFiles(sourceDir, pattern))
                        {
                            var dstFile = Path.Combine(options.InstallDirectory, Path.GetFileName(file));
                            await CopyFileAsync(file, dstFile, cancellationToken);
                        }
                    }
                }
            }
            await Task.Delay(400, cancellationToken);

            if (options.CreateDesktopShortcut)
            {
                progress?.Report(new InstallProgressInfo("Erstelle Desktop-Verknüpfung...", "ZODIAC SIX Launcher.lnk", 65));
                var desktopLnk = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    "ZODIAC SIX Launcher.lnk");
                CreateWindowsShortcut(desktopLnk, targetExe, options.InstallDirectory, "ZODIAC SIX Roleplay Launcher");
                await Task.Delay(250, cancellationToken);
            }

            if (options.CreateStartMenuShortcut)
            {
                progress?.Report(new InstallProgressInfo("Erstelle Startmenü-Eintrag...", "Programme\\ZODIAC SIX", 80));
                var startMenuDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                    "ZODIAC SIX");
                Directory.CreateDirectory(startMenuDir);
                var startMenuLnk = Path.Combine(startMenuDir, "ZODIAC SIX Launcher.lnk");
                CreateWindowsShortcut(startMenuLnk, targetExe, options.InstallDirectory, "ZODIAC SIX Roleplay Launcher");
                await Task.Delay(250, cancellationToken);
            }

            progress?.Report(new InstallProgressInfo("Registriere App & Uninstaller...", "Windows-Integration", 92));
            RegisterWindowsUninstallEntry(options.InstallDirectory, targetExe);
            ConfigureAutoStart(options.EnableAutoStart, targetExe);
            NotifyShellIconsChanged();
            await Task.Delay(300, cancellationToken);

            progress?.Report(new InstallProgressInfo("Installation abgeschlossen!", "Willkommen in Leonida.", 100));
            return targetExe;
        }

        public async Task UninstallAsync(
            UninstallOptions options,
            IProgress<UninstallProgressInfo>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var installDir = string.IsNullOrWhiteSpace(options.InstallDirectory)
                ? DefaultInstallDirectory
                : options.InstallDirectory;

            // 1. Andere laufende Launcher-Instanzen sauber beenden
            progress?.Report(new UninstallProgressInfo("Beende laufende Launcher-Prozesse...", "Prüfe aktive Instanzen", 15));
            StopOtherLauncherProcesses();
            await Task.Delay(300, cancellationToken);

            // 2. Desktop- & Startmenü-Verknüpfungen entfernen
            progress?.Report(new UninstallProgressInfo("Entferne Verknüpfungen...", "Desktop & Startmenü", 35));
            DeleteFileIfExists(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "ZODIAC SIX Launcher.lnk"));
            DeleteDirectoryIfExists(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                "ZODIAC SIX"));
            await Task.Delay(300, cancellationToken);

            // 3. Windows-Registry-Einträge entfernen (Autostart + Installierte Apps)
            progress?.Report(new UninstallProgressInfo("Bereinige Windows-Registry...", "Autostart & App-Eintrag", 55));
            RemoveRegistryEntries();
            await Task.Delay(300, cancellationToken);

            // 4. Lokale Cache-, Einstellungs- & Temp-Daten entfernen
            if (options.RemoveCacheAndTempData)
            {
                progress?.Report(new UninstallProgressInfo("Lösche Cache- & Temp-Daten...", "Updater-Temp & lokale Daten", 75));
                DeleteDirectoryIfExists(Path.Combine(Path.GetTempPath(), "ZodiacSixUpdater"));
                DeleteDirectoryIfExists(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    AppFolderName));
                await Task.Delay(300, cancellationToken);
            }

            // 5. Installationsordner (inkl. laufender .exe per Self-Delete-Skript) vollständig löschen
            progress?.Report(new UninstallProgressInfo("Entferne Programmdateien...", installDir, 92));
            ScheduleFullDirectoryCleanup(installDir);
            await Task.Delay(350, cancellationToken);

            progress?.Report(new UninstallProgressInfo("Vollständig deinstalliert", "Alle ZODIAC SIX Daten wurden entfernt.", 100));
        }

        public void LaunchInstalledApp(string installedExePath)
        {
            if (!File.Exists(installedExePath))
            {
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = installedExePath,
                WorkingDirectory = Path.GetDirectoryName(installedExePath) ?? string.Empty,
                UseShellExecute = true
            });
        }

        /// <summary>
        /// Aktualisiert nach einem Auto-Update die Registry-Version und stößt eine Erneuerung
        /// der Desktop-/Startmenü-Verknüpfungen & des Windows-Icon-Caches an.
        /// </summary>
        public void RefreshInstalledMetadataAndIcons()
        {
            var currentExe = Environment.ProcessPath;
            var installDir = string.IsNullOrWhiteSpace(currentExe) ? null : Path.GetDirectoryName(currentExe);
            if (string.IsNullOrWhiteSpace(currentExe) || string.IsNullOrWhiteSpace(installDir))
            {
                return;
            }

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(UninstallRegPath, writable: true);
                var registeredVersion = key?.GetValue("DisplayVersion") as string;
                if (string.Equals(registeredVersion, AppVersionInfo.Current, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                RegisterWindowsUninstallEntry(installDir, currentExe);

                var desktopLnk = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    "ZODIAC SIX Launcher.lnk");
                if (File.Exists(desktopLnk))
                {
                    CreateWindowsShortcut(desktopLnk, currentExe, installDir, "ZODIAC SIX Roleplay Launcher");
                }

                var startMenuLnk = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                    "ZODIAC SIX",
                    "ZODIAC SIX Launcher.lnk");
                if (File.Exists(startMenuLnk))
                {
                    CreateWindowsShortcut(startMenuLnk, currentExe, installDir, "ZODIAC SIX Roleplay Launcher");
                }

                NotifyShellIconsChanged();
            }
            catch
            {
                // Nicht kritisch
            }
        }

        private static void StopOtherLauncherProcesses()
        {
            var currentPid = Environment.ProcessId;
            var currentName = Process.GetCurrentProcess().ProcessName;

            foreach (var name in new[] { currentName, "ZodiacSix-Launcher", "ZodiacSixLauncher" }.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    foreach (var proc in Process.GetProcessesByName(name))
                    {
                        if (proc.Id == currentPid)
                        {
                            continue;
                        }

                        try
                        {
                            proc.Kill(entireProcessTree: true);
                            proc.WaitForExit(2000);
                        }
                        catch
                        {
                            // Prozess bereits beendet oder fehlende Berechtigung
                        }
                    }
                }
                catch
                {
                    // Ignorieren
                }
            }
        }

        private static void RemoveRegistryEntries()
        {
            try
            {
                using var runKey = Registry.CurrentUser.OpenSubKey(RunRegPath, writable: true);
                runKey?.DeleteValue("ZodiacSixLauncher", throwOnMissingValue: false);
            }
            catch
            {
                // Ignorieren
            }

            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(UninstallRegPath, throwOnMissingSubKey: false);
            }
            catch
            {
                // Ignorieren
            }
        }

        private static void ScheduleFullDirectoryCleanup(string installDir)
        {
            if (string.IsNullOrWhiteSpace(installDir) || !Directory.Exists(installDir))
            {
                return;
            }

            var currentExe = Environment.ProcessPath;
            var isRunningInsideInstallDir =
                !string.IsNullOrWhiteSpace(currentExe) &&
                Path.GetFullPath(currentExe).StartsWith(
                    Path.GetFullPath(installDir).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase);

            if (!isRunningInsideInstallDir)
            {
                DeleteDirectoryIfExists(installDir);
                return;
            }

            // Wenn der Uninstaller direkt aus dem Installationsverzeichnis läuft, löscht ein kurzes
            // Hintergrund-Skript nach dem Schließen des Fensters den gesamten Ordner + sich selbst.
            try
            {
                var cleanupScript = Path.Combine(Path.GetTempPath(), $"zodiac_cleanup_{Guid.NewGuid():N}.cmd");
                var scriptContent = $"""
                    @echo off
                    timeout /t 2 /nobreak > nul
                    rmdir /s /q "{installDir}"
                    del "%~f0"
                    """;
                File.WriteAllText(cleanupScript, scriptContent);

                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{cleanupScript}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }
            catch
            {
                // Fallback ignorieren
            }
        }

        private static async Task CopyFileAsync(string sourcePath, string targetPath, CancellationToken cancellationToken)
        {
            await using var src = File.OpenRead(sourcePath);
            await using var dst = File.Create(targetPath);
            await src.CopyToAsync(dst, cancellationToken);
        }

        private static string? GetRegisteredInstallDirectory()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(UninstallRegPath, writable: false);
                return key?.GetValue("InstallLocation") as string;
            }
            catch
            {
                return null;
            }
        }

        private static void DeleteFileIfExists(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Ignorieren
            }
        }

        private static void DeleteDirectoryIfExists(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
            }
            catch
            {
                // Ignorieren
            }
        }

        private static void ConfigureAutoStart(bool enable, string targetExe)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegPath, writable: true);
                if (key is null)
                {
                    return;
                }

                if (enable)
                {
                    key.SetValue("ZodiacSixLauncher", $"\"{targetExe}\"");
                }
                else
                {
                    key.DeleteValue("ZodiacSixLauncher", throwOnMissingValue: false);
                }
            }
            catch
            {
                // Registry-Zugriff ggf. eingeschränkt – nicht kritisch
            }
        }

        private static void RegisterWindowsUninstallEntry(string installDir, string targetExe)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(UninstallRegPath);
                key.SetValue("DisplayName", "ZODIAC SIX Launcher");
                key.SetValue("Publisher", "ZODIAC SIX Roleplay");
                key.SetValue("DisplayVersion", AppVersionInfo.Current);
                key.SetValue("InstallLocation", installDir);
                key.SetValue("DisplayIcon", targetExe);
                key.SetValue("UninstallString", $"\"{targetExe}\" --uninstall");
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            }
            catch
            {
                // Optionaler Eintrag
            }
        }

        private static void CreateWindowsShortcut(string shortcutPath, string targetPath, string workingDir, string description)
        {
            try
            {
                var link = (IShellLinkW)new ShellLink();
                link.SetPath(targetPath);
                link.SetWorkingDirectory(workingDir);
                link.SetDescription(description);
                link.SetIconLocation(targetPath, 0);
                ((IPersistFile)link).Save(shortcutPath, false);
            }
            catch
            {
                // Fallback falls COM in eingeschränkter Umgebung blockiert
            }
        }

        private static void NotifyShellIconsChanged()
        {
            try
            {
                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
            }
            catch
            {
                // Ignorieren
            }
        }

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLink
        {
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, IntPtr pfd, int fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
            void Resolve(IntPtr hwnd, int fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("0000010b-0000-0000-C000-000000000046")]
        private interface IPersistFile
        {
            void GetClassID(out Guid pClassID);
            [PreserveSig] int IsDirty();
            void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
            void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
            void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
            void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
        }
    }
}

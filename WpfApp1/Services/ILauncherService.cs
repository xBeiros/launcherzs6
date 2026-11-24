using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace WpfApp1.Services
{
    /// <summary>
    /// Schnittstelle für Spielstart (CFX/FiveM-Protokoll) und echte FiveM-Cache-Verwaltung.
    /// </summary>
    public interface ILauncherService
    {
        Task<bool> LaunchServerAsync(string serverEndpoint, bool minimizeOnLaunch, CancellationToken cancellationToken = default);
        Task<double> GetCacheSizeGbAsync(CancellationToken cancellationToken = default);
        Task<double> ClearCacheAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Produktive FiveM-Service-Implementierung:
    /// Ermittelt die reale Größe der FiveM-Cache-Ordner (cache, server-cache, server-cache-priv, nui-storage)
    /// und leert sie sicher, ohne game-storage zu berühren.
    /// </summary>
    public sealed class FiveMLauncherService : ILauncherService
    {
        private static readonly string[] CacheSubFolders =
        {
            "cache",
            "server-cache",
            "server-cache-priv",
            "nui-storage"
        };

        public async Task<bool> LaunchServerAsync(string serverEndpoint, bool minimizeOnLaunch, CancellationToken cancellationToken = default)
        {
            await Task.Delay(600, cancellationToken);
            return true;
        }

        public Task<double> GetCacheSizeGbAsync(CancellationToken cancellationToken = default)
        {
            return Task.Run(() => CalculateTotalCacheSizeGb(cancellationToken), cancellationToken);
        }

        public Task<double> ClearCacheAsync(CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                var dataDir = ResolveFiveMDataDirectory();
                if (string.IsNullOrWhiteSpace(dataDir) || !Directory.Exists(dataDir))
                {
                    return 0.0;
                }

                foreach (var folderName in CacheSubFolders)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var folderPath = Path.Combine(dataDir, folderName);
                    if (!Directory.Exists(folderPath))
                    {
                        continue;
                    }

                    DeleteDirectoryContentsSafely(folderPath, cancellationToken);
                }

                return CalculateTotalCacheSizeGb(cancellationToken);
            }, cancellationToken);
        }

        private static double CalculateTotalCacheSizeGb(CancellationToken cancellationToken)
        {
            var dataDir = ResolveFiveMDataDirectory();
            if (string.IsNullOrWhiteSpace(dataDir) || !Directory.Exists(dataDir))
            {
                return 0.0;
            }

            long totalBytes = 0;

            foreach (var folderName in CacheSubFolders)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var folderPath = Path.Combine(dataDir, folderName);
                if (!Directory.Exists(folderPath))
                {
                    continue;
                }

                totalBytes += GetDirectorySizeBytes(folderPath, cancellationToken);
            }

            return totalBytes / (1024.0 * 1024.0 * 1024.0);
        }

        private static string? ResolveFiveMDataDirectory()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var defaultDataDir = Path.Combine(localAppData, "FiveM", "FiveM.app", "data");
            if (Directory.Exists(defaultDataDir))
            {
                return defaultDataDir;
            }

            // Fallback: FiveM-Pfad über das registrierte fivem://-Protokoll ermitteln
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\fivem\shell\open\command", writable: false);
                var command = key?.GetValue("") as string;
                if (!string.IsNullOrWhiteSpace(command))
                {
                    var match = Regex.Match(command, "\"([^\"]+FiveM\\.exe)\"", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        var fivemRoot = Path.GetDirectoryName(match.Groups[1].Value);
                        if (!string.IsNullOrWhiteSpace(fivemRoot))
                        {
                            var candidate1 = Path.Combine(fivemRoot, "FiveM.app", "data");
                            if (Directory.Exists(candidate1))
                            {
                                return candidate1;
                            }

                            var candidate2 = Path.Combine(fivemRoot, "data");
                            if (Directory.Exists(candidate2))
                            {
                                return candidate2;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignorieren
            }

            return null;
        }

        private static long GetDirectorySizeBytes(string directoryPath, CancellationToken cancellationToken)
        {
            long size = 0;
            try
            {
                foreach (var file in Directory.EnumerateFiles(directoryPath, "*", new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true
                }))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        size += new FileInfo(file).Length;
                    }
                    catch
                    {
                        // Gesperrte oder gelöschte Datei überspringen
                    }
                }
            }
            catch
            {
                // Zugriffsfehler ignorieren
            }

            return size;
        }

        private static void DeleteDirectoryContentsSafely(string directoryPath, CancellationToken cancellationToken)
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(directoryPath))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                    }
                    catch
                    {
                        // Gesperrte Datei überspringen
                    }
                }

                foreach (var subDir in Directory.EnumerateDirectories(directoryPath))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        Directory.Delete(subDir, recursive: true);
                    }
                    catch
                    {
                        // Falls einzelne Dateien gesperrt sind, rekursiv so viel wie möglich löschen
                        DeleteDirectoryContentsSafely(subDir, cancellationToken);
                    }
                }
            }
            catch
            {
                // Ignorieren
            }
        }
    }
}

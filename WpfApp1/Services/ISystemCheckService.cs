using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using WpfApp1.Models;

namespace WpfApp1.Services
{
    /// <summary>
    /// Schnittstelle für lokale Systemprüfungen (Registry, Installationspfade, laufende Prozesse).
    /// </summary>
    public interface ISystemCheckService
    {
        Task<IReadOnlyList<SystemCheckItem>> RunSystemChecksAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Echte Windows-Systemprüfung für GTA V, FiveM, TeamSpeak 3, SaltyChat-Plugin und Discord.
    /// </summary>
    public sealed class WindowsSystemCheckService : ISystemCheckService
    {
        public Task<IReadOnlyList<SystemCheckItem>> RunSystemChecksAsync(CancellationToken cancellationToken = default)
        {
            return Task.Run<IReadOnlyList<SystemCheckItem>>(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                return new List<SystemCheckItem>
                {
                    CheckGtaV(),
                    CheckFiveM(),
                    CheckTeamSpeak(),
                    CheckSaltyChat(),
                    CheckDiscord()
                };
            }, cancellationToken);
        }

        private static SystemCheckItem CheckGtaV()
        {
            // 1. Rockstar Games Launcher Registry
            var rockstarPath = ReadRegistryString(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Rockstar Games\Grand Theft Auto V", "InstallFolder")
                               ?? ReadRegistryString(RegistryHive.LocalMachine, @"SOFTWARE\Rockstar Games\Grand Theft Auto V", "InstallFolder");
            if (HasGtaVExecutable(rockstarPath))
            {
                return new("Grand Theft Auto V", "Installiert · Rockstar", true, SystemCheckIconType.GtaSix);
            }

            // 2. Steam Registry & libraryfolders.vdf
            var steamPath = ReadRegistryString(RegistryHive.CurrentUser, @"Software\Valve\Steam", "SteamPath")
                            ?? ReadRegistryString(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath")
                            ?? ReadRegistryString(RegistryHive.LocalMachine, @"SOFTWARE\Valve\Steam", "InstallPath");

            if (!string.IsNullOrWhiteSpace(steamPath))
            {
                foreach (var lib in GetSteamLibraryFolders(steamPath))
                {
                    var candidate = Path.Combine(lib, "steamapps", "common", "Grand Theft Auto V");
                    if (HasGtaVExecutable(candidate))
                    {
                        return new("Grand Theft Auto V", "Installiert · Steam", true, SystemCheckIconType.GtaSix);
                    }

                    var enhancedCandidate = Path.Combine(lib, "steamapps", "common", "Grand Theft Auto V Enhanced");
                    if (HasGtaVExecutable(enhancedCandidate))
                    {
                        return new("Grand Theft Auto V", "Installiert · Steam", true, SystemCheckIconType.GtaSix);
                    }
                }
            }

            // 3. Epic Games Manifests
            var epicManifestsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Epic", "EpicGamesLauncher", "Data", "Manifests");
            if (Directory.Exists(epicManifestsDir))
            {
                try
                {
                    foreach (var itemFile in Directory.EnumerateFiles(epicManifestsDir, "*.item"))
                    {
                        var content = File.ReadAllText(itemFile);
                        if (content.Contains("Grand Theft Auto V", StringComparison.OrdinalIgnoreCase) ||
                            content.Contains("\"AppName\": \"9d2d0eb64d5c44529cece33fe2a46482\"", StringComparison.OrdinalIgnoreCase))
                        {
                            var match = Regex.Match(content, "\"InstallLocation\"\\s*:\\s*\"([^\"]+)\"");
                            if (match.Success)
                            {
                                var installLoc = match.Groups[1].Value.Replace("\\\\", "\\");
                                if (HasGtaVExecutable(installLoc))
                                {
                                    return new("Grand Theft Auto V", "Installiert · Epic Games", true, SystemCheckIconType.GtaSix);
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Zugriffsfehler ignorieren
                }
            }

            // 4. Häufige Standardpfade auf lokalen Laufwerken
            var commonSubPaths = new[]
            {
                @"Program Files\Rockstar Games\Grand Theft Auto V",
                @"Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V",
                @"Program Files\Epic Games\GTAV",
                @"Games\Grand Theft Auto V",
                @"SteamLibrary\steamapps\common\Grand Theft Auto V"
            };

            foreach (var drive in GetFixedDriveRoots())
            {
                foreach (var sub in commonSubPaths)
                {
                    if (HasGtaVExecutable(Path.Combine(drive, sub)))
                    {
                        return new("Grand Theft Auto V", "Installiert", true, SystemCheckIconType.GtaSix);
                    }
                }
            }

            return new("Grand Theft Auto V", "Nicht gefunden", false, SystemCheckIconType.GtaSix);
        }

        private static SystemCheckItem CheckFiveM()
        {
            var isRunning = IsProcessRunning("FiveM") || IsProcessRunning("FiveM_GTAProcess");
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var candidates = new[]
            {
                Path.Combine(localAppData, "FiveM", "FiveM.exe"),
                Path.Combine(localAppData, "FiveM", "FiveM.app", "FiveM.exe"),
                Path.Combine(localAppData, "FiveM", "FiveM.app", "CitizenFX.ini")
            };

            var found = isRunning || candidates.Any(File.Exists);

            if (!found)
            {
                var cfxProtocol = ReadRegistryString(RegistryHive.CurrentUser, @"Software\Classes\fivem\shell\open\command", "");
                if (!string.IsNullOrWhiteSpace(cfxProtocol))
                {
                    found = true;
                }
            }

            if (found)
            {
                return new("FiveM", isRunning ? "Installiert · Läuft" : "Installiert", true, SystemCheckIconType.SixM);
            }

            return new("FiveM", "Nicht gefunden", false, SystemCheckIconType.SixM);
        }

        private static SystemCheckItem CheckTeamSpeak()
        {
            var isRunning = IsProcessRunning("ts3client_win64") ||
                            IsProcessRunning("ts3client_win32") ||
                            IsProcessRunning("TeamSpeak");

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            var regPath = ReadRegistryString(RegistryHive.LocalMachine, @"SOFTWARE\TeamSpeak 3 Client", "")
                          ?? ReadRegistryString(RegistryHive.CurrentUser, @"Software\TeamSpeak 3 Client", "");

            var candidates = new[]
            {
                string.IsNullOrWhiteSpace(regPath) ? string.Empty : Path.Combine(regPath, "ts3client_win64.exe"),
                string.IsNullOrWhiteSpace(regPath) ? string.Empty : Path.Combine(regPath, "ts3client_win32.exe"),
                Path.Combine(programFiles, "TeamSpeak 3 Client", "ts3client_win64.exe"),
                Path.Combine(programFilesX86, "TeamSpeak 3 Client", "ts3client_win32.exe"),
                Path.Combine(localAppData, "TeamSpeak 3 Client", "ts3client_win64.exe"),
                Path.Combine(localAppData, "TeamSpeak", "TeamSpeak.exe")
            };

            var found = isRunning
                        || candidates.Any(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p))
                        || Directory.Exists(Path.Combine(appData, "TS3Client"));

            if (found)
            {
                return new("TeamSpeak 3", isRunning ? "Installiert · Läuft" : "Installiert", true, SystemCheckIconType.Teamspeak);
            }

            return new("TeamSpeak 3", "Nicht gefunden", false, SystemCheckIconType.Teamspeak);
        }

        private static SystemCheckItem CheckSaltyChat()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            var pluginDirs = new[]
            {
                Path.Combine(appData, "TS3Client", "plugins"),
                Path.Combine(localAppData, "TeamSpeak 3 Client", "plugins"),
                Path.Combine(programFiles, "TeamSpeak 3 Client", "plugins")
            };

            foreach (var dir in pluginDirs)
            {
                if (!Directory.Exists(dir))
                {
                    continue;
                }

                try
                {
                    var hasDll = Directory.EnumerateFiles(dir, "*saltychat*.dll", SearchOption.TopDirectoryOnly).Any();
                    var hasFolder = Directory.Exists(Path.Combine(dir, "SaltyChat"));
                    if (hasDll || hasFolder)
                    {
                        return new("SaltyChat", "Plugin installiert", true, SystemCheckIconType.Saltychat);
                    }
                }
                catch
                {
                    // Ignorieren
                }
            }

            return new("SaltyChat", "Plugin nicht gefunden", false, SystemCheckIconType.Saltychat);
        }

        private static SystemCheckItem CheckDiscord()
        {
            var isRunning = IsProcessRunning("Discord") ||
                            IsProcessRunning("DiscordPTB") ||
                            IsProcessRunning("DiscordCanary");

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var candidates = new[]
            {
                Path.Combine(localAppData, "Discord", "Update.exe"),
                Path.Combine(localAppData, "DiscordPTB", "Update.exe"),
                Path.Combine(localAppData, "DiscordCanary", "Update.exe")
            };

            var found = isRunning
                        || candidates.Any(File.Exists)
                        || HasUninstallEntry("Discord");

            if (found)
            {
                return new("Discord", isRunning ? "Installiert · Läuft" : "Installiert", true, SystemCheckIconType.Discord);
            }

            return new("Discord", "Nicht gefunden", false, SystemCheckIconType.Discord);
        }

        private static bool HasGtaVExecutable(string? folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                return false;
            }

            return File.Exists(Path.Combine(folder, "GTA5.exe")) ||
                   File.Exists(Path.Combine(folder, "GTA5_Enhanced.exe")) ||
                   File.Exists(Path.Combine(folder, "PlayGTAV.exe"));
        }

        private static IEnumerable<string> GetSteamLibraryFolders(string steamRoot)
        {
            var libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { steamRoot };
            var vdfPath = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");

            if (File.Exists(vdfPath))
            {
                try
                {
                    var text = File.ReadAllText(vdfPath);
                    foreach (Match match in Regex.Matches(text, "\"path\"\\s+\"([^\"]+)\""))
                    {
                        var path = match.Groups[1].Value.Replace("\\\\", "\\");
                        if (!string.IsNullOrWhiteSpace(path))
                        {
                            libraries.Add(path);
                        }
                    }
                }
                catch
                {
                    // Ignorieren
                }
            }

            return libraries;
        }

        private static IEnumerable<string> GetFixedDriveRoots()
        {
            try
            {
                return DriveInfo.GetDrives()
                    .Where(d => d.DriveType == DriveType.Fixed && d.IsReady)
                    .Select(d => d.RootDirectory.FullName)
                    .ToList();
            }
            catch
            {
                return new[] { @"C:\" };
            }
        }

        private static bool IsProcessRunning(string processName)
        {
            try
            {
                return Process.GetProcessesByName(processName).Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private static string? ReadRegistryString(RegistryHive hive, string subKeyPath, string valueName)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
                using var key = baseKey.OpenSubKey(subKeyPath, writable: false);
                return key?.GetValue(valueName) as string;
            }
            catch
            {
                return null;
            }
        }

        private static bool HasUninstallEntry(string nameContains)
        {
            var paths = new[]
            {
                (RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Uninstall"),
                (RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
                (RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall")
            };

            foreach (var (hive, path) in paths)
            {
                try
                {
                    using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
                    using var root = baseKey.OpenSubKey(path, writable: false);
                    if (root is null)
                    {
                        continue;
                    }

                    foreach (var subName in root.GetSubKeyNames())
                    {
                        using var sub = root.OpenSubKey(subName, writable: false);
                        var displayName = sub?.GetValue("DisplayName") as string;
                        if (!string.IsNullOrWhiteSpace(displayName) &&
                            displayName.Contains(nameContains, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
                catch
                {
                    // Ignorieren
                }
            }

            return false;
        }
    }
}

using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace WpfApp1.Services
{
    public sealed class LauncherUserSettings
    {
        public bool MinimizeOnLaunch { get; set; } = true;
        public DateTime? LastCacheClearedUtc { get; set; }
    }

    /// <summary>
    /// Persistiert Launcher-Einstellungen lokal (%AppData%\ZodiacSixLauncher\settings.json)
    /// und synchronisiert den echten Windows-Autostart-Eintrag in der Registry.
    /// </summary>
    public interface ILauncherSettingsService
    {
        bool GetAutoStartEnabled();
        void SetAutoStartEnabled(bool enabled);
        bool GetMinimizeOnLaunchEnabled();
        void SetMinimizeOnLaunchEnabled(bool enabled);
        DateTime? GetLastCacheClearedUtc();
        void SetLastCacheClearedUtc(DateTime utcTime);
    }

    public sealed class LauncherSettingsService : ILauncherSettingsService
    {
        private const string RunRegPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValueName = "ZodiacSixLauncher";
        private const string AppFolderName = "ZodiacSixLauncher";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        private readonly string _settingsFilePath;

        public LauncherSettingsService()
        {
            var appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AppFolderName);
            _settingsFilePath = Path.Combine(appDataDir, "settings.json");
        }

        public bool GetAutoStartEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegPath, writable: false);
                var value = key?.GetValue(RunValueName) as string;
                return !string.IsNullOrWhiteSpace(value);
            }
            catch
            {
                return false;
            }
        }

        public void SetAutoStartEnabled(bool enabled)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegPath, writable: true);
                if (key is null)
                {
                    return;
                }

                if (enabled)
                {
                    var exePath = Environment.ProcessPath;
                    if (string.IsNullOrWhiteSpace(exePath))
                    {
                        exePath = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            AppFolderName,
                            "ZodiacSix-Launcher.exe");
                    }

                    key.SetValue(RunValueName, $"\"{exePath}\"");
                }
                else
                {
                    key.DeleteValue(RunValueName, throwOnMissingValue: false);
                }
            }
            catch
            {
                // Registry-Zugriff ggf. eingeschränkt
            }
        }

        public bool GetMinimizeOnLaunchEnabled()
        {
            return LoadSettings().MinimizeOnLaunch;
        }

        public void SetMinimizeOnLaunchEnabled(bool enabled)
        {
            var settings = LoadSettings();
            settings.MinimizeOnLaunch = enabled;
            SaveSettings(settings);
        }

        public DateTime? GetLastCacheClearedUtc()
        {
            return LoadSettings().LastCacheClearedUtc;
        }

        public void SetLastCacheClearedUtc(DateTime utcTime)
        {
            var settings = LoadSettings();
            settings.LastCacheClearedUtc = utcTime;
            SaveSettings(settings);
        }

        private LauncherUserSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var parsed = JsonSerializer.Deserialize<LauncherUserSettings>(json, JsonOptions);
                    if (parsed is not null)
                    {
                        return parsed;
                    }
                }
            }
            catch
            {
                // Bei defekter Datei Standardwerte verwenden
            }

            return new LauncherUserSettings();
        }

        private void SaveSettings(LauncherUserSettings settings)
        {
            try
            {
                var dir = Path.GetDirectoryName(_settingsFilePath);
                if (!string.IsNullOrWhiteSpace(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var json = JsonSerializer.Serialize(settings, JsonOptions);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch
            {
                // Schreibfehler ignorieren
            }
        }
    }
}

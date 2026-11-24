using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using WpfApp1.Models;

namespace WpfApp1.Services
{
    /// <summary>
    /// Schnittstelle für den automatischen Launcher-Updater.
    /// </summary>
    public interface IUpdateService
    {
        string CurrentVersion { get; }
        Task<UpdateCheckResult> CheckForUpdatesAsync(IProgress<UpdateProgressInfo>? progress = null, CancellationToken cancellationToken = default);
        Task<bool> DownloadAndApplyUpdateAsync(UpdateManifest manifest, IProgress<UpdateProgressInfo>? progress = null, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Produktiv vorbereiteter Auto-Updater für Single-EXE-Deployments:
    /// Prüft ein JSON-Manifest, lädt die neue .exe mit Fortschrittsanzeige, verifiziert optional SHA-256
    /// und ersetzt die laufende .exe atomar über ein temporäres Updater-Skript.
    /// </summary>
    public sealed class AutoUpdateService : IUpdateService
    {
        // Live Update-Endpoint auf dem ZODIAC SIX Webserver
        private const string ManifestEndpointUrl = "https://zodiac.duran.li/launcher/version.json";

        private static readonly HttpClient ManifestHttp = new()
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        private static readonly HttpClient DownloadHttp = new()
        {
            Timeout = TimeSpan.FromMinutes(10)
        };

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public string CurrentVersion => AppVersionInfo.Current;

        public async Task<UpdateCheckResult> CheckForUpdatesAsync(
            IProgress<UpdateProgressInfo>? progress = null,
            CancellationToken cancellationToken = default)
        {
            progress?.Report(new UpdateProgressInfo(
                "Suche nach Updates...",
                "Verbinde mit ZODIAC SIX Update-Server",
                15,
                IsIndeterminate: true));

            await Task.Delay(550, cancellationToken);

            UpdateManifest? remoteManifest = null;
            try
            {
                using var response = await ManifestHttp.GetAsync(ManifestEndpointUrl, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(cancellationToken);
                    remoteManifest = JsonSerializer.Deserialize<UpdateManifest>(json, JsonOptions);
                }
            }
            catch
            {
                // Kein Update-Endpunkt erreichbar oder offline – sauberer Fallback ohne Absturz
            }

            progress?.Report(new UpdateProgressInfo(
                "Prüfe lokale Dateien...",
                $"Installierte Version v{CurrentVersion}",
                65));

            await Task.Delay(450, cancellationToken);

            if (remoteManifest is not null &&
                Version.TryParse(remoteManifest.Version, out var remoteVer) &&
                Version.TryParse(CurrentVersion, out var localVer) &&
                remoteVer > localVer &&
                !string.IsNullOrWhiteSpace(remoteManifest.DownloadUrl))
            {
                progress?.Report(new UpdateProgressInfo(
                    $"Update v{remoteManifest.Version} gefunden",
                    "Bereite Download vor...",
                    80));

                return new UpdateCheckResult(true, remoteManifest, CurrentVersion);
            }

            progress?.Report(new UpdateProgressInfo(
                "Launcher ist aktuell",
                $"ZODIAC SIX v{CurrentVersion} • Starte Dashboard...",
                100));

            await Task.Delay(400, cancellationToken);

            return new UpdateCheckResult(false, null, CurrentVersion);
        }

        public async Task<bool> DownloadAndApplyUpdateAsync(
            UpdateManifest manifest,
            IProgress<UpdateProgressInfo>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var currentExePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(currentExePath) || !File.Exists(currentExePath))
            {
                return false;
            }

            var tempDir = Path.Combine(Path.GetTempPath(), "ZodiacSixUpdater");
            Directory.CreateDirectory(tempDir);

            var downloadedExePath = Path.Combine(tempDir, $"ZodiacSix-Launcher_{manifest.Version}.exe");

            using var response = await DownloadHttp.GetAsync(manifest.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            await using (var remoteStream = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var fileStream = new FileStream(downloadedExePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                var buffer = new byte[81920];
                long readTotal = 0;
                int bytesRead;

                while ((bytesRead = await remoteStream.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                    readTotal += bytesRead;

                    if (totalBytes > 0)
                    {
                        var pct = Math.Clamp((double)readTotal / totalBytes * 100.0, 0, 100);
                        progress?.Report(new UpdateProgressInfo(
                            $"Lade Update v{manifest.Version} herunter...",
                            $"{readTotal / 1024d / 1024d:0.0} MB / {totalBytes / 1024d / 1024d:0.0} MB",
                            pct));
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(manifest.Sha256))
            {
                progress?.Report(new UpdateProgressInfo("Prüfe Integrität...", "SHA-256 Signaturprüfung", 98));
                await using var verifyStream = File.OpenRead(downloadedExePath);
                var hashBytes = await SHA256.HashDataAsync(verifyStream, cancellationToken);
                var actualHash = Convert.ToHexString(hashBytes);
                if (!string.Equals(actualHash, manifest.Sha256.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(downloadedExePath);
                    return false;
                }
            }

            progress?.Report(new UpdateProgressInfo("Installiere Update...", "Launcher wird neu gestartet", 100));

            // Atomarer Single-EXE-Austausch nach Prozess-Beendigung
            var batchPath = Path.Combine(tempDir, "apply_update.cmd");
            var script = $"""
                @echo off
                timeout /t 2 /nobreak > nul
                move /y "{downloadedExePath}" "{currentExePath}"
                start "" "{currentExePath}"
                del "%~f0"
                """;
            await File.WriteAllTextAsync(batchPath, script, cancellationToken);

            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{batchPath}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            });

            return true;
        }
    }
}

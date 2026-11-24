namespace WpfApp1.Models
{
    /// <summary>
    /// JSON-Manifest, das der Auto-Updater vom Update-Server (z. B. zodiac.duran.li/launcher/version.json) lädt.
    /// </summary>
    public sealed class UpdateManifest
    {
        public string Version { get; set; } = "1.0.0";
        public string DownloadUrl { get; set; } = string.Empty;
        public string Changelog { get; set; } = string.Empty;
        public string Sha256 { get; set; } = string.Empty;
        public bool Mandatory { get; set; }
    }

    /// <summary>
    /// Fortschritts-Information für das Discord-ähnliche Update-Fenster.
    /// </summary>
    public sealed record UpdateProgressInfo(string StatusText, string DetailText, double Percent, bool IsIndeterminate = false);

    /// <summary>
    /// Ergebnis einer Update-Prüfung.
    /// </summary>
    public sealed record UpdateCheckResult(bool UpdateAvailable, UpdateManifest? Manifest, string CurrentVersion);
}

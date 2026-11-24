using System.Reflection;

namespace WpfApp1.Services
{
    /// <summary>
    /// Zentrale globale Versionsquelle für den gesamten Launcher.
    /// Der Wert wird beim Build automatisch aus version.json in die Assembly-Metadaten geschrieben.
    /// </summary>
    public static class AppVersionInfo
    {
        public static string Current { get; } = ResolveVersion();

        public static string Display => $"v{Current}";

        private static string ResolveVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();

            var infoVersion = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            if (!string.IsNullOrWhiteSpace(infoVersion))
            {
                // Eventuellen Git-Metadata-Suffix (+sha) entfernen
                var plusIndex = infoVersion.IndexOf('+');
                var clean = plusIndex >= 0 ? infoVersion[..plusIndex] : infoVersion;
                if (!string.IsNullOrWhiteSpace(clean))
                {
                    return clean.Trim();
                }
            }

            var asmVersion = assembly.GetName().Version;
            return asmVersion is null ? "1.0.0" : $"{asmVersion.Major}.{asmVersion.Minor}.{asmVersion.Build}";
        }
    }
}

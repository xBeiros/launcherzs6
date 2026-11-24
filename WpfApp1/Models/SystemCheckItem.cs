using WpfApp1.ViewModels;

namespace WpfApp1.Models
{
    /// <summary>
    /// Definiert den Icon-Typ für einen Systemcheck-Eintrag im Launcher.
    /// </summary>
    public enum SystemCheckIconType
    {
        GtaSix,
        SixM,
        Teamspeak,
        Saltychat,
        Discord
    }

    /// <summary>
    /// Repräsentiert einen einzelnen Prüfpunkt in der Systemcheck-Liste (z. B. GTA VI, SIXM, Teamspeak 3).
    /// </summary>
    public sealed class SystemCheckItem : ObservableObject
    {
        private string _name = string.Empty;
        private string _statusText = string.Empty;
        private bool _isInstalled;
        private SystemCheckIconType _iconType;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public bool IsInstalled
        {
            get => _isInstalled;
            set => SetProperty(ref _isInstalled, value);
        }

        public SystemCheckIconType IconType
        {
            get => _iconType;
            set => SetProperty(ref _iconType, value);
        }

        public SystemCheckItem()
        {
        }

        public SystemCheckItem(string name, string statusText, bool isInstalled, SystemCheckIconType iconType)
        {
            Name = name;
            StatusText = statusText;
            IsInstalled = isInstalled;
            IconType = iconType;
        }
    }
}

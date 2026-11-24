using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using WpfApp1.Models;
using WpfApp1.Services;

namespace WpfApp1.ViewModels
{
    /// <summary>
    /// Repräsentiert einen einzelnen Slide im Hero-Banner-Slider.
    /// </summary>
    public sealed class HeroSlideItem : ObservableObject
    {
        private bool _isActive;
        private double _progressRatio;

        public int Index { get; }
        public string NumberLabel => $"0{Index + 1}";
        public string ImageUri { get; }
        public string Coordinates { get; }
        public string Location { get; }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public double ProgressRatio
        {
            get => _progressRatio;
            set => SetProperty(ref _progressRatio, value);
        }

        public HeroSlideItem(int index, string imageUri, string coordinates, string location, bool isActive = false)
        {
            Index = index;
            ImageUri = imageUri;
            Coordinates = coordinates;
            Location = location;
            _isActive = isActive;
        }
    }

    /// <summary>
    /// ViewModel für die Startseite ("DashboardView") mit Server-Status, Systemchecks,
    /// automatischem Hero-Slider, Launcher-Einstellungen und Cache-Verwaltung.
    /// </summary>
    public sealed class DashboardViewModel : ObservableObject
    {
        private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

        private readonly ISystemCheckService _systemCheckService;
        private readonly ILauncherService _launcherService;
        private readonly ILauncherSettingsService _settingsService;
        private readonly Action<string>? _reportStatus;

        // Server-Status
        private string _serverName = "ZODIACSIX";
        private string _serverSubtitle = "LEONIDA · DEUTSCH · MIDCORE";
        private string _serverStatusText = "In Vorbereitung";
        private string _serverConnectionNote = "Keine Live-Verbindung";
        private string _playersValue = "—";
        private string _playersMax = "—";
        private string _latencyValue = "—";

        // Hero-Banner & Slider
        private int _activeSlideIndex;
        private string _heroEyebrow = "GRAND THEFT AUTO VI ROLEPLAY";
        private string _heroHeadlineTop = "KEINE KULISSE.";
        private string _heroHeadlineAccent = "DEIN LEBEN.";
        private string _heroSubtitleLine1 = "Jede Straße ein neuer Anfang.";
        private string _heroSubtitleLine2 = "Schreib deine Geschichte in Leonida.";
        private string _launchButtonTitle = "LEONIDA BETRETEN";
        private string _launchButtonSubtitle = "STARTSEQUENZ ANSEHEN";
        private bool _isLaunching;

        // Systemchecks
        private bool _isCheckingSystem;

        // Launcher-Einstellungen
        private bool _isAutoStartEnabled;
        private bool _isMinimizeOnLaunchEnabled = true;

        // Cache-Verwaltung
        private double _cacheSizeGb;
        private double _cacheMaxGb = 4.00;
        private string _lastClearedText = "Noch nie";
        private bool _isClearingCache;

        public DashboardViewModel(
            ISystemCheckService systemCheckService,
            ILauncherService launcherService,
            Action<string>? reportStatus = null,
            ILauncherSettingsService? settingsService = null)
        {
            _systemCheckService = systemCheckService ?? throw new ArgumentNullException(nameof(systemCheckService));
            _launcherService = launcherService ?? throw new ArgumentNullException(nameof(launcherService));
            _settingsService = settingsService ?? new LauncherSettingsService();
            _reportStatus = reportStatus;

            // Gespeicherte Einstellungen & echten Windows-Autostart-Status laden
            _isAutoStartEnabled = _settingsService.GetAutoStartEnabled();
            _isMinimizeOnLaunchEnabled = _settingsService.GetMinimizeOnLaunchEnabled();
            _lastClearedText = FormatLastCleared(_settingsService.GetLastCacheClearedUtc());

            HeroSlides = new ObservableCollection<HeroSlideItem>
            {
                new(0, "pack://application:,,,/Assets/ZODIACSIX_Main_Background_Dark_4K.png", "25°46' N · 80°12' W", "VICE CITY / LEONIDA", isActive: true),
                new(1, "pack://application:,,,/Assets/social_4.png", "25°47' N · 80°08' W", "OCEAN DRIVE / BAYFRONT"),
                new(2, "pack://application:,,,/Assets/social_3.png", "25°45' N · 80°11' W", "DOWNTOWN / NIGHTLIFE"),
                new(3, "pack://application:,,,/Assets/social_1.png", "25°48' N · 80°14' W", "ZODIAC LOUNGE / CLUB")
            };

            SystemChecks = new ObservableCollection<SystemCheckItem>
            {
                new("Grand Theft Auto V", "Prüfe System...", false, SystemCheckIconType.GtaSix),
                new("FiveM", "Prüfe System...", false, SystemCheckIconType.SixM),
                new("TeamSpeak 3", "Prüfe System...", false, SystemCheckIconType.Teamspeak),
                new("SaltyChat", "Prüfe System...", false, SystemCheckIconType.Saltychat),
                new("Discord", "Prüfe System...", false, SystemCheckIconType.Discord)
            };

            RefreshSystemChecksCommand = new AsyncRelayCommand(RefreshSystemChecksAsync);
            LaunchGameCommand = new AsyncRelayCommand(LaunchGameAsync, () => !IsLaunching);
            ClearCacheCommand = new AsyncRelayCommand(ClearCacheAsync, () => !IsClearingCache && CacheSizeGb > 0.001);
            NextHeroSlideCommand = new RelayCommand(AdvanceHeroSlide);
            SelectHeroSlideCommand = new RelayCommand(param =>
            {
                if (param is int idx)
                {
                    SelectHeroSlide(idx);
                }
            });

            // Sofort beim Start echten Windows-Systemcheck & FiveM-Cache-Größe ermitteln
            _ = RefreshSystemChecksAsync();
            _ = RefreshCacheSizeAsync();
        }

        // --- Server-Karte ---

        public string ServerName
        {
            get => _serverName;
            set => SetProperty(ref _serverName, value);
        }

        public string ServerSubtitle
        {
            get => _serverSubtitle;
            set => SetProperty(ref _serverSubtitle, value);
        }

        public string ServerStatusText
        {
            get => _serverStatusText;
            set => SetProperty(ref _serverStatusText, value);
        }

        public string ServerConnectionNote
        {
            get => _serverConnectionNote;
            set => SetProperty(ref _serverConnectionNote, value);
        }

        public string PlayersValue
        {
            get => _playersValue;
            set => SetProperty(ref _playersValue, value);
        }

        public string PlayersMax
        {
            get => _playersMax;
            set => SetProperty(ref _playersMax, value);
        }

        public string LatencyValue
        {
            get => _latencyValue;
            set => SetProperty(ref _latencyValue, value);
        }

        // --- Systemcheck-Liste ---

        public ObservableCollection<SystemCheckItem> SystemChecks { get; }

        public bool IsCheckingSystem
        {
            get => _isCheckingSystem;
            private set => SetProperty(ref _isCheckingSystem, value);
        }

        // --- Hero-Banner & Slider ---

        public ObservableCollection<HeroSlideItem> HeroSlides { get; }

        public int ActiveSlideIndex => _activeSlideIndex;
        public HeroSlideItem ActiveSlide => HeroSlides[_activeSlideIndex];
        public string ActiveHeroImageUri => ActiveSlide.ImageUri;
        public string ActiveSlideNumber => ActiveSlide.NumberLabel;
        public string TotalSlidesLabel => $"/ 0{HeroSlides.Count}";
        public string HeroCoordinates => ActiveSlide.Coordinates;
        public string HeroLocation => ActiveSlide.Location;

        public string HeroEyebrow
        {
            get => _heroEyebrow;
            set => SetProperty(ref _heroEyebrow, value);
        }

        public string HeroHeadlineTop
        {
            get => _heroHeadlineTop;
            set => SetProperty(ref _heroHeadlineTop, value);
        }

        public string HeroHeadlineAccent
        {
            get => _heroHeadlineAccent;
            set => SetProperty(ref _heroHeadlineAccent, value);
        }

        public string HeroSubtitleLine1
        {
            get => _heroSubtitleLine1;
            set => SetProperty(ref _heroSubtitleLine1, value);
        }

        public string HeroSubtitleLine2
        {
            get => _heroSubtitleLine2;
            set => SetProperty(ref _heroSubtitleLine2, value);
        }

        public string LaunchButtonTitle
        {
            get => _launchButtonTitle;
            set => SetProperty(ref _launchButtonTitle, value);
        }

        public string LaunchButtonSubtitle
        {
            get => _launchButtonSubtitle;
            set => SetProperty(ref _launchButtonSubtitle, value);
        }

        public bool IsLaunching
        {
            get => _isLaunching;
            private set => SetProperty(ref _isLaunching, value);
        }

        // --- Launcher-Einstellungen ---

        public bool IsAutoStartEnabled
        {
            get => _isAutoStartEnabled;
            set
            {
                if (SetProperty(ref _isAutoStartEnabled, value))
                {
                    _settingsService.SetAutoStartEnabled(value);
                    _reportStatus?.Invoke(value ? "Autostart mit Windows aktiviert." : "Autostart deaktiviert.");
                }
            }
        }

        public bool IsMinimizeOnLaunchEnabled
        {
            get => _isMinimizeOnLaunchEnabled;
            set
            {
                if (SetProperty(ref _isMinimizeOnLaunchEnabled, value))
                {
                    _settingsService.SetMinimizeOnLaunchEnabled(value);
                    _reportStatus?.Invoke(value
                        ? "Beim Spielstart minimieren aktiviert."
                        : "Beim Spielstart minimieren deaktiviert.");
                }
            }
        }

        // --- Cache-Verwaltung ---

        public double CacheSizeGb
        {
            get => _cacheSizeGb;
            set
            {
                if (SetProperty(ref _cacheSizeGb, value))
                {
                    OnPropertyChanged(nameof(CacheSizeNumber));
                    OnPropertyChanged(nameof(CacheUsagePercent));
                }
            }
        }

        public double CacheMaxGb
        {
            get => _cacheMaxGb;
            set
            {
                if (SetProperty(ref _cacheMaxGb, value))
                {
                    OnPropertyChanged(nameof(CacheUsagePercent));
                }
            }
        }

        public string CacheSizeNumber => CacheSizeGb.ToString("0.00", GermanCulture);

        public string LastClearedText
        {
            get => _lastClearedText;
            set => SetProperty(ref _lastClearedText, value);
        }

        public double CacheUsagePercent => CacheMaxGb <= 0 ? 0 : Math.Min(100, (CacheSizeGb / CacheMaxGb) * 100.0);

        public bool IsClearingCache
        {
            get => _isClearingCache;
            private set => SetProperty(ref _isClearingCache, value);
        }

        // --- Commands ---

        public ICommand RefreshSystemChecksCommand { get; }
        public ICommand LaunchGameCommand { get; }
        public ICommand ClearCacheCommand { get; }
        public ICommand NextHeroSlideCommand { get; }
        public ICommand SelectHeroSlideCommand { get; }

        public void AdvanceHeroSlide()
        {
            SelectHeroSlide((_activeSlideIndex + 1) % HeroSlides.Count);
        }

        public void SelectHeroSlide(int index)
        {
            if (index < 0 || index >= HeroSlides.Count || index == _activeSlideIndex)
            {
                return;
            }

            HeroSlides[_activeSlideIndex].IsActive = false;
            HeroSlides[_activeSlideIndex].ProgressRatio = 0;
            _activeSlideIndex = index;
            HeroSlides[_activeSlideIndex].ProgressRatio = 0;
            HeroSlides[_activeSlideIndex].IsActive = true;

            OnPropertyChanged(nameof(ActiveSlideIndex));
            OnPropertyChanged(nameof(ActiveSlide));
            OnPropertyChanged(nameof(ActiveHeroImageUri));
            OnPropertyChanged(nameof(ActiveSlideNumber));
            OnPropertyChanged(nameof(HeroCoordinates));
            OnPropertyChanged(nameof(HeroLocation));
        }

        private async Task RefreshSystemChecksAsync()
        {
            IsCheckingSystem = true;
            _reportStatus?.Invoke("Systemkomponenten werden überprüft...");

            try
            {
                var results = await _systemCheckService.RunSystemChecksAsync();
                SystemChecks.Clear();
                foreach (var item in results)
                {
                    SystemChecks.Add(item);
                }

                _reportStatus?.Invoke("Systemprüfung abgeschlossen.");
            }
            finally
            {
                IsCheckingSystem = false;
            }
        }

        private async Task LaunchGameAsync()
        {
            IsLaunching = true;
            LaunchButtonTitle = "VERBINDE...";
            LaunchButtonSubtitle = "SIXM WIRD GESTARTET";
            _reportStatus?.Invoke("Starte SIXM-Startsequenz...");

            try
            {
                await _launcherService.LaunchServerAsync("cfx.zodiacsix.de", IsMinimizeOnLaunchEnabled);
                _reportStatus?.Invoke("Startsequenz bereit.");
            }
            finally
            {
                LaunchButtonTitle = "LEONIDA BETRETEN";
                LaunchButtonSubtitle = "STARTSEQUENZ ANSEHEN";
                IsLaunching = false;
            }
        }

        private async Task RefreshCacheSizeAsync()
        {
            try
            {
                var sizeGb = await _launcherService.GetCacheSizeGbAsync();
                CacheSizeGb = sizeGb;
                CacheMaxGb = Math.Max(4.0, Math.Ceiling(sizeGb));
                CommandManager.InvalidateRequerySuggested();
            }
            catch
            {
                CacheSizeGb = 0.0;
            }
        }

        private async Task ClearCacheAsync()
        {
            IsClearingCache = true;
            _reportStatus?.Invoke("FiveM-Cache wird geleert...");

            try
            {
                var remainingSize = await _launcherService.ClearCacheAsync();
                CacheSizeGb = remainingSize;
                CacheMaxGb = Math.Max(4.0, Math.Ceiling(remainingSize));

                var nowUtc = DateTime.UtcNow;
                _settingsService.SetLastCacheClearedUtc(nowUtc);
                LastClearedText = "Gerade eben";
                _reportStatus?.Invoke("FiveM-Cache erfolgreich geleert.");
            }
            finally
            {
                IsClearingCache = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private static string FormatLastCleared(DateTime? utcTime)
        {
            if (utcTime is null)
            {
                return "Noch nie";
            }

            var local = utcTime.Value.ToLocalTime();
            return local.Date == DateTime.Now.Date
                ? $"Heute, {local:HH:mm}"
                : local.ToString("dd.MM.yyyy", GermanCulture);
        }
    }
}

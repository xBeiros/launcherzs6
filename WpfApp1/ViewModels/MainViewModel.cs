using System.Windows.Input;
using WpfApp1.Services;

namespace WpfApp1.ViewModels
{
    /// <summary>
    /// Identifiziert die Haupt-Navigationstabs des Launchers.
    /// </summary>
    public enum NavigationTab
    {
        Start,
        Neuigkeiten,
        Einstellungen
    }

    /// <summary>
    /// Platzhalter-ViewModel für Tabs, die in späteren Ausbaustufen folgen.
    /// </summary>
    public sealed class PlaceholderPageViewModel : ObservableObject
    {
        public string Title { get; }
        public string Subtitle { get; }

        public PlaceholderPageViewModel(string title, string subtitle)
        {
            Title = title;
            Subtitle = subtitle;
        }
    }

    /// <summary>
    /// Haupt-ViewModel für das MainWindow: steuert Navigation, aktive View, Benutzerprofil und Footer.
    /// </summary>
    public sealed class MainViewModel : ObservableObject
    {
        private NavigationTab _selectedTab = NavigationTab.Start;
        private object _currentView;
        private string _statusMessage = "ZODIACSIX LAUNCHER";

        public MainViewModel()
            : this(new WindowsSystemCheckService(), new FiveMLauncherService())
        {
        }

        public MainViewModel(ISystemCheckService systemCheckService, ILauncherService launcherService)
        {
            DashboardViewModel = new DashboardViewModel(
                systemCheckService,
                launcherService,
                status => StatusMessage = status);

            NewsViewModel = new NewsViewModel();
            _currentView = DashboardViewModel;

            NavigateCommand = new RelayCommand(param =>
            {
                if (param is NavigationTab tab)
                {
                    SelectedTab = tab;
                }
                else if (param is string tabName && Enum.TryParse<NavigationTab>(tabName, out var parsedTab))
                {
                    SelectedTab = parsedTab;
                }
            });

            DiscordLoginCommand = new RelayCommand(() =>
            {
                StatusMessage = "Discord-Login wird vorbereitet...";
            });
        }

        public DashboardViewModel DashboardViewModel { get; }
        public NewsViewModel NewsViewModel { get; }

        // --- Navigation ---

        public NavigationTab SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (SetProperty(ref _selectedTab, value))
                {
                    UpdateCurrentView(value);
                    OnPropertyChanged(nameof(IsStartSelected));
                    OnPropertyChanged(nameof(IsNeuigkeitenSelected));
                    OnPropertyChanged(nameof(IsEinstellungenSelected));
                }
            }
        }

        public bool IsStartSelected => SelectedTab == NavigationTab.Start;
        public bool IsNeuigkeitenSelected => SelectedTab == NavigationTab.Neuigkeiten;
        public bool IsEinstellungenSelected => SelectedTab == NavigationTab.Einstellungen;

        public object CurrentView
        {
            get => _currentView;
            private set => SetProperty(ref _currentView, value);
        }

        public ICommand NavigateCommand { get; }
        public ICommand DiscordLoginCommand { get; }

        // --- Discord-Login (rechts in der Navigationsleiste) ---

        public string DiscordLoginLabel => "Mit Discord einloggen";
        public string DiscordStatusText => "ACCOUNT VERKNÜPFEN";

        // --- Footer ---

        public string FooterBrand => "ZODIACSIX LAUNCHER";
        public string FooterConcept => AppVersionInfo.Display;
        public string FooterTagline => "DEINE STADT. DEINE GESCHICHTE.";
        public string FooterPreviewLink => "zodiac.duran.li";

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private void UpdateCurrentView(NavigationTab tab)
        {
            CurrentView = tab switch
            {
                NavigationTab.Start => DashboardViewModel,
                NavigationTab.Neuigkeiten => NewsViewModel,
                NavigationTab.Einstellungen => new PlaceholderPageViewModel(
                    "Einstellungen",
                    "Erweiterte Pfad-, Audio- und Startparameter-Konfigurationen folgen im nächsten Schritt."),
                _ => DashboardViewModel
            };
        }
    }
}

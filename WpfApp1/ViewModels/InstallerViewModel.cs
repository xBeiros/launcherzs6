using System.Windows.Input;
using WpfApp1.Services;

namespace WpfApp1.ViewModels
{
    /// <summary>
    /// ViewModel für den modernen GTA VI / ZODIAC SIX Single-EXE-Installer.
    /// </summary>
    public sealed class InstallerViewModel : ObservableObject
    {
        private readonly IInstallService _installService;

        private string _installPath;
        private bool _createDesktopShortcut = true;
        private bool _createStartMenuShortcut = true;
        private bool _enableAutoStart;
        private bool _isInstalling;
        private bool _isCompleted;
        private string _statusTitle = "Bereit für Leonida";
        private string _statusDetail = "Einzeldatei-Installation • Keine zusätzlichen Laufzeitpakete nötig";
        private double _progressPercent;
        private string? _installedExePath;

        public event Action? CloseRequested;

        public InstallerViewModel(IInstallService installService)
        {
            _installService = installService ?? throw new ArgumentNullException(nameof(installService));
            _installPath = _installService.DefaultInstallDirectory;

            StartInstallCommand = new AsyncRelayCommand(InstallAsync, () => !IsInstalling && !IsCompleted);
            LaunchInstalledCommand = new RelayCommand(LaunchAndClose, () => IsCompleted && !string.IsNullOrWhiteSpace(_installedExePath));
        }

        public string InstallPath
        {
            get => _installPath;
            set => SetProperty(ref _installPath, value);
        }

        public bool CreateDesktopShortcut
        {
            get => _createDesktopShortcut;
            set => SetProperty(ref _createDesktopShortcut, value);
        }

        public bool CreateStartMenuShortcut
        {
            get => _createStartMenuShortcut;
            set => SetProperty(ref _createStartMenuShortcut, value);
        }

        public bool EnableAutoStart
        {
            get => _enableAutoStart;
            set => SetProperty(ref _enableAutoStart, value);
        }

        public bool IsInstalling
        {
            get => _isInstalling;
            private set
            {
                if (SetProperty(ref _isInstalling, value))
                {
                    OnPropertyChanged(nameof(CanEditOptions));
                    OnPropertyChanged(nameof(ShowProgressSection));
                }
            }
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            private set
            {
                if (SetProperty(ref _isCompleted, value))
                {
                    OnPropertyChanged(nameof(CanEditOptions));
                    OnPropertyChanged(nameof(ShowProgressSection));
                }
            }
        }

        public bool CanEditOptions => !IsInstalling && !IsCompleted;
        public bool ShowProgressSection => IsInstalling || IsCompleted;

        public string StatusTitle
        {
            get => _statusTitle;
            private set => SetProperty(ref _statusTitle, value);
        }

        public string StatusDetail
        {
            get => _statusDetail;
            private set => SetProperty(ref _statusDetail, value);
        }

        public double ProgressPercent
        {
            get => _progressPercent;
            private set => SetProperty(ref _progressPercent, value);
        }

        public ICommand StartInstallCommand { get; }
        public ICommand LaunchInstalledCommand { get; }

        private async Task InstallAsync()
        {
            IsInstalling = true;

            var progress = new Progress<InstallProgressInfo>(info =>
            {
                StatusTitle = info.StepTitle;
                StatusDetail = info.DetailText;
                ProgressPercent = info.Percent;
            });

            try
            {
                var options = new InstallOptions(
                    InstallPath,
                    CreateDesktopShortcut,
                    CreateStartMenuShortcut,
                    EnableAutoStart);

                _installedExePath = await _installService.InstallAsync(options, progress);
                IsCompleted = true;
            }
            catch (Exception ex)
            {
                StatusTitle = "Installation fehlgeschlagen";
                StatusDetail = ex.Message;
            }
            finally
            {
                IsInstalling = false;
            }
        }

        private void LaunchAndClose()
        {
            if (!string.IsNullOrWhiteSpace(_installedExePath))
            {
                _installService.LaunchInstalledApp(_installedExePath);
            }

            CloseRequested?.Invoke();
        }
    }
}

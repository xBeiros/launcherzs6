using System.Windows.Input;
using WpfApp1.Services;

namespace WpfApp1.ViewModels
{
    /// <summary>
    /// ViewModel für den modernen, rückstandsfreien ZODIAC SIX Uninstaller.
    /// </summary>
    public sealed class UninstallerViewModel : ObservableObject
    {
        private readonly IInstallService _installService;

        private bool _removeCacheAndTempData = true;
        private bool _isUninstalling;
        private bool _isCompleted;
        private string _statusTitle = "Bereit zur Deinstallation";
        private string _statusDetail = "Entfernt Programm, Verknüpfungen & Registry-Einträge";
        private double _progressPercent;

        public event Action? CloseRequested;

        public UninstallerViewModel(IInstallService installService)
        {
            _installService = installService ?? throw new ArgumentNullException(nameof(installService));

            StartUninstallCommand = new AsyncRelayCommand(UninstallAsync, () => !IsUninstalling && !IsCompleted);
            CloseCommand = new RelayCommand(() => CloseRequested?.Invoke());
        }

        public string InstallDirectory => _installService.DefaultInstallDirectory;

        public bool RemoveCacheAndTempData
        {
            get => _removeCacheAndTempData;
            set => SetProperty(ref _removeCacheAndTempData, value);
        }

        public bool IsUninstalling
        {
            get => _isUninstalling;
            private set
            {
                if (SetProperty(ref _isUninstalling, value))
                {
                    OnPropertyChanged(nameof(CanEditOptions));
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
                }
            }
        }

        public bool CanEditOptions => !IsUninstalling && !IsCompleted;

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

        public ICommand StartUninstallCommand { get; }
        public ICommand CloseCommand { get; }

        private async Task UninstallAsync()
        {
            IsUninstalling = true;

            var progress = new Progress<UninstallProgressInfo>(info =>
            {
                StatusTitle = info.StepTitle;
                StatusDetail = info.DetailText;
                ProgressPercent = info.Percent;
            });

            try
            {
                var options = new UninstallOptions(InstallDirectory, RemoveCacheAndTempData);
                await _installService.UninstallAsync(options, progress);
                IsCompleted = true;
            }
            catch (Exception ex)
            {
                StatusTitle = "Deinstallation fehlgeschlagen";
                StatusDetail = ex.Message;
            }
            finally
            {
                IsUninstalling = false;
            }
        }
    }
}

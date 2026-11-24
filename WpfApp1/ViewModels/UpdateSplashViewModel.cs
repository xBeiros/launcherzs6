using WpfApp1.Models;
using WpfApp1.Services;

namespace WpfApp1.ViewModels
{
    /// <summary>
    /// ViewModel für das Discord-ähnliche Start-Update-Fenster (UpdateSplashWindow).
    /// </summary>
    public sealed class UpdateSplashViewModel : ObservableObject
    {
        private readonly IUpdateService _updateService;

        private string _statusText = "Initialisiere ZODIAC SIX...";
        private string _detailText = "Verbindung wird aufgebaut";
        private double _progressPercent = 10;
        private bool _isIndeterminate = true;

        public UpdateSplashViewModel(IUpdateService updateService)
        {
            _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
        }

        public string VersionLabel => $"v{_updateService.CurrentVersion}";

        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        public string DetailText
        {
            get => _detailText;
            private set => SetProperty(ref _detailText, value);
        }

        public double ProgressPercent
        {
            get => _progressPercent;
            private set => SetProperty(ref _progressPercent, value);
        }

        public bool IsIndeterminate
        {
            get => _isIndeterminate;
            private set => SetProperty(ref _isIndeterminate, value);
        }

        /// <summary>
        /// Führt die Update-Prüfung (und ggf. den automatischen Download + Neustart) aus.
        /// Gibt true zurück, wenn ein Update eingespielt wird und die App beendet werden soll.
        /// </summary>
        public async Task<bool> RunUpdateSequenceAsync(CancellationToken cancellationToken = default)
        {
            var progress = new Progress<UpdateProgressInfo>(info =>
            {
                StatusText = info.StatusText;
                DetailText = info.DetailText;
                ProgressPercent = info.Percent;
                IsIndeterminate = info.IsIndeterminate;
            });

            try
            {
                var checkResult = await _updateService.CheckForUpdatesAsync(progress, cancellationToken);
                if (checkResult.UpdateAvailable && checkResult.Manifest is not null)
                {
                    var applied = await _updateService.DownloadAndApplyUpdateAsync(checkResult.Manifest, progress, cancellationToken);
                    if (applied)
                    {
                        return true;
                    }
                }
            }
            catch
            {
                StatusText = "Offline-Modus";
                DetailText = "Starte ZODIAC SIX Launcher...";
                IsIndeterminate = false;
                ProgressPercent = 100;
                await Task.Delay(350, cancellationToken);
            }

            return false;
        }
    }
}

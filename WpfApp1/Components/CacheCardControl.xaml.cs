using System.Globalization;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using WpfApp1.ViewModels;

namespace WpfApp1.Components
{
    /// <summary>
    /// Cache-Verwaltungskarte (rechte Spalte unten rechts) mit 2-Sekunden Hold-to-Clear
    /// per Maus, Leertaste oder Enter.
    /// </summary>
    public partial class CacheCardControl : UserControl
    {
        private const double HoldDurationSeconds = 2.0;
        private readonly DispatcherTimer _holdTimer;
        private DateTime _holdStartUtc;
        private bool _isHolding;

        public CacheCardControl()
        {
            InitializeComponent();

            _holdTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _holdTimer.Tick += HoldTimer_Tick;
        }

        private void StartHold()
        {
            if (_isHolding)
            {
                return;
            }

            if (DataContext is not DashboardViewModel vm || !vm.ClearCacheCommand.CanExecute(null))
            {
                return;
            }

            _isHolding = true;
            _holdStartUtc = DateTime.UtcNow;
            HoldButtonLabel.Text = "WEITERHALTEN...";
            _holdTimer.Start();
        }

        private void HoldClearButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StartHold();
        }

        private void HoldClearButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CancelHold();
        }

        private void HoldClearButton_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Space or Key.Enter)
            {
                StartHold();
            }
        }

        private void HoldClearButton_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Space or Key.Enter)
            {
                CancelHold();
            }
        }

        private void HoldClearButton_MouseLeave(object sender, MouseEventArgs e)
        {
            CancelHold();
        }

        private void HoldClearButton_LostMouseCapture(object sender, MouseEventArgs e)
        {
            CancelHold();
        }

        private void HoldTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isHolding)
            {
                return;
            }

            var elapsed = (DateTime.UtcNow - _holdStartUtc).TotalSeconds;
            var progress = Math.Clamp(elapsed / HoldDurationSeconds, 0.0, 1.0);
            var remaining = Math.Max(0.0, HoldDurationSeconds - elapsed);

            HoldProgressFill.Width = HoldClearButton.ActualWidth * progress;
            HoldCountdownLabel.Text = $"{remaining.ToString("0.0", CultureInfo.GetCultureInfo("de-DE"))} s";

            if (progress >= 1.0)
            {
                CancelHold();
                if (DataContext is DashboardViewModel vm && vm.ClearCacheCommand.CanExecute(null))
                {
                    vm.ClearCacheCommand.Execute(null);
                }
            }
        }

        private void CancelHold()
        {
            if (!_isHolding && HoldProgressFill.Width <= 0)
            {
                return;
            }

            _isHolding = false;
            _holdTimer.Stop();
            HoldProgressFill.Width = 0;
            HoldButtonLabel.Text = "GEDRÜCKT HALTEN ZUM LEEREN";
            HoldCountdownLabel.Text = "2 s";
        }
    }
}

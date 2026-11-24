using System.Windows;
using System.Windows.Input;
using WpfApp1.ViewModels;

namespace WpfApp1.Views
{
    /// <summary>
    /// Kompaktes Discord-ähnliches Startup-Fenster, das bei jedem Launcher-Start nach Updates sucht.
    /// </summary>
    public partial class UpdateSplashWindow : Window
    {
        private readonly UpdateSplashViewModel _viewModel;

        public bool UpdateAppliedAndRestarting { get; private set; }

        public UpdateSplashWindow(UpdateSplashViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        private async void UpdateSplashWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateAppliedAndRestarting = await _viewModel.RunUpdateSequenceAsync();
            DialogResult = true;
            Close();
        }

        private void Splash_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}

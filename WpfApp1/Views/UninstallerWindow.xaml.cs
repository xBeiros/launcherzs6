using System.Windows;
using System.Windows.Input;
using WpfApp1.ViewModels;

namespace WpfApp1.Views
{
    /// <summary>
    /// Moderner, rückstandsfreier ZODIAC SIX Uninstaller (gestartet über Windows "Installierte Apps" oder --uninstall).
    /// </summary>
    public partial class UninstallerWindow : Window
    {
        public UninstallerWindow(UninstallerViewModel viewModel)
        {
            InitializeComponent();
            viewModel.CloseRequested += Close;
            DataContext = viewModel;
        }

        private void DragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}

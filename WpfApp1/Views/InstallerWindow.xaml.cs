using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using WpfApp1.ViewModels;

namespace WpfApp1.Views
{
    /// <summary>
    /// Moderner GTA VI / ZODIAC SIX Single-EXE-Installer.
    /// </summary>
    public partial class InstallerWindow : Window
    {
        private readonly InstallerViewModel _viewModel;

        public InstallerWindow(InstallerViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _viewModel.CloseRequested += Close;
            DataContext = _viewModel;
        }

        private void DragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Installationsverzeichnis für ZODIAC SIX wählen",
                InitialDirectory = _viewModel.InstallPath
            };

            if (dialog.ShowDialog(this) == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
            {
                _viewModel.InstallPath = dialog.FolderName;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

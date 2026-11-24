using System.Windows;
using WpfApp1.ViewModels;

namespace WpfApp1
{
    /// <summary>
    /// Hauptfenster des ZODIAC SIX Launchers (rahmenlos, 1280x720, MVVM-gebunden).
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}

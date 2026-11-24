using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfApp1.Components
{
    /// <summary>
    /// Rahmenlose Fenster-Titelleiste mit DragMove-Unterstützung sowie Minimieren- und Schließen-Buttons.
    /// </summary>
    public partial class TopBarControl : UserControl
    {
        public TopBarControl()
        {
            InitializeComponent();
        }

        private void DragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState != MouseButtonState.Pressed)
            {
                return;
            }

            Window.GetWindow(this)?.DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window is not null)
            {
                window.WindowState = WindowState.Minimized;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }
    }
}

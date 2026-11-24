using System.Diagnostics;
using System.Windows;
using WpfApp1.Services;
using WpfApp1.ViewModels;
using WpfApp1.Views;

namespace WpfApp1
{
    /// <summary>
    /// Steuert den Startup-Flow der Single-EXE:
    /// 1. --uninstall -> Moderner, rückstandsfreier ZODIAC SIX Uninstaller.
    /// 2. Erststart außerhalb des Zielordners (oder --install) -> Moderner GTA VI / ZODIAC SIX Installer.
    /// 3. Normaler Start -> Discord-ähnliches Update-Splash-Fenster -> Haupt-Dashboard (MainWindow).
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var installService = new LauncherInstallService();

            // 1. Deinstallations-Modus (über Windows "Installierte Apps" oder --uninstall)
            var runUninstaller = e.Args.Any(a =>
                a.Equals("--uninstall", StringComparison.OrdinalIgnoreCase) ||
                a.Equals("/uninstall", StringComparison.OrdinalIgnoreCase));

            if (runUninstaller)
            {
                var uninstallerVm = new UninstallerViewModel(installService);
                var uninstallerWindow = new UninstallerWindow(uninstallerVm);
                MainWindow = uninstallerWindow;
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                uninstallerWindow.Show();
                return;
            }

            // 2. Installations-Modus (Single-EXE außerhalb von %LocalAppData%\ZodiacSixLauncher oder --install)
            var forceInstaller = e.Args.Any(a =>
                a.Equals("--install", StringComparison.OrdinalIgnoreCase) ||
                a.Equals("--setup", StringComparison.OrdinalIgnoreCase));

            var shouldRunInstaller = forceInstaller || (!Debugger.IsAttached && !installService.IsRunningFromInstallDirectory());

            if (shouldRunInstaller)
            {
                var installerVm = new InstallerViewModel(installService);
                var installerWindow = new InstallerWindow(installerVm);
                MainWindow = installerWindow;
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                installerWindow.Show();
                return;
            }

            // 3. Normaler Start: kompaktes Discord-ähnliches Update-Fenster -> MainWindow
            if (installService.IsRunningFromInstallDirectory())
            {
                installService.RefreshInstalledMetadataAndIcons();
            }

            var updateService = new AutoUpdateService();
            var splashVm = new UpdateSplashViewModel(updateService);
            var splashWindow = new UpdateSplashWindow(splashVm);
            splashWindow.ShowDialog();

            if (splashWindow.UpdateAppliedAndRestarting)
            {
                Shutdown();
                return;
            }

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            mainWindow.Show();
        }
    }
}

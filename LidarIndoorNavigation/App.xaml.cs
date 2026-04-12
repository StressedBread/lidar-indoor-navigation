using LidarIndoorNavigation.Helpers;
using LidarIndoorNavigation.ViewModels;
using LidarIndoorNavigation.Views;
using System.Configuration;
using System.Data;
using System.Windows;

namespace LidarIndoorNavigation
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private RobotController robotController = null!;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            robotController = new RobotController();
            var mainWindowViewModel = new MainWindowViewModel(robotController);
            var mainWindow = new MainWindow { DataContext = mainWindowViewModel };
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            robotController.Shutdown();

            base.OnExit(e);
        }
    }
}

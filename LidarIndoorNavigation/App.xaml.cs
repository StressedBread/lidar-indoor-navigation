using LidarIndoorNavigation.Helpers;
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
        protected override void OnExit(ExitEventArgs e)
        {
            RobotController.Shutdown();

            base.OnExit(e);
        }
    }
}

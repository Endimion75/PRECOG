using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Windows;
using Precog.Utils;

namespace Precog
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var appSplash = new SplashScreen("Images/PrecogSplash.png");
            appSplash.Show(false);
            appSplash.Close(TimeSpan.FromSeconds(3));
        }
    }
}

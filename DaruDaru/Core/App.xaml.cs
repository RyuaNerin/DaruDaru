using System.IO;
using System.Net;
using System.Net.Cache;
using System.Windows;
using DaruDaru.Config;

namespace DaruDaru.Core
{
    internal partial class App : Application
    {
        public const int WarningItems = 5;

        public static readonly string AppPath;
        public static readonly string AppDir;

        static App()
        {
            AppPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            AppDir  = Path.GetDirectoryName(AppPath);

            //WebRequest.DefaultWebProxy = null;

            HttpWebRequest.DefaultCachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);

            ServicePointManager.DefaultConnectionLimit = 64;
            ServicePointManager.MaxServicePoints = 0;
            ServicePointManager.Expect100Continue = false;
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            ConfigManager.Save();
        }
    }
}

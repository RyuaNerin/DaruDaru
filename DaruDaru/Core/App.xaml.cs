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
        public const int RetryCount = 3;
        public const int BufferSize = 16 * 1024;

        public static readonly string AppPath;
        public static readonly string AppDir;

        static App()
        {
            AppPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            AppDir  = Path.GetDirectoryName(AppPath);

#if !DEBUG
            WebRequest.DefaultWebProxy = null;
#endif

            HttpWebRequest.DefaultCachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);

            ServicePointManager.DefaultConnectionLimit = 256;
            ServicePointManager.MaxServicePoints = 0;
            ServicePointManager.Expect100Continue = false;
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            ConfigManager.Save();
        }
    }
}

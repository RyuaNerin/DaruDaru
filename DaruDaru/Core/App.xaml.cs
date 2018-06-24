using System.IO;
using System.Net;
using System.Net.Cache;
using System.Windows;
using DaruDaru.Marumaru;

namespace DaruDaru.Core
{
    internal partial class App : Application
    {
        public static readonly string BaseDirectory = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "DaruDaru");

        static App()
        {
            WebRequest.DefaultWebProxy = null;

            HttpWebRequest.DefaultCachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);

            ServicePointManager.DefaultConnectionLimit = 16;
            ServicePointManager.MaxServicePoints = 0;
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            SearchLog.Save();
            ArchiveLog.Save();
        }
    }
}

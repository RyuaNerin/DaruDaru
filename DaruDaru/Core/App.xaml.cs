using System.IO;
using System.Net;
using System.Net.Cache;
using System.Windows;
using DaruDaru.Config;
using Microsoft.Win32;

namespace DaruDaru.Core
{
    internal partial class App : Application
    {
        public static readonly string AppPath;
        public static readonly string AppDir;

        static App()
        {
            AppPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            AppDir  = Path.GetDirectoryName(AppPath);

            //WebRequest.DefaultWebProxy = null;

            HttpWebRequest.DefaultCachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);

            ServicePointManager.DefaultConnectionLimit = 16;
            ServicePointManager.MaxServicePoints = 0;
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            ConfigManager.Save();
        }

        private static void EnsureBrowserEmulationEnabled(string exename, bool uninstall = false)
        {
            try
            {
                using (var rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", true))
                {
                    if (!uninstall)
                    {
                        if (rk.GetValue(exename, null) == null)
                            rk.SetValue(exename, (uint)11001, RegistryValueKind.DWord);
                    }
                    else
                        rk.DeleteValue(exename);
                }
            }
            catch
            {
            }
        }
    }
}

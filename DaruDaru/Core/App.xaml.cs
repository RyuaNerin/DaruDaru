using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Threading;
using System.Windows;
using DaruDaru.Config;
using Sentry;

namespace DaruDaru.Core
{
    internal partial class App : Application
    {
        public const string MutextName = "RyuaNerin/DaruDaru";
        private static Mutex Mutex;

        public const int WarningItems = 5;
        public const int RetryCount = 3;
        public const int BufferSize = 16 * 1024;
        public const int SleepSecondWhenServerError = 10;

        public static readonly string Version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName).ProductVersion;

        public static readonly string AppPath;
        public static readonly string AppDir;

        static App()
        {
            AppPath = Assembly.GetExecutingAssembly().Location;
            AppDir  = Path.GetDirectoryName(AppPath);

#if !DEBUG
            WebRequest.DefaultWebProxy = null;
#endif

            HttpWebRequest.DefaultCachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);

            ServicePointManager.DefaultConnectionLimit = 256;
            ServicePointManager.MaxServicePoints = 0;
            ServicePointManager.Expect100Continue = false;
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            CrashReport.Init();

            try
            {
                Mutex = new Mutex(true, MutextName, out var createdNew);

                if (!createdNew)
                {
                    this.Shutdown();
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                this.Shutdown();
            }
        }

        public const int ExitCodeRestart = 18563;
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            ConfigManager.Save();
            Mutex.Dispose();
        }
    }
}

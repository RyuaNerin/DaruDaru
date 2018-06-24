using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using SharpRaven;

namespace DaruDaru.Core
{
    internal static class CrashReport
    {
        private static readonly RavenClient ravenClient = new RavenClient("https://72fd2b5995f44d4e9316acfc44c3434e@sentry.io/1227662")
        {
            Environment = "DaruDaru",
            Release = Assembly.GetExecutingAssembly().GetName().Version.ToString()
        };

        public static void Init()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Error(e.ExceptionObject as Exception);
            TaskScheduler.UnobservedTaskException += (s, e) => Error(e.Exception);
            Application.Current.DispatcherUnhandledException += (s, e) => Error(e.Exception);
            Application.Current.Dispatcher.UnhandledException += (s, e) => Error(e.Exception);
        }

        public static void Error(Exception ex)
        {
#if DEBUG
            throw ex;
#else
            var ev = new SharpRaven.Data.SentryEvent(ex)
            {
                Level = SharpRaven.Data.ErrorLevel.Error
            };

            ev.Tags.Add("ARCH", Environment.Is64BitOperatingSystem ? "x64" : "x86");
            ev.Tags.Add("OS",   Environment.OSVersion.VersionString);
            ev.Tags.Add("NET",  Environment.Version.ToString());

            ravenClient.CaptureAsync(ev);
#endif

        }
    }
}

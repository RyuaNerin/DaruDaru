using System;
using System.Threading.Tasks;
using System.Windows;
using Sentry;

namespace DaruDaru.Core
{
    internal static class CrashReport
    {
        public static void Init()
        {
            SentrySdk.Init(opt =>
            {
                opt.Dsn = new Dsn("https://bd9196b2a6cd499594f4d48ee6d8de6e@sentry.ryuar.in/3");
                opt.IsEnvironmentUser = true;

#if DEBUG
                opt.Release = "Debug";
                opt.Debug = true;
#else
                opt.Release = App.Version;
#endif
            });

            AppDomain.CurrentDomain.UnhandledException += (s, e) => SentrySdk.CaptureException(e.ExceptionObject as Exception);
            TaskScheduler.UnobservedTaskException += (s, e) => SentrySdk.CaptureException(e.Exception);
            Application.Current.DispatcherUnhandledException += (s, e) => SentrySdk.CaptureException(e.Exception);
            Application.Current.Dispatcher.UnhandledException += (s, e) => SentrySdk.CaptureException(e.Exception);
        }
    }
}

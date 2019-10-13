using System;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Navigation;
using MahApps.Metro.Controls;
using mshtml;

namespace DaruDaru.Core.Windows
{
    internal partial class Recaptcha : MetroWindow, IDisposable
    {
        public static readonly TimeSpan TimeOut = TimeSpan.FromSeconds(30);

        static Recaptcha()
        {
            NativeMethods.SetCookieSupressBehavior();
        }

        private readonly Uri m_uri;

        public Recaptcha(Uri uri)
        {
            this.InitializeComponent();

            this.m_uri = uri;
            this.ctlBrowser.Navigate(uri);
        }

        public ManualResetEventSlim Wait { get; } = new ManualResetEventSlim(false);
        public CookieContainer Cookies { get; private set; }

        ~Recaptcha()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool m_disposed;
        private void Dispose(bool disposing)
        {
            if (this.m_disposed) return;
            this.m_disposed = true;

            if (disposing)
            {
                try
                {
                    this.m_iWebBrowser?.Quit();
                }
                catch
                {
                }

                try
                {
                    Marshal.ReleaseComObject(this.m_iWebBrowser);
                }
                catch
                {
                }

                this.ctlBrowser.Dispose();
                this.Wait.Dispose();
            }
        }

        private SHDocVw.WebBrowser m_iWebBrowser;
        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.m_iWebBrowser = (SHDocVw.WebBrowser)this.ctlBrowser.GetType().InvokeMember(
                "ActiveXInstance",
                BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                this.ctlBrowser,
                new object[] { });

            this.m_iWebBrowser.Resizable = false;
            this.m_iWebBrowser.Silent = true;
            this.m_iWebBrowser.StatusBar = false;
            this.m_iWebBrowser.TheaterMode = false;
            this.m_iWebBrowser.Offline = false;
            this.m_iWebBrowser.MenuBar = false;
            this.m_iWebBrowser.RegisterAsBrowser = false;
            this.m_iWebBrowser.RegisterAsDropTarget = false;
            this.m_iWebBrowser.AddressBar = false;
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            this.m_iWebBrowser?.Stop();
        }

        private void CtlBrowser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            try
            {
                var doc = (HTMLDocument)this.ctlBrowser.Document;

                if (e.Uri == this.m_uri)
                {
                    if (!doc.documentElement.innerHTML.Contains("recaptcha"))
                    {
                        this.Cookies = NativeMethods.GetCookies(this.m_uri);
                        this.Wait.Set();
                        this.Close();
                    }
                }
            }
            catch
            {
            }
        }

        private void ctlBrowser_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;
        }

        private static class NativeMethods
        {
            [DllImport("wininet.dll", CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool InternetSetOption(
                IntPtr hInternet,
                int dwOption,
                IntPtr lpBuffer,
                int dwBufferLength);

            [DllImport("wininet.dll", CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool InternetGetCookieEx(
                string url,
                string cookieName,
                StringBuilder cookieData,
                ref int size,
                int dwFlags,
                IntPtr lpReserved);

            private const int INTERNET_COOKIE_HTTPONLY = 0x00002000;
            private const int INTERNET_OPTION_SUPPRESS_BEHAVIOR = 81;

            public static void SetCookieSupressBehavior()
            {
                var optionPtr = IntPtr.Zero;
                try
                {
                    optionPtr = Marshal.AllocHGlobal(4);
                    Marshal.WriteInt32(optionPtr, 3);

                    InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SUPPRESS_BEHAVIOR, optionPtr, 4);
                }
                finally
                {
                    if (optionPtr != IntPtr.Zero)
                        Marshal.FreeHGlobal(optionPtr);
                }
            }

            public static CookieContainer GetCookies(Uri uri)
            {
                var cc = new CookieContainer();

                GetCookies(uri, cc, 0);
                GetCookies(uri, cc, INTERNET_COOKIE_HTTPONLY);

                return cc;
            }
            private static void GetCookies(Uri uri, CookieContainer cc, int option)
            {
                int datasize = 8192 * 16;
                StringBuilder cookieData = new StringBuilder(datasize);
                if (!InternetGetCookieEx(uri.ToString(), null, cookieData, ref datasize, option, IntPtr.Zero))
                {
                    if (datasize < 0)
                        return;

                    cookieData = new StringBuilder(datasize);
                    if (!InternetGetCookieEx(uri.ToString(), null, cookieData, ref datasize, option, IntPtr.Zero))
                        return;
                }

                if (cookieData.Length > 0)
                    cc.SetCookies(uri, cookieData.ToString().Replace(';', ','));
            }
        }
    }
}

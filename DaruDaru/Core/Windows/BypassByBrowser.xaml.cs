using System;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Navigation;
using DaruDaru.Utilities;
using MahApps.Metro.Controls;

namespace DaruDaru.Core.Windows
{
    internal partial class BypassByBrowser : MetroWindow, IDisposable
    {
        static BypassByBrowser()
        {
            NativeMethods.SetCookieSupressBehavior();
            NativeMethods.ChangeUserAgent(HttpClientEx.UserAgent);
        }

        private readonly Uri m_uri;

        public BypassByBrowser(Uri uri)
        {
            this.InitializeComponent();

            this.m_uri = uri;
            this.ctlBrowser.Navigate(uri);
        }

        public ManualResetEventSlim Wait { get; } = new ManualResetEventSlim(false);
        public CookieContainer Cookies { get; private set; }

        ~BypassByBrowser()
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

                this.ctlBrowser.Dispose();

                try
                {
                    Marshal.ReleaseComObject(this.m_iWebBrowser);
                }
                catch
                {
                }

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

            this.m_iWebBrowser.NewWindow2 += (ref object ppDisp, ref bool Cancel) => Cancel = true;
            this.m_iWebBrowser.NewWindow3 += (ref object ppDisp, ref bool Cancel, uint dwFlags, string bstrUrlContext, string bstrUrl) => Cancel = true;
            this.m_iWebBrowser.NewProcess += (int lCauseFlag, object pWB2, ref bool Cancel) => Cancel = true;
            this.m_iWebBrowser.FileDownload += (bool ActiveDocument, ref bool Cancel) => Cancel = true;
            this.m_iWebBrowser.WindowClosing += (bool IsChildWindow, ref bool Cancel) => Cancel = true;

            this.m_iWebBrowser.NavigateError += this.vwWebBrowser_NavigateError;
            this.m_iWebBrowser.NavigateComplete2 += this.vwWebBrowser_NavigateComplete2;
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            this.m_iWebBrowser?.Stop();

            this.Wait.Set();
        }

        private HttpStatusCode m_lastResponse;
        private Uri m_lastUri;

        private void ctlBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            this.m_lastResponse = HttpStatusCode.OK;
            this.m_lastUri = e.Uri;
        }

        private void vwWebBrowser_NavigateError(object pDisp, ref object URL, ref object Frame, ref object StatusCode, ref bool Cancel)
        {
            if (Uri.TryCreate(URL as string, UriKind.RelativeOrAbsolute, out var uri) && uri == this.m_lastUri)
            {
                this.m_lastResponse = (HttpStatusCode)(int)StatusCode;
            }
        }

        private void vwWebBrowser_NavigateComplete2(object pDisp, ref object URL)
        {
            if (Uri.TryCreate(URL as string, UriKind.RelativeOrAbsolute, out var uri) && uri == this.m_lastUri && this.m_lastResponse == HttpStatusCode.OK)
            {
                this.Cookies = NativeMethods.Getcookies(this.m_uri);

                this.DialogResult = true;
                this.Close();
                return;
            }
        }

        private void ctlBrowser_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;
        }

        private static class NativeMethods
        {
            [DllImport("urlmon.dll", CharSet = CharSet.Ansi)]
            private static extern int UrlMkSetSessionOption(int dwOption, string pBuffer, int dwBufferLength, int dwReserved);

            private const int URLMON_OPTION_USERAGENT = 0x10000001;
            private const int URLMON_OPTION_USERAGENT_REFRESH = 0x10000002;

            public static void ChangeUserAgent(string userAgent)
            {
                UrlMkSetSessionOption(URLMON_OPTION_USERAGENT_REFRESH, null, 0, 0);
                UrlMkSetSessionOption(URLMON_OPTION_USERAGENT, userAgent, userAgent.Length, 0);
            }

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

            public static CookieContainer Getcookies(Uri uri)
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

using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Navigation;
using DaruDaru.Utilities;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using mshtml;
using static DaruDaru.Utilities.WebBrowsers;

namespace DaruDaru.Core.Windows
{
    internal partial class Recaptcha : MetroWindow, IDisposable
    {
        public enum Result
        {
            Canceled,
            NonProtected,
            UnknownError,
            Success,
        }

        public static string Cookie { get; private set; }

        public Result RecaptchaResult { get; private set; } = Result.Canceled;

        private static bool NeedToClear = true;
        public Recaptcha(Window owner, Uri uri)
        {
            if (NeedToClear)
            {
                NativeMethods.ClearCookies(uri);
                NeedToClear = false;
            }

            InitializeComponent();

            this.Owner = owner;

            this.ctlBrowser.Navigate(uri);
        }

        ~Recaptcha()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(false);
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
                    this.m_webBrowser?.Quit();
                }
                catch
                {
                }

                this.ctlBrowser.Dispose();
            }
        }

        private IWebBrowser2 m_webBrowser;

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.m_webBrowser = this.ctlBrowser.GetIWebBrowser();

            if (this.m_webBrowser != null)
            {
                this.m_webBrowser.Resizable = false;
                this.m_webBrowser.Silent = false;
                this.m_webBrowser.StatusBar = false;
                this.m_webBrowser.TheaterMode = false;
                this.m_webBrowser.Offline = false;
                this.m_webBrowser.MenuBar = false;
                this.m_webBrowser.RegisterAsBrowser = false;
                this.m_webBrowser.RegisterAsDropTarget = false;
            }
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            this.m_webBrowser?.Stop();
        }

        private void ctlBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            this.ctlProgress.IsActive = true;
            this.ctlProgress.Visibility = Visibility.Visible;
            this.ctlBrowser.Visibility = Visibility.Collapsed;
        }

        private bool m_isProtected = false;
        private void ctlBrowser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            try
            {
                var doc = (HTMLDocumentClass)this.ctlBrowser.Document;

                //기본 작업
                var pass_box = doc.getElementByClassName("pass-box");
                if (pass_box == null)
                {
                    if (!this.m_isProtected)
                    {
                        var template = doc.getElementByClassName("gallery-template");
                        if (template == null)
                        {
                            this.RecaptchaResult = Result.NonProtected;
                            this.Close();
                            return;
                        }
                    }

                    Cookie = NativeMethods.GetCookies(e.Uri);
                    this.RecaptchaResult = Result.Success;
                    this.Close();
                    return;
                }
                else
                    this.m_isProtected = true;

                doc.RemoveElementByClass("logo-top");
                doc.RemoveElementById   ("header-anchor");
                doc.RemoveElementById   ("footer-anchor");
                doc.RemoveElementByClass("footer-terms");
                doc.RemoveElementByClass("page-control");

                doc.RemoveElementByClass("top-nav viewer-hidden");
                doc.RemoveElementByClass("bottom-nav viewer-hidden");

                doc.RemoveElementByClass("top-group");
                doc.RemoveElementByClass("bottom-group");

                doc.body.style.overflow = "hidden";
                doc.body.style.backgroundColor = "#FFF";
                doc.body.style.padding = "0px";
                
                var root = doc.getElementById("root");
                if (root != null)
                {
                    root.style.margin = "0px";
                    root.style.padding = "0px";
                    root.style.backgroundColor = "transparent";
                    root.style.border = "none";
                    root.style.height = "100%";
                }

                var main = doc.getElementById("main");
                if (main != null)
                {
                    main.style.backgroundColor = "transparent";
                    main.style.padding = "0px";
                    main.style.margin = "0px";
                    main.style.height = "100%";
                }

                var section = doc.getElementByClassName("gallery-section");
                if (section != null)
                {
                    section.style.height = "100%";
                    section.style.padding = "0px";
                    section.style.margin = "0px";
                }

                var article = doc.getElementByClassName("article-gallery");
                if (article != null)
                {
                    article.style.height = "100%";
                    article.style.padding = "0px";
                    article.style.margin = "0px";
                }

                if (pass_box != null)
                {
                    pass_box.style.padding = "0px";
                    pass_box.style.display = "inline-block";
                    pass_box.style.margin = "auto";
                }

                System.IO.File.WriteAllText(DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss\".htm\""), doc.documentElement.innerHTML);

                this.ctlProgress.IsActive = false;
                this.ctlProgress.Visibility = Visibility.Collapsed;
                this.ctlBrowser.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {   
                CrashReport.Error(ex);
                this.RecaptchaResult = Result.UnknownError;
                this.Close();
            }
        }

        static class NativeMethods
        {
            [DllImport("wininet.dll", CharSet = CharSet.Unicode)]
            private static extern bool InternetGetCookieEx(string lpszUrl, string lpszCookieName, StringBuilder lpszCookieData, ref int lpdwSize, int dwFlags, IntPtr lpReserved);

            [DllImport("wininet.dll", CharSet = CharSet.Unicode)]
            private static extern bool InternetSetCookie(string lpszUrl, string lpszCookieName, string lpszCookieData);

            private const int InternetCookieHttponly = 0x2000;

            private static CookieContainer GetCookieContainer(Uri uri)
            {
                var size = 0;
                InternetGetCookieEx(uri.AbsoluteUri, null, null, ref size, InternetCookieHttponly, IntPtr.Zero);

                var sb = new StringBuilder(size);
                if (InternetGetCookieEx(uri.AbsoluteUri, null, sb, ref size, InternetCookieHttponly, IntPtr.Zero))
                {
                    var cc = new CookieContainer();
                    cc.SetCookies(uri, sb.ToString().Replace(';', ','));

                    return cc;
                }

                return null;
            }

            public static string GetCookies(Uri uri)
            {
                return GetCookieContainer(uri)?.GetCookieHeader(uri);
            }

            public static void ClearCookies(Uri uri)
            {
                var cc = GetCookieContainer(uri);
                if (cc != null)
                    foreach (Cookie cookie in cc.GetCookies(uri))
                        Console.WriteLine(InternetSetCookie(uri.AbsoluteUri, null, cookie.Name + "=_;expires=Sat,01-Jan-1970 00:00:00 GMT"));
                    //InternetSetCookie(uri.AbsoluteUri, cookie.Name, "_;expires=Sat,01-Jan-1970 00:00:00 GMT");
            }
        }

        private static void EnsureBrowserEmulationEnabled(bool uninstall)
        {
            try
            {
                using (var rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", true))
                {
                    if (!uninstall)
                    {
                        if (rk.GetValue(App.AppPath, null) == null)
                            rk.SetValue(App.AppPath, (uint)11001, RegistryValueKind.DWord);
                    }
                    else
                        rk.DeleteValue(App.AppPath);
                }
            }
            catch
            {
            }
        }
    }

    internal static class HtmlExtension
    {
        public static IHTMLElement getElementByClassName(this HTMLDocument element, string className)
            => ((IHTMLElementCollection)((dynamic)element)?.getElementsByClassName(className)).At(0);

        public static IHTMLElement At(this IHTMLElementCollection collection, int index)
            => collection != null && index < collection.length ? (IHTMLElement)collection.item(index: index) : null;

        public static void RemoveElementById(this HTMLDocument doc, string value)
        {
            try
            {
                doc.getElementById(value).RemoveElement();
            }
            catch
            {
            }
        }
        public static void RemoveElementByClass(this HTMLDocument doc, string value)
        {
            try
            {
                doc.getElementByClassName(value).RemoveElement();
            }
            catch
            {
            }
        }

        public static void RemoveElement(this IHTMLElement element)
        {
            try
            {
                ((dynamic)element).parentNode.removeChild(element);
            }
            catch
            {
            }
        }
    }
}

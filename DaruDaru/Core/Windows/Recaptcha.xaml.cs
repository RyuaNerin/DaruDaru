using System;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using MahApps.Metro.Controls;

namespace DaruDaru.Core.Windows
{
    internal partial class Recaptcha : MetroWindow
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
        
        public Recaptcha(string url)
        {
            InitializeComponent();

            this.ctlBrowser.Navigate(url);
        }

        private void ctlBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            this.ctlProgress.IsActive = true;
            this.ctlProgress.Visibility = Visibility.Visible;
            this.ctlBrowser.Visibility = Visibility.Collapsed;
        }

        private void ctlBrowser_Navigated(object sender, NavigationEventArgs e)
        {
            SetHidden(this.ctlBrowser);
        }

        private bool m_isProtected = false;
        private void ctlBrowser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            try
            {
                dynamic doc = this.ctlBrowser.Document;

                //기본 작업
                dynamic pass_box = doc.getElementsByClassName("pass-box");
                if ((int)pass_box.length == 0)
                {
                    var b = this.m_isProtected;
                    if (!b)
                    {
                        dynamic template = doc.getElementsByClassName("gallery-template");
                        if (template == null || template.length == 0)
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

                doc.body.style.overflow = "hidden";
                doc.body.Scroll = "no";

                RemoveElement(doc.getElementsByClassName("logo-top")[0]);
                RemoveElement(doc.getElementById("header-anchor"));
                RemoveElement(doc.getElementById("footer-anchor"));
                RemoveElement(doc.getElementsByClassName("footer-terms")[0]);
                RemoveElement(doc.getElementsByClassName("page-control")[0]);

                RemoveElement(doc.getElementsByClassName("top-nav viewer-hidden")[0]);
                RemoveElement(doc.getElementsByClassName("bottom-nav viewer-hidden")[0]);

                RemoveElement(doc.getElementsByClassName("top-group")[0]);
                RemoveElement(doc.getElementsByClassName("bottom-group")[0]);

                doc.body.style.backgroundColor = "#FFF";
                doc.body.style.padding = "0px";

                var root = doc.getElementById("root");
                root.style.margin = "0px";
                root.style.padding = "0px";
                root.style.backgroundColor = "transparent";
                root.style.boxShadow = "none";
                root.style.height = "100%";
                root.style.maxWidth = null;

                var main = doc.getElementById("main");
                root.style.backgroundColor = "transparent";
                main.style.padding = "0px";
                main.style.margin = "0px";
                main.style.height = "100%";

                var section = doc.getElementsByClassName("gallery-section")[0];
                section.style.height = "100%";
                section.style.padding = "0px";
                section.style.margin = "0px";

                var article = doc.getElementsByClassName("article-gallery")[0];
                article.style.height = "100%";
                article.style.padding = "0px";
                article.style.margin = "0px";

                if (pass_box.length != 0)
                {
                    pass_box = pass_box[0];
                    pass_box.style.padding = "0px";
                    pass_box.style.display = "inline-block";
                    pass_box.style.margin = "auto";
                }

                this.ctlProgress.IsActive = false;
                this.ctlProgress.Visibility = Visibility.Collapsed;
                this.ctlBrowser.Visibility = Visibility.Visible;
            }
            catch
            {
                this.RecaptchaResult = Result.UnknownError;
                this.Close();
            }
        }

        private static void RemoveElement(dynamic element)
            => element?.parentNode.removeChild(element);

        public static void SetHidden(WebBrowser browser)
        {
            if (browser.Document is NativeMethods.IOleServiceProvider provider)
            {
                var IID_IWebBrowserApp = new Guid("0002DF05-0000-0000-C000-000000000046");
                var IID_IWebBrowser2   = new Guid("D30C1661-CDAF-11d0-8A3E-00C04FC9E26E");

                provider.QueryService(ref IID_IWebBrowserApp, ref IID_IWebBrowser2, out object webBrowser);
                if (webBrowser != null)
                {
                    var type = webBrowser.GetType();
                    
                    var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.PutDispProperty;
                    type.InvokeMember("Silent",    flags, null, webBrowser, new object[] { true  });
                    type.InvokeMember("ToolBar",   flags, null, webBrowser, new object[] { false });
                    type.InvokeMember("StatusBar", flags, null, webBrowser, new object[] { false });
                    type.InvokeMember("MenuBar",   flags, null, webBrowser, new object[] { false });
                }
            }
        }

        static class NativeMethods
        {
            [DllImport("wininet.dll", CharSet = CharSet.Auto)]
            public static extern bool InternetGetCookieEx(string url, string cookieName, StringBuilder cookieData, ref int size, int dwFlags, IntPtr lpReserved);

            private const int InternetCookieHttponly = 0x2000;

            public static string GetCookies(Uri uri)
            {
                int size = 0;
                InternetGetCookieEx(uri.AbsoluteUri, null, null, ref size, InternetCookieHttponly, IntPtr.Zero);

                var sb = new StringBuilder(size);
                if (InternetGetCookieEx(uri.AbsoluteUri, null, sb, ref size, InternetCookieHttponly, IntPtr.Zero))
                {
                    var cookies = new CookieContainer();
                    cookies.SetCookies(uri, sb.ToString().Replace(';', ','));

                    return cookies.GetCookieHeader(uri);
                }

                return null;
            }


            [ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            public interface IOleServiceProvider
            {
                [PreserveSig]
                int QueryService([In] ref Guid guidService, [In] ref Guid riid, [MarshalAs(UnmanagedType.IDispatch)] out object ppvObject);
            }
        }
    }
}

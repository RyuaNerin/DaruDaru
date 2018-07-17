using System;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using mshtml;

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
                var name = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss.txt");

                var doc = (HTMLDocumentClass)this.ctlBrowser.Document;
                System.IO.File.WriteAllText(name, doc.documentElement.innerHTML);

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
            [DllImport("wininet.dll", CharSet = CharSet.Unicode)]
            private static extern bool InternetGetCookieEx(string url, string cookieName, StringBuilder cookieData, ref int size, int dwFlags, IntPtr lpReserved);

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

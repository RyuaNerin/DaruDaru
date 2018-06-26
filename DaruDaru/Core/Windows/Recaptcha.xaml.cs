using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace DaruDaru.Core.Windows
{
    internal partial class Recaptcha : CustomDialog
    {
        public enum Results
        {
            None,
            Cancel,
            CookieExisted,
            Success
        }

        static class NativeMethods
        {
            [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
            
            public static bool SetOption(int settingCode, int? option)
            {
                var optionPtr = IntPtr.Zero;

                try
                {
                    int size = 0;
                    if (option.HasValue)
                    {
                        size = sizeof(int);
                        optionPtr = Marshal.AllocCoTaskMem(size);

                        Marshal.WriteInt32(optionPtr, option.Value);
                    }

                    return InternetSetOption(IntPtr.Zero, settingCode, optionPtr, size);

                }
                finally
                {
                    if (optionPtr != IntPtr.Zero)
                        Marshal.Release(optionPtr);
                }
            }
        }

        private readonly string m_url;

        public Recaptcha(MetroWindow metroWindow, string url)
            : base(metroWindow)
        {
            InitializeComponent();

            // 3 = INTERNET_SUPPRESS_COOKIE_PERSIST 
            // 81 = INTERNET_OPTION_SUPPRESS_BEHAVIOR
            NativeMethods.SetOption(81, 3);

            this.m_url = url;
        }

        private void CustomDialog_Unloaded(object sender, RoutedEventArgs e)
        {
            // 42 = INTERNET_OPTION_END_BROWSER_SESSION
            NativeMethods.SetOption(42, null);
        }

        private void CustomDialog_Loaded(object sender, RoutedEventArgs e)
        {
            this.ctlBrowser.Navigate(this.m_url);
        }

        public Results Result { get; set; }
        public string ResultData { get; set; }

        private void Close()
        {
            this.OwningWindow.HideMetroDialogAsync(this);
        }

        private void ctlClose_Click(object sender, RoutedEventArgs e)
        {
            this.Result = Results.Cancel;
            this.Close();
        }

        private static readonly Regex RegexToken = new Regex(@"[\?&]g-recaptcha-response=([^&=#]*)", RegexOptions.Compiled);
        private static readonly Regex RegexPass  = new Regex(@"[\?&]pass=([^&=#]*)", RegexOptions.Compiled);
        
        private void ctlBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            var query = e.Uri.Query;
            if (!string.IsNullOrWhiteSpace(query))
            {
                // parse Query
                var mtk = RegexToken.Match(query);
                var mpw = RegexPass.Match(query);

                if (mtk.Success && mpw.Success)
                {
                    var vtk = Uri.EscapeDataString(Uri.UnescapeDataString(mtk.Groups[1].Value));
                    var vpw = Uri.EscapeDataString(Uri.UnescapeDataString(mpw.Groups[1].Value));

                    this.Result = Results.Success;
                    this.ResultData = $"recaptcha-token={vtk}&pass={vpw}";
                    this.Close();
                }

                e.Cancel = true;
            }
        }

        private void ctlBrowser_Navigated(object sender, NavigationEventArgs e)
        {
            SetHidden(this.ctlBrowser);
        }

        private bool m_firstLoad = false;
        private void ctlBrowser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            if (this.m_firstLoad) return;
            this.m_firstLoad = true;

            // 스크롤바 삭제
            dynamic doc = this.ctlBrowser.Document;

            doc.body.style.overflow = "hidden";
            doc.body.Scroll = "no";

            if (e.Uri.ToString() == this.m_url)
            {
                dynamic pass_box = doc.getElementsByClassName("pass-box");
                if ((int)pass_box.length == 0)
                {
                    this.Result = Results.CookieExisted;
                    this.Close();
                    return;
                }

                RemoveElement(doc.getElementsByClassName("logo-top")[0]);
                RemoveElement(doc.getElementById("header-anchor"));
                RemoveElement(doc.getElementById("footer-anchor"));
                RemoveElement(doc.getElementsByClassName("footer-terms")[0]);
                RemoveElement(doc.getElementsByClassName("page-control")[0]);

                RemoveElement(doc.getElementsByClassName("top-nav viewer-hidden")[0]);
                RemoveElement(doc.getElementsByClassName("bottom-nav viewer-hidden")[0]);

                RemoveElement(doc.getElementsByClassName("top-group")[0]);
                RemoveElement(doc.getElementsByClassName("bottom-group")[0]);

                RemoveElement(doc.getElementsByClassName("pass-icon fa fa-lock")[0]);

                doc.body.style.backgroundColor = "#FFF";

                var root = doc.getElementById("root");
                root.style.margin = "0px";
                root.style.padding = "0px";
                root.style.backgroundColor = "transparent";
                root.style.boxShadow = "none";
                root.style.height = "100%";

                var main = doc.getElementById("main");
                root.style.backgroundColor = "transparent";
                main.style.padding = "0px";
                main.style.height = "100%";
                
                var section = doc.getElementsByClassName("gallery-section")[0];
                section.style.height = "100%";

                var article = doc.getElementsByClassName("article-gallery")[0];
                article.style.height = "100%";
                article.style.margin = "0px";

                pass_box = pass_box[0];
                pass_box.style.padding = "0px";
                pass_box.style.display = "inline-block";
                pass_box.style.margin = "auto";

                doc.getElementsByTagName("form")[0].method = "GET";

                var str = doc.body.innerHTML;

                this.ctlProgress.IsActive = false;
                this.ctlProgress.Visibility = Visibility.Collapsed;
                this.ctlBrowser.Visibility = Visibility.Visible;
            }
        }

        private static void RemoveElement(dynamic element)
            => element?.parentNode.removeChild(element);

        public static void SetHidden(WebBrowser browser)
        {
            if (browser.Document is IOleServiceProvider provider)
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

        [ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IOleServiceProvider
        {
            [PreserveSig]
            int QueryService([In] ref Guid guidService, [In] ref Guid riid, [MarshalAs(UnmanagedType.IDispatch)] out object ppvObject);
        }
    }
}

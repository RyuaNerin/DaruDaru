using System;
using System.Net;
using DaruDaru.Core.Windows;

namespace DaruDaru.Utilities
{
    [System.ComponentModel.DesignerCategory("DOCE")]
    internal class WebClientEx : WebClient
    {
        public WebClientEx() : base()
        {
            this.Encoding = System.Text.Encoding.UTF8;
        }

        protected override WebRequest GetWebRequest(Uri address) => AddHeader(base.GetWebRequest(address));

        public static WebRequest AddHeader(WebRequest req)
        {
            if (req is HttpWebRequest hreq)
            {
                hreq.Timeout          = 10 * 1000;
                hreq.ReadWriteTimeout = 5000;
                hreq.ContinueTimeout  = 5000;

                if (string.IsNullOrWhiteSpace(hreq.Referer))
                    hreq.Referer            = req.RequestUri.AbsoluteUri;

                hreq.UserAgent              = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.135 Safari/537.36 Edge/12.9600";
                hreq.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                hreq.Accept                 = "html/text,image/*,*/*;q=0.9";
                hreq.AllowAutoRedirect      = true;

                hreq.Headers.Add("charset",         "utf-8");
                hreq.Headers.Add("cookie",          Recaptcha.Cookie);
                hreq.Headers.Add("Accept-Language", "ko");
            }

            return req;
        }

        private Uri m_responseUri;
        public Uri ResponseUri => this.m_responseUri;
        protected override WebResponse GetWebResponse(WebRequest request)
        {
            var res = base.GetWebResponse(request);

            if (res is HttpWebResponse hres)
            {
                this.m_responseUri = hres.ResponseUri;
            }

            return res;
        }
    }
}

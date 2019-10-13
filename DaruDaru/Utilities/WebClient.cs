using System;
using System.Net;

namespace DaruDaru.Utilities
{
    [System.ComponentModel.DesignerCategory("CODE")]
    internal class WebClientEx : WebClient
    {
        public WebClientEx() : base()
        {
            this.Encoding = System.Text.Encoding.UTF8;
        }
        
        public static CookieContainer Cookie { get; } = new CookieContainer();

        private string UserAgent { get; set; }        

        public Uri ResponseUri { get; private set; }
        public HttpStatusCode LastStatusCode { get; private set; }

        protected override WebRequest GetWebRequest(Uri address)
            => AddHeader(base.GetWebRequest(address));

        public static WebRequest AddHeader(WebRequest req)
        {
            if (req is HttpWebRequest hreq)
            {
                hreq.Timeout          = 30 * 1000;
                hreq.ReadWriteTimeout = 30 * 1000;
                hreq.ContinueTimeout  = 30 * 1000;

                if (string.IsNullOrWhiteSpace(hreq.Referer))
                    hreq.Referer            = req.RequestUri.AbsoluteUri;

                hreq.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                hreq.Accept                 = "html/text,image/*,*/*;q=0.9";
                hreq.AllowAutoRedirect      = true;

                hreq.Headers.Add("charset",         "utf-8");
                hreq.Headers.Add("Accept-Language", "ko");

                hreq.CookieContainer = Cookie;
                hreq.UserAgent = "Mozilla/4.0 (compatible; MSIE 9.0; Windows NT 6.1)";
            }

            return req;
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            WebResponse res;

            try
            {
                res = base.GetWebResponse(request);
            }
            catch (WebException ex)
            {
                res = ex.Response;
            }
            catch
            {
                return null;
            }            

            if (res is HttpWebResponse hres)
            {
                this.ResponseUri = hres.ResponseUri;

                this.LastStatusCode = hres.StatusCode;
            }

            return res;
        }
    }
}

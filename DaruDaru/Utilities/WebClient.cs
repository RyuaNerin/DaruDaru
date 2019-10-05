using System;
using System.Collections.Generic;
using System.Net;

namespace DaruDaru.Utilities
{
    [System.ComponentModel.DesignerCategory("CODE")]
    internal class WebClientEx : WebClient
    {
        private static readonly string[] UserAgents =
        {
            "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 5.1; Trident/4.0; .NET CLR 2.0.50727; .NET CLR 3.0.4506.2152; .NET CLR 3.5.30729)",
            "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Trident/4.0)",
            "Mozilla/4.0 (compatible; MSIE 9.0; Windows NT 6.1)",
            "Mozilla/5.0 (Windows NT 10.0; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko",
            "Mozilla/5.0 (Windows NT 10.0; WOW64; Trident/7.0; rv:11.0) like Gecko",
            "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:50.0) Gecko/20100101 Firefox/50.0",
            "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:52.0) Gecko/20100101 Firefox/52.0",
            "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:54.0) Gecko/20100101 Firefox/54.0",
            "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:56.0) Gecko/20100101 Firefox/56.0",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/55.0.2883.87 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0.2987.133 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.90 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.84 Safari/537.36",
            "Mozilla/5.0 (Windows NT 5.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.90 Safari/537.36",
            "Mozilla/5.0 (Windows NT 5.1; rv:7.0.1) Gecko/20100101 Firefox/7.0.1",
            "Mozilla/5.0 (Windows NT 6.1; Trident/7.0; rv:11.0) like Gecko",
            "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko",
            "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:40.0) Gecko/20100101 Firefox/40.1",
            "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:41.0) Gecko/20100101 Firefox/41.0",
            "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:44.0) Gecko/20100101 Firefox/44.0",
            "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:46.0) Gecko/20100101 Firefox/46.0",
            "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:47.0) Gecko/20100101 Firefox/47.0",
            "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:50.0) Gecko/20100101 Firefox/50.0",
            "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:52.0) Gecko/20100101 Firefox/52.0",
            "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:54.0) Gecko/20100101 Firefox/54.0",
            "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:56.0) Gecko/20100101 Firefox/56.0",
            "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/55.0.2883.87 Safari/537.36",
            "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0.2987.133 Safari/537.36",
            "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.90 Safari/537.36",
            "Mozilla/5.0 (Windows NT 6.1; Win64; x64; Trident/7.0; rv:11.0) like Gecko",
            "Mozilla/5.0 (Windows NT 6.2; WOW64; Trident/7.0; rv:11.0) like Gecko",
            "Mozilla/5.0 (Windows NT 6.2; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.90 Safari/537.36",
            "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko",
            "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; rv:11.0) like Gecko",
            "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:52.0) Gecko/20100101 Firefox/52.0",
            "Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36",
            "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; Trident/6.0)",
            "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; WOW64; Trident/6.0)",
            "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)",
            "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.0; Trident/5.0)",
            "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)",
            "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)",
        };
        private static readonly Random Random = new Random(DateTime.Now.Millisecond);

        private static Stack<WebClientEx> Pool = new Stack<WebClientEx>();
        public static WebClientEx GetOrCreate()
        {
            lock (Pool)
            {
                if (Pool.Count == 0)
                    return new WebClientEx();

                return Pool.Pop();
            }
        }


        private readonly string m_userAgent;
        private readonly CookieContainer m_cookie = new CookieContainer();
        private WebClientEx() : base()
        {
            this.Encoding = System.Text.Encoding.UTF8;
            this.m_userAgent = UserAgents[Random.Next(0, UserAgents.Length)];
        }

        protected override void Dispose(bool disposing)
        {
            //base.Dispose(disposing);
            if (disposing)
            {
                lock (Pool)
                    Pool.Push(this);
            }
        }

        public Uri ResponseUri { get; private set; }
        public HttpStatusCode LastStatusCode { get; private set; }

        protected override WebRequest GetWebRequest(Uri address)
            => AddHeader(base.GetWebRequest(address), this);

        public static WebRequest AddHeader(WebRequest req, WebClientEx wcEx = null)
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

                if (wcEx == null)
                {
                    hreq.UserAgent = UserAgents[Random.Next(0, UserAgents.Length)];
                }
                else
                {
                    hreq.CookieContainer = wcEx.m_cookie;
                    hreq.UserAgent       = wcEx.m_userAgent;
                }
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

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CloudflareSolverRe;

namespace DaruDaru.Utilities
{
    internal class HttpClientEx : HttpClient
    {
        public static CookieContainer Cookie { get; } = new CookieContainer();

        private class CustomHttpMessageHandler : ClearanceHandler
        {
            public CustomHttpMessageHandler()
                : base(new HttpClientHandler()
                {
                    UseCookies = true,
                    CookieContainer = Cookie,
                    MaxConnectionsPerServer = 256,
                    AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
                })
            {
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (request.Headers.Referrer == null)
                    request.Headers.Referrer = request.RequestUri;

                request.Headers.Add("Accpet", "html/text,image/*,*/*;q=0.9");
                request.Headers.Add("Charset", "utf-8");
                request.Headers.Add("Accept-Language", "ko");
                request.Headers.Add("User-Agent", "Mozilla/4.0 (compatible; MSIE 9.0; Windows NT 6.1)");

                return base.SendAsync(request, cancellationToken);
            }
        }

        public HttpClientEx()
            : base(new CustomHttpMessageHandler(), true)
        {
            this.Timeout = TimeSpan.FromSeconds(30);
        }
    }
}

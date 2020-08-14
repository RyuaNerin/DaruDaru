using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CloudflareSolverRe;
using CloudflareSolverRe.Types;
using KPreisser;
using Sentry;

namespace DaruDaru.Utilities
{
    internal class HttpClientEx : HttpClient
    {
        public const string UserAgent = "Mozilla/5.0 (MSIE 10.0; Windows NT 6.1; Trident/5.0)";


        public HttpClientEx()
            : base(new BypassHandler(), true)
        {
            this.Timeout = TimeSpan.FromSeconds(30);
        }

        public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.Referrer == null)
                request.Headers.Referrer = request.RequestUri;

            request.Headers.Add("User-Agent", UserAgent);

            return base.SendAsync(request, cancellationToken);
        }

        public class BypassFailed : Exception
        {
        }
        private class BypassHandler : DelegatingHandler
        {
            private class CookieInfomation
            {
                public AsyncReaderWriterLockSlim Lock { get; } = new AsyncReaderWriterLockSlim();
                public SemaphoreSlim SemaphoreSlim { get; } = new SemaphoreSlim(1);
                public string Cookie { get; set; }
            }
            private static Dictionary<string, CookieInfomation> Cookies = new Dictionary<string, CookieInfomation>();


            public BypassHandler()
                : base(new HttpClientHandler
                {
                    UseCookies = false,
                    MaxConnectionsPerServer = 256,
                    AutomaticDecompression  = DecompressionMethods.Deflate | DecompressionMethods.GZip,
                })
            {
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var baseUri = new Uri(request.RequestUri, "/");

                request.Headers.Add("User-Agent", UserAgent);

                HttpResponseMessage res = null;

                CookieInfomation ci;
                lock (Cookies)
                    if (!Cookies.TryGetValue(request.RequestUri.Host, out ci))
                        Cookies[request.RequestUri.Host] = (ci = new CookieInfomation());

                string cookie;
                using (ci.Lock.GetReadLock())
                    cookie = ci.Cookie;

                request.Headers.Add("Cookie", cookie);
                res = await base.SendAsync(request, cancellationToken);

                if (res == null)
                {
                    throw new NullReferenceException();
                }

                if (!CloudflareDetector.IsClearanceRequired(res))
                    return res;

                res.Dispose();
                res = null;

                if (!await ci.SemaphoreSlim.WaitAsync(0))
                {
                    await ci.SemaphoreSlim.WaitAsync();
                    ci.SemaphoreSlim.Release();

                    if (ci.Cookie == null)
                        throw new BypassFailed();

                    using (ci.Lock.GetReadLock())
                        cookie = ci.Cookie;

                }
                else
                {
                    try
                    {
                        using (await ci.Lock.GetWriteLockAsync())
                        {
                            var cf = new CloudflareSolver(UserAgent)
                            {
                                MaxTries = 3,
                                ClearanceDelay = 5000,
                            };

                            try
                            {
                                SolveResult cfs;
                                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                                {
                                    cts.CancelAfter(5000);

                                    cfs = await cf.Solve(request.RequestUri, null, cts.Token);
                                }

                                if (cfs.Success)
                                {
                                    ci.Cookie = cfs.Cookies.AsHeaderString();

                                    return await base.SendAsync(request, cancellationToken);
                                }
                            }
                            catch (OperationCanceledException)
                            {
                            }
                            catch (Exception ex)
                            {
                                SentrySdk.CaptureException(ex);
                            }
                        }
                    }
                    finally
                    {
                        ci.SemaphoreSlim.Release();
                    }
                }

                if (cookie != null)
                    request.Headers.Add("Cookie", cookie);

                res = await base.SendAsync(request, cancellationToken);
                if (!CloudflareDetector.IsClearanceRequired(res))
                    return res;

                res.Dispose();
                res = null;

                throw new BypassFailed();
            }
        }
    }
}

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CloudflareSolverRe;
using DaruDaru.Core.Windows;
using KPreisser;

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
            private static string Cookies;

            private static readonly AsyncReaderWriterLockSlim CookieHeaderLock = new AsyncReaderWriterLockSlim();
            private static readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);

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

                string cookie;
                using (CookieHeaderLock.GetReadLock())
                    cookie = Cookies;

                if (cookie != null)
                {
                    request.Headers.Add("Cookie", cookie);
                    res = await base.SendAsync(request, cancellationToken);

                    if (!CloudflareDetector.IsClearanceRequired(res))
                        return res;

                    res.Dispose();
                    res = null;
                }

                if (!await semaphoreSlim.WaitAsync(0))
                {
                    await semaphoreSlim.WaitAsync();
                    semaphoreSlim.Release();

                    if (Cookies == null)
                        throw new BypassFailed();

                    using (CookieHeaderLock.GetReadLock())
                        cookie = Cookies;

                }
                else
                {
                    using (await CookieHeaderLock.GetWriteLockAsync())
                    {
                        var cf = new CloudflareSolver(UserAgent)
                        {
                            MaxTries = 3,
                            ClearanceDelay = 5000,
                        };

                        try
                        {
                            var cfs = await cf.Solve(request.RequestUri).ConfigureAwait(false);
                            if (cfs.Success)
                            {
                                Cookies = cfs.Cookies.AsHeaderString();

                                return await base.SendAsync(request, cancellationToken);
                            }
                        }
                        catch
                        {
                            // 2020-02-29
                            // New CF Challenge : __cf_chl_jschl_tk__
                            // https://github.com/RyuzakiH/CloudflareSolverRe/issues/14
                        }

                        try
                        {
                            BypassByBrowser frm = null;
                            try
                            {
                                frm = Application.Current.Dispatcher.Invoke(() => new BypassByBrowser(new UriBuilder(request.RequestUri) { Path = null, Query = null }.Uri)
                                {
                                    Owner = Application.Current.MainWindow,
                                });

                                var status = Application.Current.Dispatcher.Invoke(frm.ShowDialog) ?? false;

                                frm.Wait.Wait();

                                if (status)
                                {
                                    cookie = Cookies = frm.Cookies.GetCookieHeader(baseUri);
                                }
                                else
                                {
                                    throw new BypassFailed();
                                }
                            }
                            catch (BypassFailed)
                            {
                                throw;
                            }
                            finally
                            {
                                if (frm != null)
                                {
                                    Application.Current.Dispatcher.Invoke(frm.Close);
                                    Application.Current.Dispatcher.Invoke(frm.Dispose);
                                }
                            }
                        }
                        finally
                        {
                            semaphoreSlim.Release();
                        }
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

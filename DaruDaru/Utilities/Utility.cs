using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DaruDaru.Core;
using DaruDaru.Core.Windows;
using Sentry;

namespace DaruDaru.Utilities
{
    internal static class Utility
    {
        public static string ToEICFormat(double size, string footer = null)
        {
            if (size == 0)          return "";
            if (size > 1000 * 1024) return (size / 1024 / 1024).ToString("##0.0 \" MiB\"") + footer;
            if (size > 1000       ) return (size / 1024       ).ToString("##0.0 \" KiB\"") + footer;
                                    return (size              ).ToString("##0.0 \" B\""  ) + footer;
        }

        private static readonly Regex InvalidRegex = new Regex($"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()))}]", RegexOptions.Compiled);
        public static string ReplaceInvalid(string s) => s == null ? null : InvalidRegex.Replace(s, "_");

        public static string ReplcaeHtmlTag(string s) => s?.Replace("&nbsp;", " ")
                                                          .Replace("&lt;", "<")
                                                          .Replace("&gt;", ">")
                                                          .Replace("&amp;", "&")
                                                          .Replace("&quot;", "\"")
                                                          .Replace("&apos;", "'")
                                                          .Replace("&copy;", "©")
                                                          .Replace("&reg;", "®");

        public static string ReplaceHtmlTagAndRemoveTab(string s)
        {
            s = ReplcaeHtmlTag(s).Replace('\t', ' ').Trim();

            while (s.Contains("  "))
            {
                s = s.Replace("  ", " ");
            }
            return s;
        }

        private const int HR_ERROR_HANDLE_DISK_FULL = unchecked((int)0x80070027);
        private const int HR_ERROR_DISK_FULL        = unchecked((int)0x80070070);

        public static bool Retry(Func<int, bool> action)
            => Retry(action, App.RetryCount);

        public static bool Retry(Func<int, bool> action, int retries = App.RetryCount)
        {
            do
            {
                try
                {
                    if (action(retries))
                        return true;
                }
                catch (HttpClientEx.BypassFailed)
                {
                    return false;
                }
                // 디스크 공간 부족
                catch (IOException ex) when (ex.HResult == HR_ERROR_DISK_FULL || ex.HResult == HR_ERROR_HANDLE_DISK_FULL)
                {
                    MainWindow.Instance.ShowNotEnoughDiskSpace();
                }
                catch (SocketException)
                {
                }
                // 작업 취소
                catch (TaskCanceledException)
                {
                }
                catch (WebException ex)
                {
                    SentrySdk.CaptureException(ex);
                }
                catch (Exception ex)
                {
                    SentrySdk.CaptureException(ex);
                }

                Thread.Sleep(1000);
            } while (--retries > 0);

            return false;
        }

        public static Task<bool> RetryAsync(Func<int, Task<bool>> action)
            => RetryAsync(action, App.RetryCount);

        public static async Task<bool> RetryAsync(Func<int, Task<bool>> action, int retries = App.RetryCount)
        {
            do
            {
                try
                {
                    if (await action(retries))
                        return true;
                }
                catch (HttpClientEx.BypassFailed)
                {
                    return false;
                }
                // 디스크 공간 부족
                catch (IOException ex) when (ex.HResult == HR_ERROR_DISK_FULL || ex.HResult == HR_ERROR_HANDLE_DISK_FULL)
                {
                    MainWindow.Instance.ShowNotEnoughDiskSpace();
                }
                catch (SocketException)
                {
                }
                // 작업 취소
                catch (TaskCanceledException)
                {
                }
                catch (WebException ex)
                {
                    SentrySdk.CaptureException(ex);
                }
                catch (Exception ex)
                {
                    SentrySdk.CaptureException(ex);
                }

                Thread.Sleep(1000);
            } while (--retries > 0);

            return false;
        }

        public static bool ResolvUri(HttpClientEx hc, Uri uri, out Uri newUri)
        {
            newUri = null;

            try
            {
                using (var req = new HttpRequestMessage(HttpMethod.Head, uri))
                {
                    using (var res = hc.SendAsync(req).GetAwaiter().GetResult())
                    {
                        newUri = res.RequestMessage.RequestUri;
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        public static Uri CreateUri(string uriString)
            => ReplaceHost(new Uri(uriString));

        public static Uri CreateUri(Uri baseUri, string relativeUri)
            => ReplaceHost(new Uri(baseUri, relativeUri));

        public static bool TryCreateUri(string uriString, out Uri uri)
        {
            if (Uri.TryCreate(uriString, UriKind.Absolute, out Uri tempUri))
            {
                uri = ReplaceHost(tempUri);
                return true;
            }

            uri = null;
            return false;
        }
        public static bool TryCreateUri(Uri baseUri, string relativeUri, out Uri uri)
        {
            if (Uri.TryCreate(baseUri, relativeUri, out Uri tempUri))
            {
                uri = ReplaceHost(tempUri);
                return true;
            }

            uri = null;
            return false;
        }


        private static readonly IdnMapping IdnMapping = new IdnMapping();
        private static Uri ReplaceHost(Uri uri)
        {
            var ub = new UriBuilder(uri);
            var sb = new StringBuilder(ub.Host.Length);

            char c;
            for (var i = 0; i < ub.Host.Length; ++i)
            {
                c = ub.Host[i];

                // ① ② ③ ④ ⑤ ⑥ ⑦ ⑧ ⑨
                     if ('①' <= c && c <= '⑨') sb.Append((char)(c - '①' + '1'));

                // ⑴ ⑵ ⑶ ⑷ ⑸ ⑹ ⑺ ⑻ ⑼
                else if ('⑴' <= c && c <= '⑼') sb.Append((char)(c - '⑴' + '1'));

                // ⒈ ⒉ ⒊ ⒋ ⒌ ⒍ ⒎ ⒏ ⒐
                else if ('⒈' <= c && c <= '⒐') sb.Append((char)(c - '⒈' + '1'));

                // ⑴ ⑵ ⑶ ⑷ ⑸ ⑹ ⑺ ⑻ ⑼
                else if ('⑴' <= c && c <= '⑼') sb.Append((char)(c - '⑴' + '1'));

                // ⑴ ⑵ ⑶ ⑷ ⑸ ⑹ ⑺ ⑻ ⑼
                else if ('⑴' <= c && c <= '⑼') sb.Append((char)(c - '⑴' + '1'));

                // ⑴ ⑵ ⑶ ⑷ ⑸ ⑹ ⑺ ⑻ ⑼
                else if ('⑴' <= c && c <= '⑼') sb.Append((char)(c - '⑴' + '1'));

                // ⒜ ⒝ ⒞ ⒟ ⒠ ⒡ ⒢ ⒣ ⒤ ⒥ ⒦ ⒧ ⒨ ⒩ ⒪ ⒫ ⒬ ⒭ ⒮ ⒯ ⒰ ⒱ ⒲ ⒳ ⒴ ⒵
                else if ('⒜' <= c && c <= '⒵') sb.Append((char)(c - '⒜' + 'a'));

                // Ⓐ Ⓑ Ⓒ Ⓓ Ⓔ Ⓕ Ⓖ Ⓗ Ⓘ Ⓙ Ⓚ Ⓛ Ⓜ Ⓝ Ⓞ Ⓟ Ⓠ Ⓡ Ⓢ Ⓣ Ⓤ Ⓥ Ⓦ Ⓧ Ⓨ Ⓩ
                else if ('Ⓐ' <= c && c <= 'Ⓩ') sb.Append((char)(c - 'Ⓐ' + 'a'));

                // ⓐ ⓑ ⓒ ⓓ ⓔ ⓕ ⓖ ⓗ ⓘ ⓙ ⓚ ⓛ ⓜ ⓝ ⓞ ⓟ ⓠ ⓡ ⓢ ⓣ ⓤ ⓥ ⓦ ⓧ ⓨ ⓩ
                else if ('ⓐ' <= c && c <= 'ⓩ') sb.Append((char)(c - 'ⓐ' + 'a'));

                else
                    sb.Append(c);
            }

            // 퓨니코드 대응
            ub.Host = IdnMapping.GetAscii(sb.ToString());

            return ub.Uri;
        }
    }
}

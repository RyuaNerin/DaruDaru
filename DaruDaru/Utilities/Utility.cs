using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using DaruDaru.Core;

namespace DaruDaru.Utilities
{
    internal static class Utility
    {
        private static readonly Regex InvalidRegex = new Regex($"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()))}]", RegexOptions.Compiled);
        public static string ReplaceInvalid(string s) => InvalidRegex.Replace(s, "_");

        public static string ReplcaeHtmlTag(string s) => s.Replace("&nbsp;", " ")
                                                          .Replace("&lt;", "<")
                                                          .Replace("&gt;", ">")
                                                          .Replace("&amp;", "&")
                                                          .Replace("&quot;", "\"")
                                                          .Replace("&apos;", "'")
                                                          .Replace("&copy;", "©")
                                                          .Replace("&reg;", "®");

        public static bool Retry(Func<bool> action, int retries = 3)
        {
            do
            {
                try
                {
                    if (action())
                        return true;
                }
                catch (WebException)
                {
                }
                catch (SocketException)
                {
                }
                catch (Exception ex)
                {
                    CrashReport.Error(ex);
                }

                Thread.Sleep(1000);
            } while (--retries > 0);

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

            ub.Host = sb.ToString();

            return ub.Uri;
        }
    }
}

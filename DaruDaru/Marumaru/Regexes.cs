using System;
using System.Text.RegularExpressions;

namespace DaruDaru.Marumaru
{
    internal static class RegexComic
    {
        private static readonly Regex Re = new Regex(
            @"^https?:\/\/(?:[a-zA-Z0-9][a-zA-Z0-9-]*\.)*marumaru\.in\/(?:@*\/)*(?:@*\?(?:(?!uid)@+=@+&)*uid=)?(\d+)+.*$"
.Replace("@", @"[\w\-\._~:\/#\[\]@!\$&'\(\)\*\+,;=.%]"),
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool CheckUri(Uri uri)
            => Re.IsMatch(uri.AbsoluteUri);

        public static Uri GetUri(string code)
            => new Uri("https://marumaru.in/b/manga/" + code);

        public static string GetCode(Uri uri)
        {
            if (uri == null) return null;

            var m = Re.Match(uri.AbsoluteUri);
            if (m.Success)
                return m.Groups[1].Value;
            else
                return null;
        }
    }
    internal static class RegexArchive
    {
        private static readonly Regex Re = new Regex(
            @"^https?:\/\/(?:[^\.]*\.)?(?:mangaumaru\.com|shencomics\.com|yuncomics\.com|wasabisyrup\.com)\/archives\/([^\?""']+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool CheckUri(Uri uri)
            => Re.IsMatch(uri.AbsoluteUri);

        public static Uri GetUri(string code)
            => new Uri("http://wasabisyrup.com/archives/" + code);

        public static string GetCode(Uri uri)
        {
            if (uri == null) return null;

            var m = Re.Match(uri.AbsoluteUri);
            if (m.Success)
                return m.Groups[1].Value;
            else
                return null;
        }
    }
}

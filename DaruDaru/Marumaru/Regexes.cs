using System.Text.RegularExpressions;

namespace DaruDaru.Marumaru
{
    internal static class RegexComic
    {
        private static readonly Regex Re = new Regex(
            @"^https?:\/\/(?:[a-zA-Z0-9][a-zA-Z0-9-]*\.)*marumaru\.in\/(?:@*\/)*(?:@*\?(?:(?!uid)@+=@+&)*uid=)?(\d+)+.*$"
.Replace("@", @"[\w\-\._~:\/#\[\]@!\$&'\(\)\*\+,;=.%]"),
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool CheckUrl(string url)
            => string.IsNullOrWhiteSpace(url) ? false : Re.IsMatch(url);

        public static string GetUrl(string code)
            => "https://marumaru.in/b/manga/" + code;

        public static string GetCode(string url)
        {
            if (url == null) return null;

            var m = Re.Match(url);
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

        public static bool CheckUrl(string url)
            => string.IsNullOrWhiteSpace(url) ? false : Re.IsMatch(url);

        public static string GetUrl(string code)
            => "http://wasabisyrup.com/archives/" + code;

        public static string GetCode(string url)
        {
            if (url == null) return null;

            var m = Re.Match(url);
            if (m.Success)
                return m.Groups[1].Value;
            else
                return null;
        }
    }
}

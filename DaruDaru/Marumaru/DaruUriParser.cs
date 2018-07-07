using System;
using System.Text.RegularExpressions;

namespace DaruDaru.Marumaru
{
    internal class DaruUriParser
    {
        public static DaruUriParser Marumaru { get; } = new DaruUriParser(
                @"^https?:\/\/(?:[a-zA-Z0-9][a-zA-Z0-9-]*\.)*marumaru\.in\/(?:@*\/)*(?:@*\?(?:(?!uid)@+=@+&)*uid=)?(\d+)+.*$"
                    .Replace("@", @"[\w\-\._~:\/#\[\]@!\$&'\(\)\*\+,;=.%]"),
                code => new Uri("https://marumaru.in/b/manga/" + code)
            );

        public static DaruUriParser Archive { get; } = new DaruUriParser(
                @"^https?:\/\/(?:[^\.]*\.)?(?:mangaumaru\.com|shencomics\.com|yuncomics\.com|wasabisyrup\.com)\/archives\/([^\?""']+)",
                code => new Uri("http://wasabisyrup.com/archives/" + code)
            );

        private DaruUriParser(string regex, Func<string, Uri> toUri)
        {
            this.m_re = new Regex(regex, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            this.m_toUri = toUri;
        }

        private readonly Regex m_re;
        private readonly Func<string, Uri> m_toUri;

        public bool CheckUri(Uri uri)
            => this.m_re.IsMatch(uri.AbsoluteUri);

        public Uri GetUri(string code)
            => this.m_toUri(code);

        public string GetCode(Uri uri)
        {
            if (uri == null) return null;

            var m = this.m_re.Match(uri.AbsoluteUri);
            if (m.Success)
                return m.Groups[1].Value;
            else
                return null;
        }
    }
}

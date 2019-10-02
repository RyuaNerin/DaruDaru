using System;
using System.Text.RegularExpressions;
using DaruDaru.Config;

namespace DaruDaru.Marumaru
{
    internal class DaruUriParser
    {
        public static DaruUriParser Detail { get; } = new DaruUriParser(
                @"^https?:\/\/manamoa\d*\.net\/bbs\/page\.php\?(?:(?:hid=manga_detail&|(?!manga_id)@+=@+&)*manga_id=)?(\d+)+.*$"
                    .Replace("@", @"[\w\-\._~:\/#\[\]@!\$&'\(\)\*\+,;=.%]"),
                code => new Uri($"https://{ConfigManager.CurrentServerHost}/bbs/page.php?hid=manga_detail&manga_id=" + code)
            );

        public static DaruUriParser Manga { get; } = new DaruUriParser(
                @"^^https?:\/\/manamoa\d*\.net\/bbs\/page\.php\?(?:(?:bo_table=manga&|(?!wr_id)@+=@+&)*wr_id=)?(\d+)+.*$"
                    .Replace("@", @"[\w\-\._~:\/#\[\]@!\$&'\(\)\*\+,;=.%]"),
                code => new Uri($"https://{ConfigManager.CurrentServerHost}/bbs/board.php?bo_table=manga&wr_id=" + code)
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

        public Uri FixUri(Uri uri)
            => this.GetUri(this.GetCode(uri));

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

using System.Text.RegularExpressions;

namespace DaruDaru.Marumaru
{
    internal static class Regexes
    {
        public static readonly Regex MarumaruRegex = new Regex(
            @"^https?:\/\/(?:[a-zA-Z0-9][a-zA-Z0-9-]*\.)*marumaru\.in\/(?:@*\/)*(?:@*\?(?:(?!uid)@+=@+&)*uid=)?(\d+)+.*$"
                .Replace("@", @"[\w\-\._~:\/#\[\]@!\$&'\(\)\*\+,;=.%]"),
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex RegexArchive = new Regex(@"^https?:\/\/(?:[^\.]*\.)?(?:mangaumaru\.com|shencomics\.com|yuncomics\.com|wasabisyrup\.com)\/archives\/([^\?""']+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using DaruDaru.Config.Entries;
using DaruDaru.Marumaru.ComicInfo;

namespace DaruDaru.Utilities
{
    internal static class CollectionHelper
    {
        public static string[] GetUri(this IEnumerable<MarumaruEntry> coll)
            => coll.Select(e => e.Uri.AbsoluteUri)
                   .Distinct()
                   .ToArray();

        public static string[] GetCodes(this IEnumerable<MarumaruEntry> coll)
            => coll.Select(e => e.MaruCode)
                   .Distinct()
                   .ToArray();

        public static string[] GetPath(this IEnumerable<ArchiveEntry> coll)
            => coll.Select(e => e.ZipPath)
                   .Distinct()
                   .Where(e => File.Exists(e))
                   .ToArray();

        public static string[] GetUri(this IEnumerable<ArchiveEntry> coll)
            => coll.Select(e => e.Uri.AbsoluteUri)
                   .Distinct()
                   .ToArray();

        public static string[] GetCodes(this IEnumerable<ArchiveEntry> coll)
            => coll.Select(e => e.ArchiveCode)
                   .Distinct()
                   .ToArray();

        public static string[] GetPath(this IEnumerable<WasabiPage> coll)
            => coll.Select(e => e.ZipPath)
                   .Distinct()
                   .Where(e => File.Exists(e))
                   .ToArray();

        public static string[] GetUri(this IEnumerable<Comic> coll)
            => coll.Select(e => e.Uri.AbsoluteUri)
                   .Distinct()
                   .ToArray();
    }
}

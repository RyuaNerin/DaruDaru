using System.Collections.Generic;
using System.IO;
using System.Linq;
using DaruDaru.Config.Entries;
using DaruDaru.Marumaru.ComicInfo;

namespace DaruDaru.Utilities
{
    internal static class CollectionHelper
    {
        public static string[] GetUri(this IEnumerable<DetailEntry> coll)
            => coll.Select(e => e.Uri.AbsoluteUri)
                   .Distinct()
                   .ToArray();

        public static string[] GetCodes(this IEnumerable<DetailEntry> coll)
            => coll.Select(e => e.DetailCode)
                   .Distinct()
                   .ToArray();

        public static string[] GetPath(this IEnumerable<MangaArticleEntry> coll)
            => coll.Select(e => e.ZipPath)
                   .Distinct()
                   .Where(e => File.Exists(e))
                   .ToArray();

        public static string[] GetUri(this IEnumerable<MangaArticleEntry> coll)
            => coll.Select(e => e.Uri.AbsoluteUri)
                   .Distinct()
                   .ToArray();

        public static string[] GetCodes(this IEnumerable<MangaArticleEntry> coll)
            => coll.Select(e => e.ArchiveCode)
                   .Distinct()
                   .ToArray();

        public static string[] GetPath(this IEnumerable<MangaPage> coll)
            => coll.Select(e => e.ZipPath)
                   .Distinct()
                   .Where(e => File.Exists(e))
                   .ToArray();

        public static string[] GetDir(this IEnumerable<DetailPage> coll)
            => coll.Select(e => e.DirPath)
                   .Distinct()
                   .Where(e => Directory.Exists(e))
                   .ToArray();

        public static string[] GetUri(this IEnumerable<Comic> coll)
            => coll.Select(e => e.Uri.AbsoluteUri)
                   .Distinct()
                   .ToArray();
    }
}

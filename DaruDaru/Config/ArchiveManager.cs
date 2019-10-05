using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;
using DaruDaru.Config.Entries;

namespace DaruDaru.Config
{
    internal static class ArchiveManager
    {
        public static ObservableCollection<DetailEntry> Detail { get; } = new ObservableCollection<DetailEntry>();

        public static ObservableCollection<MangaEntry> Manga { get; } = new ObservableCollection<MangaEntry>();
        private static readonly HashSet<string> MangaCodeHash = new HashSet<string>(StringComparer.Ordinal);

        static ArchiveManager()
        {
            Manga.CollectionChanged += MangaArticle_CollectionChanged;
        }

        private static void MangaArticle_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var newItem = e.NewItems.Cast<MangaEntry>();
                
                foreach (var item in newItem)
                {
                    lock (MangaCodeHash)
                        MangaCodeHash.Add(item.MangaCode);
                }
            }

            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                var newItem = e.OldItems.Cast<MangaEntry>();
                
                foreach (var item in newItem)
                    lock (MangaCodeHash)
                        MangaCodeHash.Remove(item.MangaCode);
            }

            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                lock (MangaCodeHash)
                    MangaCodeHash.Clear();
            }
        }

        public static void UpdateDetail(string detailCode, string detailTitle, string[] detailMangaCodes, bool? finished = null)
        {
            lock (Detail)
            {
                bool found = false;
                DetailEntry entry = null;

                for (var i = 0; i < Detail.Count; ++i)
                {
                    entry = Detail[i];
                    if (string.Equals(entry.DetailCode, detailCode, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    entry = new DetailEntry
                    {
                        DetailCode = detailCode
                    };

                    Application.Current.Dispatcher.Invoke(new Action<DetailEntry>(Detail.Add), entry);
                }

                if (detailTitle != null)
                    entry.Title = detailTitle;

                if (detailMangaCodes != null)
                {
                    entry.LastUpdated = DateTime.Now;
                    entry.MangaCodes = detailMangaCodes;
                }

                if (finished.HasValue)
                    entry.Finished = finished.Value;

                ConfigManager.Save();
            }
        }

        public static void UpdateManga(string mangaCode, string mangaName, string mangaZipPath)
        {
            lock (Manga)
            {
                bool found = false;
                MangaEntry entry = null;

                for (var i = 0; i < Manga.Count; ++i)
                {
                    entry = Manga[i];
                    if (string.Equals(entry.MangaCode, mangaCode, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    entry = new MangaEntry
                    {
                        MangaCode = mangaCode,
                        TitleWithNo = mangaName,
                        ZipPath     = mangaZipPath,
                    };

                    Application.Current.Dispatcher.Invoke(new Action<MangaEntry>(Manga.Add), entry);

                    lock (Detail)
                        Detail.FirstOrDefault(le => le.MangaCodes != null && le.MangaCodes.Contains(mangaCode))?.RecalcCompleted();
                }

                entry.Archived = DateTime.Now;

                ConfigManager.Save();
            }
        }

        public static DetailEntry GetDetail(string detailCode)
        {
            lock (Detail)
            {
                for (var i = 0; i < Detail.Count; ++i)
                {
                    var entry = Detail[i];
                    if (string.Equals(entry.DetailCode, detailCode, StringComparison.OrdinalIgnoreCase))
                        return entry;
                }

                return null;
            }
        }
        public static MangaEntry GetManga(string mangaCode)
        {
            lock (Manga)
            {
                for (var i = 0; i < Manga.Count; ++i)
                {
                    var entry = Manga[i];
                    if (string.Equals(entry.MangaCode, mangaCode, StringComparison.OrdinalIgnoreCase))
                        return entry;
                }

                return null;
            }
        }

        public static bool ContainsManga(string mangaCode)
        {
            lock (MangaCodeHash)
                return MangaCodeHash.Contains(mangaCode);
        }
        public static IEnumerable<TData> IsNewManga<TData>(IEnumerable<TData> src, Func<TData, string> keySelector)
        {
            var lst = new List<TData>();

            lock (MangaCodeHash)
                foreach (var data in src)
                    if (!MangaCodeHash.Contains(keySelector(data)))
                        lst.Add(data);

            return lst;
        }

        public static void RemoveDetail(string[] detailCodes, bool removeFile)
        {
            lock (Detail)
            {
                int i = 0;
                while (i < Detail.Count)
                {
                    if (Array.IndexOf(detailCodes, Detail[i].DetailCode) >= 0)
                    {
                        if (removeFile)
                            RemoveManga(Detail[i].MangaCodes, true);

                        Detail.RemoveAt(i);
                    }
                    else
                        ++i;
                }
            }
        }

        public static void RemoveManga(string[] mangaCodes, bool removeFile)
        {
            lock (Manga)
            {
                string archiveCode;
                int i = 0;
                while (i < Manga.Count)
                {
                    archiveCode = Manga[i].MangaCode;
                    if (Array.IndexOf(mangaCodes, archiveCode) >= 0)
                    {
                        if (removeFile)
                            File.Delete(Manga[i].ZipPath);

                        Manga.RemoveAt(i);

                        lock (Detail)
                            Detail.FirstOrDefault(le => le.MangaCodes != null && le.MangaCodes.Contains(archiveCode))?.RecalcCompleted();
                    }
                    else
                        ++i;
                }
            }
        }

        internal static void ClearManga()
        {
            lock (Manga)
            {
                Manga.Clear();

                lock (Detail)
                    foreach (var ml in Detail)
                        ml.RecalcCompleted();
            }

        }

        public static bool ArchiveAllDownloaded(IEnumerable<string> archiveCodes)
        {
            lock (MangaCodeHash)
                return archiveCodes?.All(e => MangaCodeHash.Contains(e)) ?? false;
        }

        public static void RecalcCompleted()
        {
            lock (Detail)
                foreach (var item in Detail)
                    item.RecalcCompleted();
        }

        public static void ClearDuplicatedFileName()
        {
            lock (Manga)
            {
                var codes = Manga.GroupBy(e => e.ZipPath).Where(e => e.Count() > 1).SelectMany(e => e).Select(e => e.MangaCode).ToArray();

                RemoveManga(codes, true);
            }
        }

        public static void ClearDuplicatedLink()
        {
            lock (Detail)
            {
                foreach (var e in Detail.GroupBy(le => le.Title).Where(le => le.Count() > 1).Where(le => le.Any(lee => lee.MangaCodes.Length == 1)))
                {
                    foreach (var ee in e.OrderByDescending(le => le.MangaCodes.Length).Skip(1))
                        Detail.Remove(ee);
                }
            }
        }
    }
}

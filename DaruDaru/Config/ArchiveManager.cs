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
        public static ObservableCollection<MarumaruEntry> MarumaruLinks { get; } = new ObservableCollection<MarumaruEntry>();

        public static ObservableCollection<ArchiveEntry> Archives { get; } = new ObservableCollection<ArchiveEntry>();
        private static readonly HashSet<string> ArchiveHash = new HashSet<string>(StringComparer.Ordinal);

        static ArchiveManager()
        {
            Archives.CollectionChanged += Archives_CollectionChanged;
        }

        private static void Archives_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var newItem = e.NewItems.Cast<ArchiveEntry>();

                lock (ArchiveHash)
                    foreach (var item in newItem)
                        ArchiveHash.Add(item.ArchiveCode);
            }

            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                var newItem = e.OldItems.Cast<ArchiveEntry>();

                lock (ArchiveHash)
                    foreach (var item in newItem)
                        ArchiveHash.Remove(item.ArchiveCode);
            }

            else if (e.Action == NotifyCollectionChangedAction.Reset)
                lock (ArchiveHash)
                    ArchiveHash.Clear();
        }

        public static void UpdateMarumaru(string maruCode, string title, string[] archiveCodes)
        {
            lock (MarumaruLinks)
            {
                bool found = false;
                MarumaruEntry entry = null;

                for (var i = 0; i < MarumaruLinks.Count; ++i)
                {
                    entry = MarumaruLinks[i];
                    if (string.Equals(entry.MaruCode, maruCode, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    entry = new MarumaruEntry
                    {
                        MaruCode = maruCode
                    };

                    Application.Current.Dispatcher.Invoke(new Action<MarumaruEntry>(MarumaruLinks.Add), entry);
                }

                entry.Title = title;
                entry.LastUpdated = DateTime.Now;
                entry.ArchiveCodes = archiveCodes;

                ConfigManager.Save();
            }
        }
        
        public static void UpdateArchive(string archiveCode, string fullTitle, string zipPath)
        {
            lock (Archives)
            {
                bool found = false;
                ArchiveEntry entry = null;

                for (var i = 0; i < Archives.Count; ++i)
                {
                    entry = Archives[i];
                    if (string.Equals(entry.ArchiveCode, archiveCode, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    entry = new ArchiveEntry
                    {
                        ArchiveCode = archiveCode,
                        TitleWithNo = fullTitle,
                        ZipPath     = zipPath,
                    };

                    Application.Current.Dispatcher.Invoke(new Action<ArchiveEntry>(Archives.Add), entry);
                }

                entry.Archived = DateTime.Now;

                ConfigManager.Save();
            }
        }

        public static ArchiveEntry GetArchive(string archiveCode)
        {
            lock (Archives)
            {
                ArchiveEntry entry = null;

                for (var i = 0; i < Archives.Count; ++i)
                {
                    entry = Archives[i];
                    if (string.Equals(entry.ArchiveCode, archiveCode, StringComparison.OrdinalIgnoreCase))
                        return entry;
                }

                return null;
            }
        }

        public static bool CheckNewArchive(string archiveCode)
        {
            lock (ArchiveHash)
                return ArchiveHash.Contains(archiveCode);
        }
        public static IEnumerable<TData> IsNewArchive<TData>(IEnumerable<TData> src, Func<TData, string> keySelector)
        {
            var lst = new List<TData>();

            lock (ArchiveHash)
                foreach (var data in src)
                    if (!ArchiveHash.Contains(keySelector(data)))
                        lst.Add(data);

            return lst;
        }

        public static void RemoveMarumaru(string[] codes, bool removeFile)
        {
            lock (MarumaruLinks)
            {
                int i = 0;
                while (i < MarumaruLinks.Count)
                {
                    if (Array.IndexOf(codes, MarumaruLinks[i].MaruCode) >= 0)
                    {
                        if (removeFile)
                            RemoveArchives(MarumaruLinks[i].ArchiveCodes, true);

                        MarumaruLinks.RemoveAt(i);
                    }
                    else
                        ++i;
                }
            }
        }

        public static void RemoveArchives(string[] codes, bool removeFile)
        {
            lock (Archives)
            {
                int i = 0;
                while (i < Archives.Count)
                {
                    if (Array.IndexOf(codes, Archives[i].ArchiveCode) >= 0)
                    {
                        if (removeFile)
                            File.Delete(Archives[i].ZipPath);

                        Archives.RemoveAt(i);
                    }
                    else
                        ++i;
                }
            }
        }
    }
}

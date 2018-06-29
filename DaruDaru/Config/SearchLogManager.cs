using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using DaruDaru.Marumaru.Entry;

namespace DaruDaru.Config
{
    internal static class SearchLogManager
    {
        public static ObservableCollection<SearchLogEntry> Instance { get; } = new ObservableCollection<SearchLogEntry>();

        public static void Clear()
        {
            lock (Instance)
                Instance.Clear();
        }

        public static void UpdateUnsafe<T>(bool updateDatetime, IEnumerable<T> src, Func<T, string> toUrl, Func<T, string> toComicName, Func<T, int> toNoCount)
        {
            Application.Current.Dispatcher.Invoke(new Action<bool, IEnumerable<T>, Func<T, string>, Func<T, string>, Func<T, int>>(UpdateSafe), updateDatetime, src, toUrl, toComicName, toNoCount);
        }
        public static void UpdateSafe<T>(bool updateDatetime, IEnumerable<T> src, Func<T, string> toUrl, Func<T, string> toComicName, Func<T, int> toNoCount)
        {
            lock (Instance)
            {
                SearchLogEntry item = null;
                string url;
                string urlHash;
                string title;
                int noCount;
                bool found;

                int i;

                foreach (var obj in src)
                {
                    url = toUrl(obj);
                    title = toComicName?.Invoke(obj);

                    noCount = toNoCount != null ? toNoCount(obj) : -1;

                    urlHash = SearchLogEntry.GetUrlHash(url);
                    
                    found = false;
                    for (i = 0; i < Instance.Count; ++i)
                    {
                        item = Instance[i];

                        if (item.UrlHash == urlHash)
                        {
                            found = true;
                            break;
                        }
                    }
                    
                    if (!found)
                    {
                        item = new SearchLogEntry
                        {
                            Url = url
                        };
                        Instance.Add(item);
                    }

                    if (!string.IsNullOrWhiteSpace(title))
                        item.ComicName = title;

                    if (updateDatetime)
                        item.DateTime = DateTime.Now;

                    if (noCount != -1)
                        item.Count = noCount;
                }
            }
        }

        public static void UpdateUnsafe(bool updateDatetime, string url, string comicName = null, int noCount = -1)
        {
            Application.Current.Dispatcher.Invoke(new Action<bool, string, string, int>(UpdateSafe), updateDatetime, url, comicName, noCount);
        }
        public static void UpdateSafe(bool updateDatetime, string url, string comicName = null, int noCount = -1)
        {
            var urlHash = SearchLogEntry.GetUrlHash(url);

            SearchLogEntry item = null;

            lock (Instance)
            {
                var found = false;

                for (int i = 0; i < Instance.Count; ++i)
                {
                    item = Instance[i];

                    if (item.UrlHash == urlHash)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    item = new SearchLogEntry
                    {
                        Url = url
                    };
                    Instance.Add(item);
                }
                
                if (!string.IsNullOrWhiteSpace(comicName))
                    item.ComicName = comicName;

                if (updateDatetime)
                    item.DateTime = DateTime.Now;

                if (noCount != -1)
                    item.Count = noCount;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using DaruDaru.Core;
using Newtonsoft.Json;

namespace DaruDaru.Marumaru
{
    internal class SearchLogEntry : INotifyPropertyChanged
    {
        public static IEnumerable<SearchLogEntry> ConvertEnumerable(SearchLogEntry[] items)
        {
            foreach (var item in items)
                yield return item;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void InvokePropertyChanged([CallerMemberName] string propertyName = null)
            => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        
        public string Url { get; set; }

        private string m_urlHash;
        [JsonIgnore]
        public string UrlHash => this.m_urlHash ?? (this.m_urlHash = GetUrlHash(this.Url));
        
        public static string GetUrlHash(string url)
        {
            Match m;

            m = Regexes.MarumaruRegex.Match(url);
            if (m.Success)
                return "M-" + m.Groups[1].Value;

            m = Regexes.RegexArchive.Match(url);
            if (m.Success)
                return "A-" + m.Groups[1].Value;

            return url;
        }

        private string m_comicName = null;
        public string ComicName
        {
            get => this.m_comicName;
            set
            {
                this.m_comicName = value;
                this.InvokePropertyChanged("DisplayName");
            }
        }

        [JsonIgnore]
        public string DisplayName => this.ComicName ?? this.Url;

        private int m_count = -1;
        public int Count
        {
            get => this.m_count;
            set
            {
                this.m_count = value;
                this.InvokePropertyChanged("CountStr");
            }
        }

        [JsonIgnore]
        public string CountStr => this.m_count == -1 ? "" : this.m_count.ToString();

        private DateTime m_dateTime;
        public DateTime DateTime
        {
            get => this.m_dateTime;
            set
            {
                this.m_dateTime = value;
                this.InvokePropertyChanged();
            }
        }
    }

    internal static class SearchLog
    {
        private static readonly string FilePath = Path.Combine(App.BaseDirectory, "searchlog.cfg");
        private static readonly JsonSerializer Serializer = JsonSerializer.Create();
        
        public static ObservableCollection<SearchLogEntry> Collection { get; } = new ObservableCollection<SearchLogEntry>();

        static SearchLog()
        {
            if (File.Exists(FilePath))
            {
                try
                {
                    using (var fs = File.OpenRead(FilePath))
                    using (var sr = new StreamReader(fs, Encoding.UTF8))
                    using (var br = new JsonTextReader(sr))
                        Serializer.Populate(br, Collection);
                }
                catch
                {
                }
            }
        }

        public static void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));

            lock (Collection)
            {
                try
                {
                    using (var fs = File.OpenWrite(FilePath))
                    {
                        fs.SetLength(0);

                        using (var sr = new StreamWriter(fs, Encoding.UTF8))
                        using (var br = new JsonTextWriter(sr))
                            Serializer.Serialize(br, Collection);
                    }
                }
                catch
                {
                }

                File.SetAttributes(FilePath, File.GetAttributes(FilePath) | FileAttributes.Hidden | FileAttributes.System);
            }
        }

        public static void Clear()
        {
            lock (Collection)
                Collection.Clear();
        }

        public static void UpdateUnsafe<T>(bool updateDatetime, IEnumerable<T> src, Func<T, string> toUrl, Func<T, string> toComicName, Func<T, int> toNoCount)
        {
            Application.Current.Dispatcher.Invoke(new Action<bool, IEnumerable<T>, Func<T, string>, Func<T, string>, Func<T, int>>(UpdateSafe), updateDatetime, src, toUrl, toComicName, toNoCount);
        }
        public static void UpdateSafe<T>(bool updateDatetime, IEnumerable<T> src, Func<T, string> toUrl, Func<T, string> toComicName, Func<T, int> toNoCount)
        {
            lock (Collection)
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
                    for (i = 0; i < Collection.Count; ++i)
                    {
                        item = Collection[i];

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
                        Collection.Add(item);
                    }

                    if (!string.IsNullOrWhiteSpace(title))
                        item.ComicName = title;

                    if (updateDatetime)
                        item.DateTime = DateTime.Now;

                    if (noCount != -1)
                        item.Count = noCount;
                }

                Save();
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

            lock (Collection)
            {
                var found = false;

                for (int i = 0; i < Collection.Count; ++i)
                {
                    item = Collection[i];

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
                    Collection.Add(item);
                }
                
                if (!string.IsNullOrWhiteSpace(comicName))
                    item.ComicName = comicName;

                if (updateDatetime)
                    item.DateTime = DateTime.Now;

                if (noCount != -1)
                    item.Count = noCount;
                
                Save();
            }
        }
    }
}

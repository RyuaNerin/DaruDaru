using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace DaruDaru.Marumaru.Entries
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

        private string m_url;
        public string Url
        {
            get => this.m_url;
            set
            {
                this.m_url = value;
                this.UrlHash = GetUrlHash(value);
            }
        }

        [JsonIgnore]
        public string UrlHash { get; private set; }

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
}

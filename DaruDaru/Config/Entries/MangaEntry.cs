using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DaruDaru.Marumaru;
using Newtonsoft.Json;

namespace DaruDaru.Config.Entries
{
    internal class MangaEntry : INotifyPropertyChanged, IEntry
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void InvokePropertyChanged([CallerMemberName] string propertyName = null)
            => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        
        private string m_detailCode;
        public string DetailCode
        {
            get => this.m_detailCode;
            set
            {
                this.m_detailCode = value;
                this.Uri = DaruUriParser.Detail.GetUri(value);
            }
        }

        [JsonIgnore]
        public Uri Uri { get; private set; }

        private string m_title;
        public string Title
        {
            get => this.m_title;
            set
            {
                this.m_title = value;
                this.InvokePropertyChanged();
            }
        }

        private string[] m_mangaCodes;
        public string[] MangaCodes
        {
            get => this.m_mangaCodes;
            set
            {
                this.m_mangaCodes = value;
                this.InvokePropertyChanged();

                this.RecalcCompleted();
            }
        }

        private DateTime m_lastUpdated;
        public DateTime LastUpdated
        {
            get => this.m_lastUpdated;
            set
            {
                this.m_lastUpdated = value;
                this.InvokePropertyChanged();
            }
        }

        private bool m_finished = false;
        public bool Finished
        {
            get => this.m_finished;
            set
            {
                this.m_finished = value;
                this.InvokePropertyChanged();

                this.RecalcCompleted();
            }
        }

        public void RecalcCompleted()
        {
            this.Completed = this.Finished && ArchiveManager.ArchiveAllDownloaded(this.MangaCodes);
        }

        private bool m_completed = false;
        [JsonIgnore]
        public bool Completed
        {
            get => this.m_completed;
            private set
            {
                if (this.m_completed == value)
                    return;

                this.m_completed = value;
                this.InvokePropertyChanged();
            }
        }

        [JsonIgnore] string IEntry.Code => this.DetailCode;
        [JsonIgnore] string IEntry.Text => this.Title;
    }
}

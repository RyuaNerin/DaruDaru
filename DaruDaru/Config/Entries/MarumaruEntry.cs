using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DaruDaru.Marumaru;
using Newtonsoft.Json;

namespace DaruDaru.Config.Entries
{
    internal class MarumaruEntry : INotifyPropertyChanged, IEntry
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void InvokePropertyChanged([CallerMemberName] string propertyName = null)
            => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        
        private string m_maruCode;
        public string MaruCode
        {
            get => this.m_maruCode;
            set
            {
                this.m_maruCode = value;
                this.Uri = DaruUriParser.Marumaru.GetUri(value);
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

        public string[] ArchiveCodes { get; set; }

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

        [JsonIgnore] string IEntry.Code => this.MaruCode;
        [JsonIgnore] string IEntry.Text => this.Title;
    }
}

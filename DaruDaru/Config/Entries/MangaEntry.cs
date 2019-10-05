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

        public string ArchiveCode { get; set; }

        [JsonIgnore]
        public Uri Uri => DaruUriParser.Manga.GetUri(this.ArchiveCode);

        public string TitleWithNo { get; set; }

        public string ZipPath { get; set; }

        private DateTime m_archived;
        public DateTime Archived
        {
            get => this.m_archived;
            set
            {
                this.m_archived = value;
                this.InvokePropertyChanged();
            }
        }

        [JsonIgnore] string IEntry.Code => this.ArchiveCode;
        [JsonIgnore] string IEntry.Text => this.TitleWithNo;
    }
}

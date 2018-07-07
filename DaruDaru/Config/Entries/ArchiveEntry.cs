using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DaruDaru.Marumaru;
using Newtonsoft.Json;

namespace DaruDaru.Config.Entries
{
    internal class ArchiveEntry : INotifyPropertyChanged, IComparable<ArchiveEntry>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void InvokePropertyChanged([CallerMemberName] string propertyName = null)
            => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private string m_archiveCode;
        public string ArchiveCode
        {
            get => this.m_archiveCode;
            set
            {
                this.m_archiveCode = value;
                this.Uri = RegexArchive.GetUri(value);
            }
        }

        [JsonIgnore]
        public Uri Uri { get; private set; }

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

        public int CompareTo(ArchiveEntry other)
            => this.TitleWithNo.CompareTo(other.TitleWithNo);
    }
}

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using DaruDaru.Config.Entries;
using DaruDaru.Core;
using Newtonsoft.Json;

namespace DaruDaru.Config
{
    internal class ConfigManager : INotifyPropertyChanged
    {
        public static ConfigManager Instance { get; } = new ConfigManager();
        private static readonly string ConfigPath = Path.ChangeExtension(App.AppPath, ".cfg");
        private static readonly string ConfigPath2 = Path.ChangeExtension(App.AppPath, ".cfg.new");
        private static readonly JsonSerializer Serializer = JsonSerializer.Create();

        static ConfigManager()
        {
            Serializer.Formatting = Formatting.Indented;

            if (File.Exists(ConfigPath))
            {
                try
                {
                    using (var fs = File.OpenRead(ConfigPath))
                    using (var sr = new StreamReader(fs, Encoding.UTF8))
                    using (var br = new JsonTextReader(sr))
                        Serializer.Populate(br, Instance);
                }
                catch
                {
                }
            }
        }

        public static void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
            
            try
            {
                using (var fs = File.OpenWrite(ConfigPath2))
                {
                    fs.SetLength(0);

                    using (var sr = new StreamWriter(fs, Encoding.UTF8))
                    using (var br = new JsonTextWriter(sr))
                        Serializer.Serialize(br, Instance);
                }

                File.Delete(ConfigPath);
                File.Move(ConfigPath2, ConfigPath);
            }
            catch
            {
            }
        }

        public static ConfigCur Cur => new ConfigCur
        {
            SavePath = Instance.SavePath,
            CreateUrlLink = Instance.CreateUrlLink,
            UrlLinkPath = Instance.UrlLinkPath
        };


        private ConfigManager()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void InvokePropertyChanged([CallerMemberName] string propertyName = null)
            => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public static readonly string DefaultSavePath = Path.Combine(App.AppDir, "DaruDaru");
        private string m_savePath = DefaultSavePath;
        public string SavePath
        {
            get => !string.IsNullOrWhiteSpace(this.m_savePath) ? this.m_savePath : DefaultSavePath;
            set
            {
                this.m_savePath = string.IsNullOrWhiteSpace(value) ? DefaultSavePath : value;

                this.InvokePropertyChanged();
            }
        }

        private bool m_createUrlLink = true;
        public bool CreateUrlLink
        {
            get => this.m_createUrlLink;
            set
            {
                this.m_createUrlLink = value;
                this.InvokePropertyChanged();
            }
        }

        private string m_urlLinkPath = DefaultSavePath;
        public string UrlLinkPath
        {
            get => !string.IsNullOrWhiteSpace(this.m_urlLinkPath) ? this.m_urlLinkPath : DefaultSavePath;
            set
            {
                this.m_urlLinkPath = string.IsNullOrWhiteSpace(value) ? DefaultSavePath : value;

                this.InvokePropertyChanged();
            }
        }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public IList<MarumaruEntry> MaruLinks => ArchiveManager.MarumaruLinks;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public IList<ArchiveEntry> Archives => ArchiveManager.Archives;

        public string ProtectedUrl { get; set; }
    }
}

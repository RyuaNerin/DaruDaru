using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using DaruDaru.Core;
using DaruDaru.Core.Windows;

namespace DaruDaru.Marumaru.ComicInfo
{
    internal enum MaruComicState : long
    {
        Wait     = 0,
        Working  = 0x10000000,
        Complete = 0x20000000,
        Error    = 0x40000000,

        Working_1_GetInfomation = Working  + 1,
        Working_2_WaitDownload  = Working  + 2,
        Working_3_Downloading   = Working  + 3,
        Working_4_Compressing   = Working  + 4,

        Complete_1_Downloaded   = Complete + 1,
        Complete_2_Archived     = Complete + 2,
        Complete_3_NoNew        = Complete + 3,

        Error_1_Error           = Error    + 2,
        Error_2_Protected       = Error    + 1,

    }

    internal abstract class Comic : INotifyPropertyChanged
    {
        public static Comic CreateForSearch(IMainWindow mainWindow, bool addNewOnly, string url, string comicName)
        {
            if (Regexes.RegexArchive.IsMatch(url))
                return new WasabiPage(mainWindow, true, addNewOnly, url, comicName, null);

            return new MaruPage(mainWindow, true, addNewOnly, url, comicName);
        }

        private static readonly Regex InvalidRegex = new Regex($"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()))}]");
        protected static string ReplaceInvalid(string s) => InvalidRegex.Replace(s, "");

        protected static string ReplcaeHtmlTag(string s) => s.Replace("&nbsp;", " " )
                                                             .Replace("&lt;"  , "<" )
                                                             .Replace("&gt;"  , ">" )
                                                             .Replace("&amp;" , "&" )
                                                             .Replace("&quot;", "\"")
                                                             .Replace("&apos;", "'" )
                                                             .Replace("&copy;", "©" )
                                                             .Replace("&reg;" , "®" );
        
        protected static bool Retry(Func<bool> action)
        {
            int retries = 3;

            do
            {
                try
                {
                    if (action())
                        return true;
                }
                catch (WebException)
                {
                }
                catch (SocketException)
                {
                }
                catch (Exception ex)
                {
                    CrashReport.Error(ex);
                }

                Thread.Sleep(1000);
            } while (--retries > 0);

            return false;
        }

        public Comic(IMainWindow mainWindow, bool fromSearch, bool addNewOnly, string url, string comicName)
        {
            this.m_mainWindow = mainWindow;
            this.m_fromSearch = fromSearch;
            this.m_addNewOnly = addNewOnly;
            this.m_comicName  = comicName;

            this.Url          = url;
        }

        protected readonly IMainWindow m_mainWindow;
        protected readonly bool m_fromSearch;
        protected readonly bool m_addNewOnly;

        // Redirect 의 경우에 주소가 바뀌는 경우가 있다.
        public string Url { get; protected set; }

        private string m_comicName;
        public string ComicName
        {
            get => this.m_comicName;
            protected set
            {
                this.m_comicName = value;
                this.InvokePropertyChanged("DisplayName");
            }
        }

        private string m_comicNoName;
        public string ComicNoName
        {
            get => this.m_comicNoName;
            protected set
            {
                this.m_comicNoName = value;
                this.InvokePropertyChanged("DisplayName");
            }
        }

        public string DisplayName => (this.ComicNoName ?? (this.ComicName ?? this.Url));

        private long m_state = (long)MaruComicState.Wait;
        public MaruComicState State
        {
            get => (MaruComicState)Interlocked.Read(ref this.m_state);
            set
            {
                Interlocked.Exchange(ref this.m_state, (long)value);
                this.InvokePropertyChanged();
                
                if (value.HasFlag(MaruComicState.Error) ||
                    value.HasFlag(MaruComicState.Complete))
                {
                    Interlocked.Exchange(ref this.m_progressValue, this.m_progressMaximum);
                    this.InvokePropertyChanged("ProgressValue");
                }

                this.InvokePropertyChanged("StateText");
            }
        }

        public bool IsRunning  => this.State.HasFlag(MaruComicState.Working);
        public bool IsError    => this.State.HasFlag(MaruComicState.Error);
        public bool IsComplete => this.State.HasFlag(MaruComicState.Complete);

        private long m_progressValue;
        public long ProgressValue
        {
            get => Interlocked.Read(ref this.m_progressValue);
            protected set
            {
                Interlocked.Exchange(ref this.m_progressValue, value);
                this.InvokePropertyChanged("ProgressValue");
                this.InvokePropertyChanged("StateText");
            }
        }

        private int m_progressMaximum = 1;
        public int ProgressMaximum
        {
            get => this.m_progressMaximum;
            protected set
            {
                this.m_progressMaximum = value;
                this.InvokePropertyChanged();
            }
        }

        private string m_speedOrFileSize;
        public string SpeedOrFileSize
        {
            get => this.m_speedOrFileSize;
            protected set
            {
                this.m_speedOrFileSize = value;
                this.InvokePropertyChanged();
            }
        }
        
        public string StateText
        {
            get
            {
                switch (this.State)
                {
                    case MaruComicState.Wait:                    return "";

                    case MaruComicState.Working_1_GetInfomation: return "-";
                    case MaruComicState.Working_2_WaitDownload:  return $"0 / {this.ProgressMaximum}";
                    case MaruComicState.Working_3_Downloading:   return $"{this.ProgressValue} / {this.ProgressMaximum}";
                    case MaruComicState.Working_4_Compressing:   return "압축중";

                    case MaruComicState.Complete_1_Downloaded:   return "완료";
                    case MaruComicState.Complete_2_Archived:     return "저장됨";
                    case MaruComicState.Complete_3_NoNew:        return "새 작품 없음";

                    case MaruComicState.Error_2_Protected:       return "보호됨";
                    case MaruComicState.Error_1_Error:           return "오류";
                }

                return null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void InvokePropertyChanged([CallerMemberName] string propertyName = null)
            => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected void IncrementProgress()
        {
            Interlocked.Increment(ref this.m_progressValue);
            this.InvokePropertyChanged("ProgressValue");
            this.InvokePropertyChanged("StateText");
        }

        public bool Restart()
        {            
            if (!this.IsRunning)
            {
                ArchiveLog.Remove(WasabiPage.GetArchiveCode(this.Url));

                this.SpeedOrFileSize = null;

                Interlocked.Exchange(ref this.m_progressValue, 0);
                this.State = MaruComicState.Wait;
                return true;
            }

            return false;
        }

        public void GetInfomation()
        {
            int count = -1;
            this.GetInfomationPriv(ref count);

            if (this.m_fromSearch)
                SearchLog.UpdateUnsafe(false, this.Url, this.ComicName, count);

            this.m_mainWindow.WakeDownloader();
        }

        protected abstract void GetInfomationPriv(ref int count);

        public virtual void StartDownload()
        {
        }
    }
}

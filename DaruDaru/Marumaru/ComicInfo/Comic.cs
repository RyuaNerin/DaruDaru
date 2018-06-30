using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using DaruDaru.Config;
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

        Error_1_Error           = Error    + 1,
        Error_2_Protected       = Error    + 2,
        Error_3_NotSupport      = Error    + 3,
    }

    internal abstract class Comic : INotifyPropertyChanged
    {
        public static Comic CreateForSearch(IMainWindow mainWindow, bool addNewOnly, string url, string title)
        {
            if (RegexComic.CheckUrl(url))
                return new MaruPage(mainWindow, addNewOnly, url, title);

            if (RegexArchive.CheckUrl(url))
                return new WasabiPage(mainWindow, addNewOnly, url, title);

            return new UnknownPage(mainWindow, addNewOnly, url, title);
        }

        private static readonly Regex InvalidRegex = new Regex($"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()))}]", RegexOptions.Compiled);
        protected static string ReplaceInvalid(string s) => InvalidRegex.Replace(s, "_");

        protected static string ReplcaeHtmlTag(string s) => s.Replace("&nbsp;", " ")
                                                             .Replace("&lt;", "<")
                                                             .Replace("&gt;", ">")
                                                             .Replace("&amp;", "&")
                                                             .Replace("&quot;", "\"")
                                                             .Replace("&apos;", "'")
                                                             .Replace("&copy;", "©")
                                                             .Replace("&reg;", "®");

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

        public Comic(IMainWindow mainWindow, bool isRootItem, bool addNewOnly, string url, string title)
        {
            this.ConfigCur = ConfigManager.Cur;

            this.IMainWindow = mainWindow;
            this.RootItem = isRootItem;
            this.AddNewonly = addNewOnly;

            this.Url = url;
            this.Title = title;
        }
        
        protected internal ConfigCur ConfigCur { get; private set; }
        
        protected internal IMainWindow IMainWindow { get; private set; }

        /// <summary>새 작품 검색하기로 추가된 경우</summary>
        protected internal bool AddNewonly { get; private set; }

        /// <summary>큐에 직접 추가된 아이템</summary>
        protected internal bool RootItem { get; private set; }

        // Redirect 의 경우에 주소가 바뀌는 경우가 있다.
        public string Url { get; protected set; }

        private string m_title;
        public string Title
        {
            get => this.m_title;
            protected set
            {
                this.m_title = value;
                this.InvokePropertyChanged("DisplayName");
            }
        }

        private string m_titleWithNo;
        public string TitleWithNo
        {
            get => this.m_titleWithNo;
            protected set
            {
                this.m_titleWithNo = value;
                this.InvokePropertyChanged("DisplayName");
            }
        }

        public string DisplayName => this.TitleWithNo ?? (this.Title ?? this.Url);

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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void InvokePropertyChanged([CallerMemberName] string propertyName = null)
            => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

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

                    case MaruComicState.Error_1_Error:           return "오류";
                    case MaruComicState.Error_2_Protected:       return "보호됨";
                    case MaruComicState.Error_3_NotSupport:      return "지원하지 않음";
                }

                return null;
            }
        }

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
                this.SpeedOrFileSize = null;

                this.ProgressValue = 0;
                this.State = MaruComicState.Wait;
                return true;
            }

            return false;
        }

        public void GetInfomation()
        {
            int count = -1;

            this.GetInfomationPriv(ref count);

            this.IMainWindow.WakeDownloader();
        }

        protected abstract bool GetInfomationPriv(ref int count);

        public void StartDownload()
        {
            this.StartDownloadPriv();

            this.IMainWindow.WakeDownloader();
            this.IMainWindow.UpdateTaskbarProgress();
        }
        protected virtual void StartDownloadPriv()
        {
        }
    }
}

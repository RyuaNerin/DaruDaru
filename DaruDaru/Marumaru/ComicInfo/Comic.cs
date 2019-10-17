using System;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using DaruDaru.Config;
using DaruDaru.Core.Windows;
using DaruDaru.Utilities;

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
        Complete_4_Skip         = Complete + 4,

        Error_1_Error           = Error    + 1,
        Error_3_NotSupport      = Error    + 3,
        Error_5_NotFound        = Error    + 5,

        /// <summary>
        /// TODO
        /// </summary>
        Error_6_ServerError          = Error    + 6,
    }

    internal abstract class Comic : INotifyPropertyChanged
    {
        public static Comic CreateForSearch(bool addNewOnly, Uri uri, string title, bool skipMarumaru)
        {
            if (DaruUriParser.Detail.CheckUri(uri))
                return new DetailPage(addNewOnly, uri, title, skipMarumaru);

            if (DaruUriParser.Manga.CheckUri(uri))
                return new MangaPage(addNewOnly, uri, title);

            return new UnknownPage(addNewOnly, uri, title);
        }

        public Comic(bool addNewOnly, Uri uri, string title)
        {
            this.ConfigCur = ConfigManager.Cur;
            
            this.AddNewonly = addNewOnly;

            this.Uri   = uri;
            this.Title = !string.IsNullOrWhiteSpace(title) ? title : uri.ToString();
        }

        private readonly object WorkingLock = new object();

        protected internal ConfigCur ConfigCur { get; private set; }

        /// <summary>새 작품 검색하기로 추가된 경우</summary>
        protected internal bool AddNewonly { get; private set; }

        // Redirect 의 경우에 주소가 바뀌는 경우가 있다.
        public Uri Uri { get; protected set; }

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

        public string DisplayName => this.TitleWithNo ?? (this.Title ?? this.Uri.AbsoluteUri);

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

        public bool IsRunning
        {
            get
            {
                if (!Monitor.TryEnter(this.WorkingLock, 0))
                    return true;
                else
                {
                    Monitor.Exit(this.WorkingLock);
                    return false;
                }
            }
        }

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
                    case MaruComicState.Complete_4_Skip:         return "건너뜀";

                    case MaruComicState.Error_1_Error:           return "오류";
                    case MaruComicState.Error_3_NotSupport:      return "지원하지 않음";
                    case MaruComicState.Error_5_NotFound:        return "Not Found";
                    case MaruComicState.Error_6_ServerError:     return "서버 오류";
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

                MainWindow.Instance.WakeThread();
                return true;
            }

            return false;
        }

        public void GetInfomation(HttpClientEx hc)
        {
            int count = -1;
            bool res;

            lock (this.WorkingLock)
                res = this.GetInfomationPriv(hc, ref count);

            if (res)
                MainWindow.Instance.WakeThread();
        }

        protected abstract bool GetInfomationPriv(HttpClientEx hc, ref int count);

        public void StartDownload(HttpClientEx hc)
        {
            lock (this.WorkingLock)
                this.StartDownloadPriv(hc);

            MainWindow.Instance.WakeThread();
            MainWindow.Instance.UpdateTaskbarProgress();
        }
        protected virtual void StartDownloadPriv(HttpClientEx hc)
        {
        }

        private static readonly AutoResetEvent GetHtmlLock = new AutoResetEvent(true);
        protected HttpResponseMessage CallRequest(HttpClientEx hc, HttpRequestMessage req)
        {
            var res = hc.SendAsync(req).Exec();
            var body = res.Content.ReadAsStringAsync().Exec();

            if (body.Contains("recaptcha"))
            {
                res.Dispose();

                if (GetHtmlLock.WaitOne(0))
                {
                    try
                    {
                        Recaptcha frm = null;
                        try
                        {
                            frm = Application.Current.Dispatcher.Invoke(() => new Recaptcha(req.RequestUri));

                            Application.Current.Dispatcher.Invoke(frm.Show);

                            var succ = frm.Wait.Wait(Recaptcha.TimeOut);

                            Application.Current.Dispatcher.Invoke(frm.Close);

                            if (!succ)
                                return null;

                            HttpClientEx.Cookie.Add(frm.Cookies.GetCookies(req.RequestUri));
                        }
                        finally
                        {
                            if (frm != null)
                                Application.Current.Dispatcher.Invoke(frm.Dispose);
                        }
                    }
                    finally
                    {
                        GetHtmlLock.Set();
                    }
                }
                else
                {
                    GetHtmlLock.WaitOne();
                    GetHtmlLock.Set();
                }

                res = hc.SendAsync(req).Exec();
            }
            
            return res;
        }

        protected bool WaitFromHttpStatusCode(int retries, HttpStatusCode statusCode)
        {
            var v = (int)statusCode;

            if (v / 100 == 2)
                return false;

            switch ((int)statusCode)
            {
            case 429:
            case int n when 500 <= n && n < 600:
                if (retries > 1)
                    Thread.Sleep(30 * 1000);
                break;
            }
            return true;
        }

        protected void SetStatusFromHttpStatusCode(HttpStatusCode statusCode)
        {
            switch ((int)statusCode)
            {
            case 404:
                this.State = MaruComicState.Error_5_NotFound;
                break;

            case 429:
            case int n when 500 <= n && n < 600:
                this.State = MaruComicState.Error_6_ServerError;
                break;

            default:
                this.State = MaruComicState.Error_1_Error;
                break;
            }
        }
    }
}

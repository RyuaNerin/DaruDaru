using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shell;
using DaruDaru.Config;
using DaruDaru.Marumaru.ComicInfo;
using DaruDaru.Utilities;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace DaruDaru.Core.Windows
{
    internal partial class MainWindow : MetroWindow, IMainWindow
    {
        public static IMainWindow Instance { get; private set; }

        private readonly Adorner m_dragDropAdorner;

        public MainWindow()
        {
            Instance = this;

            InitializeComponent();

            CrashReport.Init();
            this.DataContext = ConfigManager.Instance;
            this.TaskbarItemInfo = new TaskbarItemInfo();

            this.m_dragDropAdorner = new DragDropAdorner(this.ctlTab, (Brush)this.FindResource("AccentColorBrush3"));

            var p = Environment.ProcessorCount;
            ThreadPool.SetMinThreads(p * 2, p);

            this.m_workerInfo = new Task[p];
            this.m_workerDown = new Task[p];
        }

        private async void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var obj = await Task.Factory.StartNew(LastRelease.CheckNewVersion);
            if (obj != null)
            {
                Process.Start(new ProcessStartInfo { FileName = obj.HtmlUrl, UseShellExecute = true });
                Application.Current.Shutdown();
                this.Close();
                return;
            }
        }

        public Window Window => this;

        public void DownloadUri(bool addNewOnly, Uri uri, string comicName)
            => this.ctlSearch.DownloadUri(addNewOnly, uri, comicName);

        public void DownloadUri<T>(bool addNewOnly, IEnumerable<T> src, Func<T, Uri> toUri, Func<T, string> toComicName)
            => this.ctlSearch.DownloadUri(addNewOnly, src, toUri, toComicName);

        public void InsertNewComic(Comic sender, IEnumerable<Comic> newItems, bool removeSender)
            => this.ctlSearch.InsertNewComic(sender, newItems, removeSender);

        public void UpdateTaskbarProgress()
        {
            var v = this.ctlSearch.QueueProgress;

            this.Dispatcher.Invoke(() =>
            {
                if (v == 1)
                {
                    this.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                }
                else
                {
                    this.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                    this.TaskbarItemInfo.ProgressValue = v;
                }
            });
        }

        public Task<string> ShowInput(string message, MetroDialogSettings settings = null)
            => DialogManager.ShowInputAsync(this, null, message, settings);

        public Task<MessageDialogResult> ShowMessageBox(string message, MessageDialogStyle style = MessageDialogStyle.Affirmative, MetroDialogSettings settings = null)
            => DialogManager.ShowMessageAsync(this, null, message, style, settings);

        public async void ShowMessageBox(string text, int timeOut)
        {
            using (var ct = new CancellationTokenSource())
            {
                var setting = new MetroDialogSettings
                {
                    CancellationToken = ct.Token
                };

                var date = DateTime.Now.AddMilliseconds(timeOut);
                var dialog = DialogManager.ShowMessageAsync(this, null, text, MessageDialogStyle.Affirmative, setting);

                await Task.Factory.StartNew(() => dialog.Wait(date - DateTime.Now));

                if (!dialog.IsCompleted && !dialog.IsCanceled)
                    ct.Cancel();
            }
        }

        public void SearchArchiveByCodes(string[] codes, string text)
        {
            this.ctlArchive.SearchArchiveByCodes(codes, text);
            this.ctlTab.SelectedIndex = 2;
        }

        private readonly Task[] m_workerInfo;
        private readonly Task[] m_workerDown;

        public void WakeQueue(int count)
        {
            lock (this.m_workerInfo)
            {
                for (int i = 0; i < this.m_workerInfo.Length; ++i)
                {
                    if (this.m_workerInfo[i] == null || this.m_workerInfo[i].IsCompleted)
                    {
                        this.m_workerInfo[i]?.Dispose();
                        this.m_workerInfo[i] = Task.Factory.StartNew(this.Worker_Infomation);

                        if (--count <= 0) return;
                    }
                }
            }
        }

        public void WakeDownloader(int count)
        {
            lock (this.m_workerDown)
            {
                for (int i = 0; i < this.m_workerDown.Length; ++i)
                {
                    if (this.m_workerDown[i] == null || this.m_workerDown[i].IsCompleted)
                    {
                        this.m_workerDown[i]?.Dispose();
                        this.m_workerDown[i] = Task.Factory.StartNew(this.Worker_Download);

                        if (--count <= 0) return;
                    }
                }
            }
        }

        private void Worker_Infomation()
        {
            Comic comic = null;

            while (this.ctlSearch.GetComicFromQueue(ref comic, MaruComicState.Wait, MaruComicState.Working_1_GetInfomation))
            {
                //Thread.Sleep(500);
                comic.GetInfomation();
            }
        }
        private void Worker_Download()
        {
            Comic comic = null;

            while (this.ctlSearch.GetComicFromQueue(ref comic, MaruComicState.Working_2_WaitDownload, MaruComicState.Working_3_Downloading))
            {
                //Thread.Sleep(500);
                comic.StartDownload();
            }
        }

        private bool m_dragDropAdornerEnabled = false;
        private void SetDragDropAdnorner(bool value)
        {
            if (this.m_dragDropAdornerEnabled == value)
                return;

            if (value) AdornerLayer.GetAdornerLayer(this.ctlTab).Add(this.m_dragDropAdorner);
            else AdornerLayer.GetAdornerLayer(this.ctlTab).Remove(this.m_dragDropAdorner);

            this.m_dragDropAdornerEnabled = value;
        }
        private void MetroWindow_DragEnter(object sender, DragEventArgs e)
        {
            var succ = false;

            if (e.Data.GetDataPresent(DataFormats.FileDrop) && e.Data.GetData(DataFormats.FileDrop) is string[] files)
                succ = files.Any(le => le.EndsWith(".url"));

            else
            {
                Uri uri;

                succ = GetUriFromStream(out uri, e.Data, "text/x-moz-url") ||
                       GetUriFromStream(out uri, e.Data, "UniformResourceLocatorW");
            }

            if (succ)
            {
                e.Effects = DragDropEffects.All & e.AllowedEffects;
                if (e.Effects != DragDropEffects.None)
                    SetDragDropAdnorner(true);
            }
        }

        private void MetroWindow_DragOver(object sender, DragEventArgs e)
        {
            this.MetroWindow_DragEnter(sender, e);
        }

        private void MetroWindow_DragLeave(object sender, DragEventArgs e)
        {
            SetDragDropAdnorner(false);
        }

        private void MetroWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var data = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (data != null)
                {
                    var uris = data.Where(le => le.EndsWith(".url"))
                                   .SelectMany(le => File.ReadAllLines(le).Where(lee => lee.StartsWith("URL=", StringComparison.CurrentCultureIgnoreCase)))
                                   .Select(le => Utility.CreateUri(le.Substring(4)))
                                   .ToArray();

                    this.DownloadUri(false, uris, le => le, null);
                }
            }
            else
            {
                Uri uri;

                if (GetUriFromStream(out uri, e.Data, "text/x-moz-url") ||
                    GetUriFromStream(out uri, e.Data, "UniformResourceLocatorW"))
                    this.DownloadUri(false, uri, null);
            }

            SetDragDropAdnorner(false);
        }

        private static bool GetUriFromStream(out Uri uri, IDataObject e, string dataFormat)
        {
            if (e.GetDataPresent(dataFormat) &&
                e.GetData(dataFormat, false) is MemoryStream dt)
            {
                string UriString;

                using (dt)
                {
                    dt.Position = 0;
                    UriString = Encoding.Unicode.GetString(dt.ToArray());
                }

                var e0 = UriString.IndexOf('\0');
                if (e0 > 0)
                    UriString = UriString.Substring(0, e0);

                UriString = UriString.Split(new char[] { '\r', '\n' })[0];

                if (Utility.TryCreateUri(UriString, out uri))
                    return true;
            }

            uri = null;
            return false;
        }
    }
}

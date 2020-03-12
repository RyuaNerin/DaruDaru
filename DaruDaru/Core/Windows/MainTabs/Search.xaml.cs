using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DaruDaru.Config;
using DaruDaru.Core.Windows.MainTabs.Controls;
using DaruDaru.Marumaru.ComicInfo;
using DaruDaru.Utilities;
using MahApps.Metro.Controls.Dialogs;

namespace DaruDaru.Core.Windows.MainTabs
{
    internal partial class Search : BaseControl
    {
        public ObservableCollection<Comic> Queue { get; } = new ObservableCollection<Comic>();

        public Search()
        {
            InitializeComponent();

            this.ListItemSource = this.Queue;

            this.Queue.CollectionChanged += (ls, le) =>
            {
                if (le.Action == NotifyCollectionChangedAction.Add ||
                    le.Action == NotifyCollectionChangedAction.Remove)
                    MainWindow.Instance.UpdateTaskbarProgress();
            };

            for (var i = 0; i < ConfigManager.Instance.WorkerCount; ++i)
            {
                new Thread(this.Worker_Infomation)
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Lowest,
                }.Start();

                new Thread(this.Worker_Download)
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Lowest,
                }.Start();
            }
        }

        public double QueueProgress
        {
            get
            {
                double max = 0;
                double val = 0;

                lock (this.Queue)
                {
                    max = this.Queue.Count;
                    val = this.Queue.Count(le => le.IsComplete || le.IsError);
                }

                return val / max;
            }
        }

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.SelectedItems.Count > 0;
        }

        private async void ctlMenuOpenFile_Click(object sender, RoutedEventArgs e)
        {
            var items = this.Get<MangaPage>().GetPath();
            if (items.Length == 0) return;

            if (items.Length > App.WarningItems &&
                !await MainWindow.Instance.ShowMassageBoxTooMany())
                return;

            if (HoneyViwer.TryCreate(out var hv))
                foreach (var file in items)
                    hv.Open(file);
        }

        private async void ctlMenuOpenDir_Click(object sender, RoutedEventArgs e)
        {
            var detailPages = this.Get<DetailPage>().GetDir ();
            var mangaPages  = this.Get<MangaPage >().GetPath();
            if (detailPages.Length == 0 && mangaPages.Length == 0) return;

            if ((Explorer.GetDirectoryCount(mangaPages) + detailPages.Length) > App.WarningItems &&
                !await MainWindow.Instance.ShowMassageBoxTooMany())
                return;

            foreach (var fe in detailPages)
                Explorer.Open(fe);

            Explorer.OpenAndSelect(mangaPages);
        }

        private async void ctlMenuOpenWeb_Click(object sender, RoutedEventArgs e)
        {
            var items = this.Get<Comic>().GetUri();
            if (items.Length == 0) return;

            if (items.Length > App.WarningItems &&
                !await MainWindow.Instance.ShowMassageBoxTooMany())
                return;

            foreach (var uri in items)
                Explorer.OpenUri(uri);
        }

        private void ctlMenuRetry_Click(object sender, RoutedEventArgs e)
        {
            var items = this.Get<Comic>();
            if (items.Length == 0) return;

            foreach (var item in items)
                item.Restart();
        }

        private void ctlMenuRetryIgnoreDownloadError_Click(object sender, RoutedEventArgs e)
        {
            var items = this.Get<Comic>();
            if (items.Length == 0) return;

            foreach (var item in items)
                item.Restart(true);
        }

        private void ctlMenuRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            var items = this.Get<Comic>();
            if (items.Length == 0) return;

            lock (this.Queue)
                foreach (var item in items)
                    this.Queue.Remove(item);
        }

        private void ctlMenuRemoveCompleted_Click(object sender, RoutedEventArgs e)
        {
            lock (this.Queue)
            {
                int i = 0;
                while (i < this.Queue.Count)
                {
                    if (this.Queue[i].IsComplete)
                        this.Queue.RemoveAt(i);
                    else
                        i++;
                }
            }
        }

        private async void ctlMenuRemoveAll_Click(object sender, RoutedEventArgs e)
        {
            lock (this.Queue)
                if (this.Queue.Count == 0)
                    return;

            var setting = new MetroDialogSettings
            {
                AffirmativeButtonText = "삭제",
                NegativeButtonText = "취소",
                DefaultButtonFocus = MessageDialogResult.Negative
            };

            if (await MainWindow.Instance.ShowMessageBox("모든 대기열을 삭제할까요?\n\n삭제 후엔 되돌릴 수 없어요", MessageDialogStyle.AffirmativeAndNegative, setting)
                == MessageDialogResult.Affirmative)
            {
                lock (this.Queue)
                {
                    int i = 0;
                    while (i < this.Queue.Count)
                    {
                        if (!this.Queue[i].IsRunning)
                            this.Queue.RemoveAt(i);
                        else
                            i++;
                    }
                }
            }
        }

        private void ctlMenuCopyUri_Click(object sender, RoutedEventArgs e)
        {
            var items = this.Get<Comic>().GetUri();
            if (items.Length == 0) return;

            Clipboard.SetText(string.Join(Environment.NewLine, items));
        }

        private void Viewer_ListViewItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.ListViewDefaultCommand(((ListViewItem)sender).Content as Comic);
        }
        private void Viewer_EnterCommand(object sender, RoutedEventArgs e)
        {
            this.ListViewDefaultCommand(this.SelectedItem as Comic);
        }

        private void ListViewDefaultCommand(Comic comic)
        {
            var detailPage = comic as DetailPage;
            var mangaPage  = comic as MangaPage;

            // 진행중일 때
            if (comic.State == MaruComicState.Wait ||
                comic.State.HasFlag(MaruComicState.Working))
            {
                Explorer.OpenUri(comic.Uri.AbsoluteUri);
            }
            else if (comic.State.HasFlag(MaruComicState.Complete))
            {
                if (mangaPage != null)
                {
                    if (!string.IsNullOrWhiteSpace(mangaPage.ZipPath) && File.Exists(mangaPage.ZipPath))
                        if (HoneyViwer.TryCreate(out var hv))
                            hv.Open(mangaPage.ZipPath);
                }
                else if (detailPage != null)
                {
                    if (!string.IsNullOrWhiteSpace(detailPage.DirPath) && Directory.Exists(detailPage.DirPath))
                        Explorer.Open(detailPage.DirPath);
                }
            }
            else if (comic.State.HasFlag(MaruComicState.Error))
            {
                comic.Restart();
            }
        }

        private void Viewer_ButtonClick(object sender, RoutedEventArgs e)
        {
            var uriString = this.Text?.Trim();

            if (!Utility.TryCreateUri(uriString, out Uri uri))
            {
                this.FocusTextBox();
                return;
            }

            this.DownloadUri(false, uri, null, false);

            this.Text = null;
            this.FocusTextBox();
        }

        public void WakeThread()
        {
            lock (this.Queue)
            {
                Monitor.PulseAll(this.Queue);
            }
        }

        public void DownloadUri(bool addNewOnly, Uri uri, string comicName, bool skipMarumaru)
        {
            lock (this.Queue)
            {
                if (this.CheckExisted(uri))
                {
                    var comicItem = Comic.CreateForSearch(addNewOnly, uri, comicName, skipMarumaru);
                    comicItem.PropertyChanged += this.Item_PropertyChanged;
                    this.Queue.Add(comicItem);
                }

                Monitor.PulseAll(this.Queue);
            }
        }

        public void DownloadUri<T>(bool addNewOnly, IEnumerable<T> src, Func<T, Uri> toUri, Func<T, string> toComicName, Func<T, bool> skipMarumaru)
        {
            lock (this.Queue)
            {
                foreach (var item in src)
                {
                    var uri = toUri(item);

                    if (this.CheckExisted(uri))
                    {
                        var comicItem = Comic.CreateForSearch(addNewOnly, uri, toComicName?.Invoke(item), skipMarumaru?.Invoke(item) ?? false);
                        comicItem.PropertyChanged += this.Item_PropertyChanged;
                        this.Queue.Add(comicItem);
                    }
                }

                Monitor.PulseAll(this.Queue);
            }
        }

        private void Item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "State")
                this.WakeThread();
        }

        public void InsertNewComic(Comic sender, IEnumerable<Comic> newItems, bool removeSender)
        {
            this.Dispatcher.Invoke(new Action<Comic, IEnumerable<Comic>, bool>(this.InsertNewComicPriv), sender, newItems, removeSender);
        }
        private void InsertNewComicPriv(Comic sender, IEnumerable<Comic> newItems, bool removeSender)
        {
            lock (this.Queue)
            {
                var index = this.Queue.IndexOf(sender);

                if (removeSender)
                    this.Queue.RemoveAt(index);
                else
                {
                    index += 1;
                }

                foreach (var newItem in newItems)
                {
                    if (this.CheckExisted(newItem.Uri))
                    {
                        this.Queue.Insert(index++, newItem);
                    }
                }

                Monitor.PulseAll(this.Queue);
            }
        }

        private bool CheckExisted(Uri uri)
        {
            for (int i = 0; i < this.Queue.Count; ++i)
                if (this.Queue[i].Uri == uri)
                    return false;

            return true;
        }

        public bool GetComicFromQueue(ref Comic comic, MaruComicState state, MaruComicState value)
        {
            lock (this.Queue)
            {
                for (var i = 0; i < this.Queue.Count; ++i)
                {
                    if (this.Queue[i].State == state)
                    {
                        comic = this.Queue[i];
                        comic.State = value;

                        return true;
                    }
                }

                Monitor.Wait(this.Queue);
            }

            return false;
        }

        private void Worker_Infomation()
        {
            Comic comic = null;

            using (var hc = new HttpClientEx())
            {
                while (true)
                {
                    if (this.GetComicFromQueue(ref comic, MaruComicState.Wait, MaruComicState.Working_1_GetInfomation))
                        comic.GetInfomation(hc);
                }
            }
        }
        private void Worker_Download()
        {
            Comic comic = null;

            using (var hc = new HttpClientEx())
            {
                while (true)
                {
                    if (this.GetComicFromQueue(ref comic, MaruComicState.Working_2_WaitDownload, MaruComicState.Working_3_Downloading))
                        comic.StartDownload(hc);
                }
            }
        }
    }
}

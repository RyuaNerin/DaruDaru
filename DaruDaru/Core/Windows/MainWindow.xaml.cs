using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using DaruDaru.Marumaru;
using DaruDaru.Marumaru.ComicInfo;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;

namespace DaruDaru.Core.Windows
{
    internal partial class MainWindow : MetroWindow, IMainWindow
    {
        private readonly ObservableCollection<Comic> m_queue = new ObservableCollection<Comic>();
        private readonly Adorner m_dragDropAdorner;

        public MainWindow()
        {
            InitializeComponent();
            
            this.m_dragDropAdorner = new DragDropAdorner(this.ctlTab, (Brush)this.FindResource("AccentColorBrush3"));

            CrashReport.Init();

            this.ctlSearch.ItemsSource = this.m_queue;
            this.ctlRecent.ItemsSource = SearchLog.Collection;
            
            var p = Environment.ProcessorCount;

            ThreadPool.SetMinThreads(p * 2, p);

            for (int i = 0; i < p; ++i) Task.Factory.StartNew(this.Worker_Infomation, i, TaskCreationOptions.LongRunning);
            for (int i = 0; i < p; ++i) Task.Factory.StartNew(this.Worker_Download  , i, TaskCreationOptions.LongRunning);
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
        
        public void SearchUrl(bool addNewOnly, string url, string comicName)
        {
            lock (this.m_queue)
                if (this.CheckExisted(url))
                    this.m_queue.Add(Comic.CreateForSearch(this, addNewOnly, url, comicName));

            SearchLog.UpdateSafe(true, url, null, -1);
            this.m_eventQueue.Set();
        }
        public void SearchUrl<T>(bool addNewOnly, IEnumerable<T> src, Func<T, string> toUrl, Func<T, string> toComicName)
        {
            int count = 0;
            lock (this.m_queue)
            {
                foreach (var item in src)
                {
                    count++;

                    var url = toUrl(item);

                    if (this.CheckExisted(url))
                        this.m_queue.Add(Comic.CreateForSearch(this, addNewOnly, url, toComicName?.Invoke(item)));
                }
            }

            SearchLog.UpdateSafe(true, src, toUrl, toComicName, null);

            this.m_eventQueue.Set();
        }

        public void InsertNewComic(Comic sender, IEnumerable<Comic> newItems, bool removeSender)
        {
            this.Dispatcher.Invoke(new Action<Comic, IEnumerable<Comic>, bool>(this.InsertNewComicPriv), sender, newItems, removeSender);
        }
        private void InsertNewComicPriv(Comic sender, IEnumerable<Comic> newItems, bool removeSender)
        {
            lock (this.m_queue)
            {
                var index = this.m_queue.IndexOf(sender);

                if (removeSender)
                    index += 1;
                else
                    this.m_queue.RemoveAt(index);

                foreach (var newItem in newItems)
                    if (this.CheckExisted(newItem.Url))
                        this.m_queue.Insert(index++, newItem);

                this.m_eventQueue.Set();
            }
        }

        private bool CheckExisted(string url)
        {
            for (int i = 0; i < this.m_queue.Count; ++i)
                if (this.m_queue[i].Url == url)
                    return false;

            return true;
        }

        public void WakeDownloader()
        {
            this.m_eventDownload.Set();
        }

        private void ctlSearchUrl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                this.ctlSearchDownload_Click(null, null);
        }

        private void ctlSearchDownload_Click(object sender, RoutedEventArgs e)
        {
            var url = this.ctlSearchUrl.Text;

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                this.ctlSearchUrl.SelectAll();
                this.ctlSearchUrl.Focus();
                return;
            }

            this.SearchUrl(false, url, null);

            this.ctlSearchUrl.Text = null;
        }

        private static string GetHoneyView()
        {
            string honeyView = null;

            try
            {
                using (var reg = Registry.CurrentUser.OpenSubKey("Software\\Honeyview"))
                    honeyView = (string)reg.GetValue("ProgramPath");
            }
            catch
            {
            }

            return !string.IsNullOrWhiteSpace(honeyView) && File.Exists(honeyView) ? honeyView : null;
        }

        private static void StartProcess(string filename, string arg = null)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = filename, Arguments = arg, UseShellExecute = true }).Dispose();
            }
            catch
            {
            }
        }

        #region Worker

        private readonly EventWaitHandle m_eventQueue    = new AutoResetEvent(false);
        private readonly EventWaitHandle m_eventDownload = new AutoResetEvent(false);

        private bool GetItem(ref Comic comic, MaruComicState state, MaruComicState value)
        {
            lock (this.m_queue)
            {
                for (var i = 0; i < this.m_queue.Count; ++i)
                {
                    if (this.m_queue[i].State == state)
                    {
                        comic = this.m_queue[i];
                        comic.State = value;

                        return true;
                    }
                }
            }

            return false;
        }
        private void Worker_Infomation(object othreadId)
        {
            int threadId = (int)othreadId;
            Comic comic = null;
            
            while (true)
            {
                if (this.GetItem(ref comic, MaruComicState.Wait, MaruComicState.Working_1_GetInfomation))
                {
                    Thread.Sleep(500);
                    comic.GetInfomation();
                }
                else
                    this.m_eventQueue.WaitOne();
            }
        }
        private void Worker_Download(object othreadId)
        {
            int threadId = (int)othreadId;
            Comic comic = null;

            while (true)
            {
                if (this.GetItem(ref comic, MaruComicState.Working_2_WaitDownload, MaruComicState.Working_3_Downloading))
                {
                    Thread.Sleep(500);
                    comic.StartDownload();
                }
                else
                    this.m_eventDownload.WaitOne();
            }
        }

        #endregion

        #region ctlSearch

        private void ctlSearchMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (this.ctlSearch.SelectedItems.Count == 0)
            {
                this.ctlSearchOpenDir.IsEnabled = false;
                this.ctlSearchOpenFile.IsEnabled = false;
                this.ctlSearchOpenWeb.IsEnabled = false;

                this.ctlSearchRetry.IsEnabled = false;

                this.ctlSearchRemoveItem.IsEnabled = false;
            }
            else
            {
                var items = this.ctlSearch.SelectedItems.Cast<Comic>();

                this.ctlSearchOpenFile.IsEnabled = this.ctlSearchOpenDir.IsEnabled = items.Any(le => le.State == MaruComicState.Complete_1_Downloaded);
                this.ctlSearchOpenWeb.IsEnabled = true;

                this.ctlSearchRetry.IsEnabled = items.Any(le => le.IsError);

                this.ctlSearchRemoveItem.IsEnabled = true;
            }
        }

        private void ctlSearchOpenDir_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlSearch.SelectedItems.Count == 0)
                return;

            // 다섯개까지만 연다
            var dirs = this.ctlSearch.SelectedItems.Cast<Comic>()
                                                   .Where(le => le is WasabiPage)
                                                   .Cast<WasabiPage>()
                                                   .Where(le => !string.IsNullOrWhiteSpace(le.FileDir))
                                                   .Select(le => le.FileDir)
                                                   .Where(le => !string.IsNullOrWhiteSpace(le) && Directory.Exists(le))
                                                   .Distinct()
                                                   .Take(5)
                                                   .ToArray();

            foreach (var dir in dirs)
                StartProcess(dir);
        }

        private void ctlSearchOpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlSearch.SelectedItems.Count == 0)
                return;

            var hv = GetHoneyView();
            if (hv != null)
            {
                // 다섯개까지만 연다
                var files = this.ctlSearch.SelectedItems.Cast<Comic>()
                                                        .Where(le => le is WasabiPage)
                                                        .Cast<WasabiPage>()
                                                        .Where(le => !string.IsNullOrWhiteSpace(le.FilePath))
                                                        .Select(le => le.FilePath)
                                                        .Where(le => !string.IsNullOrWhiteSpace(le) && File.Exists(le))
                                                        .Distinct()
                                                        .Take(5)
                                                        .ToArray();

                if (files.Length > 0)
                {
                    foreach (var file in files)
                        StartProcess(hv, $"\"{file}\"");
                }
            }
        }

        private void ctlSearchOpenWeb_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlSearch.SelectedItems.Count == 0)
                return;

            // 다섯개까지만 연다
            var urls = this.ctlSearch.SelectedItems.Cast<Comic>()
                                                    .Where(le => !string.IsNullOrWhiteSpace(le.Url))
                                                    .Select(le => le.Url)
                                                    .Distinct()
                                                    .Take(5)
                                                    .ToArray();

            foreach (var url in urls)
                StartProcess(url);
        }

        private void ctlSearchRetry_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlSearch.SelectedItems.Count == 0)
                return;

            var items = this.ctlSearch.SelectedItems.Cast<Comic>()
                                                    .ToArray();

            foreach (var item in items)
                item.Restart();

            this.m_eventQueue.Set();
        }

        private void ctlSearchRemoveCompleted_Click(object sender, RoutedEventArgs e)
        {
            lock (this.m_queue)
            {
                var items = this.m_queue.Where(le => le.IsComplete)
                                        .ToArray();

                foreach (var item in items)
                    this.m_queue.Remove(item);
            }
        }

        private void ctlSearchRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlSearch.SelectedItems.Count == 0)
                return;

            var items = this.ctlSearch.SelectedItems.Cast<Comic>()
                                                    .ToArray();

            lock (this.m_queue)
            {
                foreach (var item in items)
                    this.m_queue.Remove(item);
            }
        }

        private void ctlSearchRemoveAll_Click(object sender, RoutedEventArgs e)
        {
            lock (this.m_queue)
                this.m_queue.Clear();
        }

        private void ctlSearchItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var comic = ((ListViewItem)sender).Content as WasabiPage;
            if (comic != null)
            {
                if (!string.IsNullOrWhiteSpace(comic.FilePath) && File.Exists(comic.FilePath))
                {
                    var hv = GetHoneyView();
                    if (hv != null)
                        StartProcess(hv, $"\"{comic.FilePath}\"");
                }
            }
        }

        #endregion

        #region Recent

        private void ctlRecentContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            this.ctlRecentSearch.IsEnabled =
            this.ctlRecentOpenWeb.IsEnabled =

            this.ctlRecentRemoveItem.IsEnabled = this.ctlRecent.SelectedItems.Count >= 0;
        }

        private void ctlRecentSearchNew_Click(object sender, RoutedEventArgs e)
        {
            this.AddRecentSelectedItems(true);
        }

        private void ctlRecentSearch_Click(object sender, RoutedEventArgs e)
        {
            this.AddRecentSelectedItems(false);
        }

        private void AddRecentSelectedItems(bool addNewOnly)
        {
            if (this.ctlRecent.SelectedItems.Count == 0)
                return;

            var items = this.ctlRecent.SelectedItems.Cast<SearchLogEntry>()
                                                    .ToArray();

            this.SearchUrl(addNewOnly, items, e => e.Url, e => e.ComicName);
        }

        private void ctlRecentOpenWeb_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlRecent.SelectedItems.Count == 0)
                return;

            // 다섯개까지만 연다
            var items = this.ctlRecent.SelectedItems.Cast<SearchLogEntry>()
                                                    .Select(le => le.Url)
                                                    .Distinct()
                                                    .Take(5)
                                                    .ToArray();

            foreach (var item in items)
                Process.Start(new ProcessStartInfo { FileName = item, UseShellExecute = true })?.Dispose();
        }

        private void ctlRecentRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlSearch.SelectedItems.Count == 0)
                return;

            var items = this.ctlRecent.SelectedItems.Cast<SearchLogEntry>()
                                                    .ToArray();

            lock (SearchLog.Collection)
                foreach (var item in items)
                    SearchLog.Collection.Remove(item);
        }

        private void ctlRecentRemoveAll_Click(object sender, RoutedEventArgs e)
        {
            lock (SearchLog.Collection)
                SearchLog.Collection.Clear();
        }

        private void ctlRecentItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = ((ListViewItem)sender).Content as SearchLogEntry;
            if (item != null)
                this.SearchUrl(false, item.Url, item.ComicName);
        }

        #endregion

        #region Drag Drop

        private bool m_dragDropAdornerEnabled = false;
        private void SetDragDropAdnorner(bool value)
        {
            if (this.m_dragDropAdornerEnabled == value)
                return;

            if (value) AdornerLayer.GetAdornerLayer(this.ctlTab).Add   (this.m_dragDropAdorner);
            else       AdornerLayer.GetAdornerLayer(this.ctlTab).Remove(this.m_dragDropAdorner);

            this.m_dragDropAdornerEnabled = value;
        }
        private void ctlSearchGrid_DragEnter(object sender, DragEventArgs e)
        {
            var succ = false;

            if (e.Data.GetDataPresent(DataFormats.FileDrop) && e.Data.GetData(DataFormats.FileDrop) is string[] files)
                succ = files.Any(le => le.EndsWith(".url"));

            else if (e.Data.GetDataPresent("text/x-moz-url") && !string.IsNullOrEmpty(GetStringFromMemoryStream(e.Data, "text/x-moz-url")))
                succ = true;

            else if (e.Data.GetDataPresent("UniformResourceLocatorW") && !string.IsNullOrEmpty(GetStringFromMemoryStream(e.Data, "UniformResourceLocatorW")))
                succ = true;

            if (succ)
            {
                e.Effects = DragDropEffects.All & e.AllowedEffects;
                if (e.Effects != DragDropEffects.None)
                    SetDragDropAdnorner(true);
            }
        }

        private void ctlSearchGrid_DragOver(object sender, DragEventArgs e)
        {
            this.ctlSearchGrid_DragEnter(sender, e);
        }

        private void ctlSearchGrid_DragLeave(object sender, DragEventArgs e)
        {
            SetDragDropAdnorner(false);
        }

        private void ctlSearchGrid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var data = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (data != null)
                {
                    var urls = data.Where(le => le.EndsWith(".url"))
                                   .SelectMany(le => File.ReadAllLines(le).Where(lee => lee.StartsWith("URL=", StringComparison.CurrentCultureIgnoreCase)))
                                   .Select(le => le.Substring(4))
                                   .ToArray();

                    this.SearchUrl(false, urls, le => le, null);
                }
            }

            else if (e.Data.GetDataPresent("text/x-moz-url"))
            {
                var url = GetStringFromMemoryStream(e.Data, "text/x-moz-url");
                if (!string.IsNullOrWhiteSpace(url))
                    this.SearchUrl(false, url, null);
            }

            else if (e.Data.GetDataPresent("UniformResourceLocatorW"))
            {
                var url = GetStringFromMemoryStream(e.Data, "UniformResourceLocatorW");
                if (!string.IsNullOrWhiteSpace(url))
                    this.SearchUrl(false, url, null);
            }

            SetDragDropAdnorner(false);
        }

        private static string GetStringFromMemoryStream(IDataObject e, string dataFormat)
        {
            if (e.GetDataPresent(dataFormat) &&
                e.GetData(dataFormat, false) is MemoryStream dt)
            {
                string url;

                using (dt)
                {
                    dt.Position = 0;
                    url = Encoding.Unicode.GetString(dt.ToArray());
                }

                var e0 = url.IndexOf('\0');
                if (e0 > 0)
                    url = url.Substring(0, e0);

                url = url.Split(new char[] { '\r', '\n' })[0];

                if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                    return url;
            }

            return null;
        }

        #endregion

        #region Config

        private async void ctlConfigClearSearchLog_Click(object sender, RoutedEventArgs e)
        {
            var setting = new MetroDialogSettings
            {
                AffirmativeButtonText = "삭제",
                NegativeButtonText = "취소",
                DefaultButtonFocus = MessageDialogResult.Negative
            };

            if (await this.ShowMessageAsync(null, "모든 검색 기록을 삭제할까요?\n\n삭제 후엔 되돌릴 수 없어요", MessageDialogStyle.AffirmativeAndNegative, setting)
                == MessageDialogResult.Affirmative)
            {
                SearchLog.Clear();

                ShowMessage(null, "검색 기록을 삭제했어요.", 5000);
            }
        }

        private async void ctlConfigClearDownload_Click(object sender, RoutedEventArgs e)
        {
            var setting = new MetroDialogSettings
            {
                AffirmativeButtonText = "삭제",
                NegativeButtonText = "취소",
                DefaultButtonFocus = MessageDialogResult.Negative
            };

            if (await this.ShowMessageAsync(null, "모든 다운로드 기록을 삭제할까요?\n\n삭제 후엔 되돌릴 수 없어요", MessageDialogStyle.AffirmativeAndNegative, setting)
                == MessageDialogResult.Affirmative)
            {
                ArchiveLog.Clear();

                ShowMessage(null, "다운로드 기록을 삭제했어요.", 5000);
            }
        }

        private async void ShowMessage(string title, string text, int timeOut)
        {
            using (var ct = new CancellationTokenSource())
            {
                var setting = new MetroDialogSettings
                {
                    CancellationToken = ct.Token
                };

                var date = DateTime.Now.AddMilliseconds(timeOut);
                var dialog = this.ShowMessageAsync(title, text, MessageDialogStyle.Affirmative, setting);

                await Task.Factory.StartNew(() => dialog.Wait(date - DateTime.Now));

                if (!dialog.IsCompleted && !dialog.IsCanceled)
                    ct.Cancel();
            }

        }

        #endregion
    }
}
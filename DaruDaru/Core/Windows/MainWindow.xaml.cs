using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using DaruDaru.Config;
using DaruDaru.Config.Entries;
using DaruDaru.Marumaru;
using DaruDaru.Marumaru.ComicInfo;
using DaruDaru.Utilities;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using WinForms = System.Windows.Forms;

namespace DaruDaru.Core.Windows
{
    internal partial class MainWindow : MetroWindow, IMainWindow
    {
        private readonly ObservableCollection<Comic> m_queue = new ObservableCollection<Comic>();
        private readonly Adorner m_dragDropAdorner;
        private readonly ICollectionView m_viewMaru;
        private readonly ICollectionView m_viewArchive;

        public MainWindow()
        {
            InitializeComponent();

            CrashReport.Init();

            this.DataContext = ConfigManager.Instance;

            this.TaskbarItemInfo = new TaskbarItemInfo();

            this.m_dragDropAdorner = new DragDropAdorner(this.ctlTab, (Brush)this.FindResource("AccentColorBrush3"));
            
            this.ctlSearch.ItemsSource = this.m_queue;
            this.ctlMaru.ItemsSource = ArchiveManager.MarumaruLinks;
            this.ctlArchive.ItemsSource = ArchiveManager.Archives;

            this.m_viewMaru    = CollectionViewSource.GetDefaultView(this.ctlMaru   .ItemsSource);
            this.m_viewArchive = CollectionViewSource.GetDefaultView(this.ctlArchive.ItemsSource);

            this.m_viewMaru   .Filter = this.FilterMaru;
            this.m_viewArchive.Filter = this.FilterArchive;

            TextBoxHelper.SetButtonCommand(this.ctlMaruFilterText   , new SimpleCommand(e =>
            {
                this.ctlMaruFilterText.Text = null;
                this.m_maruFilterEnabled = false;
                this.m_viewMaru.Refresh();
            }));
            TextBoxHelper.SetButtonCommand(this.ctlArchiveFilterText, new SimpleCommand(e =>
            {
                this.ctlArchiveFilterText.Text = null;
                this.m_archiveFilterEnabled = 0;
                this.m_viewArchive.Refresh();
            }));

            var p = Environment.ProcessorCount;

            ThreadPool.SetMinThreads(p * 2, p);

            for (int i = 0; i < p; ++i) Task.Factory.StartNew(this.Worker_Infomation, TaskCreationOptions.LongRunning);
            for (int i = 0; i < p; ++i) Task.Factory.StartNew(this.Worker_Download  , TaskCreationOptions.LongRunning);

            this.m_queue.CollectionChanged += (ls, le) =>
            {
                if (le.Action == NotifyCollectionChangedAction.Add ||
                    le.Action == NotifyCollectionChangedAction.Remove)
                    this.UpdateTaskbarProgress();
            };
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
        
        public void SearchUri(bool addNewOnly, Uri uri, string comicName)
        {
            lock (this.m_queue)
                if (this.CheckExisted(uri))
                    this.m_queue.Add(Comic.CreateForSearch(this, addNewOnly, uri, comicName));
            
            this.m_eventQueue.Set();
        }
        public void SearchUri<T>(bool addNewOnly, IEnumerable<T> src, Func<T, Uri> toUri, Func<T, string> toComicName)
        {
            int count = 0;
            
            lock (this.m_queue)
            {
                foreach (var item in src)
                {
                    count++;

                    var uri = toUri(item);

                    if (this.CheckExisted(uri))
                        this.m_queue.Add(Comic.CreateForSearch(this, addNewOnly, uri, toComicName?.Invoke(item)));
                }
            }

            count = Math.Min(count, 4);
            while (count-- > 0)
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
                    this.m_queue.RemoveAt(index);
                else
                    index += 1;

                foreach (var newItem in newItems)
                    if (this.CheckExisted(newItem.Uri))
                        this.m_queue.Insert(index++, newItem);

                this.m_eventQueue.Set();
            }
        }

        private bool CheckExisted(Uri uri)
        {
            for (int i = 0; i < this.m_queue.Count; ++i)
                if (this.m_queue[i].Uri == uri)
                    return false;

            return true;
        }

        public void WakeDownloader()
        {
            this.m_eventDownload.Set();
        }

        public void UpdateTaskbarProgress()
        {
            double max = 0;
            double val = 0;

            lock (this.m_queue)
            {
                max = this.m_queue.Count;
                val = this.m_queue.Count(le => le.IsComplete || le.IsError);
            }

            this.Dispatcher.Invoke(() =>
            {
                if (val == max)
                {
                    this.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                }
                else
                {
                    this.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                    this.TaskbarItemInfo.ProgressValue = val / max;
                }
            });
        }

        private void ctlSearchUrl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                this.ctlSearchDownload_Click(null, null);
        }

        private void ctlSearchDownload_Click(object sender, RoutedEventArgs e)
        {
            var uriString = this.ctlSearchUrl.Text.Trim();

            if (!Uri.TryCreate(uriString, UriKind.Absolute, out Uri uri))
            {
                this.ctlSearchUrl.SelectAll();
                this.ctlSearchUrl.Focus();
                return;
            }

            this.SearchUri(false, uri, null);

            this.ctlSearchUrl.Text = null;
            this.ctlSearchUrl.Focus();
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

        private static void OpenDir(string directory)
        {
            try
            {
                Process.Start("explorer", $"\"{directory}\"").Dispose();
            }
            catch
            {
            }
        }
        private static void StartProcess(string filename, string arg = null)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = filename, Arguments = arg }).Dispose();
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
        private void Worker_Infomation()
        {
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
        private void Worker_Download()
        {
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
                this.ctlSearchOpenFile.IsEnabled = true;
                this.ctlSearchOpenDir.IsEnabled = true;
                this.ctlSearchRetry.IsEnabled = true;
                this.ctlSearchOpenWeb.IsEnabled = true;
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
                                                   .Where(le => !string.IsNullOrWhiteSpace(le.ZipPath))
                                                   .Select(le => Path.GetDirectoryName(le.ZipPath))
                                                   .Where(le => !string.IsNullOrWhiteSpace(le) && Directory.Exists(le))
                                                   .Distinct()
                                                   .Take(5)
                                                   .ToArray();

            foreach (var dir in dirs)
                OpenDir(dir);
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
                                                        .Where(le => !string.IsNullOrWhiteSpace(le.ZipPath))
                                                        .Select(le => le.ZipPath)
                                                        .Where(le => !string.IsNullOrWhiteSpace(le) && File.Exists(le))
                                                        .Distinct()
                                                        .Take(5)
                                                        .ToArray();
                
                foreach (var file in files)
                    StartProcess(hv, $"\"{file}\"");
            }
        }

        private void ctlSearchOpenWeb_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlSearch.SelectedItems.Count == 0)
                return;

            // 다섯개까지만 연다
            var uris = this.ctlSearch.SelectedItems.Cast<Comic>()
                                                   .Where(le => le.Uri != null)
                                                   .Select(le => le.Uri.AbsoluteUri)
                                                   .Distinct()
                                                   .Take(5)
                                                   .ToArray();

            foreach (var uri in uris)
                StartProcess(uri);
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
                int i = 0;
                while (i < this.m_queue.Count)
                {
                    if (this.m_queue[i].IsComplete)
                        this.m_queue.RemoveAt(i);
                    else
                        i++;
                }
            }
        }

        private void ctlSearchRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlSearch.SelectedItems.Count == 0)
                return;

            var items = this.ctlSearch.SelectedItems.Cast<Comic>()
                                                    .ToArray();

            lock (this.m_queue)
                foreach (var item in items)
                    this.m_queue.Remove(item);
        }

        private async void ctlSearchRemoveAll_Click(object sender, RoutedEventArgs e)
        {
            var setting = new MetroDialogSettings
            {
                AffirmativeButtonText = "삭제",
                NegativeButtonText = "취소",
                DefaultButtonFocus = MessageDialogResult.Negative
            };

            if (await this.ShowMessageAsync(null, "모든 대기열을 삭제할까요?\n\n삭제 후엔 되돌릴 수 없어요", MessageDialogStyle.AffirmativeAndNegative, setting)
                == MessageDialogResult.Affirmative)
            {
                lock (this.m_queue)
                    this.m_queue.Clear();
            }
        }

        private void ctlSearchItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var comic = ((ListViewItem)sender).Content as WasabiPage;
            if (comic != null)
            {
                if (!string.IsNullOrWhiteSpace(comic.ZipPath) && File.Exists(comic.ZipPath))
                {
                    var hv = GetHoneyView();
                    if (hv != null)
                        StartProcess(hv, $"\"{comic.ZipPath}\"");
                }
            }
        }

        #endregion

        #region Marumaru

        private void ctlMaruContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            this.ctlMaruSearch.IsEnabled =
            this.ctlMaruOpenWeb.IsEnabled =
            this.ctlMaru.SelectedItems.Count >= 0;
        }

        private void ctlMaruSearchNew_Click(object sender, RoutedEventArgs e)
        {
            this.AddRecentSelectedItems(true);
        }

        private void ctlMaruSearch_Click(object sender, RoutedEventArgs e)
        {
            this.AddRecentSelectedItems(false);
        }

        private void AddRecentSelectedItems(bool addNewOnly)
        {
            if (this.ctlMaru.SelectedItems.Count == 0)
                return;

            var items = this.ctlMaru.SelectedItems.Cast<MarumaruEntry>()
                                                  .ToArray();

            this.SearchUri(addNewOnly, items, e => e.Uri, e => e.Title);
        }

        private void ctlMaruOpenWeb_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlMaru.SelectedItems.Count == 0)
                return;

            // 다섯개까지만 연다
            var items = this.ctlMaru.SelectedItems.Cast<MarumaruEntry>()
                                                  .Select(le => le.Uri.AbsoluteUri)
                                                  .Distinct()
                                                  .Take(5)
                                                  .ToArray();

            foreach (var item in items)
                StartProcess(item);
        }

        private void ctlMaruArchiveSearch_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlMaru.SelectedItems.Count == 0)
                return;

            this.FilterArchiveByMarumaruEntry((MarumaruEntry)this.ctlMaru.SelectedItem);
        }

        private void ctlMaruItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = ((ListViewItem)sender).Content as MarumaruEntry;
            if (item != null)
                FilterArchiveByMarumaruEntry(item);
        }

        private void FilterArchiveByMarumaruEntry(MarumaruEntry entry)
        {
            this.ctlArchiveFilterText.Text = entry.Title;

            this.m_archiveFilterEnabled = 2;
            this.m_archiveFilterCodes = entry.ArchiveCodes;
            this.m_viewArchive.Refresh();

            this.ctlTab.SelectedIndex = 2;

        }

        private void ctlMaruFilterText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                this.ctlMaruFilter_Click(null, null);
        }

        private void ctlMaruFilter_Click(object sender, RoutedEventArgs e)
        {
            var str = this.ctlMaruFilterText.Text;

            if (string.IsNullOrWhiteSpace(str))
                this.m_maruFilterEnabled = false;
            else
            {
                this.m_maruFilterEnabled = true;
                this.m_maruFilterByCode = Uri.TryCreate(str, UriKind.Absolute, out Uri uri);

                if (this.m_maruFilterByCode)
                    this.m_maruFilterStr = DaruUriParser.Marumaru.GetCode(uri);
                else
                    this.m_maruFilterStr = str;
            }

            this.m_viewMaru.Refresh();
        }

        private bool     m_maruFilterEnabled = false;
        private bool     m_maruFilterByCode;
        private string   m_maruFilterStr;
        private bool FilterMaru(object o)
        {
            if (!this.m_maruFilterEnabled)
                return true;

            var entry = (MarumaruEntry)o;

            if (this.m_maruFilterByCode)
                return entry.MaruCode == this.m_maruFilterStr;
            else
                return entry.Title.Contains(this.m_maruFilterStr);

        }

        #endregion

        #region Archived

        private void ctlArchiveContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            this.ctlArchiveOpen.IsEnabled =
            this.ctlArchiveOpenDir.IsEnabled =
            this.ctlArchiveOpenWeb.IsEnabled =
            this.ctlArchive.SelectedItems.Count >= 0;
        }

        private void ctlArchiveOpen_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlArchive.SelectedItems.Count == 0)
                return;

            var hv = GetHoneyView();
            if (hv != null)
            {
                // 다섯개까지만 연다
                var files = this.ctlArchive.SelectedItems.Cast<ArchiveEntry>()
                                                         .Where(le => !string.IsNullOrWhiteSpace(le.ZipPath) && File.Exists(le.ZipPath))
                                                         .Select(le => le.ZipPath)
                                                         .Distinct()
                                                         .Take(5)
                                                         .ToArray();
                
                foreach (var file in files)
                    StartProcess(hv, $"\"{file}\"");
            }
        }

        private void ctlArchiveOpenDir_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlArchive.SelectedItems.Count == 0)
                return;
            
            // 다섯개까지만 연다
            var files = this.ctlArchive.SelectedItems.Cast<ArchiveEntry>()
                                                     .Where(le => !string.IsNullOrWhiteSpace(le.ZipPath))
                                                     .Select(le => Path.GetDirectoryName(le.ZipPath))
                                                     .Where(le => Directory.Exists(le))
                                                     .Distinct()
                                                     .Take(5)
                                                     .ToArray();
            
            foreach (var file in files)
                OpenDir(file);
        }

        private void ctlArchiveOpenWeb_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlArchive.SelectedItems.Count == 0)
                return;

            // 다섯개까지만 연다
            var items = this.ctlArchive.SelectedItems.Cast<ArchiveEntry>()
                                                     .Select(le => le.Uri.AbsoluteUri)
                                                     .Distinct()
                                                     .Take(5)
                                                     .ToArray();

            foreach (var item in items)
                StartProcess(item);
        }

        private void ctlArchive_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = ((ListViewItem)sender).Content as MarumaruEntry;
            if (item != null)
                this.SearchUri(false, item.Uri, item.Title);
        }

        private void ctlArchiveFilterText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                this.ctlArchiveFilter_Click(null, null);
        }

        private void ctlArchiveFilter_Click(object sender, RoutedEventArgs e)
        {
            var str = this.ctlArchiveFilterText.Text;

            if (string.IsNullOrWhiteSpace(str))
                this.m_archiveFilterEnabled = 0;
            else
            {
                this.m_archiveFilterEnabled = 1;
                this.m_archiveFilterByCode = Uri.TryCreate(str, UriKind.Absolute, out Uri uri);

                if (this.m_archiveFilterByCode)
                    this.m_archiveFilterStr = DaruUriParser.Archive.GetCode(uri);
                else
                    this.m_archiveFilterStr = str;
            }

            this.m_viewArchive.Refresh();
        }

        private byte     m_archiveFilterEnabled = 0;
        private bool     m_archiveFilterByCode;
        private string[] m_archiveFilterCodes;
        private string   m_archiveFilterStr;
        private bool FilterArchive(object o)
        {
            if (this.m_archiveFilterEnabled == 0)
                return true;

            var entry = (ArchiveEntry)o;

            if (this.m_archiveFilterEnabled == 1)
            {
                if (this.m_archiveFilterByCode)
                    return entry.ArchiveCode == this.m_archiveFilterStr;
                else
                    return entry.TitleWithNo.Contains(this.m_archiveFilterStr);
            }
            else
                return Array.IndexOf<string>(this.m_archiveFilterCodes, entry.ArchiveCode) >= 0;

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
                                   .Select(le => new Uri(le.Substring(4)))
                                   .ToArray();

                    this.SearchUri(false, uris, le => le, null);
                }
            }
            else
            {
                Uri uri;

                if (GetUriFromStream(out uri, e.Data, "text/x-moz-url") ||
                    GetUriFromStream(out uri, e.Data, "UniformResourceLocatorW"))
                    this.SearchUri(false, uri, null);
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

                if (Uri.TryCreate(UriString, UriKind.Absolute, out uri))
                    return true;
            }

            uri = null;
            return false;
        }

        #endregion

        #region Config

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

        private static string ShowDirectory(string curPath)
        {
            using (var fsd = new WinForms.FolderBrowserDialog())
            {
                fsd.SelectedPath = curPath;

                if (fsd.ShowDialog() == WinForms.DialogResult.OK)
                    return curPath;
            }

            return null;
        }

        private void ctlConfigDownloadPathSelect_Click(object sender, RoutedEventArgs e)
        {
            var dir = ShowDirectory(ConfigManager.Instance.SavePath);
            if (dir != null)
                ConfigManager.Instance.SavePath = dir;
        }

        private void ctlConfigDownloadPathOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenDir(ConfigManager.Instance.SavePath);
        }

        private void ctlConfigDownloadPathDefault_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.Instance.SavePath = ConfigManager.DefaultSavePath;
        }

        private void ctlConfigLinkPathSelect_Click(object sender, RoutedEventArgs e)
        {
            var dir = ShowDirectory(ConfigManager.Instance.UrlLinkPath);
            if (dir != null)
                ConfigManager.Instance.UrlLinkPath = dir;
        }

        private void ctlConfigLinkPathOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenDir(ConfigManager.Instance.UrlLinkPath);
        }

        private void ctlConfigLinkPathDefault_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.Instance.SavePath = ConfigManager.DefaultSavePath;
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
                ArchiveManager.Archives.Clear();
                ConfigManager.Save();

                ShowMessage(null, "다운로드 기록을 삭제했어요.", 5000);
            }
        }
        
        private async void ctlConfigDownloadProtected_Click(object sender, RoutedEventArgs e)
        {
            var set = new MetroDialogSettings
            {
                DefaultText = ConfigManager.Instance.ProtectedUri
            };

            var uriStr = await this.ShowInputAsync(null, "보호된 만화 링크를 입력해주세요\n(로그인을 위해서 필요해요)", set);

            if (string.IsNullOrWhiteSpace(uriStr) ||
                Uri.TryCreate(uriStr, UriKind.Absolute, out Uri uri) ||
                !DaruUriParser.Archive.CheckUri(uri))
            {
                this.ShowMessage(null, "주소를 확인해주세요", 5000);
                this.ctlConfigDownloadProtected.IsChecked = false;
                return;
            }

            var wnd = new Recaptcha(uriStr)
            {
                Owner = this
            };

            wnd.ShowDialog();

            if (wnd.RecaptchaResult == Recaptcha.Result.Canceled)
            {
                this.ctlConfigDownloadProtected.IsChecked = false;
                return;
            }

            if (wnd.RecaptchaResult == Recaptcha.Result.NonProtected)
            {
                this.ShowMessage(null, "보호된 만화 링크를 입력해주세요", 5000);
                this.ctlConfigDownloadProtected.IsChecked = false;
                return;
            }

            if (wnd.RecaptchaResult == Recaptcha.Result.UnknownError)
            {
                this.ShowMessage(null, "알 수 없는 오류가 발생하였습니다.", 5000);
                this.ctlConfigDownloadProtected.IsChecked = false;
                return;
            }

            ConfigManager.Instance.ProtectedUri = uriStr;

            this.ctlConfigDownloadProtected.IsEnabled = false;
        }

        #endregion

        class SimpleCommand : ICommand
        {
            public SimpleCommand(Action<object> execute)
            {
                this.m_execute = execute;
            }

            private readonly Action<object> m_execute;

            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter)
                => true;

            public void Execute(object parameter)
                => this.m_execute(parameter);

        }
    }
}

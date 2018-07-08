using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DaruDaru.Marumaru.ComicInfo;
using DaruDaru.Utilities;
using MahApps.Metro.Controls.Dialogs;

namespace DaruDaru.Core.Windows.MainTabs
{
    internal partial class Search : ContentControl
    {
        public ObservableCollection<Comic> Queue { get; } = new ObservableCollection<Comic>();

        public Search()
        {
            InitializeComponent();

            this.ctlViewer.ListItemSource = this.Queue;

            this.Queue.CollectionChanged += (ls, le) =>
            {
                if (le.Action == NotifyCollectionChangedAction.Add ||
                    le.Action == NotifyCollectionChangedAction.Remove)
                    MainWindow.Instance.UpdateTaskbarProgress();
            };
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
        
        private void ctlMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (this.ctlViewer.SelectedItems.Count == 0)
            {
                this.ctlMenuOpenDir.IsEnabled = false;
                this.ctlMenuOpenFile.IsEnabled = false;
                this.ctlMenuOpenWeb.IsEnabled = false;

                this.ctlMenuRetry.IsEnabled = false;

                this.ctlMenuRemoveItem.IsEnabled = false;
            }
            else
            {
                this.ctlMenuOpenFile.IsEnabled = true;
                this.ctlMenuOpenDir.IsEnabled = true;
                this.ctlMenuRetry.IsEnabled = true;
                this.ctlMenuOpenWeb.IsEnabled = true;
                this.ctlMenuRemoveItem.IsEnabled = true;
            }
        }

        private void ctlMenuOpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlViewer.SelectedItems.Count == 0)
                return;

            var hv = Utility.GetHoneyView();
            if (hv != null)
            {
                var files = this.ctlViewer.SelectedItems.Cast<Comic>()
                                                        .Where(le => le is WasabiPage)
                                                        .Cast<WasabiPage>()
                                                        .Where(le => !string.IsNullOrWhiteSpace(le.ZipPath))
                                                        .Select(le => le.ZipPath)
                                                        .Where(le => !string.IsNullOrWhiteSpace(le) && File.Exists(le))
                                                        .Distinct()
                                                        .Take(App.MaxItems)
                                                        .ToArray();

                foreach (var file in files)
                    Utility.StartProcess(hv, $"\"{file}\"");
            }
        }

        private void ctlMenuOpenDir_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlViewer.SelectedItems.Count == 0)
                return;
            
            var dirs = this.ctlViewer.SelectedItems.Cast<Comic>()
                                                   .Where(le => le is WasabiPage)
                                                   .Cast<WasabiPage>()
                                                   .Where(le => !string.IsNullOrWhiteSpace(le.ZipPath))
                                                   .Select(le => Path.GetDirectoryName(le.ZipPath))
                                                   .Where(le => !string.IsNullOrWhiteSpace(le) && Directory.Exists(le))
                                                   .Distinct()
                                                   .Take(App.MaxItems)
                                                   .ToArray();

            foreach (var dir in dirs)
                Utility.OpenDir(dir);
        }

        private void ctlMenuOpenWeb_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlViewer.SelectedItems.Count == 0)
                return;

            // 다섯개까지만 연다
            var uris = this.ctlViewer.SelectedItems.Cast<Comic>()
                                                   .Where(le => le.Uri != null)
                                                   .Select(le => le.Uri.AbsoluteUri)
                                                   .Distinct()
                                                   .Take(App.MaxItems)
                                                   .ToArray();

            foreach (var uri in uris)
                Utility.StartProcess(uri);
        }

        private void ctlMenuRetry_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlViewer.SelectedItems.Count == 0)
                return;

            var items = this.ctlViewer.SelectedItems.Cast<Comic>()
                                                    .ToArray();

            foreach (var item in items)
                item.Restart();

            MainWindow.Instance.WakeQueue(items.Length);
        }

        private void ctlMenuRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlViewer.SelectedItems.Count == 0)
                return;

            var items = this.ctlViewer.SelectedItems.Cast<Comic>()
                                                    .ToArray();

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
                    this.Queue.Clear();
            }
        }

        private void Viewer_ListViewItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var content = ((ListViewItem)sender).Content;

            if (content is MaruPage maruPage)
                Utility.OpenDir(maruPage.Uri.AbsoluteUri);

            else if (content is WasabiPage wasabiPage)
            {
                if (!string.IsNullOrWhiteSpace(wasabiPage.ZipPath) && File.Exists(wasabiPage.ZipPath))
                {
                    var hv = Utility.GetHoneyView();
                    if (hv != null)
                        Utility.StartProcess(hv, $"\"{wasabiPage.ZipPath}\"");
                }
            }
        }

        private void Viewer_ButtonClick(object sender, RoutedEventArgs e)
        {
            var uriString = this.ctlViewer.Text.Trim();

            if (!Uri.TryCreate(uriString, UriKind.Absolute, out Uri uri))
            {
                this.ctlViewer.FocusTextBox();
                return;
            }

            this.DownloadUri(false, uri, null);

            this.ctlViewer.Text = null;
            this.ctlViewer.FocusTextBox();
        }

        public void DownloadUri(bool addNewOnly, Uri uri, string comicName)
        {
            lock (this.Queue)
                if (this.CheckExisted(uri))
                    this.Queue.Add(Comic.CreateForSearch(addNewOnly, uri, comicName));

            MainWindow.Instance.WakeQueue(1);
        }
        public void DownloadUri<T>(bool addNewOnly, IEnumerable<T> src, Func<T, Uri> toUri, Func<T, string> toComicName)
        {
            int count = 0;

            lock (this.Queue)
            {
                foreach (var item in src)
                {
                    var uri = toUri(item);

                    if (this.CheckExisted(uri))
                    {
                        this.Queue.Add(Comic.CreateForSearch(addNewOnly, uri, toComicName?.Invoke(item)));
                        ++count;
                    }
                }
            }

            MainWindow.Instance.WakeQueue(count);
        }

        public void InsertNewComic(Comic sender, IEnumerable<Comic> newItems, bool removeSender)
        {
            this.Dispatcher.Invoke(new Action<Comic, IEnumerable<Comic>, bool>(this.InsertNewComicPriv), sender, newItems, removeSender);
        }
        private void InsertNewComicPriv(Comic sender, IEnumerable<Comic> newItems, bool removeSender)
        {
            int count = 0;

            lock (this.Queue)
            {
                var index = this.Queue.IndexOf(sender);

                if (removeSender)
                    this.Queue.RemoveAt(index);
                else
                    index += 1;

                foreach (var newItem in newItems)
                {
                    if (this.CheckExisted(newItem.Uri))
                    {
                        this.Queue.Insert(index++, newItem);
                        ++count;
                    }
                }
            }

            MainWindow.Instance.WakeQueue(count);
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
            }

            return false;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
            this.ctlMenuOpenDir.IsEnabled =
            this.ctlMenuOpenFile.IsEnabled =
            this.ctlMenuOpenWeb.IsEnabled =
            this.ctlMenuRetry.IsEnabled =
            this.ctlMenuRemoveItem.IsEnabled =
            this.SelectedItems.Count > 0;
        }

        private async void ctlMenuOpenFile_Click(object sender, RoutedEventArgs e)
        {
            var items = this.Get<WasabiPage>().GetPath();
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
            var items = this.Get<WasabiPage>().GetPath();
            if (items.Length == 0) return;

            if (Explorer.GetDirectoryCount(items) > App.WarningItems &&
                !await MainWindow.Instance.ShowMassageBoxTooMany())
                return;

            Explorer.OpenAndSelect(items);
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

            if (await MainWindow.Instance.ShowMessageBox("완료되거나 대기중인 모든 대기열을 삭제할까요?\n\n삭제 후엔 되돌릴 수 없어요", MessageDialogStyle.AffirmativeAndNegative, setting)
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
            var content = ((ListViewItem)sender).Content;

            if (content is MaruPage maruPage)
                Explorer.OpenUri(maruPage.Uri.AbsoluteUri);

            else if (content is WasabiPage wasabiPage)
            {
                if (wasabiPage.State == MaruComicState.Error_4_Captcha)
                    InputCaptcha(wasabiPage);

                else if (wasabiPage.IsComplete && !string.IsNullOrWhiteSpace(wasabiPage.ZipPath) && HoneyViwer.TryCreate(out var hv))
                    hv.Open(wasabiPage.ZipPath);
            }
        }

        private void Viewer_ButtonClick(object sender, RoutedEventArgs e)
        {
            var uriString = this.Text.Trim();

            if (!Utility.TryCreateUri(uriString, out Uri uri))
            {
                this.FocusTextBox();
                return;
            }

            this.DownloadUri(false, uri, null);

            this.Text = null;
            this.FocusTextBox();
        }

        private void InputCaptcha(WasabiPage page)
        {
            using (var wnd = new Recaptcha(MainWindow.Instance.Window, page.Uri))
            {
                wnd.ShowDialog();

                if (wnd.RecaptchaResult == Recaptcha.Result.Success)
                    page.Restart();
            }
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

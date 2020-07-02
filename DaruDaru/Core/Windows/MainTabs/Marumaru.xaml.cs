using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DaruDaru.Config;
using DaruDaru.Config.Entries;
using DaruDaru.Core.Windows.MainTabs.Controls;
using DaruDaru.Utilities;
using MahApps.Metro.Controls.Dialogs;
using Sentry;

namespace DaruDaru.Core.Windows.MainTabs
{
    internal partial class Marumaru : BaseControl
    {
        public Marumaru()
        {
            InitializeComponent();

            this.ListItemSource = ArchiveManager.Detail;
        }

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.SelectedItems.Count > 0;
        }

        private void ctlMenuArchiveSearch_Click(object sender, RoutedEventArgs e)
        {
            if (this.SelectedItem is DetailEntry entry)
                MainWindow.Instance.SearchArchiveByCodes(entry.MangaCodes, entry.Title);
        }

        private void ctlMenuSearchNew_Click(object sender, RoutedEventArgs e)
        {
            this.AddRecentSelectedItems(true);
        }

        private void ctlMenuSearch_Click(object sender, RoutedEventArgs e)
        {
            this.AddRecentSelectedItems(false);
        }

        private void AddRecentSelectedItems(bool addNewOnly)
        {
            var items = this.Get<DetailEntry>();
            if (items.Length == 0) return;

            MainWindow.Instance.DownloadUri(addNewOnly, items, e => e.Uri, e => e.Title, e => e.Completed);
        }

        private async void ctlMenuOpenWeb_Click(object sender, RoutedEventArgs e)
        {
            var items = this.Get<DetailEntry>().GetUri();
            if (items.Length == 0) return;

            if (items.Length > App.WarningItems &&
                !await MainWindow.Instance.ShowMessageBoxTooMany())
                return;

            foreach (var item in items)
                Explorer.OpenUri(item);
        }

        private void ctlMenuCopyUri_Click(object sender, RoutedEventArgs e)
        {
            var items = this.Get<DetailEntry>().GetUri();
            if (items.Length == 0) return;


            try
            {
                Clipboard.SetText(string.Join("\n", items));
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
            }
        }

        private void Viewer_ListViewItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var entry = ((ListViewItem)sender).Content as DetailEntry;
            if (entry != null)
                MainWindow.Instance.SearchArchiveByCodes(entry.MangaCodes, entry.Title);
        }

        private void ctlMenuRemoveOnly_Click(object sender, RoutedEventArgs e)
        {
            this.RemoveArchive(false);
        }

        private void ctlMenuRemoveAndDelete_Click(object sender, RoutedEventArgs e)
        {
            this.RemoveArchive(true);
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            var items = this.Get<DetailEntry>();
            var isChecked = items.Any(le => le.Finished);

            this.MenuItemFinished.IsChecked = isChecked;
        }

        private void ctlMenuFinished_Click(object sender, ExecutedRoutedEventArgs e)
        {
            var newValue = !this.MenuItemFinished.IsChecked;

            var items = this.Get<DetailEntry>();
            foreach (var item in items)
                item.Finished = newValue;
        }

        private async void RemoveArchive(bool removeFile)
        {
            var items = this.Get<DetailEntry>().GetCodes();
            if (items.Length == 0) return;

            var settings = new MetroDialogSettings
            {
                AffirmativeButtonText = "계속",
                NegativeButtonText    = "취소",
                DefaultButtonFocus    = MessageDialogResult.Negative
            };

            var message = removeFile ?
                "마나모아 기록과 모든 파일을 삭제합니다":
                "마나모아 기록에서 삭제합니다";

            if (await MainWindow.Instance.ShowMessageBox(message, MessageDialogStyle.AffirmativeAndNegative, settings)
                == MessageDialogResult.Negative)
                return;

            ArchiveManager.RemoveDetail(items, removeFile);
        }
    }
}

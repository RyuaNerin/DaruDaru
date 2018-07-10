using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DaruDaru.Config;
using DaruDaru.Config.Entries;
using DaruDaru.Core.Windows.MainTabs.Controls;
using DaruDaru.Utilities;
using MahApps.Metro.Controls.Dialogs;

namespace DaruDaru.Core.Windows.MainTabs
{
    internal partial class Marumaru : BaseControl
    {
        public Marumaru()
        {
            InitializeComponent();

            this.ListItemSource = ArchiveManager.MarumaruLinks;
        }

        private void ctlMenuContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            this.ctlMenuArchiveSearch.IsEnabled =
            this.ctlMenuSearch.IsEnabled =
            this.ctlMenuSearchNew.IsEnabled =
            this.ctlMenuOpenWeb.IsEnabled =
            this.ctlMenuCopyUri.IsEnabled =
            this.SelectedItems.Count >= 0;
        }

        private void ctlMenuArchiveSearch_Click(object sender, RoutedEventArgs e)
        {
            if (this.SelectedItem is MarumaruEntry entry)
                MainWindow.Instance.SearchArchiveByCodes(entry.ArchiveCodes, entry.Title);
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
            var items = this.Get<MarumaruEntry>();
            if (items.Length == 0) return;

            MainWindow.Instance.DownloadUri(addNewOnly, items, e => e.Uri, e => e.Title);
        }

        private async void ctlMenuOpenWeb_Click(object sender, RoutedEventArgs e)
        {
            var items = this.Get<MarumaruEntry>().GetUri();
            if (items.Length == 0) return;

            if (items.Length > App.WarningItems &&
                !await MainWindow.Instance.ShowMassageBoxTooMany())
                return;

            foreach (var item in items)
                Explorer.OpenUri(item);
        }

        private void ctlMenuCopyUri_Click(object sender, RoutedEventArgs e)
        {
            var items = this.Get<MarumaruEntry>().GetUri();
            if (items.Length == 0) return;

            Clipboard.SetText(string.Join("\n", items));
        }

        private void Viewer_ListViewItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var entry = ((ListViewItem)sender).Content as MarumaruEntry;
            if (entry != null)
                MainWindow.Instance.SearchArchiveByCodes(entry.ArchiveCodes, entry.Title);
        }

        private void ctlMenuRemoveOnly_Click(object sender, RoutedEventArgs e)
        {
            this.RemoveArchive(false);
        }

        private void ctlMenuRemoveAndDelete_Click(object sender, RoutedEventArgs e)
        {
            this.RemoveArchive(true);
        }

        private async void RemoveArchive(bool removeFile)
        {
            var items = this.Get<MarumaruEntry>().GetCodes();
            if (items.Length == 0) return;

            var settings = new MetroDialogSettings
            {
                AffirmativeButtonText = "계속",
                NegativeButtonText    = "취소",
                DefaultButtonFocus    = MessageDialogResult.Negative
            };

            var message = removeFile ?
                "마루마루 기록에서 삭제합니다" :
                "마루마루 기록과 모든 파일을 삭제합니다";

            if (await MainWindow.Instance.ShowMessageBox(message, MessageDialogStyle.AffirmativeAndNegative, settings)
                == MessageDialogResult.Negative)
                return;

            ArchiveManager.RemoveMarumaru(items, removeFile);
        }
    }
}

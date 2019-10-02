using System.Linq;
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
        public static ICommand ShowArchive     = Create("다운로드한 파일 검색",       "ShowArchive",     typeof(Marumaru), (Key.H, ModifierKeys.Control), (Key.Enter, ModifierKeys.None));
        public static ICommand SearchNew       = Create("다시 검색 (새로운 것만)",    "SearchNew",       typeof(Marumaru), (Key.R, ModifierKeys.Control));
        public static ICommand Search          = Create("다시 검색",                  "Search",          typeof(Marumaru), (Key.R, ModifierKeys.Control | ModifierKeys.Shift));
        public static ICommand Finished        = Create("완결 (갱신하지 않음)",       "Finished",        typeof(Marumaru), (Key.F, ModifierKeys.Control));
        public static ICommand OpenUri         = Create("웹 페이지 열기",             "OpenUri",         typeof(Marumaru), (Key.W, ModifierKeys.Control));
        public static ICommand CopyUri         = Create("링크 복사",                  "CopyUri",         typeof(Marumaru), (Key.C, ModifierKeys.Control));
        public static ICommand Remove          = Create("삭제",                       "Remove",          typeof(Marumaru));
        public static ICommand RemoveOnly      = Create("링크 삭제",                  "RemoveOnly",      typeof(Marumaru));
        public static ICommand RemoveAndDelete = Create("링크와 관련 파일 모두 삭제", "RemoveAndDelete", typeof(Marumaru));

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
            if (this.SelectedItem is MangaEntry entry)
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
            var items = this.Get<MangaEntry>();
            if (items.Length == 0) return;

            MainWindow.Instance.DownloadUri(addNewOnly, items, e => e.Uri, e => e.Title, e => e.Completed);
        }

        private async void ctlMenuOpenWeb_Click(object sender, RoutedEventArgs e)
        {
            var items = this.Get<MangaEntry>().GetUri();
            if (items.Length == 0) return;

            if (items.Length > App.WarningItems &&
                !await MainWindow.Instance.ShowMassageBoxTooMany())
                return;

            foreach (var item in items)
                Explorer.OpenUri(item);
        }

        private void ctlMenuCopyUri_Click(object sender, RoutedEventArgs e)
        {
            var items = this.Get<MangaEntry>().GetUri();
            if (items.Length == 0) return;

            Clipboard.SetText(string.Join("\n", items));
        }

        private void Viewer_ListViewItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var entry = ((ListViewItem)sender).Content as MangaEntry;
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
            var items = this.Get<MangaEntry>();
            var isChecked = items.Any(le => le.Finished);

            this.MenuItemFinished.IsChecked = isChecked;
        }

        private void ctlMenuFinished_Click(object sender, ExecutedRoutedEventArgs e)
        {
            var newValue = !this.MenuItemFinished.IsChecked;

            var items = this.Get<MangaEntry>();
            foreach (var item in items)
                item.Finished = newValue;
        }

        private async void RemoveArchive(bool removeFile)
        {
            var items = this.Get<MangaEntry>().GetCodes();
            if (items.Length == 0) return;

            var settings = new MetroDialogSettings
            {
                AffirmativeButtonText = "계속",
                NegativeButtonText    = "취소",
                DefaultButtonFocus    = MessageDialogResult.Negative
            };

            var message = removeFile ?
                "마루마루 기록과 모든 파일을 삭제합니다":
                "마루마루 기록에서 삭제합니다";

            if (await MainWindow.Instance.ShowMessageBox(message, MessageDialogStyle.AffirmativeAndNegative, settings)
                == MessageDialogResult.Negative)
                return;

            ArchiveManager.RemoveDetail(items, removeFile);
        }
    }
}

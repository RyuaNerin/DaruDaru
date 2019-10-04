using System;
using System.Collections.Specialized;
using System.IO;
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
    internal partial class Archive : BaseControl
    {
        public static ICommand OpenZip         = Create("꿀뷰로 열기",      "OpenZip",         typeof(Archive), (Key.H, ModifierKeys.Control), (Key.Enter, ModifierKeys.None));
        public static ICommand OpenDir         = Create("폴더 열기",        "OpenDir",         typeof(Archive), (Key.D, ModifierKeys.Control));
        public static ICommand OpenWeb         = Create("웹에서 보기",      "OpenWeb",         typeof(Archive), (Key.W, ModifierKeys.Control));
        public static ICommand OpenCopyZip     = Create("파일 복사",        "OpenCopyZip",     typeof(Archive), (Key.C, ModifierKeys.Control | ModifierKeys.Shift));
        public static ICommand OpenCopyWeb     = Create("웹 주소 복사",     "OpenCopyWeb",     typeof(Archive), (Key.C, ModifierKeys.Control));
        public static ICommand Remove          = Create("삭제",             "Remove",          typeof(Archive));
        public static ICommand RemoveOnly      = Create("기록 삭제",        "RemoveOnly",      typeof(Archive));
        public static ICommand RemoveAndDelete = Create("기록과 파일 삭제", "RemoveAndDelete", typeof(Archive));

        public Archive()
        {
            InitializeComponent();

            this.ListItemSource = ArchiveManager.Manga;
        }

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.SelectedItems.Count > 0;
        }

        private async void ctlMenuOpen_Click(object sender, RoutedEventArgs e)
        {
            var items = this.Get<MangaEntry>().GetPath();
            if (items.Length == 0) return;

            if (items.Count() > App.WarningItems &&
                !await MainWindow.Instance.ShowMassageBoxTooMany())
                return;

            if (HoneyViwer.TryCreate(out HoneyViwer hv))
                foreach (var item in items)
                    hv.Open(item);
        }

        private async void ctlMenuOpenDir_Click(object sender, RoutedEventArgs e)
        {
            var items = this.Get<MangaEntry>().GetPath();
            if (items.Length == 0) return;

            if (Explorer.GetDirectoryCount(items) > App.WarningItems &&
                !await MainWindow.Instance.ShowMassageBoxTooMany())
                return;

            Explorer.OpenAndSelect(items);
        }

        private async void ctlMenuOpenWeb_Click(object sender, RoutedEventArgs e)
        {
            var items = this.Get<MangaEntry>().GetUri();
            if (items.Length == 0) return;

            if (items.Count() > App.WarningItems &&
                !await MainWindow.Instance.ShowMassageBoxTooMany())
                return;

            foreach (var item in items)
                Explorer.OpenUri(item);
        }

        private void ctlMenuCopyFile_Click(object sender, RoutedEventArgs e)
        {
            var items = this.Get<MangaEntry>().GetPath();
            if (items.Length == 0) return;

            var files = new StringCollection();
            files.AddRange(items);

            Clipboard.SetFileDropList(files);
        }

        private void ctlMenuCopyUri_Click(object sender, RoutedEventArgs e)
        {
            var items = this.Get<MangaEntry>().GetUri();
            if (items.Length == 0) return;

            Clipboard.SetText(string.Join(Environment.NewLine, items));
        }

        private void Viewer_ListViewItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (HoneyViwer.TryCreate(out var hv))
            {
                var item = ((ListViewItem)sender).Content as MangaEntry;

                if (File.Exists(item.ZipPath))
                    hv.Open(item.ZipPath);
            }
        }

        private void ctlViewer_DragDropStarted(object sender, DragDropStartedEventArgs e)
        {
            if (e.IList.Count == 0)
                return;

            e.AllowedEffects = DragDropEffects.Copy | DragDropEffects.Link;
            e.Data           = e.IList.Cast<MangaEntry>().Select(le => le.ZipPath).ToArray();
            e.DataFormat     = DataFormats.FileDrop;
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
            var items = this.Get<MangaEntry>().GetCodes();
            if (items.Length == 0) return;

            var settings = new MetroDialogSettings
            {
                AffirmativeButtonText = "계속",
                NegativeButtonText    = "취소",
                DefaultButtonFocus    = MessageDialogResult.Negative
            };

            var message = !removeFile ?
                "다운로드 기록에서 삭제합니다" :
                "다운로드 기록과 파일을 삭제합니다";

            if (await MainWindow.Instance.ShowMessageBox(message, MessageDialogStyle.AffirmativeAndNegative, settings)
                == MessageDialogResult.Negative)
                return;

            ArchiveManager.RemoveManga(items, removeFile);
        }
    }
}

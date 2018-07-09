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

namespace DaruDaru.Core.Windows.MainTabs
{
    internal partial class Archive : ContentControl
    {
        public Archive()
        {
            InitializeComponent();

            this.ctlViewer.ListItemSource = ArchiveManager.Archives;
        }

        private void ctlMenuContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            this.ctlMenuOpen.IsEnabled =
            this.ctlMenuOpenDir.IsEnabled =
            this.ctlMenuOpenWeb.IsEnabled =
            this.ctlViewer.SelectedItems.Count >= 0;
        }

        private async void ctlMenuOpen_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlViewer.SelectedItems.Count == 0)
                return;

            if (HoneyViwer.TryCreate(out HoneyViwer hv))
            {
                // 다섯개까지만 연다
                var items = this.ctlViewer.SelectedItems.Cast<ArchiveEntry>()
                                                        .Where(le => !string.IsNullOrWhiteSpace(le.ZipPath))
                                                        .Select(le => le.ZipPath)
                                                        .Distinct()
                                                        .ToArray();

                if (items.Length > App.WarningItems &&
                    !await MainWindow.Instance.ShowMassageBoxTooMany())
                    return;

                foreach (var file in items)
                    hv.Open(file);
            }
        }

        private async void ctlMenuOpenDir_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlViewer.SelectedItems.Count == 0)
                return;
            
            var items = this.ctlViewer.SelectedItems.Cast<ArchiveEntry>()
                                                    .Where(le => !string.IsNullOrWhiteSpace(le.ZipPath))
                                                    .Select(le => le.ZipPath)
                                                    .Distinct()
                                                    .ToArray();

                if (Explorer.GetDirectoryCount(items) > App.WarningItems &&
                    !await MainWindow.Instance.ShowMassageBoxTooMany())
                    return;

            Explorer.OpenAndSelect(items);
        }

        private async void ctlMenuOpenWeb_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlViewer.SelectedItems.Count == 0)
                return;
            
            var items = this.ctlViewer.SelectedItems.Cast<ArchiveEntry>()
                                                    .Select(le => le.Uri.AbsoluteUri)
                                                    .Distinct()
                                                    .ToArray();

            if (items.Length > App.WarningItems &&
                !await MainWindow.Instance.ShowMassageBoxTooMany())
                return;

            foreach (var item in items)
                Explorer.OpenUri(item);
        }

        private void ctlMenuCopyUri_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlViewer.SelectedItems.Count == 0)
                return;

            var items = this.ctlViewer.SelectedItems.Cast<ArchiveEntry>()
                                                    .Select(le => le.Uri.AbsoluteUri)
                                                    .Distinct()
                                                    .ToArray();

            Clipboard.SetText(string.Join("\n", items));
        }

        private void ctlMenuCopyFile_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlViewer.SelectedItems.Count == 0)
                return;

            var items = this.ctlViewer.SelectedItems.Cast<ArchiveEntry>()
                                                    .Select(le => le.ZipPath)
                                                    .Distinct()
                                                    .ToArray();

            var files = new StringCollection();
            files.AddRange(items);

            Clipboard.SetFileDropList(files);
        }

        private void Viewer_ListViewItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (HoneyViwer.TryCreate(out var hv))
            {
                var item = ((ListViewItem)sender).Content as ArchiveEntry;

                if (File.Exists(item.ZipPath))
                    hv.Open(item.ZipPath);
            }
        }

        public void SearchArchiveByCodes(string[] codes, string text)
            => this.ctlViewer.FilterByCode(codes, text);

        private void ctlViewer_DragDropStarted(object sender, DragDropStartedEventArgs e)
        {
            if (e.IList.Count == 0)
                return;

            e.AllowedEffects = DragDropEffects.Copy | DragDropEffects.Link;
            e.Data           = e.IList.Cast<ArchiveEntry>().Select(le => le.ZipPath).ToArray();
            e.DataFormat     = DataFormats.FileDrop;
        }
    }
}

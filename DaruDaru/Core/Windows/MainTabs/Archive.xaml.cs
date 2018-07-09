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
            this.ctlMenuCopyFile.IsEnabled = 
            this.ctlMenuCopyUri.IsEnabled =
            this.ctlViewer.SelectedItems.Count >= 0;
        }

        private async void ctlMenuOpen_Click(object sender, RoutedEventArgs e)
        {
            var items = this.ctlViewer.Get<ArchiveEntry>().GetUri();
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
            var items = this.ctlViewer.Get<ArchiveEntry>().GetPath();
            if (items.Length == 0) return;

            if (Explorer.GetDirectoryCount(items) > App.WarningItems &&
                !await MainWindow.Instance.ShowMassageBoxTooMany())
                return;

            Explorer.OpenAndSelect(items);
        }

        private async void ctlMenuOpenWeb_Click(object sender, RoutedEventArgs e)
        {
            var items = this.ctlViewer.Get<ArchiveEntry>().GetUri();
            if (items.Length == 0) return;

            if (items.Count() > App.WarningItems &&
                !await MainWindow.Instance.ShowMassageBoxTooMany())
                return;

            foreach (var item in items)
                Explorer.OpenUri(item);
        }

        private void ctlMenuCopyFile_Click(object sender, RoutedEventArgs e)
        {
            var items = this.ctlViewer.Get<ArchiveEntry>().GetPath();
            if (items.Length == 0) return;

            var files = new StringCollection();
            files.AddRange(items);

            Clipboard.SetFileDropList(files);
        }

        private void ctlMenuCopyUri_Click(object sender, RoutedEventArgs e)
        {
            var items = this.ctlViewer.Get<ArchiveEntry>().GetUri();
            if (items.Length == 0) return;

            Clipboard.SetText(string.Join("\n", items));
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

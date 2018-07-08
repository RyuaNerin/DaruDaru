using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DaruDaru.Config;
using DaruDaru.Config.Entries;
using DaruDaru.Utilities;

namespace DaruDaru.Core.Windows.MainTabs
{
    public partial class Archive : ContentControl
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

        private void ctlMenuOpen_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlViewer.SelectedItems.Count == 0)
                return;

            var hv = Utility.GetHoneyView();
            if (hv != null)
            {
                // 다섯개까지만 연다
                var files = this.ctlViewer.SelectedItems.Cast<ArchiveEntry>()
                                                        .Where(le => !string.IsNullOrWhiteSpace(le.ZipPath) && File.Exists(le.ZipPath))
                                                        .Select(le => le.ZipPath)
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
            
            var files = this.ctlViewer.SelectedItems.Cast<ArchiveEntry>()
                                                     .Where(le => !string.IsNullOrWhiteSpace(le.ZipPath))
                                                     .Select(le => Path.GetDirectoryName(le.ZipPath))
                                                     .Where(le => Directory.Exists(le))
                                                     .Distinct()
                                                     .Take(App.MaxItems)
                                                     .ToArray();

            foreach (var file in files)
                Utility.OpenDir(file);
        }

        private void ctlMenuOpenWeb_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlViewer.SelectedItems.Count == 0)
                return;
            
            var items = this.ctlViewer.SelectedItems.Cast<ArchiveEntry>()
                                                    .Select(le => le.Uri.AbsoluteUri)
                                                    .Distinct()
                                                    .Take(App.MaxItems)
                                                    .ToArray();

            foreach (var item in items)
                Utility.StartProcess(item);
        }

        private void Viewer_ListViewItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = ((ListViewItem)sender).Content as MarumaruEntry;
            if (item != null)
                MainWindow.Instance.DownloadUri(false, item.Uri, item.Title);
        }

        public void SearchArchiveByCodes(string[] codes, string text)
            => this.ctlViewer.FilterByCode(codes, text);
    }
}

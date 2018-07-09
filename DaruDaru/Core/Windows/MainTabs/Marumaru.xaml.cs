using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DaruDaru.Config;
using DaruDaru.Config.Entries;
using DaruDaru.Utilities;

namespace DaruDaru.Core.Windows.MainTabs
{
    internal partial class Marumaru : ContentControl
    {
        public Marumaru()
        {
            InitializeComponent();

            this.ctlViewer.ListItemSource = ArchiveManager.MarumaruLinks;
        }

        private void ctlMenuContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            this.ctlMenuSearch.IsEnabled =
            this.ctlMenuOpenWeb.IsEnabled =
            this.ctlViewer.SelectedItems.Count >= 0;
        }

        private void ctlMenuArchiveSearch_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlViewer.SelectedItem is MarumaruEntry entry)
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
            if (this.ctlViewer.SelectedItems.Count == 0)
                return;

            var items = this.ctlViewer.SelectedItems.Cast<MarumaruEntry>()
                                                    .ToArray();

            MainWindow.Instance.DownloadUri(addNewOnly, items, e => e.Uri, e => e.Title);
        }

        private void ctlMenuOpenWeb_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlViewer.SelectedItems.Count == 0)
                return;

            // 다섯개까지만 연다
            var items = this.ctlViewer.SelectedItems.Cast<MarumaruEntry>()
                                                    .Select(le => le.Uri.AbsoluteUri)
                                                    .Distinct()
                                                    .Take(App.MaxItems)
                                                    .ToArray();

            foreach (var item in items)
                Utility.StartProcess(item);
        }

        private void ctlMenuCopyUri_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlViewer.SelectedItems.Count == 0)
                return;

            var items = this.ctlViewer.SelectedItems.Cast<MarumaruEntry>()
                                                    .Select(le => le.Uri.AbsoluteUri)
                                                    .Distinct()
                                                    .ToArray();

            Clipboard.SetText(string.Join("\n", items));
        }

        private void Viewer_ListViewItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var entry = ((ListViewItem)sender).Content as MarumaruEntry;
            if (entry != null)
                MainWindow.Instance.SearchArchiveByCodes(entry.ArchiveCodes, entry.Title);
        }
    }
}

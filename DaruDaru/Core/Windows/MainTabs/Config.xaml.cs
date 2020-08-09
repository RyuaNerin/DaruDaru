using System;
using System.Windows;
using System.Windows.Controls;
using DaruDaru.Config;
using DaruDaru.Utilities;
using MahApps.Metro.Controls.Dialogs;
using WinForms = System.Windows.Forms;

namespace DaruDaru.Core.Windows.MainTabs
{
    internal partial class Config : ContentControl
    {
        public Config()
        {
            InitializeComponent();

            this.ctlVersion.Text = $"v{App.Version}";
        }

        private static string ShowDirectory(string curPath)
        {
            using (var fsd = new WinForms.FolderBrowserDialog())
            {
                fsd.SelectedPath = curPath;

                if (fsd.ShowDialog() == WinForms.DialogResult.OK)
                    return fsd.SelectedPath;
            }

            return null;
        }

        private void ctlConfigDownloadPathSelect_Click(object sender, RoutedEventArgs e)
        {
            var dir = ShowDirectory(ConfigManager.Instance.SavePath);
            if (dir != null)
                ConfigManager.Instance.SavePath = dir;
        }

        private void ctlConfigDownloadPathOpen_Click(object sender, RoutedEventArgs e)
        {
            Explorer.Open(ConfigManager.Instance.SavePath);
        }

        private void ctlConfigDownloadPathDefault_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.Instance.SavePath = ConfigManager.DefaultSavePath;
        }

        private void ctlConfigLinkPathSelect_Click(object sender, RoutedEventArgs e)
        {
            var dir = ShowDirectory(ConfigManager.Instance.UrlLinkPath);
            if (dir != null)
                ConfigManager.Instance.UrlLinkPath = dir;
        }

        private void ctlConfigLinkPathOpen_Click(object sender, RoutedEventArgs e)
        {
            Explorer.Open(ConfigManager.Instance.UrlLinkPath);
        }

        private void ctlConfigLinkPathDefault_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.Instance.UrlLinkPath = ConfigManager.DefaultSavePath;
        }

        private void ctlWorkerCountDefault_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.Instance.WorkerCount = ConfigManager.WorkerCountDefault;
        }

        private void ctlConfigServerHost_LostFocus(object sender, RoutedEventArgs e)
        {
            if (Uri.TryCreate(this.ctlConfigServerHost.Text, UriKind.Absolute, out var u))
            {
                this.ctlConfigServerHost.Text = u.Host;
            }
        }

        private async void ctlConfigClearDownload_Click(object sender, RoutedEventArgs e)
        {
            var setting = new MetroDialogSettings
            {
                AffirmativeButtonText = "삭제",
                NegativeButtonText = "취소",
                DefaultButtonFocus = MessageDialogResult.Negative
            };

            if (await MainWindow.Instance.ShowMessageBox("모든 다운로드 기록을 삭제할까요?\n\n삭제 후엔 되돌릴 수 없어요", MessageDialogStyle.AffirmativeAndNegative, setting)
                == MessageDialogResult.Affirmative)
            {
                ArchiveManager.ClearManga();
                ConfigManager.Save();

                MainWindow.Instance.ShowMessageBox("다운로드 기록을 삭제했어요.", 5000);
            }
        }

        private async void ctlRemoveDuplicatedArchive_Click(object sender, RoutedEventArgs e)
        {
            var setting = new MetroDialogSettings
            {
                AffirmativeButtonText = "삭제",
                NegativeButtonText = "취소",
                DefaultButtonFocus = MessageDialogResult.Negative
            };

            if (await MainWindow.Instance.ShowMessageBox("파일명이 겹친 만화들을 모두 삭제할까요?\n이 작업은 되돌릴 수 없어요", MessageDialogStyle.AffirmativeAndNegative, setting)
                == MessageDialogResult.Affirmative)
            {
                ArchiveManager.ClearDuplicatedFileName();
                ConfigManager.Save();

                MainWindow.Instance.ShowMessageBox("중복으로 추정되는 만화들을 모두 지웠어요!", 5000);
            }
        }

        private async void ctlRemoveDuplicatedLink_Click(object sender, RoutedEventArgs e)
        {
            var setting = new MetroDialogSettings
            {
                AffirmativeButtonText = "삭제",
                NegativeButtonText = "취소",
                DefaultButtonFocus = MessageDialogResult.Negative
            };

            if (await MainWindow.Instance.ShowMessageBox("같은 링크가 두번 이상 등록되 있는 문제를 수정할까요?\n이 작업은 되돌릴 수 없어요", MessageDialogStyle.AffirmativeAndNegative, setting)
                == MessageDialogResult.Affirmative)
            {
                ArchiveManager.ClearDuplicatedLink();
                ConfigManager.Save();

                MainWindow.Instance.ShowMessageBox("두번 이상 등록된 링크들을 정리했어요!", 5000);
            }
        }
    }
}

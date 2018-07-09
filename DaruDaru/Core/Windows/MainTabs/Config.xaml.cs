using System;
using System.Windows;
using System.Windows.Controls;
using DaruDaru.Config;
using DaruDaru.Marumaru;
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
        }

        private static string ShowDirectory(string curPath)
        {
            using (var fsd = new WinForms.FolderBrowserDialog())
            {
                fsd.SelectedPath = curPath;

                if (fsd.ShowDialog() == WinForms.DialogResult.OK)
                    return curPath;
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
            ConfigManager.Instance.SavePath = ConfigManager.DefaultSavePath;
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
                ArchiveManager.Archives.Clear();
                ConfigManager.Save();

                MainWindow.Instance.ShowMessageBox("다운로드 기록을 삭제했어요.", 5000);
            }
        }

        private async void ctlConfigDownloadProtected_Click(object sender, RoutedEventArgs e)
        {
            var set = new MetroDialogSettings
            {
                DefaultText = ConfigManager.Instance.ProtectedUri
            };

            var uriStr = await MainWindow.Instance.ShowInput("보호된 만화 링크를 입력해주세요\n(로그인을 위해서 필요해요)", set);

            if (uriStr == null)
            {
                this.ctlConfigDownloadProtected.IsChecked = false;
                return;
            }

            if (string.IsNullOrWhiteSpace(uriStr) ||
                Utility.TryCreateUri(uriStr, out Uri uri) ||
                !DaruUriParser.Archive.CheckUri(uri))
            {
                MainWindow.Instance.ShowMessageBox("주소를 확인해주세요", 5000);
                this.ctlConfigDownloadProtected.IsChecked = false;
                return;
            }

            var wnd = new Recaptcha(uriStr)
            {
                Owner = MainWindow.Instance.Window
            };

            wnd.ShowDialog();

            if (wnd.RecaptchaResult == Recaptcha.Result.Canceled)
            {
                this.ctlConfigDownloadProtected.IsChecked = false;
                return;
            }

            if (wnd.RecaptchaResult == Recaptcha.Result.NonProtected)
            {
                MainWindow.Instance.ShowMessageBox("보호된 만화 링크를 입력해주세요", 5000);
                this.ctlConfigDownloadProtected.IsChecked = false;
                return;
            }

            if (wnd.RecaptchaResult == Recaptcha.Result.UnknownError)
            {
                MainWindow.Instance.ShowMessageBox("알 수 없는 오류가 발생하였습니다.", 5000);
                this.ctlConfigDownloadProtected.IsChecked = false;
                return;
            }

            ConfigManager.Instance.ProtectedUri = uriStr;

            this.ctlConfigDownloadProtected.IsEnabled = false;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shell;
using DaruDaru.Config;
using DaruDaru.Marumaru.ComicInfo;
using DaruDaru.Utilities;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace DaruDaru.Core.Windows
{
    internal partial class MainWindow : MetroWindow, IMainWindow
    {
        public static IMainWindow Instance { get; private set; }

        private readonly Adorner m_dragDropAdorner;

        public MainWindow()
        {
            Instance = this;

            this.InitializeComponent();

            CrashReport.Init();
            this.DataContext = ConfigManager.Instance;
            this.TaskbarItemInfo = new TaskbarItemInfo();

            this.m_dragDropAdorner = new DragDropAdorner(this.ctlTab, (Brush)this.FindResource("AccentColorBrush3"));
        }

        private async void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var obj = await Task.Factory.StartNew(LastRelease.CheckNewVersion);
            if (obj != null)
            {
                Explorer.OpenUri(obj.HtmlUrl);
                Application.Current.Shutdown();
                this.Close();
                return;
            }
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            App.Current.Shutdown();
        }

        public Window Window => this;

        public void DownloadUri(bool addNewOnly, Uri uri, string comicName, bool skipMarumaru)
            => this.ctlSearch.DownloadUri(addNewOnly, uri, comicName, skipMarumaru);

        public void DownloadUri<T>(bool addNewOnly, IEnumerable<T> src, Func<T, Uri> toUri, Func<T, string> toComicName, Func<T, bool> skipMarumaru)
            => this.ctlSearch.DownloadUri(addNewOnly, src, toUri, toComicName, skipMarumaru);

        public void InsertNewComic(Comic sender, IEnumerable<Comic> newItems, bool removeSender)
            => this.ctlSearch.InsertNewComic(sender, newItems, removeSender);

        public void UpdateTaskbarProgress()
        {
            var v = this.ctlSearch.QueueProgress;

            this.Dispatcher.Invoke(() =>
            {
                if (v == 1)
                {
                    this.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                }
                else
                {
                    this.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                    this.TaskbarItemInfo.ProgressValue = v;
                }
            });
        }

        public void WakeThread()
            => this.ctlSearch.WakeThread();

        public Task<string> ShowInput(string message, MetroDialogSettings settings = null)
            => DialogManager.ShowInputAsync(this, null, message, settings);

        public Task<MessageDialogResult> ShowMessageBox(string message, MessageDialogStyle style = MessageDialogStyle.Affirmative, MetroDialogSettings settings = null)
            => DialogManager.ShowMessageAsync(this, null, message, style, settings);

        public async void ShowMessageBox(string text, int timeOut)
        {
            using (var ct = new CancellationTokenSource())
            {
                var setting = new MetroDialogSettings
                {
                    CancellationToken = ct.Token
                };

                var date = DateTime.Now.AddMilliseconds(timeOut);
                var dialog = DialogManager.ShowMessageAsync(this, null, text, MessageDialogStyle.Affirmative, setting);

                await Task.Factory.StartNew(() => dialog.Wait(date - DateTime.Now));

                if (!dialog.IsCompleted && !dialog.IsCanceled)
                    ct.Cancel();
            }
        }

        public Task<bool> ShowMassageBoxTooMany()
        {
            var settings = new MetroDialogSettings
            {
                AffirmativeButtonText = "계속",
                NegativeButtonText = "취소",
                DefaultButtonFocus = MessageDialogResult.Negative
            };

            return DialogManager.ShowMessageAsync(this, null, "이 작업은 컴퓨터가 느려질 수도 있어요!", MessageDialogStyle.AffirmativeAndNegative, settings).ContinueWith(e => e.Result == MessageDialogResult.Affirmative);
        }

        public void SearchArchiveByCodes(string[] codes, string text)
        {
            this.ctlArchive.SearchArchiveByCodes(codes, text);
            this.ctlTab.SelectedIndex = 2;
        }

        private bool m_dragDropAdornerEnabled = false;
        private void SetDragDropAdnorner(bool value)
        {
            if (this.m_dragDropAdornerEnabled == value)
                return;

            if (value) AdornerLayer.GetAdornerLayer(this.ctlTab).Add(this.m_dragDropAdorner);
            else AdornerLayer.GetAdornerLayer(this.ctlTab).Remove(this.m_dragDropAdorner);

            this.m_dragDropAdornerEnabled = value;
        }
        private void MetroWindow_DragEnter(object sender, DragEventArgs e)
        {
            var succ = false;

            if (e.Data.GetDataPresent(DataFormats.FileDrop) && e.Data.GetData(DataFormats.FileDrop) is string[] files)
                succ = files.Any(le => le.EndsWith(".url"));

            else
            {
                Uri uri;

                succ = GetUriFromStream(out uri, e.Data, "text/x-moz-url") ||
                       GetUriFromStream(out uri, e.Data, "UniformResourceLocatorW");
            }

            if (succ)
            {
                e.Effects = DragDropEffects.All & e.AllowedEffects;
                if (e.Effects != DragDropEffects.None)
                    SetDragDropAdnorner(true);
            }
        }

        private void MetroWindow_DragOver(object sender, DragEventArgs e)
        {
            this.MetroWindow_DragEnter(sender, e);
        }

        private void MetroWindow_DragLeave(object sender, DragEventArgs e)
        {
            SetDragDropAdnorner(false);
        }

        private void MetroWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var data = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (data != null)
                {
                    Task.Factory.StartNew(() =>
                    {
                        var uriList = new List<Uri>(data.Length);

                        Parallel.ForEach(data, lnkPath =>
                        {
                            if (!lnkPath.EndsWith(".url"))
                                return;

                            try
                            {
                                using (var fs = File.Open(lnkPath, FileMode.Open))
                                {
                                    var reader = new StreamReader(fs);

                                    string line = null;
                                    while ((line = reader.ReadLine()) != null)
                                    {
                                        if (line.StartsWith("URL=", StringComparison.CurrentCultureIgnoreCase))
                                        {
                                            if (Utility.TryCreateUri(line.Substring(4), out var uri))
                                            {
                                                uriList.Add(uri);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            catch
                            {
                            }
                        });

                        Application.Current.Dispatcher.Invoke(() => this.DownloadUri(false, uriList, le => le, null, null));
                    });
                }
            }
            else
            {
                Uri uri;

                Task.Factory.StartNew(() =>
                {
                    if (GetUriFromStream(out uri, e.Data, "text/x-moz-url") || GetUriFromStream(out uri, e.Data, "UniformResourceLocatorW"))
                        Application.Current.Dispatcher.Invoke(() => this.DownloadUri(false, uri, null, false));
                });
            }

            this.SetDragDropAdnorner(false);
        }

        private static bool GetUriFromStream(out Uri uri, IDataObject e, string dataFormat)
        {
            if (e.GetDataPresent(dataFormat) &&
                e.GetData(dataFormat, false) is MemoryStream dt)
            {
                string UriString;

                using (dt)
                {
                    dt.Position = 0;
                    UriString = Encoding.Unicode.GetString(dt.ToArray());
                }

                var e0 = UriString.IndexOf('\0');
                if (e0 > 0)
                    UriString = UriString.Substring(0, e0);

                UriString = UriString.Split(new char[] { '\r', '\n' })[0];

                if (Utility.TryCreateUri(UriString, out uri))
                    return true;
            }

            uri = null;
            return false;
        }

        public void ShowNotEnoughDiskSpace()
        {
            DialogManager.ShowMessageAsync(this, null, "디스크 공간이 부족합니다!", MessageDialogStyle.Affirmative);
        }
    }
}

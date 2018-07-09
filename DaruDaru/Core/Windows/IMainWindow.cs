using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using DaruDaru.Marumaru.ComicInfo;
using MahApps.Metro.Controls.Dialogs;

namespace DaruDaru.Core.Windows
{
    internal interface IMainWindow
    {
        Window Window { get; }

        void DownloadUri(bool addNewOnly, Uri uri, string comicName);
        void DownloadUri<T>(bool addNewOnly, IEnumerable<T> src, Func<T, Uri> toUri, Func<T, string> toComicName);
        void InsertNewComic(Comic sender, IEnumerable<Comic> newItems, bool removeSender);

        void UpdateTaskbarProgress();

        void WakeDownloader(int count);
        void WakeQueue(int count);

        void SearchArchiveByCodes(string[] codes, string text);

        Task<string> ShowInput(string message, MetroDialogSettings settings = null);
        Task<MessageDialogResult> ShowMessageBox(string message, MessageDialogStyle style = MessageDialogStyle.Affirmative, MetroDialogSettings settings = null);
        void ShowMessageBox(string message, int timeOut);

        Task<bool> ShowMassageBoxTooMany();
    }
}

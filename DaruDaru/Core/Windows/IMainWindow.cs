using System.Collections.Generic;
using DaruDaru.Marumaru.ComicInfo;

namespace DaruDaru.Core.Windows
{
    internal interface IMainWindow
    {
        void InsertNewComic(Comic sender, IEnumerable<Comic> newItems, bool removeSender);

        void WakeDownloader();

        void UpdateTaskbarProgress();

        string GetProtectedCookie(string url);
    }
}

using System.Net;
using DaruDaru.Core.Windows;
using DaruDaru.Utilities;

namespace DaruDaru.Marumaru.ComicInfo
{
    internal class UnknownPage : Comic
    {
        public UnknownPage(IMainWindow mainWindow, bool addNewOnly, string url, string comicName)
            : base(mainWindow, true, addNewOnly, url, comicName)
        {
        }

        protected override bool GetInfomationPriv(ref int count)
        {
            // Short url 검증용 페이지
            string newUrl = null;

            var succ = Retry(() =>
            {
                var req = WebRequest.CreateHttp(this.Url);
                WebClientEx.AddHeader(req);
                req.AllowAutoRedirect = true;

                using (var res = req.GetResponse() as HttpWebResponse)
                    newUrl = res.ResponseUri.AbsoluteUri;

                return true;
            });

            if (succ)
            {
                Comic comic = null;

                if (RegexComic.CheckUrl(newUrl))
                    comic = new WasabiPage(this.IMainWindow, this.AddNewonly, newUrl, null);

                else if (RegexArchive.CheckUrl(newUrl))
                    comic = new MaruPage(this.IMainWindow, this.AddNewonly, newUrl, null);

                if (comic != null)
                    this.IMainWindow.InsertNewComic(this, new Comic[] { comic }, true);
                else
                    this.State = MaruComicState.Error_3_NotSupport;
            }

            // 항상 실패로 전달해서 기록등 기타 작업을 하지 않도록 한다
            return false;
        }
    }
}

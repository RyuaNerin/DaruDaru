using System;
using System.Net;
using DaruDaru.Core.Windows;
using DaruDaru.Utilities;

namespace DaruDaru.Marumaru.ComicInfo
{
    internal class UnknownPage : Comic
    {
        public UnknownPage(bool addNewOnly, Uri uri, string comicName)
            : base(addNewOnly, uri, comicName)
        {
        }

        protected override bool GetInfomationPriv(ref int count)
        {
            // Short uri 검증용 페이지
            Uri newUri = null;

            var succ = Retry(() =>
            {
                var req = WebRequest.CreateHttp(this.Uri);
                WebClientEx.AddHeader(req);
                req.AllowAutoRedirect = true;

                using (var res = req.GetResponse() as HttpWebResponse)
                    newUri = res.ResponseUri;

                return true;
            });

            if (succ)
            {
                Comic comic = null;

                if (DaruUriParser.Marumaru.CheckUri(newUri))
                    comic = new WasabiPage(this.AddNewonly, newUri, null);

                else if (DaruUriParser.Archive.CheckUri(newUri))
                    comic = new MaruPage(this.AddNewonly, newUri, null);

                if (comic != null)
                    MainWindow.Instance.InsertNewComic(this, new Comic[] { comic }, true);
                else
                    this.State = MaruComicState.Error_3_NotSupport;
            }

            // 항상 실패로 전달해서 기록등 기타 작업을 하지 않도록 한다
            return false;
        }
    }
}

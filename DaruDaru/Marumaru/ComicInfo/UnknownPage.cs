using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DaruDaru.Config;
using DaruDaru.Core.Windows;

namespace DaruDaru.Marumaru.ComicInfo
{
    internal class UnknownPage : Comic
    {
        public UnknownPage(IMainWindow mainWindow, bool fromSearch, bool addNewOnly, string url, string comicName)
            : base(mainWindow, fromSearch, addNewOnly, url, comicName)
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

                if (Regexes.RegexArchive.IsMatch(newUrl))
                    comic = new WasabiPage(this.m_mainWindow, true, this.m_addNewOnly, newUrl, this.ComicName, null);

                else if (Regexes.MarumaruRegex.IsMatch(newUrl))
                    comic = new MaruPage(this.m_mainWindow, true, this.m_addNewOnly, newUrl, this.ComicName);

                if (comic != null)
                    this.m_mainWindow.InsertNewComic(this, new Comic[] { comic }, true);
                else
                {
                    SearchLogManager.ChangeUrlUnsafe(this.Url, newUrl);

                    this.State = MaruComicState.Error_3_NotSupport;
                }
            }

            // 항상 실패로 전달해서 기록등 기타 작업을 하지 않도록 한다
            return false;
        }
    }
}

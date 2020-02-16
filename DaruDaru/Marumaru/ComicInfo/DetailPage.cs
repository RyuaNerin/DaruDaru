using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using DaruDaru.Config;
using DaruDaru.Core.Windows;
using DaruDaru.Utilities;
using HtmlAgilityPack;
using Sentry;

namespace DaruDaru.Marumaru.ComicInfo
{
    internal class DetailPage : Comic
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="addNewOnly">[새 작품으로 검색] 으로 검색됨</param>
        /// <param name="uri"></param>
        /// <param name="comicName"></param>
        /// <param name="skip">Status 를 Skip 으로 표시할 것인지</param>
        public DetailPage(bool addNewOnly, Uri uri, string comicName, bool skip)
            : base(addNewOnly, DaruUriParser.Detail.GetUri(DaruUriParser.Detail.GetCode(uri)), comicName)
        {
            if (skip)
                this.State = MaruComicState.Complete_4_Skip;
        }

        public string DirPath => Path.Combine(this.ConfigCur.SavePath, Utility.ReplaceInvalid(this.Title));

        public struct Links
        {
            public Uri Uri;
            public string MangaCode;
            public string MangaTitle;
        }
        public class DetailInfomation
        {
            public List<Links>  MangaList { get; } = new List<Links>();
            public Uri          NewUri;
            public string       MaruCode;
            public string       Title;
            public bool         IsFinished;
        }
        protected override bool GetInfomationPriv(HttpClientEx hc, ref int count)
        {
            DetailInfomation detailInfo = null;
            HttpStatusCode lastStatusCode = 0;

            var retrySuccess = Utility.Retry((retries) => (detailInfo = this.GetInfomationWorker(hc, retries, out lastStatusCode)) != null);

            if (!retrySuccess)
            {
                this.SetStatusFromHttpStatusCode(lastStatusCode);
                return false;
            }

            this.Title = detailInfo.Title;
            this.Uri   = detailInfo.NewUri;

            try
            {
                ArchiveManager.UpdateDetail(detailInfo.MaruCode, this.Title, detailInfo.MangaList.Select(e => e.MangaCode).ToArray(), detailInfo.IsFinished);

                IEnumerable<Links> items = detailInfo.MangaList;

                if (this.AddNewonly)
                    items = ArchiveManager.IsNewManga(items, e => DaruUriParser.Manga.GetCode(e.Uri));

                var comics = items.Select(e => new MangaPage(this.AddNewonly, e.Uri, this.Title, e.MangaTitle)).ToArray();
                
                var noNew = this.AddNewonly && comics.Length == 0;

                MainWindow.Instance.InsertNewComic(this, comics, !noNew);

                if (noNew)
                {
                    this.State = MaruComicState.Complete_3_NoNew;
                    MainWindow.Instance.UpdateTaskbarProgress();
                }

                count = detailInfo.MangaList.Count;

                // Create Shortcut
                if (this.ConfigCur.CreateUrlLink)
                {
                    Directory.CreateDirectory(this.ConfigCur.UrlLinkPath);

                    var path = Path.Combine(this.ConfigCur.UrlLinkPath, $"{Utility.ReplaceInvalid(this.Title)}.url");
                    if (!File.Exists(path))
                        File.WriteAllText(path, $"[InternetShortcut]\r\nURL=" + this.Uri.AbsoluteUri);
                }

                return count > 0;
            }
            catch (Exception ex)
            {
                this.State = MaruComicState.Error_1_Error;

                SentrySdk.CaptureException(ex);
            }

            return false;
        }

        private DetailInfomation GetInfomationWorker(HttpClientEx hc, int retries, out HttpStatusCode statusCode)
        {
            using (var req = new HttpRequestMessage(HttpMethod.Get, this.Uri))
            using (var res = this.CallRequest(hc, req))
            {
                statusCode = res.StatusCode;
                if (this.WaitFromHttpStatusCode(retries, statusCode))
                    return null;

                var html = res.Content.ReadAsStringAsync().Exec();
                if (string.IsNullOrWhiteSpace(html))
                    return null;

                return GetDetailInfomation(hc, res.RequestMessage.RequestUri ?? this.Uri, html);
            }
        }

        public static DetailInfomation GetDetailInfomation(HttpClientEx hc, Uri uri, string body)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(body);

            var node = doc.DocumentNode;

            var detailInfo = new DetailInfomation
            {
                NewUri = uri,
                MaruCode = DaruUriParser.Detail.GetCode(uri),
                IsFinished = Utility.ReplcaeHtmlTag(node.SelectSingleNode(".//a[contains(@class, 'publish_type')]")?.InnerText) == "완결",
                Title = Utility.ReplcaeHtmlTag(node.SelectSingleNode(".//div[contains(@class, 'red') and contains(@class, 'title')]").InnerText.Replace("\n", "")).Trim(),
            };

            var mangaDetail = node.SelectSingleNode(".//div[contains(@class, 'manga-detail-list')]");
            if (mangaDetail == null)
                return null;

            foreach (var slot in mangaDetail.SelectNodes(".//div[contains(@class, 'slot')]").ToArray())
            {
                Uri mangaUri = null;

                {
                    var mangaCode = slot.GetAttributeValue("data-wrid", null);
                    if (mangaCode != null)
                    {
                        mangaUri = DaruUriParser.Manga.GetUri(mangaCode);
                    }
                }

                if (mangaUri == null)
                {
                    foreach (var a in slot.SelectNodes(".//a[contains(@class, 'href')]"))
                    {
                        var href = a.Attributes["href"].Value;
                        if (href == "#")
                            continue;

                        if (Utility.TryCreateUri(detailInfo.NewUri, href, out mangaUri))
                        {
                            if (DaruUriParser.Manga.CheckUri(mangaUri))
                                break;

                            if (Utility.ResolvUri(hc, mangaUri, out mangaUri) && DaruUriParser.Manga.CheckUri(mangaUri))
                                break;
                        }
                    }
                }

                if (mangaUri == null)
                    continue;

                string title = null;

                var titleNode = slot.SelectSingleNode(".//div[contains(@class, 'title')]");
                if (titleNode != null)
                {
                    foreach (var child in titleNode.ChildNodes.ToArray())
                    {
                        if (child.NodeType == HtmlNodeType.Element)
                            child.Remove();
                    }

                    title = Utility.ReplaceHtmlTagAndRemoveTab(titleNode.InnerText ?? string.Empty);
                }

                detailInfo.MangaList.Add(new Links
                {
                    Uri        = mangaUri,
                    MangaCode  = DaruUriParser.Manga.GetCode(mangaUri),
                    MangaTitle = title,
                });
            }

            // 내림차순에서 오름차순으로 변경
            detailInfo.MangaList.Reverse();

            return detailInfo;
        }
    }
}

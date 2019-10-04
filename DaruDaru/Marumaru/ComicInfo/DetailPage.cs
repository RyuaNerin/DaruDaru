using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DaruDaru.Config;
using DaruDaru.Core;
using DaruDaru.Core.Windows;
using DaruDaru.Utilities;
using HtmlAgilityPack;

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
            : base(addNewOnly, uri, comicName)
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
            public bool         OccurredError;
        }
        protected override bool GetInfomationPriv(ref int count)
        {
            DetailInfomation detailInfo = null;

            bool retrySuccess;
            using (var wc = new WebClientEx())
                retrySuccess = Utility.Retry(() => (detailInfo = this.GetInfomationWorker(wc)) != null);

            if (detailInfo.OccurredError)
                return false;

            if (!retrySuccess || detailInfo.MangaList.Count == 0)
            {
                this.State = MaruComicState.Error_1_Error;
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

                CrashReport.Error(ex);
            }

            return false;
        }

        private DetailInfomation GetInfomationWorker(WebClientEx wc)
        {
            var html = this.GetHtml(wc, this.Uri);
            if (html == null)
            {
                return new DetailInfomation
                {
                    OccurredError = true,
                };
            }


            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            return GetDetailInfomation(wc.ResponseUri ?? this.Uri, doc.DocumentNode);
        }

        public static DetailInfomation GetDetailInfomation(Uri uri, HtmlNode node)
        {
            var detailInfo = new DetailInfomation
            {
                NewUri = uri,
                MaruCode = DaruUriParser.Detail.GetCode(uri),
                IsFinished = Utility.ReplcaeHtmlTag(node.SelectSingleNode(".//a[@class='publish_type']")?.InnerText) == "완결",
                Title = Utility.ReplcaeHtmlTag(node.SelectSingleNode(".//div[@class='red title']").InnerText.Replace("\n", "")).Trim(),
            };

            var mangaDetail = node.SelectSingleNode(".//div[@class='manga-detail-list']");
            if (mangaDetail == null)
                return null;

            foreach (var slot in mangaDetail.SelectNodes(".//div[@class='slot']"))
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
                    foreach (var a in slot.SelectNodes(".//a[@class='href']"))
                    {
                        var href = a.Attributes["href"].Value;
                        if (href == "#")
                            continue;

                        if (Utility.TryCreateUri(detailInfo.NewUri, href, out mangaUri))
                        {
                            if (DaruUriParser.Manga.CheckUri(mangaUri))
                                break;

                            if (Utility.ResolvUri(mangaUri, out mangaUri) && DaruUriParser.Manga.CheckUri(mangaUri))
                                break;
                        }
                    }
                }

                if (mangaUri == null)
                    continue;

                string title = null;

                var titleNode = slot.SelectSingleNode(".//div[@class='title']");
                if (titleNode != null)
                {
                    foreach (var child in titleNode.ChildNodes)
                    {
                        if (child.NodeType == HtmlNodeType.Element)
                            child.Remove();
                    }

                    title = Utility.ReplcaeHtmlTag(titleNode.InnerText ?? string.Empty);
                }

                detailInfo.MangaList.Add(new Links
                {
                    Uri        = mangaUri,
                    MangaCode  = DaruUriParser.Manga.GetCode(mangaUri),
                    MangaTitle = title,
                });
            }


            return detailInfo;
        }
    }
}

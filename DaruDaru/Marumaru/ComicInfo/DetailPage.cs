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
        public struct DetailInfomation
        {
            public List<Links>  MangaList;
            public Uri          NewUri;
            public string       MaruCode;
            public string       Title;
            public bool         IsFinished;
            public bool         OccurredError;
        }
        protected override bool GetInfomationPriv(ref int count)
        {
            var args = new DetailInfomation
            {
                MangaList = new List<Links>(),
            };

            bool retrySuccess;
            using (var wc = new WebClientEx())
                retrySuccess = Utility.Retry(() => this.GetInfomationWorker(wc, ref args));

            if (args.OccurredError)
                return false;

            if (!retrySuccess || args.MangaList.Count == 0)
            {
                this.State = MaruComicState.Error_1_Error;
                return false;
            }

            this.Title = args.Title;
            this.Uri   = args.NewUri;

            try
            {
                ArchiveManager.UpdateDetail(args.MaruCode, this.Title, args.MangaList.Select(e => e.MangaCode).ToArray(), args.IsFinished);

                IEnumerable<Links> items = args.MangaList;

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

                count = args.MangaList.Count;

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

        private bool GetInfomationWorker(WebClientEx wc, ref DetailInfomation args)
        {
            var html = this.GetHtml(wc, this.Uri);
            if (html == null)
            {
                args.OccurredError = true;
                return false;
            }

            args.NewUri = wc.ResponseUri ?? this.Uri;
            args.MaruCode = DaruUriParser.Detail.GetCode(args.NewUri);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            return GetDetailInfomation(doc.DocumentNode, ref args);
        }

        public static bool GetDetailInfomation(HtmlNode node, ref DetailInfomation args)
        {
            args.MangaList.Clear();

            args.IsFinished = Utility.ReplcaeHtmlTag(node.SelectSingleNode(".//a[@class='publish_type']")?.InnerText) == "완결";

            var mangaDetail = node.SelectSingleNode(".//div[@class='manga-detail-list']");
            if (mangaDetail == null)
                return false;

            foreach (var a in mangaDetail.SelectNodes(".//a[@href]"))
            {
                var href = a.Attributes["href"].Value;
                if (href == "#")
                    continue;

                if (Utility.TryCreateUri(args.NewUri, href, out Uri a_uri))
                {
                    if (!DaruUriParser.Manga.CheckUri(a_uri) && !Utility.ResolvUri(a_uri, out a_uri))
                        continue;

                    if (DaruUriParser.Manga.CheckUri(a_uri))
                    {
                        var titleNo = a.InnerText;

                        if (!string.IsNullOrWhiteSpace(titleNo))
                        {
                            var title = a.SelectSingleNode(".//div[@class='title']");

                            args.MangaList.Add(new Links
                            {
                                Uri        = a_uri,
                                MangaCode  = DaruUriParser.Manga.GetCode(a_uri),
                                MangaTitle = Utility.ReplcaeHtmlTag(title?.InnerText),
                            });
                        }
                    }
                }
            }

            args.Title = Utility.ReplcaeHtmlTag(node.SelectSingleNode(".//div[@class='red title']").InnerText.Replace("\n", "")).Trim();

            return true;
        }
    }
}

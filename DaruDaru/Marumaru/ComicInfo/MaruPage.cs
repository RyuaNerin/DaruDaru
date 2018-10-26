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
    internal class MaruPage : Comic
    {
        public MaruPage(bool addNewOnly, Uri uri, string comicName, bool skip)
            : base(addNewOnly, uri, comicName)
        {
            if (skip)
                this.State = MaruComicState.Complete_4_Skip;
        }

        public string DirPath => Path.Combine(this.ConfigCur.SavePath, Utility.ReplaceInvalid(this.Title));

        private struct Links
        {
            public Uri Uri;
            public string TitleNo;
        }
        private struct Args
        {
            public List<Links>  Archives;
            public Uri          NewUri;
            public string       MaruCode;
            public string       Title;
            public bool         IsFinished;
        }
        protected override bool GetInfomationPriv(ref int count)
        {
            var args = new Args
            {
                Archives = new List<Links>()
            };

            bool success;
            using (var wc = new WebClientEx())
                success = Utility.Retry(() => this.GetInfomationWorker(wc, ref args));

            this.Title = args.Title;

            if (!success || args.Archives.Count == 0)
            {
                this.State = MaruComicState.Error_1_Error;
                return false;
            }

            this.Uri = args.NewUri;

            try
            {
                ArchiveManager.UpdateMarumaru(args.MaruCode, this.Title, args.Archives.Select(e => DaruUriParser.Archive.GetCode(e.Uri)).ToArray(), args.IsFinished);

                IEnumerable<Links> items = args.Archives;

                if (this.AddNewonly)
                    items = ArchiveManager.IsNewArchive(items, e => DaruUriParser.Archive.GetCode(e.Uri));

                var comics = items.Select(e => new WasabiPage(this.AddNewonly, e.Uri, this.Title, e.TitleNo)).ToArray();
                
                var noNew = this.AddNewonly && comics.Length == 0;

                MainWindow.Instance.InsertNewComic(this, comics, !noNew);

                if (noNew)
                {
                    this.State = MaruComicState.Complete_3_NoNew;
                    MainWindow.Instance.UpdateTaskbarProgress();
                }

                count = args.Archives.Count;

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

        private bool GetInfomationWorker(WebClientEx wc, ref Args args)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(wc.DownloadString(this.Uri));

            args.NewUri = wc.ResponseUri ?? this.Uri;
            args.MaruCode = DaruUriParser.Marumaru.GetCode(args.NewUri);

            HtmlNode rcontent = null;
            HtmlNode vcontent = null;
            try
            {
                rcontent = doc.DocumentNode.SelectSingleNode("//div[@id='rcontent']");
                vcontent = rcontent.SelectSingleNode(".//div[@id='vContent']");
            }
            catch (Exception ex)
            {
                if (doc.DocumentNode.InnerHtml.Contains("서비스 점검"))
                    throw new MaruSystemException();
                else
                    throw ex;
            }

            args.Title = Utility.ReplcaeHtmlTag(rcontent.SelectSingleNode(".//div[@class='subject']").InnerText.Replace("\n", "")).Trim();

            var isMangaup = false;
            string titleNo;

            args.Archives.Clear();
            foreach (var a in vcontent.SelectNodes(".//a[@href]"))
            {
                var href = a.Attributes["href"].Value;
                if (href == "#")
                    continue;

                if (Utility.TryCreateUri(args.NewUri, href, out Uri a_uri))
                {
                    if (!DaruUriParser.Archive.CheckUri(a_uri) &&
                        !DaruUriParser.Marumaru.CheckUri(a_uri) &&
                        !Utility.ResolvUri(a_uri, out a_uri))
                        continue;

                    if (DaruUriParser.Archive.CheckUri(a_uri))
                    {
                        titleNo = a.InnerText;

                        if (!string.IsNullOrWhiteSpace(titleNo))
                            args.Archives.Add(new Links
                            {
                                Uri = a_uri,
                                TitleNo = Utility.ReplcaeHtmlTag(a.InnerText)
                            });
                    }
                    else if (DaruUriParser.Marumaru.CheckUri(a_uri))
                    {
                        if (Utility.ReplcaeHtmlTag(a.InnerText).IndexOf("전편 보러가기") >= 0)
                        {
                            isMangaup = true;
                            args.NewUri = a_uri;
                        }
                        else
                        {
                            if (DaruUriParser.Marumaru.GetCode(a_uri) == args.MaruCode)
                            {
                                var innerText = Utility.ReplcaeHtmlTag(a.InnerText);

                                if (innerText.StartsWith("[완결]") || innerText.StartsWith("[단편]"))
                                    args.IsFinished = true;
                            }
                        }
                    }
                }
            }

            if (isMangaup)
            {
                doc.LoadHtml(wc.DownloadString(args.NewUri));

                rcontent = doc.DocumentNode.SelectSingleNode("//div[@id='rcontent']");

                args.Title = Utility.ReplcaeHtmlTag(rcontent.SelectSingleNode(".//div[@class='subject']").InnerText.Replace("\n", "")).Trim();
            }

            return true;
        }
    }
}

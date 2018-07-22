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
        public MaruPage(bool addNewOnly, Uri uri, string comicName)
            : base(addNewOnly, uri, comicName)
        {
        }

        struct WasabisyrupLinks
        {
            public Uri Uri;
            public string TitleNo;
        }

        protected override bool GetInfomationPriv(ref int count)
        {
            var lstArchives = new List<WasabisyrupLinks>();
            Uri newUri = null;
            string title = null;

            using (var wc = new WebClientEx())
            {
                var doc = new HtmlDocument();

                var success = Utility.Retry(() =>
                {
                    doc.LoadHtml(wc.DownloadString(this.Uri));

                    newUri = wc.ResponseUri ?? this.Uri;

                    var rcontent = doc.DocumentNode.SelectSingleNode("//div[@id='rcontent']");
                    var vcontent = rcontent.SelectSingleNode(".//div[@id='vContent']");

                    title = Utility.ReplcaeHtmlTag(rcontent.SelectSingleNode(".//div[@class='subject']").InnerText.Replace("\n", "")).Trim();

                    var isMangaup = false;
                    string titleNo;
                    foreach (var a in vcontent.SelectNodes(".//a[@href]"))
                    {
                        var href = a.Attributes["href"].Value;
                        if (href == "#")
                            continue;

                        if (Utility.TryCreateUri(newUri, href, out Uri a_uri))
                        {
                            if (DaruUriParser.Archive.CheckUri(a_uri))
                            {
                                titleNo = a.InnerText;

                                if (!string.IsNullOrWhiteSpace(titleNo))
                                    lstArchives.Add(new WasabisyrupLinks
                                    {
                                        Uri     = a_uri,
                                        TitleNo = Utility.ReplcaeHtmlTag(a.InnerText)
                                    });
                            }
                            else if (DaruUriParser.Marumaru.CheckUri(a_uri) && Utility.ReplcaeHtmlTag(a.InnerText).IndexOf("전편 보러가기") >= 0)
                            {
                                isMangaup = true;
                                newUri = a_uri;
                            }
                        }
                    }

                    if (isMangaup)
                    {
                        doc.LoadHtml(wc.DownloadString(newUri));

                        rcontent = doc.DocumentNode.SelectSingleNode("//div[@id='rcontent']");

                        title = Utility.ReplcaeHtmlTag(rcontent.SelectSingleNode(".//div[@class='subject']").InnerText.Replace("\n", "")).Trim();
                    }

                    return true;
                });

                this.Title = title;

                if (!success || lstArchives.Count == 0)
                {
                    this.State = MaruComicState.Error_1_Error;
                    return false;
                }
            }

            this.Uri = newUri;

            try
            {
                ArchiveManager.UpdateMarumaru(DaruUriParser.Marumaru.GetCode(this.Uri), this.Title, lstArchives.Select(e => DaruUriParser.Archive.GetCode(e.Uri)).ToArray());

                IEnumerable<WasabisyrupLinks> items = lstArchives;

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

                count = lstArchives.Count;

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
    }
}

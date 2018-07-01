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
        public MaruPage(IMainWindow mainWindow, bool addNewOnly, string url, string comicName)
            : base(mainWindow, true, addNewOnly, url, comicName)
        {
        }

        struct WasabisyrupLinks
        {
            public string Url;
            public string TitleNo;
        }

        protected override bool GetInfomationPriv(ref int count)
        {
            var lstArchives = new List<WasabisyrupLinks>();

            var baseUri = new Uri(this.Url);

            using (var wc = new WebClientEx())
            {
                var doc = new HtmlDocument();

                var success = Retry(() =>
                {
                    doc.LoadHtml(wc.DownloadString(this.Url));

                    this.Title = ReplcaeHtmlTag(doc.DocumentNode.SelectSingleNode("//div[@class='subject']").InnerText.Replace("\n", "")).Trim();

                    string titleNo;
                    foreach (var a in doc.DocumentNode.SelectSingleNode("//div[@class='content']").SelectNodes(".//a[@href]"))
                    {
                        var a_url = new Uri(baseUri, a.Attributes["href"].Value).AbsoluteUri;

                        if (RegexArchive.CheckUrl(a_url))
                        {
                            titleNo = a.InnerText;

                            if (!string.IsNullOrWhiteSpace(titleNo))
                                lstArchives.Add(new WasabisyrupLinks
                                {
                                    Url = a_url,
                                    TitleNo = ReplcaeHtmlTag(a.InnerText)
                                });
                        }
                    }

                    return true;
                });

                if (!success || lstArchives.Count == 0)
                {
                    this.State = MaruComicState.Error_1_Error;
                    return false;
                }
            }

            try
            {
                ArchiveManager.UpdateMarumaru(RegexComic.GetCode(this.Url), this.Title);

                IEnumerable<WasabisyrupLinks> items = lstArchives;

                if (this.AddNewonly)
                    items = ArchiveManager.IsNewArchive(items, e => RegexArchive.GetCode(e.Url));

                var comics = items.Select(e => new WasabiPage(this.IMainWindow, this.AddNewonly, e.Url, this.Title, e.TitleNo)).ToArray();
                
                var noNew = this.AddNewonly && comics.Length == 0;

                this.IMainWindow.InsertNewComic(this, comics, !noNew);

                if (noNew)
                {
                    this.State = MaruComicState.Complete_3_NoNew;
                    this.IMainWindow.UpdateTaskbarProgress();
                }

                count = lstArchives.Count;

                // Create Shortcut
                if (this.ConfigCur.CreateUrlLink)
                {
                    Directory.CreateDirectory(this.ConfigCur.UrlLinkPath);
                    File.WriteAllText(Path.Combine(this.ConfigCur.UrlLinkPath, $"{ReplaceInvalid(this.Title)}.url"), $"[InternetShortcut]\r\nURL=" + this.Url);
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DaruDaru.Core;
using DaruDaru.Core.Windows;
using HtmlAgilityPack;

namespace DaruDaru.Marumaru.ComicInfo
{
    internal class MaruPage : Comic
    {
        public MaruPage(IMainWindow mainWindow, bool fromSearch, bool addNewOnly, string url, string comicName)
            : base(mainWindow, fromSearch, addNewOnly, url, comicName)
        {
        }

        struct WasabisyrupLinks
        {
            public string Url;
            public string TitleNo;
        }

        protected override void GetInfomationPriv(ref int count)
        {
            var lstArchives = new List<WasabisyrupLinks>();

            var baseUri = new Uri(this.Url);

            using (var wc = new WebClientEx())
            {
                var doc = new HtmlDocument();

                var success = Retry(() =>
                {
                    doc.LoadHtml(wc.DownloadString(this.Url));

                    this.ComicName = ReplcaeHtmlTag(doc.DocumentNode.SelectSingleNode("//div[@class='subject']").InnerText.Replace("\n", ""));

                    string titleNo;
                    foreach (var a in doc.DocumentNode.SelectSingleNode("//div[@class='content']").SelectNodes(".//a[@href]"))
                    {
                        var a_url = new Uri(baseUri, a.Attributes["href"].Value).ToString();

                        var match = Regexes.RegexArchive.Match(a_url);
                        if (match.Success)
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
                    return;
                }
            }

            try
            {
                // Create Shortcut
                Directory.CreateDirectory(App.BaseDirectory);
                File.WriteAllText(Path.Combine(App.BaseDirectory, $"{ReplaceInvalid(this.ComicName)}.url"), $"[InternetShortcut]\r\nURL=" + this.Url);

                IEnumerable<WasabisyrupLinks> items = lstArchives;

                if (this.m_addNewOnly)
                    items = ArchiveLog.CheckNewUrl(items, e => WasabiPage.GetArchiveCode(e.Url));

                var comics = items.Select(e => new WasabiPage(this.m_mainWindow, false, false, e.Url, this.ComicName, e.TitleNo)).ToArray();
                var noNew = this.m_addNewOnly && comics.Length == 0;

                this.m_mainWindow.InsertNewComic(this, comics, noNew);

                if (noNew)
                    this.State = MaruComicState.Complete_3_NoNew;

                count = lstArchives.Count;
            }
            catch (Exception ex)
            {
                this.State = MaruComicState.Error_1_Error;

                CrashReport.Error(ex);
            }
        }
    }
}

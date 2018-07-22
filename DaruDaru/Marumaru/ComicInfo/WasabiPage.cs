using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DaruDaru.Config;
using DaruDaru.Core;
using DaruDaru.Core.Windows;
using DaruDaru.Utilities;
using HtmlAgilityPack;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;

namespace DaruDaru.Marumaru.ComicInfo
{
    internal class WasabiPage : Comic
    {
        public WasabiPage(bool addNewOnly, Uri uri, string title, string tempTitleWithNo = null)
            : base(addNewOnly, uri, title)
        {
            this.TitleWithNo = tempTitleWithNo;

            var entry = ArchiveManager.GetArchive(this.ArchiveCode);
            if (entry != null)
            {
                this.TitleWithNo = entry.TitleWithNo;
                this.ZipPath     = entry.ZipPath;
                this.State       = MaruComicState.Complete_2_Archived;

                MainWindow.Instance.WakeDownloader(1);
            }
        }

        private ImageInfomation[] m_images;

        public string ZipPath { get; set; }

        public string ArchiveCode => DaruUriParser.Archive.GetCode(this.Uri);
        
        private class ImageInfomation
        {
            public int Index;
            public Uri ImageUri;
            public string TempPath;
            public string Extension;
        }
        protected override bool GetInfomationPriv(ref int count)
        {
            var lst = new List<ImageInfomation>();
            Uri newUri = null;

            using (var wc = new WebClientEx())
            {
                var doc = new HtmlDocument();
                
                var success = Utility.Retry(() =>
                {
                    wc.Headers.Set(HttpRequestHeader.Referer, this.Uri.AbsoluteUri);
                    var body = wc.DownloadString(this.Uri);

                    newUri = wc.ResponseUri ?? this.Uri;

                    doc.LoadHtml(body);

                    // 타이틀은 항상 마루마루 기준으로 맞춤.
                    // 2018-07-10 파일 이름은 Archive 기준으로 맞춤: https://marumaru.in/b/manga/208070
                    var innerTitle = Utility.ReplcaeHtmlTag(doc.DocumentNode.SelectSingleNode("//span[@class='title-subject']").InnerText).Trim();

                    if (string.IsNullOrWhiteSpace(this.Title))
                        this.Title = innerTitle;

                    // 제목이 바뀌는 경우가 있어서
                    // 제목은 그대로 사용하고, xx화 는 새로 가져온다.
                    var titleNo = Utility.ReplcaeHtmlTag(doc.DocumentNode.SelectSingleNode("//span[@class='title-no']").InnerText).Trim();
                    this.TitleWithNo = $"{innerTitle} {titleNo}";

                    // 잠긴 파일
                    if (doc.DocumentNode.SelectSingleNode("//div[@class='pass-box']") != null)
                    {
                        if (doc.DocumentNode.SelectSingleNode("//input[@name='captcha2']") != null)
                        {
                            // Captcha 걸린 파일
                            lst.Add(null);
                            lst.Add(null);
                        }
                        else
                        {
                            // 실제로 암호걸린 파일
                            lst.Add(null);
                        }

                        return true;
                    }

                    var imgs = doc.DocumentNode.SelectNodes("//img[@data-src]");
                    if (imgs != null && imgs.Count > 0)
                    {
                        foreach (var img in imgs)
                        {
                            if (Utility.TryCreateUri(newUri, img.Attributes["data-src"].Value, out Uri imgUri))
                            {
                                lst.Add(new ImageInfomation
                                {
                                    Index    = lst.Count + 1,
                                    ImageUri = imgUri,
                                    TempPath = Path.GetTempFileName()
                                });
                            }
                        }
                    }
                    else
                    {
                        var galleryTemplate = doc.DocumentNode.SelectSingleNode("//div[@class='gallery-template']");

                        var sig = Uri.EscapeDataString(galleryTemplate.Attributes["data-signature"].Value);
                        var key = Uri.EscapeDataString(galleryTemplate.Attributes["data-key"].Value);

                        wc.Headers.Set(HttpRequestHeader.Referer, this.Uri.AbsoluteUri);
                        var jsonDoc = wc.DownloadString($"http://wasabisyrup.com/assets/{this.ArchiveCode}/1.json?signature={sig}&key={key}");

                        var json = JsonConvert.DeserializeObject<Assets>(jsonDoc);

                        if (json.Message != "ok" || json.Status != "ok")
                            return false;

                        foreach (var img in json.Sources)
                        {
                            if (Utility.TryCreateUri(newUri, img, out Uri imgUri))
                            {
                                lst.Add(new ImageInfomation
                                {
                                    Index    = lst.Count + 1,
                                    ImageUri = imgUri,
                                    TempPath = Path.GetTempFileName()
                                });
                            }
                        }
                    }


                    return true;
                });

                if (!success || lst.Count == 0)
                {
                    this.State = MaruComicState.Error_1_Error;
                    return false;
                }

                if (lst.Count == 1 && lst[0] == null)
                {
                    this.State = MaruComicState.Error_2_Protected;
                    return true;
                }

                if (lst.Count == 2 && lst[0] == null)
                {
                    this.State = MaruComicState.Error_4_Captcha;
                    return true;
                }
            }

            this.Uri = newUri;

            this.ProgressValue = 0;
            this.ProgressMaximum = lst.Count;

            this.m_images = lst.ToArray();

            // 다운로드 시작
            this.State = MaruComicState.Working_2_WaitDownload;

            count = 1;

            return true;
        }

        protected override void StartDownloadPriv()
        {
            this.ZipPath = Path.Combine(this.ConfigCur.SavePath, Utility.ReplaceInvalid(this.Title), Utility.ReplaceInvalid(this.TitleWithNo) + ".zip");

            try
            {
                if (!File.Exists(this.ZipPath))
                {
                    if (!Download())
                    {
                        this.State = MaruComicState.Error_1_Error;
                        return;
                    }

                    this.State = MaruComicState.Working_4_Compressing;
                    Compress();
                    this.SpeedOrFileSize = Utility.ToEICFormat(new FileInfo(this.ZipPath).Length);

                    this.State = MaruComicState.Complete_1_Downloaded;

                    // 디렉토리 수정시간 업데이트
                    Directory.SetCreationTime(this.ZipPath, DateTime.Now);
                    Directory.SetLastWriteTime(this.ZipPath, DateTime.Now);
                    Directory.SetLastAccessTime(this.ZipPath, DateTime.Now);
                }
                else
                    this.State = MaruComicState.Complete_2_Archived;
                
                ArchiveManager.UpdateArchive(this.ArchiveCode, this.TitleWithNo, this.ZipPath);
            }
            catch (Exception ex)
            {
                this.State = MaruComicState.Error_1_Error;

                CrashReport.Error(ex);
            }
            finally
            {
                if (this.m_images != null)
                {
                    foreach (var file in this.m_images)
                    {
                        try
                        {
                            File.Delete(file.TempPath);
                        }
                        catch
                        {
                        }
                    }

                    this.m_images = null;
                }
            }
        }

        private bool Download()
        {            
            using (var downloadErrorTokenSource = new CancellationTokenSource())
            {
                var startTime = DateTime.Now;
                long downloaded = 0;

                var taskDownload = Task.Factory.StartNew(() =>
                {
                    var po = new ParallelOptions
                    {
                        CancellationToken = downloadErrorTokenSource.Token
                    };

                    Parallel.ForEach(
                        this.m_images,
                        po,
                        e =>
                        {
                            var succ = Utility.Retry(() =>
                            {
                                var req = WebClientEx.AddHeader(WebRequest.Create(e.ImageUri));
                                if (req is HttpWebRequest hreq)
                                {
                                    hreq.Referer = this.Uri.AbsoluteUri;
                                    hreq.AllowWriteStreamBuffering = false;
                                    hreq.AllowReadStreamBuffering = false;
                                }

                                using (var res = req.GetResponse() as HttpWebResponse)
                                using (var resBody = res.GetResponseStream())
                                {
                                    if (res.ContentType.Contains("text/"))
                                    {
                                        // 파일이 없는 경우
                                        e.Extension = null;
                                        return true;
                                    }

                                    using (var fileStream = new FileStream(e.TempPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                                    {
                                        var buff = new byte[4096];
                                        int read;

                                        while ((read = resBody.Read(buff, 0, 4096)) > 0)
                                        {
                                            Interlocked.Add(ref downloaded, read);
                                            fileStream.Write(buff, 0, read);
                                        }

                                        fileStream.Flush();

                                        fileStream.Position = 0;
                                        e.Extension = Signatures.GetExtension(fileStream);
                                    }
                                }

                                if (e.Extension != null)
                                    this.IncrementProgress();

                                return e.Extension != null;
                            });

                            if (!succ)
                                downloadErrorTokenSource.Cancel();
                        });

                    return downloadErrorTokenSource.IsCancellationRequested;
                });

                double befSpeed = 0;
                while (!taskDownload.Wait(0))
                {
                    Thread.Sleep(500);

                    befSpeed = (befSpeed + Interlocked.Read(ref downloaded) / (DateTime.Now - startTime).TotalSeconds) / 2;

                    this.SpeedOrFileSize = Utility.ToEICFormat(befSpeed, "/s");
                }

                if (downloadErrorTokenSource.IsCancellationRequested)
                    return false;
            }

            this.SpeedOrFileSize = null;

            return true;
        }

        private void Compress()
        {
            var padLength = Math.Min(3, this.m_images.Length.ToString().Length);

            var dir = Path.GetDirectoryName(this.ZipPath);
            dir = Directory.CreateDirectory(dir).FullName;

            this.ZipPath = Path.Combine(dir, Path.GetFileName(this.ZipPath));

            using (var zipFile = new FileStream(this.ZipPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            using (var zipStream = new ZipOutputStream(zipFile))
            {
                zipStream.SetComment(this.Uri.AbsoluteUri + "\nby DaruDaru");
                zipStream.SetLevel(0);

                var buff = new byte[4096];
                int read;
                foreach (var file in this.m_images)
                {
                    if (file.Extension == null)
                        continue;

                    var entry = new ZipEntry(file.Index.ToString().PadLeft(padLength, '0') + file.Extension);

                    zipStream.PutNextEntry(entry);

                    using (var fileStream = File.OpenRead(file.TempPath))
                        while ((read = fileStream.Read(buff, 0, 4096)) > 0)
                            zipStream.Write(buff, 0, read);
                }

                zipFile.Flush();
            }
        }

        private class Assets
        {
            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("sources")]
            public string[] Sources { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }
        }
    }
}

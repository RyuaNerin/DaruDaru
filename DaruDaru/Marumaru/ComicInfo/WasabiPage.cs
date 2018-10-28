using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            : base(addNewOnly, DaruUriParser.Archive.FixUri(uri), title)
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
        private struct GetInfomationArgs
        {
            public List<ImageInfomation> Images;
            public Uri                   NewUri;
            public bool                 IsError;
        }
        protected override bool GetInfomationPriv(ref int count)
        {
            var args = new GetInfomationArgs()
            {
                Images = new List<ImageInfomation>()
            };

            bool success;
            using (var wc = new WebClientEx())
                success = Utility.Retry(() => this.GetInfomationWorker(wc, ref args));

            if (args.IsError)
                return false;

            if (!success || args.Images.Count == 0)
            {
                this.State = MaruComicState.Error_1_Error;
                return false;
            }

            this.Uri = args.NewUri;

            this.ProgressValue = 0;
            this.ProgressMaximum = args.Images.Count;

            this.m_images = args.Images.ToArray();

            // 다운로드 시작
            this.State = MaruComicState.Working_2_WaitDownload;

            count = 1;

            return true;
        }

        private bool GetInfomationWorker(WebClientEx wc, ref GetInfomationArgs args)
        {
            wc.Headers.Set(HttpRequestHeader.Referer, this.Uri.AbsoluteUri);
            var body = wc.DownloadString(this.Uri);
            if (wc.LastStatusCode == HttpStatusCode.NotFound)
            {
                args.IsError = true;
                this.State = MaruComicState.Error_5_NotFound;
                return true;
            }

            args.NewUri = wc.ResponseUri ?? this.Uri;

            var doc = new HtmlDocument();
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

            // 제목이 설정되어 있지 않은 경우가 있음. 이럴땐 에러로 처리.
            if (string.IsNullOrWhiteSpace(this.TitleWithNo.Trim()))
                return false;

            // 잠긴 파일
            if (doc.DocumentNode.SelectSingleNode("//div[@class='pass-box']") != null)
            {
                if (doc.DocumentNode.SelectSingleNode("//input[@name='captcha2']") != null)
                {
                    // Captcha 걸린 파일
                    if (Recaptcha.LastPostData != null)
                    {
                        wc.Headers.Set(HttpRequestHeader.ContentType, "application/x-www-form-urlencoded; charset=UTF-8");
                        body = wc.UploadString(args.NewUri, "POST", Recaptcha.LastPostData);
                        doc.LoadHtml(body);

                        if (doc.DocumentNode.SelectSingleNode("//div[@class='pass-box']") != null ||
                            doc.DocumentNode.SelectSingleNode("//input[@name='captcha2']") != null)
                        {
                            args.IsError = true;
                            this.State = MaruComicState.Error_4_Captcha;
                            return true;
                        }
                    }
                    else
                    {
                        args.IsError = true;
                        this.State = MaruComicState.Error_4_Captcha;
                        return true;
                    }
                }
                else
                {
                    // 실제로 암호걸린 파일
                    args.IsError = true;
                    this.State = MaruComicState.Error_2_Protected;
                    return true;
                }
            }

            var imgs = doc.DocumentNode.SelectNodes("//img[@data-src]");
            if (imgs != null && imgs.Count > 0)
            {
                foreach (var img in imgs)
                {
                    if (Utility.TryCreateUri(args.NewUri, img.Attributes["data-src"].Value, out Uri imgUri))
                    {
                        args.Images.Add(new ImageInfomation
                        {
                            Index = args.Images.Count + 1,
                            ImageUri = imgUri,
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
                    if (Utility.TryCreateUri(args.NewUri, img, out Uri imgUri))
                    {
                        args.Images.Add(new ImageInfomation
                        {
                            Index = args.Images.Count + 1,
                            ImageUri = imgUri,
                        });
                    }
                }
            }

            return true;
        }

        protected override void StartDownloadPriv()
        {
            this.ZipPath = Path.Combine(this.ConfigCur.SavePath, Utility.ReplaceInvalid(this.Title), Utility.ReplaceInvalid(this.TitleWithNo) + ".zip");

            var tempPath = Path.GetTempFileName();

            try
            {
                foreach (var e in this.m_images)
                    e.TempPath = Path.GetTempFileName();

                if (!this.Download())
                {
                    this.State = MaruComicState.Error_1_Error;
                    return;
                }

                this.State = MaruComicState.Working_4_Compressing;
                this.Compress(tempPath);

                bool fileMode = true;
                if (File.Exists(this.ZipPath))
                {
                    string comment = null;

                    try
                    {
                        using (var fs = new FileStream(this.ZipPath, FileMode.Open, FileAccess.Read))
                        using (var zf = new ZipFile(fs))
                        {
                            if (!string.IsNullOrWhiteSpace(zf.ZipFileComment))
                                comment = zf.ZipFileComment;
                        }
                    }
                    catch
                    {
                    }

                    if (
                        new FileInfo(this.ZipPath).Length == new FileInfo(tempPath).Length ||
                        (
                            comment != null &&
                            Utility.TryCreateUri(comment.Split('\n')[0], out Uri uri) &&
                            DaruUriParser.Archive.GetCode(uri) == this.ArchiveCode
                        ))
                    {
                        fileMode = false;
                        this.State = MaruComicState.Complete_2_Archived;
                    }
                }
                if (fileMode)
                {
                    MoveFile(tempPath, this.ZipPath);
                    this.SpeedOrFileSize = Utility.ToEICFormat(new FileInfo(this.ZipPath).Length);
                    this.State = MaruComicState.Complete_1_Downloaded;
                }
                
                // 디렉토리 수정시간 업데이트
                Directory.SetCreationTime(this.ZipPath, DateTime.Now);
                Directory.SetLastWriteTime(this.ZipPath, DateTime.Now);
                Directory.SetLastAccessTime(this.ZipPath, DateTime.Now);
                
                ArchiveManager.UpdateArchive(this.ArchiveCode, this.TitleWithNo, this.ZipPath);
            }
            catch (Exception ex)
            {
                this.SpeedOrFileSize = null;
                this.State = MaruComicState.Error_1_Error;

                CrashReport.Error(ex);
            }
            finally
            {
                if (this.m_images != null)
                {
                    foreach (var file in this.m_images)
                    {
                        if (file.TempPath != null)
                        {
                            try
                            {
                                File.Delete(file.TempPath);
                            }
                            catch
                            {
                            }
                        }

                        file.TempPath = null;
                    }

                    this.m_images = null;
                }

                try
                {
                    File.Delete(tempPath);
                }
                catch
                {
                }
            }
        }

        private static string MoveFile(string orig, string dest)
        {
            var dir = Directory.CreateDirectory(Path.GetDirectoryName(dest)).FullName;
            dest = Path.Combine(dir, Path.GetFileName(dest));

            try
            {
                File.Move(orig, dest);
            }
            catch (IOException)
            {
                var i = 2;
                string newZipPath;

                do
                {
                    newZipPath = Path.Combine(
                        dir,
                        string.Format(
                            "{0} ({1}){2}",
                            Path.GetFileNameWithoutExtension(dest),
                            i++,
                            Path.GetExtension(dest))
                        );

                    try
                    {
                        File.Move(orig, newZipPath);
                        dest = newZipPath;
                        break;
                    }
                    catch (IOException)
                    {
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                } while (true);
            }
            catch (Exception e)
            {
                throw e;
            }

            return dest;
        }

        private long m_downloaded;
        private bool Download()
        {
            var startTime = DateTime.Now;

            this.m_downloaded = 0;
            var taskDownload = Task.Factory.StartNew(() =>
            {
                return Parallel.ForEach(
                    this.m_images,
                    (e, state) =>
                    {
                        var succ = Utility.Retry(() => this.DownloadWorker(e));

                        if (!succ)
                            state.Stop();
                    }).IsCompleted;
            });

            double befSpeed = 0;
            while (!taskDownload.Wait(0))
            {
                Thread.Sleep(500);

                befSpeed = (befSpeed + Interlocked.Read(ref this.m_downloaded) / (DateTime.Now - startTime).TotalSeconds) / 2;

                this.SpeedOrFileSize = Utility.ToEICFormat(befSpeed, "/s");
            }

            this.SpeedOrFileSize = null;

            // 최소한 하나 이상의 이미지가 포함되어 있어야 함
            return this.m_images.Any(e => e.Extension != null);
        }

        private bool DownloadWorker(ImageInfomation e)
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
                using (var fileStream = new FileStream(e.TempPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    var buff = new byte[4096];
                    int read;

                    while ((read = resBody.Read(buff, 0, 4096)) > 0)
                    {
                        Interlocked.Add(ref this.m_downloaded, read);
                        fileStream.Write(buff, 0, read);
                    }

                    fileStream.Flush();

                    fileStream.Position = 0;
                    e.Extension = Signatures.GetExtension(fileStream);
                }
            }

            this.IncrementProgress();
            return true;
        }

        private void Compress(string tempPath)
        {
            var padLength = Math.Min(3, this.m_images.Length.ToString().Length);

            using (var zipFile = new FileStream(tempPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            using (var zipStream = new ZipOutputStream(zipFile))
            {
                zipFile.SetLength(0);

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

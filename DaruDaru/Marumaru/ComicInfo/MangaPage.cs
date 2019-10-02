using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
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
    internal class MangaPage : Comic
    {
        public MangaPage(bool addNewOnly, Uri uri, string title, string tempTitleWithNo = null)
            : base(addNewOnly, DaruUriParser.Manga.FixUri(uri), title)
        {
            this.TitleWithNo = tempTitleWithNo;

            var entry = ArchiveManager.GetManga(this.ArchiveCode);
            if (entry != null)
            {
                this.TitleWithNo = entry.TitleWithNo;
                this.ZipPath     = entry.ZipPath;
                this.State       = MaruComicState.Complete_2_Archived;

                MainWindow.Instance.WakeDownloader(1);
            }
        }

        private ImageInfomation[] m_images;
        private ImageDecryptor m_decryptor;
        
        public string ZipPath { get; set; }

        public string ArchiveCode => DaruUriParser.Manga.GetCode(this.Uri);
        
        private class ImageInfomation
        {
            public int          Index;
            public Uri[]        ImageUri;
            public FileStream   TempStream;
            public string       Extension;
        }
        private struct GetInfomationArgs
        {
            public List<ImageInfomation> Images;
            public Uri                   NewUri;
            public bool                  OccurredError;
        }

        protected override bool GetInfomationPriv(ref int count)
        {
            var args = new GetInfomationArgs()
            {
                Images = new List<ImageInfomation>()
            };

            bool retrySuccess;
            using (var wc = new WebClientEx())
                retrySuccess = Utility.Retry(() => this.GetInfomationWorker(wc, ref args));

            if (args.OccurredError)
                return false;

            if (!retrySuccess)
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
            var html = this.GetHtml(wc, this.Uri);

            if (html == null)
            {
                args.OccurredError = true;
                return false;
            }

            args.NewUri = wc.ResponseUri ?? this.Uri;

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

#region 폴더 이름은 Detail 에서 설정
            {
                // /bbs/page.php?hid=manga_detail&manga_id=9495
                var toonNav = doc.DocumentNode.SelectSingleNode("//div[@class='toon-nav']");
                if (toonNav == null)
                    return false;

                string detailCode = null;
                foreach (HtmlNode node in toonNav.SelectNodes(".//a[@href]"))
                {
                    var href = node.GetAttributeValue("href", "");
                    if (string.IsNullOrWhiteSpace(href))
                        continue;

                    var code = DaruUriParser.Detail.GetCode(new Uri(this.Uri, href));
                    if (string.IsNullOrWhiteSpace(code))
                        continue;

                    detailCode = code;
                    break;
                }

                var detailEntry = ArchiveManager.GetDetail(detailCode);

                if (detailEntry == null)
                {
                    var info = new DetailPage.DetailInfomation();
                    var htmlDetail = this.GetHtml(wc, DaruUriParser.Detail.GetUri(detailCode));

                    var docDetail = new HtmlDocument();
                    docDetail.LoadHtml(htmlDetail);

                    if (!DetailPage.GetDetailInfomation(docDetail.DocumentNode, ref info))
                        return false;

                    ArchiveManager.UpdateDetail(detailCode, info.Title, info.MangaList.Select(e => e.MangaCode).ToArray());

                    detailEntry = ArchiveManager.GetDetail(detailCode);

                    if (detailEntry == null)
                        return false;
                }

                this.Title = detailEntry.Title;
            }
#endregion

            #region Detail.Title + xx화
            {
                var mangaNo =
                    Regex.Matches(
                        Regex.Match(doc.DocumentNode.InnerHtml, "var only_chapter ?= ?\\[[^;]+\\];").Groups[0].Value,
                        "\\[ *\"([^\"]+)\" *, *\"([^\"]+)\" *\\]"
                    )
                    .Cast<Match>()
                    .FirstOrDefault(e => e.Groups[2].Value == this.ArchiveCode)
                    ?.Groups[1].Value;

                string titleWithNo;
                if (!string.IsNullOrWhiteSpace(mangaNo))
                {
                    titleWithNo = $"{this.Title} {mangaNo}";
                }
                else
                {
                    var title = Utility.ReplcaeHtmlTag(doc.DocumentNode.SelectSingleNode("//meta[@name='title']").GetAttributeValue("content", "")).Trim();
                    if (string.IsNullOrWhiteSpace(title))
                        return false;
                    titleWithNo = title;
                }

                if (string.IsNullOrWhiteSpace(titleWithNo))
                    return false;
                this.TitleWithNo = titleWithNo;
            }
            #endregion

            #region 이미지 정보 가져오는 부분
            {
                var imgList = Regex.Matches(Regex.Match(doc.DocumentNode.InnerHtml, "var img_list = [^;]+").Groups[0].Value, "\"([^\"]+)\"")
                          .Cast<Match>()
                          .Select(e => e.Groups[1].Value.Replace("\\", ""))
                          .Select(e => new Uri(this.Uri, e))
                          .ToArray();
                var imgList1 = Regex.Matches(Regex.Match(html, "var img_list1 = [^;]+").Groups[0].Value, "\"([^\"]+)\"")
                          .Cast<Match>()
                          .Select(e => e.Groups[1].Value.Replace("\\", ""))
                          .Select(e => new Uri(this.Uri, e))
                          .ToArray();

                this.m_images = new ImageInfomation[imgList.Length];
                for (var i = 0; i < imgList.Length; i++)
                {
                    this.m_images[i] = new ImageInfomation();

                    if (i < imgList1.Length)
                    {
                        this.m_images[i].ImageUri = new Uri[]
                        {
                            imgList[i],
                            new Uri(imgList[i].ToString().Replace("//img.", "//s3.")),
                            imgList1[i],
                        };
                    }
                    else
                    {
                        this.m_images[i].ImageUri = new Uri[]
                        {
                            imgList[i],
                            new Uri(imgList[i].ToString().Replace("//img.", "//s3.")),
                        };
                    }
                }
            }
            #endregion

            this.m_decryptor = new ImageDecryptor(html, args.NewUri);

            return true;
        }

        protected override void StartDownloadPriv()
        {
            this.ZipPath = Path.Combine(new DirectoryInfo(Path.Combine(this.ConfigCur.SavePath, Utility.ReplaceInvalid(this.Title))).FullName, Utility.ReplaceInvalid(this.TitleWithNo) + ".zip");

            string tempPath = null;

            try
            {
                foreach (var e in this.m_images)
                    e.TempStream = new FileStream(Path.GetTempFileName(), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose);

                if (!this.Download())
                {
                    this.State = MaruComicState.Error_1_Error;
                    return;
                }

                tempPath = Path.GetTempFileName();

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
                            DaruUriParser.Manga.GetCode(uri) == this.ArchiveCode
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
                
                ArchiveManager.UpdateManga(this.ArchiveCode, this.TitleWithNo, this.ZipPath);
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
                        if (file.TempStream != null)
                            file.TempStream.Dispose();

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

        private const int HR_ERROR_FILE_EXISTS = unchecked((int)0x80070050);
        private static string MoveFile(string orig, string dest)
        {
            var dir = Directory.CreateDirectory(Path.GetDirectoryName(dest)).FullName;
            dest = Path.Combine(dir, Path.GetFileName(dest));

            try
            {
                File.Move(orig, dest);
            }
            catch (IOException ioex) when (ioex.HResult == HR_ERROR_FILE_EXISTS)
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
                    catch (IOException ioex2) when (ioex2.HResult == HR_ERROR_FILE_EXISTS)
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
                        for (var index = 0; index < e.ImageUri.Length; ++index)
                        {
                            var succ = Utility.Retry(() => this.DownloadWorker(e, index));

                            if (!succ)
                                state.Stop();
                        }
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

        private bool DownloadWorker(ImageInfomation e, int uriIndex)
        {
            var req = WebClientEx.AddHeader(WebRequest.Create(e.ImageUri[uriIndex]));
            if (req is HttpWebRequest hreq)
            {
                hreq.Referer = this.Uri.AbsoluteUri;
                hreq.AllowWriteStreamBuffering = false;
                hreq.AllowReadStreamBuffering = false;
            }

            using (var res = req.GetResponse() as HttpWebResponse)
            using (var resBody = res.GetResponseStream())
            {
                e.TempStream.SetLength(0);

                var buff = new byte[4096];
                int read;

                while ((read = resBody.Read(buff, 0, 4096)) > 0)
                {
                    Interlocked.Add(ref this.m_downloaded, read);
                    e.TempStream.Write(buff, 0, read);
                }

                e.TempStream.Flush();

                e.TempStream.Position = 0;
                e.Extension = Signatures.GetExtension(e.TempStream);

                e.TempStream.Position = 0;
            }

            // 이미지 암호화 푸는 작업
            this.m_decryptor.Decrypt(e.TempStream);

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

                    while ((read = file.TempStream.Read(buff, 0, 4096)) > 0)
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
